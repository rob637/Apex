/**
 * Apex Citadels - Shared Type Definitions
 * 
 * All TypeScript interfaces used across Cloud Functions
 */

import * as admin from 'firebase-admin';

// ============================================================================
// User Types
// ============================================================================

export type Faction = 'builders' | 'raiders' | 'merchants';

export interface UserStats {
  level: number;
  xp: number;
  totalXp: number;
  citadelsBuilt: number;
  citadelsLost: number;
  raidsCompleted: number;
  raidsWon: number;
  raidsLost: number;
  raidsDefended: number;
  resourcesHarvested: number;
  blocksPlaced: number;
  territoriesClaimed: number;
  alliancesJoined: number;
  achievementsUnlocked: number;
  daysPlayed: number;
  longestStreak: number;
}

/**
 * Player resources
 * 
 * Resource Types:
 * - stone: Basic building material, abundant
 * - wood: Basic building material, abundant  
 * - iron: (formerly 'metal') Refined material for weapons/armor
 * - crystal: Magical amplifier, moderately rare
 * - arcaneEssence: Rare magical resource for elite units/structures
 * - gems: Premium currency (IAP)
 * 
 * Note: 'metal' kept as alias for backwards compatibility
 */
export interface UserResources {
  stone: number;
  wood: number;
  iron: number;        // Renamed from metal
  metal?: number;      // Deprecated alias for iron (backwards compat)
  crystal: number;
  arcaneEssence: number; // New: rare magical resource
  gems: number;        // Premium currency (IAP)
}

/** Resource type identifiers */
export type ResourceType = 'stone' | 'wood' | 'iron' | 'crystal' | 'arcaneEssence' | 'gems';

/** Resource display configuration */
export const RESOURCE_CONFIG: Record<ResourceType, {
  displayName: string;
  icon: string;
  color: string;
  rarity: 'common' | 'uncommon' | 'rare' | 'premium';
  description: string;
}> = {
  stone: {
    displayName: 'Stone',
    icon: '🪨',
    color: '#808080',
    rarity: 'common',
    description: 'Sturdy building material quarried from the earth'
  },
  wood: {
    displayName: 'Wood',
    icon: '🪵',
    color: '#8B4513',
    rarity: 'common',
    description: 'Versatile material harvested from enchanted forests'
  },
  iron: {
    displayName: 'Iron',
    icon: '⚙️',
    color: '#4A4A4A',
    rarity: 'uncommon',
    description: 'Refined ore forged into weapons and armor'
  },
  crystal: {
    displayName: 'Crystal',
    icon: '💎',
    color: '#00CED1',
    rarity: 'rare',
    description: 'Magical amplifier found in ancient ruins'
  },
  arcaneEssence: {
    displayName: 'Arcane Essence',
    icon: '✨',
    color: '#9932CC',
    rarity: 'rare',
    description: 'Concentrated magical energy for elite conjurations'
  },
  gems: {
    displayName: 'Gems',
    icon: '💠',
    color: '#FFD700',
    rarity: 'premium',
    description: 'Premium currency for special purchases'
  }
};

/** Default starting resources for new players */
export const DEFAULT_RESOURCES: UserResources = {
  stone: 500,
  wood: 500,
  iron: 100,
  crystal: 25,
  arcaneEssence: 0,
  gems: 50
};

export interface User {
  uid: string;
  displayName: string;
  email?: string;
  photoURL?: string;
  faction: Faction;
  allianceId?: string;
  allianceRole?: AllianceRole;
  stats: UserStats;
  resources: UserResources;
  settings: UserSettings;
  dailyReward: DailyRewardState;
  createdAt: admin.firestore.Timestamp;
  lastActive: admin.firestore.Timestamp;
  lastLoginDate: string; // YYYY-MM-DD format for daily tracking
  isPremium: boolean;
  premiumExpires?: admin.firestore.Timestamp;
  banStatus?: BanStatus;
  // Protection fields
  newcomerShieldExpires?: admin.firestore.Timestamp; // 7 days from account creation
  protectionStatus?: ProtectionStatus;
}

// ============================================================================
// Protection Types
// ============================================================================

