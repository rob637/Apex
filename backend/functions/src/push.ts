/**
 * Push Notifications System
 * 
 * Complete push notification service for re-engagement and real-time alerts
 * Supports iOS (APNs), Android (FCM), and topic-based messaging
 */

import { onCall, HttpsError, onRequest } from 'firebase-functions/v2/https';
import { onSchedule } from 'firebase-functions/v2/scheduler';
import { onDocumentCreated, onDocumentUpdated } from 'firebase-functions/v2/firestore';
import * as admin from 'firebase-admin';

const db = admin.firestore();
const messaging = admin.messaging();

// ============================================================================
// TYPES
// ============================================================================

export enum NotificationType {
  // Combat & Territory
  TERRITORY_ATTACKED = 'territory_attacked',
  TERRITORY_LOST = 'territory_lost',
  TERRITORY_DEFENDED = 'territory_defended',
  ATTACK_COMPLETE = 'attack_complete',
  
  // Alliance
  ALLIANCE_WAR_STARTED = 'alliance_war_started',
  ALLIANCE_WAR_ENDED = 'alliance_war_ended',
  ALLIANCE_INVITE = 'alliance_invite',
  ALLIANCE_MESSAGE = 'alliance_message',
  ALLIANCE_MEMBER_JOINED = 'alliance_member_joined',
  
  // Social
  FRIEND_REQUEST = 'friend_request',
  FRIEND_NEARBY = 'friend_nearby',
  FRIEND_STARTED_PLAYING = 'friend_started_playing',
  
  // Rewards & Progress
  DAILY_REWARD_AVAILABLE = 'daily_reward_available',
  LEVEL_UP = 'level_up',
  ACHIEVEMENT_UNLOCKED = 'achievement_unlocked',
  CHEST_READY = 'chest_ready',
  
  // Season & Events
  SEASON_TIER_UNLOCKED = 'season_tier_unlocked',
  SEASON_ENDING_SOON = 'season_ending_soon',
  WORLD_EVENT_STARTING = 'world_event_starting',
  WORLD_EVENT_ENDING = 'world_event_ending',
  
  // Economy
  RESOURCES_FULL = 'resources_full',
  BUILDING_COMPLETE = 'building_complete',
  VIP_REWARD_READY = 'vip_reward_ready',
  SPECIAL_OFFER = 'special_offer',
  
  // System
  MAINTENANCE = 'maintenance',
  NEW_UPDATE = 'new_update',
  WELCOME_BACK = 'welcome_back'
}

export enum NotificationPriority {
  LOW = 'low',
  NORMAL = 'normal',
  HIGH = 'high',
  URGENT = 'urgent'
}

export interface NotificationPayload {
  type: NotificationType;
  title: string;
  body: string;
  imageUrl?: string;
  data?: Record<string, string>;
  priority?: NotificationPriority;
  badge?: number;
  sound?: string;
  channelId?: string;
  collapseKey?: string;
  ttlSeconds?: number;
}

export interface UserNotificationSettings {
  enabled: boolean;
  fcmTokens: string[];
  preferences: {
    combat: boolean;
    alliance: boolean;
    social: boolean;
    rewards: boolean;
    events: boolean;
    marketing: boolean;
  };
  quietHours: {
    enabled: boolean;
    startHour: number;
    endHour: number;
    timezone: string;
  };
  lastUpdated: FirebaseFirestore.Timestamp;
}

// ScheduledNotification interface - used for scheduled notification documents
export interface ScheduledNotification {
  id: string;
  userId: string;
  payload: NotificationPayload;
  scheduledFor: FirebaseFirestore.Timestamp;
  status: 'pending' | 'sent' | 'failed' | 'cancelled';
  createdAt: FirebaseFirestore.Timestamp;
}

// ============================================================================
// NOTIFICATION TEMPLATES
// ============================================================================

