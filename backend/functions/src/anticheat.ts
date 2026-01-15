/**
 * Apex Citadels - Anti-Cheat & Location Validation System
 * 
 * Critical for location-based games:
 * - GPS spoofing detection
 * - Movement speed validation
 * - Behavior analysis
 * - Rate limiting
 * - Device fingerprinting
 */

import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';

const db = admin.firestore();

// ============================================================================
// Types
// ============================================================================

export interface LocationValidation {
  isValid: boolean;
  confidence: number; // 0-100
  flags: ValidationFlag[];
  riskScore: number; // 0-100, higher = more suspicious
}

export interface ValidationFlag {
  code: string;
  severity: 'low' | 'medium' | 'high' | 'critical';
  message: string;
  timestamp: admin.firestore.Timestamp;
}

export interface UserLocationHistory {
  userId: string;
  locations: LocationRecord[];
  lastUpdated: admin.firestore.Timestamp;
}

export interface LocationRecord {
  latitude: number;
  longitude: number;
  accuracy: number;
  altitude?: number;
  timestamp: admin.firestore.Timestamp;
  source: 'gps' | 'network' | 'fused';
  validated: boolean;
}

export interface SuspiciousActivity {
  id: string;
  userId: string;
  type: 'teleport' | 'impossible_speed' | 'gps_spoof' | 'emulator' | 
        'modified_client' | 'time_manipulation' | 'bot_behavior' | 'rate_abuse';
  severity: 'low' | 'medium' | 'high' | 'critical';
  description: string;
  evidence: Record<string, unknown>;
  createdAt: admin.firestore.Timestamp;
  reviewed: boolean;
  action?: 'warning' | 'temp_ban' | 'perm_ban' | 'dismissed';
}

export interface UserTrustScore {
  userId: string;
  score: number; // 0-100
  tier: 'untrusted' | 'suspicious' | 'neutral' | 'trusted' | 'verified';
  factors: TrustFactor[];
  lastCalculated: admin.firestore.Timestamp;
  violations: number;
  warnings: number;
  tempBans: number;
}

export interface TrustFactor {
  name: string;
  impact: number; // -20 to +20
  reason: string;
}

export interface RateLimitConfig {
  action: string;
  maxRequests: number;
  windowSeconds: number;
  cooldownSeconds: number;
}

// ============================================================================
// Constants
// ============================================================================

// Maximum possible speeds (m/s)
// Note: Walking (2.5), Running (8), Cycling (15) speeds reserved for future granular validation
const MAX_DRIVING_SPEED = 45; // ~162 km/h
const MAX_REASONABLE_SPEED = 100; // ~360 km/h (high-speed rail)
const TELEPORT_THRESHOLD = 200; // 200 m/s = instant flag

// Rate limits per action
const RATE_LIMITS: Record<string, RateLimitConfig> = {
  claim_territory: { action: 'claim_territory', maxRequests: 10, windowSeconds: 60, cooldownSeconds: 30 },
  attack_territory: { action: 'attack_territory', maxRequests: 20, windowSeconds: 60, cooldownSeconds: 10 },
  place_block: { action: 'place_block', maxRequests: 100, windowSeconds: 60, cooldownSeconds: 5 },
  harvest_resource: { action: 'harvest_resource', maxRequests: 30, windowSeconds: 60, cooldownSeconds: 10 },
  send_message: { action: 'send_message', maxRequests: 30, windowSeconds: 60, cooldownSeconds: 5 },
  friend_request: { action: 'friend_request', maxRequests: 20, windowSeconds: 3600, cooldownSeconds: 60 }
};

// Trust score thresholds
const TRUST_TIERS = {
  untrusted: { min: 0, max: 20 },
  suspicious: { min: 21, max: 40 },
  neutral: { min: 41, max: 60 },
  trusted: { min: 61, max: 80 },
  verified: { min: 81, max: 100 }
};

// ============================================================================
// Location Validation
// ============================================================================

