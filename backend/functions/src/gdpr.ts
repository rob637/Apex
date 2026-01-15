/**
 * Apex Citadels - GDPR & Privacy Compliance System
 * 
 * Implements user rights under GDPR, CCPA, and similar regulations:
 * - Right to Access (data export)
 * - Right to Erasure (data deletion)
 * - Consent Management
 * - Data Portability
 * - Privacy Settings
 */

import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';

const db = admin.firestore();
const storage = admin.storage();
const auth = admin.auth();

// ============================================================================
// Types
// ============================================================================

export interface DataExportRequest {
  id: string;
  userId: string;
  status: 'pending' | 'processing' | 'completed' | 'failed' | 'expired';
  requestedAt: admin.firestore.Timestamp;
  completedAt?: admin.firestore.Timestamp;
  downloadUrl?: string;
  expiresAt?: admin.firestore.Timestamp;
  format: 'json' | 'csv';
  errorMessage?: string;
}

export interface DataDeletionRequest {
  id: string;
  userId: string;
  status: 'pending' | 'processing' | 'completed' | 'failed' | 'cancelled';
  requestedAt: admin.firestore.Timestamp;
  scheduledDeletionAt: admin.firestore.Timestamp;
  completedAt?: admin.firestore.Timestamp;
  collectionsDeleted: string[];
  errorMessage?: string;
  cancellationReason?: string;
}

export interface ConsentRecord {
  userId: string;
  consents: {
    essential: boolean; // Always true - required for service
    analytics: boolean;
    marketing: boolean;
    personalization: boolean;
    thirdPartySharing: boolean;
  };
  ipAddress?: string;
  userAgent?: string;
  consentVersion: string;
  createdAt: admin.firestore.Timestamp;
  updatedAt: admin.firestore.Timestamp;
  history: ConsentChange[];
}

export interface ConsentChange {
  timestamp: admin.firestore.Timestamp;
  changes: Record<string, { from: boolean; to: boolean }>;
  source: 'user' | 'system' | 'admin';
}

export interface PrivacySettings {
  userId: string;
  profileVisibility: 'public' | 'friends' | 'private';
  showOnLeaderboards: boolean;
  allowFriendRequests: boolean;
  showOnlineStatus: boolean;
  showLastActive: boolean;
  allowAllianceInvites: boolean;
  shareLocationWithAlliance: boolean;
  updatedAt: admin.firestore.Timestamp;
}

export interface UserDataExport {
  exportedAt: string;
  userId: string;
  format: string;
  sections: {
    profile: unknown;
    gameProgress: unknown;
    territories: unknown[];
    buildings: unknown[];
    resources: unknown;
    alliance: unknown;
    friends: unknown[];
    chat: unknown[];
    purchases: unknown[];
    achievements: unknown[];
    seasonPass: unknown;
    notifications: unknown;
    settings: unknown;
    analytics: unknown[];
    loginHistory: unknown[];
  };
}

// ============================================================================
// Constants
// ============================================================================

const CURRENT_CONSENT_VERSION = '1.0.0';
const DATA_EXPORT_EXPIRY_DAYS = 7;
const DELETION_GRACE_PERIOD_DAYS = 30; // 30 days before actual deletion

// Collections that contain user data
const USER_DATA_COLLECTIONS = [
  'users',
  'user_profiles',
  'territories',
  'buildings',
  'alliances',
  'alliance_members',
  'friends',
  'friend_requests',
  'chat_messages',
  'purchases',
  'iap_receipts',
  'achievements',
  'season_progress',
  'daily_rewards',
  'notifications',
  'push_tokens',
  'analytics_events',
  'login_history',
  'location_history',
  'suspicious_activities',
  'world_event_participation',
  'referrals',
  'user_settings',
  'consent_records',
  'data_requests'
];

// ============================================================================
// Request Data Export
// ============================================================================

/**
 * Request a full export of user's personal data
 * GDPR Article 15 - Right of Access
 * GDPR Article 20 - Right to Data Portability
 */