const NOTIFICATION_TEMPLATES: Record<NotificationType, {
  title: string;
  body: string;
  sound: string;
  channelId: string;
  priority: NotificationPriority;
  category: keyof UserNotificationSettings['preferences'];
}> = {
  // Combat & Territory
  [NotificationType.TERRITORY_ATTACKED]: {
    title: '⚔️ Your Territory is Under Attack!',
    body: '{attackerName} is attacking your citadel at {location}!',
    sound: 'alert.wav',
    channelId: 'combat',
    priority: NotificationPriority.URGENT,
    category: 'combat'
  },
  [NotificationType.TERRITORY_LOST]: {
    title: '💔 Territory Lost',
    body: 'You lost control of {location} to {attackerName}',
    sound: 'defeat.wav',
    channelId: 'combat',
    priority: NotificationPriority.HIGH,
    category: 'combat'
  },
  [NotificationType.TERRITORY_DEFENDED]: {
    title: '🛡️ Defense Successful!',
    body: 'You defended {location} against {attackerName}! +{xp} XP',
    sound: 'victory.wav',
    channelId: 'combat',
    priority: NotificationPriority.NORMAL,
    category: 'combat'
  },
  [NotificationType.ATTACK_COMPLETE]: {
    title: '⚔️ Attack Complete',
    body: 'Your attack on {location} has finished. Tap to see results!',
    sound: 'complete.wav',
    channelId: 'combat',
    priority: NotificationPriority.NORMAL,
    category: 'combat'
  },
  
  // Alliance
  [NotificationType.ALLIANCE_WAR_STARTED]: {
    title: '🏰 Alliance War Begins!',
    body: '{allianceName} vs {enemyAlliance} - Rally your forces!',
    sound: 'war_horn.wav',
    channelId: 'alliance',
    priority: NotificationPriority.URGENT,
    category: 'alliance'
  },
  [NotificationType.ALLIANCE_WAR_ENDED]: {
    title: '🏆 Alliance War Ended',
    body: '{result}! Your alliance {outcome}',
    sound: 'fanfare.wav',
    channelId: 'alliance',
    priority: NotificationPriority.HIGH,
    category: 'alliance'
  },
  [NotificationType.ALLIANCE_INVITE]: {
    title: '📨 Alliance Invitation',
    body: '{allianceName} has invited you to join their ranks!',
    sound: 'notification.wav',
    channelId: 'alliance',
    priority: NotificationPriority.NORMAL,
    category: 'alliance'
  },
  [NotificationType.ALLIANCE_MESSAGE]: {
    title: '💬 {senderName}',
    body: '{message}',
    sound: 'message.wav',
    channelId: 'alliance',
    priority: NotificationPriority.LOW,
    category: 'alliance'
  },
  [NotificationType.ALLIANCE_MEMBER_JOINED]: {
    title: '👋 New Alliance Member',
    body: '{memberName} has joined {allianceName}!',
    sound: 'notification.wav',
    channelId: 'alliance',
    priority: NotificationPriority.LOW,
    category: 'alliance'
  },
  
  // Social
  [NotificationType.FRIEND_REQUEST]: {
    title: '👋 Friend Request',
    body: '{senderName} wants to be your friend!',
    sound: 'notification.wav',
    channelId: 'social',
    priority: NotificationPriority.NORMAL,
    category: 'social'
  },
  [NotificationType.FRIEND_NEARBY]: {
    title: '📍 Friend Nearby',
    body: '{friendName} is playing nearby! Team up?',
    sound: 'ping.wav',
    channelId: 'social',
    priority: NotificationPriority.LOW,
    category: 'social'
  },
  [NotificationType.FRIEND_STARTED_PLAYING]: {
    title: '🎮 Friend Online',
    body: '{friendName} just started playing',
    sound: 'notification.wav',
    channelId: 'social',
    priority: NotificationPriority.LOW,
    category: 'social'
  },
  
  // Rewards & Progress
  [NotificationType.DAILY_REWARD_AVAILABLE]: {
    title: '🎁 Daily Reward Ready!',
    body: 'Claim your Day {day} reward before it resets!',
    sound: 'reward.wav',
    channelId: 'rewards',
    priority: NotificationPriority.NORMAL,
    category: 'rewards'
  },
  [NotificationType.LEVEL_UP]: {
    title: '⭐ Level Up!',
    body: 'Congratulations! You reached Level {level}!',
    sound: 'levelup.wav',
    channelId: 'rewards',
    priority: NotificationPriority.NORMAL,
    category: 'rewards'
  },
  [NotificationType.ACHIEVEMENT_UNLOCKED]: {
    title: '🏅 Achievement Unlocked!',
    body: '{achievementName} - {reward}',
    sound: 'achievement.wav',
    channelId: 'rewards',
    priority: NotificationPriority.NORMAL,
    category: 'rewards'
  },
  [NotificationType.CHEST_READY]: {
    title: '📦 Chest Ready!',
    body: 'Your {chestType} chest is ready to open!',
    sound: 'chest.wav',
    channelId: 'rewards',
    priority: NotificationPriority.NORMAL,
    category: 'rewards'
  },
  
  // Season & Events
  [NotificationType.SEASON_TIER_UNLOCKED]: {
    title: '🌟 Season Tier Unlocked!',
    body: 'You reached Tier {tier}! Claim your rewards!',
    sound: 'tier_up.wav',
    channelId: 'events',
    priority: NotificationPriority.NORMAL,
    category: 'events'
  },
  [NotificationType.SEASON_ENDING_SOON]: {
    title: '⏰ Season Ending Soon!',
    body: 'Only {days} days left! Don\'t miss your rewards!',
    sound: 'alert.wav',
    channelId: 'events',
    priority: NotificationPriority.HIGH,
    category: 'events'
  },
  [NotificationType.WORLD_EVENT_STARTING]: {
    title: '🌍 World Event Starting!',
    body: '{eventName} begins in {time}! Prepare your forces!',
    sound: 'event.wav',
    channelId: 'events',
    priority: NotificationPriority.HIGH,
    category: 'events'
  },
  [NotificationType.WORLD_EVENT_ENDING]: {
    title: '🌍 World Event Ending!',
    body: '{eventName} ends in {time}! Final push!',
    sound: 'alert.wav',
    channelId: 'events',
    priority: NotificationPriority.HIGH,
    category: 'events'
  },
  
  // Economy
  [NotificationType.RESOURCES_FULL]: {
    title: '📦 Storage Full!',
    body: 'Your {resource} storage is full. Spend or upgrade!',
    sound: 'notification.wav',
    channelId: 'rewards',
    priority: NotificationPriority.LOW,
    category: 'rewards'
  },
  [NotificationType.BUILDING_COMPLETE]: {
    title: '🏗️ Building Complete!',
    body: 'Your {buildingName} is ready!',
    sound: 'complete.wav',
    channelId: 'rewards',
    priority: NotificationPriority.NORMAL,
    category: 'rewards'
  },
  [NotificationType.VIP_REWARD_READY]: {
    title: '👑 VIP Reward Ready!',
    body: 'Your daily VIP gems are waiting!',
    sound: 'reward.wav',
    channelId: 'rewards',
    priority: NotificationPriority.NORMAL,
    category: 'rewards'
  },
  [NotificationType.SPECIAL_OFFER]: {
    title: '🔥 Special Offer!',
    body: '{offerName} - {discount}% off! Limited time!',
    sound: 'notification.wav',
    channelId: 'marketing',
    priority: NotificationPriority.LOW,
    category: 'marketing'
  },
  
  // System
  [NotificationType.MAINTENANCE]: {
    title: '🔧 Scheduled Maintenance',
    body: 'Servers will be down from {startTime} to {endTime}',
    sound: 'notification.wav',
    channelId: 'system',
    priority: NotificationPriority.HIGH,
    category: 'events'
  },
  [NotificationType.NEW_UPDATE]: {
    title: '🆕 New Update Available!',
    body: 'Version {version} is here with new features!',
    sound: 'notification.wav',
    channelId: 'system',
    priority: NotificationPriority.NORMAL,
    category: 'events'
  },
  [NotificationType.WELCOME_BACK]: {
    title: '👋 Welcome Back, Commander!',
    body: 'Your empire awaits! {days} days of rewards ready!',
    sound: 'welcome.wav',
    channelId: 'system',
    priority: NotificationPriority.NORMAL,
    category: 'rewards'
  }
};

