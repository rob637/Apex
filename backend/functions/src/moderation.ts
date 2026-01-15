/**
 * Apex Citadels - Content Moderation System
 * 
 * Provides multi-layered content filtering and moderation:
 * - Profanity/slur detection
 * - Toxicity scoring
 * - Spam detection
 * - Report handling
 * - User warnings and bans
 * - Appeal system
 */

import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';

const db = admin.firestore();

// ============================================================================
// Types
// ============================================================================

export interface ModerationResult {
  approved: boolean;
  filteredContent?: string;
  flags: ModerationFlag[];
  toxicityScore: number;
  autoAction?: 'none' | 'filter' | 'block' | 'warn' | 'mute' | 'ban';
  requiresReview: boolean;
}

export interface ModerationFlag {
  type: 'profanity' | 'slur' | 'harassment' | 'threat' | 'spam' | 'scam' | 
        'personal_info' | 'advertising' | 'inappropriate' | 'hate_speech';
  severity: 'low' | 'medium' | 'high' | 'critical';
  matchedPattern?: string;
  confidence: number;
}

export interface ContentReport {
  id: string;
  reporterId: string;
  reportedUserId: string;
  contentType: 'chat' | 'username' | 'alliance_name' | 'building_name' | 'other';
  contentId: string;
  content: string;
  reason: 'harassment' | 'hate_speech' | 'spam' | 'inappropriate' | 
          'impersonation' | 'cheating' | 'other';
  description?: string;
  status: 'pending' | 'reviewing' | 'resolved' | 'dismissed';
  resolution?: 'no_action' | 'content_removed' | 'warning' | 'mute' | 'ban';
  resolvedBy?: string;
  resolvedAt?: admin.firestore.Timestamp;
  createdAt: admin.firestore.Timestamp;
}

export interface UserModerationRecord {
  userId: string;
  warnings: Warning[];
  mutes: Mute[];
  bans: Ban[];
  totalReportsAgainst: number;
  totalReportsMade: number;
  falseReportCount: number;
  trustLevel: 'new' | 'normal' | 'trusted' | 'suspicious' | 'restricted';
  lastUpdated: admin.firestore.Timestamp;
}

export interface Warning {
  id: string;
  reason: string;
  issuedAt: admin.firestore.Timestamp;
  issuedBy: string;
  contentId?: string;
  acknowledged: boolean;
}

export interface Mute {
  id: string;
  reason: string;
  startedAt: admin.firestore.Timestamp;
  expiresAt: admin.firestore.Timestamp;
  issuedBy: string;
  active: boolean;
}

export interface Ban {
  id: string;
  reason: string;
  startedAt: admin.firestore.Timestamp;
  expiresAt?: admin.firestore.Timestamp; // null = permanent
  issuedBy: string;
  appealed: boolean;
  appealStatus?: 'pending' | 'approved' | 'denied';
}

// ============================================================================
// Word Lists (Configurable via Firestore)
// ============================================================================

// Base profanity list (would typically be loaded from Firestore for easy updates)
const PROFANITY_PATTERNS: RegExp[] = [
  // Common profanities (patterns to avoid explicit words in code)
  /\b(f+[u*@]+[c*@]+[k*@]+)\b/gi,
  /\b(s+h+[i*@]+[t*@]+)\b/gi,
  /\b(a+[s*@]+[s*@]+)\b/gi,
  /\b(b+[i*@]+[t*@]+c+h+)\b/gi,
  /\b(d+[a*@]+m+n+)\b/gi,
  /\b(h+e+l+l+)\b/gi,
  /\b(c+r+[a*@]+p+)\b/gi,
];

// Slurs and hate speech (highest severity)
const SLUR_PATTERNS: RegExp[] = [
  // Patterns for various slurs - would be more comprehensive in production
  /\b(n+[i*@1]+[g*@]+[e*@3]+r+)\b/gi,
  /\b(f+[a*@]+[g*@]+[o*@]*[t*@]*)\b/gi,
  /\b(r+[e*@3]+t+[a*@]+r+d+)\b/gi,
];

// Spam patterns
const SPAM_PATTERNS: RegExp[] = [
  /(.)\1{5,}/gi, // Same character repeated 5+ times
  /\b(buy|sell|cheap|free|discount|click|visit)\b.*\b(now|here|link)\b/gi,
  /(https?:\/\/|www\.)[^\s]+/gi, // URLs
  /[A-Z]{10,}/g, // Long ALL CAPS
];

