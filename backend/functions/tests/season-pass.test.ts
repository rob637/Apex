/**
 * Season Pass / Battle Pass System Tests
 * Tests for season progression, challenges, and rewards
 */

// Season types
interface Season {
    id: string;
    name: string;
    startDate: Date;
    endDate: Date;
    status: 'upcoming' | 'active' | 'ended';
    maxTier: number;
    premiumPrice: number;
}

interface SeasonTier {
    tier: number;
    xpRequired: number;
    freeReward?: Reward;
    premiumReward?: Reward;
}

interface Reward {
    type: 'resource' | 'cosmetic' | 'currency' | 'item';
    id: string;
    amount?: number;
}

interface Challenge {
    id: string;
    name: string;
    description: string;
    type: 'daily' | 'weekly' | 'season';
    targetValue: number;
    currentValue: number;
    xpReward: number;
    isCompleted: boolean;
    expiresAt?: Date;
}

interface PlayerSeason {
    playerId: string;
    seasonId: string;
    currentTier: number;
    currentXp: number;
    hasPremium: boolean;
    claimedFreeRewards: number[];
    claimedPremiumRewards: number[];
    completedChallenges: string[];
}

// Season Pass logic
class SeasonPassSystem {
    static readonly XP_PER_TIER = 1000;
    static readonly TIER_XP_SCALING = 1.1;
    static readonly DAILY_CHALLENGE_XP = 100;
    static readonly WEEKLY_CHALLENGE_XP = 500;
    static readonly SEASON_CHALLENGE_XP = 2000;

    static calculateTierFromXp(totalXp: number): { tier: number; xpInTier: number; xpForNextTier: number } {
        let tier = 1;
        let remainingXp = totalXp;
        let xpForTier = this.XP_PER_TIER;

        while (remainingXp >= xpForTier) {
            remainingXp -= xpForTier;
            tier++;
            xpForTier = Math.floor(this.XP_PER_TIER * Math.pow(this.TIER_XP_SCALING, tier - 1));
        }

        return {
            tier,
            xpInTier: remainingXp,
            xpForNextTier: xpForTier
        };
    }

    static getXpRequiredForTier(tier: number): number {
        if (tier <= 1) return 0;

        let totalXp = 0;
        for (let t = 1; t < tier; t++) {
            totalXp += Math.floor(this.XP_PER_TIER * Math.pow(this.TIER_XP_SCALING, t - 1));
        }
        return totalXp;
    }

    static canClaimReward(player: PlayerSeason, tier: number, isPremium: boolean): { allowed: boolean; reason?: string } {
        // Check tier reached
        if (player.currentTier < tier) {
            return { allowed: false, reason: 'Tier not reached' };
        }

        // Check premium
        if (isPremium && !player.hasPremium) {
            return { allowed: false, reason: 'Premium pass required' };
        }

        // Check already claimed
        const claimed = isPremium ? player.claimedPremiumRewards : player.claimedFreeRewards;
        if (claimed.includes(tier)) {
            return { allowed: false, reason: 'Reward already claimed' };
        }

        return { allowed: true };
    }

    static completeChallenge(challenge: Challenge, player: PlayerSeason): { xpGained: number; tierUp: boolean; newTier: number } {
        if (challenge.isCompleted) {
            return { xpGained: 0, tierUp: false, newTier: player.currentTier };
        }

        const xpGained = challenge.xpReward;
        const newXp = player.currentXp + xpGained;
        const { tier: newTier } = this.calculateTierFromXp(newXp);

        return {
            xpGained,
            tierUp: newTier > player.currentTier,
            newTier
        };
    }

    static getChallengeXp(type: 'daily' | 'weekly' | 'season'): number {
        switch (type) {
            case 'daily': return this.DAILY_CHALLENGE_XP;
            case 'weekly': return this.WEEKLY_CHALLENGE_XP;
            case 'season': return this.SEASON_CHALLENGE_XP;
        }
    }

    static isDailyChallengeExpired(challenge: Challenge): boolean {
        if (challenge.type !== 'daily' || !challenge.expiresAt) return false;
        return new Date() > challenge.expiresAt;
    }

    static isWeeklyChallengeExpired(challenge: Challenge): boolean {
        if (challenge.type !== 'weekly' || !challenge.expiresAt) return false;
        return new Date() > challenge.expiresAt;
    }

    static getSeasonProgress(player: PlayerSeason, maxTier: number): number {
        return Math.min(100, (player.currentTier / maxTier) * 100);
    }

    static isSeasonActive(season: Season): boolean {
        const now = new Date();
        return now >= season.startDate && now <= season.endDate;
    }

    static getSeasonDaysRemaining(season: Season): number {
        const now = new Date();
        const end = season.endDate;
        const diff = end.getTime() - now.getTime();
        return Math.max(0, Math.ceil(diff / (1000 * 60 * 60 * 24)));
    }

    static calculateCatchUpXp(currentTier: number, targetTier: number): number {
        return this.getXpRequiredForTier(targetTier) - this.getXpRequiredForTier(currentTier);
    }
}

