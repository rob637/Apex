/**
 * Alliance System Tests
 * Tests for alliance creation, membership, wars, and management
 */

// Alliance types
interface Alliance {
    id: string;
    name: string;
    tag: string;
    leaderId: string;
    memberCount: number;
    maxMembers: number;
    level: number;
    xp: number;
    isRecruiting: boolean;
    minLevel: number;
    createdAt: Date;
    wars: AllianceWar[];
}

interface AllianceMember {
    id: string;
    playerId: string;
    allianceId: string;
    role: 'leader' | 'officer' | 'member';
    joinedAt: Date;
    contributedXp: number;
}

interface AllianceWar {
    id: string;
    attackerId: string;
    defenderId: string;
    status: 'pending' | 'active' | 'completed';
    attackerScore: number;
    defenderScore: number;
    startedAt: Date;
    endsAt: Date;
}

interface Player {
    id: string;
    level: number;
    allianceId: string | null;
}

// Alliance logic (simplified for testing)
class AllianceSystem {
    static readonly MAX_TAG_LENGTH = 4;
    static readonly MIN_NAME_LENGTH = 3;
    static readonly MAX_NAME_LENGTH = 20;
    static readonly BASE_MAX_MEMBERS = 10;
    static readonly WAR_DURATION_HOURS = 24;

    static validateAllianceName(name: string): { valid: boolean; error?: string } {
        if (name.length < this.MIN_NAME_LENGTH) {
            return { valid: false, error: 'Name too short' };
        }
        if (name.length > this.MAX_NAME_LENGTH) {
            return { valid: false, error: 'Name too long' };
        }
        if (!/^[a-zA-Z0-9\s]+$/.test(name)) {
            return { valid: false, error: 'Invalid characters in name' };
        }
        return { valid: true };
    }

    static validateTag(tag: string): { valid: boolean; error?: string } {
        if (tag.length < 2) {
            return { valid: false, error: 'Tag too short' };
        }
        if (tag.length > this.MAX_TAG_LENGTH) {
            return { valid: false, error: 'Tag too long' };
        }
        if (!/^[A-Z0-9]+$/.test(tag)) {
            return { valid: false, error: 'Tag must be uppercase letters/numbers' };
        }
        return { valid: true };
    }

    static canJoinAlliance(player: Player, alliance: Alliance): { allowed: boolean; reason?: string } {
        // Already in an alliance
        if (player.allianceId !== null) {
            return { allowed: false, reason: 'Already in an alliance' };
        }

        // Alliance not recruiting
        if (!alliance.isRecruiting) {
            return { allowed: false, reason: 'Alliance is not recruiting' };
        }

        // Level requirement
        if (player.level < alliance.minLevel) {
            return { allowed: false, reason: 'Level requirement not met' };
        }

        // Alliance full
        if (alliance.memberCount >= alliance.maxMembers) {
            return { allowed: false, reason: 'Alliance is full' };
        }

        return { allowed: true };
    }

    static calculateMaxMembers(level: number): number {
        return this.BASE_MAX_MEMBERS + (level - 1) * 5;
    }

    static calculateXpForLevel(level: number): number {
        return Math.floor(1000 * Math.pow(1.5, level - 1));
    }

    static canStartWar(attacker: Alliance, defender: Alliance): { allowed: boolean; reason?: string } {
        // Can't attack self
        if (attacker.id === defender.id) {
            return { allowed: false, reason: 'Cannot war with yourself' };
        }

        // Check existing war
        const existingWar = attacker.wars.find(w => 
            (w.attackerId === defender.id || w.defenderId === defender.id) && 
            (w.status === 'pending' || w.status === 'active')
        );
        if (existingWar) {
            return { allowed: false, reason: 'Already at war with this alliance' };
        }

        // Level difference check (within 5 levels)
        if (Math.abs(attacker.level - defender.level) > 5) {
            return { allowed: false, reason: 'Level difference too large' };
        }

        // Both alliances need minimum members
        if (attacker.memberCount < 3 || defender.memberCount < 3) {
            return { allowed: false, reason: 'Both alliances need at least 3 members' };
        }

        return { allowed: true };
    }

    static determineWarWinner(war: AllianceWar): string | null {
        if (war.status !== 'completed') {
            return null;
        }

        if (war.attackerScore > war.defenderScore) {
            return war.attackerId;
        } else if (war.defenderScore > war.attackerScore) {
            return war.defenderId;
        }

        return null; // Draw
    }

    static canPromoteMember(promoter: AllianceMember, target: AllianceMember, newRole: string): { allowed: boolean; reason?: string } {
        // Only leaders and officers can promote
        if (promoter.role !== 'leader' && promoter.role !== 'officer') {
            return { allowed: false, reason: 'Insufficient permissions' };
        }

        // Officers can only promote to member
        if (promoter.role === 'officer' && newRole !== 'member') {
            return { allowed: false, reason: 'Officers can only promote to member' };
        }

        // Can't promote to leader (must transfer leadership)
        if (newRole === 'leader') {
            return { allowed: false, reason: 'Use transfer leadership instead' };
        }

        // Can't promote self
        if (promoter.id === target.id) {
            return { allowed: false, reason: 'Cannot promote yourself' };
        }

        return { allowed: true };
    }
}

