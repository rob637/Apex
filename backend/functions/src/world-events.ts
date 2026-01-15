/**
 * Apex Citadels - World Events System
 * 
 * Creates urgency and FOMO through time-limited global events:
 * - World Bosses (all players attack together)
 * - Territory Rush (bonus XP for claiming)
 * - Resource Surge (increased harvest rates)
 * - Alliance Wars Weekend
 * - Conquest Frenzy (reduced cooldowns)
 * - Mystery Events (random bonuses)
 */

import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';
import { sendNotification } from './notifications';

const db = admin.firestore();

// ============================================================================
// Types
// ============================================================================

export type WorldEventType = 
  | 'world_boss'
  | 'territory_rush'
  | 'resource_surge'
  | 'alliance_war_weekend'
  | 'conquest_frenzy'
  | 'double_xp'
  | 'mystery_box_rain'
  | 'defend_the_realm'
  | 'treasure_hunt'
  | 'faction_war';

export interface WorldEvent {
  id: string;
  type: WorldEventType;
  name: string;
  description: string;
  shortDescription: string;
  icon: string;
  startTime: admin.firestore.Timestamp;
  endTime: admin.firestore.Timestamp;
  status: 'scheduled' | 'active' | 'completed';
  
  // Event-specific data
  config: WorldEventConfig;
  
  // Progress tracking
  globalProgress?: number;
  globalTarget?: number;
  
  // Rewards
  participantRewards: EventRewards;
  completionRewards?: EventRewards; // If global target met
  topContributorRewards?: EventRewards;
  
  // Stats
  participantCount: number;
  totalContributions: number;
  
  // Regional targeting (null = global)
  regions?: string[];
  
  createdAt: admin.firestore.Timestamp;
}

export interface WorldEventConfig {
  // Multipliers
  xpMultiplier?: number;
  resourceMultiplier?: number;
  cooldownReduction?: number;
  
  // World Boss specific
  bossHealth?: number;
  bossCurrentHealth?: number;
  bossDamagePerAttack?: number;
  
  // Treasure Hunt specific
  treasureLocations?: TreasureLocation[];
  treasuresFound?: number;
  
  // Territory Rush specific
  bonusTerritoriesRequired?: number;
  
  // Custom data
  [key: string]: unknown;
}

export interface TreasureLocation {
  id: string;
  latitude: number;
  longitude: number;
  geoHash: string;
  foundBy?: string;
  foundAt?: admin.firestore.Timestamp;
  rewards: EventRewards;
}

export interface EventRewards {
  xp?: number;
  gems?: number;
  stone?: number;
  wood?: number;
  iron?: number;
  crystal?: number;
  arcaneEssence?: number;
  exclusiveTitle?: string;
  exclusiveBadge?: string;
  exclusiveBlock?: string; // Limited edition building block
}

export interface EventParticipation {
  userId: string ;
  eventId: string;
  joinedAt: admin.firestore.Timestamp;
  contributions: number;
  lastContributionAt?: admin.firestore.Timestamp;
  rewardsClaimed: boolean;
  claimedAt?: admin.firestore.Timestamp;
}

// ============================================================================
// Event Templates
// ============================================================================