// ============================================================================
// HELPER FUNCTIONS
// ============================================================================

/**
 * Check if user has notifications enabled for this category
 */
async function shouldSendNotification(
  userId: string,
  type: NotificationType
): Promise<{ canSend: boolean; settings: UserNotificationSettings | null; tokens: string[] }> {
  const settingsDoc = await db.collection('notification_settings').doc(userId).get();
  
  if (!settingsDoc.exists) {
    return { canSend: false, settings: null, tokens: [] };
  }
  
  const settings = settingsDoc.data() as UserNotificationSettings;
  
  if (!settings.enabled || settings.fcmTokens.length === 0) {
    return { canSend: false, settings, tokens: [] };
  }
  
  // Check category preference
  const template = NOTIFICATION_TEMPLATES[type];
  if (template && !settings.preferences[template.category]) {
    return { canSend: false, settings, tokens: [] };
  }
  
  // Check quiet hours
  if (settings.quietHours.enabled) {
    const now = new Date();
    // Simple hour check (could be improved with proper timezone handling)
    const currentHour = now.getUTCHours();
    const { startHour, endHour } = settings.quietHours;
    
    if (startHour <= endHour) {
      // Quiet hours don't span midnight
      if (currentHour >= startHour && currentHour < endHour) {
        return { canSend: false, settings, tokens: [] };
      }
    } else {
      // Quiet hours span midnight
      if (currentHour >= startHour || currentHour < endHour) {
        return { canSend: false, settings, tokens: [] };
      }
    }
  }
  
  return { canSend: true, settings, tokens: settings.fcmTokens };
}

/**
 * Build notification message from template and data
 */
function buildNotificationMessage(
  payload: NotificationPayload,
  tokens: string[]
): admin.messaging.MulticastMessage {
  const template = NOTIFICATION_TEMPLATES[payload.type];
  
  // Replace placeholders in title and body
  let title = payload.title || template?.title || 'Apex Citadels';
  let body = payload.body || template?.body || '';
  
  if (payload.data) {
    for (const [key, value] of Object.entries(payload.data)) {
      title = title.replace(`{${key}}`, value);
      body = body.replace(`{${key}}`, value);
    }
  }
  
  const message: admin.messaging.MulticastMessage = {
    tokens,
    notification: {
      title,
      body,
      imageUrl: payload.imageUrl
    },
    data: {
      type: payload.type,
      ...payload.data
    },
    android: {
      priority: payload.priority === NotificationPriority.URGENT ? 'high' : 'normal',
      notification: {
        channelId: payload.channelId || template?.channelId || 'default',
        sound: payload.sound || template?.sound || 'default',
        icon: 'ic_notification',
        color: '#4A90D9'
      },
      ttl: (payload.ttlSeconds || 86400) * 1000
    },
    apns: {
      payload: {
        aps: {
          badge: payload.badge,
          sound: payload.sound || template?.sound || 'default',
          'mutable-content': 1,
          'content-available': 1
        }
      },
      headers: {
        'apns-priority': payload.priority === NotificationPriority.URGENT ? '10' : '5',
        ...(payload.collapseKey && { 'apns-collapse-id': payload.collapseKey })
      }
    }
  };
  
  return message;
}