// Personal info patterns
const PERSONAL_INFO_PATTERNS: RegExp[] = [
  /\b\d{3}[-.\s]?\d{3}[-.\s]?\d{4}\b/g, // Phone numbers
  /\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b/gi, // Email
  /\b\d{3}[-.\s]?\d{2}[-.\s]?\d{4}\b/g, // SSN pattern
  /\b\d{16}\b/g, // Credit card pattern
];

// Leetspeak substitutions
const LEETSPEAK_MAP: Record<string, string> = {
  '@': 'a', '4': 'a', '^': 'a',
  '8': 'b',
  '(': 'c', '<': 'c',
  '3': 'e',
  '6': 'g', '9': 'g',
  '#': 'h',
  '1': 'i', '!': 'i', '|': 'i',
  '7': 'l', // 7 most commonly represents L in leetspeak
  '0': 'o',
  '5': 's', '$': 's',
  '+': 't',
  'v': 'u',
  '2': 'z',
};

// ============================================================================
// Moderation Functions
// ============================================================================

/**
 * Moderate content before posting
 * Returns moderation result with filtered content if applicable
 */
export const moderateContent = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  const userId = context.auth.uid;
  const content = data?.content;
  const contentType = data?.contentType || 'chat';

  if (!content || typeof content !== 'string') {
    throw new functions.https.HttpsError('invalid-argument', 'Content required');
  }

  // Check if user is muted
  const userRecord = await getUserModerationRecord(userId);
  if (isUserMuted(userRecord)) {
    return {
      approved: false,
      flags: [{ type: 'user_muted', severity: 'high', confidence: 1.0 }],
      toxicityScore: 0,
      autoAction: 'block',
      requiresReview: false,
      message: 'You are currently muted and cannot post content.'
    };
  }

  // Perform moderation
  const result = await analyzeContent(content, contentType);

  // Track moderation event
  await trackModerationEvent(userId, content, contentType, result);

  // Apply auto-actions if needed
  if (result.autoAction && result.autoAction !== 'none') {
    await applyAutoAction(userId, result, content, contentType);
  }

  return result;
});

/**
 * Analyze content for moderation issues
 */
async function analyzeContent(content: string, contentType: string): Promise<ModerationResult> {
  const flags: ModerationFlag[] = [];
  let filteredContent = content;
  let toxicityScore = 0;

  // Normalize content for detection (handle leetspeak)
  const normalizedContent = normalizeLeetspeak(content.toLowerCase());

  // Check for slurs (highest priority)
  for (const pattern of SLUR_PATTERNS) {
    if (pattern.test(content) || pattern.test(normalizedContent)) {
      flags.push({
        type: 'slur',
        severity: 'critical',
        matchedPattern: pattern.source,
        confidence: 0.95
      });
      toxicityScore += 50;
      // Replace with asterisks
      filteredContent = filteredContent.replace(pattern, (match) => '*'.repeat(match.length));
    }
  }

  // Check for profanity
  for (const pattern of PROFANITY_PATTERNS) {
    if (pattern.test(content) || pattern.test(normalizedContent)) {
      flags.push({
        type: 'profanity',
        severity: 'medium',
        matchedPattern: pattern.source,
        confidence: 0.85
      });
      toxicityScore += 15;
      filteredContent = filteredContent.replace(pattern, (match) => '*'.repeat(match.length));
    }
  }

  // Check for spam
  for (const pattern of SPAM_PATTERNS) {
    const matches = content.match(pattern);
    if (matches && matches.length > 0) {
      flags.push({
        type: 'spam',
        severity: matches.length > 2 ? 'high' : 'low',
        matchedPattern: pattern.source,
        confidence: 0.7
      });
      toxicityScore += matches.length > 2 ? 20 : 5;
    }
  }

  // Check for personal info
  for (const pattern of PERSONAL_INFO_PATTERNS) {
    if (pattern.test(content)) {
      flags.push({
        type: 'personal_info',
        severity: 'high',
        matchedPattern: pattern.source,
        confidence: 0.9
      });
      toxicityScore += 25;
      filteredContent = filteredContent.replace(pattern, '[REDACTED]');
    }
  }

  // Check message length and caps ratio
  const capsRatio = (content.match(/[A-Z]/g) || []).length / content.length;
  if (content.length > 5 && capsRatio > 0.7) {
    flags.push({
      type: 'spam',
      severity: 'low',
      matchedPattern: 'excessive_caps',
      confidence: 0.6
    });
    toxicityScore += 5;
  }

  // Calculate toxicity score (0-100)
  toxicityScore = Math.min(100, toxicityScore);

  // Determine auto-action
  let autoAction: ModerationResult['autoAction'] = 'none';
  let requiresReview = false;

  if (toxicityScore >= 80) {
    autoAction = 'block';
    requiresReview = true;
  } else if (toxicityScore >= 50) {
    autoAction = 'warn';
    requiresReview = true;
  } else if (toxicityScore >= 20) {
    autoAction = 'filter';
  }

  // For usernames and alliance names, be stricter
  if (contentType === 'username' || contentType === 'alliance_name') {
    if (flags.length > 0) {
      autoAction = 'block';
    }
  }

  return {
    approved: autoAction !== 'block',
    filteredContent: filteredContent !== content ? filteredContent : undefined,
    flags,
    toxicityScore,
    autoAction,
    requiresReview
  };
}

