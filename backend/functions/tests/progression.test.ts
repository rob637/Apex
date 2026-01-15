import { calculateLevel, getXpForNextLevel, XP_PER_LEVEL } from '../src/types';

describe('Progression System', () => {
  describe('calculateLevel', () => {
    it('should return level 1 for 0 XP', () => {
      expect(calculateLevel(0)).toBe(1);
    });

    it('should return level 1 for XP below first threshold', () => {
      expect(calculateLevel(50)).toBe(1);
    });

    it('should return level 2 for XP at first threshold', () => {
      expect(calculateLevel(100)).toBe(2);
    });

    it('should return level 3 for 250 XP', () => {
      expect(calculateLevel(250)).toBe(3);
    });

    it('should return level 10 for XP at level 10 threshold', () => {
      expect(calculateLevel(XP_PER_LEVEL[9])).toBe(10);
    });

    it('should handle very high XP beyond table', () => {
      // The function extends beyond the table using 10000 XP increments
      const maxTableLevel = XP_PER_LEVEL.length;
      const highXp = XP_PER_LEVEL[XP_PER_LEVEL.length - 1] + 50000;
      expect(calculateLevel(highXp)).toBeGreaterThan(maxTableLevel);
    });
  });

  describe('getXpForNextLevel', () => {
    it('should return XP needed for level 2', () => {
      expect(getXpForNextLevel(1)).toBe(XP_PER_LEVEL[1]);
    });

    it('should return XP for level beyond table', () => {
      const beyondTableLevel = XP_PER_LEVEL.length + 5;
      const result = getXpForNextLevel(beyondTableLevel);
      expect(result).toBeGreaterThan(XP_PER_LEVEL[XP_PER_LEVEL.length - 1]);
    });
  });

  describe('XP_PER_LEVEL array', () => {
    it('should have at least 20 entries', () => {
      expect(XP_PER_LEVEL.length).toBeGreaterThanOrEqual(20);
    });

    it('should start with 0', () => {
      expect(XP_PER_LEVEL[0]).toBe(0);
    });

    it('should be monotonically increasing', () => {
      for (let i = 1; i < XP_PER_LEVEL.length; i++) {
        expect(XP_PER_LEVEL[i]).toBeGreaterThan(XP_PER_LEVEL[i - 1]);
      }
    });
  });
});
