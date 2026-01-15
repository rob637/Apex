/**
 * Protection System
 * 
 * Handles newcomer shields, activity-based defense bonuses, and attack eligibility.
 * 
 * Protection Types:
 * 1. Newcomer Shield - 7 days of immunity for new accounts
 * 2. Activity Bonus - 0-50% defense bonus based on inactivity (protects casual players)
 * 3. Post-Battle Shield - Temporary immunity after being attacked
 * 
 * Design Philosophy:
 * - New players need time to learn without being griefed
 * - Casual players shouldn't lose everything for taking a break
 * - But abandoned territories should be claimable
 */

import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';
import {
  User,
  Territory,
  ProtectionStatus,
  ActivityLevel,
  ACTIVITY_DEFENSE_BONUSES,
  BATTLE_CONFIG
} from './types';

const db = admin.firestore();

// ============================================================================
// PROTECTION STATUS CALCULATION
// ============================================================================

/**
 * Calculate complete protection status for a user
 * Called when checking if a user can be attacked
 */
export async function getProtectionStatus(userId: string): Promise<ProtectionStatus> {
  const userDoc = await db.collection('users').doc(userId).get();
  
  if (!userDoc.exists) {
    // User doesn't exist - can be "attacked" (territory can be claimed)
    return {
      hasNewcomerShield: false,
      activityDefenseBonus: 0,
      canBeAttacked: true,
      reason: 'User not found'
    };
  }

  const user = userDoc.data() as User;
  const now = new Date();

  // Check newcomer shield
  const newcomerShield = checkNewcomerShield(user, now);
  if (newcomerShield.hasShield) {
    return {
      hasNewcomerShield: true,
      shieldExpiresAt: newcomerShield.expiresAt,
      activityDefenseBonus: 0,
      canBeAttacked: false,
      reason: `New player protection until ${newcomerShield.expiresAt?.toDate().toLocaleDateString()}`
    };
  }

  // Calculate activity-based defense bonus
  const activityBonus = calculateActivityBonus(user, now);
  
  // Check if player is abandoned (7+ days inactive - no protection)
  if (activityBonus.level === 'abandoned') {
    return {
      hasNewcomerShield: false,
      activityDefenseBonus: 0,
      canBeAttacked: true,
      reason: 'Player abandoned (7+ days inactive)'
    };
  }

  return {
    hasNewcomerShield: false,
    activityDefenseBonus: activityBonus.bonus,
    canBeAttacked: true
  };
}

/**
 * Check if user still has newcomer shield
 */
function checkNewcomerShield(user: User, now: Date): { hasShield: boolean; expiresAt?: admin.firestore.Timestamp } {
  // Check explicit shield expiry field
  if (user.newcomerShieldExpires) {
    const expiresAt = user.newcomerShieldExpires.toDate();
    if (expiresAt > now) {
      return { hasShield: true, expiresAt: user.newcomerShieldExpires };
    }
  }

  // Fallback: Calculate from account creation date
  const createdAt = user.createdAt.toDate();
  const shieldDays = BATTLE_CONFIG.NEWCOMER_SHIELD_DAYS;
  const shieldExpires = new Date(createdAt.getTime() + shieldDays * 24 * 60 * 60 * 1000);
  
  if (shieldExpires > now) {
    return { 
      hasShield: true, 
      expiresAt: admin.firestore.Timestamp.fromDate(shieldExpires) 
    };
  }

  return { hasShield: false };
}

/**
 * Calculate activity level and defense bonus
 */
function calculateActivityBonus(user: User, now: Date): { level: ActivityLevel; bonus: number } {
  const lastActive = user.lastActive.toDate();
  const daysSinceActive = (now.getTime() - lastActive.getTime()) / (24 * 60 * 60 * 1000);

  // Determine activity level
  if (daysSinceActive < ACTIVITY_DEFENSE_BONUSES.active.maxDaysInactive) {
    return { level: 'active', bonus: ACTIVITY_DEFENSE_BONUSES.active.defenseBonus };
  }
  if (daysSinceActive < ACTIVITY_DEFENSE_BONUSES.away.maxDaysInactive) {
    return { level: 'away', bonus: ACTIVITY_DEFENSE_BONUSES.away.defenseBonus };
  }
  if (daysSinceActive < ACTIVITY_DEFENSE_BONUSES.inactive.maxDaysInactive) {
    return { level: 'inactive', bonus: ACTIVITY_DEFENSE_BONUSES.inactive.defenseBonus };
  }
  
  return { level: 'abandoned', bonus: ACTIVITY_DEFENSE_BONUSES.abandoned.defenseBonus };
}