/**
 * Send notification and handle token cleanup
 */
async function sendNotificationToUser(
  userId: string,
  payload: NotificationPayload
): Promise<{ success: boolean; sentCount: number; failedCount: number }> {
  const { canSend, tokens } = await shouldSendNotification(userId, payload.type);
  
  if (!canSend || tokens.length === 0) {
    return { success: false, sentCount: 0, failedCount: 0 };
  }
  
  const message = buildNotificationMessage(payload, tokens);
  
  try {
    const response = await messaging.sendEachForMulticast(message);
    
    // Clean up invalid tokens
    if (response.failureCount > 0) {
      const invalidTokens: string[] = [];
      response.responses.forEach((resp, idx) => {
        if (!resp.success && resp.error) {
          const errorCode = resp.error.code;
          if (
            errorCode === 'messaging/invalid-registration-token' ||
            errorCode === 'messaging/registration-token-not-registered'
          ) {
            invalidTokens.push(tokens[idx]);
          }
        }
      });
      
      if (invalidTokens.length > 0) {
        await db.collection('notification_settings').doc(userId).update({
          fcmTokens: admin.firestore.FieldValue.arrayRemove(...invalidTokens),
          lastUpdated: admin.firestore.FieldValue.serverTimestamp()
        });
      }
    }
    
    // Log notification
    await db.collection('notification_logs').add({
      userId,
      type: payload.type,
      title: payload.title,
      body: payload.body,
      sentCount: response.successCount,
      failedCount: response.failureCount,
      sentAt: admin.firestore.FieldValue.serverTimestamp()
    });
    
    return {
      success: response.successCount > 0,
      sentCount: response.successCount,
      failedCount: response.failureCount
    };
  } catch (error) {
    console.error('Failed to send notification:', error);
    return { success: false, sentCount: 0, failedCount: tokens.length };
  }
}

/**
 * Send notification to multiple users
 */
async function sendNotificationToUsers(
  userIds: string[],
  payload: NotificationPayload
): Promise<{ totalSent: number; totalFailed: number }> {
  let totalSent = 0;
  let totalFailed = 0;
  
  // Process in batches of 500
  const batchSize = 500;
  for (let i = 0; i < userIds.length; i += batchSize) {
    const batch = userIds.slice(i, i + batchSize);
    const results = await Promise.all(
      batch.map(userId => sendNotificationToUser(userId, payload))
    );
    
    results.forEach(result => {
      totalSent += result.sentCount;
      totalFailed += result.failedCount;
    });
  }
  
  return { totalSent, totalFailed };
}

/**
 * Send notification to a topic
 */
async function sendNotificationToTopic(
  topic: string,
  payload: NotificationPayload
): Promise<boolean> {
  const template = NOTIFICATION_TEMPLATES[payload.type];
  
  let title = payload.title || template?.title || 'Apex Citadels';
  let body = payload.body || template?.body || '';
  
  if (payload.data) {
    for (const [key, value] of Object.entries(payload.data)) {
      title = title.replace(`{${key}}`, value);
      body = body.replace(`{${key}}`, value);
    }
  }
  
  const message: admin.messaging.Message = {
    topic,
    notification: {
      title,
      body,
      imageUrl: payload.imageUrl
    },
    data: {
      type: payload.type,
      ...payload.data
    },
    android: {
      priority: payload.priority === NotificationPriority.URGENT ? 'high' : 'normal',
      notification: {
        channelId: payload.channelId || template?.channelId || 'default',
        sound: payload.sound || template?.sound || 'default'
      }
    },
    apns: {
      payload: {
        aps: {
          sound: payload.sound || template?.sound || 'default'
        }
      }
    }
  };
  
  try {
    await messaging.send(message);
    return true;
  } catch (error) {
    console.error('Failed to send topic notification:', error);
    return false;
  }
}

// ============================================================================
// CLOUD FUNCTIONS - User Management
// ============================================================================

/**
 * Register FCM token for push notifications
 */
