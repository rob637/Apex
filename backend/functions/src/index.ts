/**
 * Apex Citadels - Cloud Functions
 * 
 * This module provides the serverless backend for the game, including:
 * - Spatial anchor management
 * - User authentication hooks
 * - Game state validation
 * - Leaderboard calculations
 * - Combat system
 * - Progression (daily rewards, achievements, XP)
 * - Alliance management
 * - Notifications
 * - World Events (FOMO system)
 * - Season Pass / Battle Pass
 * - Friends & Social
 * - Anti-cheat & Location Validation
 * - Analytics & Telemetry
 * - Real-time Map API
 * - Referral System
 * - Chat System
 */

import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';

admin.initializeApp();

// ============================================================================
// Re-export Modular Functions
// ============================================================================

// Combat System
export {
  attackTerritory,
  getAttackCooldowns,
  getBattleHistory,
  repairTerritory,
  activateShield
} from './combat';

// Battle System (Turn-based Strategic Combat)
export {
  scheduleBattle,
  setBattleFormation,
  executeBattle,
  reclaimTerritory,
  reportBattleParticipation,
  trainTroops,
  collectTrainedTroops,
  processPendingBattles
} from './battle';

// Protection System (Shields & Activity Bonuses)
export {
  getMyProtectionStatus,
  checkTerritoryAttackable,
  waiveNewcomerShield,
  grantTemporaryShield
} from './protection';

// Blueprint System (Save/Restore Citadel Layouts)
export {
  saveBlueprint,
  getMyBlueprints,
  getBlueprintDetails,
  deleteBlueprint,
  renameBlueprint,
  applyBlueprint,
  previewBlueprintCost
} from './blueprint';

// Location Utilities (Density & Territory Sizing)
export {
  setAreaDensity,
  getLocationInfo,
  precomputeRegionDensity
} from './location-utils';

// Progression System (Daily Rewards, Achievements, XP)
export {
  claimDailyReward,
  getDailyRewardStatus,
  awardXp,
  checkAchievements,
  claimAchievementReward,
  getAchievements
} from './progression';

// Alliance System
export {
  createAlliance,
  joinAlliance,
  leaveAlliance,
  inviteToAlliance,
  respondToInvitation,
  startAllianceWar,
  declareAllianceWar,
  cancelAllianceWar,
  getAllianceWarStatus,
  processWarPhases,
  getAllianceDetails,
  searchAlliances
} from './alliance';

// Notification System
export {
  getNotifications,
  markNotificationRead,
  cleanupOldNotifications,
  registerFcmToken,
  updateNotificationSettings,
  sendDailyRewardReminder as sendDailyRewardReminderLegacy
} from './notifications';

// World Events System (FOMO)
export {
  getActiveEvents,
  joinEvent,
  contributeToEvent,
  claimEventRewards,
  createWorldEvent,
  activateScheduledEvents,
  completeEndedEvents,
  getEventLeaderboard
} from './world-events';

// Season Pass / Battle Pass System
export {
  getCurrentSeason,
  purchasePremiumPass,
  claimSeasonReward,
  completeChallenge,
  refreshDailyChallenges,
  createSeason
} from './season-pass';

// Friends & Social System
export {
  sendFriendRequest,
  respondToFriendRequest,
  getFriendsList,
  getFriendRequests,
  sendGift,
  claimGift,
  getPendingGifts,
  getActivityFeed,
  likeActivity,
  removeFriend,
  searchUsers,
  getFriendLeaderboard
} from './friends';

// Anti-cheat & Location Validation
export {
  validateLocationRequest,
  checkActionRateLimit,
  getTrustScore,
  reviewSuspiciousActivity,
  cleanupRateLimits
} from './anticheat';

// Analytics & Telemetry
export {
  trackEvent,
  startSession,
  endSession,
  getFunnelAnalysis,
  getEconomyMetrics,
  getRetentionCohorts,
  getABTestVariant,
  recordABTestConversion,
  calculateDailyMetrics
} from './analytics';