export async function validateLocation(
  userId: string,
  latitude: number,
  longitude: number,
  accuracy: number,
  timestamp: number,
  deviceInfo?: DeviceInfo
): Promise<LocationValidation> {
  const flags: ValidationFlag[] = [];
  let riskScore = 0;
  
  // Get user's location history
  const historyDoc = await db.collection('location_history').doc(userId).get();
  const history = historyDoc.exists ? historyDoc.data() as UserLocationHistory : null;
  
  // Check 1: Accuracy threshold
  if (accuracy > 100) {
    flags.push({
      code: 'LOW_ACCURACY',
      severity: 'low',
      message: `GPS accuracy is ${accuracy}m (threshold: 100m)`,
      timestamp: admin.firestore.Timestamp.now()
    });
    riskScore += 5;
  }
  
  // Check 2: Movement speed validation
  if (history && history.locations.length > 0) {
    const lastLocation = history.locations[history.locations.length - 1];
    const distance = calculateDistance(
      lastLocation.latitude, lastLocation.longitude,
      latitude, longitude
    );
    const timeDiff = (timestamp - lastLocation.timestamp.toMillis()) / 1000;
    
    if (timeDiff > 0) {
      const speed = distance / timeDiff;
      
      if (speed > TELEPORT_THRESHOLD) {
        flags.push({
          code: 'TELEPORT_DETECTED',
          severity: 'critical',
          message: `Impossible speed: ${speed.toFixed(1)}m/s (${(speed * 3.6).toFixed(1)}km/h)`,
          timestamp: admin.firestore.Timestamp.now()
        });
        riskScore += 50;
        
        // Log suspicious activity
        await logSuspiciousActivity(userId, 'teleport', 'critical', 
          'Teleportation detected - impossible movement speed', {
            fromLat: lastLocation.latitude,
            fromLng: lastLocation.longitude,
            toLat: latitude,
            toLng: longitude,
            distance,
            timeDiff,
            speed
          }
        );
      } else if (speed > MAX_REASONABLE_SPEED) {
        flags.push({
          code: 'IMPOSSIBLE_SPEED',
          severity: 'high',
          message: `Very high speed: ${speed.toFixed(1)}m/s (${(speed * 3.6).toFixed(1)}km/h)`,
          timestamp: admin.firestore.Timestamp.now()
        });
        riskScore += 30;
      } else if (speed > MAX_DRIVING_SPEED) {
        flags.push({
          code: 'HIGH_SPEED',
          severity: 'medium',
          message: `High speed detected: ${(speed * 3.6).toFixed(1)}km/h`,
          timestamp: admin.firestore.Timestamp.now()
        });
        riskScore += 10;
      }
    }
    
    // Check 3: Zigzag pattern (potential spoofing)
    if (history.locations.length >= 3) {
      const zigzagScore = detectZigzagPattern(history.locations.slice(-10));
      if (zigzagScore > 0.7) {
        flags.push({
          code: 'ZIGZAG_PATTERN',
          severity: 'medium',
          message: 'Suspicious zigzag movement pattern detected',
          timestamp: admin.firestore.Timestamp.now()
        });
        riskScore += 15;
      }
    }
  }
  
  // Check 4: Device fingerprint analysis
  if (deviceInfo) {
    const deviceFlags = analyzeDeviceInfo(deviceInfo);
    flags.push(...deviceFlags);
    riskScore += deviceFlags.reduce((sum, f) => {
      const severityScore = { low: 2, medium: 5, high: 10, critical: 25 };
      return sum + severityScore[f.severity];
    }, 0);
  }
  
  // Store location in history
  await storeLocation(userId, latitude, longitude, accuracy, timestamp, riskScore < 30);
  
  // Update trust score if suspicious
  if (riskScore >= 20) {
    await updateTrustScore(userId, -Math.floor(riskScore / 10));
  }
  
  const confidence = Math.max(0, 100 - riskScore);
  
  return {
    isValid: riskScore < 50,
    confidence,
    flags,
    riskScore
  };
}

// ============================================================================
// Rate Limiting
// ============================================================================

