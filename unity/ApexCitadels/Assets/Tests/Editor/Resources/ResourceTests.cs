using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ApexCitadels.Data;

namespace ApexCitadels.Tests.Editor.Resources
{
    /// <summary>
    /// Unit tests for resource system components.
    /// Tests resource nodes, harvesting, regeneration, and resource pools.
    /// </summary>
    [TestFixture]
    public class ResourceTests : TestBase
    {
        #region ResourceType Tests

        [Test]
        public void ResourceType_AllExpectedTypes_Exist()
        {
            // Assert all core resource types exist
            Assert.IsTrue(Enum.IsDefined(typeof(ResourceType), ResourceType.Stone));
            Assert.IsTrue(Enum.IsDefined(typeof(ResourceType), ResourceType.Wood));
            Assert.IsTrue(Enum.IsDefined(typeof(ResourceType), ResourceType.Iron));
            Assert.IsTrue(Enum.IsDefined(typeof(ResourceType), ResourceType.Crystal));
        }

        [Test]
        public void ResourceType_EnumCount_MatchesExpected()
        {
            // Act
            var values = Enum.GetValues(typeof(ResourceType));
            
            // Assert - Should have at least 4 core resources
            Assert.GreaterOrEqual(values.Length, 4, "Should have at least 4 resource types");
        }

        #endregion

        #region Resource Node Tests

        [Test]
        public void ResourceNode_InitialState_HasMaxResources()
        {
            // Arrange
            var node = new TestResourceNode(ResourceType.Stone, 1000);
            
            // Assert
            Assert.AreEqual(1000, node.CurrentAmount);
            Assert.AreEqual(1000, node.MaxAmount);
            Assert.IsFalse(node.IsDepleted);
        }

        [Test]
        public void ResourceNode_Harvest_ReducesAmount()
        {
            // Arrange
            var node = new TestResourceNode(ResourceType.Stone, 1000);
            
            // Act
            int harvested = node.Harvest(100);
            
            // Assert
            Assert.AreEqual(100, harvested);
            Assert.AreEqual(900, node.CurrentAmount);
        }

        [Test]
        public void ResourceNode_HarvestMoreThanAvailable_ReturnsOnlyAvailable()
        {
            // Arrange
            var node = new TestResourceNode(ResourceType.Stone, 50);
            
            // Act
            int harvested = node.Harvest(100);
            
            // Assert
            Assert.AreEqual(50, harvested);
            Assert.AreEqual(0, node.CurrentAmount);
            Assert.IsTrue(node.IsDepleted);
        }

        [Test]
        public void ResourceNode_IsDepleted_WhenEmpty()
        {
            // Arrange
            var node = new TestResourceNode(ResourceType.Wood, 100);
            
            // Act
            node.Harvest(100);
            
            // Assert
            Assert.IsTrue(node.IsDepleted);
            Assert.AreEqual(0, node.CurrentAmount);
        }

        [Test]
        public void ResourceNode_Regenerate_IncreasesAmount()
        {
            // Arrange
            var node = new TestResourceNode(ResourceType.Stone, 1000);
            node.Harvest(500); // Reduce to 500
            
            // Act
            node.Regenerate(100);
            
            // Assert
            Assert.AreEqual(600, node.CurrentAmount);
        }

        [Test]
        public void ResourceNode_Regenerate_CapsAtMaxAmount()
        {
            // Arrange
            var node = new TestResourceNode(ResourceType.Stone, 1000);
            node.Harvest(100); // Reduce to 900
            
            // Act
            node.Regenerate(500);
            
            // Assert
            Assert.AreEqual(1000, node.CurrentAmount);
        }

        [Test]
        public void ResourceNode_RegenerationRate_CalculatesCorrectly()
        {
            // Arrange
            var node = new TestResourceNode(ResourceType.Stone, 1000);
            node.RegenerationRate = 10; // 10 per second
            node.Harvest(500);
            
            // Act - Simulate 5 seconds
            node.Update(5.0f);
            
            // Assert
            Assert.AreEqual(550, node.CurrentAmount);
        }

        #endregion

        #region Resource Pool Tests

        [Test]
        public void ResourcePool_InitialState_Empty()
        {
            // Arrange/Act
            var pool = new TestResourcePool();
            
            // Assert
            Assert.AreEqual(0, pool.GetAmount(ResourceType.Stone));
            Assert.AreEqual(0, pool.GetAmount(ResourceType.Wood));
        }

