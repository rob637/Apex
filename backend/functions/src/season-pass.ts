/**
 * Apex Citadels - Season Pass / Battle Pass System
 * 
 * The #1 monetization and engagement driver:
 * - Free and Premium tracks
 * - 100 levels per season
 * - Daily/Weekly/Seasonal challenges
 * - Exclusive cosmetics and rewards
 */

import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';
import { sendNotification } from './notifications';

const db = admin.firestore();

// ============================================================================
// Types
// ============================================================================

export interface Season {
  id: string;
  number: number;
  name: string;
  theme: string;
  description: string;
  startDate: admin.firestore.Timestamp;
  endDate: admin.firestore.Timestamp;
  status: 'upcoming' | 'active' | 'ended';
  
  // Pass configuration
  maxLevel: number;
  xpPerLevel: number;
  premiumPrice: number; // In gems
  premiumPlusPrice: number; // Premium + 25 level skip
  
  // Track rewards
  freeTrack: SeasonReward[];
  premiumTrack: SeasonReward[];
  
  // Challenges
  dailyChallenges: ChallengeTemplate[];
  weeklyChallenges: ChallengeTemplate[];
  seasonalChallenges: ChallengeTemplate[];
  
  createdAt: admin.firestore.Timestamp;
}

export interface SeasonReward {
  level: number;
  rewards: {
    xp?: number;
    gems?: number;
    stone?: number;
    wood?: number;
    iron?: number;
    crystal?: number;
    arcaneEssence?: number;
    cosmetic?: CosmeticReward;
  };
  isMilestone?: boolean; // Levels 25, 50, 75, 100
}

export interface CosmeticReward {
  type: 'title' | 'badge' | 'banner' | 'block_skin' | 'territory_effect' | 'avatar_frame';
  id: string;
  name: string;
  rarity: 'common' | 'rare' | 'epic' | 'legendary';
  preview?: string; // Image URL
}

export interface ChallengeTemplate {
  id: string;
  name: string;
  description: string;
  type: 'daily' | 'weekly' | 'seasonal';
  category: 'combat' | 'building' | 'social' | 'exploration' | 'resources';
  
  // Requirements
  requirement: {
    action: string; // e.g., 'attack_territory', 'place_block', 'claim_territory'
    target: number;
    conditions?: Record<string, unknown>;
  };
  
  // Rewards
  seasonXp: number;
  bonusRewards?: {
    gems?: number;
    resources?: Partial<Record<string, number>>;
  };
  
  // Difficulty
  difficulty: 'easy' | 'medium' | 'hard';
}

export interface UserSeasonProgress {
  userId: string;
  seasonId: string;
  currentLevel: number;
  currentXp: number;
  totalXpEarned: number;
  
  // Premium status
  hasPremium: boolean;
  premiumPurchasedAt?: admin.firestore.Timestamp;
  levelSkipsPurchased: number;
  
  // Claimed rewards
  claimedFreeLevels: number[];
  claimedPremiumLevels: number[];
  
  // Challenges
  activeDailyChallenges: ActiveChallenge[];
  activeWeeklyChallenges: ActiveChallenge[];
  activeSeasonalChallenges: ActiveChallenge[];
  completedChallengeIds: string[];
  
  // Stats
  challengesCompleted: number;
  lastPlayedAt: admin.firestore.Timestamp;
}

export interface ActiveChallenge {
  challengeId: string;
  templateId: string;
  name: string;
  description: string;
  progress: number;
  target: number;
  seasonXp: number;
  expiresAt?: admin.firestore.Timestamp;
  completedAt?: admin.firestore.Timestamp;
  claimed: boolean;
}

// ============================================================================
// Season Templates
// ============================================================================