export async function checkRateLimit(
  userId: string,
  action: string
): Promise<{ allowed: boolean; waitSeconds?: number; remaining?: number }> {
  const config = RATE_LIMITS[action];
  if (!config) {
    return { allowed: true };
  }
  
  const now = Date.now();
  const windowStart = now - (config.windowSeconds * 1000);
  
  const rateLimitRef = db.collection('rate_limits').doc(`${userId}_${action}`);
  const rateLimitDoc = await rateLimitRef.get();
  
  if (!rateLimitDoc.exists) {
    // First request
    await rateLimitRef.set({
      userId,
      action,
      requests: [now],
      lastRequest: now
    });
    return { allowed: true, remaining: config.maxRequests - 1 };
  }
  
  const rateData = rateLimitDoc.data()!;
  const requests: number[] = rateData.requests || [];
  
  // Filter requests within window
  const recentRequests = requests.filter(r => r > windowStart);
  
  if (recentRequests.length >= config.maxRequests) {
    // Check cooldown
    const lastRequest = rateData.lastRequest || 0;
    const cooldownRemaining = Math.ceil((lastRequest + config.cooldownSeconds * 1000 - now) / 1000);
    
    if (cooldownRemaining > 0) {
      // Log rate abuse if repeated
      if (recentRequests.length > config.maxRequests * 1.5) {
        await logSuspiciousActivity(userId, 'rate_abuse', 'medium',
          `Rate limit exceeded for ${action}`, {
            action,
            requests: recentRequests.length,
            maxRequests: config.maxRequests,
            windowSeconds: config.windowSeconds
          }
        );
      }
      
      return { allowed: false, waitSeconds: cooldownRemaining };
    }
  }
  
  // Add new request
  recentRequests.push(now);
  await rateLimitRef.update({
    requests: recentRequests,
    lastRequest: now
  });
  
  return { allowed: true, remaining: Math.max(0, config.maxRequests - recentRequests.length) };
}

// ============================================================================
// Trust Score System
// ============================================================================

export async function getUserTrustScore(userId: string): Promise<UserTrustScore> {
  const trustDoc = await db.collection('trust_scores').doc(userId).get();
  
  if (trustDoc.exists) {
    return trustDoc.data() as UserTrustScore;
  }
  
  // Initialize trust score for new user
  return initializeTrustScore(userId);
}

async function initializeTrustScore(userId: string): Promise<UserTrustScore> {
  const trustScore: UserTrustScore = {
    userId,
    score: 50, // Start neutral
    tier: 'neutral',
    factors: [
      { name: 'new_account', impact: 0, reason: 'New account - no history' }
    ],
    lastCalculated: admin.firestore.Timestamp.now(),
    violations: 0,
    warnings: 0,
    tempBans: 0
  };
  
  await db.collection('trust_scores').doc(userId).set(trustScore);
  return trustScore;
}

async function updateTrustScore(userId: string, change: number, reason?: string): Promise<void> {
  const trustDoc = await db.collection('trust_scores').doc(userId).get();
  
  let currentScore = 50;
  let factors: TrustFactor[] = [];
  let violations = 0;
  let warnings = 0;
  let tempBans = 0;
  
  if (trustDoc.exists) {
    const data = trustDoc.data()!;
    currentScore = data.score;
    factors = data.factors || [];
    violations = data.violations || 0;
    warnings = data.warnings || 0;
    tempBans = data.tempBans || 0;
  }
  
  const newScore = Math.max(0, Math.min(100, currentScore + change));
  
  if (reason) {
    factors.push({
      name: reason,
      impact: change,
      reason: `Score changed by ${change}`
    });
    
    // Keep only last 20 factors
    if (factors.length > 20) {
      factors = factors.slice(-20);
    }
  }
  
  if (change < 0) {
    violations++;
  }
  
  // Determine tier
  let tier: UserTrustScore['tier'] = 'neutral';
  for (const [tierName, range] of Object.entries(TRUST_TIERS)) {
    if (newScore >= range.min && newScore <= range.max) {
      tier = tierName as UserTrustScore['tier'];
      break;
    }
  }
  
  await db.collection('trust_scores').doc(userId).set({
    userId,
    score: newScore,
    tier,
    factors,
    lastCalculated: admin.firestore.Timestamp.now(),
    violations,
    warnings,
    tempBans
  }, { merge: true });
  
  // Check for automatic actions
  if (newScore < 10 && violations >= 5) {
    await issueAutomaticBan(userId, 'Repeated trust score violations');
  } else if (newScore < 20 && violations >= 3) {
    await issueWarning(userId, 'Low trust score due to suspicious activity');
  }
}

// ============================================================================
// Suspicious Activity Logging
// ============================================================================

async function logSuspiciousActivity(
  userId: string,
  type: SuspiciousActivity['type'],
  severity: SuspiciousActivity['severity'],
  description: string,
  evidence: Record<string, unknown>
): Promise<void> {
  const activityRef = db.collection('suspicious_activities').doc();
  
  const activity: SuspiciousActivity = {
    id: activityRef.id,
    userId,
    type,
    severity,
    description,
    evidence,
    createdAt: admin.firestore.Timestamp.now(),
    reviewed: false
  };
  
  await activityRef.set(activity);
  
  functions.logger.warn('Suspicious activity detected', {
    userId,
    type,
    severity,
    description
  });
}