export const requestDataExport = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  const userId = context.auth.uid;
  const format = data?.format === 'csv' ? 'csv' : 'json';

  // Check for existing pending request
  const existingRequest = await db.collection('data_export_requests')
    .where('userId', '==', userId)
    .where('status', 'in', ['pending', 'processing'])
    .limit(1)
    .get();

  if (!existingRequest.empty) {
    const existing = existingRequest.docs[0].data() as DataExportRequest;
    return {
      success: false,
      message: 'You already have a pending export request',
      requestId: existingRequest.docs[0].id,
      status: existing.status
    };
  }

  // Create new request
  const requestRef = db.collection('data_export_requests').doc();
  const request: DataExportRequest = {
    id: requestRef.id,
    userId,
    status: 'pending',
    requestedAt: admin.firestore.Timestamp.now(),
    format
  };

  await requestRef.set(request);

  // Trigger async processing
  await db.collection('data_export_queue').doc(requestRef.id).set({
    requestId: requestRef.id,
    userId,
    createdAt: admin.firestore.Timestamp.now()
  });

  return {
    success: true,
    message: 'Data export request submitted. You will be notified when ready.',
    requestId: requestRef.id,
    estimatedTime: '24-48 hours'
  };
});

/**
 * Process data export request (triggered by queue)
 */
export const processDataExport = functions.firestore
  .document('data_export_queue/{requestId}')
  .onCreate(async (snap) => {
    const { requestId, userId } = snap.data();
    const requestRef = db.collection('data_export_requests').doc(requestId);

    try {
      // Update status to processing
      await requestRef.update({ status: 'processing' });

      // Collect all user data
      const exportData = await collectUserData(userId);

      // Generate export file
      const request = (await requestRef.get()).data() as DataExportRequest;
      const fileContent = request.format === 'json' 
        ? JSON.stringify(exportData, null, 2)
        : convertToCSV(exportData);

      // Upload to Cloud Storage
      const bucket = storage.bucket();
      const fileName = `exports/${userId}/${requestId}.${request.format}`;
      const file = bucket.file(fileName);

      await file.save(fileContent, {
        contentType: request.format === 'json' ? 'application/json' : 'text/csv',
        metadata: {
          userId,
          requestId,
          exportedAt: new Date().toISOString()
        }
      });

      // Generate signed download URL (valid for 7 days)
      const expiresAt = new Date();
      expiresAt.setDate(expiresAt.getDate() + DATA_EXPORT_EXPIRY_DAYS);

      const [downloadUrl] = await file.getSignedUrl({
        action: 'read',
        expires: expiresAt
      });

      // Update request with completion info
      await requestRef.update({
        status: 'completed',
        completedAt: admin.firestore.Timestamp.now(),
        downloadUrl,
        expiresAt: admin.firestore.Timestamp.fromDate(expiresAt)
      });

      // Send notification to user
      await sendExportReadyNotification(userId, downloadUrl);

      // Clean up queue item
      await snap.ref.delete();

    } catch (error) {
      console.error('Data export failed:', error);
      await requestRef.update({
        status: 'failed',
        errorMessage: error instanceof Error ? error.message : 'Unknown error'
      });
    }
  });

/**
 * Get status of data export request
 */
export const getDataExportStatus = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  const userId = context.auth.uid;
  const requestId = data?.requestId;

  let query;
  if (requestId) {
    const doc = await db.collection('data_export_requests').doc(requestId).get();
    if (!doc.exists || (doc.data() as DataExportRequest).userId !== userId) {
      throw new functions.https.HttpsError('not-found', 'Export request not found');
    }
    return { request: doc.data() };
  }

  // Get all user's requests
  query = await db.collection('data_export_requests')
    .where('userId', '==', userId)
    .orderBy('requestedAt', 'desc')
    .limit(10)
    .get();

  return {
    requests: query.docs.map(doc => doc.data())
  };
});

// ============================================================================
// Request Data Deletion
// ============================================================================

/**
 * Request deletion of all user data
 * GDPR Article 17 - Right to Erasure ("Right to be Forgotten")
 */