// Real-time Map API
export {
  getMapTiles,
  getTerritoriesInArea,
  getNearbyPlayers,
  getRecentActivity,
  getHeatmapData,
  cleanupMapData
} from './map-api';

// Referral & Viral Growth System
export {
  generateReferralCode,
  useReferralCode,
  claimReferralRewards,
  claimMilestoneReward,
  recordShare,
  getReferralStats,
  getActiveViralChallenges,
  generateShareLink
} from './referrals';

// Chat System
export {
  sendMessage,
  getMessages,
  createChannel,
  joinChannel,
  leaveChannel,
  reactToMessage,
  reportMessage,
  deleteMessage,
  muteUser,
  getUserChannels,
  initializeGlobalChat
} from './chat';

// In-App Purchases System
export {
  getProductCatalog,
  verifyApplePurchase,
  verifyGooglePurchase,
  restorePurchases,
  getPurchaseHistory,
  getEntitlements,
  adminGrantProduct,
  appleWebhook,
  checkSubscriptionExpiry,
  grantDailyVIPRewards,
  getRevenueAnalytics
} from './iap';

// Push Notifications
export {
  registerPushToken,
  unregisterPushToken,
  updateNotificationPreferences,
  getNotificationSettings,
  sendPushNotification,
  broadcastNotification,
  subscribeToAllianceTopic,
  unsubscribeFromAllianceTopic,
  onTerritoryAttacked,
  onFriendRequestCreated,
  onAllianceInviteCreated,
  onAllianceWarStarted,
  onUserLevelUp,
  sendDailyRewardReminder,
  sendWelcomeBackNotification,
  sendWorldEventReminders,
  sendSeasonEndingReminder,
  notificationWebhook
} from './push';

// World Seed System
export {
  seedRegion,
  getNearbySeeds,
  discoverSeed,
  claimSeedResources,
  getSeededRegions,
  clearRegionSeeds,
  importPOIData
} from './world-seed';

// GDPR & Privacy Compliance
export {
  requestDataExport,
  processDataExport,
  getDataExportStatus,
  requestDataDeletion,
  cancelDataDeletion,
  processScheduledDeletions,
  updateConsent,
  getConsent,
  updatePrivacySettings,
  getPrivacySettings,
  adminGetDeletionRequests,
  adminForceDelete
} from './gdpr';

// Content Moderation
export {
  moderateContent,
  reportContent,
  resolveReport,
  getPendingReports,
  muteUser as moderationMuteUser,
  banUserFunction,
  appealBan,
  cleanupExpiredMutes,
  checkBanStatus
} from './moderation';

// Cosmetics Shop
export {
  getShopCatalog,
  getFeaturedItems,
  purchaseCosmetic,
  getUserCosmetics,
  equipCosmetic,
  unequipCosmetic,
  toggleFavorite,
  getCurrencyBalance,
  awardCurrency,
  adminCreateCosmetic,
  adminCreateRotation,
  adminGrantCosmetic
} from './cosmetics';

const db = admin.firestore();

// ============================================================================
// Types
// ============================================================================

interface SpatialAnchor {
  id: string;
  latitude: number;
  longitude: number;
  altitude: number;
  rotationX: number;
  rotationY: number;
  rotationZ: number;
  rotationW: number;
  createdAt: admin.firestore.Timestamp;
  anchorType: 'Local' | 'Geospatial' | 'Cloud';
  ownerId: string;
  attachedObjectType: string;
  attachedObjectId: string;
  geoHash: string;
}

interface User {
  uid: string;
  displayName: string;
  faction: 'builders' | 'raiders' | 'merchants';
  guildId?: string;
  stats: {
    level: number;
    xp: number;
    citadelsBuilt: number;
    raidsCompleted: number;
    raidsDefended: number;
  };
  createdAt: admin.firestore.Timestamp;
  lastActive: admin.firestore.Timestamp;
}