/**
 * Normalize leetspeak to regular text
 */
function normalizeLeetspeak(text: string): string {
  let normalized = text;
  for (const [leet, char] of Object.entries(LEETSPEAK_MAP)) {
    normalized = normalized.replace(new RegExp(`\\${leet}`, 'g'), char);
  }
  return normalized;
}

/**
 * Report content for moderation review
 */
export const reportContent = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  const reporterId = context.auth.uid;
  const { reportedUserId, contentType, contentId, content, reason, description } = data;

  if (!reportedUserId || !contentType || !reason) {
    throw new functions.https.HttpsError('invalid-argument', 'Missing required fields');
  }

  // Prevent self-reports
  if (reporterId === reportedUserId) {
    throw new functions.https.HttpsError('invalid-argument', 'Cannot report yourself');
  }

  // Check for duplicate reports
  const existingReport = await db.collection('content_reports')
    .where('reporterId', '==', reporterId)
    .where('contentId', '==', contentId)
    .where('status', '==', 'pending')
    .limit(1)
    .get();

  if (!existingReport.empty) {
    throw new functions.https.HttpsError('already-exists', 'You already reported this content');
  }

  // Check reporter's false report count
  const reporterRecord = await getUserModerationRecord(reporterId);
  if (reporterRecord.falseReportCount >= 5) {
    throw new functions.https.HttpsError(
      'permission-denied', 
      'Your reporting privileges are restricted due to multiple false reports'
    );
  }

  // Create report
  const reportRef = db.collection('content_reports').doc();
  const report: ContentReport = {
    id: reportRef.id,
    reporterId,
    reportedUserId,
    contentType,
    contentId: contentId || '',
    content: content || '',
    reason,
    description,
    status: 'pending',
    createdAt: admin.firestore.Timestamp.now()
  };

  await reportRef.set(report);

  // Update reported user's record
  await db.collection('moderation_records').doc(reportedUserId).set({
    totalReportsAgainst: admin.firestore.FieldValue.increment(1),
    lastUpdated: admin.firestore.Timestamp.now()
  }, { merge: true });

  // Update reporter's record
  await db.collection('moderation_records').doc(reporterId).set({
    totalReportsMade: admin.firestore.FieldValue.increment(1),
    lastUpdated: admin.firestore.Timestamp.now()
  }, { merge: true });

  // If many reports against same content, auto-escalate
  const reportsAgainstContent = await db.collection('content_reports')
    .where('contentId', '==', contentId)
    .where('status', '==', 'pending')
    .get();

  if (reportsAgainstContent.size >= 3) {
    // Auto-hide content pending review
    await hideContentPendingReview(contentType, contentId);
    await reportRef.update({ status: 'reviewing' });
  }

  return {
    success: true,
    reportId: report.id,
    message: 'Report submitted successfully. Thank you for helping keep the community safe.'
  };
});

/**
 * Admin: Review and resolve a report
 */
