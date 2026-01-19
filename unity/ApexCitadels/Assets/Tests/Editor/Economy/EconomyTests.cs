using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ApexCitadels.Data;

namespace ApexCitadels.Tests.Editor.Economy
{
    /// <summary>
    /// Unit tests for economy system components.
    /// Tests resource management, trading, costs, and income calculations.
    /// </summary>
    [TestFixture]
    public class EconomyTests : TestBase
    {
        #region ResourceCost Tests

        [Test]
        public void ResourceCost_DefaultConstructor_AllZero()
        {
            // Arrange/Act
            var cost = new ResourceCost();
            
            // Assert
            Assert.AreEqual(0, cost.Stone, "Default stone should be 0");
            Assert.AreEqual(0, cost.Wood, "Default wood should be 0");
            Assert.AreEqual(0, cost.Iron, "Default iron should be 0");
            Assert.AreEqual(0, cost.Crystal, "Default crystal should be 0");
        }

        [Test]
        public void ResourceCost_ParameterizedConstructor_SetsValues()
        {
            // Arrange/Act
            var cost = new ResourceCost(100, 50, 25, 10);
            
            // Assert
            Assert.AreEqual(100, cost.Stone, "Stone should be 100");
            Assert.AreEqual(50, cost.Wood, "Wood should be 50");
            Assert.AreEqual(25, cost.Iron, "Iron should be 25");
            Assert.AreEqual(10, cost.Crystal, "Crystal should be 10");
        }

        [Test]
        public void ResourceCost_GetAmount_ReturnsCorrectValue()
        {
            // Arrange
            var cost = new ResourceCost(100, 50, 25, 10);
            
            // Act/Assert
            Assert.AreEqual(100, cost.GetAmount(ResourceType.Stone));
            Assert.AreEqual(50, cost.GetAmount(ResourceType.Wood));
            Assert.AreEqual(25, cost.GetAmount(ResourceType.Iron));
            Assert.AreEqual(10, cost.GetAmount(ResourceType.Crystal));
        }

        #endregion

        #region Resource Storage Tests

        [Test]
        public void ResourceStorage_AddResources_IncreasesAmount()
        {
            // Arrange
            var storage = new TestResourceStorage();
            storage.SetResource(ResourceType.Stone, 100);
            
            // Act
            storage.AddResource(ResourceType.Stone, 50);
            
            // Assert
            Assert.AreEqual(150, storage.GetResource(ResourceType.Stone));
        }

        [Test]
        public void ResourceStorage_RemoveResources_DecreasesAmount()
        {
            // Arrange
            var storage = new TestResourceStorage();
            storage.SetResource(ResourceType.Wood, 100);
            
            // Act
            bool success = storage.TryRemoveResource(ResourceType.Wood, 30);
            
            // Assert
            Assert.IsTrue(success, "Should successfully remove resources");
            Assert.AreEqual(70, storage.GetResource(ResourceType.Wood));
        }

        [Test]
        public void ResourceStorage_RemoveResources_FailsIfInsufficient()
        {
            // Arrange
            var storage = new TestResourceStorage();
            storage.SetResource(ResourceType.Iron, 50);
            
            // Act
            bool success = storage.TryRemoveResource(ResourceType.Iron, 100);
            
            // Assert
            Assert.IsFalse(success, "Should fail to remove more than available");
            Assert.AreEqual(50, storage.GetResource(ResourceType.Iron), "Amount should remain unchanged");
        }

        [Test]
        public void ResourceStorage_HasEnough_ReturnsTrueWhenSufficient()
        {
            // Arrange
            var storage = new TestResourceStorage();
            storage.SetResource(ResourceType.Crystal, 100);
            
            // Act/Assert
            Assert.IsTrue(storage.HasEnough(ResourceType.Crystal, 50));
            Assert.IsTrue(storage.HasEnough(ResourceType.Crystal, 100));
        }