const EVENT_TEMPLATES: Record<WorldEventType, Partial<WorldEvent>> = {
  world_boss: {
    name: 'World Boss: The Titan',
    description: 'A massive titan threatens all territories! Unite with players worldwide to defeat it before time runs out.',
    shortDescription: 'Defeat the Titan together!',
    icon: '👹',
    config: {
      bossHealth: 10000000, // 10 million HP
      bossDamagePerAttack: 100
    },
    participantRewards: { xp: 500, gems: 50 },
    completionRewards: { xp: 2000, gems: 200, exclusiveTitle: 'Titan Slayer' }
  },
  territory_rush: {
    name: 'Territory Rush',
    description: 'Claim territories faster! Reduced claim cooldowns and bonus XP for every territory claimed.',
    shortDescription: '2x Territory XP!',
    icon: '🏃',
    config: {
      xpMultiplier: 2.0,
      cooldownReduction: 0.5
    },
    participantRewards: { xp: 200 }
  },
  resource_surge: {
    name: 'Resource Surge',
    description: 'Resources are overflowing! All resource nodes yield 3x materials for a limited time.',
    shortDescription: '3x Resource Yield!',
    icon: '💎',
    config: {
      resourceMultiplier: 3.0
    },
    participantRewards: { stone: 500, wood: 500, iron: 200 }
  },
  alliance_war_weekend: {
    name: 'Alliance War Weekend',
    description: 'All alliance wars grant double rewards! Rally your alliance and dominate!',
    shortDescription: '2x War Rewards!',
    icon: '⚔️',
    config: {
      xpMultiplier: 2.0
    },
    participantRewards: { xp: 300, gems: 30 }
  },
  conquest_frenzy: {
    name: 'Conquest Frenzy',
    description: 'Attack cooldowns reduced by 75%! Launch rapid assaults on enemy territories.',
    shortDescription: '75% Faster Attacks!',
    icon: '🔥',
    config: {
      cooldownReduction: 0.75
    },
    participantRewards: { xp: 250 }
  },
  double_xp: {
    name: 'Double XP Weekend',
    description: 'All activities grant double XP! Level up faster than ever.',
    shortDescription: '2x XP Everything!',
    icon: '⭐',
    config: {
      xpMultiplier: 2.0
    },
    participantRewards: { xp: 500 }
  },
  mystery_box_rain: {
    name: 'Mystery Box Rain',
    description: 'Mystery boxes are spawning everywhere! Find them on the map for random rewards.',
    shortDescription: 'Find Mystery Boxes!',
    icon: '🎁',
    config: {
      treasureLocations: [] // Dynamically generated
    },
    participantRewards: { gems: 25 }
  },
  defend_the_realm: {
    name: 'Defend the Realm',
    description: 'NPC raiders are attacking! Defend territories to earn bonus rewards.',
    shortDescription: 'Stop the Raiders!',
    icon: '🛡️',
    config: {
      xpMultiplier: 1.5
    },
    participantRewards: { xp: 400, iron: 300 }
  },
  treasure_hunt: {
    name: 'Global Treasure Hunt',
    description: 'Hidden treasures have appeared worldwide! Be the first to find them.',
    shortDescription: 'Hunt for Treasure!',
    icon: '🗺️',
    config: {
      treasureLocations: []
    },
    participantRewards: { gems: 50 },
    topContributorRewards: { gems: 500, exclusiveTitle: 'Master Treasure Hunter' }
  },
  faction_war: {
    name: 'Faction War',
    description: 'Builders vs Raiders vs Merchants! Your faction needs you. Fight for supremacy!',
    shortDescription: 'Fight for your Faction!',
    icon: '🏴',
    config: {
      xpMultiplier: 1.5
    },
    participantRewards: { xp: 300 },
    completionRewards: { gems: 100, exclusiveTitle: 'Faction Champion' }
  }
};

// ============================================================================
// Get Active Events
// ============================================================================

export const getActiveEvents = functions.https.onCall(async (data, context) => {
  const now = admin.firestore.Timestamp.now();
  
  // Get active and upcoming events
  const activeSnapshot = await db.collection('world_events')
    .where('status', '==', 'active')
    .orderBy('endTime', 'asc')
    .get();
  
  const upcomingSnapshot = await db.collection('world_events')
    .where('status', '==', 'scheduled')
    .where('startTime', '>', now)
    .orderBy('startTime', 'asc')
    .limit(5)
    .get();
  
  const activeEvents = activeSnapshot.docs.map(doc => doc.data() as WorldEvent);
  const upcomingEvents = upcomingSnapshot.docs.map(doc => doc.data() as WorldEvent);
  
  // If authenticated, include user's participation
  let userParticipation: Record<string, EventParticipation> = {};
  if (context.auth) {
    const participationSnapshot = await db.collection('event_participation')
      .where('userId', '==', context.auth.uid)
      .get();
    
    for (const doc of participationSnapshot.docs) {
      const participation = doc.data() as EventParticipation;
      userParticipation[participation.eventId] = participation;
    }
  }
  
  return {
    activeEvents,
    upcomingEvents,
    userParticipation,
    serverTime: now
  };
});

