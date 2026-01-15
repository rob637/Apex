/**
 * Apex Citadels - Battle System Cloud Functions
 * 
 * Turn-based strategic combat with:
 * - 24-hour battle scheduling
 * - 6 troop types with counters
 * - Physical/remote participation
 * - Territory siege state machine
 * - Reclaim mechanics
 */

import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';
import {
  Territory,
  TerritoryState,
  User,
  UserResources,
  ScheduledBattle,
  BattleStatus,
  BattleFormation,
  BattleResult,
  BattleRound,
  BattleEvent,
  Troop,
  TroopType,
  TROOP_DEFINITIONS,
  COUNTER_MULTIPLIER,
  WEAKNESS_MULTIPLIER,
  ParticipationType,
  PARTICIPATION_EFFECTIVENESS,
  BATTLE_CONFIG
} from './types';
import { sendNotification } from './notifications';
import { autoSaveBlueprint } from './blueprint';
import { canAttackTerritory } from './protection';
import { awardXp, checkAchievements } from './progression';

const db = admin.firestore();

// ============================================================================
// BATTLE SCHEDULING
// ============================================================================

/**
 * Schedule a battle against a territory (24-hour advance notice)
 */
export const scheduleBattle = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const { territoryId } = data as { territoryId: string };
  const attackerId = context.auth.uid;

  // Get attacker
  const attackerDoc = await db.collection('users').doc(attackerId).get();
  if (!attackerDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Attacker not found');
  }
  const attacker = attackerDoc.data() as User;

  // Get territory
  const territoryDoc = await db.collection('territories').doc(territoryId).get();
  if (!territoryDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Territory not found');
  }
  const territory = territoryDoc.data() as Territory;

  // Validation checks (returns defense bonus for inactive players)
  const { defenseBonus } = await validateBattleScheduling(attacker, territory, attackerId);

  // Get defender
  const defenderDoc = await db.collection('users').doc(territory.ownerId).get();
  const _defender = defenderDoc.data() as User; // Reserved for future battle log enrichment
  void _defender;

  // Calculate battle time (24 hours from now)
  const now = new Date();
  const battleStartsAt = new Date(now.getTime() + BATTLE_CONFIG.SCHEDULING_WINDOW_HOURS * 3600000);

  // Create scheduled battle
  const battleId = db.collection('scheduled_battles').doc().id;
  const battle: ScheduledBattle = {
    id: battleId,
    attackerId,
    attackerName: attacker.displayName,
    attackerAllianceId: attacker.allianceId,
    defenderId: territory.ownerId,
    defenderName: territory.ownerName,
    defenderAllianceId: territory.allianceId,
    territoryId,
    territoryName: `Territory at ${territory.centerLatitude.toFixed(4)}, ${territory.centerLongitude.toFixed(4)}`,
    scheduledAt: admin.firestore.Timestamp.now(),
    battleStartsAt: admin.firestore.Timestamp.fromDate(battleStartsAt),
    status: 'scheduled',
    defenderActivityBonus: defenseBonus, // Store activity-based defense bonus
    createdAt: admin.firestore.Timestamp.now(),
    updatedAt: admin.firestore.Timestamp.now()
  };

  await db.collection('scheduled_battles').doc(battleId).set(battle);

  // Notify defender
  await sendNotification(territory.ownerId, {
    type: 'territory_attacked',
    title: '⚔️ Battle Scheduled!',
    body: `${attacker.displayName} will attack your territory in 24 hours! Prepare your defenses.`,
    data: { battleId, territoryId, attackerId }
  });

  // Notify attacker's alliance (if any)
  if (attacker.allianceId) {
    await notifyAlliance(attacker.allianceId, attackerId, {
      type: 'alliance_war_started',
      title: 'Alliance Battle Scheduled',
      body: `${attacker.displayName} scheduled an attack. Rally to support!`,
      data: { battleId, territoryId }
    });
  }

  return {
    success: true,
    battleId,
    battleStartsAt: battleStartsAt.toISOString(),
    message: `Battle scheduled for ${battleStartsAt.toLocaleString()}`
  };
});

/**
 * Validate battle scheduling requirements
 * Returns defense bonus that should be applied to defender
 */
async function validateBattleScheduling(
  attacker: User, 
  territory: Territory, 
  attackerId: string
): Promise<{ defenseBonus: number }> {
  // Can't attack own territory
  if (territory.ownerId === attackerId) {
    throw new functions.https.HttpsError('failed-precondition', 'Cannot attack your own territory');
  }

  // Can't attack alliance member
  if (attacker.allianceId && territory.allianceId === attacker.allianceId) {
    throw new functions.https.HttpsError('failed-precondition', 'Cannot attack alliance member');
  }

  // Use comprehensive attack eligibility check
  const attackCheck = await canAttackTerritory(attackerId, territory.id);
  if (!attackCheck.canAttack) {
    throw new functions.https.HttpsError(
      'failed-precondition',
      attackCheck.reason || 'Attack not allowed'
    );
  }

  // Check existing scheduled battle for this territory
  const existingBattle = await db.collection('scheduled_battles')
    .where('territoryId', '==', territory.id)
    .where('status', 'in', ['scheduled', 'preparing', 'active'])
    .limit(1)
    .get();

  if (!existingBattle.empty) {
    throw new functions.https.HttpsError(
      'failed-precondition',
      'A battle is already scheduled for this territory'
    );
  }

  // Return defense bonus to apply during battle
  return { defenseBonus: attackCheck.defenseBonus };
}

