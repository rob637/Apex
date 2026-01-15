/**
 * Apex Citadels - Combat System Cloud Functions
 * 
 * Handles all combat-related operations:
 * - Territory attacks
 * - Damage calculations
 * - Conquest mechanics
 * - Raid windows
 */

import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';
import {
  Territory,
  Attack,
  AttackType,
  ATTACK_DEFINITIONS,
  User,
  UserResources
} from './types';
import { sendNotification } from './notifications';
import { awardXp, checkAchievements } from './progression';

const db = admin.firestore();

// ============================================================================
// Configuration
// ============================================================================

const RAID_WINDOW = {
  startHour: 18, // 6 PM
  endHour: 22,   // 10 PM
  // Set to null to allow attacks 24/7
  enabled: false // Disabled for MVP testing
};

const CONQUEST_BONUS_RESOURCES = {
  stone: 100,
  wood: 100,
  iron: 50,
  crystal: 25,
  gems: 10,
  arcaneEssence: 5
};

const SHIELD_DURATION_HOURS = 8; // Shield after being conquered

// ============================================================================
// Attack Territory
// ============================================================================

export const attackTerritory = functions.https.onCall(async (data, context) => {
  // Verify authentication
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const { territoryId, attackType } = data as { territoryId: string; attackType: AttackType };

  // Validate attack type
  if (!ATTACK_DEFINITIONS[attackType]) {
    throw new functions.https.HttpsError('invalid-argument', 'Invalid attack type');
  }

  const attackDef = ATTACK_DEFINITIONS[attackType];
  const attackerId = context.auth.uid;

  // Get attacker data
  const attackerDoc = await db.collection('users').doc(attackerId).get();
  if (!attackerDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Attacker not found');
  }
  const attacker = attackerDoc.data() as User;

  // Get territory data
  const territoryDoc = await db.collection('territories').doc(territoryId).get();
  if (!territoryDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Territory not found');
  }
  const territory = territoryDoc.data() as Territory;

  // Can't attack own territory
  if (territory.ownerId === attackerId) {
    throw new functions.https.HttpsError('failed-precondition', 'Cannot attack your own territory');
  }

  // Can't attack alliance member's territory
  if (attacker.allianceId && territory.allianceId === attacker.allianceId) {
    throw new functions.https.HttpsError('failed-precondition', 'Cannot attack alliance member');
  }

  // Check raid window (if enabled)
  if (RAID_WINDOW.enabled) {
    const now = new Date();
    const hour = now.getHours();
    if (hour < RAID_WINDOW.startHour || hour >= RAID_WINDOW.endHour) {
      throw new functions.https.HttpsError(
        'failed-precondition',
        `Raids only allowed between ${RAID_WINDOW.startHour}:00 and ${RAID_WINDOW.endHour}:00`
      );
    }
  }

  // Check shield
  if (territory.shieldExpiresAt && territory.shieldExpiresAt.toDate() > new Date()) {
    const remaining = Math.ceil((territory.shieldExpiresAt.toDate().getTime() - Date.now()) / 60000);
    throw new functions.https.HttpsError(
      'failed-precondition',
      `Territory is shielded for ${remaining} more minutes`
    );
  }

  // Check cooldown
  const cooldownDoc = await db
    .collection('users')
    .doc(attackerId)
    .collection('cooldowns')
    .doc(attackType)
    .get();

  if (cooldownDoc.exists) {
    const lastUsed = cooldownDoc.data()?.lastUsedAt?.toDate();
    if (lastUsed) {
      const elapsed = (Date.now() - lastUsed.getTime()) / 1000;
      if (elapsed < attackDef.cooldownSeconds) {
        const remaining = Math.ceil(attackDef.cooldownSeconds - elapsed);
        throw new functions.https.HttpsError(
          'failed-precondition',
          `Cooldown active. Try again in ${remaining} seconds`
        );
      }
    }
  }

  // Check resource cost
  for (const [resource, cost] of Object.entries(attackDef.resourceCost)) {
    const available = attacker.resources[resource as keyof UserResources] || 0;
    if (available < cost) {
      throw new functions.https.HttpsError(
        'failed-precondition',
        `Not enough ${resource}. Need ${cost}, have ${available}`
      );
    }
  }

  // Get defender data
  const defenderDoc = await db.collection('users').doc(territory.ownerId).get();
  const _defender = defenderDoc.data() as User; // May be used for future battle log enrichment
  void _defender; // Suppress unused warning

  // Calculate damage (with level bonus)
  const levelBonus = 1 + (attacker.stats.level - 1) * 0.05; // 5% per level
  const damage = Math.round(attackDef.baseDamage * levelBonus);

  // Apply damage
  const newHealth = Math.max(0, territory.health - damage);
  const conquered = newHealth === 0;

  // Create attack record
  const attackId = db.collection('attacks').doc().id;
  const attack: Attack = {
    id: attackId,
    attackerId,
    attackerName: attacker.displayName,
    attackerAllianceId: attacker.allianceId,
    defenderId: territory.ownerId,
    defenderName: territory.ownerName,
    defenderAllianceId: territory.allianceId,
    territoryId,
    attackType,
    damage,
    startedAt: admin.firestore.Timestamp.now(),
    completedAt: admin.firestore.Timestamp.now(),
    result: conquered ? 'success' : 'failed',
    xpEarned: attackDef.xpReward,
    resourcesLooted: conquered ? CONQUEST_BONUS_RESOURCES : undefined
  };

  // Begin transaction
  const batch = db.batch();

  // Update territory
  const territoryUpdate: Partial<Territory> = {
    health: newHealth,
    lastAttackedAt: admin.firestore.Timestamp.now()
  };

  if (conquered) {
    // Transfer ownership
    territoryUpdate.ownerId = attackerId;
    territoryUpdate.ownerName = attacker.displayName;
    territoryUpdate.allianceId = attacker.allianceId || undefined;
    territoryUpdate.health = territory.maxHealth; // Restore health
    territoryUpdate.shieldExpiresAt = admin.firestore.Timestamp.fromDate(
      new Date(Date.now() + SHIELD_DURATION_HOURS * 3600000)
    );
  }

  batch.update(db.collection('territories').doc(territoryId), territoryUpdate);

  // Deduct resources from attacker
  const resourceUpdates: Record<string, admin.firestore.FieldValue> = {};
  for (const [resource, cost] of Object.entries(attackDef.resourceCost)) {
    resourceUpdates[`resources.${resource}`] = admin.firestore.FieldValue.increment(-cost);
  }

  // Award resources if conquered
  if (conquered) {
    for (const [resource, amount] of Object.entries(CONQUEST_BONUS_RESOURCES)) {
      const key = `resources.${resource}`;
      // Add to existing increment if present
      resourceUpdates[key] = admin.firestore.FieldValue.increment(amount);
    }
  }

  // Update attacker stats
  batch.update(db.collection('users').doc(attackerId), {
    ...resourceUpdates,
    'stats.raidsCompleted': admin.firestore.FieldValue.increment(1),
    'stats.raidsWon': admin.firestore.FieldValue.increment(conquered ? 1 : 0),
    'stats.territoriesClaimed': admin.firestore.FieldValue.increment(conquered ? 1 : 0),
    lastActive: admin.firestore.Timestamp.now()
  });

  // Update defender stats
  batch.update(db.collection('users').doc(territory.ownerId), {
    'stats.raidsDefended': admin.firestore.FieldValue.increment(conquered ? 0 : 1),
    'stats.territoriesClaimed': admin.firestore.FieldValue.increment(conquered ? -1 : 0),
    'stats.citadelsLost': admin.firestore.FieldValue.increment(conquered ? 1 : 0)
  });

  // Save attack record
  batch.set(db.collection('attacks').doc(attackId), attack);

  // Update cooldown
  batch.set(
    db.collection('users').doc(attackerId).collection('cooldowns').doc(attackType),
    { lastUsedAt: admin.firestore.Timestamp.now() }
  );

  await batch.commit();

  // Award XP
  await awardXp(attackerId, attackDef.xpReward);
  if (conquered) {
    await awardXp(attackerId, 50); // Conquest bonus
  }

  // Send notifications
  if (conquered) {
    await sendNotification(territory.ownerId, {
      type: 'territory_conquered',
      title: 'Territory Conquered!',
      body: `${attacker.displayName} conquered your territory!`,
      data: { territoryId, attackerId }
    });

    await sendNotification(attackerId, {
      type: 'territory_conquered',
      title: 'Victory!',
      body: `You conquered ${territory.ownerName}'s territory!`,
      data: { territoryId }
    });
  } else {
    await sendNotification(territory.ownerId, {
      type: 'territory_attacked',
      title: 'Under Attack!',
      body: `${attacker.displayName} attacked your territory! (${newHealth}/${territory.maxHealth} HP)`,
      data: { territoryId, attackerId }
    });
  }

  // Check achievements
  await checkAchievements(attackerId, 'combat');
  if (conquered) {
    await checkAchievements(attackerId, 'territory');
  }

  return {
    success: true,
    damage,
    newHealth,
    conquered,
    xpEarned: attackDef.xpReward + (conquered ? 50 : 0),
    resourcesLooted: conquered ? CONQUEST_BONUS_RESOURCES : null,
    cooldownSeconds: attackDef.cooldownSeconds
  };
});