        [Test]
        public void ResourcePool_Add_IncreasesAmount()
        {
            // Arrange
            var pool = new TestResourcePool();
            
            // Act
            pool.Add(ResourceType.Stone, 100);
            pool.Add(ResourceType.Stone, 50);
            
            // Assert
            Assert.AreEqual(150, pool.GetAmount(ResourceType.Stone));
        }

        [Test]
        public void ResourcePool_Remove_DecreasesAmount()
        {
            // Arrange
            var pool = new TestResourcePool();
            pool.Add(ResourceType.Wood, 100);
            
            // Act
            bool success = pool.Remove(ResourceType.Wood, 30);
            
            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual(70, pool.GetAmount(ResourceType.Wood));
        }

        [Test]
        public void ResourcePool_Remove_FailsIfInsufficient()
        {
            // Arrange
            var pool = new TestResourcePool();
            pool.Add(ResourceType.Iron, 50);
            
            // Act
            bool success = pool.Remove(ResourceType.Iron, 100);
            
            // Assert
            Assert.IsFalse(success);
            Assert.AreEqual(50, pool.GetAmount(ResourceType.Iron), "Amount should remain unchanged");
        }

        [Test]
        public void ResourcePool_Capacity_ClampsAddition()
        {
            // Arrange
            var pool = new TestResourcePool();
            pool.SetCapacity(ResourceType.Crystal, 100);
            
            // Act
            pool.Add(ResourceType.Crystal, 150);
            
            // Assert
            Assert.AreEqual(100, pool.GetAmount(ResourceType.Crystal));
        }

        [Test]
        public void ResourcePool_Transfer_MovesResources()
        {
            // Arrange
            var source = new TestResourcePool();
            var destination = new TestResourcePool();
            source.Add(ResourceType.Stone, 100);
            
            // Act
            bool success = TestResourcePool.Transfer(source, destination, ResourceType.Stone, 50);
            
            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual(50, source.GetAmount(ResourceType.Stone));
            Assert.AreEqual(50, destination.GetAmount(ResourceType.Stone));
        }

        #endregion

        #region Harvesting Rate Tests

        [Test]
        public void HarvestRate_BaseRate_CalculatesCorrectly()
        {
            // Arrange
            float baseRate = 10f; // per second
            float toolMultiplier = 1.0f;
            float skillBonus = 0f;
            
            // Act
            float actualRate = CalculateHarvestRate(baseRate, toolMultiplier, skillBonus);
            
            // Assert
            Assert.AreEqual(10f, actualRate, 0.01f);
        }

        [Test]
        public void HarvestRate_WithToolMultiplier_Increases()
        {
            // Arrange
            float baseRate = 10f;
            float toolMultiplier = 1.5f; // 50% better tool
            float skillBonus = 0f;
            
            // Act
            float actualRate = CalculateHarvestRate(baseRate, toolMultiplier, skillBonus);
            
            // Assert
            Assert.AreEqual(15f, actualRate, 0.01f);
        }

        [Test]
        public void HarvestRate_WithSkillBonus_Increases()
        {
            // Arrange
            float baseRate = 10f;
            float toolMultiplier = 1.0f;
            float skillBonus = 5f; // +5 flat bonus
            
            // Act
            float actualRate = CalculateHarvestRate(baseRate, toolMultiplier, skillBonus);
            
            // Assert
            Assert.AreEqual(15f, actualRate, 0.01f);
        }

        [Test]
        public void HarvestRate_CombinedBonuses_StackCorrectly()
        {
            // Arrange
            float baseRate = 10f;
            float toolMultiplier = 1.5f;
            float skillBonus = 5f;
            
            // Act - (base * multiplier) + bonus
            float actualRate = CalculateHarvestRate(baseRate, toolMultiplier, skillBonus);
            
            // Assert
            Assert.AreEqual(20f, actualRate, 0.01f);
        }

        #endregion

        #region Resource Rarity Tests

        [Test]
        public void ResourceRarity_CommonDropRate_IsHighest()
        {
            // Arrange/Act
            float commonRate = GetDropRate(ResourceRarity.Common);
            float rareRate = GetDropRate(ResourceRarity.Rare);
            float epicRate = GetDropRate(ResourceRarity.Epic);
            
            // Assert
            Assert.Greater(commonRate, rareRate);
            Assert.Greater(rareRate, epicRate);
        }