// ============================================================================
// BATTLE PREPARATION
// ============================================================================

/**
 * Set battle formation (troops and strategy)
 */
export const setBattleFormation = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const { battleId, troops, strategy } = data as {
    battleId: string;
    troops: Troop[];
    strategy?: 'aggressive' | 'defensive' | 'balanced';
  };

  const userId = context.auth.uid;

  // Get battle
  const battleDoc = await db.collection('scheduled_battles').doc(battleId).get();
  if (!battleDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Battle not found');
  }
  const battle = battleDoc.data() as ScheduledBattle;

  // Verify user is participant
  const isAttacker = battle.attackerId === userId;
  const isDefender = battle.defenderId === userId;
  
  if (!isAttacker && !isDefender) {
    throw new functions.https.HttpsError('permission-denied', 'You are not a participant in this battle');
  }

  // Verify battle is in preparation or scheduled phase
  if (battle.status !== 'scheduled' && battle.status !== 'preparing') {
    throw new functions.https.HttpsError(
      'failed-precondition',
      `Cannot set formation when battle is ${battle.status}`
    );
  }

  // Validate troops are available
  await validateTroopAvailability(userId, troops);

  // Calculate formation power
  const totalPower = calculateFormationPower(troops);

  const formation: BattleFormation = {
    troops,
    totalPower,
    strategy: strategy || 'balanced'
  };

  // Update battle
  const updateField = isAttacker ? 'attackerFormation' : 'defenderFormation';
  const updates: Partial<ScheduledBattle> = {
    [updateField]: formation,
    status: 'preparing',
    updatedAt: admin.firestore.Timestamp.now()
  };

  await db.collection('scheduled_battles').doc(battleId).update(updates);

  return {
    success: true,
    formation,
    message: `Formation set with ${totalPower} total power`
  };
});

/**
 * Validate user has the troops they're trying to deploy
 */
async function validateTroopAvailability(userId: string, troops: Troop[]): Promise<void> {
  const userTroopsDoc = await db.collection('users').doc(userId).collection('troops').doc('current').get();
  
  if (!userTroopsDoc.exists) {
    throw new functions.https.HttpsError('failed-precondition', 'You have no troops');
  }

  const userTroops = userTroopsDoc.data()?.troops as Troop[] || [];
  
  for (const deployTroop of troops) {
    const available = userTroops.find(t => t.type === deployTroop.type);
    if (!available || available.count < deployTroop.count) {
      throw new functions.https.HttpsError(
        'failed-precondition',
        `Insufficient ${deployTroop.type}: need ${deployTroop.count}, have ${available?.count || 0}`
      );
    }
  }
}

/**
 * Calculate total formation power for display/matchmaking
 */
function calculateFormationPower(troops: Troop[]): number {
  let power = 0;
  for (const troop of troops) {
    const def = TROOP_DEFINITIONS[troop.type];
    const troopPower = (def.baseAttack + def.baseDefense + def.baseHealth / 10) * troop.count * troop.level;
    power += troopPower;
  }
  return Math.round(power);
}

// ============================================================================
// BATTLE EXECUTION
// ============================================================================

/**
 * Internal battle execution (called by scheduler and callable function)
 */