export const resolveReport = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  // Check admin status
  const adminDoc = await db.collection('users').doc(context.auth.uid).get();
  if (!adminDoc.exists || !adminDoc.data()?.isAdmin && !adminDoc.data()?.isModerator) {
    throw new functions.https.HttpsError('permission-denied', 'Moderator access required');
  }

  const { reportId, resolution, notes } = data;

  if (!reportId || !resolution) {
    throw new functions.https.HttpsError('invalid-argument', 'Report ID and resolution required');
  }

  const reportRef = db.collection('content_reports').doc(reportId);
  const reportDoc = await reportRef.get();

  if (!reportDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Report not found');
  }

  const report = reportDoc.data() as ContentReport;

  // Update report
  await reportRef.update({
    status: 'resolved',
    resolution,
    resolvedBy: context.auth.uid,
    resolvedAt: admin.firestore.Timestamp.now(),
    notes
  });

  // Apply resolution
  switch (resolution) {
    case 'content_removed':
      await removeContent(report.contentType, report.contentId);
      break;
    case 'warning':
      await issueWarning(report.reportedUserId, `Content violation: ${report.reason}`, report.contentId);
      break;
    case 'mute':
      await muteUserInternal(report.reportedUserId, 24 * 60, `Content violation: ${report.reason}`);
      break;
    case 'ban':
      await banUser(report.reportedUserId, `Severe content violation: ${report.reason}`, 7);
      break;
    case 'no_action':
      // Mark as false report if pattern suggests abuse
      // Check if reporter has many dismissed reports
      const dismissedReports = await db.collection('content_reports')
        .where('reporterId', '==', report.reporterId)
        .where('resolution', '==', 'no_action')
        .get();
      if (dismissedReports.size >= 3) {
        await db.collection('moderation_records').doc(report.reporterId).update({
          falseReportCount: admin.firestore.FieldValue.increment(1)
        });
      }
      break;
  }

  return {
    success: true,
    message: `Report resolved with action: ${resolution}`
  };
});

/**
 * Get pending reports (admin only)
 */
export const getPendingReports = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  // Check admin status
  const adminDoc = await db.collection('users').doc(context.auth.uid).get();
  if (!adminDoc.exists || !adminDoc.data()?.isAdmin && !adminDoc.data()?.isModerator) {
    throw new functions.https.HttpsError('permission-denied', 'Moderator access required');
  }

  const status = data?.status || 'pending';
  const limit = Math.min(data?.limit || 50, 100);

  const reports = await db.collection('content_reports')
    .where('status', '==', status)
    .orderBy('createdAt', 'desc')
    .limit(limit)
    .get();

  return {
    reports: reports.docs.map(doc => ({
      ...doc.data(),
      createdAt: doc.data().createdAt?.toDate()?.toISOString()
    })),
    total: reports.size
  };
});

/**
 * Issue a warning to a user
 */
export const issueWarning = async (
  userId: string, 
  reason: string, 
  contentId?: string,
  issuedBy: string = 'system'
): Promise<void> => {
  const warning: Warning = {
    id: db.collection('_').doc().id,
    reason,
    issuedAt: admin.firestore.Timestamp.now(),
    issuedBy,
    contentId,
    acknowledged: false
  };

  await db.collection('moderation_records').doc(userId).set({
    warnings: admin.firestore.FieldValue.arrayUnion(warning),
    lastUpdated: admin.firestore.Timestamp.now()
  }, { merge: true });

  // Notify user
  await db.collection('notifications').add({
    userId,
    type: 'moderation_warning',
    title: 'Warning',
    message: `You received a warning: ${reason}`,
    data: { warningId: warning.id },
    read: false,
    createdAt: admin.firestore.Timestamp.now()
  });

  // Check if too many warnings - auto-escalate
  const record = await getUserModerationRecord(userId);
  const recentWarnings = record.warnings.filter(w => {
    const warningTime = w.issuedAt.toDate().getTime();
    const weekAgo = Date.now() - 7 * 24 * 60 * 60 * 1000;
    return warningTime > weekAgo;
  });

  if (recentWarnings.length >= 3) {
    await muteUserInternal(userId, 60, 'Multiple warnings in short period');
  }
};

/**
 * Mute a user
 */
export const muteUser = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  // Check admin status
  const adminDoc = await db.collection('users').doc(context.auth.uid).get();
  if (!adminDoc.exists || !adminDoc.data()?.isAdmin && !adminDoc.data()?.isModerator) {
    throw new functions.https.HttpsError('permission-denied', 'Moderator access required');
  }

  const { userId, durationMinutes, reason } = data;

  if (!userId || !durationMinutes || !reason) {
    throw new functions.https.HttpsError('invalid-argument', 'Missing required fields');
  }

  await muteUserInternal(userId, durationMinutes, reason, context.auth.uid);

  return { success: true, message: `User muted for ${durationMinutes} minutes` };
});

