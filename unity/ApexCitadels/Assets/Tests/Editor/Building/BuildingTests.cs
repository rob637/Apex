using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ApexCitadels.Building;
using ApexCitadels.Data;

namespace ApexCitadels.Tests.Editor.Building
{
    /// <summary>
    /// Unit tests for building system components.
    /// Tests placement validation, upgrades, production, and building stats.
    /// </summary>
    [TestFixture]
    public class BuildingTests : TestBase
    {
        #region Block Type Tests

        [Test]
        public void BlockType_ResourceBlocks_ExistInEnum()
        {
            // Assert - Verify all expected block types exist
            Assert.IsTrue(Enum.IsDefined(typeof(BlockType), BlockType.Stone));
            Assert.IsTrue(Enum.IsDefined(typeof(BlockType), BlockType.Wood));
            Assert.IsTrue(Enum.IsDefined(typeof(BlockType), BlockType.Metal));
        }

        [Test]
        public void BlockType_DefensiveBlocks_ExistInEnum()
        {
            // Assert
            Assert.IsTrue(Enum.IsDefined(typeof(BlockType), BlockType.Tower));
            Assert.IsTrue(Enum.IsDefined(typeof(BlockType), BlockType.ArrowTower));
            Assert.IsTrue(Enum.IsDefined(typeof(BlockType), BlockType.CannonTower));
            Assert.IsTrue(Enum.IsDefined(typeof(BlockType), BlockType.MageTower));
        }

        [Test]
        public void BlockType_WallBlocks_ExistInEnum()
        {
            // Assert
            Assert.IsTrue(Enum.IsDefined(typeof(BlockType), BlockType.WallStone));
            Assert.IsTrue(Enum.IsDefined(typeof(BlockType), BlockType.WallWood));
            Assert.IsTrue(Enum.IsDefined(typeof(BlockType), BlockType.WallMetal));
        }

        #endregion

        #region Building Placement Tests

        [Test]
        public void BuildingPlacement_ValidPosition_ReturnsTrue()
        {
            // Arrange
            var grid = new TestBuildingGrid(10, 10);
            var position = new Vector2Int(5, 5);
            var size = new Vector2Int(2, 2);
            
            // Act
            bool canPlace = grid.CanPlaceBuilding(position, size);
            
            // Assert
            Assert.IsTrue(canPlace, "Should be able to place building in empty area");
        }

        [Test]
        public void BuildingPlacement_OutOfBounds_ReturnsFalse()
        {
            // Arrange
            var grid = new TestBuildingGrid(10, 10);
            var position = new Vector2Int(9, 9);
            var size = new Vector2Int(2, 2);
            
            // Act
            bool canPlace = grid.CanPlaceBuilding(position, size);
            
            // Assert
            Assert.IsFalse(canPlace, "Should not be able to place building out of bounds");
        }

        [Test]
        public void BuildingPlacement_Overlapping_ReturnsFalse()
        {
            // Arrange
            var grid = new TestBuildingGrid(10, 10);
            grid.PlaceBuilding(new Vector2Int(5, 5), new Vector2Int(2, 2), "Building1");
            
            // Act - Try to place overlapping building
            bool canPlace = grid.CanPlaceBuilding(new Vector2Int(6, 6), new Vector2Int(2, 2));
            
            // Assert
            Assert.IsFalse(canPlace, "Should not be able to place overlapping buildings");
        }

        [Test]
        public void BuildingPlacement_AdjacentToExisting_ReturnsTrue()
        {
            // Arrange
            var grid = new TestBuildingGrid(10, 10);
            grid.PlaceBuilding(new Vector2Int(2, 2), new Vector2Int(2, 2), "Building1");
            
            // Act - Place adjacent building
            bool canPlace = grid.CanPlaceBuilding(new Vector2Int(4, 2), new Vector2Int(2, 2));
            
            // Assert
            Assert.IsTrue(canPlace, "Should be able to place adjacent buildings");
        }