async function executeBattleInternal(battleId: string): Promise<BattleResult> {
  // Get battle
  const battleDoc = await db.collection('scheduled_battles').doc(battleId).get();
  if (!battleDoc.exists) {
    throw new Error('Battle not found');
  }
  const battle = battleDoc.data() as ScheduledBattle;

  // Verify battle can be executed
  if (battle.status === 'completed' || battle.status === 'cancelled') {
    throw new Error(`Battle already ${battle.status}`);
  }

  // Set default formations if not set
  const attackerFormation = battle.attackerFormation || await getDefaultFormation(battle.attackerId);
  const defenderFormation = battle.defenderFormation || await getDefaultFormation(battle.defenderId);

  // Get participation types (for effectiveness)
  const attackerParticipation = battle.attackerParticipation || 'remote';
  const defenderParticipation = battle.defenderParticipation || 'remote';

  // Get activity defense bonus (protects inactive players)
  const defenderActivityBonus = battle.defenderActivityBonus || 0;

  // Run battle simulation
  const result = simulateBattle(
    attackerFormation,
    defenderFormation,
    attackerParticipation,
    defenderParticipation,
    battle.territoryId,
    defenderActivityBonus
  );

  // Get territory for state update
  const territoryDoc = await db.collection('territories').doc(battle.territoryId).get();
  const territory = territoryDoc.data() as Territory;

  // Calculate new territory state
  const { newState, territoryFallen } = calculateTerritoryStateChange(territory, result.winner);
  result.previousTerritoryState = territory.state;
  result.newTerritoryState = newState;
  result.territoryFallen = territoryFallen;

  // Begin transaction for all updates
  const batch = db.batch();

  // Update battle record
  batch.update(db.collection('scheduled_battles').doc(battleId), {
    status: 'completed' as BattleStatus,
    result,
    battleEndsAt: admin.firestore.Timestamp.now(),
    updatedAt: admin.firestore.Timestamp.now()
  });

  // Update territory state
  const territoryUpdate: Partial<Territory> = {
    state: newState,
    battleLosses: result.winner === 'defender' ? territory.battleLosses : territory.battleLosses + 1,
    lastStateChangeAt: admin.firestore.Timestamp.now(),
    lastAttackedAt: admin.firestore.Timestamp.now(),
    lastBattleId: battleId,
    shieldExpiresAt: admin.firestore.Timestamp.fromDate(
      new Date(Date.now() + BATTLE_CONFIG.POST_BATTLE_SHIELD_HOURS * 3600000)
    )
  };

  // Handle territory falling
  if (territoryFallen) {
    territoryUpdate.fallenAt = admin.firestore.Timestamp.now();
    territoryUpdate.previousOwnerId = territory.ownerId;
    
    // Auto-save blueprint (outside batch for better error handling)
    const blueprintId = await autoSaveBlueprint(territory.ownerId, territory);
    if (blueprintId) {
      territoryUpdate.blueprintId = blueprintId;
    }
  }

  batch.update(db.collection('territories').doc(battle.territoryId), territoryUpdate);

  // Update troop counts for both players
  await updateTroopCounts(battle.attackerId, attackerFormation.troops, result.attackerLosses, batch);
  await updateTroopCounts(battle.defenderId, defenderFormation.troops, result.defenderLosses, batch);

  // Award XP
  batch.update(db.collection('users').doc(battle.attackerId), {
    'stats.raidsCompleted': admin.firestore.FieldValue.increment(1),
    'stats.raidsWon': admin.firestore.FieldValue.increment(result.winner === 'attacker' ? 1 : 0),
    lastActive: admin.firestore.Timestamp.now()
  });

  batch.update(db.collection('users').doc(battle.defenderId), {
    'stats.raidsDefended': admin.firestore.FieldValue.increment(result.winner === 'defender' ? 1 : 0),
    'stats.raidsLost': admin.firestore.FieldValue.increment(result.winner === 'attacker' ? 1 : 0),
    lastActive: admin.firestore.Timestamp.now()
  });

  await batch.commit();

  // Award XP (separate calls for achievement checking)
  await awardXp(battle.attackerId, result.attackerXp);
  await awardXp(battle.defenderId, result.defenderXp);

  // Send notifications
  await sendBattleResultNotifications(battle, result, territoryFallen);

  // Check achievements
  await checkAchievements(battle.attackerId, 'combat');
  await checkAchievements(battle.defenderId, 'combat');

  if (territoryFallen) {
    await checkAchievements(battle.attackerId, 'territory');
  }

  return result;
}

/**
 * Execute a scheduled battle (called by scheduler or manually after time)
 */
export const executeBattle = functions.https.onCall(async (data, _context) => {
  const { battleId } = data as { battleId: string };

  // Validate battle exists and is ready
  const battleDoc = await db.collection('scheduled_battles').doc(battleId).get();
  if (!battleDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Battle not found');
  }
  
  const battle = battleDoc.data() as ScheduledBattle;
  
  // Additional validation for callable (time check)
  const now = new Date();
  if (battle.battleStartsAt.toDate() > now) {
    throw new functions.https.HttpsError(
      'failed-precondition',
      'Battle has not started yet'
    );
  }

  try {
    const result = await executeBattleInternal(battleId);
    return {
      success: true,
      result,
      message: `Battle complete! ${result.winner === 'attacker' ? 'Attacker' : 'Defender'} wins!`
    };
  } catch (error) {
    throw new functions.https.HttpsError('internal', (error as Error).message);
  }
});

/**
 * Get default formation if player didn't set one
 */
async function getDefaultFormation(userId: string): Promise<BattleFormation> {
  const troopsDoc = await db.collection('users').doc(userId).collection('troops').doc('current').get();
  
  if (!troopsDoc.exists) {
    // No troops = weakest possible formation
    return {
      troops: [{ type: 'infantry', count: 1, level: 1 }],
      totalPower: 10,
      strategy: 'balanced'
    };
  }

  const troops = troopsDoc.data()?.troops as Troop[] || [];
  return {
    troops,
    totalPower: calculateFormationPower(troops),
    strategy: 'balanced'
  };
}

// ============================================================================
// BATTLE SIMULATION
// ============================================================================

/**
 * Simulate turn-based battle between two formations
 */
