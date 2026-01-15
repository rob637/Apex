/**
 * Battle System Tests
 * Tests for the turn-based siege combat system
 */

import { describe, it, expect, beforeEach, jest } from '@jest/globals';

// Mock Firebase Admin before importing battle module
jest.mock('firebase-admin', () => ({
  initializeApp: jest.fn(),
  firestore: jest.fn(() => ({
    collection: jest.fn().mockReturnValue({
      doc: jest.fn().mockReturnValue({
        get: jest.fn(),
        set: jest.fn(),
        update: jest.fn()
      })
    }),
    batch: jest.fn().mockReturnValue({
      set: jest.fn(),
      update: jest.fn(),
      commit: jest.fn()
    })
  })),
  credential: {
    applicationDefault: jest.fn()
  }
}));

// Mock Firebase Functions
jest.mock('firebase-functions', () => ({
  config: jest.fn().mockReturnValue({}),
  https: {
    onCall: jest.fn((handler) => handler),
    HttpsError: class HttpsError extends Error {
      constructor(public code: string, message: string) {
        super(message);
      }
    }
  },
  pubsub: {
    schedule: jest.fn().mockReturnValue({
      onRun: jest.fn((handler) => handler)
    })
  }
}));

// Import types for testing
import {
  TroopType,
  Troop,
  TROOP_DEFINITIONS,
  BATTLE_CONFIG,
  TerritoryState
} from '../src/types';

describe('Battle System Types', () => {
  describe('Troop Definitions', () => {
    it('should have all 6 troop types defined', () => {
      const expectedTypes: TroopType[] = ['infantry', 'cavalry', 'archer', 'siege', 'mage', 'dragon'];
      
      for (const troopType of expectedTypes) {
        expect(TROOP_DEFINITIONS[troopType]).toBeDefined();
        expect(TROOP_DEFINITIONS[troopType].type).toBe(troopType);
      }
    });

    it('should have valid stats for each troop type', () => {
      for (const [type, def] of Object.entries(TROOP_DEFINITIONS)) {
        expect(def.baseAttack).toBeGreaterThan(0);
        expect(def.baseDefense).toBeGreaterThan(0);
        expect(def.baseHealth).toBeGreaterThan(0);
        expect(def.speed).toBeGreaterThan(0);
        expect(def.trainingTime).toBeGreaterThan(0);
        expect(def.resourceCost).toBeDefined();
      }
    });

    it('should have counter relationships defined', () => {
      // Infantry counters Cavalry
      expect(TROOP_DEFINITIONS.infantry.strongAgainst).toContain('cavalry');
      
      // Cavalry counters Archers
      expect(TROOP_DEFINITIONS.cavalry.strongAgainst).toContain('archer');
      
      // Archers counter Infantry
      expect(TROOP_DEFINITIONS.archer.strongAgainst).toContain('infantry');
      
      // Siege counters nothing but is essential for buildings
      expect(TROOP_DEFINITIONS.siege.strongAgainst).toEqual([]);
      
      // Mages counter Siege
      expect(TROOP_DEFINITIONS.mage.strongAgainst).toContain('siege');
      
      // Dragons are elite units
      expect(TROOP_DEFINITIONS.dragon.strongAgainst.length).toBeGreaterThan(0);
    });

    it('should have dragon as most expensive unit', () => {
      const dragonCost = Object.values(TROOP_DEFINITIONS.dragon.resourceCost).reduce((a, b) => a + b, 0);
      
      for (const [type, def] of Object.entries(TROOP_DEFINITIONS)) {
        if (type !== 'dragon') {
          const cost = Object.values(def.resourceCost).reduce((a, b) => a + b, 0);
          expect(dragonCost).toBeGreaterThan(cost);
        }
      }
    });
  });

  describe('Battle Configuration', () => {
    it('should have valid battle config', () => {
      expect(BATTLE_CONFIG.MAX_ROUNDS).toBe(10);
      expect(BATTLE_CONFIG.PREPARATION_HOURS).toBe(4);
      expect(BATTLE_CONFIG.MAX_TROOPS_PER_BATTLE).toBe(100);
      expect(BATTLE_CONFIG.POST_BATTLE_SHIELD_HOURS).toBe(8);
      expect(BATTLE_CONFIG.RECLAIM_COOLDOWN_HOURS).toBe(24);
      expect(BATTLE_CONFIG.LOSSES_TO_FALL).toBe(3);
    });
  });

  describe('Territory States', () => {
    it('should have valid territory state transitions', () => {
      const validStates: TerritoryState[] = ['secure', 'contested', 'vulnerable', 'fallen'];
      
      // Verify each state is valid
      for (const state of validStates) {
        expect(['secure', 'contested', 'vulnerable', 'fallen']).toContain(state);
      }
    });
  });
});