// ============================================================================
// Join Event
// ============================================================================

export const joinEvent = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { eventId } = data as { eventId: string };
  const userId = context.auth.uid;
  
  // Check event exists and is active
  const eventDoc = await db.collection('world_events').doc(eventId).get();
  if (!eventDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Event not found');
  }
  
  const event = eventDoc.data() as WorldEvent;
  if (event.status !== 'active') {
    throw new functions.https.HttpsError('failed-precondition', 'Event is not active');
  }
  
  // Check if already participating
  const participationId = `${userId}_${eventId}`;
  const existingParticipation = await db.collection('event_participation').doc(participationId).get();
  
  if (existingParticipation.exists) {
    return { 
      success: true, 
      alreadyJoined: true,
      participation: existingParticipation.data()
    };
  }
  
  // Create participation record
  const participation: EventParticipation = {
    userId ,
    eventId,
    joinedAt: admin.firestore.Timestamp.now(),
    contributions: 0,
    rewardsClaimed: false
  };
  
  await db.runTransaction(async (transaction) => {
    transaction.set(db.collection('event_participation').doc(participationId), participation);
    transaction.update(db.collection('world_events').doc(eventId), {
      participantCount: admin.firestore.FieldValue.increment(1)
    });
  });
  
  return { success: true, participation };
});

// ============================================================================
// Contribute to Event
// ============================================================================

export const contributeToEvent = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { eventId, contribution, contributionType: _contributionType } = data as { 
    eventId: string; 
    contribution: number;
    contributionType: string;
  };
  const userId = context.auth.uid;
  
  // Validate
  if (contribution <= 0) {
    throw new functions.https.HttpsError('invalid-argument', 'Contribution must be positive');
  }
  
  const participationId = `${userId}_${eventId}`;
  
  // Get event and participation
  const [eventDoc, participationDoc] = await Promise.all([
    db.collection('world_events').doc(eventId).get(),
    db.collection('event_participation').doc(participationId).get()
  ]);
  
  if (!eventDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Event not found');
  }
  
  const event = eventDoc.data() as WorldEvent;
  if (event.status !== 'active') {
    throw new functions.https.HttpsError('failed-precondition', 'Event is not active');
  }
  
  // Auto-join if not participating
  let isNewParticipant = false;
  if (!participationDoc.exists) {
    isNewParticipant = true;
  }
  
  await db.runTransaction(async (transaction) => {
    const eventUpdate: Record<string, unknown> = {
      totalContributions: admin.firestore.FieldValue.increment(contribution)
    };
    
    // Update global progress for world boss type events
    if (event.type === 'world_boss' && event.config.bossCurrentHealth) {
      const newHealth = Math.max(0, (event.config.bossCurrentHealth || 0) - contribution);
      eventUpdate['config.bossCurrentHealth'] = newHealth;
      eventUpdate['globalProgress'] = (event.config.bossHealth || 0) - newHealth;
      
      // Check if boss defeated
      if (newHealth === 0 && event.status === 'active') {
        eventUpdate['status'] = 'completed';
      }
    }
    
    if (isNewParticipant) {
      eventUpdate['participantCount'] = admin.firestore.FieldValue.increment(1);
      const newParticipation: EventParticipation = {
        userId,
        eventId,
        joinedAt: admin.firestore.Timestamp.now(),
        contributions: contribution,
        lastContributionAt: admin.firestore.Timestamp.now(),
        rewardsClaimed: false
      };
      transaction.set(db.collection('event_participation').doc(participationId), newParticipation);
    } else {
      transaction.update(db.collection('event_participation').doc(participationId), {
        contributions: admin.firestore.FieldValue.increment(contribution),
        lastContributionAt: admin.firestore.Timestamp.now()
      });
    }
    
    transaction.update(db.collection('world_events').doc(eventId), eventUpdate);
  });
  
  return { 
    success: true, 
    contribution,
    eventProgress: event.globalProgress
  };
});