interface Citadel {
  id: string;
  ownerId: string;
  guildId?: string;
  anchorId: string;
  name: string;
  coreLevel: number;
  health: number;
  maxHealth: number;
  zoneId: string;
  districtId: string;
  cityId: string;
  createdAt: admin.firestore.Timestamp;
  lastRaided?: admin.firestore.Timestamp;
}

// ============================================================================
// Spatial Anchors API
// ============================================================================

/**
 * Get all spatial anchors within a radius of a given location.
 * Uses GeoHash for efficient queries.
 */
export const getAnchorsNearby = functions.https.onCall(async (data, context) => {
  const { latitude, longitude, radiusMeters } = data;

  if (!latitude || !longitude || !radiusMeters) {
    throw new functions.https.HttpsError(
      'invalid-argument',
      'Missing required parameters: latitude, longitude, radiusMeters'
    );
  }

  // Calculate GeoHash precision based on radius
  const precision = getGeoHashPrecision(radiusMeters);
  const centerHash = encodeGeoHash(latitude, longitude, precision);
  
  // Get neighboring geohashes
  const searchHashes = getNeighborGeoHashes(centerHash);
  searchHashes.push(centerHash);

  const anchors: SpatialAnchor[] = [];

  // Query each geohash range
  for (const hash of searchHashes) {
    const snapshot = await db.collection('spatial_anchors')
      .where('geoHash', '>=', hash)
      .where('geoHash', '<', hash + '~')
      .limit(50)
      .get();

    for (const doc of snapshot.docs) {
      const anchor = doc.data() as SpatialAnchor;
      
      // Post-filter by actual distance
      const distance = haversineDistance(
        latitude, longitude,
        anchor.latitude, anchor.longitude
      );

      if (distance <= radiusMeters) {
        anchors.push(anchor);
      }
    }
  }

  return { anchors };
});

/**
 * Create a new spatial anchor (server-side validation).
 */
export const createAnchor = functions.https.onCall(async (data, context) => {
  // Verify authentication
  if (!context.auth) {
    throw new functions.https.HttpsError(
      'unauthenticated',
      'User must be authenticated to create anchors'
    );
  }

  const { latitude, longitude, altitude, rotation, objectType, objectId } = data;

  // Validate coordinates
  if (!isValidCoordinate(latitude, longitude)) {
    throw new functions.https.HttpsError(
      'invalid-argument',
      'Invalid coordinates'
    );
  }

  // Rate limiting: max 10 anchors per user per hour
  const recentAnchors = await db.collection('spatial_anchors')
    .where('ownerId', '==', context.auth.uid)
    .where('createdAt', '>', admin.firestore.Timestamp.fromDate(
      new Date(Date.now() - 3600000)
    ))
    .get();

  if (recentAnchors.size >= 10) {
    throw new functions.https.HttpsError(
      'resource-exhausted',
      'Rate limit exceeded. Maximum 10 anchors per hour.'
    );
  }

  // Create anchor
  const anchorId = db.collection('spatial_anchors').doc().id;
  const geoHash = encodeGeoHash(latitude, longitude, 8);

  const anchor: SpatialAnchor = {
    id: anchorId,
    latitude,
    longitude,
    altitude: altitude || 0,
    rotationX: rotation?.x || 0,
    rotationY: rotation?.y || 0,
    rotationZ: rotation?.z || 0,
    rotationW: rotation?.w || 1,
    createdAt: admin.firestore.Timestamp.now(),
    anchorType: 'Geospatial',
    ownerId: context.auth.uid,
    attachedObjectType: objectType || '',
    attachedObjectId: objectId || '',
    geoHash,
  };

  await db.collection('spatial_anchors').doc(anchorId).set(anchor);

  functions.logger.info(`Anchor created: ${anchorId} by user ${context.auth.uid}`);

  return { anchor };
});

/**
 * Delete a spatial anchor (owner only).
 */