const CHALLENGE_TEMPLATES: ChallengeTemplate[] = [
  // Daily - Easy
  {
    id: 'daily_claim_1',
    name: 'Land Grab',
    description: 'Claim 1 territory',
    type: 'daily',
    category: 'exploration',
    requirement: { action: 'claim_territory', target: 1 },
    seasonXp: 100,
    difficulty: 'easy'
  },
  {
    id: 'daily_attack_2',
    name: 'Raider',
    description: 'Attack 2 enemy territories',
    type: 'daily',
    category: 'combat',
    requirement: { action: 'attack_territory', target: 2 },
    seasonXp: 150,
    difficulty: 'easy'
  },
  {
    id: 'daily_build_10',
    name: 'Builder',
    description: 'Place 10 building blocks',
    type: 'daily',
    category: 'building',
    requirement: { action: 'place_block', target: 10 },
    seasonXp: 100,
    difficulty: 'easy'
  },
  {
    id: 'daily_harvest_5',
    name: 'Gatherer',
    description: 'Harvest 5 resource nodes',
    type: 'daily',
    category: 'resources',
    requirement: { action: 'harvest_resource', target: 5 },
    seasonXp: 100,
    difficulty: 'easy'
  },
  // Daily - Medium
  {
    id: 'daily_defend_1',
    name: 'Defender',
    description: 'Successfully defend a territory',
    type: 'daily',
    category: 'combat',
    requirement: { action: 'defend_territory', target: 1 },
    seasonXp: 200,
    difficulty: 'medium'
  },
  {
    id: 'daily_conquer_1',
    name: 'Conqueror',
    description: 'Conquer an enemy territory',
    type: 'daily',
    category: 'combat',
    requirement: { action: 'conquer_territory', target: 1 },
    seasonXp: 250,
    difficulty: 'medium'
  },
  // Weekly challenges
  {
    id: 'weekly_claim_10',
    name: 'Territory Baron',
    description: 'Claim 10 territories this week',
    type: 'weekly',
    category: 'exploration',
    requirement: { action: 'claim_territory', target: 10 },
    seasonXp: 500,
    bonusRewards: { gems: 25 },
    difficulty: 'medium'
  },
  {
    id: 'weekly_attack_20',
    name: 'War Machine',
    description: 'Attack 20 territories this week',
    type: 'weekly',
    category: 'combat',
    requirement: { action: 'attack_territory', target: 20 },
    seasonXp: 750,
    bonusRewards: { gems: 50 },
    difficulty: 'hard'
  },
  {
    id: 'weekly_build_100',
    name: 'Master Builder',
    description: 'Place 100 building blocks this week',
    type: 'weekly',
    category: 'building',
    requirement: { action: 'place_block', target: 100 },
    seasonXp: 600,
    bonusRewards: { gems: 30 },
    difficulty: 'medium'
  },
  {
    id: 'weekly_alliance_war',
    name: 'Alliance Warrior',
    description: 'Participate in an alliance war',
    type: 'weekly',
    category: 'social',
    requirement: { action: 'alliance_war_participate', target: 1 },
    seasonXp: 500,
    bonusRewards: { gems: 50 },
    difficulty: 'medium'
  },
  // Seasonal challenges
  {
    id: 'seasonal_level_50',
    name: 'Halfway There',
    description: 'Reach Season Pass level 50',
    type: 'seasonal',
    category: 'exploration',
    requirement: { action: 'reach_season_level', target: 50 },
    seasonXp: 2000,
    bonusRewards: { gems: 200 },
    difficulty: 'hard'
  },
  {
    id: 'seasonal_conquer_50',
    name: 'Empire Builder',
    description: 'Conquer 50 territories this season',
    type: 'seasonal',
    category: 'combat',
    requirement: { action: 'conquer_territory', target: 50 },
    seasonXp: 3000,
    bonusRewards: { gems: 300 },
    difficulty: 'hard'
  },
  {
    id: 'seasonal_all_daily',
    name: 'Dedicated Player',
    description: 'Complete all daily challenges for 30 days',
    type: 'seasonal',
    category: 'exploration',
    requirement: { action: 'complete_daily_challenges', target: 30 },
    seasonXp: 5000,
    bonusRewards: { gems: 500 },
    difficulty: 'hard'
  }
];

// ============================================================================
// Get Current Season
// ============================================================================