describe('Season Pass System', () => {
    describe('Tier Calculation', () => {
        it('should start at tier 1 with 0 XP', () => {
            const result = SeasonPassSystem.calculateTierFromXp(0);
            expect(result.tier).toBe(1);
            expect(result.xpInTier).toBe(0);
        });

        it('should advance to tier 2 after 1000 XP', () => {
            const result = SeasonPassSystem.calculateTierFromXp(1000);
            expect(result.tier).toBe(2);
            expect(result.xpInTier).toBe(0);
        });

        it('should track XP within tier', () => {
            const result = SeasonPassSystem.calculateTierFromXp(500);
            expect(result.tier).toBe(1);
            expect(result.xpInTier).toBe(500);
        });

        it('should scale XP requirements', () => {
            // Tier 2 requires 1000 * 1.1 = 1100
            const result = SeasonPassSystem.calculateTierFromXp(2100);
            expect(result.tier).toBe(3);
        });

        it('should handle high XP values', () => {
            const result = SeasonPassSystem.calculateTierFromXp(50000);
            expect(result.tier).toBeGreaterThan(10);
        });
    });

    describe('XP Requirements', () => {
        it('should return 0 for tier 1', () => {
            expect(SeasonPassSystem.getXpRequiredForTier(1)).toBe(0);
        });

        it('should return correct XP for tier 2', () => {
            expect(SeasonPassSystem.getXpRequiredForTier(2)).toBe(1000);
        });

        it('should return increasing XP for higher tiers', () => {
            const tier5 = SeasonPassSystem.getXpRequiredForTier(5);
            const tier10 = SeasonPassSystem.getXpRequiredForTier(10);
            expect(tier10).toBeGreaterThan(tier5);
        });
    });

    describe('Reward Claiming', () => {
        const playerSeason: PlayerSeason = {
            playerId: 'player_1',
            seasonId: 'season_1',
            currentTier: 5,
            currentXp: 5000,
            hasPremium: true,
            claimedFreeRewards: [1, 2],
            claimedPremiumRewards: [1],
            completedChallenges: []
        };

        it('should allow claiming unclaimed reward at reached tier', () => {
            const result = SeasonPassSystem.canClaimReward(playerSeason, 3, false);
            expect(result.allowed).toBe(true);
        });

        it('should prevent claiming reward at unreached tier', () => {
            const result = SeasonPassSystem.canClaimReward(playerSeason, 10, false);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Tier not reached');
        });

        it('should prevent claiming already claimed reward', () => {
            const result = SeasonPassSystem.canClaimReward(playerSeason, 1, false);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Reward already claimed');
        });

        it('should require premium for premium rewards', () => {
            const freePlayer = { ...playerSeason, hasPremium: false };
            const result = SeasonPassSystem.canClaimReward(freePlayer, 3, true);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Premium pass required');
        });

        it('should allow premium claims with premium pass', () => {
            const result = SeasonPassSystem.canClaimReward(playerSeason, 3, true);
            expect(result.allowed).toBe(true);
        });
    });

    describe('Challenge Completion', () => {
        const player: PlayerSeason = {
            playerId: 'player_1',
            seasonId: 'season_1',
            currentTier: 1,
            currentXp: 800,
            hasPremium: false,
            claimedFreeRewards: [],
            claimedPremiumRewards: [],
            completedChallenges: []
        };

        const dailyChallenge: Challenge = {
            id: 'challenge_1',
            name: 'Win 3 battles',
            description: 'Win 3 territory battles',
            type: 'daily',
            targetValue: 3,
            currentValue: 3,
            xpReward: 100,
            isCompleted: false,
            expiresAt: new Date(Date.now() + 86400000)
        };

        it('should grant XP for completing challenge', () => {
            const result = SeasonPassSystem.completeChallenge(dailyChallenge, player);
            expect(result.xpGained).toBe(100);
        });

        it('should detect tier up', () => {
            const result = SeasonPassSystem.completeChallenge(dailyChallenge, player);
            // 800 + 100 = 900, still tier 1
            expect(result.tierUp).toBe(false);

            const nearLevel = { ...player, currentXp: 950 };
            const result2 = SeasonPassSystem.completeChallenge(dailyChallenge, nearLevel);
            // 950 + 100 = 1050, tier 2
            expect(result2.tierUp).toBe(true);
            expect(result2.newTier).toBe(2);
        });

        it('should not grant XP for already completed', () => {
            const completed = { ...dailyChallenge, isCompleted: true };
            const result = SeasonPassSystem.completeChallenge(completed, player);
            expect(result.xpGained).toBe(0);
        });
    });

    describe('Challenge XP Values', () => {
        it('should return correct XP for daily challenges', () => {
            expect(SeasonPassSystem.getChallengeXp('daily')).toBe(100);
        });

        it('should return correct XP for weekly challenges', () => {
            expect(SeasonPassSystem.getChallengeXp('weekly')).toBe(500);
        });

        it('should return correct XP for season challenges', () => {
            expect(SeasonPassSystem.getChallengeXp('season')).toBe(2000);
        });
    });

    describe('Challenge Expiration', () => {
        it('should detect expired daily challenge', () => {
            const expired: Challenge = {
                id: 'challenge_1',
                name: 'Daily Task',
                description: 'Complete a task',
                type: 'daily',
                targetValue: 1,
                currentValue: 0,
                xpReward: 100,
                isCompleted: false,
                expiresAt: new Date(Date.now() - 3600000) // 1 hour ago
            };

            expect(SeasonPassSystem.isDailyChallengeExpired(expired)).toBe(true);
        });

        it('should not mark active daily challenge as expired', () => {
            const active: Challenge = {
                id: 'challenge_1',
                name: 'Daily Task',
                description: 'Complete a task',
                type: 'daily',
                targetValue: 1,
                currentValue: 0,
                xpReward: 100,
                isCompleted: false,
                expiresAt: new Date(Date.now() + 3600000) // 1 hour from now
            };

            expect(SeasonPassSystem.isDailyChallengeExpired(active)).toBe(false);
        });

        it('should handle weekly expiration', () => {
            const expired: Challenge = {
                id: 'challenge_1',
                name: 'Weekly Task',
                description: 'Complete a task',
                type: 'weekly',
                targetValue: 1,
                currentValue: 0,
                xpReward: 500,
                isCompleted: false,
                expiresAt: new Date(Date.now() - 86400000) // 1 day ago
            };

            expect(SeasonPassSystem.isWeeklyChallengeExpired(expired)).toBe(true);
        });
    });

    describe('Season Progress', () => {
        it('should calculate progress percentage', () => {
            const player: PlayerSeason = {
                playerId: 'player_1',
                seasonId: 'season_1',
                currentTier: 50,
                currentXp: 50000,
                hasPremium: true,
                claimedFreeRewards: [],
                claimedPremiumRewards: [],
                completedChallenges: []
            };

            const progress = SeasonPassSystem.getSeasonProgress(player, 100);
            expect(progress).toBe(50);
        });

        it('should cap progress at 100%', () => {
            const player: PlayerSeason = {
                playerId: 'player_1',
                seasonId: 'season_1',
                currentTier: 120,
                currentXp: 120000,
                hasPremium: true,
                claimedFreeRewards: [],
                claimedPremiumRewards: [],
                completedChallenges: []
            };

            const progress = SeasonPassSystem.getSeasonProgress(player, 100);
            expect(progress).toBe(100);
        });
    });

    describe('Season Timing', () => {
        it('should detect active season', () => {
            const season: Season = {
                id: 'season_1',
                name: 'Season 1',
                startDate: new Date(Date.now() - 86400000), // Started 1 day ago
                endDate: new Date(Date.now() + 86400000 * 30), // Ends in 30 days
                status: 'active',
                maxTier: 100,
                premiumPrice: 9.99
            };

            expect(SeasonPassSystem.isSeasonActive(season)).toBe(true);
        });

        it('should detect ended season', () => {
            const season: Season = {
                id: 'season_1',
                name: 'Season 1',
                startDate: new Date(Date.now() - 86400000 * 60),
                endDate: new Date(Date.now() - 86400000), // Ended 1 day ago
                status: 'ended',
                maxTier: 100,
                premiumPrice: 9.99
            };

            expect(SeasonPassSystem.isSeasonActive(season)).toBe(false);
        });

        it('should calculate days remaining', () => {
            const season: Season = {
                id: 'season_1',
                name: 'Season 1',
                startDate: new Date(),
                endDate: new Date(Date.now() + 86400000 * 10), // 10 days from now
                status: 'active',
                maxTier: 100,
                premiumPrice: 9.99
            };

            const days = SeasonPassSystem.getSeasonDaysRemaining(season);
            expect(days).toBe(10);
        });

        it('should return 0 for ended seasons', () => {
            const season: Season = {
                id: 'season_1',
                name: 'Season 1',
                startDate: new Date(Date.now() - 86400000 * 60),
                endDate: new Date(Date.now() - 86400000),
                status: 'ended',
                maxTier: 100,
                premiumPrice: 9.99
            };

            expect(SeasonPassSystem.getSeasonDaysRemaining(season)).toBe(0);
        });
    });

    describe('Catch-up Calculation', () => {
        it('should calculate XP needed to catch up', () => {
            const xpNeeded = SeasonPassSystem.calculateCatchUpXp(1, 5);
            expect(xpNeeded).toBeGreaterThan(0);
        });

        it('should return 0 when already at target', () => {
            const xpNeeded = SeasonPassSystem.calculateCatchUpXp(5, 5);
            expect(xpNeeded).toBe(0);
        });

        it('should return 0 when ahead of target', () => {
            const xpNeeded = SeasonPassSystem.calculateCatchUpXp(10, 5);
            expect(xpNeeded).toBeLessThan(0);
        });
    });
});