export const deleteAnchor = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const { anchorId } = data;

  const anchorDoc = await db.collection('spatial_anchors').doc(anchorId).get();
  
  if (!anchorDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Anchor not found');
  }

  const anchor = anchorDoc.data() as SpatialAnchor;
  
  if (anchor.ownerId !== context.auth.uid) {
    throw new functions.https.HttpsError('permission-denied', 'Not anchor owner');
  }

  await db.collection('spatial_anchors').doc(anchorId).delete();

  return { success: true };
});

// ============================================================================
// User Management
// ============================================================================

/**
 * Initialize new user on first sign-in.
 */
export const onUserCreated = functions.auth.user().onCreate(async (user) => {
  const newUser: User = {
    uid: user.uid,
    displayName: user.displayName || `Player_${user.uid.slice(0, 6)}`,
    faction: 'builders', // Default faction
    stats: {
      level: 1,
      xp: 0,
      citadelsBuilt: 0,
      raidsCompleted: 0,
      raidsDefended: 0,
    },
    createdAt: admin.firestore.Timestamp.now(),
    lastActive: admin.firestore.Timestamp.now(),
  };

  await db.collection('users').doc(user.uid).set(newUser);
  
  functions.logger.info(`New user initialized: ${user.uid}`);
});

/**
 * Clean up user data on account deletion.
 */
export const onUserDeleted = functions.auth.user().onDelete(async (user) => {
  // Delete user document
  await db.collection('users').doc(user.uid).delete();

  // Delete user's anchors
  const anchors = await db.collection('spatial_anchors')
    .where('ownerId', '==', user.uid)
    .get();

  const batch = db.batch();
  anchors.docs.forEach(doc => batch.delete(doc.ref));
  await batch.commit();

  // Delete user's citadels
  const citadels = await db.collection('citadels')
    .where('ownerId', '==', user.uid)
    .get();

  const citadelBatch = db.batch();
  citadels.docs.forEach(doc => citadelBatch.delete(doc.ref));
  await citadelBatch.commit();

  functions.logger.info(`User data cleaned up: ${user.uid}`);
});

// ============================================================================
// Citadel Management
// ============================================================================

/**
 * Create a new citadel.
 */
export const createCitadel = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const { anchorId, name, zoneId, districtId, cityId } = data;

  // Verify anchor exists and belongs to user
  const anchorDoc = await db.collection('spatial_anchors').doc(anchorId).get();
  if (!anchorDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Anchor not found');
  }

  const anchor = anchorDoc.data() as SpatialAnchor;
  if (anchor.ownerId !== context.auth.uid) {
    throw new functions.https.HttpsError('permission-denied', 'Not anchor owner');
  }

  // Check if zone already has a citadel
  const existingCitadel = await db.collection('citadels')
    .where('zoneId', '==', zoneId)
    .limit(1)
    .get();

  if (!existingCitadel.empty) {
    throw new functions.https.HttpsError(
      'already-exists',
      'Zone already has a citadel'
    );
  }

  const citadelId = db.collection('citadels').doc().id;

  const citadel: Citadel = {
    id: citadelId,
    ownerId: context.auth.uid,
    anchorId,
    name: name || `Citadel_${citadelId.slice(0, 6)}`,
    coreLevel: 1,
    health: 100,
    maxHealth: 100,
    zoneId,
    districtId,
    cityId,
    createdAt: admin.firestore.Timestamp.now(),
  };

  // Update user stats
  await db.collection('users').doc(context.auth.uid).update({
    'stats.citadelsBuilt': admin.firestore.FieldValue.increment(1),
  });

  await db.collection('citadels').doc(citadelId).set(citadel);

  // Update anchor with citadel reference
  await db.collection('spatial_anchors').doc(anchorId).update({
    attachedObjectType: 'Citadel',
    attachedObjectId: citadelId,
  });

  return { citadel };
});

/**
 * Get citadels in a zone.
 */