/** Player protection status calculated from activity and account age */
export interface ProtectionStatus {
  hasNewcomerShield: boolean;
  shieldExpiresAt?: admin.firestore.Timestamp;
  activityDefenseBonus: number; // 0 to 0.5 (50% bonus)
  canBeAttacked: boolean;
  reason?: string; // Why they can't be attacked
}

/** Location density classification for territory radius */
export type LocationDensity = 'urban' | 'suburban' | 'rural';

/** Territory radius by location density */
export const TERRITORY_RADIUS_BY_DENSITY: Record<LocationDensity, number> = {
  urban: 25,      // Dense city: 25m radius
  suburban: 35,   // Suburbs: 35m radius  
  rural: 50       // Rural: 50m radius
};

/** Activity level for defense bonus calculation */
export type ActivityLevel = 'active' | 'away' | 'inactive' | 'abandoned';

/** Activity thresholds and defense bonuses */
export const ACTIVITY_DEFENSE_BONUSES: Record<ActivityLevel, { maxDaysInactive: number; defenseBonus: number }> = {
  active: { maxDaysInactive: 1, defenseBonus: 0 },       // Active: no bonus needed
  away: { maxDaysInactive: 3, defenseBonus: 0.25 },      // 1-3 days: 25% bonus
  inactive: { maxDaysInactive: 7, defenseBonus: 0.5 },   // 3-7 days: 50% bonus
  abandoned: { maxDaysInactive: Infinity, defenseBonus: 0 } // 7+ days: no protection, can be taken
}

export interface UserSettings {
  pushNotifications: boolean;
  soundEnabled: boolean;
  musicEnabled: boolean;
  hapticFeedback: boolean;
  language: string;
  showOnLeaderboard: boolean;
}

export interface BanStatus {
  isBanned: boolean;
  reason?: string;
  bannedAt?: admin.firestore.Timestamp;
  expiresAt?: admin.firestore.Timestamp;
}

// ============================================================================
// Territory Types
// ============================================================================

/** Territory state in the siege system */
export type TerritoryState = 'secure' | 'contested' | 'vulnerable' | 'fallen';

/** Production multipliers for each territory state */
export const TERRITORY_STATE_EFFECTS: Record<TerritoryState, { productionMultiplier: number; description: string }> = {
  secure: { productionMultiplier: 1.0, description: 'Full production, all defenses active' },
  contested: { productionMultiplier: 0.8, description: '20% production loss after first battle loss' },
  vulnerable: { productionMultiplier: 0.5, description: '50% production loss, structures damaged' },
  fallen: { productionMultiplier: 0, description: 'Territory lost, 24hr reclaim cooldown' }
};

export interface Territory {
  id: string;
  ownerId: string;
  ownerName: string;
  allianceId?: string;
  centerLatitude: number;
  centerLongitude: number;
  geoHash: string;
  radiusMeters: number;
  locationDensity?: LocationDensity; // Urban/suburban/rural classification
  level: number;
  health: number;
  maxHealth: number;
  // Siege system fields
  state: TerritoryState;
  battleLosses: number; // 0-3, resets when reclaimed or state returns to secure
  lastStateChangeAt?: admin.firestore.Timestamp;
  fallenAt?: admin.firestore.Timestamp;
  previousOwnerId?: string; // Track for reclaim eligibility
  blueprintId?: string; // Saved citadel layout
  // Protection fields
  shieldExpiresAt?: admin.firestore.Timestamp;
  lastAttackedAt?: admin.firestore.Timestamp;
  lastBattleId?: string;
  // NPC territory fields
  isNPC?: boolean;
  npcDifficulty?: number; // 1-10
  npcRespawnHours?: number;
  // Timestamps
  createdAt: admin.firestore.Timestamp;
  buildings: BuildingPlacement[];
}

export interface BuildingPlacement {
  blockType: string;
  positionX: number;
  positionY: number;
  positionZ: number;
  rotationY: number;
  placedAt: admin.firestore.Timestamp;
}

// ============================================================================
// Combat Types
// ============================================================================

export type AttackType = 'quick_strike' | 'heavy_assault' | 'siege';

export interface Attack {
  id: string; attackerId: string;
  attackerName: string;
  attackerAllianceId?: string;
  defenderId: string;
  defenderName: string;
  defenderAllianceId?: string;
  territoryId: string;
  attackType: AttackType;
  damage: number;
  startedAt: admin.firestore.Timestamp;
  completedAt?: admin.firestore.Timestamp;
  result?: 'success' | 'failed' | 'defended';
  xpEarned: number;
  resourcesLooted?: UserResources;
}

