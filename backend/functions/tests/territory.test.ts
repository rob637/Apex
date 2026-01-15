/**
 * Territory System Tests
 * Tests for territory claiming, attacking, defending, and control
 */

// Territory types
interface Territory {
    id: string;
    name: string;
    ownerId: string | null;
    allianceId: string | null;
    latitude: number;
    longitude: number;
    radius: number;
    level: number;
    health: number;
    maxHealth: number;
    defense: number;
    lastAttackedAt: Date | null;
    shieldExpiresAt: Date | null;
    createdAt: Date;
}

interface Player {
    id: string;
    level: number;
    attackPower: number;
    territoriesOwned: number;
    allianceId: string | null;
}

// Territory logic (simplified for testing)
class TerritorySystem {
    static canClaimTerritory(player: Player, territory: Territory): { allowed: boolean; reason?: string } {
        // Territory must be unclaimed
        if (territory.ownerId !== null) {
            return { allowed: false, reason: 'Territory already claimed' };
        }

        // Player must have sufficient level
        if (player.level < territory.level) {
            return { allowed: false, reason: 'Player level too low' };
        }

        // Check territory limit
        const maxTerritories = 3 + Math.floor(player.level / 5);
        if (player.territoriesOwned >= maxTerritories) {
            return { allowed: false, reason: 'Territory limit reached' };
        }

        return { allowed: true };
    }

    static canAttackTerritory(attacker: Player, territory: Territory): { allowed: boolean; reason?: string } {
        // Can't attack own territory
        if (territory.ownerId === attacker.id) {
            return { allowed: false, reason: 'Cannot attack own territory' };
        }

        // Can't attack alliance territory
        if (attacker.allianceId && territory.allianceId === attacker.allianceId) {
            return { allowed: false, reason: 'Cannot attack alliance territory' };
        }

        // Check if shielded
        if (territory.shieldExpiresAt && territory.shieldExpiresAt > new Date()) {
            return { allowed: false, reason: 'Territory is shielded' };
        }

        // Level restriction (can't attack much higher level)
        if (territory.level > attacker.level + 5) {
            return { allowed: false, reason: 'Territory level too high' };
        }

        return { allowed: true };
    }

    static calculateDamage(attacker: Player, territory: Territory, attackType: string): number {
        let baseDamage = attacker.attackPower;

        switch (attackType) {
            case 'quick_strike':
                baseDamage *= 1.0;
                break;
            case 'heavy_assault':
                baseDamage *= 2.0;
                break;
            case 'siege':
                baseDamage *= 3.0;
                break;
        }

        // Apply defense reduction
        const defenseMultiplier = 1 - (territory.defense / 100);
        const finalDamage = Math.max(1, Math.floor(baseDamage * defenseMultiplier));

        return finalDamage;
    }

    static applyDamage(territory: Territory, damage: number): { captured: boolean; remainingHealth: number } {
        const newHealth = Math.max(0, territory.health - damage);
        return {
            captured: newHealth <= 0,
            remainingHealth: newHealth
        };
    }

    static calculateControlBonus(territories: Territory[]): { attackBonus: number; defenseBonus: number; resourceBonus: number } {
        const count = territories.length;
        return {
            attackBonus: Math.min(50, count * 5),
            defenseBonus: Math.min(50, count * 5),
            resourceBonus: Math.min(100, count * 10)
        };
    }
}

