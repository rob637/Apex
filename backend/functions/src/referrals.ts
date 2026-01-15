/**
 * Apex Citadels - Referral & Viral Growth System
 * 
 * Growth engine for user acquisition:
 * - Referral codes
 * - Invite rewards
 * - Viral challenges
 * - Share incentives
 */

import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';
import { sendNotification } from './notifications';
import { awardSeasonXp } from './season-pass';

const db = admin.firestore();

// ============================================================================
// Types
// ============================================================================

export interface ReferralCode {
  code: string;
  userId: string;
  username: string;
  createdAt: admin.firestore.Timestamp;
  
  // Stats
  totalUses: number;
  successfulReferrals: number; // Reached level 5
  
  // Rewards tracking
  rewardsEarned: number;
  rewardsClaimed: number;
  pendingRewards: number;
  
  // Milestones
  milestones: ReferralMilestone[];
}

export interface ReferralMilestone {
  referralCount: number;
  reward: {
    gems: number;
    title?: string;
    badge?: string;
    exclusiveReward?: string;
  };
  claimed: boolean;
  claimedAt?: admin.firestore.Timestamp;
}

export interface ReferralUse {
  id: string;
  code: string;
  referrerId: string;
  referredUserId: string;
  referredUsername: string;
  usedAt: admin.firestore.Timestamp;
  
  // Progress
  currentLevel: number;
  qualified: boolean; // Reached level 5
  qualifiedAt?: admin.firestore.Timestamp;
  
  // Rewards
  referrerRewardClaimed: boolean;
  referredRewardClaimed: boolean;
}

export interface ShareReward {
  id: string;
  userId: string;
  platform: 'twitter' | 'facebook' | 'instagram' | 'tiktok' | 'copy_link';
  shareType: 'conquest' | 'achievement' | 'level_up' | 'territory' | 'alliance' | 'general';
  contentId?: string;
  sharedAt: admin.firestore.Timestamp;
  
  // Verification
  verified: boolean;
  rewardClaimed: boolean;
}

export interface ViralChallenge {
  id: string;
  name: string;
  description: string;
  type: 'invite_friends' | 'share_content' | 'play_with_friends' | 'alliance_recruitment';
  
  // Requirements
  requirement: {
    action: string;
    target: number;
    timeframeHours?: number;
  };
  
  // Rewards
  rewards: {
    gems: number;
    seasonXp: number;
    title?: string;
    badge?: string;
  };
  
  // Timing
  startDate: admin.firestore.Timestamp;
  endDate: admin.firestore.Timestamp;
  status: 'upcoming' | 'active' | 'ended';
}

// ============================================================================
// Referral Milestones
// ============================================================================

const REFERRAL_MILESTONES: ReferralMilestone[] = [
  { referralCount: 1, reward: { gems: 100 }, claimed: false },
  { referralCount: 3, reward: { gems: 250, badge: 'referral_starter' }, claimed: false },
  { referralCount: 5, reward: { gems: 500, title: 'Recruiter' }, claimed: false },
  { referralCount: 10, reward: { gems: 1000, badge: 'referral_pro' }, claimed: false },
  { referralCount: 25, reward: { gems: 2500, title: 'Ambassador' }, claimed: false },
  { referralCount: 50, reward: { gems: 5000, badge: 'referral_master', exclusiveReward: 'golden_referral_banner' }, claimed: false },
  { referralCount: 100, reward: { gems: 10000, title: 'Legend', badge: 'referral_legend' }, claimed: false }
];

// ============================================================================
// Generate Referral Code
// ============================================================================

export const generateReferralCode = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const userId = context.auth.uid;
  
  // Check if user already has a code
  const existingCode = await db.collection('referral_codes')
    .where('userId', '==', userId)
    .limit(1)
    .get();
  
  if (!existingCode.empty) {
    return { code: existingCode.docs[0].data().code };
  }
  
  // Get user info
  const userDoc = await db.collection('users').doc(userId).get();
  if (!userDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'User not found');
  }
  const user = userDoc.data()!;
  
  // Generate unique code
  const code = await generateUniqueCode(user.username);
  
  const referralCode: ReferralCode = {
    code,
    userId,
    username: user.username,
    createdAt: admin.firestore.Timestamp.now(),
    totalUses: 0,
    successfulReferrals: 0,
    rewardsEarned: 0,
    rewardsClaimed: 0,
    pendingRewards: 0,
    milestones: REFERRAL_MILESTONES.map(m => ({ ...m }))
  };
  
  await db.collection('referral_codes').doc(code).set(referralCode);
  
  return { code };
});

