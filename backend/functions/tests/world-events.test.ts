/**
 * World Events System Tests
 * Tests for FOMO-style limited time events
 */

// World Event types
interface WorldEvent {
    id: string;
    name: string;
    description: string;
    type: 'boss_raid' | 'resource_rush' | 'territory_war' | 'seasonal';
    status: 'scheduled' | 'active' | 'completed' | 'cancelled';
    startTime: Date;
    endTime: Date;
    maxParticipants: number;
    currentParticipants: number;
    requirements: EventRequirements;
    rewards: EventRewards;
    progress: number;
    goal: number;
}

interface EventRequirements {
    minLevel: number;
    allianceRequired: boolean;
    entryFee?: { type: string; amount: number };
}

interface EventRewards {
    xp: number;
    resources: { type: string; amount: number }[];
    specialItems?: string[];
    achievementId?: string;
}

interface EventParticipant {
    playerId: string;
    joinedAt: Date;
    contribution: number;
    rewardsClaimed: boolean;
}

// World Events logic
class WorldEventSystem {
    static canJoinEvent(player: { level: number; allianceId: string | null; resources: Record<string, number> }, event: WorldEvent): { allowed: boolean; reason?: string } {
        // Check status
        if (event.status !== 'active') {
            return { allowed: false, reason: 'Event is not active' };
        }

        // Check time
        const now = new Date();
        if (now < event.startTime) {
            return { allowed: false, reason: 'Event has not started' };
        }
        if (now > event.endTime) {
            return { allowed: false, reason: 'Event has ended' };
        }

        // Check capacity
        if (event.currentParticipants >= event.maxParticipants) {
            return { allowed: false, reason: 'Event is full' };
        }

        // Check level
        if (player.level < event.requirements.minLevel) {
            return { allowed: false, reason: 'Level requirement not met' };
        }

        // Check alliance
        if (event.requirements.allianceRequired && !player.allianceId) {
            return { allowed: false, reason: 'Alliance membership required' };
        }

        // Check entry fee
        if (event.requirements.entryFee) {
            const { type, amount } = event.requirements.entryFee;
            if (!player.resources[type] || player.resources[type] < amount) {
                return { allowed: false, reason: 'Insufficient resources for entry fee' };
            }
        }

        return { allowed: true };
    }

    static calculateContribution(player: { level: number; attackPower: number }, action: string, baseValue: number): number {
        let contribution = baseValue;

        // Level bonus
        const levelBonus = 1 + (player.level - 1) * 0.05;
        contribution *= levelBonus;

        // Action type multiplier
        switch (action) {
            case 'attack':
                contribution *= 1.0;
                break;
            case 'defend':
                contribution *= 0.8;
                break;
            case 'gather':
                contribution *= 0.5;
                break;
            case 'boss_damage':
                contribution *= 1.5;
                break;
        }

        return Math.floor(contribution);
    }

    static calculateRewardMultiplier(contribution: number, totalContribution: number, isTopContributor: boolean): number {
        // Base multiplier from contribution percentage
        const percentage = totalContribution > 0 ? contribution / totalContribution : 0;
        let multiplier = 1 + percentage;

        // Top contributor bonus
        if (isTopContributor) {
            multiplier *= 1.5;
        }

        // Cap at 3x
        return Math.min(3, multiplier);
    }

    static isEventSuccessful(event: WorldEvent): boolean {
        return event.progress >= event.goal;
    }

    static calculateEventProgress(contributions: number[]): number {
        return contributions.reduce((sum, c) => sum + c, 0);
    }

    static getTimeRemaining(event: WorldEvent): number {
        const now = new Date().getTime();
        const end = event.endTime.getTime();
        return Math.max(0, end - now);
    }

    static shouldActivateEvent(event: WorldEvent): boolean {
        if (event.status !== 'scheduled') return false;
        const now = new Date();
        return now >= event.startTime;
    }

    static shouldEndEvent(event: WorldEvent): boolean {
        if (event.status !== 'active') return false;
        const now = new Date();
        return now >= event.endTime;
    }
}