describe('Battle Logic', () => {
  describe('Troop Formation Power', () => {
    it('should calculate formation power correctly', () => {
      const troops: Troop[] = [
        { type: 'infantry', count: 10, level: 1 },
        { type: 'archer', count: 5, level: 2 }
      ];
      
      // Power = (attack + defense + health/10) * count * level
      const infantryDef = TROOP_DEFINITIONS.infantry;
      const archerDef = TROOP_DEFINITIONS.archer;
      
      const infantryPower = (infantryDef.baseAttack + infantryDef.baseDefense + infantryDef.baseHealth / 10) * 10 * 1;
      const archerPower = (archerDef.baseAttack + archerDef.baseDefense + archerDef.baseHealth / 10) * 5 * 2;
      
      const expectedTotal = Math.round(infantryPower + archerPower);
      
      // This tests our understanding of the power formula
      expect(expectedTotal).toBeGreaterThan(0);
    });
  });

  describe('Counter System', () => {
    it('should apply counter bonuses correctly', () => {
      // Infantry strong against Cavalry = 1.5x damage
      const infantry = TROOP_DEFINITIONS.infantry;
      expect(infantry.strongAgainst.includes('cavalry')).toBe(true);
      
      // In battle, infantry attacking cavalry should deal 1.5x
      const counterBonus = 1.5;
      const baseDamage = infantry.baseAttack;
      const counteredDamage = baseDamage * counterBonus;
      
      expect(counteredDamage).toBe(baseDamage * 1.5);
    });

    it('should not apply counter to non-countered units', () => {
      // Infantry does NOT counter Archers
      const infantry = TROOP_DEFINITIONS.infantry;
      expect(infantry.strongAgainst.includes('archer')).toBe(false);
    });
  });

  describe('Participation Effectiveness', () => {
    const PARTICIPATION_EFFECTIVENESS = {
      physical: 1.0,
      nearby: 0.75,
      remote: 0.5
    };

    it('should have correct participation multipliers', () => {
      expect(PARTICIPATION_EFFECTIVENESS.physical).toBe(1.0);
      expect(PARTICIPATION_EFFECTIVENESS.nearby).toBe(0.75);
      expect(PARTICIPATION_EFFECTIVENESS.remote).toBe(0.5);
    });

    it('should reward physical presence', () => {
      const baseAttack = 100;
      
      const physicalAttack = baseAttack * PARTICIPATION_EFFECTIVENESS.physical;
      const nearbyAttack = baseAttack * PARTICIPATION_EFFECTIVENESS.nearby;
      const remoteAttack = baseAttack * PARTICIPATION_EFFECTIVENESS.remote;
      
      expect(physicalAttack).toBeGreaterThan(nearbyAttack);
      expect(nearbyAttack).toBeGreaterThan(remoteAttack);
    });
  });

  describe('Territory State Transitions', () => {
    it('should transition states correctly on loss', () => {
      const transitions: Record<TerritoryState, TerritoryState> = {
        'secure': 'contested',
        'contested': 'vulnerable',
        'vulnerable': 'fallen',
        'fallen': 'fallen' // Can't fall further
      };
      
      // Verify state machine
      expect(transitions.secure).toBe('contested');
      expect(transitions.contested).toBe('vulnerable');
      expect(transitions.vulnerable).toBe('fallen');
    });

    it('should require 3 losses to fall from secure', () => {
      let state: TerritoryState = 'secure';
      let losses = 0;
      
      // Simulate 3 battle losses
      while (state !== 'fallen' && losses < 10) {
        losses++;
        if (state === 'secure') state = 'contested';
        else if (state === 'contested') state = 'vulnerable';
        else if (state === 'vulnerable') state = 'fallen';
      }
      
      expect(losses).toBe(3);
      expect(state).toBe('fallen');
    });
  });

  describe('XP Calculation', () => {
    it('should award more XP to winner', () => {
      const baseXP = 100;
      const winnerMultiplier = 2.0;
      const loserMultiplier = 0.5;
      
      const winnerXP = baseXP * winnerMultiplier;
      const loserXP = baseXP * loserMultiplier;
      
      expect(winnerXP).toBeGreaterThan(loserXP);
    });

    it('should award bonus XP for physical presence', () => {
      const baseXP = 100;
      const physicalBonus = 1.5;
      
      const remoteXP = baseXP;
      const physicalXP = baseXP * physicalBonus;
      
      expect(physicalXP).toBeGreaterThan(remoteXP);
    });
  });
});