describe('Alliance System', () => {
    describe('Name Validation', () => {
        it('should accept valid alliance names', () => {
            expect(AllianceSystem.validateAllianceName('Phoenix Legion').valid).toBe(true);
            expect(AllianceSystem.validateAllianceName('Team123').valid).toBe(true);
            expect(AllianceSystem.validateAllianceName('ABC').valid).toBe(true);
        });

        it('should reject names that are too short', () => {
            const result = AllianceSystem.validateAllianceName('AB');
            expect(result.valid).toBe(false);
            expect(result.error).toBe('Name too short');
        });

        it('should reject names that are too long', () => {
            const result = AllianceSystem.validateAllianceName('A'.repeat(25));
            expect(result.valid).toBe(false);
            expect(result.error).toBe('Name too long');
        });

        it('should reject names with invalid characters', () => {
            expect(AllianceSystem.validateAllianceName('Test!').valid).toBe(false);
            expect(AllianceSystem.validateAllianceName('Test@123').valid).toBe(false);
            expect(AllianceSystem.validateAllianceName('Test_Name').valid).toBe(false);
        });
    });

    describe('Tag Validation', () => {
        it('should accept valid tags', () => {
            expect(AllianceSystem.validateTag('ABC').valid).toBe(true);
            expect(AllianceSystem.validateTag('X1').valid).toBe(true);
            expect(AllianceSystem.validateTag('APEX').valid).toBe(true);
        });

        it('should reject tags that are too short', () => {
            const result = AllianceSystem.validateTag('A');
            expect(result.valid).toBe(false);
            expect(result.error).toBe('Tag too short');
        });

        it('should reject tags that are too long', () => {
            const result = AllianceSystem.validateTag('ABCDE');
            expect(result.valid).toBe(false);
            expect(result.error).toBe('Tag too long');
        });

        it('should reject lowercase tags', () => {
            expect(AllianceSystem.validateTag('abc').valid).toBe(false);
            expect(AllianceSystem.validateTag('Abc').valid).toBe(false);
        });
    });

    describe('Join Alliance', () => {
        const baseAlliance: Alliance = {
            id: 'alliance_1',
            name: 'Test Alliance',
            tag: 'TEST',
            leaderId: 'leader_1',
            memberCount: 5,
            maxMembers: 10,
            level: 1,
            xp: 0,
            isRecruiting: true,
            minLevel: 5,
            createdAt: new Date(),
            wars: []
        };

        const basePlayer: Player = {
            id: 'player_1',
            level: 10,
            allianceId: null
        };

        it('should allow eligible players to join', () => {
            const result = AllianceSystem.canJoinAlliance(basePlayer, baseAlliance);
            expect(result.allowed).toBe(true);
        });

        it('should prevent joining if already in alliance', () => {
            const inAlliance = { ...basePlayer, allianceId: 'other_alliance' };
            const result = AllianceSystem.canJoinAlliance(inAlliance, baseAlliance);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Already in an alliance');
        });

        it('should prevent joining if alliance not recruiting', () => {
            const closed = { ...baseAlliance, isRecruiting: false };
            const result = AllianceSystem.canJoinAlliance(basePlayer, closed);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Alliance is not recruiting');
        });

        it('should prevent joining if level too low', () => {
            const lowLevel = { ...basePlayer, level: 3 };
            const result = AllianceSystem.canJoinAlliance(lowLevel, baseAlliance);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Level requirement not met');
        });

        it('should prevent joining if alliance is full', () => {
            const full = { ...baseAlliance, memberCount: 10 };
            const result = AllianceSystem.canJoinAlliance(basePlayer, full);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Alliance is full');
        });
    });

    describe('Alliance Progression', () => {
        it('should calculate max members based on level', () => {
            expect(AllianceSystem.calculateMaxMembers(1)).toBe(10);
            expect(AllianceSystem.calculateMaxMembers(5)).toBe(30);
            expect(AllianceSystem.calculateMaxMembers(10)).toBe(55);
        });

        it('should calculate XP requirements exponentially', () => {
            const xp1 = AllianceSystem.calculateXpForLevel(1);
            const xp2 = AllianceSystem.calculateXpForLevel(2);
            const xp5 = AllianceSystem.calculateXpForLevel(5);

            expect(xp1).toBe(1000);
            expect(xp2).toBe(1500);
            expect(xp5).toBeGreaterThan(xp2);
        });
    });

    describe('Alliance Wars', () => {
        const alliance1: Alliance = {
            id: 'alliance_1',
            name: 'Phoenix Legion',
            tag: 'PHX',
            leaderId: 'leader_1',
            memberCount: 10,
            maxMembers: 20,
            level: 5,
            xp: 5000,
            isRecruiting: true,
            minLevel: 5,
            createdAt: new Date(),
            wars: []
        };

        const alliance2: Alliance = {
            id: 'alliance_2',
            name: 'Shadow Clan',
            tag: 'SHD',
            leaderId: 'leader_2',
            memberCount: 8,
            maxMembers: 20,
            level: 6,
            xp: 7000,
            isRecruiting: true,
            minLevel: 5,
            createdAt: new Date(),
            wars: []
        };

        it('should allow wars between eligible alliances', () => {
            const result = AllianceSystem.canStartWar(alliance1, alliance2);
            expect(result.allowed).toBe(true);
        });

        it('should prevent war with self', () => {
            const result = AllianceSystem.canStartWar(alliance1, alliance1);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Cannot war with yourself');
        });

        it('should prevent duplicate wars', () => {
            const atWar: Alliance = {
                ...alliance1,
                wars: [{
                    id: 'war_1',
                    attackerId: 'alliance_1',
                    defenderId: 'alliance_2',
                    status: 'active',
                    attackerScore: 0,
                    defenderScore: 0,
                    startedAt: new Date(),
                    endsAt: new Date(Date.now() + 86400000)
                }]
            };

            const result = AllianceSystem.canStartWar(atWar, alliance2);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Already at war with this alliance');
        });

        it('should prevent war with large level difference', () => {
            const highLevel = { ...alliance2, level: 15 };
            const result = AllianceSystem.canStartWar(alliance1, highLevel);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Level difference too large');
        });

        it('should require minimum members for war', () => {
            const small = { ...alliance2, memberCount: 2 };
            const result = AllianceSystem.canStartWar(alliance1, small);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Both alliances need at least 3 members');
        });
    });

    describe('War Resolution', () => {
        it('should determine attacker as winner with higher score', () => {
            const war: AllianceWar = {
                id: 'war_1',
                attackerId: 'alliance_1',
                defenderId: 'alliance_2',
                status: 'completed',
                attackerScore: 1500,
                defenderScore: 1000,
                startedAt: new Date(),
                endsAt: new Date()
            };

            expect(AllianceSystem.determineWarWinner(war)).toBe('alliance_1');
        });

        it('should determine defender as winner with higher score', () => {
            const war: AllianceWar = {
                id: 'war_1',
                attackerId: 'alliance_1',
                defenderId: 'alliance_2',
                status: 'completed',
                attackerScore: 800,
                defenderScore: 1200,
                startedAt: new Date(),
                endsAt: new Date()
            };

            expect(AllianceSystem.determineWarWinner(war)).toBe('alliance_2');
        });

        it('should return null for draw', () => {
            const war: AllianceWar = {
                id: 'war_1',
                attackerId: 'alliance_1',
                defenderId: 'alliance_2',
                status: 'completed',
                attackerScore: 1000,
                defenderScore: 1000,
                startedAt: new Date(),
                endsAt: new Date()
            };

            expect(AllianceSystem.determineWarWinner(war)).toBeNull();
        });

        it('should return null for incomplete wars', () => {
            const war: AllianceWar = {
                id: 'war_1',
                attackerId: 'alliance_1',
                defenderId: 'alliance_2',
                status: 'active',
                attackerScore: 1000,
                defenderScore: 500,
                startedAt: new Date(),
                endsAt: new Date()
            };

            expect(AllianceSystem.determineWarWinner(war)).toBeNull();
        });
    });

    describe('Member Management', () => {
        const leader: AllianceMember = {
            id: 'member_1',
            playerId: 'player_1',
            allianceId: 'alliance_1',
            role: 'leader',
            joinedAt: new Date(),
            contributedXp: 5000
        };

        const officer: AllianceMember = {
            id: 'member_2',
            playerId: 'player_2',
            allianceId: 'alliance_1',
            role: 'officer',
            joinedAt: new Date(),
            contributedXp: 2000
        };

        const member: AllianceMember = {
            id: 'member_3',
            playerId: 'player_3',
            allianceId: 'alliance_1',
            role: 'member',
            joinedAt: new Date(),
            contributedXp: 500
        };

        it('should allow leader to promote to officer', () => {
            const result = AllianceSystem.canPromoteMember(leader, member, 'officer');
            expect(result.allowed).toBe(true);
        });

        it('should prevent officers from promoting to officer', () => {
            const result = AllianceSystem.canPromoteMember(officer, member, 'officer');
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Officers can only promote to member');
        });

        it('should prevent promoting to leader', () => {
            const result = AllianceSystem.canPromoteMember(leader, officer, 'leader');
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Use transfer leadership instead');
        });

        it('should prevent members from promoting', () => {
            const result = AllianceSystem.canPromoteMember(member, member, 'officer');
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Insufficient permissions');
        });

        it('should prevent self-promotion', () => {
            const result = AllianceSystem.canPromoteMember(leader, leader, 'officer');
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Cannot promote yourself');
        });
    });
});