// ============================================================================
// Ban System
// ============================================================================

async function issueWarning(userId: string, reason: string): Promise<void> {
  await db.collection('user_warnings').add({
    userId,
    reason,
    issuedAt: admin.firestore.Timestamp.now(),
    acknowledged: false
  });
  
  await db.collection('trust_scores').doc(userId).update({
    warnings: admin.firestore.FieldValue.increment(1)
  });
  
  functions.logger.info('Warning issued', { userId, reason });
}

async function issueAutomaticBan(userId: string, reason: string): Promise<void> {
  const banUntil = new Date();
  banUntil.setDate(banUntil.getDate() + 7); // 7-day temp ban
  
  await db.collection('users').doc(userId).update({
    isBanned: true,
    banReason: reason,
    banUntil: admin.firestore.Timestamp.fromDate(banUntil),
    banIssuedAt: admin.firestore.Timestamp.now()
  });
  
  await db.collection('trust_scores').doc(userId).update({
    tempBans: admin.firestore.FieldValue.increment(1)
  });
  
  functions.logger.warn('Automatic ban issued', { userId, reason, banUntil });
}

// ============================================================================
// Device Info Analysis
// ============================================================================

export interface DeviceInfo {
  platform: string;
  osVersion: string;
  deviceModel: string;
  appVersion: string;
  isRooted?: boolean;
  isEmulator?: boolean;
  mockLocationsEnabled?: boolean;
  developerOptionsEnabled?: boolean;
  fingerprint?: string;
}

function analyzeDeviceInfo(deviceInfo: DeviceInfo): ValidationFlag[] {
  const flags: ValidationFlag[] = [];
  
  if (deviceInfo.isEmulator) {
    flags.push({
      code: 'EMULATOR_DETECTED',
      severity: 'critical',
      message: 'Device appears to be an emulator',
      timestamp: admin.firestore.Timestamp.now()
    });
  }
  
  if (deviceInfo.isRooted) {
    flags.push({
      code: 'ROOTED_DEVICE',
      severity: 'high',
      message: 'Device is rooted/jailbroken',
      timestamp: admin.firestore.Timestamp.now()
    });
  }
  
  if (deviceInfo.mockLocationsEnabled) {
    flags.push({
      code: 'MOCK_LOCATIONS',
      severity: 'critical',
      message: 'Mock locations are enabled',
      timestamp: admin.firestore.Timestamp.now()
    });
  }
  
  if (deviceInfo.developerOptionsEnabled) {
    flags.push({
      code: 'DEV_OPTIONS',
      severity: 'low',
      message: 'Developer options are enabled',
      timestamp: admin.firestore.Timestamp.now()
    });
  }
  
  return flags;
}

// ============================================================================
// Helper Functions
// ============================================================================

function calculateDistance(lat1: number, lng1: number, lat2: number, lng2: number): number {
  const R = 6371000; // Earth's radius in meters
  const dLat = (lat2 - lat1) * Math.PI / 180;
  const dLng = (lng2 - lng1) * Math.PI / 180;
  const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
    Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
    Math.sin(dLng / 2) * Math.sin(dLng / 2);
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  return R * c;
}

function detectZigzagPattern(locations: LocationRecord[]): number {
  if (locations.length < 3) return 0;
  
  let directionChanges = 0;
  
  for (let i = 2; i < locations.length; i++) {
    const prev = locations[i - 2];
    const curr = locations[i - 1];
    const next = locations[i];
    
    const bearing1 = calculateBearing(prev.latitude, prev.longitude, curr.latitude, curr.longitude);
    const bearing2 = calculateBearing(curr.latitude, curr.longitude, next.latitude, next.longitude);
    
    const bearingDiff = Math.abs(bearing1 - bearing2);
    
    // Count significant direction changes (> 90 degrees)
    if (bearingDiff > 90 && bearingDiff < 270) {
      directionChanges++;
    }
  }
  
  return directionChanges / (locations.length - 2);
}