async function muteUserInternal(
  userId: string, 
  durationMinutes: number, 
  reason: string,
  issuedBy: string = 'system'
): Promise<void> {
  const now = admin.firestore.Timestamp.now();
  const expiresAt = admin.firestore.Timestamp.fromDate(
    new Date(now.toDate().getTime() + durationMinutes * 60 * 1000)
  );

  const mute: Mute = {
    id: db.collection('_').doc().id,
    reason,
    startedAt: now,
    expiresAt,
    issuedBy,
    active: true
  };

  await db.collection('moderation_records').doc(userId).set({
    mutes: admin.firestore.FieldValue.arrayUnion(mute),
    lastUpdated: now
  }, { merge: true });

  // Notify user
  await db.collection('notifications').add({
    userId,
    type: 'moderation_mute',
    title: 'You have been muted',
    message: `You are muted for ${durationMinutes} minutes. Reason: ${reason}`,
    data: { muteId: mute.id, expiresAt: expiresAt.toDate().toISOString() },
    read: false,
    createdAt: now
  });
}

/**
 * Ban a user
 */
export const banUserFunction = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  // Check admin status
  const adminDoc = await db.collection('users').doc(context.auth.uid).get();
  if (!adminDoc.exists || !adminDoc.data()?.isAdmin) {
    throw new functions.https.HttpsError('permission-denied', 'Admin access required');
  }

  const { userId, reason, durationDays } = data;

  if (!userId || !reason) {
    throw new functions.https.HttpsError('invalid-argument', 'User ID and reason required');
  }

  await banUser(userId, reason, durationDays, context.auth.uid);

  return { 
    success: true, 
    message: durationDays ? `User banned for ${durationDays} days` : 'User permanently banned' 
  };
});

async function banUser(
  userId: string, 
  reason: string, 
  durationDays?: number,
  issuedBy: string = 'system'
): Promise<void> {
  const now = admin.firestore.Timestamp.now();
  const expiresAt = durationDays 
    ? admin.firestore.Timestamp.fromDate(
        new Date(now.toDate().getTime() + durationDays * 24 * 60 * 60 * 1000)
      )
    : undefined;

  const ban: Ban = {
    id: db.collection('_').doc().id,
    reason,
    startedAt: now,
    expiresAt,
    issuedBy,
    appealed: false
  };

  await db.collection('moderation_records').doc(userId).set({
    bans: admin.firestore.FieldValue.arrayUnion(ban),
    trustLevel: 'restricted',
    lastUpdated: now
  }, { merge: true });

  // Update user document
  await db.collection('users').doc(userId).update({
    banned: true,
    banExpiresAt: expiresAt || null,
    banReason: reason
  });

  // Send email notification (would integrate with email service)
  console.log(`User ${userId} banned: ${reason}`);
}

/**
 * Appeal a ban
 */
export const appealBan = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  const userId = context.auth.uid;
  const { banId, reason } = data;

  if (!banId || !reason) {
    throw new functions.https.HttpsError('invalid-argument', 'Ban ID and appeal reason required');
  }

  const recordDoc = await db.collection('moderation_records').doc(userId).get();
  if (!recordDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'No moderation record found');
  }

  const record = recordDoc.data() as UserModerationRecord;
  const ban = record.bans.find(b => b.id === banId);

  if (!ban) {
    throw new functions.https.HttpsError('not-found', 'Ban not found');
  }

  if (ban.appealed) {
    throw new functions.https.HttpsError('already-exists', 'Ban already appealed');
  }

  // Create appeal
  await db.collection('ban_appeals').add({
    userId,
    banId,
    reason,
    status: 'pending',
    createdAt: admin.firestore.Timestamp.now()
  });

  // Mark ban as appealed
  const updatedBans = record.bans.map(b => 
    b.id === banId ? { ...b, appealed: true, appealStatus: 'pending' } : b
  );
  
  await db.collection('moderation_records').doc(userId).update({
    bans: updatedBans
  });

  return {
    success: true,
    message: 'Appeal submitted. You will be notified of the decision.'
  };
});

// ============================================================================
// Helper Functions
// ============================================================================

async function getUserModerationRecord(userId: string): Promise<UserModerationRecord> {
  const doc = await db.collection('moderation_records').doc(userId).get();
  
  if (!doc.exists) {
    return {
      userId,
      warnings: [],
      mutes: [],
      bans: [],
      totalReportsAgainst: 0,
      totalReportsMade: 0,
      falseReportCount: 0,
      trustLevel: 'new',
      lastUpdated: admin.firestore.Timestamp.now()
    };
  }

  return doc.data() as UserModerationRecord;
}

