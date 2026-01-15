/**
 * Apex Citadels - Analytics & Telemetry System
 * 
 * Data-driven decisions require comprehensive analytics:
 * - Player behavior tracking
 * - Game economy monitoring
 * - Performance metrics
 * - A/B testing support
 * - Retention analytics
 */

import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';

const db = admin.firestore();

// ============================================================================
// Types
// ============================================================================

export interface AnalyticsEvent {
  id: string;
  userId: string;
  sessionId: string;
  eventType: string;
  eventName: string;
  category: 'gameplay' | 'economy' | 'social' | 'engagement' | 'technical' | 'monetization';
  properties: Record<string, unknown>;
  timestamp: admin.firestore.Timestamp;
  
  // Context
  platform: string;
  appVersion: string;
  osVersion: string;
  deviceModel: string;
  
  // Location (optional, anonymized)
  region?: string;
  country?: string;
}

export interface UserSession {
  id: string;
  userId: string;
  startTime: admin.firestore.Timestamp;
  endTime?: admin.firestore.Timestamp;
  durationSeconds?: number;
  
  // Session stats
  eventsCount: number;
  actions: SessionAction[];
  
  // Technical
  platform: string;
  appVersion: string;
}

export interface SessionAction {
  action: string;
  count: number;
  firstAt: admin.firestore.Timestamp;
  lastAt: admin.firestore.Timestamp;
}

export interface DailyMetrics {
  date: string; // YYYY-MM-DD
  
  // User metrics
  dau: number; // Daily Active Users
  newUsers: number;
  returningUsers: number;
  
  // Engagement
  totalSessions: number;
  avgSessionDuration: number;
  avgSessionsPerUser: number;
  
  // Gameplay
  territoriesClaimed: number;
  territoriesConquered: number;
  attacksInitiated: number;
  defensesTriggered: number;
  blocksPlaced: number;
  
  // Economy
  resourcesGenerated: Record<string, number>;
  resourcesSpent: Record<string, number>;
  gemsEarned: number;
  gemsSpent: number;
  
  // Social
  alliancesCreated: number;
  allianceJoins: number;
  friendRequestsSent: number;
  giftsSent: number;
  
  // Monetization
  revenue: number;
  purchases: number;
  avgPurchaseValue: number;
  payingUsers: number;
  
  // Retention
  d1Retention: number;
  d7Retention: number;
  d30Retention: number;
  
  calculatedAt: admin.firestore.Timestamp;
}

export interface FunnelStep {
  name: string;
  count: number;
  conversionRate?: number;
}

export interface ABTest {
  id: string;
  name: string;
  description: string;
  status: 'draft' | 'running' | 'completed' | 'cancelled';
  
  // Configuration
  variants: ABVariant[];
  targetPercentage: number;
  
  // Timing
  startDate: admin.firestore.Timestamp;
  endDate?: admin.firestore.Timestamp;
  
  // Results
  results?: ABTestResults;
  
  createdAt: admin.firestore.Timestamp;
}

export interface ABVariant {
  id: string;
  name: string;
  percentage: number;
  config: Record<string, unknown>;
}

export interface ABTestResults {
  totalParticipants: number;
  variantResults: {
    variantId: string;
    participants: number;
    conversions: number;
    conversionRate: number;
    avgValue: number;
  }[];
  winner?: string;
  confidence?: number;
}

// ============================================================================
// Event Tracking
// ============================================================================

export const trackEvent = functions.https.onCall(async (data, context) => {
  const { 
    eventType, 
    eventName, 
    category, 
    properties,
    sessionId,
    platform,
    appVersion,
    osVersion,
    deviceModel,
    region,
    country
  } = data as {
    eventType: string;
    eventName: string;
    category: AnalyticsEvent['category'];
    properties: Record<string, unknown>;
    sessionId: string;
    platform: string;
    appVersion: string;
    osVersion?: string;
    deviceModel?: string;
    region?: string;
    country?: string;
  };
  
  const userId = context.auth?.uid || 'anonymous';
  
  const eventRef = db.collection('analytics_events').doc();
  const event: AnalyticsEvent = {
    id: eventRef.id,
    userId,
    sessionId,
    eventType,
    eventName,
    category,
    properties,
    timestamp: admin.firestore.Timestamp.now(),
    platform,
    appVersion,
    osVersion: osVersion || 'unknown',
    deviceModel: deviceModel || 'unknown',
    region,
    country
  };
  
  await eventRef.set(event);
  
  // Update session
  if (userId !== 'anonymous') {
    await updateSession(userId, sessionId, eventName);
  }
  
  return { success: true, eventId: eventRef.id };
});