export const requestDataDeletion = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  const userId = context.auth.uid;
  const confirmation = data?.confirmation;

  // Require explicit confirmation
  if (confirmation !== 'DELETE_MY_DATA') {
    throw new functions.https.HttpsError(
      'invalid-argument',
      'Must provide confirmation string: DELETE_MY_DATA'
    );
  }

  // Check for existing pending request
  const existingRequest = await db.collection('data_deletion_requests')
    .where('userId', '==', userId)
    .where('status', 'in', ['pending', 'processing'])
    .limit(1)
    .get();

  if (!existingRequest.empty) {
    const existing = existingRequest.docs[0].data() as DataDeletionRequest;
    return {
      success: false,
      message: 'You already have a pending deletion request',
      requestId: existingRequest.docs[0].id,
      scheduledDeletionAt: existing.scheduledDeletionAt.toDate().toISOString()
    };
  }

  // Schedule deletion after grace period
  const scheduledDeletionAt = new Date();
  scheduledDeletionAt.setDate(scheduledDeletionAt.getDate() + DELETION_GRACE_PERIOD_DAYS);

  const requestRef = db.collection('data_deletion_requests').doc();
  const request: DataDeletionRequest = {
    id: requestRef.id,
    userId,
    status: 'pending',
    requestedAt: admin.firestore.Timestamp.now(),
    scheduledDeletionAt: admin.firestore.Timestamp.fromDate(scheduledDeletionAt),
    collectionsDeleted: []
  };

  await requestRef.set(request);

  // Mark user account as pending deletion
  await db.collection('users').doc(userId).update({
    pendingDeletion: true,
    deletionRequestId: requestRef.id,
    deletionScheduledAt: admin.firestore.Timestamp.fromDate(scheduledDeletionAt)
  });

  // Send confirmation email/notification
  await sendDeletionScheduledNotification(userId, scheduledDeletionAt);

  return {
    success: true,
    message: `Your data is scheduled for deletion on ${scheduledDeletionAt.toISOString().split('T')[0]}. You can cancel this request within the grace period.`,
    requestId: requestRef.id,
    scheduledDeletionAt: scheduledDeletionAt.toISOString(),
    gracePeriodDays: DELETION_GRACE_PERIOD_DAYS
  };
});

/**
 * Cancel a pending data deletion request
 */
export const cancelDataDeletion = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  const userId = context.auth.uid;
  const requestId = data?.requestId;

  if (!requestId) {
    throw new functions.https.HttpsError('invalid-argument', 'Request ID required');
  }

  const requestRef = db.collection('data_deletion_requests').doc(requestId);
  const requestDoc = await requestRef.get();

  if (!requestDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Deletion request not found');
  }

  const request = requestDoc.data() as DataDeletionRequest;

  if (request.userId !== userId) {
    throw new functions.https.HttpsError('permission-denied', 'Not your request');
  }

  if (request.status !== 'pending') {
    throw new functions.https.HttpsError(
      'failed-precondition',
      `Cannot cancel request in ${request.status} status`
    );
  }

  // Cancel the request
  await requestRef.update({
    status: 'cancelled',
    cancellationReason: 'Cancelled by user'
  });

  // Remove pending deletion flag from user
  await db.collection('users').doc(userId).update({
    pendingDeletion: admin.firestore.FieldValue.delete(),
    deletionRequestId: admin.firestore.FieldValue.delete(),
    deletionScheduledAt: admin.firestore.FieldValue.delete()
  });

  return {
    success: true,
    message: 'Data deletion request cancelled successfully'
  };
});

/**
 * Process scheduled deletions (run daily)
 */
export const processScheduledDeletions = functions.pubsub
  .schedule('0 3 * * *') // Run at 3 AM UTC daily
  .onRun(async () => {
    const now = admin.firestore.Timestamp.now();

    // Find requests ready for deletion
    const pendingDeletions = await db.collection('data_deletion_requests')
      .where('status', '==', 'pending')
      .where('scheduledDeletionAt', '<=', now)
      .get();

    for (const doc of pendingDeletions.docs) {
      const request = doc.data() as DataDeletionRequest;
      await executeDataDeletion(request.id, request.userId);
    }

    console.log(`Processed ${pendingDeletions.size} deletion requests`);
  });

/**
 * Execute the actual data deletion
 */
async function executeDataDeletion(requestId: string, userId: string): Promise<void> {
  const requestRef = db.collection('data_deletion_requests').doc(requestId);

  try {
    await requestRef.update({ status: 'processing' });

    const deletedCollections: string[] = [];

    // Delete data from each collection
    for (const collection of USER_DATA_COLLECTIONS) {
      try {
        await deleteUserDataFromCollection(userId, collection);
        deletedCollections.push(collection);
      } catch (error) {
        console.error(`Error deleting from ${collection}:`, error);
      }
    }

    // Delete files from Cloud Storage
    await deleteUserFiles(userId);

    // Delete Firebase Auth account
    try {
      await auth.deleteUser(userId);
    } catch (error) {
      console.error('Error deleting auth account:', error);
    }

    // Update request status
    await requestRef.update({
      status: 'completed',
      completedAt: admin.firestore.Timestamp.now(),
      collectionsDeleted: deletedCollections
    });

    console.log(`Successfully deleted all data for user ${userId}`);

  } catch (error) {
    console.error('Data deletion failed:', error);
    await requestRef.update({
      status: 'failed',
      errorMessage: error instanceof Error ? error.message : 'Unknown error'
    });
  }
}