export const registerPushToken = onCall(async (request) => {
  const userId = request.auth?.uid;
  if (!userId) {
    throw new HttpsError('unauthenticated', 'Must be logged in');
  }
  
  const { token, platform } = request.data;
  
  if (!token || typeof token !== 'string') {
    throw new HttpsError('invalid-argument', 'Token is required');
  }
  
  const settingsRef = db.collection('notification_settings').doc(userId);
  const settingsDoc = await settingsRef.get();
  
  if (settingsDoc.exists) {
    // Add token if not already present
    await settingsRef.update({
      fcmTokens: admin.firestore.FieldValue.arrayUnion(token),
      lastUpdated: admin.firestore.FieldValue.serverTimestamp(),
      platform: platform || 'unknown'
    });
  } else {
    // Create new settings with defaults
    const defaultSettings: UserNotificationSettings = {
      enabled: true,
      fcmTokens: [token],
      preferences: {
        combat: true,
        alliance: true,
        social: true,
        rewards: true,
        events: true,
        marketing: false
      },
      quietHours: {
        enabled: false,
        startHour: 22,
        endHour: 8,
        timezone: 'UTC'
      },
      lastUpdated: admin.firestore.FieldValue.serverTimestamp() as any
    };
    
    await settingsRef.set({
      ...defaultSettings,
      platform: platform || 'unknown'
    });
  }
  
  // Subscribe to user's personal topic
  await messaging.subscribeToTopic([token], `user_${userId}`);
  
  // Subscribe to general topics
  await messaging.subscribeToTopic([token], 'all_users');
  
  return { success: true };
});

/**
 * Unregister FCM token
 */
export const unregisterPushToken = onCall(async (request) => {
  const userId = request.auth?.uid;
  if (!userId) {
    throw new HttpsError('unauthenticated', 'Must be logged in');
  }
  
  const { token } = request.data;
  
  if (!token) {
    throw new HttpsError('invalid-argument', 'Token is required');
  }
  
  // Remove token from user's settings
  await db.collection('notification_settings').doc(userId).update({
    fcmTokens: admin.firestore.FieldValue.arrayRemove(token),
    lastUpdated: admin.firestore.FieldValue.serverTimestamp()
  });
  
  // Unsubscribe from topics
  try {
    await messaging.unsubscribeFromTopic([token], `user_${userId}`);
    await messaging.unsubscribeFromTopic([token], 'all_users');
  } catch (e) {
    // Token might already be invalid
  }
  
  return { success: true };
});

/**
 * Update notification preferences
 */
export const updateNotificationPreferences = onCall(async (request) => {
  const userId = request.auth?.uid;
  if (!userId) {
    throw new HttpsError('unauthenticated', 'Must be logged in');
  }
  
  const { enabled, preferences, quietHours } = request.data;
  
  const updates: Record<string, any> = {
    lastUpdated: admin.firestore.FieldValue.serverTimestamp()
  };
  
  if (typeof enabled === 'boolean') {
    updates.enabled = enabled;
  }
  
  if (preferences) {
    updates.preferences = preferences;
  }
  
  if (quietHours) {
    updates.quietHours = quietHours;
  }
  
  await db.collection('notification_settings').doc(userId).update(updates);
  
  return { success: true };
});

/**
 * Get notification settings
 */
export const getNotificationSettings = onCall(async (request) => {
  const userId = request.auth?.uid;
  if (!userId) {
    throw new HttpsError('unauthenticated', 'Must be logged in');
  }
  
  const settingsDoc = await db.collection('notification_settings').doc(userId).get();
  
  if (!settingsDoc.exists) {
    return {
      enabled: false,
      hasToken: false,
      preferences: {
        combat: true,
        alliance: true,
        social: true,
        rewards: true,
        events: true,
        marketing: false
      },
      quietHours: {
        enabled: false,
        startHour: 22,
        endHour: 8,
        timezone: 'UTC'
      }
    };
  }
  
  const settings = settingsDoc.data() as UserNotificationSettings;
  
  return {
    enabled: settings.enabled,
    hasToken: settings.fcmTokens.length > 0,
    preferences: settings.preferences,
    quietHours: settings.quietHours
  };
});

// ============================================================================
// CLOUD FUNCTIONS - Send Notifications
// ============================================================================

/**
 * Send a notification to a specific user (admin or system)
 */
export const sendPushNotification = onCall(async (request) => {
  const userId = request.auth?.uid;
  if (!userId) {
    throw new HttpsError('unauthenticated', 'Must be logged in');
  }
  
  // Check if admin
  const userDoc = await db.collection('players').doc(userId).get();
  const userData = userDoc.data();
  if (!userData?.isAdmin) {
    throw new HttpsError('permission-denied', 'Admin access required');
  }
  
  const { targetUserId, type, title, body, data, imageUrl } = request.data;
  
  if (!targetUserId || !type) {
    throw new HttpsError('invalid-argument', 'targetUserId and type are required');
  }
  
  const payload: NotificationPayload = {
    type,
    title: title || NOTIFICATION_TEMPLATES[type as NotificationType]?.title || 'Notification',
    body: body || NOTIFICATION_TEMPLATES[type as NotificationType]?.body || '',
    data,
    imageUrl
  };
  
  const result = await sendNotificationToUser(targetUserId, payload);
  
  return result;
});

