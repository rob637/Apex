/**
 * Apex Citadels - Daily Rewards & Progression System
 * 
 * Handles:
 * - Daily login rewards
 * - Streak tracking
 * - XP and level progression
 * - Achievement tracking
 */

import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';
import {
  User,
  UserResources,
  DAILY_REWARDS,
  Achievement,
  AchievementCategory,
  UserAchievement,
  calculateLevel
} from './types';
import { sendNotification } from './notifications';

const db = admin.firestore();

// ============================================================================
// Achievement Definitions
// ============================================================================

export const ACHIEVEMENTS: Achievement[] = [
  // Territory Achievements
  {
    id: 'first_territory',
    name: 'Landowner',
    description: 'Claim your first territory',
    category: 'territory',
    icon: 'territory_1',
    requirement: { type: 'territories_claimed', target: 1 },
    rewards: { xp: 50, gems: 10 },
    isHidden: false,
    order: 1
  },
  {
    id: 'territory_5',
    name: 'Land Baron',
    description: 'Own 5 territories simultaneously',
    category: 'territory',
    icon: 'territory_5',
    requirement: { type: 'territories_owned', target: 5 },
    rewards: { xp: 200, gems: 50 },
    isHidden: false,
    order: 2
  },
  {
    id: 'territory_conqueror',
    name: 'Conqueror',
    description: 'Conquer 10 territories from other players',
    category: 'territory',
    icon: 'conqueror',
    requirement: { type: 'territories_conquered', target: 10 },
    rewards: { xp: 500, gems: 100, title: 'The Conqueror' },
    isHidden: false,
    order: 3
  },

  // Building Achievements
  {
    id: 'first_block',
    name: 'Builder',
    description: 'Place your first building block',
    category: 'building',
    icon: 'block_1',
    requirement: { type: 'blocks_placed', target: 1 },
    rewards: { xp: 25, gems: 5 },
    isHidden: false,
    order: 1
  },
  {
    id: 'blocks_100',
    name: 'Architect',
    description: 'Place 100 building blocks',
    category: 'building',
    icon: 'block_100',
    requirement: { type: 'blocks_placed', target: 100 },
    rewards: { xp: 300, gems: 75, title: 'Architect' },
    isHidden: false,
    order: 2
  },
  {
    id: 'blocks_1000',
    name: 'Master Builder',
    description: 'Place 1,000 building blocks',
    category: 'building',
    icon: 'block_1000',
    requirement: { type: 'blocks_placed', target: 1000 },
    rewards: { xp: 1000, gems: 250, title: 'Master Builder' },
    isHidden: false,
    order: 3
  },

  // Combat Achievements
  {
    id: 'first_raid',
    name: 'Raider',
    description: 'Complete your first raid',
    category: 'combat',
    icon: 'raid_1',
    requirement: { type: 'raids_completed', target: 1 },
    rewards: { xp: 50, gems: 10 },
    isHidden: false,
    order: 1
  },
  {
    id: 'raids_50',
    name: 'Warlord',
    description: 'Complete 50 successful raids',
    category: 'combat',
    icon: 'raid_50',
    requirement: { type: 'raids_won', target: 50 },
    rewards: { xp: 500, gems: 150, title: 'Warlord' },
    isHidden: false,
    order: 2
  },
  {
    id: 'defender_10',
    name: 'Defender',
    description: 'Successfully defend 10 attacks',
    category: 'combat',
    icon: 'defend_10',
    requirement: { type: 'raids_defended', target: 10 },
    rewards: { xp: 250, gems: 50 },
    isHidden: false,
    order: 3
  },

  // Social Achievements
  {
    id: 'join_alliance',
    name: 'Team Player',
    description: 'Join an alliance',
    category: 'social',
    icon: 'alliance_join',
    requirement: { type: 'alliances_joined', target: 1 },
    rewards: { xp: 100, gems: 25 },
    isHidden: false,
    order: 1
  },
  {
    id: 'create_alliance',
    name: 'Leader',
    description: 'Create your own alliance',
    category: 'social',
    icon: 'alliance_create',
    requirement: { type: 'alliance_created', target: 1 },
    rewards: { xp: 200, gems: 50, title: 'Leader' },
    isHidden: false,
    order: 2
  },

  // Resource Achievements
  {
    id: 'harvest_1000',
    name: 'Gatherer',
    description: 'Harvest 1,000 total resources',
    category: 'resources',
    icon: 'harvest_1000',
    requirement: { type: 'resources_harvested', target: 1000 },
    rewards: { xp: 150, gems: 25 },
    isHidden: false,
    order: 1
  },
  {
    id: 'harvest_100000',
    name: 'Resource Tycoon',
    description: 'Harvest 100,000 total resources',
    category: 'resources',
    icon: 'harvest_100k',
    requirement: { type: 'resources_harvested', target: 100000 },
    rewards: { xp: 1000, gems: 300, title: 'Tycoon' },
    isHidden: false,
    order: 2
  },

  // Milestone Achievements
  {
    id: 'level_5',
    name: 'Rising Star',
    description: 'Reach level 5',
    category: 'milestones',
    icon: 'level_5',
    requirement: { type: 'level', target: 5 },
    rewards: { xp: 100, gems: 50 },
    isHidden: false,
    order: 1
  },
  {
    id: 'level_10',
    name: 'Veteran',
    description: 'Reach level 10',
    category: 'milestones',
    icon: 'level_10',
    requirement: { type: 'level', target: 10 },
    rewards: { xp: 250, gems: 100, title: 'Veteran' },
    isHidden: false,
    order: 2
  },
  {
    id: 'level_20',
    name: 'Legend',
    description: 'Reach level 20',
    category: 'milestones',
    icon: 'level_20',
    requirement: { type: 'level', target: 20 },
    rewards: { xp: 1000, gems: 500, title: 'Legend' },
    isHidden: false,
    order: 3
  },
  {
    id: 'streak_7',
    name: 'Dedicated',
    description: 'Maintain a 7-day login streak',
    category: 'milestones',
    icon: 'streak_7',
    requirement: { type: 'longest_streak', target: 7 },
    rewards: { xp: 200, gems: 100 },
    isHidden: false,
    order: 4
  },
  {
    id: 'streak_30',
    name: 'Committed',
    description: 'Maintain a 30-day login streak',
    category: 'milestones',
    icon: 'streak_30',
    requirement: { type: 'longest_streak', target: 30 },
    rewards: { xp: 1000, gems: 500, title: 'Committed' },
    isHidden: false,
    order: 5
  },

  // Hidden Achievements
  {
    id: 'night_owl',
    name: 'Night Owl',
    description: 'Claim a territory between midnight and 4 AM',
    category: 'exploration',
    icon: 'night_owl',
    requirement: { type: 'special', target: 1, condition: { timeRange: [0, 4] } },
    rewards: { xp: 100, gems: 50 },
    isHidden: true,
    order: 99
  },
  {
    id: 'early_bird',
    name: 'Early Bird',
    description: 'Claim a territory between 5 AM and 7 AM',
    category: 'exploration',
    icon: 'early_bird',
    requirement: { type: 'special', target: 1, condition: { timeRange: [5, 7] } },
    rewards: { xp: 100, gems: 50 },
    isHidden: true,
    order: 99
  }
];