async function deleteUserDataFromCollection(userId: string, collection: string): Promise<void> {
  const batch = db.batch();
  let deleted = 0;

  // Try different query patterns based on collection structure
  const queries = [
    db.collection(collection).where('userId', '==', userId),
    db.collection(collection).where('ownerId', '==', userId),
    db.collection(collection).where('playerId', '==', userId),
    db.collection(collection).where('senderId', '==', userId),
    db.collection(collection).where('recipientId', '==', userId)
  ];

  for (const query of queries) {
    try {
      const snapshot = await query.limit(500).get();
      snapshot.forEach(doc => {
        batch.delete(doc.ref);
        deleted++;
      });
    } catch {
      // Query might fail if field doesn't exist - that's okay
    }
  }

  // Also try direct document ID match
  try {
    const directDoc = db.collection(collection).doc(userId);
    const docSnap = await directDoc.get();
    if (docSnap.exists) {
      batch.delete(directDoc);
      deleted++;
    }
  } catch {
    // Ignore errors
  }

  if (deleted > 0) {
    await batch.commit();
  }
}

async function deleteUserFiles(userId: string): Promise<void> {
  const bucket = storage.bucket();
  const [files] = await bucket.getFiles({
    prefix: `users/${userId}/`
  });

  const [exportFiles] = await bucket.getFiles({
    prefix: `exports/${userId}/`
  });

  const allFiles = [...files, ...exportFiles];

  for (const file of allFiles) {
    try {
      await file.delete();
    } catch (error) {
      console.error(`Error deleting file ${file.name}:`, error);
    }
  }
}

// ============================================================================
// Consent Management
// ============================================================================

/**
 * Record or update user consent preferences
 */
export const updateConsent = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  const userId = context.auth.uid;
  const consents = data?.consents;

  if (!consents || typeof consents !== 'object') {
    throw new functions.https.HttpsError('invalid-argument', 'Consents object required');
  }

  const consentRef = db.collection('consent_records').doc(userId);
  const existingDoc = await consentRef.get();
  const existing = existingDoc.exists ? existingDoc.data() as ConsentRecord : null;

  const newConsents = {
    essential: true, // Always required
    analytics: Boolean(consents.analytics),
    marketing: Boolean(consents.marketing),
    personalization: Boolean(consents.personalization),
    thirdPartySharing: Boolean(consents.thirdPartySharing)
  };

  // Track changes
  const changes: Record<string, { from: boolean; to: boolean }> = {};
  if (existing) {
    for (const [key, value] of Object.entries(newConsents)) {
      const oldValue = existing.consents[key as keyof typeof existing.consents];
      if (oldValue !== value) {
        changes[key] = { from: oldValue, to: value };
      }
    }
  }

  const consentRecord: ConsentRecord = {
    userId,
    consents: newConsents,
    ipAddress: context.rawRequest?.ip,
    userAgent: context.rawRequest?.headers?.['user-agent'] as string,
    consentVersion: CURRENT_CONSENT_VERSION,
    createdAt: existing?.createdAt || admin.firestore.Timestamp.now(),
    updatedAt: admin.firestore.Timestamp.now(),
    history: existing?.history || []
  };

  // Add change to history
  if (Object.keys(changes).length > 0) {
    consentRecord.history.push({
      timestamp: admin.firestore.Timestamp.now(),
      changes,
      source: 'user'
    });
  }

  await consentRef.set(consentRecord);

  // If analytics consent withdrawn, delete analytics data
  if (existing?.consents.analytics && !newConsents.analytics) {
    await deleteAnalyticsData(userId);
  }

  return {
    success: true,
    consents: newConsents,
    consentVersion: CURRENT_CONSENT_VERSION
  };
});

/**
 * Get current consent status
 */