// ============================================================================
// TERRITORY SHIELD CHECKS
// ============================================================================

/**
 * Check if a territory has an active shield (post-battle immunity)
 */
export function checkTerritoryShield(territory: Territory): { hasShield: boolean; expiresAt?: Date } {
  if (!territory.shieldExpiresAt) {
    return { hasShield: false };
  }

  const now = new Date();
  const shieldExpires = territory.shieldExpiresAt.toDate();
  
  if (shieldExpires > now) {
    return { hasShield: true, expiresAt: shieldExpires };
  }

  return { hasShield: false };
}

/**
 * Apply post-battle shield to a territory
 */
export async function applyPostBattleShield(territoryId: string): Promise<void> {
  const shieldHours = BATTLE_CONFIG.POST_BATTLE_SHIELD_HOURS;
  const shieldExpires = new Date(Date.now() + shieldHours * 60 * 60 * 1000);

  await db.collection('territories').doc(territoryId).update({
    shieldExpiresAt: admin.firestore.Timestamp.fromDate(shieldExpires)
  });
}

// ============================================================================
// ATTACK ELIGIBILITY
// ============================================================================

/**
 * Comprehensive check if a territory can be attacked
 * Returns detailed reason if attack is blocked
 */
export async function canAttackTerritory(
  attackerId: string,
  territoryId: string
): Promise<{ canAttack: boolean; reason?: string; defenseBonus: number }> {
  // Get territory
  const territoryDoc = await db.collection('territories').doc(territoryId).get();
  if (!territoryDoc.exists) {
    return { canAttack: false, reason: 'Territory not found', defenseBonus: 0 };
  }
  const territory = territoryDoc.data() as Territory;

  // Can't attack your own territory
  if (territory.ownerId === attackerId) {
    return { canAttack: false, reason: 'Cannot attack your own territory', defenseBonus: 0 };
  }

  // Check territory shield
  const territoryShield = checkTerritoryShield(territory);
  if (territoryShield.hasShield) {
    return { 
      canAttack: false, 
      reason: `Territory has shield until ${territoryShield.expiresAt?.toLocaleString()}`,
      defenseBonus: 0 
    };
  }

  // Check if territory is already fallen
  if (territory.state === 'fallen') {
    return { canAttack: false, reason: 'Territory has already fallen', defenseBonus: 0 };
  }

  // Check defender's protection status
  const defenderProtection = await getProtectionStatus(territory.ownerId);
  
  if (!defenderProtection.canBeAttacked) {
    return { 
      canAttack: false, 
      reason: defenderProtection.reason || 'Defender is protected',
      defenseBonus: 0 
    };
  }

  // Check same-attacker cooldown
  const cooldownCheck = await checkAttackerCooldown(attackerId, territoryId);
  if (!cooldownCheck.canAttack) {
    return { 
      canAttack: false, 
      reason: cooldownCheck.reason,
      defenseBonus: 0 
    };
  }

  // Attack allowed - return defense bonus
  return { 
    canAttack: true, 
    defenseBonus: defenderProtection.activityDefenseBonus 
  };
}

/**
 * Check if attacker is on cooldown for this specific territory
 */
async function checkAttackerCooldown(
  attackerId: string, 
  territoryId: string
): Promise<{ canAttack: boolean; reason?: string }> {
  const cooldownHours = BATTLE_CONFIG.SAME_ATTACKER_COOLDOWN_HOURS;
  const cooldownThreshold = new Date(Date.now() - cooldownHours * 60 * 60 * 1000);

  // Check recent battles from this attacker on this territory
  const recentBattles = await db.collection('scheduled_battles')
    .where('attackerId', '==', attackerId)
    .where('territoryId', '==', territoryId)
    .where('status', '==', 'completed')
    .where('battleEndsAt', '>', admin.firestore.Timestamp.fromDate(cooldownThreshold))
    .limit(1)
    .get();

  if (!recentBattles.empty) {
    const lastBattle = recentBattles.docs[0].data();
    const lastBattleTime = lastBattle.battleEndsAt.toDate();
    const nextAttackTime = new Date(lastBattleTime.getTime() + cooldownHours * 60 * 60 * 1000);
    
    return { 
      canAttack: false, 
      reason: `You attacked this territory recently. Next attack available: ${nextAttackTime.toLocaleString()}`
    };
  }

  return { canAttack: true };
}