export const getCurrentSeason = functions.https.onCall(async (data, context) => {
  const now = admin.firestore.Timestamp.now();
  
  // Get active season
  const activeSeasonSnapshot = await db.collection('seasons')
    .where('status', '==', 'active')
    .limit(1)
    .get();
  
  if (activeSeasonSnapshot.empty) {
    return { season: null, userProgress: null };
  }
  
  const season = activeSeasonSnapshot.docs[0].data() as Season;
  
  // Get user progress if authenticated
  let userProgress: UserSeasonProgress | null = null;
  if (context.auth) {
    const progressDoc = await db.collection('season_progress')
      .doc(`${context.auth.uid}_${season.id}`)
      .get();
    
    if (progressDoc.exists) {
      userProgress = progressDoc.data() as UserSeasonProgress;
    } else {
      // Initialize progress
      userProgress = await initializeSeasonProgress(context.auth.uid, season);
    }
  }
  
  return { 
    season,
    userProgress,
    serverTime: now,
    daysRemaining: Math.ceil((season.endDate.toMillis() - now.toMillis()) / (1000 * 60 * 60 * 24))
  };
});

// ============================================================================
// Purchase Premium Pass
// ============================================================================

export const purchasePremiumPass = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { seasonId, includeLevelSkip = false } = data as { 
    seasonId: string; 
    includeLevelSkip?: boolean;
  };
  const userId = context.auth.uid;
  
  // Get season
  const seasonDoc = await db.collection('seasons').doc(seasonId).get();
  if (!seasonDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Season not found');
  }
  const season = seasonDoc.data() as Season;
  
  if (season.status !== 'active') {
    throw new functions.https.HttpsError('failed-precondition', 'Season is not active');
  }
  
  // Get user
  const userDoc = await db.collection('users').doc(userId).get();
  if (!userDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'User not found');
  }
  const user = userDoc.data();
  
  // Calculate price
  const price = includeLevelSkip ? season.premiumPlusPrice : season.premiumPrice;
  
  // Check gems
  if ((user?.resources?.gems || 0) < price) {
    throw new functions.https.HttpsError(
      'failed-precondition', 
      `Not enough gems. Need ${price}, have ${user?.resources?.gems || 0}`
    );
  }
  
  const progressId = `${userId}_${seasonId}`;
  
  await db.runTransaction(async (transaction) => {
    // Deduct gems
    transaction.update(db.collection('users').doc(userId), {
      'resources.gems': admin.firestore.FieldValue.increment(-price)
    });
    
    // Update progress
    const progressUpdate: Record<string, unknown> = {
      hasPremium: true,
      premiumPurchasedAt: admin.firestore.Timestamp.now()
    };
    
    if (includeLevelSkip) {
      // Add 25 levels worth of XP
      const bonusXp = 25 * season.xpPerLevel;
      progressUpdate['currentXp'] = admin.firestore.FieldValue.increment(bonusXp);
      progressUpdate['totalXpEarned'] = admin.firestore.FieldValue.increment(bonusXp);
      progressUpdate['levelSkipsPurchased'] = admin.firestore.FieldValue.increment(25);
    }
    
    transaction.update(db.collection('season_progress').doc(progressId), progressUpdate);
  });
  
  // Send notification
  await sendNotification(userId, {
    type: 'system_announcement',
    title: 'Premium Pass Activated! 🌟',
    body: includeLevelSkip 
      ? 'You now have access to all premium rewards + 25 bonus levels!'
      : 'You now have access to all premium track rewards!',
    data: { seasonId }
  });
  
  return { success: true, premiumActivated: true, levelSkip: includeLevelSkip };
});

// ============================================================================
// Claim Season Reward
// ============================================================================