// ============================================================================
// Get Attack Cooldowns
// ============================================================================

export const getAttackCooldowns = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const userId = context.auth.uid;
  const cooldowns: Record<string, number> = {};

  const snapshot = await db
    .collection('users')
    .doc(userId)
    .collection('cooldowns')
    .get();

  const now = Date.now();

  for (const doc of snapshot.docs) {
    const attackType = doc.id as AttackType;
    const lastUsed = doc.data().lastUsedAt?.toDate();
    const attackDef = ATTACK_DEFINITIONS[attackType];

    if (lastUsed && attackDef) {
      const elapsed = (now - lastUsed.getTime()) / 1000;
      const remaining = Math.max(0, attackDef.cooldownSeconds - elapsed);
      cooldowns[attackType] = Math.ceil(remaining);
    } else {
      cooldowns[attackType] = 0;
    }
  }

  // Fill in any missing attack types
  for (const attackType of Object.keys(ATTACK_DEFINITIONS)) {
    if (!(attackType in cooldowns)) {
      cooldowns[attackType] = 0;
    }
  }

  return { cooldowns };
});

// ============================================================================
// Get Battle History
// ============================================================================

export const getBattleHistory = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const { limit = 20, type = 'all' } = data as { limit?: number; type?: 'attacks' | 'defenses' | 'all' };
  const userId = context.auth.uid;

  let query = db.collection('attacks').orderBy('startedAt', 'desc').limit(limit);

  if (type === 'attacks') {
    query = query.where('attackerId', '==', userId);
  } else if (type === 'defenses') {
    query = query.where('defenderId', '==', userId);
  } else {
    // For 'all', we need to do two queries and merge
    const [attacks, defenses] = await Promise.all([
      db.collection('attacks')
        .where('attackerId', '==', userId)
        .orderBy('startedAt', 'desc')
        .limit(limit)
        .get(),
      db.collection('attacks')
        .where('defenderId', '==', userId)
        .orderBy('startedAt', 'desc')
        .limit(limit)
        .get()
    ]);

    const allBattles = [
      ...attacks.docs.map(d => ({ ...d.data() as Attack, id: d.id, role: 'attacker' as const })),
      ...defenses.docs.map(d => ({ ...d.data() as Attack, id: d.id, role: 'defender' as const }))
    ].sort((a, b) => b.startedAt.toMillis() - a.startedAt.toMillis())
      .slice(0, limit);

    return { battles: allBattles };
  }

  const snapshot = await query.get();
  const battles = snapshot.docs.map(doc => ({
    ...doc.data(),
    id: doc.id,
    role: type === 'attacks' ? 'attacker' : 'defender'
  }));

  return { battles };
});