export interface AttackCooldown {
  attackType: AttackType;
  cooldownSeconds: number;
  lastUsedAt?: admin.firestore.Timestamp;
}

export const ATTACK_DEFINITIONS: Record<AttackType, {
  baseDamage: number;
  cooldownSeconds: number;
  xpReward: number;
  resourceCost: Partial<UserResources>;
}> = {
  quick_strike: {
    baseDamage: 10,
    cooldownSeconds: 60,
    xpReward: 5,
    resourceCost: {}
  },
  heavy_assault: {
    baseDamage: 25,
    cooldownSeconds: 300,
    xpReward: 15,
    resourceCost: { iron: 10 }
  },
  siege: {
    baseDamage: 50,
    cooldownSeconds: 3600,
    xpReward: 50,
    resourceCost: { iron: 50, stone: 50 }
  }
};

// ============================================================================
// Troop System Types
// ============================================================================

/** Six troop types with rock-paper-scissors counters */
export type TroopType = 'infantry' | 'archer' | 'cavalry' | 'siege' | 'mage' | 'guardian';

export interface Troop {
  type: TroopType;
  count: number;
  level: number;
}

export interface TroopDefinition {
  type: TroopType;
  displayName: string;
  description: string;
  baseAttack: number;
  baseDefense: number;
  baseHealth: number;
  trainingTimeSeconds: number;
  trainingCost: Partial<UserResources>;
  strongAgainst: TroopType[];
  weakAgainst: TroopType[];
  specialAbility?: string;
}

/** Counter multiplier when attacking a troop type you're strong against */
export const COUNTER_MULTIPLIER = 1.5;
/** Weakness multiplier when attacking a troop type you're weak against */
export const WEAKNESS_MULTIPLIER = 0.6;

export const TROOP_DEFINITIONS: Record<TroopType, TroopDefinition> = {
  infantry: {
    type: 'infantry',
    displayName: 'Infantry',
    description: 'Balanced front-line troops. Strong against archers.',
    baseAttack: 10,
    baseDefense: 10,
    baseHealth: 100,
    trainingTimeSeconds: 60,
    trainingCost: { iron: 10, wood: 5 },
    strongAgainst: ['archer'],
    weakAgainst: ['cavalry']
  },
  archer: {
    type: 'archer',
    displayName: 'Archer',
    description: 'Ranged damage dealers. Strong against cavalry.',
    baseAttack: 15,
    baseDefense: 5,
    baseHealth: 60,
    trainingTimeSeconds: 90,
    trainingCost: { wood: 15, iron: 5 },
    strongAgainst: ['cavalry'],
    weakAgainst: ['infantry']
  },
  cavalry: {
    type: 'cavalry',
    displayName: 'Cavalry',
    description: 'Fast flanking units. Strong against infantry and siege.',
    baseAttack: 12,
    baseDefense: 8,
    baseHealth: 80,
    trainingTimeSeconds: 180,
    trainingCost: { iron: 20, wood: 10 },
    strongAgainst: ['infantry', 'siege'],
    weakAgainst: ['archer', 'guardian']
  },
  siege: {
    type: 'siege',
    displayName: 'Siege Engine',
    description: 'Devastating against structures. Weak against troops.',
    baseAttack: 25,
    baseDefense: 3,
    baseHealth: 150,
    trainingTimeSeconds: 300,
    trainingCost: { wood: 50, iron: 30, stone: 20 },
    strongAgainst: [], // Strong against buildings, not troops
    weakAgainst: ['cavalry', 'mage'],
    specialAbility: 'structure_damage_x2'
  },
  mage: {
    type: 'mage',
    displayName: 'Mage',
    description: 'Area damage and debuffs. Strong against groups.',
    baseAttack: 18,
    baseDefense: 4,
    baseHealth: 50,
    trainingTimeSeconds: 240,
    trainingCost: { crystal: 15, iron: 10, arcaneEssence: 5 },
    strongAgainst: ['siege', 'guardian'],
    weakAgainst: ['cavalry'],
    specialAbility: 'area_damage'
  },
  guardian: {
    type: 'guardian',
    displayName: 'Guardian',
    description: 'Defensive specialists. Protects other troops.',
    baseAttack: 6,
    baseDefense: 18,
    baseHealth: 200,
    trainingTimeSeconds: 200,
    trainingCost: { stone: 30, iron: 20, arcaneEssence: 3 },
    strongAgainst: ['cavalry'],
    weakAgainst: ['mage', 'siege'],
    specialAbility: 'damage_redirect'
  }
};