// ============================================================================
// Session Management
// ============================================================================

export const startSession = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    return { sessionId: `anon_${Date.now()}` };
  }
  
  const { platform, appVersion } = data as { platform: string; appVersion: string };
  const userId = context.auth.uid;
  
  const sessionRef = db.collection('user_sessions').doc();
  const session: UserSession = {
    id: sessionRef.id,
    userId,
    startTime: admin.firestore.Timestamp.now(),
    eventsCount: 0,
    actions: [],
    platform,
    appVersion
  };
  
  await sessionRef.set(session);
  
  // Update user's last active
  await db.collection('users').doc(userId).update({
    lastActive: admin.firestore.Timestamp.now(),
    'stats.totalSessions': admin.firestore.FieldValue.increment(1)
  });
  
  // Track as daily active
  await trackDailyActive(userId);
  
  return { sessionId: sessionRef.id };
});

export const endSession = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    return { success: true };
  }
  
  const { sessionId } = data as { sessionId: string };
  const userId = context.auth.uid;
  
  const sessionDoc = await db.collection('user_sessions').doc(sessionId).get();
  if (!sessionDoc.exists) {
    return { success: false };
  }
  
  const session = sessionDoc.data() as UserSession;
  const endTime = admin.firestore.Timestamp.now();
  const durationSeconds = Math.floor(
    (endTime.toMillis() - session.startTime.toMillis()) / 1000
  );
  
  await sessionDoc.ref.update({
    endTime,
    durationSeconds
  });
  
  // Update user's total playtime
  await db.collection('users').doc(userId).update({
    'stats.totalPlaytimeSeconds': admin.firestore.FieldValue.increment(durationSeconds)
  });
  
  return { success: true, durationSeconds };
});

async function updateSession(userId: string, sessionId: string, action: string): Promise<void> {
  const sessionRef = db.collection('user_sessions').doc(sessionId);
  const sessionDoc = await sessionRef.get();
  
  if (!sessionDoc.exists) return;
  
  const session = sessionDoc.data() as UserSession;
  const now = admin.firestore.Timestamp.now();
  
  // Find or create action
  const existingAction = session.actions.find(a => a.action === action);
  
  if (existingAction) {
    existingAction.count++;
    existingAction.lastAt = now;
  } else {
    session.actions.push({
      action,
      count: 1,
      firstAt: now,
      lastAt: now
    });
  }
  
  // Keep only top 50 actions
  if (session.actions.length > 50) {
    session.actions = session.actions
      .sort((a, b) => b.count - a.count)
      .slice(0, 50);
  }
  
  await sessionRef.update({
    eventsCount: admin.firestore.FieldValue.increment(1),
    actions: session.actions
  });
}

// ============================================================================
// Daily Active Users Tracking
// ============================================================================

async function trackDailyActive(userId: string): Promise<void> {
  const today = new Date().toISOString().split('T')[0];
  const dauRef = db.collection('daily_active_users').doc(today);
  
  await dauRef.set({
    date: today,
    users: admin.firestore.FieldValue.arrayUnion(userId),
    count: admin.firestore.FieldValue.increment(1)
  }, { merge: true });
}

// ============================================================================
// Funnel Analysis
// ============================================================================

