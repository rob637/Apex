/**
 * Apex Citadels - Notification System
 * 
 * Handles:
 * - In-app notifications storage
 * - Push notification sending (FCM)
 * - Notification preferences
 */

import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';
import { Notification, NotificationType, User } from './types';

const db = admin.firestore();

// ============================================================================
// Send Notification
// ============================================================================

interface NotificationPayload {
  type: NotificationType;
  title: string;
  body: string;
  data?: Record<string, string>;
}

export async function sendNotification(
  userId: string,
  payload: NotificationPayload
): Promise<void> {
  // Store in-app notification
  const notificationId = db.collection('notifications').doc().id;
  
  const notification: Notification = {
    id: notificationId,
    userId,
    type: payload.type,
    title: payload.title,
    body: payload.body,
    data: payload.data,
    read: false,
    createdAt: admin.firestore.Timestamp.now()
  };

  await db.collection('notifications').doc(notificationId).set(notification);

  // Get user for push preferences and FCM token
  const userDoc = await db.collection('users').doc(userId).get();
  if (!userDoc.exists) return;

  const user = userDoc.data() as User;

  // Check if push notifications are enabled
  if (!user.settings?.pushNotifications) return;

  // Get FCM token
  const tokenDoc = await db.collection('users').doc(userId).collection('tokens').doc('fcm').get();
  if (!tokenDoc.exists) return;

  const fcmToken = tokenDoc.data()?.token;
  if (!fcmToken) return;

  // Send push notification
  try {
    await admin.messaging().send({
      token: fcmToken,
      notification: {
        title: payload.title,
        body: payload.body
      },
      data: {
        type: payload.type,
        notificationId,
        ...payload.data
      },
      android: {
        priority: 'high',
        notification: {
          channelId: getChannelId(payload.type),
          icon: 'ic_notification',
          color: '#6366F1'
        }
      },
      apns: {
        payload: {
          aps: {
            badge: 1,
            sound: 'default'
          }
        }
      }
    });

    functions.logger.info(`Push sent to ${userId}: ${payload.title}`);
  } catch (error) {
    functions.logger.error(`Push failed to ${userId}:`, error);
    
    // If token is invalid, remove it
    if ((error as { code?: string }).code === 'messaging/invalid-registration-token' ||
        (error as { code?: string }).code === 'messaging/registration-token-not-registered') {
      await db.collection('users').doc(userId).collection('tokens').doc('fcm').delete();
    }
  }
}

function getChannelId(type: NotificationType): string {
  switch (type) {
    case 'territory_attacked':
    case 'territory_conquered':
    case 'territory_defended':
      return 'combat';
    case 'alliance_invite':
    case 'alliance_war_started':
    case 'alliance_war_ended':
      return 'alliance';
    case 'achievement_unlocked':
    case 'level_up':
      return 'progression';
    case 'daily_reward_available':
      return 'rewards';
    default:
      return 'general';
  }
}

// ============================================================================
// Get User Notifications
// ============================================================================

export const getNotifications = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const { limit = 50, unreadOnly = false } = data as {
    limit?: number;
    unreadOnly?: boolean;
  };

  const userId = context.auth.uid;

  let query = db.collection('notifications')
    .where('userId', '==', userId)
    .orderBy('createdAt', 'desc')
    .limit(limit);

  if (unreadOnly) {
    query = query.where('read', '==', false);
  }

  const snapshot = await query.get();
  const notifications = snapshot.docs.map(doc => doc.data() as Notification);

  // Get unread count
  const unreadSnapshot = await db.collection('notifications')
    .where('userId', '==', userId)
    .where('read', '==', false)
    .count()
    .get();

  return {
    notifications,
    unreadCount: unreadSnapshot.data().count
  };
});

// ============================================================================
// Mark Notification as Read
// ============================================================================