// ============================================================================
// Battle System Types
// ============================================================================

/** Battle status through its lifecycle */
export type BattleStatus = 'scheduled' | 'preparing' | 'active' | 'completed' | 'cancelled';

/** Participation type affects combat effectiveness */
export type ParticipationType = 'physical' | 'nearby' | 'remote';

/** Effectiveness multipliers based on participation */
export const PARTICIPATION_EFFECTIVENESS: Record<ParticipationType, number> = {
  physical: 1.0,   // At the territory location
  nearby: 0.75,    // Within 1km
  remote: 0.5      // Anywhere else
};

export interface ScheduledBattle {
  id: string;
  
  // Participants
  attackerId: string;
  attackerName: string;
  attackerAllianceId?: string;
  defenderId: string;
  defenderName: string;
  defenderAllianceId?: string;
  
  // Target
  territoryId: string;
  territoryName: string;
  
  // Timing (24-hour scheduling)
  scheduledAt: admin.firestore.Timestamp;
  battleStartsAt: admin.firestore.Timestamp; // scheduledAt + 24 hours
  battleEndsAt?: admin.firestore.Timestamp;
  
  // Status
  status: BattleStatus;
  
  // Formations (set during preparation phase)
  attackerFormation?: BattleFormation;
  defenderFormation?: BattleFormation;
  
  // Participation tracking
  attackerParticipation?: ParticipationType;
  defenderParticipation?: ParticipationType;
  
  // Protection bonuses
  defenderActivityBonus?: number; // 0-0.5 defense bonus based on activity
  
  // Results (after battle completes)
  result?: BattleResult;
  
  // Alliance war context
  warId?: string;
  isWarBattle?: boolean;
  
  // Timestamps
  createdAt: admin.firestore.Timestamp;
  updatedAt: admin.firestore.Timestamp;
}

export interface BattleFormation {
  troops: Troop[];
  totalPower: number;
  strategy?: 'aggressive' | 'defensive' | 'balanced';
}

export interface BattleResult {
  winner: 'attacker' | 'defender';
  isDecisive: boolean; // Citadel destroyed or 70%+ troops eliminated
  rounds: BattleRound[];
  totalRounds: number;
  
  // Casualties
  attackerLosses: Troop[];
  defenderLosses: Troop[];
  attackerSurvivors: Troop[];
  defenderSurvivors: Troop[];
  
  // Structure damage (if attacker won)
  structureDamagePercent?: number;
  
  // Territory state change
  previousTerritoryState: TerritoryState;
  newTerritoryState: TerritoryState;
  territoryFallen: boolean;
  
  // Rewards
  attackerXp: number;
  defenderXp: number;
  resourcesLooted?: Partial<UserResources>;
}

export interface BattleRound {
  roundNumber: number;
  attackerAction: RoundAction;
  defenderAction: RoundAction;
  attackerDamageDealt: number;
  defenderDamageDealt: number;
  attackerCasualties: Troop[];
  defenderCasualties: Troop[];
  events: BattleEvent[];
}

export interface RoundAction {
  type: 'attack' | 'defend' | 'special';
  targetTroopType?: TroopType;
  abilityUsed?: string;
}

export interface BattleEvent {
  type: 'counter' | 'critical' | 'ability' | 'casualty' | 'morale';
  message: string;
  impact?: number;
}

// ============================================================================
// Battle Configuration
// ============================================================================