export const getConsent = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  const userId = context.auth.uid;
  const consentDoc = await db.collection('consent_records').doc(userId).get();

  if (!consentDoc.exists) {
    return {
      hasConsent: false,
      needsUpdate: true,
      currentVersion: CURRENT_CONSENT_VERSION
    };
  }

  const consent = consentDoc.data() as ConsentRecord;
  const needsUpdate = consent.consentVersion !== CURRENT_CONSENT_VERSION;

  return {
    hasConsent: true,
    consents: consent.consents,
    consentVersion: consent.consentVersion,
    needsUpdate,
    currentVersion: CURRENT_CONSENT_VERSION,
    lastUpdated: consent.updatedAt.toDate().toISOString()
  };
});

async function deleteAnalyticsData(userId: string): Promise<void> {
  // Delete analytics events
  const analyticsSnapshot = await db.collection('analytics_events')
    .where('userId', '==', userId)
    .get();

  const batch = db.batch();
  analyticsSnapshot.forEach(doc => batch.delete(doc.ref));
  
  if (!analyticsSnapshot.empty) {
    await batch.commit();
  }
}

// ============================================================================
// Privacy Settings
// ============================================================================

/**
 * Update privacy settings
 */
export const updatePrivacySettings = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  const userId = context.auth.uid;
  const settings = data?.settings;

  if (!settings || typeof settings !== 'object') {
    throw new functions.https.HttpsError('invalid-argument', 'Settings object required');
  }

  const privacySettings: PrivacySettings = {
    userId,
    profileVisibility: ['public', 'friends', 'private'].includes(settings.profileVisibility) 
      ? settings.profileVisibility 
      : 'public',
    showOnLeaderboards: settings.showOnLeaderboards !== false,
    allowFriendRequests: settings.allowFriendRequests !== false,
    showOnlineStatus: settings.showOnlineStatus !== false,
    showLastActive: settings.showLastActive !== false,
    allowAllianceInvites: settings.allowAllianceInvites !== false,
    shareLocationWithAlliance: settings.shareLocationWithAlliance !== false,
    updatedAt: admin.firestore.Timestamp.now()
  };

  await db.collection('privacy_settings').doc(userId).set(privacySettings);

  // If hiding from leaderboards, remove from cached leaderboards
  if (!privacySettings.showOnLeaderboards) {
    await removeFromLeaderboards(userId);
  }

  return {
    success: true,
    settings: privacySettings
  };
});

/**
 * Get privacy settings
 */
export const getPrivacySettings = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  const userId = context.auth.uid;
  const settingsDoc = await db.collection('privacy_settings').doc(userId).get();

  if (!settingsDoc.exists) {
    // Return defaults
    return {
      settings: {
        profileVisibility: 'public',
        showOnLeaderboards: true,
        allowFriendRequests: true,
        showOnlineStatus: true,
        showLastActive: true,
        allowAllianceInvites: true,
        shareLocationWithAlliance: true
      }
    };
  }

  return {
    settings: settingsDoc.data()
  };
});

async function removeFromLeaderboards(userId: string): Promise<void> {
  // This would integrate with your leaderboard system
  // For now, just mark user as hidden
  await db.collection('users').doc(userId).update({
    hiddenFromLeaderboards: true
  });
}

// ============================================================================
// Helper Functions
// ============================================================================