describe('Territory System', () => {
    describe('Territory Claiming', () => {
        const baseTerritory: Territory = {
            id: 'territory_1',
            name: 'Test Territory',
            ownerId: null,
            allianceId: null,
            latitude: 37.7749,
            longitude: -122.4194,
            radius: 100,
            level: 1,
            health: 1000,
            maxHealth: 1000,
            defense: 0,
            lastAttackedAt: null,
            shieldExpiresAt: null,
            createdAt: new Date()
        };

        const basePlayer: Player = {
            id: 'player_1',
            level: 5,
            attackPower: 100,
            territoriesOwned: 0,
            allianceId: null
        };

        it('should allow claiming unclaimed territory', () => {
            const result = TerritorySystem.canClaimTerritory(basePlayer, baseTerritory);
            expect(result.allowed).toBe(true);
        });

        it('should prevent claiming already claimed territory', () => {
            const claimed = { ...baseTerritory, ownerId: 'other_player' };
            const result = TerritorySystem.canClaimTerritory(basePlayer, claimed);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Territory already claimed');
        });

        it('should prevent claiming if player level is too low', () => {
            const highLevel = { ...baseTerritory, level: 10 };
            const lowPlayer = { ...basePlayer, level: 3 };
            const result = TerritorySystem.canClaimTerritory(lowPlayer, highLevel);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Player level too low');
        });

        it('should prevent claiming if territory limit reached', () => {
            const maxedPlayer = { ...basePlayer, territoriesOwned: 10 };
            const result = TerritorySystem.canClaimTerritory(maxedPlayer, baseTerritory);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Territory limit reached');
        });

        it('should increase territory limit with player level', () => {
            // Level 5: max = 3 + floor(5/5) = 4
            const player5 = { ...basePlayer, level: 5, territoriesOwned: 3 };
            expect(TerritorySystem.canClaimTerritory(player5, baseTerritory).allowed).toBe(true);

            // Level 20: max = 3 + floor(20/5) = 7
            const player20 = { ...basePlayer, level: 20, territoriesOwned: 6 };
            expect(TerritorySystem.canClaimTerritory(player20, baseTerritory).allowed).toBe(true);
        });
    });

    describe('Territory Attacking', () => {
        const ownedTerritory: Territory = {
            id: 'territory_1',
            name: 'Test Territory',
            ownerId: 'enemy_player',
            allianceId: 'enemy_alliance',
            latitude: 37.7749,
            longitude: -122.4194,
            radius: 100,
            level: 5,
            health: 1000,
            maxHealth: 1000,
            defense: 20,
            lastAttackedAt: null,
            shieldExpiresAt: null,
            createdAt: new Date()
        };

        const attacker: Player = {
            id: 'attacker_1',
            level: 10,
            attackPower: 100,
            territoriesOwned: 2,
            allianceId: 'ally_alliance'
        };

        it('should allow attacking enemy territory', () => {
            const result = TerritorySystem.canAttackTerritory(attacker, ownedTerritory);
            expect(result.allowed).toBe(true);
        });

        it('should prevent attacking own territory', () => {
            const ownTerritory = { ...ownedTerritory, ownerId: attacker.id };
            const result = TerritorySystem.canAttackTerritory(attacker, ownTerritory);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Cannot attack own territory');
        });

        it('should prevent attacking alliance territory', () => {
            const allyTerritory = { ...ownedTerritory, allianceId: attacker.allianceId };
            const result = TerritorySystem.canAttackTerritory(attacker, allyTerritory);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Cannot attack alliance territory');
        });

        it('should prevent attacking shielded territory', () => {
            const shielded = { ...ownedTerritory, shieldExpiresAt: new Date(Date.now() + 3600000) };
            const result = TerritorySystem.canAttackTerritory(attacker, shielded);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Territory is shielded');
        });

        it('should allow attacking if shield expired', () => {
            const expiredShield = { ...ownedTerritory, shieldExpiresAt: new Date(Date.now() - 3600000) };
            const result = TerritorySystem.canAttackTerritory(attacker, expiredShield);
            expect(result.allowed).toBe(true);
        });

        it('should prevent attacking territory much higher level', () => {
            const highLevel = { ...ownedTerritory, level: 20 };
            const result = TerritorySystem.canAttackTerritory(attacker, highLevel);
            expect(result.allowed).toBe(false);
            expect(result.reason).toBe('Territory level too high');
        });
    });

    describe('Damage Calculation', () => {
        const territory: Territory = {
            id: 'territory_1',
            name: 'Test Territory',
            ownerId: 'enemy',
            allianceId: null,
            latitude: 37.7749,
            longitude: -122.4194,
            radius: 100,
            level: 5,
            health: 1000,
            maxHealth: 1000,
            defense: 25,
            lastAttackedAt: null,
            shieldExpiresAt: null,
            createdAt: new Date()
        };

        const attacker: Player = {
            id: 'attacker_1',
            level: 10,
            attackPower: 100,
            territoriesOwned: 2,
            allianceId: null
        };

        it('should calculate quick_strike damage correctly', () => {
            const damage = TerritorySystem.calculateDamage(attacker, territory, 'quick_strike');
            // 100 * 1.0 * (1 - 0.25) = 75
            expect(damage).toBe(75);
        });

        it('should calculate heavy_assault damage correctly', () => {
            const damage = TerritorySystem.calculateDamage(attacker, territory, 'heavy_assault');
            // 100 * 2.0 * (1 - 0.25) = 150
            expect(damage).toBe(150);
        });

        it('should calculate siege damage correctly', () => {
            const damage = TerritorySystem.calculateDamage(attacker, territory, 'siege');
            // 100 * 3.0 * (1 - 0.25) = 225
            expect(damage).toBe(225);
        });

        it('should apply defense reduction', () => {
            const noDefense = { ...territory, defense: 0 };
            const highDefense = { ...territory, defense: 50 };

            const noDef = TerritorySystem.calculateDamage(attacker, noDefense, 'quick_strike');
            const hiDef = TerritorySystem.calculateDamage(attacker, highDefense, 'quick_strike');

            expect(noDef).toBe(100);
            expect(hiDef).toBe(50);
        });

        it('should deal at least 1 damage', () => {
            const maxDefense = { ...territory, defense: 99 };
            const damage = TerritorySystem.calculateDamage(attacker, maxDefense, 'quick_strike');
            expect(damage).toBeGreaterThanOrEqual(1);
        });
    });

    describe('Territory Capture', () => {
        const territory: Territory = {
            id: 'territory_1',
            name: 'Test Territory',
            ownerId: 'enemy',
            allianceId: null,
            latitude: 37.7749,
            longitude: -122.4194,
            radius: 100,
            level: 5,
            health: 100,
            maxHealth: 1000,
            defense: 0,
            lastAttackedAt: null,
            shieldExpiresAt: null,
            createdAt: new Date()
        };

        it('should capture territory when health reaches 0', () => {
            const result = TerritorySystem.applyDamage(territory, 100);
            expect(result.captured).toBe(true);
            expect(result.remainingHealth).toBe(0);
        });

        it('should not capture if health remains', () => {
            const result = TerritorySystem.applyDamage(territory, 50);
            expect(result.captured).toBe(false);
            expect(result.remainingHealth).toBe(50);
        });

        it('should not go below 0 health', () => {
            const result = TerritorySystem.applyDamage(territory, 200);
            expect(result.captured).toBe(true);
            expect(result.remainingHealth).toBe(0);
        });
    });

    describe('Control Bonuses', () => {
        it('should calculate bonuses based on territory count', () => {
            const territories: Territory[] = Array(5).fill(null).map((_, i) => ({
                id: `territory_${i}`,
                name: `Territory ${i}`,
                ownerId: 'player_1',
                allianceId: null,
                latitude: 37.7749,
                longitude: -122.4194,
                radius: 100,
                level: 1,
                health: 1000,
                maxHealth: 1000,
                defense: 0,
                lastAttackedAt: null,
                shieldExpiresAt: null,
                createdAt: new Date()
            }));

            const bonus = TerritorySystem.calculateControlBonus(territories);
            expect(bonus.attackBonus).toBe(25);
            expect(bonus.defenseBonus).toBe(25);
            expect(bonus.resourceBonus).toBe(50);
        });

        it('should cap bonuses at maximum', () => {
            const territories: Territory[] = Array(20).fill(null).map((_, i) => ({
                id: `territory_${i}`,
                name: `Territory ${i}`,
                ownerId: 'player_1',
                allianceId: null,
                latitude: 37.7749,
                longitude: -122.4194,
                radius: 100,
                level: 1,
                health: 1000,
                maxHealth: 1000,
                defense: 0,
                lastAttackedAt: null,
                shieldExpiresAt: null,
                createdAt: new Date()
            }));

            const bonus = TerritorySystem.calculateControlBonus(territories);
            expect(bonus.attackBonus).toBe(50);  // Capped
            expect(bonus.defenseBonus).toBe(50); // Capped
            expect(bonus.resourceBonus).toBe(100); // Capped
        });

        it('should return 0 bonuses with no territories', () => {
            const bonus = TerritorySystem.calculateControlBonus([]);
            expect(bonus.attackBonus).toBe(0);
            expect(bonus.defenseBonus).toBe(0);
            expect(bonus.resourceBonus).toBe(0);
        });
    });
});