async function generateUniqueCode(username: string): Promise<string> {
  // Try username-based code first
  const cleanUsername = username.replace(/[^a-zA-Z0-9]/g, '').toUpperCase().substring(0, 6);
  
  for (let attempt = 0; attempt < 10; attempt++) {
    const suffix = attempt === 0 ? '' : String(attempt);
    const code = cleanUsername + suffix;
    
    const existing = await db.collection('referral_codes').doc(code).get();
    if (!existing.exists) {
      return code;
    }
  }
  
  // Fallback to random code
  const randomPart = Math.random().toString(36).substring(2, 8).toUpperCase();
  return randomPart;
}

// ============================================================================
// Use Referral Code
// ============================================================================

export const useReferralCode = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { code } = data as { code: string };
  const userId = context.auth.uid;
  
  // Get user info
  const userDoc = await db.collection('users').doc(userId).get();
  if (!userDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'User not found');
  }
  const user = userDoc.data()!;
  
  // Check if user already used a referral code
  const existingUse = await db.collection('referral_uses')
    .where('referredUserId', '==', userId)
    .limit(1)
    .get();
  
  if (!existingUse.empty) {
    throw new functions.https.HttpsError('already-exists', 'Already used a referral code');
  }
  
  // Get referral code
  const codeDoc = await db.collection('referral_codes').doc(code.toUpperCase()).get();
  if (!codeDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Invalid referral code');
  }
  
  const referralCode = codeDoc.data() as ReferralCode;
  
  // Can't refer yourself
  if (referralCode.userId === userId) {
    throw new functions.https.HttpsError('invalid-argument', 'Cannot use your own code');
  }
  
  // Record the use
  const useRef = db.collection('referral_uses').doc();
  const use: ReferralUse = {
    id: useRef.id,
    code,
    referrerId: referralCode.userId,
    referredUserId: userId,
    referredUsername: user.username,
    usedAt: admin.firestore.Timestamp.now(),
    currentLevel: user.level || 1,
    qualified: false,
    referrerRewardClaimed: false,
    referredRewardClaimed: false
  };
  
  await db.runTransaction(async (transaction) => {
    // Create use record
    transaction.set(useRef, use);
    
    // Update code stats
    transaction.update(codeDoc.ref, {
      totalUses: admin.firestore.FieldValue.increment(1)
    });
    
    // Mark user as referred
    transaction.update(db.collection('users').doc(userId), {
      referredBy: referralCode.userId,
      referralCode: code
    });
    
    // Instant reward for new user: 50 gems
    transaction.update(db.collection('users').doc(userId), {
      'resources.gems': admin.firestore.FieldValue.increment(50)
    });
  });
  
  // Notify referrer
  await sendNotification(referralCode.userId, {
    type: 'referral_used',
    title: 'New Referral! 🎉',
    body: `${user.username} joined using your code!`,
    data: { referredUserId: userId }
  });
  
  return { 
    success: true, 
    referrerName: referralCode.username,
    instantReward: { gems: 50 }
  };
});

// ============================================================================
// Check Referral Qualification (Called on level up)
// ============================================================================

export async function checkReferralQualification(userId: string, newLevel: number): Promise<void> {
  if (newLevel < 5) return; // Must reach level 5 to qualify
  
  const useSnapshot = await db.collection('referral_uses')
    .where('referredUserId', '==', userId)
    .where('qualified', '==', false)
    .limit(1)
    .get();
  
  if (useSnapshot.empty) return;
  
  const use = useSnapshot.docs[0];
  const useData = use.data() as ReferralUse;
  
  // Update use as qualified
  await use.ref.update({
    qualified: true,
    qualifiedAt: admin.firestore.Timestamp.now(),
    currentLevel: newLevel
  });
  
  // Update referrer's code
  const codeDoc = await db.collection('referral_codes').doc(useData.code).get();
  if (codeDoc.exists) {
    await codeDoc.ref.update({
      successfulReferrals: admin.firestore.FieldValue.increment(1),
      pendingRewards: admin.firestore.FieldValue.increment(100) // 100 gems per qualified referral
    });
  }
  
  // Notify referrer
  await sendNotification(useData.referrerId, {
    type: 'referral_qualified',
    title: 'Referral Reward Ready! 💰',
    body: `${useData.referredUsername} reached level 5! Claim your 100 gems.`,
    data: { referredUserId: userId }
  });
}

// ============================================================================
// Claim Referral Rewards
// ============================================================================