/**
 * Send notification to all users (admin broadcast)
 */
export const broadcastNotification = onCall(async (request) => {
  const userId = request.auth?.uid;
  if (!userId) {
    throw new HttpsError('unauthenticated', 'Must be logged in');
  }
  
  // Check if admin
  const userDoc = await db.collection('players').doc(userId).get();
  const userData = userDoc.data();
  if (!userData?.isAdmin) {
    throw new HttpsError('permission-denied', 'Admin access required');
  }
  
  const { type, title, body, data, imageUrl, topic } = request.data;
  
  if (!type) {
    throw new HttpsError('invalid-argument', 'type is required');
  }
  
  const payload: NotificationPayload = {
    type,
    title: title || NOTIFICATION_TEMPLATES[type as NotificationType]?.title || 'Notification',
    body: body || NOTIFICATION_TEMPLATES[type as NotificationType]?.body || '',
    data,
    imageUrl
  };
  
  // Send to topic (all_users or specific)
  const targetTopic = topic || 'all_users';
  const success = await sendNotificationToTopic(targetTopic, payload);
  
  // Log broadcast
  await db.collection('broadcast_logs').add({
    type,
    title: payload.title,
    body: payload.body,
    topic: targetTopic,
    sentBy: userId,
    sentAt: admin.firestore.FieldValue.serverTimestamp(),
    success
  });
  
  return { success };
});

/**
 * Subscribe to an alliance topic
 */
export const subscribeToAllianceTopic = onCall(async (request) => {
  const userId = request.auth?.uid;
  if (!userId) {
    throw new HttpsError('unauthenticated', 'Must be logged in');
  }
  
  const { allianceId } = request.data;
  
  if (!allianceId) {
    throw new HttpsError('invalid-argument', 'allianceId is required');
  }
  
  // Get user's tokens
  const settingsDoc = await db.collection('notification_settings').doc(userId).get();
  if (!settingsDoc.exists) {
    return { success: false, message: 'No notification settings' };
  }
  
  const settings = settingsDoc.data() as UserNotificationSettings;
  if (settings.fcmTokens.length === 0) {
    return { success: false, message: 'No registered tokens' };
  }
  
  await messaging.subscribeToTopic(settings.fcmTokens, `alliance_${allianceId}`);
  
  return { success: true };
});

/**
 * Unsubscribe from alliance topic
 */
export const unsubscribeFromAllianceTopic = onCall(async (request) => {
  const userId = request.auth?.uid;
  if (!userId) {
    throw new HttpsError('unauthenticated', 'Must be logged in');
  }
  
  const { allianceId } = request.data;
  
  if (!allianceId) {
    throw new HttpsError('invalid-argument', 'allianceId is required');
  }
  
  const settingsDoc = await db.collection('notification_settings').doc(userId).get();
  if (!settingsDoc.exists) {
    return { success: false };
  }
  
  const settings = settingsDoc.data() as UserNotificationSettings;
  
  try {
    await messaging.unsubscribeFromTopic(settings.fcmTokens, `alliance_${allianceId}`);
  } catch (e) {
    // Tokens might be invalid
  }
  
  return { success: true };
});

// ============================================================================
// FIRESTORE TRIGGERS - Auto Notifications
// ============================================================================

/**
 * Send notification when territory is attacked
 */
export const onTerritoryAttacked = onDocumentCreated('attacks/{attackId}', async (event) => {
  const attack = event.data?.data();
  if (!attack) return;
  
  const { defenderId, attackerId, territoryId } = attack;
  
  // Get attacker name
  const attackerDoc = await db.collection('players').doc(attackerId).get();
  const attackerName = attackerDoc.data()?.displayName || 'Unknown';
  
  // Get territory info
  const territoryDoc = await db.collection('territories').doc(territoryId).get();
  const territoryData = territoryDoc.data();
  const location = territoryData?.name || 'your territory';
  
  await sendNotificationToUser(defenderId, {
    type: NotificationType.TERRITORY_ATTACKED,
    title: '⚔️ Your Territory is Under Attack!',
    body: `${attackerName} is attacking your citadel at ${location}!`,
    data: {
      attackerName,
      location,
      attackId: event.params.attackId,
      territoryId
    },
    priority: NotificationPriority.URGENT
  });
});

/**
 * Send notification when friend request is received
 */
export const onFriendRequestCreated = onDocumentCreated('friend_requests/{requestId}', async (event) => {
  const request = event.data?.data();
  if (!request) return;
  
  const { recipientId, senderId } = request;
  
  // Get sender name
  const senderDoc = await db.collection('players').doc(senderId).get();
  const senderName = senderDoc.data()?.displayName || 'Someone';
  
  await sendNotificationToUser(recipientId, {
    type: NotificationType.FRIEND_REQUEST,
    title: '👋 Friend Request',
    body: `${senderName} wants to be your friend!`,
    data: {
      senderName,
      senderId,
      requestId: event.params.requestId
    }
  });
});