export const getFunnelAnalysis = functions.https.onCall(async (data, context) => {
  const { funnelSteps, startDate, endDate } = data as {
    funnelSteps: string[];
    startDate: string;
    endDate: string;
  };
  
  const results: FunnelStep[] = [];
  
  for (let i = 0; i < funnelSteps.length; i++) {
    const step = funnelSteps[i];
    
    const eventsSnapshot = await db.collection('analytics_events')
      .where('eventName', '==', step)
      .where('timestamp', '>=', admin.firestore.Timestamp.fromDate(new Date(startDate)))
      .where('timestamp', '<=', admin.firestore.Timestamp.fromDate(new Date(endDate)))
      .get();
    
    // Count unique users
    const uniqueUsers = new Set<string>();
    eventsSnapshot.docs.forEach(doc => {
      uniqueUsers.add(doc.data().userId);
    });
    
    const count = uniqueUsers.size;
    const prevCount = i > 0 ? results[i - 1].count : count;
    
    results.push({
      name: step,
      count,
      conversionRate: prevCount > 0 ? (count / prevCount) * 100 : 100
    });
  }
  
  return { funnel: results };
});

// ============================================================================
// Game Economy Metrics
// ============================================================================

export const getEconomyMetrics = functions.https.onCall(async (data, context) => {
  const { days = 7 } = data as { days?: number };
  
  const startDate = new Date();
  startDate.setDate(startDate.getDate() - days);
  
  const metricsSnapshot = await db.collection('daily_metrics')
    .where('date', '>=', startDate.toISOString().split('T')[0])
    .orderBy('date', 'desc')
    .get();
  
  const metrics = metricsSnapshot.docs.map(doc => doc.data());
  
  // Calculate aggregates
  const totals = {
    gemsEarned: 0,
    gemsSpent: 0,
    resourcesGenerated: {} as Record<string, number>,
    resourcesSpent: {} as Record<string, number>
  };
  
  metrics.forEach(m => {
    totals.gemsEarned += m.gemsEarned || 0;
    totals.gemsSpent += m.gemsSpent || 0;
    
    Object.entries(m.resourcesGenerated || {}).forEach(([resource, amount]) => {
      totals.resourcesGenerated[resource] = (totals.resourcesGenerated[resource] || 0) + (amount as number);
    });
    
    Object.entries(m.resourcesSpent || {}).forEach(([resource, amount]) => {
      totals.resourcesSpent[resource] = (totals.resourcesSpent[resource] || 0) + (amount as number);
    });
  });
  
  return { 
    daily: metrics,
    totals,
    gemInflation: totals.gemsEarned - totals.gemsSpent,
    days
  };
});

// ============================================================================
// Retention Analysis
// ============================================================================

export const getRetentionCohorts = functions.https.onCall(async (data, context) => {
  const { cohortDate, retentionDays = [1, 7, 14, 30] } = data as {
    cohortDate: string;
    retentionDays?: number[];
  };
  
  // Get users who signed up on cohort date
  const cohortStart = new Date(cohortDate);
  const cohortEnd = new Date(cohortDate);
  cohortEnd.setDate(cohortEnd.getDate() + 1);
  
  const cohortUsersSnapshot = await db.collection('users')
    .where('createdAt', '>=', admin.firestore.Timestamp.fromDate(cohortStart))
    .where('createdAt', '<', admin.firestore.Timestamp.fromDate(cohortEnd))
    .get();
  
  const cohortUserIds = cohortUsersSnapshot.docs.map(doc => doc.id);
  const cohortSize = cohortUserIds.length;
  
  if (cohortSize === 0) {
    return { cohortDate, cohortSize: 0, retention: {} };
  }
  
  // Check retention for each day
  const retention: Record<number, { retained: number; rate: number }> = {};
  
  for (const day of retentionDays) {
    const checkDate = new Date(cohortDate);
    checkDate.setDate(checkDate.getDate() + day);
    const checkDateStr = checkDate.toISOString().split('T')[0];
    
    const dauDoc = await db.collection('daily_active_users').doc(checkDateStr).get();
    
    if (dauDoc.exists) {
      const dauUsers = dauDoc.data()?.users || [];
      const retained = cohortUserIds.filter(id => dauUsers.includes(id)).length;
      
      retention[day] = {
        retained,
        rate: (retained / cohortSize) * 100
      };
    } else {
      retention[day] = { retained: 0, rate: 0 };
    }
  }
  
  return { cohortDate, cohortSize, retention };
});