export const BATTLE_CONFIG = {
  // Scheduling
  SCHEDULING_WINDOW_HOURS: 24,          // Time between scheduling and battle
  BATTLE_DURATION_MINUTES: 30,          // Max time for battle
  MAX_ROUNDS: 10,                        // Turn limit
  
  // Post-battle
  POST_BATTLE_SHIELD_HOURS: 4,          // Shield after any battle
  SAME_ATTACKER_COOLDOWN_HOURS: 48,     // Same attacker can't hit same territory
  SAME_ALLIANCE_COOLDOWN_HOURS: 24,     // Same alliance cooldown
  
  // Siege system
  LOSSES_TO_FALL: 3,                    // Battle losses before territory falls
  RECLAIM_COOLDOWN_HOURS: 24,           // Wait time before reclaiming
  RECLAIM_COST_PERCENT: 0.3,            // 30% of original build cost
  
  // Protection
  NEWCOMER_SHIELD_DAYS: 7,              // New player protection
  INACTIVE_BONUS_THRESHOLD_DAYS: 2,     // Days before activity defense bonus
  INACTIVE_DEFENSE_BONUS: 0.5,          // 50% defense bonus when inactive
  
  // Participation radius
  PHYSICAL_PRESENCE_RADIUS_M: 50,       // Must be within 50m
  NEARBY_RADIUS_M: 1000,                // Within 1km for nearby bonus
  
  // Victory conditions
  DECISIVE_VICTORY_THRESHOLD: 0.7,      // 70% enemy troops eliminated
  CITADEL_DESTROY_THRESHOLD: 0.5,       // 50%+ structures destroyed = win
};

// ============================================================================
// User Troops (Subcollection)
// ============================================================================

export interface UserTroops {
  oderId: string;
  troops: Troop[];
  maxCapacity: number;
  trainingQueue: TrainingQueueItem[];
  lastUpdated: admin.firestore.Timestamp;
}

export interface TrainingQueueItem {
  troopType: TroopType;
  count: number;
  startedAt: admin.firestore.Timestamp;
  completesAt: admin.firestore.Timestamp;
}

// ============================================================================
// Blueprint System
// ============================================================================

export interface Blueprint {
  id: string;
  ownerId: string;
  name: string;
  description?: string;
  buildings: BuildingPlacement[];
  totalBuildCost: Partial<UserResources>;
  sourceTerritoryId?: string;
  createdAt: admin.firestore.Timestamp;
  isAutoSaved: boolean; // True if auto-saved when territory fell
}

// ============================================================================
// Alliance Types
// ============================================================================

export type AllianceRole = 'leader' | 'officer' | 'member';

export interface Alliance {
  id: string;
  name: string;
  tag: string; // 3-4 character tag
  description: string;
  leaderId: string;
  leaderName: string;
  memberCount: number;
  maxMembers: number;
  isOpen: boolean; // Can anyone join?
  requiredLevel: number;
  totalTerritories: number;
  totalXp: number;
  warsWon: number;
  warsLost: number;
  createdAt: admin.firestore.Timestamp;
  emblemId: string;
  settings: AllianceSettings;
}

export interface AllianceSettings {
  allowInvites: boolean; // Officers can invite
  minLevelToJoin: number;
}

export interface AllianceMember {
  userId: string;
  displayName: string;
  role: AllianceRole;
  joinedAt: admin.firestore.Timestamp;
  xpContributed: number;
  territoriesContributed: number;
  lastActive: admin.firestore.Timestamp;
}

export interface AllianceInvitation {
  id: string;
  allianceId: string;
  allianceName: string;
  invitedUserId: string;
  invitedBy: string;
  invitedByName: string;
  createdAt: admin.firestore.Timestamp;
  expiresAt: admin.firestore.Timestamp;
  status: 'pending' | 'accepted' | 'declined' | 'expired';
}

export interface AllianceWar {
  id: string;
  challengerAllianceId: string;
  challengerAllianceName: string;
  defenderAllianceId: string;
  defenderAllianceName: string;
  
  // Enhanced war status with phases
  status: WarStatus;
  phase: WarPhase;
  
  // Timeline (24hr warning → 48hr war → 72hr peace)
  declaredAt: admin.firestore.Timestamp;     // When war was declared
  warningEndsAt: admin.firestore.Timestamp;  // +24hr - when fighting begins
  startsAt: admin.firestore.Timestamp;       // Same as warningEndsAt (for backwards compat)
  endsAt: admin.firestore.Timestamp;         // +48hr from start - when fighting ends
  peaceTreatyEndsAt?: admin.firestore.Timestamp; // +72hr from end - can't war again
  
  // Scores
  challengerScore: number;
  defenderScore: number;
  