// ============================================================================
// Claim Daily Reward
// ============================================================================

export const claimDailyReward = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const userId = context.auth.uid;
  const today = new Date().toISOString().split('T')[0]; // YYYY-MM-DD

  const userDoc = await db.collection('users').doc(userId).get();
  if (!userDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'User not found');
  }

  const user = userDoc.data() as User;
  const dailyState = user.dailyReward || {
    currentDay: 0,
    currentStreak: 0,
    lastClaimDate: undefined,
    totalClaims: 0
  };

  // Check if already claimed today
  if (dailyState.lastClaimDate === today) {
    throw new functions.https.HttpsError(
      'already-exists',
      'Daily reward already claimed today'
    );
  }

  // Calculate streak
  const yesterday = new Date();
  yesterday.setDate(yesterday.getDate() - 1);
  const yesterdayStr = yesterday.toISOString().split('T')[0];

  let newStreak = 1;
  let newDay = 1;

  if (dailyState.lastClaimDate === yesterdayStr) {
    // Continuing streak
    newStreak = dailyState.currentStreak + 1;
    newDay = (dailyState.currentDay % 7) + 1; // Cycle through 1-7
  } else if (dailyState.lastClaimDate) {
    // Streak broken, reset
    newStreak = 1;
    newDay = 1;
  }

  // Get reward for this day
  const rewardDef = DAILY_REWARDS[newDay - 1];
  const streakMultiplier = rewardDef.streakBonus;

  // Calculate final rewards with streak bonus
  const finalRewards: Partial<UserResources> = {};
  for (const [resource, amount] of Object.entries(rewardDef.rewards)) {
    finalRewards[resource as keyof UserResources] = Math.floor(amount * streakMultiplier);
  }
  const finalXp = Math.floor(rewardDef.xp * streakMultiplier);

  // Update user
  const updateData: Record<string, unknown> = {
    'dailyReward.currentDay': newDay,
    'dailyReward.currentStreak': newStreak,
    'dailyReward.lastClaimDate': today,
    'dailyReward.totalClaims': admin.firestore.FieldValue.increment(1),
    'stats.daysPlayed': admin.firestore.FieldValue.increment(1),
    lastLoginDate: today,
    lastActive: admin.firestore.Timestamp.now()
  };

  // Add resources
  for (const [resource, amount] of Object.entries(finalRewards)) {
    updateData[`resources.${resource}`] = admin.firestore.FieldValue.increment(amount);
  }

  // Update longest streak if applicable
  if (newStreak > (user.stats.longestStreak || 0)) {
    updateData['stats.longestStreak'] = newStreak;
  }

  await db.collection('users').doc(userId).update(updateData);

  // Award XP
  await awardXp(userId, finalXp);

  // Check streak achievements
  await checkAchievements(userId, 'milestones');

  return {
    success: true,
    day: newDay,
    streak: newStreak,
    rewards: finalRewards,
    xp: finalXp,
    streakBonus: streakMultiplier,
    nextDay: (newDay % 7) + 1
  };
});