function simulateBattle(
  attackerFormation: BattleFormation,
  defenderFormation: BattleFormation,
  attackerParticipation: ParticipationType,
  defenderParticipation: ParticipationType,
  _territoryId: string,
  defenderActivityBonus: number = 0
): BattleResult {
  // Deep copy troops for simulation
  let attackerTroops = JSON.parse(JSON.stringify(attackerFormation.troops)) as Troop[];
  let defenderTroops = JSON.parse(JSON.stringify(defenderFormation.troops)) as Troop[];

  // Track original counts
  const originalAttacker = JSON.parse(JSON.stringify(attackerTroops)) as Troop[];
  const originalDefender = JSON.parse(JSON.stringify(defenderTroops)) as Troop[];

  // Apply participation effectiveness
  const attackerEffectiveness = PARTICIPATION_EFFECTIVENESS[attackerParticipation];
  // Defender gets participation bonus PLUS activity bonus (protects inactive players)
  const defenderEffectiveness = Math.min(1.5, PARTICIPATION_EFFECTIVENESS[defenderParticipation] + defenderActivityBonus);

  const rounds: BattleRound[] = [];
  let roundNumber = 0;

  // Battle loop (max 10 rounds)
  while (roundNumber < BATTLE_CONFIG.MAX_ROUNDS) {
    roundNumber++;

    // Check if either side is eliminated
    const attackerAlive = getTotalTroopCount(attackerTroops) > 0;
    const defenderAlive = getTotalTroopCount(defenderTroops) > 0;

    if (!attackerAlive || !defenderAlive) {
      break;
    }

    // Execute round
    const round = executeRound(
      roundNumber,
      attackerTroops,
      defenderTroops,
      attackerEffectiveness,
      defenderEffectiveness,
      attackerFormation.strategy || 'balanced',
      defenderFormation.strategy || 'balanced'
    );

    rounds.push(round);

    // Apply casualties
    attackerTroops = applyCasualties(attackerTroops, round.attackerCasualties);
    defenderTroops = applyCasualties(defenderTroops, round.defenderCasualties);
  }

  // Determine winner
  const attackerSurviving = getTotalTroopCount(attackerTroops);
  const defenderSurviving = getTotalTroopCount(defenderTroops);
  const attackerOriginal = getTotalTroopCount(originalAttacker);
  const defenderOriginal = getTotalTroopCount(originalDefender);

  let winner: 'attacker' | 'defender';
  let isDecisive = false;

  if (defenderSurviving === 0) {
    winner = 'attacker';
    isDecisive = true;
  } else if (attackerSurviving === 0) {
    winner = 'defender';
    isDecisive = true;
  } else if (attackerSurviving / attackerOriginal > defenderSurviving / defenderOriginal) {
    winner = 'attacker';
    // Decisive if defender lost 70%+
    isDecisive = (defenderOriginal - defenderSurviving) / defenderOriginal >= BATTLE_CONFIG.DECISIVE_VICTORY_THRESHOLD;
  } else {
    winner = 'defender';
    isDecisive = (attackerOriginal - attackerSurviving) / attackerOriginal >= BATTLE_CONFIG.DECISIVE_VICTORY_THRESHOLD;
  }

  // Calculate losses
  const attackerLosses = calculateLosses(originalAttacker, attackerTroops);
  const defenderLosses = calculateLosses(originalDefender, defenderTroops);

  // Calculate XP rewards
  const baseXp = 50;
  const winnerBonus = 100;
  const decisiveBonus = 50;

  const attackerXp = baseXp + 
    (winner === 'attacker' ? winnerBonus : 0) + 
    (winner === 'attacker' && isDecisive ? decisiveBonus : 0);
  
  const defenderXp = baseXp + 
    (winner === 'defender' ? winnerBonus : 0) + 
    (winner === 'defender' && isDecisive ? decisiveBonus : 0);

  return {
    winner,
    isDecisive,
    rounds,
    totalRounds: rounds.length,
    attackerLosses,
    defenderLosses,
    attackerSurvivors: attackerTroops,
    defenderSurvivors: defenderTroops,
    previousTerritoryState: 'secure', // Will be set by caller
    newTerritoryState: 'secure',      // Will be set by caller
    territoryFallen: false,            // Will be set by caller
    attackerXp,
    defenderXp
  };
}

/**
 * Execute a single round of combat
 */