export const claimSeasonReward = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { seasonId, level, track } = data as { 
    seasonId: string; 
    level: number;
    track: 'free' | 'premium';
  };
  const userId = context.auth.uid;
  const progressId = `${userId}_${seasonId}`;
  
  // Get season and progress
  const [seasonDoc, progressDoc] = await Promise.all([
    db.collection('seasons').doc(seasonId).get(),
    db.collection('season_progress').doc(progressId).get()
  ]);
  
  if (!seasonDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Season not found');
  }
  if (!progressDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Season progress not found');
  }
  
  const season = seasonDoc.data() as Season;
  const progress = progressDoc.data() as UserSeasonProgress;
  
  // Validate level reached
  if (progress.currentLevel < level) {
    throw new functions.https.HttpsError(
      'failed-precondition', 
      `Haven't reached level ${level} yet. Current: ${progress.currentLevel}`
    );
  }
  
  // Check premium for premium track
  if (track === 'premium' && !progress.hasPremium) {
    throw new functions.https.HttpsError('failed-precondition', 'Premium pass required');
  }
  
  // Check if already claimed
  const claimedField = track === 'free' ? 'claimedFreeLevels' : 'claimedPremiumLevels';
  const claimedLevels = progress[claimedField] || [];
  
  if (claimedLevels.includes(level)) {
    throw new functions.https.HttpsError('already-exists', 'Reward already claimed');
  }
  
  // Get reward
  const rewardTrack = track === 'free' ? season.freeTrack : season.premiumTrack;
  const reward = rewardTrack.find(r => r.level === level);
  
  if (!reward) {
    throw new functions.https.HttpsError('not-found', 'Reward not found for this level');
  }
  
  // Apply reward
  const userUpdate: Record<string, unknown> = {};
  
  if (reward.rewards.xp) {
    userUpdate['stats.totalXp'] = admin.firestore.FieldValue.increment(reward.rewards.xp);
  }
  if (reward.rewards.gems) {
    userUpdate['resources.gems'] = admin.firestore.FieldValue.increment(reward.rewards.gems);
  }
  if (reward.rewards.stone) {
    userUpdate['resources.stone'] = admin.firestore.FieldValue.increment(reward.rewards.stone);
  }
  if (reward.rewards.wood) {
    userUpdate['resources.wood'] = admin.firestore.FieldValue.increment(reward.rewards.wood);
  }
  if (reward.rewards.iron) {
    userUpdate['resources.iron'] = admin.firestore.FieldValue.increment(reward.rewards.iron);
  }
  if (reward.rewards.crystal) {
    userUpdate['resources.crystal'] = admin.firestore.FieldValue.increment(reward.rewards.crystal);
  }
  if (reward.rewards.arcaneEssence) {
    userUpdate['resources.arcaneEssence'] = admin.firestore.FieldValue.increment(reward.rewards.arcaneEssence);
  }
  
  // Handle cosmetic reward
  if (reward.rewards.cosmetic) {
    const cosmeticField = getCosmeticField(reward.rewards.cosmetic.type);
    userUpdate[cosmeticField] = admin.firestore.FieldValue.arrayUnion(reward.rewards.cosmetic.id);
  }
  
  await db.runTransaction(async (transaction) => {
    transaction.update(db.collection('users').doc(userId), userUpdate);
    transaction.update(db.collection('season_progress').doc(progressId), {
      [claimedField]: admin.firestore.FieldValue.arrayUnion(level)
    });
  });
  
  return { 
    success: true, 
    reward: reward.rewards,
    level
  };
});

// ============================================================================
// Award Season XP (Called by other systems)
// ============================================================================