describe('World Events System', () => {
    describe('Event Joining', () => {
        const baseEvent: WorldEvent = {
            id: 'event_1',
            name: 'Dragon Raid',
            description: 'Defeat the ancient dragon',
            type: 'boss_raid',
            status: 'active',
            startTime: new Date(Date.now() - 3600000),
            endTime: new Date(Date.now() + 3600000),
            maxParticipants: 100,
            currentParticipants: 50,
            requirements: {
                minLevel: 5,
                allianceRequired: false
            },
            rewards: {
                xp: 1000,
                resources: [{ type: 'crystal', amount: 100 }]
            },
            progress: 0,
            goal: 10000
        };

        const basePlayer = {
            level: 10,
            allianceId: null,
            resources: { gold: 1000 }
        };

        it('should allow eligible player to join', () => {
            const result = WorldEventSystem.canJoinEvent(basePlayer, baseEvent);
            expect(result.allowed).toBe(true);
        });

        it('should prevent joining inactive event', () => {
            const inactive = { ...baseEvent, status: 'scheduled' as const };
            const result = WorldEventSystem.canJoinEvent(basePlayer, inactive);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Event is not active');
        });

        it('should prevent joining event not yet started', () => {
            const future = { ...baseEvent, startTime: new Date(Date.now() + 86400000) };
            const result = WorldEventSystem.canJoinEvent(basePlayer, future);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Event has not started');
        });

        it('should prevent joining ended event', () => {
            const ended = { ...baseEvent, endTime: new Date(Date.now() - 3600000) };
            const result = WorldEventSystem.canJoinEvent(basePlayer, ended);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Event has ended');
        });

        it('should prevent joining full event', () => {
            const full = { ...baseEvent, currentParticipants: 100 };
            const result = WorldEventSystem.canJoinEvent(basePlayer, full);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Event is full');
        });

        it('should enforce level requirement', () => {
            const lowLevel = { ...basePlayer, level: 3 };
            const result = WorldEventSystem.canJoinEvent(lowLevel, baseEvent);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Level requirement not met');
        });

        it('should enforce alliance requirement', () => {
            const allianceEvent = { 
                ...baseEvent, 
                requirements: { ...baseEvent.requirements, allianceRequired: true } 
            };
            const result = WorldEventSystem.canJoinEvent(basePlayer, allianceEvent);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Alliance membership required');
        });

        it('should allow if player has alliance', () => {
            const allianceEvent = { 
                ...baseEvent, 
                requirements: { ...baseEvent.requirements, allianceRequired: true } 
            };
            const alliancePlayer = { ...basePlayer, allianceId: 'alliance_1' };
            const result = WorldEventSystem.canJoinEvent(alliancePlayer, allianceEvent);
            expect(result.allowed).toBe(true);
        });

        it('should check entry fee', () => {
            const feeEvent = {
                ...baseEvent,
                requirements: { 
                    ...baseEvent.requirements, 
                    entryFee: { type: 'gems', amount: 100 } 
                }
            };
            const poorPlayer = { ...basePlayer, resources: { gems: 50 } };
            const result = WorldEventSystem.canJoinEvent(poorPlayer, feeEvent);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Insufficient resources for entry fee');
        });

        it('should allow if player has enough for fee', () => {
            const feeEvent = {
                ...baseEvent,
                requirements: { 
                    ...baseEvent.requirements, 
                    entryFee: { type: 'gold', amount: 100 } 
                }
            };
            const result = WorldEventSystem.canJoinEvent(basePlayer, feeEvent);
            expect(result.allowed).toBe(true);
        });
    });

    describe('Contribution Calculation', () => {
        const player = { level: 10, attackPower: 100 };

        it('should calculate attack contribution', () => {
            const contribution = WorldEventSystem.calculateContribution(player, 'attack', 100);
            // 100 * (1 + 9*0.05) * 1.0 = 145
            expect(contribution).toBe(145);
        });

        it('should calculate defend contribution with 0.8x', () => {
            const contribution = WorldEventSystem.calculateContribution(player, 'defend', 100);
            // 100 * 1.45 * 0.8 = 116
            expect(contribution).toBe(116);
        });

        it('should calculate gather contribution with 0.5x', () => {
            const contribution = WorldEventSystem.calculateContribution(player, 'gather', 100);
            // 100 * 1.45 * 0.5 = 72
            expect(contribution).toBe(72);
        });

        it('should calculate boss_damage contribution with 1.5x', () => {
            const contribution = WorldEventSystem.calculateContribution(player, 'boss_damage', 100);
            // 100 * 1.45 * 1.5 = 217
            expect(contribution).toBe(217);
        });

        it('should scale with player level', () => {
            const level1 = WorldEventSystem.calculateContribution({ level: 1, attackPower: 100 }, 'attack', 100);
            const level20 = WorldEventSystem.calculateContribution({ level: 20, attackPower: 100 }, 'attack', 100);

            expect(level20).toBeGreaterThan(level1);
        });
    });

    describe('Reward Calculation', () => {
        it('should calculate base multiplier from contribution', () => {
            const multiplier = WorldEventSystem.calculateRewardMultiplier(1000, 10000, false);
            // 1 + 0.1 = 1.1
            expect(multiplier).toBeCloseTo(1.1);
        });

        it('should give bonus to top contributor', () => {
            const regular = WorldEventSystem.calculateRewardMultiplier(1000, 10000, false);
            const top = WorldEventSystem.calculateRewardMultiplier(1000, 10000, true);

            expect(top).toBeGreaterThan(regular);
            expect(top).toBeCloseTo(1.65); // 1.1 * 1.5
        });

        it('should cap multiplier at 3x', () => {
            const multiplier = WorldEventSystem.calculateRewardMultiplier(10000, 10000, true);
            expect(multiplier).toBe(3);
        });

        it('should handle zero total contribution', () => {
            const multiplier = WorldEventSystem.calculateRewardMultiplier(100, 0, false);
            expect(multiplier).toBe(1);
        });
    });

    describe('Event Success', () => {
        it('should be successful when goal reached', () => {
            const event: WorldEvent = {
                id: 'event_1',
                name: 'Test Event',
                description: 'Test',
                type: 'boss_raid',
                status: 'completed',
                startTime: new Date(),
                endTime: new Date(),
                maxParticipants: 100,
                currentParticipants: 50,
                requirements: { minLevel: 1, allianceRequired: false },
                rewards: { xp: 100, resources: [] },
                progress: 10000,
                goal: 10000
            };

            expect(WorldEventSystem.isEventSuccessful(event)).toBe(true);
        });

        it('should fail when goal not reached', () => {
            const event: WorldEvent = {
                id: 'event_1',
                name: 'Test Event',
                description: 'Test',
                type: 'boss_raid',
                status: 'completed',
                startTime: new Date(),
                endTime: new Date(),
                maxParticipants: 100,
                currentParticipants: 50,
                requirements: { minLevel: 1, allianceRequired: false },
                rewards: { xp: 100, resources: [] },
                progress: 5000,
                goal: 10000
            };

            expect(WorldEventSystem.isEventSuccessful(event)).toBe(false);
        });
    });

    describe('Event Progress', () => {
        it('should sum all contributions', () => {
            const contributions = [100, 200, 300, 150, 250];
            const progress = WorldEventSystem.calculateEventProgress(contributions);
            expect(progress).toBe(1000);
        });

        it('should return 0 for empty contributions', () => {
            const progress = WorldEventSystem.calculateEventProgress([]);
            expect(progress).toBe(0);
        });
    });

    describe('Event Timing', () => {
        it('should calculate remaining time', () => {
            const event: WorldEvent = {
                id: 'event_1',
                name: 'Test Event',
                description: 'Test',
                type: 'boss_raid',
                status: 'active',
                startTime: new Date(),
                endTime: new Date(Date.now() + 3600000), // 1 hour from now
                maxParticipants: 100,
                currentParticipants: 0,
                requirements: { minLevel: 1, allianceRequired: false },
                rewards: { xp: 100, resources: [] },
                progress: 0,
                goal: 10000
            };

            const remaining = WorldEventSystem.getTimeRemaining(event);
            expect(remaining).toBeGreaterThan(3500000);
            expect(remaining).toBeLessThanOrEqual(3600000);
        });

        it('should return 0 for ended events', () => {
            const event: WorldEvent = {
                id: 'event_1',
                name: 'Test Event',
                description: 'Test',
                type: 'boss_raid',
                status: 'completed',
                startTime: new Date(Date.now() - 7200000),
                endTime: new Date(Date.now() - 3600000), // Ended 1 hour ago
                maxParticipants: 100,
                currentParticipants: 0,
                requirements: { minLevel: 1, allianceRequired: false },
                rewards: { xp: 100, resources: [] },
                progress: 0,
                goal: 10000
            };

            expect(WorldEventSystem.getTimeRemaining(event)).toBe(0);
        });

        it('should activate scheduled events at start time', () => {
            const scheduled: WorldEvent = {
                id: 'event_1',
                name: 'Test Event',
                description: 'Test',
                type: 'boss_raid',
                status: 'scheduled',
                startTime: new Date(Date.now() - 1000), // Started 1 second ago
                endTime: new Date(Date.now() + 3600000),
                maxParticipants: 100,
                currentParticipants: 0,
                requirements: { minLevel: 1, allianceRequired: false },
                rewards: { xp: 100, resources: [] },
                progress: 0,
                goal: 10000
            };

            expect(WorldEventSystem.shouldActivateEvent(scheduled)).toBe(true);
        });

        it('should not activate future events', () => {
            const future: WorldEvent = {
                id: 'event_1',
                name: 'Test Event',
                description: 'Test',
                type: 'boss_raid',
                status: 'scheduled',
                startTime: new Date(Date.now() + 3600000), // Starts in 1 hour
                endTime: new Date(Date.now() + 7200000),
                maxParticipants: 100,
                currentParticipants: 0,
                requirements: { minLevel: 1, allianceRequired: false },
                rewards: { xp: 100, resources: [] },
                progress: 0,
                goal: 10000
            };

            expect(WorldEventSystem.shouldActivateEvent(future)).toBe(false);
        });

        it('should end active events past end time', () => {
            const active: WorldEvent = {
                id: 'event_1',
                name: 'Test Event',
                description: 'Test',
                type: 'boss_raid',
                status: 'active',
                startTime: new Date(Date.now() - 7200000),
                endTime: new Date(Date.now() - 1000), // Ended 1 second ago
                maxParticipants: 100,
                currentParticipants: 0,
                requirements: { minLevel: 1, allianceRequired: false },
                rewards: { xp: 100, resources: [] },
                progress: 0,
                goal: 10000
            };

            expect(WorldEventSystem.shouldEndEvent(active)).toBe(true);
        });
    });
});