export const getCitadelsInZone = functions.https.onCall(async (data) => {
  const { zoneId } = data;

  const snapshot = await db.collection('citadels')
    .where('zoneId', '==', zoneId)
    .get();

  const citadels = snapshot.docs.map(doc => doc.data());

  return { citadels };
});

// ============================================================================
// Leaderboard Functions
// ============================================================================

/**
 * Calculate and update district leaderboards (runs daily).
 */
export const updateLeaderboards = functions.pubsub
  .schedule('every 24 hours')
  .onRun(async () => {
    // Get all districts
    const districts = await db.collection('districts').get();

    for (const district of districts.docs) {
      const districtId = district.id;

      // Count citadels per faction in this district
      const factionCounts: Record<string, number> = {
        builders: 0,
        raiders: 0,
        merchants: 0,
      };

      const citadels = await db.collection('citadels')
        .where('districtId', '==', districtId)
        .get();

      for (const citadel of citadels.docs) {
        const ownerId = citadel.data().ownerId;
        const userDoc = await db.collection('users').doc(ownerId).get();
        if (userDoc.exists) {
          const faction = userDoc.data()?.faction || 'builders';
          factionCounts[faction]++;
        }
      }

      // Update leaderboard
      await db.collection('leaderboards').doc(districtId).set({
        districtId,
        factionCounts,
        totalCitadels: citadels.size,
        updatedAt: admin.firestore.Timestamp.now(),
      });
    }

    functions.logger.info('Leaderboards updated');
  });

// ============================================================================
// Utility Functions
// ============================================================================

const BASE32 = '0123456789bcdefghjkmnpqrstuvwxyz';

function encodeGeoHash(lat: number, lng: number, precision: number): string {
  let latRange = [-90.0, 90.0];
  let lngRange = [-180.0, 180.0];
  let hash = '';
  let isEven = true;
  let bit = 0;
  let ch = 0;

  while (hash.length < precision) {
    let mid: number;

    if (isEven) {
      mid = (lngRange[0] + lngRange[1]) / 2;
      if (lng > mid) {
        ch |= 1 << (4 - bit);
        lngRange[0] = mid;
      } else {
        lngRange[1] = mid;
      }
    } else {
      mid = (latRange[0] + latRange[1]) / 2;
      if (lat > mid) {
        ch |= 1 << (4 - bit);
        latRange[0] = mid;
      } else {
        latRange[1] = mid;
      }
    }

    isEven = !isEven;

    if (bit < 4) {
      bit++;
    } else {
      hash += BASE32[ch];
      bit = 0;
      ch = 0;
    }
  }

  return hash;
}

function getGeoHashPrecision(radiusMeters: number): number {
  if (radiusMeters < 100) return 8;
  if (radiusMeters < 500) return 7;
  if (radiusMeters < 2000) return 6;
  if (radiusMeters < 10000) return 5;
  return 4;
}

function getNeighborGeoHashes(hash: string): string[] {
  if (!hash) return [];
  
  const neighbors: string[] = [];
  const lastChar = hash[hash.length - 1];
  const baseHash = hash.slice(0, -1);
  const index = BASE32.indexOf(lastChar);

  if (index > 0) neighbors.push(baseHash + BASE32[index - 1]);
  if (index < BASE32.length - 1) neighbors.push(baseHash + BASE32[index + 1]);

  return neighbors;
}

function haversineDistance(
  lat1: number, lon1: number,
  lat2: number, lon2: number
): number {
  const R = 6371000; // Earth's radius in meters
  const dLat = toRadians(lat2 - lat1);
  const dLon = toRadians(lon2 - lon1);
  
  const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
            Math.cos(toRadians(lat1)) * Math.cos(toRadians(lat2)) *
            Math.sin(dLon / 2) * Math.sin(dLon / 2);
  
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  
  return R * c;
}

function toRadians(degrees: number): number {
  return degrees * Math.PI / 180;
}

function isValidCoordinate(lat: number, lng: number): boolean {
  return lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180;
}