function isUserMuted(record: UserModerationRecord): boolean {
  const now = Date.now();
  return record.mutes.some(mute => 
    mute.active && mute.expiresAt.toDate().getTime() > now
  );
}

function isUserBanned(record: UserModerationRecord): boolean {
  const now = Date.now();
  return record.bans.some(ban => 
    !ban.expiresAt || ban.expiresAt.toDate().getTime() > now
  );
}

async function trackModerationEvent(
  userId: string, 
  content: string, 
  contentType: string, 
  result: ModerationResult
): Promise<void> {
  await db.collection('moderation_events').add({
    userId,
    contentPreview: content.substring(0, 100),
    contentType,
    flags: result.flags,
    toxicityScore: result.toxicityScore,
    autoAction: result.autoAction,
    timestamp: admin.firestore.Timestamp.now()
  });
}

async function applyAutoAction(
  userId: string, 
  result: ModerationResult, 
  content: string,
  contentType: string
): Promise<void> {
  switch (result.autoAction) {
    case 'warn':
      const flagTypes = result.flags.map(f => f.type).join(', ');
      await issueWarning(userId, `Auto-detected violation: ${flagTypes}`);
      break;
    case 'mute':
      await muteUserInternal(userId, 30, 'Auto-mute for severe content violation');
      break;
    case 'ban':
      await banUser(userId, 'Auto-ban for critical content violation', 1);
      break;
  }
}

async function hideContentPendingReview(contentType: string, contentId: string): Promise<void> {
  // Hide the content while pending review
  switch (contentType) {
    case 'chat':
      await db.collection('chat_messages').doc(contentId).update({
        hidden: true,
        hiddenReason: 'pending_review'
      });
      break;
    case 'building_name':
      await db.collection('buildings').doc(contentId).update({
        nameHidden: true
      });
      break;
    // Add other content types as needed
  }
}

async function removeContent(contentType: string, contentId: string): Promise<void> {
  switch (contentType) {
    case 'chat':
      await db.collection('chat_messages').doc(contentId).update({
        deleted: true,
        content: '[Content removed by moderator]'
      });
      break;
    case 'username':
      await db.collection('users').doc(contentId).update({
        displayName: 'User' + contentId.substring(0, 6)
      });
      break;
    case 'alliance_name':
      await db.collection('alliances').doc(contentId).update({
        name: 'Alliance ' + contentId.substring(0, 6)
      });
      break;
    case 'building_name':
      await db.collection('buildings').doc(contentId).update({
        name: 'Building'
      });
      break;
  }
}

/**
 * Scheduled: Clean up expired mutes
 */
export const cleanupExpiredMutes = functions.pubsub
  .schedule('0 * * * *') // Every hour
  .onRun(async () => {
    const now = admin.firestore.Timestamp.now();
    
    const records = await db.collection('moderation_records')
      .where('mutes', '!=', [])
      .get();

    let cleaned = 0;
    for (const doc of records.docs) {
      const record = doc.data() as UserModerationRecord;
      const activeMutes = record.mutes.filter(m => 
        m.active && m.expiresAt.toDate().getTime() > now.toDate().getTime()
      );
      
      if (activeMutes.length !== record.mutes.filter(m => m.active).length) {
        // Some mutes expired, update
        const updatedMutes = record.mutes.map(m => ({
          ...m,
          active: m.expiresAt.toDate().getTime() > now.toDate().getTime()
        }));
        
        await doc.ref.update({ mutes: updatedMutes });
        cleaned++;
      }
    }

    console.log(`Cleaned up ${cleaned} expired mutes`);
  });

/**
 * Check if user is banned (callable for login check)
 */
export const checkBanStatus = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  const userId = context.auth.uid;
  const record = await getUserModerationRecord(userId);
  const banned = isUserBanned(record);

  if (banned) {
    const activeBan = record.bans.find(b => 
      !b.expiresAt || b.expiresAt.toDate().getTime() > Date.now()
    );

    return {
      banned: true,
      reason: activeBan?.reason,
      expiresAt: activeBan?.expiresAt?.toDate()?.toISOString(),
      permanent: !activeBan?.expiresAt,
      canAppeal: activeBan && !activeBan.appealed
    };
  }

  return { banned: false };
});