export async function awardSeasonXp(userId: string, amount: number, reason: string): Promise<{
  awarded: boolean;
  levelUp: boolean;
  newLevel?: number;
}> {
  // Get active season
  const activeSeasonSnapshot = await db.collection('seasons')
    .where('status', '==', 'active')
    .limit(1)
    .get();
  
  if (activeSeasonSnapshot.empty) {
    return { awarded: false, levelUp: false };
  }
  
  const season = activeSeasonSnapshot.docs[0].data() as Season;
  const progressId = `${userId}_${season.id}`;
  
  const progressDoc = await db.collection('season_progress').doc(progressId).get();
  
  if (!progressDoc.exists) {
    // Initialize if needed
    await initializeSeasonProgress(userId, season);
  }
  
  const progress = (await db.collection('season_progress').doc(progressId).get()).data() as UserSeasonProgress;
  
  // Calculate new level
  const newTotalXp = progress.currentXp + amount;
  const newLevel = Math.min(
    Math.floor(newTotalXp / season.xpPerLevel) + 1,
    season.maxLevel
  );
  const levelUp = newLevel > progress.currentLevel;
  
  await db.collection('season_progress').doc(progressId).update({
    currentXp: newTotalXp,
    totalXpEarned: admin.firestore.FieldValue.increment(amount),
    currentLevel: newLevel,
    lastPlayedAt: admin.firestore.Timestamp.now()
  });
  
  // Send notification on level up
  if (levelUp) {
    await sendNotification(userId, {
      type: 'level_up',
      title: `Season Level ${newLevel}! 🎉`,
      body: 'New rewards are available to claim!',
      data: { seasonId: season.id, level: String(newLevel) }
    });
  }
  
  return { awarded: true, levelUp, newLevel };
}

// ============================================================================
// Complete Challenge
// ============================================================================

export const completeChallenge = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { challengeId, seasonId } = data as { challengeId: string; seasonId: string };
  const userId = context.auth.uid;
  const progressId = `${userId}_${seasonId}`;
  
  const progressDoc = await db.collection('season_progress').doc(progressId).get();
  if (!progressDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Season progress not found');
  }
  
  const progress = progressDoc.data() as UserSeasonProgress;
  
  // Find challenge
  const allChallenges = [
    ...progress.activeDailyChallenges,
    ...progress.activeWeeklyChallenges,
    ...progress.activeSeasonalChallenges
  ];
  
  const challenge = allChallenges.find(c => c.challengeId === challengeId);
  if (!challenge) {
    throw new functions.https.HttpsError('not-found', 'Challenge not found');
  }
  
  if (challenge.claimed) {
    throw new functions.https.HttpsError('already-exists', 'Challenge already claimed');
  }
  
  if (challenge.progress < challenge.target) {
    throw new functions.https.HttpsError('failed-precondition', 'Challenge not completed');
  }
  
  // Award XP
  const xpResult = await awardSeasonXp(userId, challenge.seasonXp, `Challenge: ${challenge.name}`);
  
  // Mark claimed
  await db.collection('season_progress').doc(progressId).update({
    completedChallengeIds: admin.firestore.FieldValue.arrayUnion(challengeId),
    challengesCompleted: admin.firestore.FieldValue.increment(1)
  });
  
  return { 
    success: true, 
    seasonXpAwarded: challenge.seasonXp,
    levelUp: xpResult.levelUp,
    newLevel: xpResult.newLevel
  };
});

// ============================================================================
// Refresh Daily Challenges
// ============================================================================

export const refreshDailyChallenges = functions.pubsub
  .schedule('every day 00:00')
  .timeZone('UTC')
  .onRun(async () => {
    const activeSeasonSnapshot = await db.collection('seasons')
      .where('status', '==', 'active')
      .limit(1)
      .get();
    
    if (activeSeasonSnapshot.empty) return;
    
    const season = activeSeasonSnapshot.docs[0].data() as Season;
    
    // Get all active progress documents
    const progressSnapshot = await db.collection('season_progress')
      .where('seasonId', '==', season.id)
      .get();
    
    const dailyTemplates = CHALLENGE_TEMPLATES.filter(t => t.type === 'daily');
    
    // Shuffle and select 3 random daily challenges
    const shuffled = dailyTemplates.sort(() => Math.random() - 0.5);
    const selectedTemplates = shuffled.slice(0, 3);
    
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    tomorrow.setHours(0, 0, 0, 0);
    
    const batch = db.batch();
    
    for (const doc of progressSnapshot.docs) {
      const newDailies: ActiveChallenge[] = selectedTemplates.map(template => ({
        challengeId: `daily_${Date.now()}_${template.id}`,
        templateId: template.id,
        name: template.name,
        description: template.description,
        progress: 0,
        target: template.requirement.target,
        seasonXp: template.seasonXp,
        expiresAt: admin.firestore.Timestamp.fromDate(tomorrow),
        claimed: false
      }));
      
      batch.update(doc.ref, {
        activeDailyChallenges: newDailies
      });
    }
    
    await batch.commit();
    functions.logger.info(`Refreshed daily challenges for ${progressSnapshot.size} players`);
  });