async function collectUserData(userId: string): Promise<UserDataExport> {
  const exportData: UserDataExport = {
    exportedAt: new Date().toISOString(),
    userId,
    format: 'json',
    sections: {
      profile: null,
      gameProgress: null,
      territories: [],
      buildings: [],
      resources: null,
      alliance: null,
      friends: [],
      chat: [],
      purchases: [],
      achievements: [],
      seasonPass: null,
      notifications: null,
      settings: null,
      analytics: [],
      loginHistory: []
    }
  };

  // User profile
  const userDoc = await db.collection('users').doc(userId).get();
  if (userDoc.exists) {
    const userData = userDoc.data()!;
    exportData.sections.profile = {
      displayName: userData.displayName,
      email: userData.email,
      createdAt: userData.createdAt?.toDate?.()?.toISOString(),
      lastLogin: userData.lastLogin?.toDate?.()?.toISOString(),
      level: userData.level,
      xp: userData.xp
    };
    exportData.sections.resources = userData.resources;
    exportData.sections.gameProgress = {
      level: userData.level,
      xp: userData.xp,
      totalPlayTime: userData.totalPlayTime,
      territoriesClaimed: userData.territoriesClaimed,
      buildingsPlaced: userData.buildingsPlaced,
      battlesWon: userData.battlesWon,
      battlesLost: userData.battlesLost
    };
  }

  // Territories
  const territoriesSnap = await db.collection('territories')
    .where('ownerId', '==', userId)
    .get();
  exportData.sections.territories = territoriesSnap.docs.map(doc => ({
    id: doc.id,
    ...doc.data(),
    createdAt: doc.data().createdAt?.toDate?.()?.toISOString()
  }));

  // Buildings
  const buildingsSnap = await db.collection('buildings')
    .where('ownerId', '==', userId)
    .get();
  exportData.sections.buildings = buildingsSnap.docs.map(doc => ({
    id: doc.id,
    ...doc.data()
  }));

  // Alliance membership
  const allianceMemberSnap = await db.collection('alliance_members')
    .where('userId', '==', userId)
    .limit(1)
    .get();
  if (!allianceMemberSnap.empty) {
    const membership = allianceMemberSnap.docs[0].data();
    const allianceDoc = await db.collection('alliances').doc(membership.allianceId).get();
    exportData.sections.alliance = {
      membership,
      allianceInfo: allianceDoc.exists ? allianceDoc.data() : null
    };
  }

  // Friends
  const friendsSnap = await db.collection('friends')
    .where('userId', '==', userId)
    .get();
  exportData.sections.friends = friendsSnap.docs.map(doc => doc.data());

  // Chat messages (last 1000)
  const chatSnap = await db.collection('chat_messages')
    .where('senderId', '==', userId)
    .orderBy('createdAt', 'desc')
    .limit(1000)
    .get();
  exportData.sections.chat = chatSnap.docs.map(doc => ({
    id: doc.id,
    ...doc.data(),
    createdAt: doc.data().createdAt?.toDate?.()?.toISOString()
  }));

  // Purchases
  const purchasesSnap = await db.collection('purchases')
    .where('userId', '==', userId)
    .get();
  exportData.sections.purchases = purchasesSnap.docs.map(doc => ({
    id: doc.id,
    ...doc.data()
  }));

  // Achievements
  const achievementsSnap = await db.collection('user_achievements')
    .where('userId', '==', userId)
    .get();
  exportData.sections.achievements = achievementsSnap.docs.map(doc => doc.data());

  // Season pass progress
  const seasonPassDoc = await db.collection('season_progress').doc(userId).get();
  if (seasonPassDoc.exists) {
    exportData.sections.seasonPass = seasonPassDoc.data();
  }

  // Notification settings
  const notifDoc = await db.collection('notification_settings').doc(userId).get();
  if (notifDoc.exists) {
    exportData.sections.notifications = notifDoc.data();
  }

  // Privacy & consent settings
  const consentDoc = await db.collection('consent_records').doc(userId).get();
  const privacyDoc = await db.collection('privacy_settings').doc(userId).get();
  exportData.sections.settings = {
    consent: consentDoc.exists ? consentDoc.data() : null,
    privacy: privacyDoc.exists ? privacyDoc.data() : null
  };

  // Analytics events (last 1000)
  const analyticsSnap = await db.collection('analytics_events')
    .where('userId', '==', userId)
    .orderBy('timestamp', 'desc')
    .limit(1000)
    .get();
  exportData.sections.analytics = analyticsSnap.docs.map(doc => ({
    ...doc.data(),
    timestamp: doc.data().timestamp?.toDate?.()?.toISOString()
  }));

  // Login history (last 100)
  const loginSnap = await db.collection('login_history')
    .where('userId', '==', userId)
    .orderBy('timestamp', 'desc')
    .limit(100)
    .get();
  exportData.sections.loginHistory = loginSnap.docs.map(doc => ({
    ...doc.data(),
    timestamp: doc.data().timestamp?.toDate?.()?.toISOString()
  }));

  return exportData;
}