// ============================================================================
// Claim Event Rewards
// ============================================================================

export const claimEventRewards = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { eventId } = data as { eventId: string };
  const userId = context.auth.uid;
  const participationId = `${userId}_${eventId}`;
  
  // Get event and participation
  const [eventDoc, participationDoc, userDoc] = await Promise.all([
    db.collection('world_events').doc(eventId).get(),
    db.collection('event_participation').doc(participationId).get(),
    db.collection('users').doc(userId).get()
  ]);
  
  if (!eventDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Event not found');
  }
  if (!participationDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Not participating in event');
  }
  if (!userDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'User not found');
  }
  
  const event = eventDoc.data() as WorldEvent;
  const participation = participationDoc.data() as EventParticipation;
  
  // Check if event is completed or ended
  const now = admin.firestore.Timestamp.now();
  if (event.status === 'active' && event.endTime.toMillis() > now.toMillis()) {
    throw new functions.https.HttpsError('failed-precondition', 'Event still active');
  }
  
  // Check if already claimed
  if (participation.rewardsClaimed) {
    throw new functions.https.HttpsError('already-exists', 'Rewards already claimed');
  }
  
  // Calculate rewards
  let totalRewards: EventRewards = { ...event.participantRewards };
  
  // Add completion rewards if global target met
  if (event.completionRewards && event.globalProgress && event.globalTarget) {
    if (event.globalProgress >= event.globalTarget) {
      totalRewards = mergeRewards(totalRewards, event.completionRewards);
    }
  }
  
  // Check if top contributor
  if (event.topContributorRewards) {
    const topContributors = await db.collection('event_participation')
      .where('eventId', '==', eventId)
      .orderBy('contributions', 'desc')
      .limit(10)
      .get();
    
    const isTopContributor = topContributors.docs.some(doc => doc.id === participationId);
    if (isTopContributor) {
      totalRewards = mergeRewards(totalRewards, event.topContributorRewards);
    }
  }
  
  // Apply rewards
  const userUpdate: Record<string, unknown> = {};
  
  if (totalRewards.xp) {
    userUpdate['stats.totalXp'] = admin.firestore.FieldValue.increment(totalRewards.xp);
  }
  if (totalRewards.gems) {
    userUpdate['resources.gems'] = admin.firestore.FieldValue.increment(totalRewards.gems);
  }
  if (totalRewards.stone) {
    userUpdate['resources.stone'] = admin.firestore.FieldValue.increment(totalRewards.stone);
  }
  if (totalRewards.wood) {
    userUpdate['resources.wood'] = admin.firestore.FieldValue.increment(totalRewards.wood);
  }
  if (totalRewards.iron) {
    userUpdate['resources.iron'] = admin.firestore.FieldValue.increment(totalRewards.iron);
  }
  if (totalRewards.crystal) {
    userUpdate['resources.crystal'] = admin.firestore.FieldValue.increment(totalRewards.crystal);
  }
  if (totalRewards.arcaneEssence) {
    userUpdate['resources.arcaneEssence'] = admin.firestore.FieldValue.increment(totalRewards.arcaneEssence);
  }
  
  // Handle exclusive rewards
  const exclusives: string[] = [];
  if (totalRewards.exclusiveTitle) {
    exclusives.push(`title:${totalRewards.exclusiveTitle}`);
    userUpdate['unlockedTitles'] = admin.firestore.FieldValue.arrayUnion(totalRewards.exclusiveTitle);
  }
  if (totalRewards.exclusiveBadge) {
    exclusives.push(`badge:${totalRewards.exclusiveBadge}`);
    userUpdate['unlockedBadges'] = admin.firestore.FieldValue.arrayUnion(totalRewards.exclusiveBadge);
  }
  if (totalRewards.exclusiveBlock) {
    exclusives.push(`block:${totalRewards.exclusiveBlock}`);
    userUpdate['unlockedBlocks'] = admin.firestore.FieldValue.arrayUnion(totalRewards.exclusiveBlock);
  }
  
  await db.runTransaction(async (transaction) => {
    transaction.update(db.collection('users').doc(userId), userUpdate);
    transaction.update(db.collection('event_participation').doc(participationId), {
      rewardsClaimed: true,
      claimedAt: admin.firestore.Timestamp.now()
    });
  });
  
  // Send notification
  await sendNotification(userId, {
    type: 'achievement_unlocked',
    title: `${event.name} Rewards!`,
    body: `You earned ${totalRewards.xp || 0} XP and ${totalRewards.gems || 0} gems!`,
    data: { eventId }
  });
  
  return { 
    success: true, 
    rewards: totalRewards,
    exclusives
  };
});

