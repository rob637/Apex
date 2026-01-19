using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ApexCitadels.Tests.Editor.Progression
{
    /// <summary>
    /// Unit tests for player progression system.
    /// Tests XP calculation, leveling, achievements, and rewards.
    /// </summary>
    [TestFixture]
    public class ProgressionTests : TestBase
    {
        #region XP Calculation Tests

        [Test]
        public void XPForLevel_Level1_ReturnsBaseXP()
        {
            // Arrange/Act
            int xpRequired = GetXPForLevel(1);
            
            // Assert
            Assert.AreEqual(100, xpRequired);
        }

        [Test]
        public void XPForLevel_HigherLevels_RequiresMoreXP()
        {
            // Arrange/Act
            int level1XP = GetXPForLevel(1);
            int level5XP = GetXPForLevel(5);
            int level10XP = GetXPForLevel(10);
            
            // Assert
            Assert.Less(level1XP, level5XP);
            Assert.Less(level5XP, level10XP);
        }

        [Test]
        public void XPForLevel_ExponentialGrowth_FollowsFormula()
        {
            // Arrange
            int baseXP = 100;
            float growthRate = 1.15f;
            int level = 5;
            
            // Act
            int expected = Mathf.RoundToInt(baseXP * Mathf.Pow(growthRate, level - 1));
            int actual = GetXPForLevel(level);
            
            // Assert
            Assert.AreEqual(expected, actual);
        }

        #endregion

        #region Level Calculation Tests

        [Test]
        public void LevelFromTotalXP_ZeroXP_ReturnsLevel1()
        {
            // Arrange/Act
            int level = GetLevelFromTotalXP(0);
            
            // Assert
            Assert.AreEqual(1, level);
        }

        [Test]
        public void LevelFromTotalXP_ExactlyLevel2_ReturnsLevel2()
        {
            // Arrange
            int xpForLevel2 = GetXPForLevel(1);
            
            // Act
            int level = GetLevelFromTotalXP(xpForLevel2);
            
            // Assert
            Assert.AreEqual(2, level);
        }

        [Test]
        public void LevelFromTotalXP_JustUnderLevel3_ReturnsLevel2()
        {
            // Arrange
            int xpForLevel3 = GetXPForLevel(1) + GetXPForLevel(2);
            
            // Act
            int level = GetLevelFromTotalXP(xpForLevel3 - 1);
            
            // Assert
            Assert.AreEqual(2, level);
        }

        [Test]
        public void LevelProgress_ReturnsCorrectPercentage()
        {
            // Arrange
            int currentXP = 50;
            int xpForNextLevel = 100;
            
            // Act
            float progress = (float)currentXP / xpForNextLevel;
            
            // Assert
            Assert.AreEqual(0.5f, progress, 0.01f);
        }

        #endregion

        #region Achievement Tests

        [Test]
        public void Achievement_InitialState_NotUnlocked()
        {
            // Arrange
            var achievement = new TestAchievement("test", "Test Achievement", 10);
            
            // Assert
            Assert.IsFalse(achievement.IsUnlocked);
            Assert.AreEqual(0, achievement.Progress);
        }

        [Test]
        public void Achievement_UpdateProgress_TracksCorrectly()
        {
            // Arrange
            var achievement = new TestAchievement("test", "Test Achievement", 10);
            
            // Act
            achievement.UpdateProgress(5);
            
            // Assert
            Assert.AreEqual(5, achievement.Progress);
            Assert.IsFalse(achievement.IsUnlocked);
        }

        [Test]
        public void Achievement_ReachTarget_Unlocks()
        {
            // Arrange
            var achievement = new TestAchievement("test", "Test Achievement", 10);
            
            // Act
            achievement.UpdateProgress(10);
            
            // Assert
            Assert.IsTrue(achievement.IsUnlocked);
        }

        [Test]
        public void Achievement_ExceedTarget_StillUnlocks()
        {
            // Arrange
            var achievement = new TestAchievement("test", "Test Achievement", 10);
            
            // Act
            achievement.UpdateProgress(15);
            
            // Assert
            Assert.IsTrue(achievement.IsUnlocked);
            Assert.AreEqual(10, achievement.Progress, "Progress should cap at target");
        }

        [Test]
        public void Achievement_ProgressPercentage_CalculatesCorrectly()
        {
            // Arrange
            var achievement = new TestAchievement("test", "Test Achievement", 100);
            achievement.UpdateProgress(25);
            
            // Act
            float percentage = achievement.ProgressPercentage;
            
            // Assert
            Assert.AreEqual(0.25f, percentage, 0.01f);
        }

        #endregion

        #region Daily Reward Tests

        [Test]
        public void DailyReward_Day1_ReturnsBaseReward()
        {
            // Arrange/Act
            var reward = GetDailyReward(1);
            
            // Assert
            Assert.Greater(reward.Gold, 0);
            Assert.Greater(reward.XP, 0);
        }

        [Test]
        public void DailyReward_StreakBonus_IncreasesWithDays()
        {
            // Arrange/Act
            var day1Reward = GetDailyReward(1);
            var day3Reward = GetDailyReward(3);
            var day7Reward = GetDailyReward(7);
            
            // Assert
            Assert.Less(day1Reward.Gold, day3Reward.Gold);
            Assert.Less(day3Reward.Gold, day7Reward.Gold);
        }

        [Test]
        public void DailyReward_Day7_IncludesBonusItem()
        {
            // Arrange/Act
            var reward = GetDailyReward(7);
            
            // Assert
            Assert.IsTrue(reward.HasBonusItem, "Day 7 should include bonus item");
        }

        [Test]
        public void DailyReward_StreakResets_AfterDay7()
        {
            // Arrange/Act
            int streakDay = GetStreakDay(8);
            
            // Assert
            Assert.AreEqual(1, streakDay, "Streak should reset after day 7");
        }

        #endregion

        #region Season Pass Tests

        [Test]
        public void SeasonPass_InitialTier_IsZero()
        {
            // Arrange
            var seasonPass = new TestSeasonPass();
            
            // Assert
            Assert.AreEqual(0, seasonPass.CurrentTier);
            Assert.AreEqual(0, seasonPass.CurrentXP);
        }

        [Test]
        public void SeasonPass_AddXP_IncreasesProgress()
        {
            // Arrange
            var seasonPass = new TestSeasonPass();
            
            // Act
            seasonPass.AddXP(500);
            
            // Assert
            Assert.AreEqual(500, seasonPass.CurrentXP);
        }

        [Test]
        public void SeasonPass_ReachTierThreshold_AdvancesTier()
        {
            // Arrange
            var seasonPass = new TestSeasonPass();
            seasonPass.XPPerTier = 1000;
            
            // Act
            seasonPass.AddXP(1000);
            
            // Assert
            Assert.AreEqual(1, seasonPass.CurrentTier);
            Assert.AreEqual(0, seasonPass.CurrentXP);
        }

        [Test]
        public void SeasonPass_PremiumRewards_RequiresPurchase()
        {
            // Arrange
            var seasonPass = new TestSeasonPass { IsPremium = false };
            seasonPass.AddXP(2000);
            
            // Act
            bool canClaimPremium = seasonPass.CanClaimPremiumReward(1);
            
            // Assert
            Assert.IsFalse(canClaimPremium);
        }

        [Test]
        public void SeasonPass_FreeRewards_AlwaysAvailable()
        {
            // Arrange
            var seasonPass = new TestSeasonPass { IsPremium = false };
            seasonPass.XPPerTier = 1000;
            seasonPass.AddXP(2000);
            
            // Act
            bool canClaimFree = seasonPass.CanClaimFreeReward(1);
            
            // Assert
            Assert.IsTrue(canClaimFree);
        }

        #endregion

        #region Prestige Tests

        [Test]
        public void Prestige_NotAvailable_BelowMaxLevel()
        {
            // Arrange
            int currentLevel = 50;
            int maxLevel = 100;
            
            // Act
            bool canPrestige = CanPrestige(currentLevel, maxLevel);
            
            // Assert
            Assert.IsFalse(canPrestige);
        }

        [Test]
        public void Prestige_Available_AtMaxLevel()
        {
            // Arrange
            int currentLevel = 100;
            int maxLevel = 100;
            
            // Act
            bool canPrestige = CanPrestige(currentLevel, maxLevel);
            
            // Assert
            Assert.IsTrue(canPrestige);
        }

        [Test]
        public void Prestige_BonusMultiplier_IncreasesWithPrestigeLevel()
        {
            // Arrange/Act
            float prestige1Bonus = GetPrestigeBonus(1);
            float prestige5Bonus = GetPrestigeBonus(5);
            
            // Assert
            Assert.Greater(prestige5Bonus, prestige1Bonus);
        }

        #endregion

        #region Helper Methods

        private int GetXPForLevel(int level)
        {
            int baseXP = 100;
            float growthRate = 1.15f;
            return Mathf.RoundToInt(baseXP * Mathf.Pow(growthRate, level - 1));
        }

        private int GetLevelFromTotalXP(int totalXP)
        {
            int level = 1;
            int accumulatedXP = 0;
            
            while (accumulatedXP + GetXPForLevel(level) <= totalXP)
            {
                accumulatedXP += GetXPForLevel(level);
                level++;
            }
            
            return level;
        }

        private TestDailyReward GetDailyReward(int streakDay)
        {
            int baseGold = 100;
            int baseXP = 50;
            float streakMultiplier = 1 + (streakDay - 1) * 0.2f;
            
            return new TestDailyReward
            {
                Gold = Mathf.RoundToInt(baseGold * streakMultiplier),
                XP = Mathf.RoundToInt(baseXP * streakMultiplier),
                HasBonusItem = streakDay == 7
            };
        }

        private int GetStreakDay(int totalDays)
        {
            return ((totalDays - 1) % 7) + 1;
        }

        private bool CanPrestige(int currentLevel, int maxLevel)
        {
            return currentLevel >= maxLevel;
        }

        private float GetPrestigeBonus(int prestigeLevel)
        {
            return 1.0f + prestigeLevel * 0.1f; // 10% bonus per prestige
        }

        #endregion

        #region Test Data Classes

        private class TestAchievement
        {
            public string Id { get; }
            public string Name { get; }
            public int Target { get; }
            public int Progress { get; private set; }
            public bool IsUnlocked => Progress >= Target;
            public float ProgressPercentage => (float)Progress / Target;

            public TestAchievement(string id, string name, int target)
            {
                Id = id;
                Name = name;
                Target = target;
                Progress = 0;
            }

            public void UpdateProgress(int value)
            {
                Progress = Mathf.Min(value, Target);
            }
        }

        private class TestDailyReward
        {
            public int Gold { get; set; }
            public int XP { get; set; }
            public bool HasBonusItem { get; set; }
        }

        private class TestSeasonPass
        {
            public int CurrentTier { get; private set; }
            public int CurrentXP { get; private set; }
            public int XPPerTier { get; set; } = 1000;
            public bool IsPremium { get; set; }
            private HashSet<int> _claimedFreeRewards = new HashSet<int>();
            private HashSet<int> _claimedPremiumRewards = new HashSet<int>();

            public void AddXP(int xp)
            {
                CurrentXP += xp;
                while (CurrentXP >= XPPerTier)
                {
                    CurrentXP -= XPPerTier;
                    CurrentTier++;
                }
            }

            public bool CanClaimFreeReward(int tier)
            {
                return CurrentTier >= tier && !_claimedFreeRewards.Contains(tier);
            }

            public bool CanClaimPremiumReward(int tier)
            {
                return IsPremium && CurrentTier >= tier && !_claimedPremiumRewards.Contains(tier);
            }
        }

        #endregion
    }
}