// ============================================================================
// A/B Testing
// ============================================================================

export const getABTestVariant = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { testId } = data as { testId: string };
  const userId = context.auth.uid;
  
  // Check if user is already assigned
  const assignmentDoc = await db.collection('ab_test_assignments')
    .doc(`${testId}_${userId}`)
    .get();
  
  if (assignmentDoc.exists) {
    return assignmentDoc.data();
  }
  
  // Get test config
  const testDoc = await db.collection('ab_tests').doc(testId).get();
  if (!testDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Test not found');
  }
  
  const test = testDoc.data() as ABTest;
  
  if (test.status !== 'running') {
    throw new functions.https.HttpsError('failed-precondition', 'Test is not running');
  }
  
  // Assign variant based on percentage
  const random = Math.random() * 100;
  let cumulativePercentage = 0;
  let assignedVariant: ABVariant | null = null;
  
  for (const variant of test.variants) {
    cumulativePercentage += variant.percentage;
    if (random <= cumulativePercentage) {
      assignedVariant = variant;
      break;
    }
  }
  
  if (!assignedVariant) {
    // Fallback to control (first variant)
    assignedVariant = test.variants[0];
  }
  
  // Store assignment
  const assignment = {
    testId,
    userId,
    variantId: assignedVariant.id,
    variantName: assignedVariant.name,
    config: assignedVariant.config,
    assignedAt: admin.firestore.Timestamp.now()
  };
  
  await db.collection('ab_test_assignments').doc(`${testId}_${userId}`).set(assignment);
  
  return assignment;
});

export const recordABTestConversion = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { testId, conversionType, value } = data as {
    testId: string;
    conversionType: string;
    value?: number;
  };
  const userId = context.auth.uid;
  
  const assignmentId = `${testId}_${userId}`;
  const assignmentDoc = await db.collection('ab_test_assignments').doc(assignmentId).get();
  
  if (!assignmentDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'User not in test');
  }
  
  await db.collection('ab_test_conversions').add({
    testId,
    userId,
    variantId: assignmentDoc.data()?.variantId,
    conversionType,
    value: value || 1,
    convertedAt: admin.firestore.Timestamp.now()
  });
  
  return { success: true };
});

// ============================================================================
// Daily Metrics Calculation (Scheduled)
// ============================================================================