// ============================================================================
// CALLABLE FUNCTIONS
// ============================================================================

/**
 * Get protection status for current user
 */
export const getMyProtectionStatus = functions.https.onCall(async (_data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  const protection = await getProtectionStatus(context.auth.uid);
  
  // Add additional user info
  const userDoc = await db.collection('users').doc(context.auth.uid).get();
  const user = userDoc.data() as User;
  
  const now = new Date();
  const lastActive = user.lastActive.toDate();
  const daysSinceActive = Math.floor((now.getTime() - lastActive.getTime()) / (24 * 60 * 60 * 1000));

  return {
    success: true,
    protection,
    activityInfo: {
      lastActive: user.lastActive,
      daysSinceActive,
      activityLevel: getActivityLevel(daysSinceActive)
    }
  };
});

/**
 * Check if a specific territory can be attacked
 */
export const checkTerritoryAttackable = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  const { territoryId } = data as { territoryId: string };
  
  if (!territoryId) {
    throw new functions.https.HttpsError('invalid-argument', 'Territory ID required');
  }

  const result = await canAttackTerritory(context.auth.uid, territoryId);
  
  return {
    success: true,
    ...result
  };
});

/**
 * Waive newcomer shield early (for players who want to participate in combat sooner)
 */
export const waiveNewcomerShield = functions.https.onCall(async (_data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  const userRef = db.collection('users').doc(context.auth.uid);
  const userDoc = await userRef.get();
  
  if (!userDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'User not found');
  }

  const user = userDoc.data() as User;
  const now = new Date();
  
  // Check if shield is still active
  const shieldCheck = checkNewcomerShield(user, now);
  if (!shieldCheck.hasShield) {
    return {
      success: false,
      message: 'Newcomer shield has already expired'
    };
  }

  // Waive shield by setting expiry to now
  await userRef.update({
    newcomerShieldExpires: admin.firestore.Timestamp.now(),
    'stats.newcomerShieldWaived': true
  });

  return {
    success: true,
    message: 'Newcomer shield waived. You can now participate in battles but can also be attacked.'
  };
});

/**
 * Admin function to grant temporary shield to a user (for compensation, etc.)
 */
export const grantTemporaryShield = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  // Verify admin
  const adminDoc = await db.collection('admins').doc(context.auth.uid).get();
  if (!adminDoc.exists) {
    throw new functions.https.HttpsError('permission-denied', 'Admin access required');
  }

  const { userId, durationHours, reason } = data as { 
    userId: string; 
    durationHours: number; 
    reason: string;
  };

  if (!userId || !durationHours || !reason) {
    throw new functions.https.HttpsError('invalid-argument', 'userId, durationHours, and reason required');
  }

  const shieldExpires = new Date(Date.now() + durationHours * 60 * 60 * 1000);

  await db.collection('users').doc(userId).update({
    newcomerShieldExpires: admin.firestore.Timestamp.fromDate(shieldExpires)
  });

  // Log admin action
  await db.collection('admin_logs').add({
    action: 'grant_shield',
    adminId: context.auth.uid,
    targetUserId: userId,
    durationHours,
    reason,
    shieldExpires: admin.firestore.Timestamp.fromDate(shieldExpires),
    createdAt: admin.firestore.Timestamp.now()
  });

  return {
    success: true,
    message: `Shield granted until ${shieldExpires.toLocaleString()}`
  };
});

// ============================================================================
// HELPER FUNCTIONS
// ============================================================================

function getActivityLevel(daysSinceActive: number): ActivityLevel {
  if (daysSinceActive < 1) return 'active';
  if (daysSinceActive < 3) return 'away';
  if (daysSinceActive < 7) return 'inactive';
  return 'abandoned';
}

/**
 * Update user's last active timestamp
 * Called on any meaningful user action
 */
export async function updateUserActivity(userId: string): Promise<void> {
  await db.collection('users').doc(userId).update({
    lastActive: admin.firestore.Timestamp.now()
  });
}

/**
 * Initialize newcomer shield for a new user
 * Called during user registration
 */
export async function initializeNewcomerShield(userId: string): Promise<void> {
  const shieldDays = BATTLE_CONFIG.NEWCOMER_SHIELD_DAYS;
  const shieldExpires = new Date(Date.now() + shieldDays * 24 * 60 * 60 * 1000);

  await db.collection('users').doc(userId).update({
    newcomerShieldExpires: admin.firestore.Timestamp.fromDate(shieldExpires)
  });
}