// ============================================================================
// Get Daily Reward Status
// ============================================================================

export const getDailyRewardStatus = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const userId = context.auth.uid;
  const today = new Date().toISOString().split('T')[0];

  const userDoc = await db.collection('users').doc(userId).get();
  if (!userDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'User not found');
  }

  const user = userDoc.data() as User;
  const dailyState = user.dailyReward || {
    currentDay: 0,
    currentStreak: 0,
    lastClaimDate: undefined,
    totalClaims: 0
  };

  const canClaim = dailyState.lastClaimDate !== today;

  // Calculate what the next reward would be
  let nextDay = 1;
  const yesterday = new Date();
  yesterday.setDate(yesterday.getDate() - 1);
  const yesterdayStr = yesterday.toISOString().split('T')[0];

  if (dailyState.lastClaimDate === yesterdayStr) {
    nextDay = (dailyState.currentDay % 7) + 1;
  } else if (dailyState.lastClaimDate === today) {
    nextDay = (dailyState.currentDay % 7) + 1;
  }

  const nextReward = DAILY_REWARDS[nextDay - 1];

  return {
    canClaim,
    currentDay: dailyState.currentDay,
    currentStreak: dailyState.currentStreak,
    lastClaimDate: dailyState.lastClaimDate,
    nextDay,
    nextReward,
    allRewards: DAILY_REWARDS
  };
});

// ============================================================================
// Award XP
// ============================================================================

export async function awardXp(userId: string, xpAmount: number): Promise<{ newLevel: number; leveledUp: boolean }> {
  const userDoc = await db.collection('users').doc(userId).get();
  if (!userDoc.exists) {
    throw new Error('User not found');
  }

  const user = userDoc.data() as User;
  const oldLevel = user.stats.level;
  const newTotalXp = (user.stats.totalXp || 0) + xpAmount;
  const newLevel = calculateLevel(newTotalXp);
  const leveledUp = newLevel > oldLevel;

  await db.collection('users').doc(userId).update({
    'stats.xp': admin.firestore.FieldValue.increment(xpAmount),
    'stats.totalXp': newTotalXp,
    'stats.level': newLevel
  });

  // Send notification if leveled up
  if (leveledUp) {
    await sendNotification(userId, {
      type: 'level_up',
      title: 'Level Up!',
      body: `Congratulations! You reached level ${newLevel}!`,
      data: { level: String(newLevel) }
    });

    // Check level achievements
    await checkAchievements(userId, 'milestones');
  }

  return { newLevel, leveledUp };
}

// ============================================================================
// Check Achievements
// ============================================================================

export async function checkAchievements(userId: string, category?: AchievementCategory): Promise<string[]> {
  const userDoc = await db.collection('users').doc(userId).get();
  if (!userDoc.exists) {
    return [];
  }

  const user = userDoc.data() as User;
  const achievementsToCheck = category 
    ? ACHIEVEMENTS.filter(a => a.category === category)
    : ACHIEVEMENTS;

  const unlockedAchievements: string[] = [];

  // Get user's current achievement progress
  const progressSnapshot = await db
    .collection('users')
    .doc(userId)
    .collection('achievements')
    .get();

  const currentProgress: Record<string, UserAchievement> = {};
  for (const doc of progressSnapshot.docs) {
    currentProgress[doc.id] = doc.data() as UserAchievement;
  }

  for (const achievement of achievementsToCheck) {
    // Skip if already unlocked
    if (currentProgress[achievement.id]?.unlockedAt) {
      continue;
    }

    // Check requirement
    const currentValue = getStatValue(user, achievement.requirement.type);
    const progress = Math.min(currentValue, achievement.requirement.target);

    // Update progress
    const progressDoc = db
      .collection('users')
      .doc(userId)
      .collection('achievements')
      .doc(achievement.id);

    if (currentValue >= achievement.requirement.target) {
      // Achievement unlocked!
      await progressDoc.set({
        achievementId: achievement.id,
        progress: achievement.requirement.target,
        unlockedAt: admin.firestore.Timestamp.now(),
        claimed: false
      });

      unlockedAchievements.push(achievement.id);

      // Send notification
      await sendNotification(userId, {
        type: 'achievement_unlocked',
        title: 'Achievement Unlocked!',
        body: `${achievement.name}: ${achievement.description}`,
        data: { achievementId: achievement.id }
      });

      // Update achievement count
      await db.collection('users').doc(userId).update({
        'stats.achievementsUnlocked': admin.firestore.FieldValue.increment(1)
      });
    } else if (progress > (currentProgress[achievement.id]?.progress || 0)) {
      // Update progress
      await progressDoc.set({
        achievementId: achievement.id,
        progress,
        unlockedAt: null,
        claimed: false
      }, { merge: true });
    }
  }

  return unlockedAchievements;
}