export const calculateDailyMetrics = functions.pubsub
  .schedule('every day 01:00')
  .timeZone('UTC')
  .onRun(async () => {
    const yesterday = new Date();
    yesterday.setDate(yesterday.getDate() - 1);
    const dateStr = yesterday.toISOString().split('T')[0];
    
    const dayStart = new Date(dateStr);
    const dayEnd = new Date(dateStr);
    dayEnd.setDate(dayEnd.getDate() + 1);
    
    // Get DAU
    const dauDoc = await db.collection('daily_active_users').doc(dateStr).get();
    const dau = dauDoc.exists ? (dauDoc.data()?.users?.length || 0) : 0;
    
    // Get new users
    const newUsersSnapshot = await db.collection('users')
      .where('createdAt', '>=', admin.firestore.Timestamp.fromDate(dayStart))
      .where('createdAt', '<', admin.firestore.Timestamp.fromDate(dayEnd))
      .get();
    const newUsers = newUsersSnapshot.size;
    
    // Get sessions
    const sessionsSnapshot = await db.collection('user_sessions')
      .where('startTime', '>=', admin.firestore.Timestamp.fromDate(dayStart))
      .where('startTime', '<', admin.firestore.Timestamp.fromDate(dayEnd))
      .get();
    
    const totalSessions = sessionsSnapshot.size;
    let totalDuration = 0;
    sessionsSnapshot.docs.forEach(doc => {
      totalDuration += doc.data().durationSeconds || 0;
    });
    const avgSessionDuration = totalSessions > 0 ? totalDuration / totalSessions : 0;
    
    // Get gameplay events
    const eventsSnapshot = await db.collection('analytics_events')
      .where('timestamp', '>=', admin.firestore.Timestamp.fromDate(dayStart))
      .where('timestamp', '<', admin.firestore.Timestamp.fromDate(dayEnd))
      .get();
    
    let territoriesClaimed = 0;
    let territoriesConquered = 0;
    let attacksInitiated = 0;
    let blocksPlaced = 0;
    
    eventsSnapshot.docs.forEach(doc => {
      const event = doc.data();
      switch (event.eventName) {
        case 'territory_claimed':
          territoriesClaimed++;
          break;
        case 'territory_conquered':
          territoriesConquered++;
          break;
        case 'attack_initiated':
          attacksInitiated++;
          break;
        case 'block_placed':
          blocksPlaced++;
          break;
      }
    });
    
    const metrics: DailyMetrics = {
      date: dateStr,
      dau,
      newUsers,
      returningUsers: dau - newUsers,
      totalSessions,
      avgSessionDuration,
      avgSessionsPerUser: dau > 0 ? totalSessions / dau : 0,
      territoriesClaimed,
      territoriesConquered,
      attacksInitiated,
      defensesTriggered: 0, // Calculate from events
      blocksPlaced,
      resourcesGenerated: {},
      resourcesSpent: {},
      gemsEarned: 0,
      gemsSpent: 0,
      alliancesCreated: 0,
      allianceJoins: 0,
      friendRequestsSent: 0,
      giftsSent: 0,
      revenue: 0,
      purchases: 0,
      avgPurchaseValue: 0,
      payingUsers: 0,
      d1Retention: 0,
      d7Retention: 0,
      d30Retention: 0,
      calculatedAt: admin.firestore.Timestamp.now()
    };
    
    await db.collection('daily_metrics').doc(dateStr).set(metrics);
    
    functions.logger.info('Daily metrics calculated', { date: dateStr, dau, newUsers });
  });

// ============================================================================
// Predefined Event Names (For consistency)
// ============================================================================

export const EventNames = {
  // Gameplay
  TERRITORY_CLAIMED: 'territory_claimed',
  TERRITORY_CONQUERED: 'territory_conquered',
  TERRITORY_LOST: 'territory_lost',
  ATTACK_INITIATED: 'attack_initiated',
  ATTACK_WON: 'attack_won',
  ATTACK_LOST: 'attack_lost',
  DEFENSE_TRIGGERED: 'defense_triggered',
  DEFENSE_WON: 'defense_won',
  DEFENSE_LOST: 'defense_lost',
  BLOCK_PLACED: 'block_placed',
  BLOCK_DESTROYED: 'block_destroyed',
  BUILDING_COMPLETED: 'building_completed',
  
  // Progression
  LEVEL_UP: 'level_up',
  ACHIEVEMENT_UNLOCKED: 'achievement_unlocked',
  DAILY_REWARD_CLAIMED: 'daily_reward_claimed',
  SEASON_LEVEL_UP: 'season_level_up',
  
  // Economy
  RESOURCES_GAINED: 'resources_gained',
  RESOURCES_SPENT: 'resources_spent',
  GEMS_PURCHASED: 'gems_purchased',
  IAP_COMPLETED: 'iap_completed',
  
  // Social
  ALLIANCE_JOINED: 'alliance_joined',
  ALLIANCE_LEFT: 'alliance_left',
  FRIEND_ADDED: 'friend_added',
  GIFT_SENT: 'gift_sent',
  GIFT_RECEIVED: 'gift_received',
  
  // Engagement
  APP_OPEN: 'app_open',
  SESSION_START: 'session_start',
  SESSION_END: 'session_end',
  TUTORIAL_STARTED: 'tutorial_started',
  TUTORIAL_COMPLETED: 'tutorial_completed',
  PUSH_NOTIFICATION_OPENED: 'push_notification_opened',
  
  // Technical
  ERROR_OCCURRED: 'error_occurred',
  CRASH_DETECTED: 'crash_detected',
  PERFORMANCE_ISSUE: 'performance_issue'
};