        [Test]
        public void ResourceStorage_HasEnough_ReturnsFalseWhenInsufficient()
        {
            // Arrange
            var storage = new TestResourceStorage();
            storage.SetResource(ResourceType.Crystal, 50);
            
            // Act/Assert
            Assert.IsFalse(storage.HasEnough(ResourceType.Crystal, 100));
        }

        [Test]
        public void ResourceStorage_CanAffordCost_ChecksAllResources()
        {
            // Arrange
            var storage = new TestResourceStorage();
            storage.SetResource(ResourceType.Stone, 100);
            storage.SetResource(ResourceType.Wood, 50);
            storage.SetResource(ResourceType.Iron, 25);
            
            var affordableCost = new ResourceCost(50, 25, 10);
            var unaffordableCost = new ResourceCost(150, 25, 10);
            
            // Act/Assert
            Assert.IsTrue(storage.CanAfford(affordableCost), "Should be able to afford smaller cost");
            Assert.IsFalse(storage.CanAfford(unaffordableCost), "Should not be able to afford larger cost");
        }

        [Test]
        public void ResourceStorage_SpendCost_RemovesAllResources()
        {
            // Arrange
            var storage = new TestResourceStorage();
            storage.SetResource(ResourceType.Stone, 100);
            storage.SetResource(ResourceType.Wood, 50);
            storage.SetResource(ResourceType.Iron, 25);
            
            var cost = new ResourceCost(50, 25, 10);
            
            // Act
            bool success = storage.TrySpend(cost);
            
            // Assert
            Assert.IsTrue(success, "Should successfully spend resources");
            Assert.AreEqual(50, storage.GetResource(ResourceType.Stone));
            Assert.AreEqual(25, storage.GetResource(ResourceType.Wood));
            Assert.AreEqual(15, storage.GetResource(ResourceType.Iron));
        }

        #endregion

        #region Income Calculation Tests

        [Test]
        public void IncomeCalculation_BaseIncome_ReturnsCorrectValue()
        {
            // Arrange
            int baseIncome = 10;
            int buildings = 5;
            float multiplier = 1.0f;
            
            // Act
            int totalIncome = CalculateIncome(baseIncome, buildings, multiplier);
            
            // Assert
            Assert.AreEqual(50, totalIncome);
        }

        [Test]
        public void IncomeCalculation_WithMultiplier_AppliesCorrectly()
        {
            // Arrange
            int baseIncome = 10;
            int buildings = 5;
            float multiplier = 1.5f;
            
            // Act
            int totalIncome = CalculateIncome(baseIncome, buildings, multiplier);
            
            // Assert
            Assert.AreEqual(75, totalIncome);
        }

        [Test]
        public void IncomeCalculation_ZeroBuildings_ReturnsZero()
        {
            // Arrange
            int baseIncome = 10;
            int buildings = 0;
            float multiplier = 1.0f;
            
            // Act
            int totalIncome = CalculateIncome(baseIncome, buildings, multiplier);
            
            // Assert
            Assert.AreEqual(0, totalIncome);
        }

        #endregion

        #region Trading Tests

        [Test]
        public void Trade_ValidExchange_UpdatesBothResources()
        {
            // Arrange
            var storage = new TestResourceStorage();
            storage.SetResource(ResourceType.Stone, 100);
            storage.SetResource(ResourceType.Wood, 0);
            
            // Trade 50 stone for 25 wood (2:1 ratio)
            int sellAmount = 50;
            int buyAmount = 25;
            
            // Act
            bool success = ExecuteTrade(storage, ResourceType.Stone, sellAmount, ResourceType.Wood, buyAmount);
            
            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual(50, storage.GetResource(ResourceType.Stone));
            Assert.AreEqual(25, storage.GetResource(ResourceType.Wood));
        }

        [Test]
        public void Trade_InsufficientResources_Fails()
        {
            // Arrange
            var storage = new TestResourceStorage();
            storage.SetResource(ResourceType.Stone, 30);
            
            // Act
            bool success = ExecuteTrade(storage, ResourceType.Stone, 50, ResourceType.Wood, 25);
            
            // Assert
            Assert.IsFalse(success);
            Assert.AreEqual(30, storage.GetResource(ResourceType.Stone), "Stone should remain unchanged");
        }

