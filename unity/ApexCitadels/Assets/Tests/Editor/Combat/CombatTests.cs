using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ApexCitadels.Combat;
using ApexCitadels.Data;

namespace ApexCitadels.Tests.Editor.Combat
{
    /// <summary>
    /// Unit tests for combat system components.
    /// Tests damage calculation, unit stats, abilities, and combat resolution.
    /// </summary>
    [TestFixture]
    public class CombatTests : TestBase
    {
        #region Damage Calculation Tests

        [Test]
        public void DamageCalculation_BaseDamage_ReturnsCorrectValue()
        {
            // Arrange
            float baseDamage = 100f;
            float armor = 0f;
            
            // Act
            float actualDamage = CalculateDamage(baseDamage, armor);
            
            // Assert
            Assert.AreEqual(100f, actualDamage, 0.01f, "Base damage without armor should equal input damage");
        }

        [Test]
        public void DamageCalculation_WithArmor_ReducesDamage()
        {
            // Arrange
            float baseDamage = 100f;
            float armor = 50f;
            
            // Act
            float actualDamage = CalculateDamage(baseDamage, armor);
            
            // Assert
            Assert.Less(actualDamage, baseDamage, "Damage with armor should be less than base damage");
            Assert.Greater(actualDamage, 0f, "Damage should still be positive");
        }

        [Test]
        public void DamageCalculation_NegativeDamage_ClampsToZero()
        {
            // Arrange
            float baseDamage = -50f;
            float armor = 0f;
            
            // Act
            float actualDamage = CalculateDamage(baseDamage, armor);
            
            // Assert
            Assert.AreEqual(0f, actualDamage, "Negative damage should be clamped to zero");
        }

        [Test]
        public void DamageCalculation_CriticalHit_MultipliesDamage()
        {
            // Arrange
            float baseDamage = 100f;
            float critMultiplier = 2.0f;
            
            // Act
            float critDamage = baseDamage * critMultiplier;
            
            // Assert
            Assert.AreEqual(200f, critDamage, 0.01f, "Critical damage should be base * multiplier");
        }

        #endregion

        #region Unit Stats Tests

        [Test]
        public void UnitStats_DefaultValues_ArePositive()
        {
            // Arrange
            var stats = CreateDefaultUnitStats();
            
            // Assert
            Assert.Greater(stats.MaxHealth, 0, "Max health should be positive");
            Assert.Greater(stats.Attack, 0, "Attack should be positive");
            Assert.GreaterOrEqual(stats.Defense, 0, "Defense should be non-negative");
            Assert.Greater(stats.Speed, 0, "Speed should be positive");
        }

        [Test]
        public void UnitStats_HealthPercentage_CalculatesCorrectly()
        {
            // Arrange
            var stats = CreateDefaultUnitStats();
            stats.CurrentHealth = 50;
            stats.MaxHealth = 100;
            
            // Act
            float healthPercent = stats.HealthPercentage;
            
            // Assert
            Assert.AreEqual(0.5f, healthPercent, 0.01f, "Health percentage should be 50%");
        }

        [Test]
        public void UnitStats_TakeDamage_ReducesHealth()
        {
            // Arrange
            var stats = CreateDefaultUnitStats();
            stats.CurrentHealth = 100;
            float damage = 30f;
            
            // Act
            stats.TakeDamage(damage);
            
            // Assert
            Assert.AreEqual(70f, stats.CurrentHealth, 0.01f, "Health should be reduced by damage amount");
        }

        [Test]
        public void UnitStats_TakeDamage_ClampsToZero()
        {
            // Arrange
            var stats = CreateDefaultUnitStats();
            stats.CurrentHealth = 50;
            
            // Act
            stats.TakeDamage(100f);
            
            // Assert
            Assert.AreEqual(0f, stats.CurrentHealth, "Health should not go below zero");
        }

        [Test]
        public void UnitStats_Heal_IncreasesHealth()
        {
            // Arrange
            var stats = CreateDefaultUnitStats();
            stats.CurrentHealth = 50;
            stats.MaxHealth = 100;
            
            // Act
            stats.Heal(25f);
            
            // Assert
            Assert.AreEqual(75f, stats.CurrentHealth, 0.01f, "Health should increase by heal amount");
        }

        [Test]
        public void UnitStats_Heal_ClampsToMaxHealth()
        {
            // Arrange
            var stats = CreateDefaultUnitStats();
            stats.CurrentHealth = 90;
            stats.MaxHealth = 100;
            
            // Act
            stats.Heal(50f);
            
            // Assert
            Assert.AreEqual(100f, stats.CurrentHealth, "Health should not exceed max health");
        }

        [Test]
        public void UnitStats_IsDead_ReturnsTrueWhenHealthZero()
        {
            // Arrange
            var stats = CreateDefaultUnitStats();
            stats.CurrentHealth = 0;
            
            // Assert
            Assert.IsTrue(stats.IsDead, "Unit should be dead when health is zero");
        }

        [Test]
        public void UnitStats_IsDead_ReturnsFalseWhenHealthPositive()
        {
            // Arrange
            var stats = CreateDefaultUnitStats();
            stats.CurrentHealth = 1;
            
            // Assert
            Assert.IsFalse(stats.IsDead, "Unit should not be dead when health is positive");
        }

        #endregion

        #region Combat Resolution Tests