/**
 * Send notification when alliance invite is received
 */
export const onAllianceInviteCreated = onDocumentCreated('alliance_invitations/{inviteId}', async (event) => {
  const invite = event.data?.data();
  if (!invite) return;
  
  const { userId, allianceId } = invite;
  
  // Get alliance name
  const allianceDoc = await db.collection('alliances').doc(allianceId).get();
  const allianceName = allianceDoc.data()?.name || 'An alliance';
  
  await sendNotificationToUser(userId, {
    type: NotificationType.ALLIANCE_INVITE,
    title: '📨 Alliance Invitation',
    body: `${allianceName} has invited you to join their ranks!`,
    data: {
      allianceName,
      allianceId,
      inviteId: event.params.inviteId
    }
  });
});

/**
 * Send notification when alliance war starts
 */
export const onAllianceWarStarted = onDocumentUpdated('alliance_wars/{warId}', async (event) => {
  const before = event.data?.before.data();
  const after = event.data?.after.data();
  
  if (!before || !after) return;
  
  // Check if war just started
  if (before.status !== 'active' && after.status === 'active') {
    const { alliance1Id, alliance2Id } = after;
    
    // Get alliance names
    const [alliance1Doc, alliance2Doc] = await Promise.all([
      db.collection('alliances').doc(alliance1Id).get(),
      db.collection('alliances').doc(alliance2Id).get()
    ]);
    
    const alliance1Name = alliance1Doc.data()?.name || 'Alliance 1';
    const alliance2Name = alliance2Doc.data()?.name || 'Alliance 2';
    
    // Notify both alliances via topics
    await sendNotificationToTopic(`alliance_${alliance1Id}`, {
      type: NotificationType.ALLIANCE_WAR_STARTED,
      title: '🏰 Alliance War Begins!',
      body: `${alliance1Name} vs ${alliance2Name} - Rally your forces!`,
      data: {
        allianceName: alliance1Name,
        enemyAlliance: alliance2Name,
        warId: event.params.warId
      },
      priority: NotificationPriority.URGENT
    });
    
    await sendNotificationToTopic(`alliance_${alliance2Id}`, {
      type: NotificationType.ALLIANCE_WAR_STARTED,
      title: '🏰 Alliance War Begins!',
      body: `${alliance2Name} vs ${alliance1Name} - Rally your forces!`,
      data: {
        allianceName: alliance2Name,
        enemyAlliance: alliance1Name,
        warId: event.params.warId
      },
      priority: NotificationPriority.URGENT
    });
  }
});

/**
 * Send notification when user levels up
 */
export const onUserLevelUp = onDocumentUpdated('players/{userId}', async (event) => {
  const before = event.data?.before.data();
  const after = event.data?.after.data();
  
  if (!before || !after) return;
  
  const oldLevel = before.stats?.level || 1;
  const newLevel = after.stats?.level || 1;
  
  if (newLevel > oldLevel) {
    await sendNotificationToUser(event.params.userId, {
      type: NotificationType.LEVEL_UP,
      title: '⭐ Level Up!',
      body: `Congratulations! You reached Level ${newLevel}!`,
      data: {
        level: newLevel.toString(),
        oldLevel: oldLevel.toString()
      }
    });
  }
});

// ============================================================================
// SCHEDULED FUNCTIONS
// ============================================================================

/**
 * Send daily reward reminder (runs at 6 PM UTC)
 */
export const sendDailyRewardReminder = onSchedule('0 18 * * *', async () => {
  // Find users who haven't claimed today's reward
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  
  const usersSnapshot = await db.collection('players')
    .where('dailyReward.lastClaimDate', '<', today)
    .limit(10000)
    .get();
  
  if (usersSnapshot.empty) {
    console.log('No users need daily reward reminder');
    return;
  }
  
  const userIds = usersSnapshot.docs.map(doc => doc.id);
  
  // Get day number for each user
  const notifications = userIds.map(async (userId) => {
    const userDoc = usersSnapshot.docs.find(d => d.id === userId);
    const userData = userDoc?.data();
    const currentStreak = userData?.dailyReward?.currentStreak || 0;
    const day = (currentStreak % 7) + 1;
    
    return sendNotificationToUser(userId, {
      type: NotificationType.DAILY_REWARD_AVAILABLE,
      title: '🎁 Daily Reward Ready!',
      body: `Claim your Day ${day} reward before it resets!`,
      data: { day: day.toString() }
    });
  });
  
  await Promise.all(notifications);
  
  console.log(`Sent daily reward reminders to ${userIds.length} users`);
});

/**
 * Send welcome back notification to inactive users (runs daily at 10 AM UTC)
 */