function calculateBearing(lat1: number, lng1: number, lat2: number, lng2: number): number {
  const dLng = (lng2 - lng1) * Math.PI / 180;
  const lat1Rad = lat1 * Math.PI / 180;
  const lat2Rad = lat2 * Math.PI / 180;
  
  const y = Math.sin(dLng) * Math.cos(lat2Rad);
  const x = Math.cos(lat1Rad) * Math.sin(lat2Rad) -
    Math.sin(lat1Rad) * Math.cos(lat2Rad) * Math.cos(dLng);
  
  return (Math.atan2(y, x) * 180 / Math.PI + 360) % 360;
}

async function storeLocation(
  userId: string,
  latitude: number,
  longitude: number,
  accuracy: number,
  timestamp: number,
  validated: boolean
): Promise<void> {
  const historyRef = db.collection('location_history').doc(userId);
  const record: LocationRecord = {
    latitude,
    longitude,
    accuracy,
    timestamp: admin.firestore.Timestamp.fromMillis(timestamp),
    source: 'fused',
    validated
  };
  
  await historyRef.set({
    userId,
    locations: admin.firestore.FieldValue.arrayUnion(record),
    lastUpdated: admin.firestore.Timestamp.now()
  }, { merge: true });
  
  // Cleanup old locations (keep last 100)
  const historyDoc = await historyRef.get();
  if (historyDoc.exists) {
    const history = historyDoc.data() as UserLocationHistory;
    if (history.locations.length > 100) {
      await historyRef.update({
        locations: history.locations.slice(-100)
      });
    }
  }
}

// ============================================================================
// Cloud Functions
// ============================================================================

export const validateLocationRequest = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { latitude, longitude, accuracy, timestamp, deviceInfo } = data as {
    latitude: number;
    longitude: number;
    accuracy: number;
    timestamp: number;
    deviceInfo?: DeviceInfo;
  };
  
  const validation = await validateLocation(
    context.auth.uid,
    latitude,
    longitude,
    accuracy,
    timestamp,
    deviceInfo
  );
  
  return validation;
});

export const checkActionRateLimit = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { action } = data as { action: string };
  
  const result = await checkRateLimit(context.auth.uid, action);
  
  if (!result.allowed) {
    throw new functions.https.HttpsError(
      'resource-exhausted',
      `Rate limit exceeded. Wait ${result.waitSeconds} seconds.`
    );
  }
  
  return result;
});

export const getTrustScore = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const trustScore = await getUserTrustScore(context.auth.uid);
  
  // Don't expose full details to user
  return {
    tier: trustScore.tier,
    isVerified: trustScore.tier === 'verified',
    isTrusted: trustScore.score >= 60
  };
});

// Admin function to review suspicious activities
export const reviewSuspiciousActivity = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { activityId, action, reason } = data as {
    activityId: string;
    action: 'warning' | 'temp_ban' | 'perm_ban' | 'dismissed';
    reason?: string;
  };
  
  const activityDoc = await db.collection('suspicious_activities').doc(activityId).get();
  if (!activityDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Activity not found');
  }
  
  const activity = activityDoc.data() as SuspiciousActivity;
  
  await activityDoc.ref.update({
    reviewed: true,
    action,
    reviewedAt: admin.firestore.Timestamp.now(),
    reviewReason: reason
  });
  
  // Apply action
  switch (action) {
    case 'warning':
      await issueWarning(activity.userId, reason || activity.description);
      break;
    case 'temp_ban':
      await issueAutomaticBan(activity.userId, reason || activity.description);
      break;
    case 'perm_ban':
      await db.collection('users').doc(activity.userId).update({
        isBanned: true,
        banReason: reason || activity.description,
        banIsPermanent: true,
        banIssuedAt: admin.firestore.Timestamp.now()
      });
      break;
    case 'dismissed':
      // Optionally increase trust score
      await updateTrustScore(activity.userId, 5, 'false_positive_cleared');
      break;
  }
  
  return { success: true };
});

// Scheduled cleanup of old rate limit data
export const cleanupRateLimits = functions.pubsub
  .schedule('every 6 hours')
  .onRun(async () => {
    const oneHourAgo = Date.now() - (60 * 60 * 1000);
    
    const rateLimitsSnapshot = await db.collection('rate_limits')
      .where('lastRequest', '<', oneHourAgo)
      .limit(500)
      .get();
    
    const batch = db.batch();
    rateLimitsSnapshot.docs.forEach(doc => {
      batch.delete(doc.ref);
    });
    
    await batch.commit();
    functions.logger.info(`Cleaned up ${rateLimitsSnapshot.size} rate limit records`);
  });