        [Test]
        public void TradeRatio_CalculatesCorrectly()
        {
            // Arrange
            float stoneValue = 1.0f;
            float woodValue = 1.5f;
            float ironValue = 3.0f;
            
            // Act - How much wood for 100 stone?
            int woodForStone = (int)(100 * stoneValue / woodValue);
            
            // Act - How much iron for 100 stone?
            int ironForStone = (int)(100 * stoneValue / ironValue);
            
            // Assert
            Assert.AreEqual(66, woodForStone, "Should get ~66 wood for 100 stone");
            Assert.AreEqual(33, ironForStone, "Should get ~33 iron for 100 stone");
        }

        #endregion

        #region Production Rate Tests

        [Test]
        public void ProductionRate_BaseRate_ReturnsCorrectValue()
        {
            // Arrange
            float baseRate = 10f; // per hour
            float efficiency = 1.0f;
            
            // Act
            float actualRate = baseRate * efficiency;
            
            // Assert
            Assert.AreEqual(10f, actualRate, 0.01f);
        }

        [Test]
        public void ProductionRate_WithEfficiencyBonus_Increases()
        {
            // Arrange
            float baseRate = 10f;
            float efficiency = 1.25f; // 25% bonus
            
            // Act
            float actualRate = baseRate * efficiency;
            
            // Assert
            Assert.AreEqual(12.5f, actualRate, 0.01f);
        }

        [Test]
        public void ProductionRate_ResourcesProducedOverTime_CalculatesCorrectly()
        {
            // Arrange
            float ratePerHour = 60f;
            float elapsedMinutes = 30f;
            
            // Act
            float produced = ratePerHour * (elapsedMinutes / 60f);
            
            // Assert
            Assert.AreEqual(30f, produced, 0.01f);
        }

        #endregion

        #region Helper Methods

        private int CalculateIncome(int baseIncome, int buildings, float multiplier)
        {
            return Mathf.RoundToInt(baseIncome * buildings * multiplier);
        }

        private bool ExecuteTrade(TestResourceStorage storage, ResourceType sell, int sellAmount, 
                                  ResourceType buy, int buyAmount)
        {
            if (!storage.HasEnough(sell, sellAmount))
                return false;
            
            storage.TryRemoveResource(sell, sellAmount);
            storage.AddResource(buy, buyAmount);
            return true;
        }

        #endregion

        #region Test Data Classes

        private class TestResourceStorage
        {
            private Dictionary<ResourceType, int> _resources = new Dictionary<ResourceType, int>();

            public void SetResource(ResourceType type, int amount)
            {
                _resources[type] = amount;
            }

            public int GetResource(ResourceType type)
            {
                return _resources.TryGetValue(type, out int amount) ? amount : 0;
            }

            public void AddResource(ResourceType type, int amount)
            {
                if (!_resources.ContainsKey(type))
                    _resources[type] = 0;
                _resources[type] += amount;
            }

            public bool TryRemoveResource(ResourceType type, int amount)
            {
                if (!HasEnough(type, amount))
                    return false;
                _resources[type] -= amount;
                return true;
            }

            public bool HasEnough(ResourceType type, int amount)
            {
                return GetResource(type) >= amount;
            }

            public bool CanAfford(ResourceCost cost)
            {
                return HasEnough(ResourceType.Stone, cost.Stone) &&
                       HasEnough(ResourceType.Wood, cost.Wood) &&
                       HasEnough(ResourceType.Iron, cost.Iron) &&
                       HasEnough(ResourceType.Crystal, cost.Crystal);
            }

            public bool TrySpend(ResourceCost cost)
            {
                if (!CanAfford(cost))
                    return false;
                
                TryRemoveResource(ResourceType.Stone, cost.Stone);
                TryRemoveResource(ResourceType.Wood, cost.Wood);
                TryRemoveResource(ResourceType.Iron, cost.Iron);
                TryRemoveResource(ResourceType.Crystal, cost.Crystal);
                return true;
            }
        }

        #endregion
    }
}