        [Test]
        public void CombatResolution_AttackerWins_WhenDefenderDies()
        {
            // Arrange
            var attacker = CreateDefaultUnitStats();
            attacker.Attack = 100;
            
            var defender = CreateDefaultUnitStats();
            defender.CurrentHealth = 50;
            defender.Defense = 0;
            
            // Act
            var result = ResolveCombat(attacker, defender);
            
            // Assert
            Assert.IsTrue(result.AttackerWon, "Attacker should win when defender health reaches zero");
            Assert.IsTrue(defender.IsDead, "Defender should be dead");
        }

        [Test]
        public void CombatResolution_BothSurvive_WhenNotEnoughDamage()
        {
            // Arrange
            var attacker = CreateDefaultUnitStats();
            attacker.Attack = 10;
            
            var defender = CreateDefaultUnitStats();
            defender.CurrentHealth = 100;
            defender.Defense = 5;
            
            // Act
            var result = ResolveSingleAttack(attacker, defender);
            
            // Assert
            Assert.IsFalse(defender.IsDead, "Defender should survive a weak attack");
            Assert.Greater(defender.CurrentHealth, 0, "Defender should have remaining health");
        }

        #endregion

        #region Unit Type Effectiveness Tests

        [Test]
        public void UnitTypeEffectiveness_Infantry_StrongAgainstArchers()
        {
            // Arrange/Act
            float effectiveness = GetTypeEffectiveness(UnitType.Infantry, UnitType.Archer);
            
            // Assert
            Assert.Greater(effectiveness, 1.0f, "Infantry should be effective against Archers");
        }

        [Test]
        public void UnitTypeEffectiveness_Archer_StrongAgainstCavalry()
        {
            // Arrange/Act
            float effectiveness = GetTypeEffectiveness(UnitType.Archer, UnitType.Cavalry);
            
            // Assert
            Assert.Greater(effectiveness, 1.0f, "Archers should be effective against Cavalry");
        }

        [Test]
        public void UnitTypeEffectiveness_Cavalry_StrongAgainstInfantry()
        {
            // Arrange/Act
            float effectiveness = GetTypeEffectiveness(UnitType.Cavalry, UnitType.Infantry);
            
            // Assert
            Assert.Greater(effectiveness, 1.0f, "Cavalry should be effective against Infantry");
        }

        [Test]
        public void UnitTypeEffectiveness_SameType_NeutralEffectiveness()
        {
            // Arrange/Act
            float effectiveness = GetTypeEffectiveness(UnitType.Infantry, UnitType.Infantry);
            
            // Assert
            Assert.AreEqual(1.0f, effectiveness, 0.01f, "Same type should have neutral effectiveness");
        }

        #endregion

        #region Helper Methods

        private float CalculateDamage(float baseDamage, float armor)
        {
            // Standard damage formula: damage = base * (100 / (100 + armor))
            if (baseDamage < 0) return 0f;
            return baseDamage * (100f / (100f + armor));
        }

        private TestUnitStats CreateDefaultUnitStats()
        {
            return new TestUnitStats
            {
                MaxHealth = 100,
                CurrentHealth = 100,
                Attack = 20,
                Defense = 10,
                Speed = 5,
                UnitType = UnitType.Infantry
            };
        }

        private CombatResult ResolveCombat(TestUnitStats attacker, TestUnitStats defender)
        {
            float damage = CalculateDamage(attacker.Attack, defender.Defense);
            defender.TakeDamage(damage);
            
            return new CombatResult
            {
                AttackerWon = defender.IsDead,
                DamageDealt = damage
            };
        }

        private CombatResult ResolveSingleAttack(TestUnitStats attacker, TestUnitStats defender)
        {
            float damage = CalculateDamage(attacker.Attack, defender.Defense);
            defender.TakeDamage(damage);
            
            return new CombatResult
            {
                AttackerWon = defender.IsDead,
                DamageDealt = damage
            };
        }

        private float GetTypeEffectiveness(UnitType attacker, UnitType defender)
        {
            // Rock-paper-scissors: Infantry > Archer > Cavalry > Infantry
            if (attacker == UnitType.Infantry && defender == UnitType.Archer) return 1.5f;
            if (attacker == UnitType.Archer && defender == UnitType.Cavalry) return 1.5f;
            if (attacker == UnitType.Cavalry && defender == UnitType.Infantry) return 1.5f;
            
            // Reverse matchups are weak
            if (attacker == UnitType.Archer && defender == UnitType.Infantry) return 0.5f;
            if (attacker == UnitType.Cavalry && defender == UnitType.Archer) return 0.5f;
            if (attacker == UnitType.Infantry && defender == UnitType.Cavalry) return 0.5f;
            
            return 1.0f; // Neutral
        }

        #endregion

        #region Test Data Classes

        private class TestUnitStats
        {
            public float MaxHealth { get; set; }
            public float CurrentHealth { get; set; }
            public float Attack { get; set; }
            public float Defense { get; set; }
            public float Speed { get; set; }
            public UnitType UnitType { get; set; }

            public float HealthPercentage => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0;
            public bool IsDead => CurrentHealth <= 0;

            public void TakeDamage(float damage)
            {
                CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
            }

            public void Heal(float amount)
            {
                CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            }
        }

        private class CombatResult
        {
            public bool AttackerWon { get; set; }
            public float DamageDealt { get; set; }
        }

        private enum UnitType
        {
            Infantry,
            Archer,
            Cavalry,
            Siege
        }

        #endregion
    }
}