  // Participation tracking
  challengerParticipants?: WarParticipant[];
  defenderParticipants?: WarParticipant[];
  
  // Results
  winnerId?: string;
  winnerName?: string;
  rewards?: WarRewards;
  
  // War battles
  totalBattles?: number;
  challengerBattlesWon?: number;
  defenderBattlesWon?: number;
}

/** War status for simple queries */
export type WarStatus = 'pending' | 'active' | 'completed' | 'cancelled';

/** War phase for detailed state tracking */
export type WarPhase = 
  | 'warning'      // 24hr preparation period
  | 'active'       // 48hr active combat
  | 'ended'        // War complete, determining winner
  | 'peace_treaty' // 72hr cooldown, no new wars
  | 'cancelled';   // War was cancelled before starting

/** Individual participant contribution */
export interface WarParticipant {
  oderId: string;
  displayName: string;
  attacksLaunched: number;
  defensesWon: number;
  territoriesCaptured: number;
  scoreContributed: number;
}

/** War timeline configuration */
export const WAR_TIMELINE = {
  WARNING_HOURS: 24,      // Preparation period before war starts
  DURATION_HOURS: 48,     // Active war duration
  PEACE_TREATY_HOURS: 72, // Cooldown after war ends
  MIN_MEMBERS_TO_WAR: 3,  // Minimum alliance size to declare war
  WAR_BATTLE_BONUS_XP: 1.5, // XP bonus for battles during war
};

export interface WarRewards {
  winnerXp: number;
  winnerGems: number;
  loserXp: number;
}

// ============================================================================
// Daily Rewards Types
// ============================================================================

export interface DailyRewardState {
  currentDay: number; // 1-7
  currentStreak: number;
  lastClaimDate?: string; // YYYY-MM-DD
  totalClaims: number;
}

export interface DailyReward {
  day: number;
  rewards: Partial<UserResources>;
  xp: number;
  streakBonus: number; // Multiplier
}

export const DAILY_REWARDS: DailyReward[] = [
  { day: 1, rewards: { stone: 100, wood: 100 }, xp: 10, streakBonus: 1.0 },
  { day: 2, rewards: { iron: 50, crystal: 25 }, xp: 15, streakBonus: 1.1 },
  { day: 3, rewards: { stone: 200, wood: 200, iron: 100 }, xp: 25, streakBonus: 1.2 },
  { day: 4, rewards: { crystal: 50, gems: 10, arcaneEssence: 5 }, xp: 35, streakBonus: 1.3 },
  { day: 5, rewards: { stone: 300, iron: 150 }, xp: 50, streakBonus: 1.4 },
  { day: 6, rewards: { crystal: 100, gems: 25, arcaneEssence: 10 }, xp: 75, streakBonus: 1.5 },
  { day: 7, rewards: { stone: 500, wood: 500, iron: 250, crystal: 100, gems: 50, arcaneEssence: 25 }, xp: 100, streakBonus: 2.0 }
];

// ============================================================================
// Achievement Types
// ============================================================================

export type AchievementCategory = 
  | 'territory' 
  | 'building' 
  | 'combat' 
  | 'social' 
  | 'resources' 
  | 'exploration' 
  | 'milestones';

export interface Achievement {
  id: string;
  name: string;
  description: string;
  category: AchievementCategory;
  icon: string;
  requirement: AchievementRequirement;
  rewards: AchievementRewards;
  isHidden: boolean; // Hidden until unlocked
  order: number; // Display order
}

export interface AchievementRequirement {
  type: string;
  target: number;
  // Additional conditions
  condition?: Record<string, unknown>;
}

export interface AchievementRewards {
  xp: number;
  gems: number;
  title?: string; // Unlockable title
  badge?: string; // Badge ID
}

export interface UserAchievement {
  achievementId: string;
  progress: number;
  unlockedAt?: admin.firestore.Timestamp;
  claimed: boolean;
  claimedAt?: admin.firestore.Timestamp;
}

// ============================================================================
// Leaderboard Types
// ============================================================================

export type LeaderboardType = 
  | 'global_xp' 
  | 'global_territories' 
  | 'global_raids' 
  | 'weekly_xp' 
  | 'alliance_xp'
  | 'regional';