function convertToCSV(data: UserDataExport): string {
  const lines: string[] = [];
  
  lines.push('=== APEX CITADELS DATA EXPORT ===');
  lines.push(`Exported At: ${data.exportedAt}`);
  lines.push(`User ID: ${data.userId}`);
  lines.push('');

  // Profile section
  lines.push('=== PROFILE ===');
  if (data.sections.profile) {
    for (const [key, value] of Object.entries(data.sections.profile)) {
      lines.push(`${key},${JSON.stringify(value)}`);
    }
  }
  lines.push('');

  // Game Progress
  lines.push('=== GAME PROGRESS ===');
  if (data.sections.gameProgress) {
    for (const [key, value] of Object.entries(data.sections.gameProgress)) {
      lines.push(`${key},${JSON.stringify(value)}`);
    }
  }
  lines.push('');

  // Territories
  lines.push('=== TERRITORIES ===');
  if (data.sections.territories.length > 0) {
    const headers = Object.keys(data.sections.territories[0] as object);
    lines.push(headers.join(','));
    for (const territory of data.sections.territories) {
      const t = territory as Record<string, unknown>;
      lines.push(headers.map(h => JSON.stringify(t[h])).join(','));
    }
  }
  lines.push('');

  // Buildings
  lines.push('=== BUILDINGS ===');
  if (data.sections.buildings.length > 0) {
    const headers = Object.keys(data.sections.buildings[0] as object);
    lines.push(headers.join(','));
    for (const building of data.sections.buildings) {
      const b = building as Record<string, unknown>;
      lines.push(headers.map(h => JSON.stringify(b[h])).join(','));
    }
  }
  lines.push('');

  // Purchases
  lines.push('=== PURCHASES ===');
  if (data.sections.purchases.length > 0) {
    const headers = Object.keys(data.sections.purchases[0] as object);
    lines.push(headers.join(','));
    for (const purchase of data.sections.purchases) {
      const p = purchase as Record<string, unknown>;
      lines.push(headers.map(h => JSON.stringify(p[h])).join(','));
    }
  }

  return lines.join('\n');
}

async function sendExportReadyNotification(userId: string, downloadUrl: string): Promise<void> {
  // Create in-app notification
  await db.collection('notifications').add({
    userId,
    type: 'data_export_ready',
    title: 'Your Data Export is Ready',
    message: 'Your personal data export has been completed and is ready for download.',
    data: { downloadUrl },
    read: false,
    createdAt: admin.firestore.Timestamp.now()
  });

  // Could also trigger push notification or email here
}

async function sendDeletionScheduledNotification(userId: string, scheduledDate: Date): Promise<void> {
  await db.collection('notifications').add({
    userId,
    type: 'data_deletion_scheduled',
    title: 'Data Deletion Scheduled',
    message: `Your account and all associated data will be permanently deleted on ${scheduledDate.toISOString().split('T')[0]}. You can cancel this request in your account settings.`,
    data: { scheduledDate: scheduledDate.toISOString() },
    read: false,
    createdAt: admin.firestore.Timestamp.now()
  });
}

// ============================================================================
// Admin Functions
// ============================================================================

/**
 * Admin: Get all pending deletion requests
 */
export const adminGetDeletionRequests = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  // Check admin status
  const userDoc = await db.collection('users').doc(context.auth.uid).get();
  if (!userDoc.exists || !userDoc.data()?.isAdmin) {
    throw new functions.https.HttpsError('permission-denied', 'Admin access required');
  }

  const status = data?.status || 'pending';
  const limit = Math.min(data?.limit || 50, 100);

  const requests = await db.collection('data_deletion_requests')
    .where('status', '==', status)
    .orderBy('requestedAt', 'desc')
    .limit(limit)
    .get();

  return {
    requests: requests.docs.map(doc => doc.data()),
    total: requests.size
  };
});

/**
 * Admin: Force immediate deletion (for urgent requests)
 */
export const adminForceDelete = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  // Check admin status
  const userDoc = await db.collection('users').doc(context.auth.uid).get();
  if (!userDoc.exists || !userDoc.data()?.isAdmin) {
    throw new functions.https.HttpsError('permission-denied', 'Admin access required');
  }

  const requestId = data?.requestId;
  if (!requestId) {
    throw new functions.https.HttpsError('invalid-argument', 'Request ID required');
  }

  const requestDoc = await db.collection('data_deletion_requests').doc(requestId).get();
  if (!requestDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Request not found');
  }

  const request = requestDoc.data() as DataDeletionRequest;
  
  await executeDataDeletion(requestId, request.userId);

  return {
    success: true,
    message: `Data deletion completed for user ${request.userId}`
  };
});