// ============================================================================
// Repair Territory
// ============================================================================

export const repairTerritory = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const { territoryId } = data as { territoryId: string };
  const userId = context.auth.uid;

  const [userDoc, territoryDoc] = await Promise.all([
    db.collection('users').doc(userId).get(),
    db.collection('territories').doc(territoryId).get()
  ]);

  if (!territoryDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Territory not found');
  }

  const territory = territoryDoc.data() as Territory;
  const user = userDoc.data() as User;

  // Must own territory
  if (territory.ownerId !== userId) {
    throw new functions.https.HttpsError('permission-denied', 'Not your territory');
  }

  // Calculate repair cost (10 stone per HP)
  const damage = territory.maxHealth - territory.health;
  if (damage === 0) {
    throw new functions.https.HttpsError('failed-precondition', 'Territory is at full health');
  }

  const stoneCost = damage * 10;
  if (user.resources.stone < stoneCost) {
    throw new functions.https.HttpsError(
      'failed-precondition',
      `Not enough stone. Need ${stoneCost}, have ${user.resources.stone}`
    );
  }

  // Apply repair
  await db.runTransaction(async (transaction) => {
    transaction.update(db.collection('territories').doc(territoryId), {
      health: territory.maxHealth
    });

    transaction.update(db.collection('users').doc(userId), {
      'resources.stone': admin.firestore.FieldValue.increment(-stoneCost)
    });
  });

  return {
    success: true,
    stoneCost,
    newHealth: territory.maxHealth
  };
});

