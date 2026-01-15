import { ATTACK_DEFINITIONS, DAILY_REWARDS, AttackType } from '../src/types';

describe('Combat System', () => {
  describe('ATTACK_DEFINITIONS', () => {
    it('should have all attack types defined', () => {
      const expectedTypes: AttackType[] = ['quick_strike', 'heavy_assault', 'siege'];
      expectedTypes.forEach(type => {
        expect(ATTACK_DEFINITIONS[type]).toBeDefined();
      });
    });

    it('should have positive base damage for all attacks', () => {
      Object.values(ATTACK_DEFINITIONS).forEach(attack => {
        expect(attack.baseDamage).toBeGreaterThan(0);
      });
    });

    it('should have positive cooldowns for all attacks', () => {
      Object.values(ATTACK_DEFINITIONS).forEach(attack => {
        expect(attack.cooldownSeconds).toBeGreaterThan(0);
      });
    });

    it('quick_strike should be the weakest attack', () => {
      const quickStrike = ATTACK_DEFINITIONS['quick_strike'];
      expect(ATTACK_DEFINITIONS['heavy_assault'].baseDamage).toBeGreaterThan(quickStrike.baseDamage);
      expect(ATTACK_DEFINITIONS['siege'].baseDamage).toBeGreaterThan(quickStrike.baseDamage);
    });

    it('siege should be the strongest attack', () => {
      const siege = ATTACK_DEFINITIONS['siege'];
      expect(ATTACK_DEFINITIONS['quick_strike'].baseDamage).toBeLessThan(siege.baseDamage);
      expect(ATTACK_DEFINITIONS['heavy_assault'].baseDamage).toBeLessThan(siege.baseDamage);
    });
  });
});

describe('Daily Rewards System', () => {
  describe('DAILY_REWARDS', () => {
    it('should have exactly 7 days of rewards', () => {
      expect(DAILY_REWARDS.length).toBe(7);
    });

    it('should have increasing value over the week', () => {
      // Calculate total resource value for each day (simple sum)
      const dayValues = DAILY_REWARDS.map(day => {
        let total = 0;
        const rewards = day.rewards;
        if (rewards.stone) total += rewards.stone;
        if (rewards.wood) total += rewards.wood;
        if (rewards.metal) total += rewards.metal * 3; // Metal is rarer
        if (rewards.crystal) total += rewards.crystal * 10; // Crystals are rare
        if (rewards.gems) total += rewards.gems * 20; // Gems are premium
        return total;
      });

      // Day 7 should definitely be worth the most
      expect(dayValues[6]).toBeGreaterThan(dayValues[0]);
    });

    it('day 7 should include gems', () => {
      expect(DAILY_REWARDS[6].rewards.gems).toBeDefined();
      expect(DAILY_REWARDS[6].rewards.gems).toBeGreaterThan(0);
    });

    it('day 1 should have stone and wood', () => {
      const day1 = DAILY_REWARDS[0];
      expect(day1.rewards.stone).toBeGreaterThan(0);
      expect(day1.rewards.wood).toBeGreaterThan(0);
    });

    it('all days should have xp rewards', () => {
      DAILY_REWARDS.forEach((day) => {
        expect(day.xp).toBeGreaterThan(0);
      });
    });
  });
});