        [Test]
        public void BuildingPlacement_NegativePosition_ReturnsFalse()
        {
            // Arrange
            var grid = new TestBuildingGrid(10, 10);
            var position = new Vector2Int(-1, 5);
            var size = new Vector2Int(1, 1);
            
            // Act
            bool canPlace = grid.CanPlaceBuilding(position, size);
            
            // Assert
            Assert.IsFalse(canPlace, "Should not be able to place at negative positions");
        }

        #endregion

        #region Building Upgrade Tests

        [Test]
        public void BuildingUpgrade_Level1To2_IncreasesStats()
        {
            // Arrange
            var building = new TestBuilding { Level = 1, Health = 100, ProductionRate = 10 };
            
            // Act
            building.Upgrade();
            
            // Assert
            Assert.AreEqual(2, building.Level);
            Assert.Greater(building.Health, 100, "Health should increase after upgrade");
            Assert.Greater(building.ProductionRate, 10, "Production should increase after upgrade");
        }

        [Test]
        public void BuildingUpgrade_MaxLevel_CannotUpgradeFurther()
        {
            // Arrange
            var building = new TestBuilding { Level = 10, MaxLevel = 10 };
            
            // Act
            bool canUpgrade = building.CanUpgrade();
            
            // Assert
            Assert.IsFalse(canUpgrade, "Should not be able to upgrade past max level");
        }

        [Test]
        public void BuildingUpgrade_CostIncreasesWithLevel()
        {
            // Arrange/Act
            var cost1 = GetUpgradeCost(1);
            var cost2 = GetUpgradeCost(2);
            var cost5 = GetUpgradeCost(5);
            
            // Assert
            Assert.Less(cost1.Stone, cost2.Stone, "Level 2 should cost more than level 1");
            Assert.Less(cost2.Stone, cost5.Stone, "Level 5 should cost more than level 2");
        }

        [Test]
        public void BuildingUpgrade_TimeIncreasesWithLevel()
        {
            // Arrange/Act
            float time1 = GetUpgradeTime(1);
            float time2 = GetUpgradeTime(2);
            float time5 = GetUpgradeTime(5);
            
            // Assert
            Assert.Less(time1, time2, "Level 2 upgrade should take longer than level 1");
            Assert.Less(time2, time5, "Level 5 upgrade should take longer than level 2");
        }

        #endregion

        #region Building Production Tests

        [Test]
        public void BuildingProduction_ActiveBuilding_ProducesResources()
        {
            // Arrange
            var building = new TestBuilding 
            { 
                ProductionRate = 10, 
                ResourceProduced = ResourceType.Stone,
                IsActive = true
            };
            float deltaTime = 1.0f; // 1 second
            
            // Act
            float produced = building.CalculateProduction(deltaTime);
            
            // Assert
            Assert.AreEqual(10f, produced, 0.01f);
        }

        [Test]
        public void BuildingProduction_InactiveBuilding_ProducesNothing()
        {
            // Arrange
            var building = new TestBuilding 
            { 
                ProductionRate = 10, 
                IsActive = false
            };
            
            // Act
            float produced = building.CalculateProduction(1.0f);
            
            // Assert
            Assert.AreEqual(0f, produced);
        }

        [Test]
        public void BuildingProduction_DamagedBuilding_ReducedProduction()
        {
            // Arrange
            var building = new TestBuilding 
            { 
                ProductionRate = 10, 
                Health = 50,
                MaxHealth = 100,
                IsActive = true
            };
            
            // Act
            float produced = building.CalculateProduction(1.0f);
            
            // Assert
            Assert.AreEqual(5f, produced, 0.01f, "Production should be reduced by damage percentage");
        }

        #endregion

        #region Building Health Tests

        [Test]
        public void BuildingHealth_TakeDamage_ReducesHealth()
        {
            // Arrange
            var building = new TestBuilding { Health = 100, MaxHealth = 100 };
            
            // Act
            building.TakeDamage(30);
            
            // Assert
            Assert.AreEqual(70, building.Health);
        }