// ============================================================================
// Admin: Create Event
// ============================================================================

export const createWorldEvent = functions.https.onCall(async (data, context) => {
  // Verify admin (in production, check custom claims)
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { 
    type, 
    name, 
    description, 
    startTime, 
    endTime,
    customConfig,
    regions 
  } = data as {
    type: WorldEventType;
    name?: string;
    description?: string;
    startTime: string; // ISO string
    endTime: string;
    customConfig?: Partial<WorldEventConfig>;
    regions?: string[];
  };
  
  // Get template
  const template = EVENT_TEMPLATES[type];
  if (!template) {
    throw new functions.https.HttpsError('invalid-argument', 'Invalid event type');
  }
  
  const eventId = db.collection('world_events').doc().id;
  const start = admin.firestore.Timestamp.fromDate(new Date(startTime));
  const end = admin.firestore.Timestamp.fromDate(new Date(endTime));
  const now = admin.firestore.Timestamp.now();
  
  const event: WorldEvent = {
    id: eventId,
    type,
    name: name || template.name || type,
    description: description || template.description || '',
    shortDescription: template.shortDescription || '',
    icon: template.icon || '🎮',
    startTime: start,
    endTime: end,
    status: start.toMillis() <= now.toMillis() ? 'active' : 'scheduled',
    config: { ...template.config, ...customConfig },
    participantRewards: template.participantRewards || {},
    completionRewards: template.completionRewards,
    topContributorRewards: template.topContributorRewards,
    participantCount: 0,
    totalContributions: 0,
    regions,
    createdAt: now
  };
  
  // Initialize world boss health
  if (type === 'world_boss') {
    event.config.bossCurrentHealth = event.config.bossHealth;
    event.globalProgress = 0;
    event.globalTarget = event.config.bossHealth;
  }
  
  await db.collection('world_events').doc(eventId).set(event);
  
  functions.logger.info(`World event created: ${eventId} - ${event.name}`);
  
  return { success: true, event };
});

// ============================================================================
// Scheduled: Activate Events
// ============================================================================

export const activateScheduledEvents = functions.pubsub
  .schedule('every 5 minutes')
  .onRun(async () => {
    const now = admin.firestore.Timestamp.now();
    
    // Find events that should be activated
    const scheduledEvents = await db.collection('world_events')
      .where('status', '==', 'scheduled')
      .where('startTime', '<=', now)
      .get();
    
    const batch = db.batch();
    const activatedEvents: string[] = [];
    
    for (const doc of scheduledEvents.docs) {
      const event = doc.data() as WorldEvent;
      batch.update(doc.ref, { status: 'active' });
      activatedEvents.push(event.id);
      
      // Send notification to all users (in production, use FCM topics)
      functions.logger.info(`Event activated: ${event.name}`);
    }
    
    if (activatedEvents.length > 0) {
      await batch.commit();
      functions.logger.info(`Activated ${activatedEvents.length} events`);
    }
  });

// ============================================================================
// Scheduled: Complete Ended Events
// ============================================================================