// ============================================================================
// Initialize Season Progress
// ============================================================================

async function initializeSeasonProgress(userId: string, season: Season): Promise<UserSeasonProgress> {
  const progressId = `${userId}_${season.id}`;
  
  // Select initial challenges
  const dailyTemplates = CHALLENGE_TEMPLATES.filter(t => t.type === 'daily');
  const weeklyTemplates = CHALLENGE_TEMPLATES.filter(t => t.type === 'weekly');
  const seasonalTemplates = CHALLENGE_TEMPLATES.filter(t => t.type === 'seasonal');
  
  const tomorrow = new Date();
  tomorrow.setDate(tomorrow.getDate() + 1);
  tomorrow.setHours(0, 0, 0, 0);
  
  const nextWeek = new Date();
  nextWeek.setDate(nextWeek.getDate() + 7);
  
  const progress: UserSeasonProgress = {
    userId,
    seasonId: season.id,
    currentLevel: 1,
    currentXp: 0,
    totalXpEarned: 0,
    hasPremium: false,
    levelSkipsPurchased: 0,
    claimedFreeLevels: [],
    claimedPremiumLevels: [],
    activeDailyChallenges: dailyTemplates.slice(0, 3).map(t => ({
      challengeId: `daily_${Date.now()}_${t.id}`,
      templateId: t.id,
      name: t.name,
      description: t.description,
      progress: 0,
      target: t.requirement.target,
      seasonXp: t.seasonXp,
      expiresAt: admin.firestore.Timestamp.fromDate(tomorrow),
      claimed: false
    })),
    activeWeeklyChallenges: weeklyTemplates.slice(0, 3).map(t => ({
      challengeId: `weekly_${Date.now()}_${t.id}`,
      templateId: t.id,
      name: t.name,
      description: t.description,
      progress: 0,
      target: t.requirement.target,
      seasonXp: t.seasonXp,
      expiresAt: admin.firestore.Timestamp.fromDate(nextWeek),
      claimed: false
    })),
    activeSeasonalChallenges: seasonalTemplates.map(t => ({
      challengeId: `seasonal_${Date.now()}_${t.id}`,
      templateId: t.id,
      name: t.name,
      description: t.description,
      progress: 0,
      target: t.requirement.target,
      seasonXp: t.seasonXp,
      claimed: false
    })),
    completedChallengeIds: [],
    challengesCompleted: 0,
    lastPlayedAt: admin.firestore.Timestamp.now()
  };
  
  await db.collection('season_progress').doc(progressId).set(progress);
  
  return progress;
}

// ============================================================================
// Helper Functions
// ============================================================================

function getCosmeticField(type: string): string {
  const fields: Record<string, string> = {
    title: 'unlockedTitles',
    badge: 'unlockedBadges',
    banner: 'unlockedBanners',
    block_skin: 'unlockedBlockSkins',
    territory_effect: 'unlockedTerritoryEffects',
    avatar_frame: 'unlockedAvatarFrames'
  };
  return fields[type] || 'unlockedCosmetics';
}

// ============================================================================
// Admin: Create Season
// ============================================================================