function getStatValue(user: User, statType: string): number {
  switch (statType) {
    case 'territories_claimed':
    case 'territories_owned':
      return user.stats.territoriesClaimed || 0;
    case 'territories_conquered':
      return user.stats.raidsWon || 0;
    case 'blocks_placed':
      return user.stats.blocksPlaced || 0;
    case 'raids_completed':
      return user.stats.raidsCompleted || 0;
    case 'raids_won':
      return user.stats.raidsWon || 0;
    case 'raids_defended':
      return user.stats.raidsDefended || 0;
    case 'alliances_joined':
      return user.stats.alliancesJoined || 0;
    case 'resources_harvested':
      return user.stats.resourcesHarvested || 0;
    case 'level':
      return user.stats.level || 1;
    case 'longest_streak':
      return user.stats.longestStreak || 0;
    case 'days_played':
      return user.stats.daysPlayed || 0;
    default:
      return 0;
  }
}

// ============================================================================
// Claim Achievement Reward
// ============================================================================

export const claimAchievementReward = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const { achievementId } = data as { achievementId: string };
  const userId = context.auth.uid;

  // Find achievement definition
  const achievement = ACHIEVEMENTS.find(a => a.id === achievementId);
  if (!achievement) {
    throw new functions.https.HttpsError('not-found', 'Achievement not found');
  }

  // Get user's achievement progress
  const progressDoc = await db
    .collection('users')
    .doc(userId)
    .collection('achievements')
    .doc(achievementId)
    .get();

  if (!progressDoc.exists) {
    throw new functions.https.HttpsError('failed-precondition', 'Achievement not unlocked');
  }

  const progress = progressDoc.data() as UserAchievement;

  if (!progress.unlockedAt) {
    throw new functions.https.HttpsError('failed-precondition', 'Achievement not unlocked');
  }

  if (progress.claimed) {
    throw new functions.https.HttpsError('already-exists', 'Reward already claimed');
  }

  // Award rewards
  const batch = db.batch();

  // Mark as claimed
  batch.update(progressDoc.ref, {
    claimed: true,
    claimedAt: admin.firestore.Timestamp.now()
  });

  // Add resources
  if (achievement.rewards.gems) {
    batch.update(db.collection('users').doc(userId), {
      'resources.gems': admin.firestore.FieldValue.increment(achievement.rewards.gems)
    });
  }

  await batch.commit();

  // Award XP
  if (achievement.rewards.xp) {
    await awardXp(userId, achievement.rewards.xp);
  }

  return {
    success: true,
    rewards: achievement.rewards
  };
});

// ============================================================================
// Get All Achievements
// ============================================================================

export const getAchievements = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const userId = context.auth.uid;
  const { category } = data as { category?: AchievementCategory };

  // Get user's achievement progress
  const progressSnapshot = await db
    .collection('users')
    .doc(userId)
    .collection('achievements')
    .get();

  const userProgress: Record<string, UserAchievement> = {};
  for (const doc of progressSnapshot.docs) {
    userProgress[doc.id] = doc.data() as UserAchievement;
  }

  // Filter achievements
  let achievements = category 
    ? ACHIEVEMENTS.filter(a => a.category === category)
    : ACHIEVEMENTS;

  // Don't show hidden achievements unless unlocked
  achievements = achievements.filter(a => 
    !a.isHidden || userProgress[a.id]?.unlockedAt
  );

  // Combine with user progress
  const result = achievements.map(a => ({
    ...a,
    progress: userProgress[a.id]?.progress || 0,
    unlockedAt: userProgress[a.id]?.unlockedAt || null,
    claimed: userProgress[a.id]?.claimed || false
  }));

  return {
    achievements: result.sort((a, b) => a.order - b.order),
    totalUnlocked: Object.values(userProgress).filter(p => p.unlockedAt).length,
    totalAchievements: ACHIEVEMENTS.filter(a => !a.isHidden).length
  };
});