export const sendWelcomeBackNotification = onSchedule('0 10 * * *', async () => {
  const threeDaysAgo = new Date();
  threeDaysAgo.setDate(threeDaysAgo.getDate() - 3);
  
  const sevenDaysAgo = new Date();
  sevenDaysAgo.setDate(sevenDaysAgo.getDate() - 7);
  
  // Find users inactive for 3-7 days
  const usersSnapshot = await db.collection('players')
    .where('lastLoginAt', '<=', threeDaysAgo)
    .where('lastLoginAt', '>=', sevenDaysAgo)
    .limit(5000)
    .get();
  
  if (usersSnapshot.empty) {
    console.log('No inactive users to notify');
    return;
  }
  
  for (const doc of usersSnapshot.docs) {
    const userId = doc.id;
    const userData = doc.data();
    const lastLogin = userData.lastLoginAt?.toDate();
    
    if (!lastLogin) continue;
    
    const daysAway = Math.floor((Date.now() - lastLogin.getTime()) / (1000 * 60 * 60 * 24));
    
    await sendNotificationToUser(userId, {
      type: NotificationType.WELCOME_BACK,
      title: '👋 Welcome Back, Commander!',
      body: `Your empire awaits! ${daysAway} days of rewards ready!`,
      data: { days: daysAway.toString() }
    });
  }
  
  console.log(`Sent welcome back notifications to ${usersSnapshot.size} users`);
});

/**
 * Send world event reminders (runs every hour)
 */
export const sendWorldEventReminders = onSchedule('0 * * * *', async () => {
  const now = new Date();
  const oneHourFromNow = new Date(now.getTime() + 60 * 60 * 1000);
  
  // Find events starting in the next hour
  const eventsSnapshot = await db.collection('world_events')
    .where('status', '==', 'scheduled')
    .where('startTime', '>=', now)
    .where('startTime', '<=', oneHourFromNow)
    .get();
  
  for (const doc of eventsSnapshot.docs) {
    const event = doc.data();
    const startTime = event.startTime.toDate();
    const minutesUntilStart = Math.round((startTime.getTime() - now.getTime()) / (1000 * 60));
    
    let timeString = '';
    if (minutesUntilStart <= 5) {
      timeString = 'now';
    } else if (minutesUntilStart <= 30) {
      timeString = `${minutesUntilStart} minutes`;
    } else {
      timeString = '1 hour';
    }
    
    await sendNotificationToTopic('all_users', {
      type: NotificationType.WORLD_EVENT_STARTING,
      title: '🌍 World Event Starting!',
      body: `${event.name} begins in ${timeString}! Prepare your forces!`,
      data: {
        eventName: event.name,
        time: timeString,
        eventId: doc.id
      },
      priority: NotificationPriority.HIGH
    });
  }
});

/**
 * Send season ending reminders (runs daily at 12 PM UTC)
 */
export const sendSeasonEndingReminder = onSchedule('0 12 * * *', async () => {
  // Get current season
  const seasonsSnapshot = await db.collection('seasons')
    .where('status', '==', 'active')
    .limit(1)
    .get();
  
  if (seasonsSnapshot.empty) return;
  
  const season = seasonsSnapshot.docs[0].data();
  const endDate = season.endDate.toDate();
  const now = new Date();
  const daysRemaining = Math.ceil((endDate.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
  
  // Send reminders at 7 days, 3 days, and 1 day
  if ([7, 3, 1].includes(daysRemaining)) {
    await sendNotificationToTopic('all_users', {
      type: NotificationType.SEASON_ENDING_SOON,
      title: '⏰ Season Ending Soon!',
      body: `Only ${daysRemaining} day${daysRemaining > 1 ? 's' : ''} left! Don't miss your rewards!`,
      data: {
        days: daysRemaining.toString(),
        seasonId: seasonsSnapshot.docs[0].id
      },
      priority: NotificationPriority.HIGH
    });
  }
});

// ============================================================================
// WEBHOOK ENDPOINT (for external integrations)
// ============================================================================

/**
 * Webhook endpoint for external notification triggers
 */
export const notificationWebhook = onRequest(async (req, res) => {
  // Verify webhook secret
  const webhookSecret = process.env.NOTIFICATION_WEBHOOK_SECRET;
  const providedSecret = req.headers['x-webhook-secret'];
  
  if (!webhookSecret || providedSecret !== webhookSecret) {
    res.status(401).json({ error: 'Unauthorized' });
    return;
  }
  
  if (req.method !== 'POST') {
    res.status(405).json({ error: 'Method not allowed' });
    return;
  }
  
  const { action, data } = req.body;
  
  try {
    switch (action) {
      case 'broadcast':
        await sendNotificationToTopic(data.topic || 'all_users', data.payload);
        break;
        
      case 'send_to_user':
        await sendNotificationToUser(data.userId, data.payload);
        break;
        
      case 'send_to_users':
        await sendNotificationToUsers(data.userIds, data.payload);
        break;
        
      default:
        res.status(400).json({ error: 'Unknown action' });
        return;
    }
    
    res.json({ success: true });
  } catch (error) {
    console.error('Webhook error:', error);
    res.status(500).json({ error: 'Internal error' });
  }
});