export const createSeason = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const {
    number,
    name,
    theme,
    description,
    startDate,
    endDate,
    premiumPrice = 950,
    premiumPlusPrice = 2800
  } = data as {
    number: number;
    name: string;
    theme: string;
    description: string;
    startDate: string;
    endDate: string;
    premiumPrice?: number;
    premiumPlusPrice?: number;
  };
  
  const seasonId = `season_${number}`;
  
  // Generate reward tracks
  const freeTrack = generateFreeTrack();
  const premiumTrack = generatePremiumTrack();
  
  const season: Season = {
    id: seasonId,
    number,
    name,
    theme,
    description,
    startDate: admin.firestore.Timestamp.fromDate(new Date(startDate)),
    endDate: admin.firestore.Timestamp.fromDate(new Date(endDate)),
    status: 'upcoming',
    maxLevel: 100,
    xpPerLevel: 1000,
    premiumPrice,
    premiumPlusPrice,
    freeTrack,
    premiumTrack,
    dailyChallenges: CHALLENGE_TEMPLATES.filter(t => t.type === 'daily'),
    weeklyChallenges: CHALLENGE_TEMPLATES.filter(t => t.type === 'weekly'),
    seasonalChallenges: CHALLENGE_TEMPLATES.filter(t => t.type === 'seasonal'),
    createdAt: admin.firestore.Timestamp.now()
  };
  
  await db.collection('seasons').doc(seasonId).set(season);
  
  return { success: true, season };
});

function generateFreeTrack(): SeasonReward[] {
  const rewards: SeasonReward[] = [];
  
  for (let level = 1; level <= 100; level++) {
    const reward: SeasonReward = {
      level,
      rewards: {},
      isMilestone: level % 25 === 0
    };
    
    // Every level gets something
    if (level % 5 === 0) {
      reward.rewards.gems = 10 + Math.floor(level / 10) * 5;
    } else if (level % 2 === 0) {
      reward.rewards.stone = 100 + level * 10;
      reward.rewards.wood = 100 + level * 10;
    } else {
      reward.rewards.iron = 25 + level * 2;
    }
    
    // Milestones
    if (level === 25) {
      reward.rewards.cosmetic = { type: 'badge', id: 'season_25', name: 'Rising Star', rarity: 'rare' };
    } else if (level === 50) {
      reward.rewards.cosmetic = { type: 'title', id: 'season_50', name: 'Dedicated', rarity: 'rare' };
    } else if (level === 75) {
      reward.rewards.cosmetic = { type: 'avatar_frame', id: 'season_75', name: 'Elite Frame', rarity: 'epic' };
    } else if (level === 100) {
      reward.rewards.gems = 200;
      reward.rewards.cosmetic = { type: 'banner', id: 'season_100_free', name: 'Season Completionist', rarity: 'epic' };
    }
    
    rewards.push(reward);
  }
  
  return rewards;
}

function generatePremiumTrack(): SeasonReward[] {
  const rewards: SeasonReward[] = [];
  
  for (let level = 1; level <= 100; level++) {
    const reward: SeasonReward = {
      level,
      rewards: {},
      isMilestone: level % 25 === 0
    };
    
    // Premium rewards are better
    if (level % 10 === 0) {
      reward.rewards.gems = 50 + Math.floor(level / 10) * 25;
    } else if (level % 5 === 0) {
      reward.rewards.crystal = 50 + level;
      reward.rewards.arcaneEssence = Math.floor(level / 5);
    } else if (level % 2 === 0) {
      reward.rewards.stone = 200 + level * 20;
      reward.rewards.wood = 200 + level * 20;
      reward.rewards.iron = 50 + level * 5;
    } else {
      reward.rewards.gems = 5 + Math.floor(level / 20) * 5;
    }
    
    // Premium cosmetics
    if (level === 1) {
      reward.rewards.cosmetic = { type: 'badge', id: 'premium_badge', name: 'Premium Player', rarity: 'epic' };
    } else if (level === 25) {
      reward.rewards.cosmetic = { type: 'block_skin', id: 'premium_25', name: 'Golden Blocks', rarity: 'epic' };
    } else if (level === 50) {
      reward.rewards.cosmetic = { type: 'territory_effect', id: 'premium_50', name: 'Flame Aura', rarity: 'epic' };
    } else if (level === 75) {
      reward.rewards.cosmetic = { type: 'avatar_frame', id: 'premium_75', name: 'Legendary Frame', rarity: 'legendary' };
    } else if (level === 100) {
      reward.rewards.gems = 500;
      reward.rewards.cosmetic = { type: 'title', id: 'premium_100', name: 'Season Champion', rarity: 'legendary' };
    }
    
    rewards.push(reward);
  }
  
  return rewards;
}