export const claimReferralRewards = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const userId = context.auth.uid;
  
  // Get user's referral code
  const codeSnapshot = await db.collection('referral_codes')
    .where('userId', '==', userId)
    .limit(1)
    .get();
  
  if (codeSnapshot.empty) {
    throw new functions.https.HttpsError('not-found', 'No referral code found');
  }
  
  const codeDoc = codeSnapshot.docs[0];
  const code = codeDoc.data() as ReferralCode;
  
  if (code.pendingRewards <= 0) {
    throw new functions.https.HttpsError('failed-precondition', 'No pending rewards');
  }
  
  const rewardsToClaim = code.pendingRewards;
  
  await db.runTransaction(async (transaction) => {
    // Award gems
    transaction.update(db.collection('users').doc(userId), {
      'resources.gems': admin.firestore.FieldValue.increment(rewardsToClaim)
    });
    
    // Update code
    transaction.update(codeDoc.ref, {
      rewardsClaimed: admin.firestore.FieldValue.increment(rewardsToClaim),
      rewardsEarned: admin.firestore.FieldValue.increment(rewardsToClaim),
      pendingRewards: 0
    });
  });
  
  return { success: true, gemsClaimed: rewardsToClaim };
});

// ============================================================================
// Claim Milestone Reward
// ============================================================================

export const claimMilestoneReward = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { milestoneCount } = data as { milestoneCount: number };
  const userId = context.auth.uid;
  
  // Get user's referral code
  const codeSnapshot = await db.collection('referral_codes')
    .where('userId', '==', userId)
    .limit(1)
    .get();
  
  if (codeSnapshot.empty) {
    throw new functions.https.HttpsError('not-found', 'No referral code found');
  }
  
  const codeDoc = codeSnapshot.docs[0];
  const code = codeDoc.data() as ReferralCode;
  
  // Find milestone
  const milestoneIndex = code.milestones.findIndex(m => m.referralCount === milestoneCount);
  if (milestoneIndex === -1) {
    throw new functions.https.HttpsError('not-found', 'Milestone not found');
  }
  
  const milestone = code.milestones[milestoneIndex];
  
  if (milestone.claimed) {
    throw new functions.https.HttpsError('already-exists', 'Milestone already claimed');
  }
  
  if (code.successfulReferrals < milestone.referralCount) {
    throw new functions.https.HttpsError(
      'failed-precondition', 
      `Need ${milestone.referralCount} referrals, have ${code.successfulReferrals}`
    );
  }
  
  // Award rewards
  const userUpdate: Record<string, unknown> = {};
  userUpdate['resources.gems'] = admin.firestore.FieldValue.increment(milestone.reward.gems);
  
  if (milestone.reward.title) {
    userUpdate['unlockedTitles'] = admin.firestore.FieldValue.arrayUnion(milestone.reward.title);
  }
  if (milestone.reward.badge) {
    userUpdate['unlockedBadges'] = admin.firestore.FieldValue.arrayUnion(milestone.reward.badge);
  }
  if (milestone.reward.exclusiveReward) {
    userUpdate['unlockedExclusives'] = admin.firestore.FieldValue.arrayUnion(milestone.reward.exclusiveReward);
  }
  
  // Update milestone as claimed
  code.milestones[milestoneIndex].claimed = true;
  code.milestones[milestoneIndex].claimedAt = admin.firestore.Timestamp.now();
  
  await db.runTransaction(async (transaction) => {
    transaction.update(db.collection('users').doc(userId), userUpdate);
    transaction.update(codeDoc.ref, {
      milestones: code.milestones
    });
  });
  
  return { success: true, reward: milestone.reward };
});

// ============================================================================
// Record Share
// ============================================================================

export const recordShare = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { platform, shareType, contentId } = data as {
    platform: ShareReward['platform'];
    shareType: ShareReward['shareType'];
    contentId?: string;
  };
  const userId = context.auth.uid;
  
  // Check daily share limit (5 per day)
  const today = new Date().toISOString().split('T')[0];
  const sharesSnapshot = await db.collection('share_rewards')
    .where('userId', '==', userId)
    .where('sharedAt', '>=', admin.firestore.Timestamp.fromDate(new Date(today)))
    .get();
  
  if (sharesSnapshot.size >= 5) {
    return { success: true, rewardClaimed: false, message: 'Daily share limit reached' };
  }
  
  // Record share
  const shareRef = db.collection('share_rewards').doc();
  const share: ShareReward = {
    id: shareRef.id,
    userId,
    platform,
    shareType,
    contentId,
    sharedAt: admin.firestore.Timestamp.now(),
    verified: true, // Auto-verify for now
    rewardClaimed: true
  };
  
  // Award small reward (10 gems per share)
  await db.runTransaction(async (transaction) => {
    transaction.set(shareRef, share);
    transaction.update(db.collection('users').doc(userId), {
      'resources.gems': admin.firestore.FieldValue.increment(10),
      'stats.totalShares': admin.firestore.FieldValue.increment(1)
    });
  });
  
  // Award season XP
  await awardSeasonXp(userId, 25, `Share on ${platform}`);
  
  return { 
    success: true, 
    rewardClaimed: true, 
    reward: { gems: 10, seasonXp: 25 },
    sharesRemaining: 4 - sharesSnapshot.size
  };
});