function executeRound(
  roundNumber: number,
  attackerTroops: Troop[],
  defenderTroops: Troop[],
  attackerEffectiveness: number,
  defenderEffectiveness: number,
  attackerStrategy: string,
  defenderStrategy: string
): BattleRound {
  const events: BattleEvent[] = [];

  // Calculate damage from attacker to defender
  let attackerDamage = 0;
  const attackerCasualties: Troop[] = [];
  const defenderCasualties: Troop[] = [];

  for (const troop of attackerTroops) {
    if (troop.count <= 0) continue;
    
    const def = TROOP_DEFINITIONS[troop.type];
    let damage = def.baseAttack * troop.count * troop.level * attackerEffectiveness;
    
    // Apply strategy modifier
    if (attackerStrategy === 'aggressive') damage *= 1.2;
    if (attackerStrategy === 'defensive') damage *= 0.8;

    // Check for counters
    for (const enemyTroop of defenderTroops) {
      if (def.strongAgainst.includes(enemyTroop.type)) {
        damage *= COUNTER_MULTIPLIER;
        events.push({
          type: 'counter',
          message: `${def.displayName} counters ${TROOP_DEFINITIONS[enemyTroop.type].displayName}!`,
          impact: COUNTER_MULTIPLIER
        });
        break;
      }
      if (def.weakAgainst.includes(enemyTroop.type)) {
        damage *= WEAKNESS_MULTIPLIER;
        break;
      }
    }

    attackerDamage += damage;
  }

  // Calculate damage from defender to attacker
  let defenderDamage = 0;
  
  for (const troop of defenderTroops) {
    if (troop.count <= 0) continue;
    
    const def = TROOP_DEFINITIONS[troop.type];
    let damage = def.baseAttack * troop.count * troop.level * defenderEffectiveness;
    
    // Defenders get slight bonus
    damage *= 1.1;
    
    // Apply strategy modifier
    if (defenderStrategy === 'aggressive') damage *= 1.2;
    if (defenderStrategy === 'defensive') damage *= 0.8;

    // Check for counters
    for (const enemyTroop of attackerTroops) {
      if (def.strongAgainst.includes(enemyTroop.type)) {
        damage *= COUNTER_MULTIPLIER;
        events.push({
          type: 'counter',
          message: `${def.displayName} counters ${TROOP_DEFINITIONS[enemyTroop.type].displayName}!`,
          impact: COUNTER_MULTIPLIER
        });
        break;
      }
      if (def.weakAgainst.includes(enemyTroop.type)) {
        damage *= WEAKNESS_MULTIPLIER;
        break;
      }
    }

    defenderDamage += damage;
  }

  // Apply damage as casualties (simplified: damage / avg HP = troops lost)
  const avgAttackerHP = getAverageHP(attackerTroops);
  const avgDefenderHP = getAverageHP(defenderTroops);

  // Defender takes attacker's damage
  const defenderTroopsLost = Math.floor(attackerDamage / avgDefenderHP);
  distributesCasualties(defenderTroops, defenderTroopsLost, defenderCasualties);

  // Attacker takes defender's damage
  const attackerTroopsLost = Math.floor(defenderDamage / avgAttackerHP);
  distributesCasualties(attackerTroops, attackerTroopsLost, attackerCasualties);

  return {
    roundNumber,
    attackerAction: { type: 'attack' },
    defenderAction: { type: 'defend' },
    attackerDamageDealt: Math.round(attackerDamage),
    defenderDamageDealt: Math.round(defenderDamage),
    attackerCasualties,
    defenderCasualties,
    events
  };
}

/**
 * Get total troop count
 */
function getTotalTroopCount(troops: Troop[]): number {
  return troops.reduce((sum, t) => sum + t.count, 0);
}

/**
 * Get average HP of a troop formation
 */
function getAverageHP(troops: Troop[]): number {
  if (troops.length === 0) return 100;
  
  let totalHP = 0;
  let totalCount = 0;
  
  for (const troop of troops) {
    const def = TROOP_DEFINITIONS[troop.type];
    totalHP += def.baseHealth * troop.count * troop.level;
    totalCount += troop.count;
  }
  
  return totalCount > 0 ? totalHP / totalCount : 100;
}

/**
 * Distribute casualties across troop types
 */
function distributesCasualties(troops: Troop[], totalLost: number, casualties: Troop[]): void {
  let remaining = totalLost;
  
  // Lose troops proportionally
  for (const troop of troops) {
    if (remaining <= 0) break;
    if (troop.count <= 0) continue;

    const thisLoss = Math.min(troop.count, Math.ceil(remaining * (troop.count / getTotalTroopCount(troops))));
    if (thisLoss > 0) {
      casualties.push({ type: troop.type, count: thisLoss, level: troop.level });
      troop.count -= thisLoss;
      remaining -= thisLoss;
    }
  }
}

/**
 * Apply casualties to troops array
 */
function applyCasualties(troops: Troop[], casualties: Troop[]): Troop[] {
  // Already applied in distributesCasualties, just filter out zeros
  return troops.filter(t => t.count > 0);
}

/**
 * Calculate losses between original and surviving troops
 */
function calculateLosses(original: Troop[], surviving: Troop[]): Troop[] {
  const losses: Troop[] = [];
  
  for (const orig of original) {
    const surv = surviving.find(t => t.type === orig.type);
    const survCount = surv?.count || 0;
    const lost = orig.count - survCount;
    
    if (lost > 0) {
      losses.push({ type: orig.type, count: lost, level: orig.level });
    }
  }
  
  return losses;
}

// ============================================================================
// TERRITORY STATE MACHINE
// ============================================================================

/**
 * Calculate new territory state based on battle result
 */