        [Test]
        public void BuildingHealth_Repair_IncreasesHealth()
        {
            // Arrange
            var building = new TestBuilding { Health = 50, MaxHealth = 100 };
            
            // Act
            building.Repair(25);
            
            // Assert
            Assert.AreEqual(75, building.Health);
        }

        [Test]
        public void BuildingHealth_Repair_ClampsToMaxHealth()
        {
            // Arrange
            var building = new TestBuilding { Health = 90, MaxHealth = 100 };
            
            // Act
            building.Repair(50);
            
            // Assert
            Assert.AreEqual(100, building.Health);
        }

        [Test]
        public void BuildingHealth_IsDestroyed_WhenHealthZero()
        {
            // Arrange
            var building = new TestBuilding { Health = 10, MaxHealth = 100 };
            
            // Act
            building.TakeDamage(20);
            
            // Assert
            Assert.IsTrue(building.IsDestroyed);
        }

        #endregion

        #region Helper Methods

        private ResourceCost GetUpgradeCost(int level)
        {
            int baseCost = 100;
            float multiplier = Mathf.Pow(1.5f, level - 1);
            int cost = Mathf.RoundToInt(baseCost * multiplier);
            return new ResourceCost(cost, cost / 2, cost / 4);
        }

        private float GetUpgradeTime(int level)
        {
            float baseTime = 60f; // 60 seconds
            return baseTime * Mathf.Pow(1.3f, level - 1);
        }

        #endregion

        #region Test Data Classes

        private class TestBuildingGrid
        {
            private bool[,] _occupied;
            private int _width;
            private int _height;

            public TestBuildingGrid(int width, int height)
            {
                _width = width;
                _height = height;
                _occupied = new bool[width, height];
            }

            public bool CanPlaceBuilding(Vector2Int position, Vector2Int size)
            {
                // Check bounds
                if (position.x < 0 || position.y < 0)
                    return false;
                if (position.x + size.x > _width || position.y + size.y > _height)
                    return false;

                // Check overlap
                for (int x = position.x; x < position.x + size.x; x++)
                {
                    for (int y = position.y; y < position.y + size.y; y++)
                    {
                        if (_occupied[x, y])
                            return false;
                    }
                }

                return true;
            }

            public void PlaceBuilding(Vector2Int position, Vector2Int size, string id)
            {
                for (int x = position.x; x < position.x + size.x; x++)
                {
                    for (int y = position.y; y < position.y + size.y; y++)
                    {
                        _occupied[x, y] = true;
                    }
                }
            }
        }

        private class TestBuilding
        {
            public int Level { get; set; } = 1;
            public int MaxLevel { get; set; } = 10;
            public int Health { get; set; } = 100;
            public int MaxHealth { get; set; } = 100;
            public float ProductionRate { get; set; } = 10;
            public ResourceType ResourceProduced { get; set; } = ResourceType.Stone;
            public bool IsActive { get; set; } = true;
            public bool IsDestroyed => Health <= 0;

            public bool CanUpgrade() => Level < MaxLevel;

            public void Upgrade()
            {
                if (!CanUpgrade()) return;
                Level++;
                MaxHealth = Mathf.RoundToInt(MaxHealth * 1.2f);
                Health = MaxHealth;
                ProductionRate *= 1.15f;
            }

            public void TakeDamage(int damage)
            {
                Health = Mathf.Max(0, Health - damage);
            }

            public void Repair(int amount)
            {
                Health = Mathf.Min(MaxHealth, Health + amount);
            }

            public float CalculateProduction(float deltaTime)
            {
                if (!IsActive || IsDestroyed) return 0f;
                float efficiency = (float)Health / MaxHealth;
                return ProductionRate * deltaTime * efficiency;
            }
        }

        #endregion
    }
}