export const markNotificationRead = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const { notificationId, markAll = false } = data as {
    notificationId?: string;
    markAll?: boolean;
  };

  const userId = context.auth.uid;

  if (markAll) {
    // Mark all as read
    const unreadSnapshot = await db.collection('notifications')
      .where('userId', '==', userId)
      .where('read', '==', false)
      .get();

    const batch = db.batch();
    for (const doc of unreadSnapshot.docs) {
      batch.update(doc.ref, { read: true });
    }
    await batch.commit();

    return { success: true, count: unreadSnapshot.size };
  } else if (notificationId) {
    // Mark single notification
    const notifDoc = await db.collection('notifications').doc(notificationId).get();
    
    if (!notifDoc.exists) {
      throw new functions.https.HttpsError('not-found', 'Notification not found');
    }

    const notif = notifDoc.data() as Notification;
    if (notif.userId !== userId) {
      throw new functions.https.HttpsError('permission-denied', 'Not your notification');
    }

    await db.collection('notifications').doc(notificationId).update({ read: true });

    return { success: true, count: 1 };
  }

  throw new functions.https.HttpsError('invalid-argument', 'Provide notificationId or markAll');
});

// ============================================================================
// Delete Old Notifications (Scheduled)
// ============================================================================

export const cleanupOldNotifications = functions.pubsub
  .schedule('every 24 hours')
  .onRun(async () => {
    const thirtyDaysAgo = new Date();
    thirtyDaysAgo.setDate(thirtyDaysAgo.getDate() - 30);

    const oldNotifications = await db.collection('notifications')
      .where('createdAt', '<', admin.firestore.Timestamp.fromDate(thirtyDaysAgo))
      .limit(500)
      .get();

    if (oldNotifications.empty) {
      functions.logger.info('No old notifications to clean up');
      return;
    }

    const batch = db.batch();
    for (const doc of oldNotifications.docs) {
      batch.delete(doc.ref);
    }
    await batch.commit();

    functions.logger.info(`Deleted ${oldNotifications.size} old notifications`);
  });

// ============================================================================
// Register FCM Token
// ============================================================================

export const registerFcmToken = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const { token } = data as { token: string };
  const userId = context.auth.uid;

  if (!token) {
    throw new functions.https.HttpsError('invalid-argument', 'Token required');
  }

  await db.collection('users').doc(userId).collection('tokens').doc('fcm').set({
    token,
    updatedAt: admin.firestore.Timestamp.now(),
    platform: data.platform || 'unknown'
  });

  return { success: true };
});

// ============================================================================
// Update Notification Settings
// ============================================================================

export const updateNotificationSettings = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const { pushNotifications } = data as { pushNotifications: boolean };
  const userId = context.auth.uid;

  await db.collection('users').doc(userId).update({
    'settings.pushNotifications': pushNotifications
  });

  return { success: true };
});

// ============================================================================
// Send Scheduled Notifications (Daily Reward Reminder)
// ============================================================================

export const sendDailyRewardReminder = functions.pubsub
  .schedule('every day 12:00')
  .timeZone('America/New_York')
  .onRun(async () => {
    const today = new Date().toISOString().split('T')[0];

    // Find users who haven't claimed today
    const usersSnapshot = await db.collection('users')
      .where('dailyReward.lastClaimDate', '!=', today)
      .limit(1000)
      .get();

    let sentCount = 0;

    for (const userDoc of usersSnapshot.docs) {
      const user = userDoc.data() as User;
      
      // Check if they have push enabled
      if (!user.settings?.pushNotifications) continue;

      try {
        await sendNotification(userDoc.id, {
          type: 'daily_reward_available',
          title: 'Daily Reward Waiting!',
          body: `Day ${(user.dailyReward?.currentDay || 0) % 7 + 1} reward is ready to claim!`,
          data: {}
        });
        sentCount++;
      } catch (error) {
        functions.logger.error(`Failed to send reminder to ${userDoc.id}:`, error);
      }
    }

    functions.logger.info(`Sent ${sentCount} daily reward reminders`);
  });