function calculateTerritoryStateChange(
  territory: Territory,
  winner: 'attacker' | 'defender'
): { newState: TerritoryState; territoryFallen: boolean } {
  // If defender wins, state improves (or stays secure)
  if (winner === 'defender') {
    // Successful defense can recover state
    const stateProgression: Record<TerritoryState, TerritoryState> = {
      'secure': 'secure',
      'contested': 'secure',      // Recovery from contested
      'vulnerable': 'contested',  // Partial recovery
      'fallen': 'fallen'          // Can't recover fallen through defense
    };
    
    return {
      newState: stateProgression[territory.state],
      territoryFallen: false
    };
  }

  // Attacker wins - territory degrades
  const currentLosses = territory.battleLosses || 0;
  const newLosses = currentLosses + 1;

  if (newLosses >= BATTLE_CONFIG.LOSSES_TO_FALL) {
    return {
      newState: 'fallen',
      territoryFallen: true
    };
  }

  // Degrade state
  const stateByLosses: Record<number, TerritoryState> = {
    1: 'contested',
    2: 'vulnerable'
  };

  return {
    newState: stateByLosses[newLosses] || 'contested',
    territoryFallen: false
  };
}

/**
 * Calculate total build cost from buildings (simplified)
 */
function calculateBuildCost(_buildings: Territory['buildings']): Partial<UserResources> {
  // Simplified - would normally calculate from building definitions
  return {
    stone: 100 * _buildings.length,
    wood: 50 * _buildings.length,
    iron: 25 * _buildings.length
  };
}

/**
 * Update player's troop counts after battle
 */
async function updateTroopCounts(
  userId: string,
  deployed: Troop[],
  losses: Troop[],
  batch: admin.firestore.WriteBatch
): Promise<void> {
  const troopsRef = db.collection('users').doc(userId).collection('troops').doc('current');
  const troopsDoc = await troopsRef.get();
  
  if (!troopsDoc.exists) return;

  const currentTroops = troopsDoc.data()?.troops as Troop[] || [];

  // Calculate surviving troops
  for (const loss of losses) {
    const current = currentTroops.find(t => t.type === loss.type);
    if (current) {
      current.count = Math.max(0, current.count - loss.count);
    }
  }

  batch.update(troopsRef, {
    troops: currentTroops.filter(t => t.count > 0),
    lastUpdated: admin.firestore.Timestamp.now()
  });
}

// ============================================================================
// NOTIFICATIONS
// ============================================================================

/**
 * Send battle result notifications to both parties
 */
async function sendBattleResultNotifications(
  battle: ScheduledBattle,
  result: BattleResult,
  territoryFallen: boolean
): Promise<void> {
  // Notify attacker
  await sendNotification(battle.attackerId, {
    type: result.winner === 'attacker' ? 'territory_conquered' : 'territory_defended',
    title: result.winner === 'attacker' ? '⚔️ Victory!' : '😞 Defeat',
    body: result.winner === 'attacker'
      ? `You won the battle! ${territoryFallen ? 'Territory has fallen!' : `Territory weakened (${result.newTerritoryState})`}`
      : `${battle.defenderName} successfully defended their territory.`,
    data: { battleId: battle.id, territoryId: battle.territoryId }
  });

  // Notify defender
  await sendNotification(battle.defenderId, {
    type: result.winner === 'defender' ? 'territory_defended' : 'territory_conquered',
    title: result.winner === 'defender' ? '🛡️ Defense Successful!' : '⚠️ Territory Under Siege',
    body: result.winner === 'defender'
      ? `You defended against ${battle.attackerName}!`
      : territoryFallen
        ? `Your territory has fallen to ${battle.attackerName}! You have 24 hours to reclaim it.`
        : `${battle.attackerName} damaged your territory. State: ${result.newTerritoryState}`,
    data: { battleId: battle.id, territoryId: battle.territoryId }
  });
}

/**
 * Notify alliance members
 */
async function notifyAlliance(
  allianceId: string,
  excludeUserId: string,
  notification: { type: string; title: string; body: string; data: Record<string, string> }
): Promise<void> {
  const membersSnapshot = await db
    .collection('alliances')
    .doc(allianceId)
    .collection('members')
    .get();

  for (const member of membersSnapshot.docs) {
    if (member.id !== excludeUserId) {
      await sendNotification(member.id, notification as any);
    }
  }
}

// ============================================================================
// RECLAIM SYSTEM
// ============================================================================

/**
 * Reclaim a fallen territory
 */