export const completeEndedEvents = functions.pubsub
  .schedule('every 5 minutes')
  .onRun(async () => {
    const now = admin.firestore.Timestamp.now();
    
    // Find events that have ended
    const endedEvents = await db.collection('world_events')
      .where('status', '==', 'active')
      .where('endTime', '<=', now)
      .get();
    
    const batch = db.batch();
    
    for (const doc of endedEvents.docs) {
      batch.update(doc.ref, { status: 'completed' });
      functions.logger.info(`Event completed: ${doc.id}`);
    }
    
    if (endedEvents.size > 0) {
      await batch.commit();
      functions.logger.info(`Completed ${endedEvents.size} events`);
    }
  });

// ============================================================================
// Get Event Leaderboard
// ============================================================================

export const getEventLeaderboard = functions.https.onCall(async (data, context) => {
  const { eventId, limit = 100 } = data as { eventId: string; limit?: number };
  
  const leaderboardSnapshot = await db.collection('event_participation')
    .where('eventId', '==', eventId)
    .orderBy('contributions', 'desc')
    .limit(limit)
    .get();
  
  const leaderboard = await Promise.all(
    leaderboardSnapshot.docs.map(async (doc, index) => {
      const participation = doc.data() as EventParticipation;
      const userDoc = await db.collection('users').doc(participation.userId ).get();
      const user = userDoc.data();
      
      return {
        rank: index + 1,
        userId: participation.userId ,
        displayName: user?.displayName || 'Unknown',
        allianceTag: user?.allianceTag,
        contributions: participation.contributions,
        joinedAt: participation.joinedAt
      };
    })
  );
  
  // Get user's rank if authenticated
  let userRank = null;
  if (context.auth) {
    const participationId = `${context.auth.uid}_${eventId}`;
    const userParticipation = await db.collection('event_participation').doc(participationId).get();
    
    if (userParticipation.exists) {
      const participation = userParticipation.data() as EventParticipation;
      const higherCount = await db.collection('event_participation')
        .where('eventId', '==', eventId)
        .where('contributions', '>', participation.contributions)
        .count()
        .get();
      
      userRank = {
        rank: higherCount.data().count + 1,
        contributions: participation.contributions
      };
    }
  }
  
  return { leaderboard, userRank };
});

// ============================================================================
// Helper Functions
// ============================================================================

function mergeRewards(base: EventRewards, additional: EventRewards): EventRewards {
  return {
    xp: (base.xp || 0) + (additional.xp || 0),
    gems: (base.gems || 0) + (additional.gems || 0),
    stone: (base.stone || 0) + (additional.stone || 0),
    wood: (base.wood || 0) + (additional.wood || 0),
    iron: (base.iron || 0) + (additional.iron || 0),
    crystal: (base.crystal || 0) + (additional.crystal || 0),
    arcaneEssence: (base.arcaneEssence || 0) + (additional.arcaneEssence || 0),
    exclusiveTitle: additional.exclusiveTitle || base.exclusiveTitle,
    exclusiveBadge: additional.exclusiveBadge || base.exclusiveBadge,
    exclusiveBlock: additional.exclusiveBlock || base.exclusiveBlock
  };
}

// ============================================================================
// Apply Event Multipliers (Called by other systems)
// ============================================================================

export async function getActiveEventMultipliers(): Promise<{
  xpMultiplier: number;
  resourceMultiplier: number;
  cooldownReduction: number;
}> {
  const activeEvents = await db.collection('world_events')
    .where('status', '==', 'active')
    .get();
  
  let xpMultiplier = 1.0;
  let resourceMultiplier = 1.0;
  let cooldownReduction = 0;
  
  for (const doc of activeEvents.docs) {
    const event = doc.data() as WorldEvent;
    
    if (event.config.xpMultiplier) {
      xpMultiplier = Math.max(xpMultiplier, event.config.xpMultiplier);
    }
    if (event.config.resourceMultiplier) {
      resourceMultiplier = Math.max(resourceMultiplier, event.config.resourceMultiplier);
    }
    if (event.config.cooldownReduction) {
      cooldownReduction = Math.max(cooldownReduction, event.config.cooldownReduction);
    }
  }
  
  return { xpMultiplier, resourceMultiplier, cooldownReduction };
}