// ============================================================================
// Activate Shield
// ============================================================================

export const activateShield = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const { territoryId, hours } = data as { territoryId: string; hours: number };
  const userId = context.auth.uid;

  // Validate hours (1, 4, 8, or 24)
  const validHours = [1, 4, 8, 24];
  if (!validHours.includes(hours)) {
    throw new functions.https.HttpsError('invalid-argument', 'Invalid shield duration');
  }

  const gemCost = hours * 10; // 10 gems per hour

  const [userDoc, territoryDoc] = await Promise.all([
    db.collection('users').doc(userId).get(),
    db.collection('territories').doc(territoryId).get()
  ]);

  if (!territoryDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Territory not found');
  }

  const territory = territoryDoc.data() as Territory;
  const user = userDoc.data() as User;

  // Must own territory
  if (territory.ownerId !== userId) {
    throw new functions.https.HttpsError('permission-denied', 'Not your territory');
  }

  // Check gems
  if (user.resources.gems < gemCost) {
    throw new functions.https.HttpsError(
      'failed-precondition',
      `Not enough gems. Need ${gemCost}, have ${user.resources.gems}`
    );
  }

  // Calculate new shield expiry
  const currentShield = territory.shieldExpiresAt?.toDate() || new Date();
  const baseTime = currentShield > new Date() ? currentShield : new Date();
  const newExpiry = new Date(baseTime.getTime() + hours * 3600000);

  // Apply shield
  await db.runTransaction(async (transaction) => {
    transaction.update(db.collection('territories').doc(territoryId), {
      shieldExpiresAt: admin.firestore.Timestamp.fromDate(newExpiry)
    });

    transaction.update(db.collection('users').doc(userId), {
      'resources.gems': admin.firestore.FieldValue.increment(-gemCost)
    });
  });

  return {
    success: true,
    gemCost,
    shieldExpiresAt: newExpiry.toISOString()
  };
});