export const reclaimTerritory = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const { territoryId } = data as { territoryId: string };
  const userId = context.auth.uid;

  // Get territory
  const territoryDoc = await db.collection('territories').doc(territoryId).get();
  if (!territoryDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Territory not found');
  }
  const territory = territoryDoc.data() as Territory;

  // Verify territory is fallen
  if (territory.state !== 'fallen') {
    throw new functions.https.HttpsError('failed-precondition', 'Territory is not fallen');
  }

  // Verify user was previous owner
  if (territory.previousOwnerId !== userId) {
    throw new functions.https.HttpsError(
      'permission-denied',
      'Only the previous owner can reclaim this territory'
    );
  }

  // Check cooldown
  if (territory.fallenAt) {
    const hoursSinceFall = (Date.now() - territory.fallenAt.toDate().getTime()) / 3600000;
    if (hoursSinceFall < BATTLE_CONFIG.RECLAIM_COOLDOWN_HOURS) {
      const remaining = Math.ceil(BATTLE_CONFIG.RECLAIM_COOLDOWN_HOURS - hoursSinceFall);
      throw new functions.https.HttpsError(
        'failed-precondition',
        `Must wait ${remaining} more hours before reclaiming`
      );
    }
  }

  // Get user for resources
  const userDoc = await db.collection('users').doc(userId).get();
  const user = userDoc.data() as User;

  // Calculate reclaim cost
  const buildCost = calculateBuildCost(territory.buildings);
  const reclaimCost: Partial<UserResources> = {};
  
  for (const [resource, amount] of Object.entries(buildCost)) {
    reclaimCost[resource as keyof UserResources] = Math.ceil(amount * BATTLE_CONFIG.RECLAIM_COST_PERCENT);
  }

  // Check user has resources
  for (const [resource, cost] of Object.entries(reclaimCost)) {
    const available = user.resources[resource as keyof UserResources] || 0;
    if (available < cost) {
      throw new functions.https.HttpsError(
        'failed-precondition',
        `Not enough ${resource}: need ${cost}, have ${available}`
      );
    }
  }

  // Execute reclaim
  const batch = db.batch();

  // Update territory
  batch.update(db.collection('territories').doc(territoryId), {
    ownerId: userId,
    ownerName: user.displayName,
    allianceId: user.allianceId || null,
    state: 'secure',
    battleLosses: 0,
    previousOwnerId: null,
    fallenAt: null,
    lastStateChangeAt: admin.firestore.Timestamp.now(),
    shieldExpiresAt: admin.firestore.Timestamp.fromDate(
      new Date(Date.now() + BATTLE_CONFIG.POST_BATTLE_SHIELD_HOURS * 3600000)
    )
  });

  // Deduct resources
  const resourceUpdates: Record<string, admin.firestore.FieldValue> = {};
  for (const [resource, cost] of Object.entries(reclaimCost)) {
    resourceUpdates[`resources.${resource}`] = admin.firestore.FieldValue.increment(-cost);
  }
  batch.update(db.collection('users').doc(userId), resourceUpdates);

  await batch.commit();

  return {
    success: true,
    message: 'Territory reclaimed!',
    costPaid: reclaimCost
  };
});

// ============================================================================
// PARTICIPATION VERIFICATION
// ============================================================================

/**
 * Report participation location for a battle
 */
export const reportBattleParticipation = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const { battleId, latitude, longitude } = data as {
    battleId: string;
    latitude: number;
    longitude: number;
  };

  const userId = context.auth.uid;

  // Get battle
  const battleDoc = await db.collection('scheduled_battles').doc(battleId).get();
  if (!battleDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Battle not found');
  }
  const battle = battleDoc.data() as ScheduledBattle;

  // Verify user is participant
  const isAttacker = battle.attackerId === userId;
  const isDefender = battle.defenderId === userId;
  
  if (!isAttacker && !isDefender) {
    throw new functions.https.HttpsError('permission-denied', 'Not a battle participant');
  }

  // Get territory location
  const territoryDoc = await db.collection('territories').doc(battle.territoryId).get();
  const territory = territoryDoc.data() as Territory;

  // Calculate distance to territory
  const distance = calculateDistance(
    latitude,
    longitude,
    territory.centerLatitude,
    territory.centerLongitude
  );

  // Determine participation type
  let participationType: ParticipationType;
  if (distance <= BATTLE_CONFIG.PHYSICAL_PRESENCE_RADIUS_M) {
    participationType = 'physical';
  } else if (distance <= BATTLE_CONFIG.NEARBY_RADIUS_M) {
    participationType = 'nearby';
  } else {
    participationType = 'remote';
  }

  // Update battle
  const updateField = isAttacker ? 'attackerParticipation' : 'defenderParticipation';
  await db.collection('scheduled_battles').doc(battleId).update({
    [updateField]: participationType,
    updatedAt: admin.firestore.Timestamp.now()
  });

  return {
    success: true,
    participationType,
    effectiveness: PARTICIPATION_EFFECTIVENESS[participationType],
    distanceMeters: Math.round(distance)
  };
});

/**
 * Calculate distance between two coordinates in meters
 */
function calculateDistance(lat1: number, lon1: number, lat2: number, lon2: number): number {
  const R = 6371000; // Earth radius in meters
  const dLat = (lat2 - lat1) * Math.PI / 180;
  const dLon = (lon2 - lon1) * Math.PI / 180;
  const a = Math.sin(dLat/2) * Math.sin(dLat/2) +
            Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
            Math.sin(dLon/2) * Math.sin(dLon/2);
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
  return R * c;
}

// ============================================================================
// TROOP TRAINING
// ============================================================================

/**
 * Train new troops
 */