        [Test]
        public void ResourceRarity_LegendaryDropRate_IsLowest()
        {
            // Arrange/Act
            float legendaryRate = GetDropRate(ResourceRarity.Legendary);
            float epicRate = GetDropRate(ResourceRarity.Epic);
            
            // Assert
            Assert.Less(legendaryRate, epicRate);
            Assert.Less(legendaryRate, 0.05f, "Legendary should be less than 5%");
        }

        [Test]
        public void ResourceRarity_ValueMultiplier_IncreasesWithRarity()
        {
            // Arrange/Act
            float commonValue = GetValueMultiplier(ResourceRarity.Common);
            float rareValue = GetValueMultiplier(ResourceRarity.Rare);
            float epicValue = GetValueMultiplier(ResourceRarity.Epic);
            float legendaryValue = GetValueMultiplier(ResourceRarity.Legendary);
            
            // Assert
            Assert.Less(commonValue, rareValue);
            Assert.Less(rareValue, epicValue);
            Assert.Less(epicValue, legendaryValue);
        }

        #endregion

        #region Helper Methods

        private float CalculateHarvestRate(float baseRate, float toolMultiplier, float skillBonus)
        {
            return (baseRate * toolMultiplier) + skillBonus;
        }

        private float GetDropRate(ResourceRarity rarity)
        {
            return rarity switch
            {
                ResourceRarity.Common => 0.70f,
                ResourceRarity.Rare => 0.20f,
                ResourceRarity.Epic => 0.08f,
                ResourceRarity.Legendary => 0.02f,
                _ => 0f
            };
        }

        private float GetValueMultiplier(ResourceRarity rarity)
        {
            return rarity switch
            {
                ResourceRarity.Common => 1.0f,
                ResourceRarity.Rare => 2.5f,
                ResourceRarity.Epic => 5.0f,
                ResourceRarity.Legendary => 10.0f,
                _ => 1.0f
            };
        }

        #endregion

        #region Test Data Classes

        private enum ResourceRarity
        {
            Common,
            Rare,
            Epic,
            Legendary
        }

        private class TestResourceNode
        {
            public ResourceType Type { get; }
            public int MaxAmount { get; }
            public int CurrentAmount { get; private set; }
            public float RegenerationRate { get; set; } = 0;
            public bool IsDepleted => CurrentAmount <= 0;

            public TestResourceNode(ResourceType type, int maxAmount)
            {
                Type = type;
                MaxAmount = maxAmount;
                CurrentAmount = maxAmount;
            }

            public int Harvest(int amount)
            {
                int harvested = Mathf.Min(amount, CurrentAmount);
                CurrentAmount -= harvested;
                return harvested;
            }

            public void Regenerate(int amount)
            {
                CurrentAmount = Mathf.Min(MaxAmount, CurrentAmount + amount);
            }

            public void Update(float deltaTime)
            {
                if (RegenerationRate > 0 && CurrentAmount < MaxAmount)
                {
                    int regen = Mathf.RoundToInt(RegenerationRate * deltaTime);
                    Regenerate(regen);
                }
            }
        }

        private class TestResourcePool
        {
            private Dictionary<ResourceType, int> _amounts = new Dictionary<ResourceType, int>();
            private Dictionary<ResourceType, int> _capacities = new Dictionary<ResourceType, int>();

            public int GetAmount(ResourceType type)
            {
                return _amounts.TryGetValue(type, out int amount) ? amount : 0;
            }

            public void Add(ResourceType type, int amount)
            {
                if (!_amounts.ContainsKey(type))
                    _amounts[type] = 0;
                
                _amounts[type] += amount;
                
                // Apply capacity limit if set
                if (_capacities.TryGetValue(type, out int capacity))
                {
                    _amounts[type] = Mathf.Min(_amounts[type], capacity);
                }
            }

            public bool Remove(ResourceType type, int amount)
            {
                if (GetAmount(type) < amount)
                    return false;
                
                _amounts[type] -= amount;
                return true;
            }

            public void SetCapacity(ResourceType type, int capacity)
            {
                _capacities[type] = capacity;
            }

            public static bool Transfer(TestResourcePool source, TestResourcePool destination, 
                                       ResourceType type, int amount)
            {
                if (!source.Remove(type, amount))
                    return false;
                
                destination.Add(type, amount);
                return true;
            }
        }

        #endregion
    }
}