// ============================================================================
// Get Referral Stats
// ============================================================================

export const getReferralStats = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const userId = context.auth.uid;
  
  // Get user's referral code
  const codeSnapshot = await db.collection('referral_codes')
    .where('userId', '==', userId)
    .limit(1)
    .get();
  
  let code: ReferralCode | null = null;
  if (!codeSnapshot.empty) {
    code = codeSnapshot.docs[0].data() as ReferralCode;
  }
  
  // Get recent referrals
  let recentReferrals: Array<{
    username: string;
    level: number;
    qualified: boolean;
    usedAt: admin.firestore.Timestamp;
  }> = [];
  
  if (code) {
    const referralsSnapshot = await db.collection('referral_uses')
      .where('referrerId', '==', userId)
      .orderBy('usedAt', 'desc')
      .limit(10)
      .get();
    
    recentReferrals = referralsSnapshot.docs.map(doc => {
      const d = doc.data() as ReferralUse;
      return {
        username: d.referredUsername,
        level: d.currentLevel,
        qualified: d.qualified,
        usedAt: d.usedAt
      };
    });
  }
  
  // Get next milestone
  let nextMilestone: ReferralMilestone | null = null;
  if (code) {
    nextMilestone = code.milestones.find(m => !m.claimed && m.referralCount > code!.successfulReferrals) || null;
  }
  
  return {
    code: code?.code || null,
    totalUses: code?.totalUses || 0,
    successfulReferrals: code?.successfulReferrals || 0,
    pendingRewards: code?.pendingRewards || 0,
    totalEarned: code?.rewardsEarned || 0,
    recentReferrals,
    milestones: code?.milestones || REFERRAL_MILESTONES,
    nextMilestone
  };
});

// ============================================================================
// Viral Challenges
// ============================================================================

export const getActiveViralChallenges = functions.https.onCall(async (data, context) => {
  const now = admin.firestore.Timestamp.now();
  
  const challengesSnapshot = await db.collection('viral_challenges')
    .where('status', '==', 'active')
    .where('endDate', '>', now)
    .get();
  
  const challenges = challengesSnapshot.docs.map(doc => doc.data());
  
  // Get user progress if authenticated
  let userProgress: Record<string, number> = {};
  if (context.auth) {
    const progressSnapshot = await db.collection('viral_challenge_progress')
      .where('userId', '==', context.auth.uid)
      .get();
    
    progressSnapshot.docs.forEach(doc => {
      const d = doc.data();
      userProgress[d.challengeId] = d.progress;
    });
  }
  
  return { 
    challenges: challenges.map(c => ({
      ...c,
      userProgress: userProgress[c.id] || 0
    }))
  };
});

// ============================================================================
// Generate Share Link
// ============================================================================

export const generateShareLink = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { shareType, contentId, content } = data as {
    shareType: string;
    contentId?: string;
    content?: string;
  };
  const userId = context.auth.uid;
  
  // Get referral code
  const codeSnapshot = await db.collection('referral_codes')
    .where('userId', '==', userId)
    .limit(1)
    .get();
  
  let referralCode = '';
  if (!codeSnapshot.empty) {
    referralCode = codeSnapshot.docs[0].data().code;
  }
  
  // Generate dynamic link (in production, use Firebase Dynamic Links)
  const baseUrl = 'https://apexcitadels.page.link';
  const params = new URLSearchParams({
    type: shareType,
    ref: referralCode,
    ...(contentId && { id: contentId })
  });
  
  const link = `${baseUrl}?${params.toString()}`;
  
  // Generate share messages
  const messages: Record<string, string> = {
    general: `Join me in Apex Citadels! Build, battle, and conquer in AR! 🏰⚔️\n\nUse my code ${referralCode} for bonus rewards!\n${link}`,
    conquest: `I just conquered a territory in Apex Citadels! 🏆\n\nJoin the battle: ${link}`,
    achievement: `Just unlocked an achievement in Apex Citadels! 🎖️\n\nCan you beat my score? ${link}`,
    level_up: `Level up! 📈 I'm now level ${content || '?'} in Apex Citadels!\n\n${link}`,
    territory: `Check out my territory in Apex Citadels! 🏰\n\n${link}`,
    alliance: `Our alliance is dominating in Apex Citadels! ⚔️\n\nJoin us: ${link}`
  };
  
  return { 
    link,
    referralCode,
    message: messages[shareType] || messages.general
  };
});