export const trainTroops = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const { troopType, count } = data as { troopType: TroopType; count: number };
  const userId = context.auth.uid;

  // Validate troop type
  const troopDef = TROOP_DEFINITIONS[troopType];
  if (!troopDef) {
    throw new functions.https.HttpsError('invalid-argument', 'Invalid troop type');
  }

  // Get user
  const userDoc = await db.collection('users').doc(userId).get();
  const user = userDoc.data() as User;

  // Calculate total cost
  const totalCost: Partial<UserResources> = {};
  for (const [resource, costPer] of Object.entries(troopDef.trainingCost)) {
    totalCost[resource as keyof UserResources] = costPer * count;
  }

  // Check resources
  for (const [resource, cost] of Object.entries(totalCost)) {
    const available = user.resources[resource as keyof UserResources] || 0;
    if (available < cost) {
      throw new functions.https.HttpsError(
        'failed-precondition',
        `Not enough ${resource}: need ${cost}, have ${available}`
      );
    }
  }

  // Calculate training time
  const trainingTimeMs = troopDef.trainingTimeSeconds * count * 1000;
  const completesAt = new Date(Date.now() + trainingTimeMs);

  // Update in transaction
  await db.runTransaction(async (transaction) => {
    // Deduct resources
    const resourceUpdates: Record<string, admin.firestore.FieldValue> = {};
    for (const [resource, cost] of Object.entries(totalCost)) {
      resourceUpdates[`resources.${resource}`] = admin.firestore.FieldValue.increment(-cost);
    }
    transaction.update(db.collection('users').doc(userId), resourceUpdates);

    // Add to training queue
    const troopsRef = db.collection('users').doc(userId).collection('troops').doc('current');
    const troopsDoc = await transaction.get(troopsRef);
    
    const currentQueue = troopsDoc.data()?.trainingQueue || [];
    currentQueue.push({
      troopType,
      count,
      startedAt: admin.firestore.Timestamp.now(),
      completesAt: admin.firestore.Timestamp.fromDate(completesAt)
    });

    transaction.set(troopsRef, {
      troops: troopsDoc.data()?.troops || [],
      trainingQueue: currentQueue,
      lastUpdated: admin.firestore.Timestamp.now()
    }, { merge: true });
  });

  return {
    success: true,
    troopType,
    count,
    completesAt: completesAt.toISOString(),
    trainingTimeSeconds: troopDef.trainingTimeSeconds * count,
    costPaid: totalCost
  };
});

/**
 * Collect trained troops from queue
 */
export const collectTrainedTroops = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const userId = context.auth.uid;
  const troopsRef = db.collection('users').doc(userId).collection('troops').doc('current');
  
  const troopsDoc = await troopsRef.get();
  if (!troopsDoc.exists) {
    return { success: true, collected: [] };
  }

  const currentTroops = troopsDoc.data()?.troops as Troop[] || [];
  const queue = troopsDoc.data()?.trainingQueue || [];
  const now = Date.now();

  // Find completed training
  const completed: Troop[] = [];
  const stillTraining: typeof queue = [];

  for (const item of queue) {
    if (item.completesAt.toDate().getTime() <= now) {
      completed.push({
        type: item.troopType,
        count: item.count,
        level: 1
      });
    } else {
      stillTraining.push(item);
    }
  }

  if (completed.length === 0) {
    return { success: true, collected: [], message: 'No troops ready' };
  }

  // Merge completed troops with existing
  for (const newTroops of completed) {
    const existing = currentTroops.find(t => t.type === newTroops.type && t.level === newTroops.level);
    if (existing) {
      existing.count += newTroops.count;
    } else {
      currentTroops.push(newTroops);
    }
  }

  // Update
  await troopsRef.update({
    troops: currentTroops,
    trainingQueue: stillTraining,
    lastUpdated: admin.firestore.Timestamp.now()
  });

  return {
    success: true,
    collected: completed,
    message: `Collected ${completed.reduce((s, t) => s + t.count, 0)} troops!`
  };
});

// ============================================================================
// SCHEDULED BATTLE EXECUTOR
// ============================================================================

/**
 * Scheduled function to execute battles that have reached their start time
 * Runs every 5 minutes to check for pending battles
 */
export const processPendingBattles = functions.pubsub
  .schedule('every 5 minutes')
  .onRun(async () => {
    const now = admin.firestore.Timestamp.now();
    
    // Find battles ready to execute
    const pendingBattles = await db.collection('scheduled_battles')
      .where('status', 'in', ['scheduled', 'preparing'])
      .where('battleStartsAt', '<=', now)
      .limit(10)
      .get();

    console.log(`Found ${pendingBattles.size} pending battles to execute`);

    for (const battleDoc of pendingBattles.docs) {
      try {
        // Execute battle directly (internal call)
        await executeBattleInternal(battleDoc.id);
        console.log(`Executed battle ${battleDoc.id}`);
      } catch (error) {
        console.error(`Failed to execute battle ${battleDoc.id}:`, error);
        
        // Mark as failed
        await battleDoc.ref.update({
          status: 'cancelled',
          error: (error as Error).message,
          updatedAt: admin.firestore.Timestamp.now()
        });
      }
    }

    return null;
  });