export interface LeaderboardEntry {
  rank: number;
  userId: string;
  displayName: string;
  allianceTag?: string;
  score: number;
  previousRank?: number;
  change?: number; // Positive = moved up
}

export interface Leaderboard {
  id: string;
  type: LeaderboardType;
  region?: string; // For regional leaderboards
  entries: LeaderboardEntry[];
  updatedAt: admin.firestore.Timestamp;
  periodStart?: admin.firestore.Timestamp; // For weekly
  periodEnd?: admin.firestore.Timestamp;
}

// ============================================================================
// Notification Types
// ============================================================================

export type NotificationType = 
  | 'territory_attacked'
  | 'territory_conquered'
  | 'territory_defended'
  | 'alliance_invite'
  | 'alliance_war_started'
  | 'alliance_war_ended'
  | 'alliance_war_declared'
  | 'alliance_war_incoming'
  | 'alliance_war_cancelled'
  | 'achievement_unlocked'
  | 'daily_reward_available'
  | 'level_up'
  | 'system_announcement'
  // Friends & Social
  | 'friend_request'
  | 'friend_request_accepted'
  | 'gift_received'
  // Chat
  | 'chat_mention'
  | 'direct_message'
  | 'chat_muted'
  // Referrals
  | 'referral_used'
  | 'referral_qualified'
  // World Events
  | 'world_event_starting'
  | 'world_event_reward';

export interface Notification {
  id: string;
  userId: string;
  type: NotificationType;
  title: string;
  body: string;
  data?: Record<string, string>;
  read: boolean;
  createdAt: admin.firestore.Timestamp;
}

// ============================================================================
// Resource Node Types
// ============================================================================

export type ResourceNodeType = 'stone_quarry' | 'forest' | 'ore_mine' | 'crystal_cave' | 'gem_mine';

export interface ResourceNode {
  id: string;
  type: ResourceNodeType;
  latitude: number;
  longitude: number;
  geoHash: string;
  resourceType: keyof UserResources;
  baseYield: number;
  currentYield: number;
  maxYield: number;
  regenerationRate: number; // Per hour
  lastHarvestedAt?: admin.firestore.Timestamp;
  ownerId?: string; // If claimed
  ownerAllianceId?: string;
}

// ============================================================================
// XP & Level System
// ============================================================================

export const XP_PER_LEVEL: number[] = [
  0,      // Level 1 (starting)
  100,    // Level 2
  250,    // Level 3
  500,    // Level 4
  1000,   // Level 5
  1750,   // Level 6
  2750,   // Level 7
  4000,   // Level 8
  5500,   // Level 9
  7500,   // Level 10
  10000,  // Level 11
  13000,  // Level 12
  16500,  // Level 13
  20500,  // Level 14
  25000,  // Level 15
  30000,  // Level 16
  36000,  // Level 17
  43000,  // Level 18
  51000,  // Level 19
  60000,  // Level 20
  // Beyond level 20, each level requires 10000 more XP
];

export function calculateLevel(totalXp: number): number {
  for (let level = XP_PER_LEVEL.length - 1; level >= 0; level--) {
    if (totalXp >= XP_PER_LEVEL[level]) {
      // For levels beyond our table
      if (level === XP_PER_LEVEL.length - 1) {
        const extraXp = totalXp - XP_PER_LEVEL[level];
        return level + 1 + Math.floor(extraXp / 10000);
      }
      return level + 1;
    }
  }
  return 1;
}

export function getXpForNextLevel(currentLevel: number): number {
  if (currentLevel < XP_PER_LEVEL.length) {
    return XP_PER_LEVEL[currentLevel];
  }
  // Beyond our table
  return XP_PER_LEVEL[XP_PER_LEVEL.length - 1] + (currentLevel - XP_PER_LEVEL.length + 1) * 10000;
}

// ============================================================================
// Spatial Anchor Types (for AR)
// ============================================================================

export type AnchorType = 'Local' | 'Geospatial' | 'Cloud';

export interface SpatialAnchor {
  id: string;
  latitude: number;
  longitude: number;
  altitude: number;
  rotationX: number;
  rotationY: number;
  rotationZ: number;
  rotationW: number;
  createdAt: admin.firestore.Timestamp;
  anchorType: AnchorType;
  ownerId: string;
  attachedObjectType: string;
  attachedObjectId: string;
  geoHash: string;
}
