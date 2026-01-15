using System;
using System.Collections.Generic;
using UnityEngine;
using ApexCitadels.Data;

namespace ApexCitadels.Data
{
    /// <summary>
    /// Static troop definitions - matches backend TROOP_DEFINITIONS
    /// </summary>
    public static class TroopConfig
    {
        public static readonly Dictionary<TroopType, TroopDefinition> Definitions = new()
        {
            {
                TroopType.Infantry,
                new TroopDefinition
                {
                    Type = TroopType.Infantry,
                    DisplayName = "Infantry",
                    BaseAttack = 10,
                    BaseDefense = 10,
                    BaseHealth = 100,
                    TrainingTimeSeconds = 60,
                    TrainingCost = new ResourceCost(iron: 10),
                    StrongAgainst = new List<TroopType> { TroopType.Archer },
                    WeakAgainst = new List<TroopType> { TroopType.Cavalry }
                }
            },
            {
                TroopType.Archer,
                new TroopDefinition
                {
                    Type = TroopType.Archer,
                    DisplayName = "Archer",
                    BaseAttack = 15,
                    BaseDefense = 5,
                    BaseHealth = 60,
                    TrainingTimeSeconds = 45,
                    TrainingCost = new ResourceCost(wood: 15),
                    StrongAgainst = new List<TroopType> { TroopType.Cavalry },
                    WeakAgainst = new List<TroopType> { TroopType.Infantry }
                }
            },
            {
                TroopType.Cavalry,
                new TroopDefinition
                {
                    Type = TroopType.Cavalry,
                    DisplayName = "Cavalry",
                    BaseAttack = 20,
                    BaseDefense = 8,
                    BaseHealth = 80,
                    TrainingTimeSeconds = 120,
                    TrainingCost = new ResourceCost(iron: 20, wood: 10),
                    StrongAgainst = new List<TroopType> { TroopType.Archer, TroopType.Siege },
                    WeakAgainst = new List<TroopType> { TroopType.Guardian }
                }
            },
            {
                TroopType.Siege,
                new TroopDefinition
                {
                    Type = TroopType.Siege,
                    DisplayName = "Siege Engine",
                    BaseAttack = 30,
                    BaseDefense = 3,
                    BaseHealth = 150,
                    TrainingTimeSeconds = 300,
                    TrainingCost = new ResourceCost(wood: 50, iron: 30, stone: 20),
                    StrongAgainst = new List<TroopType>(), // Strong vs buildings, not troops
                    WeakAgainst = new List<TroopType> { TroopType.Cavalry }
                }
            },
            {
                TroopType.Mage,
                new TroopDefinition
                {
                    Type = TroopType.Mage,
                    DisplayName = "Mage",
                    BaseAttack = 25,
                    BaseDefense = 4,
                    BaseHealth = 50,
                    TrainingTimeSeconds = 180,
                    TrainingCost = new ResourceCost(crystal: 10, arcaneEssence: 5),
                    StrongAgainst = new List<TroopType>(), // AoE damage
                    WeakAgainst = new List<TroopType> { TroopType.Infantry }
                }
            },
            {
                TroopType.Guardian,
                new TroopDefinition
                {
                    Type = TroopType.Guardian,
                    DisplayName = "Guardian",
                    BaseAttack = 8,
                    BaseDefense = 20,
                    BaseHealth = 200,
                    TrainingTimeSeconds = 240,
                    TrainingCost = new ResourceCost(stone: 30, iron: 20, arcaneEssence: 3),
                    StrongAgainst = new List<TroopType> { TroopType.Cavalry },
                    WeakAgainst = new List<TroopType> { TroopType.Siege }
                }
            }
        };

        /// <summary>
        /// Get stat multiplier based on counter relationship
        /// </summary>
        public static float GetCombatMultiplier(TroopType attacker, TroopType defender)
        {
            var attackerDef = Definitions[attacker];

            if (attackerDef.StrongAgainst.Contains(defender))
                return CombatConfig.CounterMultiplier;

            if (attackerDef.WeakAgainst.Contains(defender))
                return CombatConfig.WeaknessMultiplier;

            return 1.0f;
        }

        /// <summary>
        /// Calculate total training time for multiple troops
        /// </summary>
        public static int GetTotalTrainingTime(TroopType type, int count)
        {
            return Definitions[type].TrainingTimeSeconds * count;
        }

        /// <summary>
        /// Calculate total training cost for multiple troops
        /// </summary>
        public static ResourceCost GetTotalTrainingCost(TroopType type, int count)
        {
            var baseCost = Definitions[type].TrainingCost;
            return new ResourceCost(
                baseCost.Stone * count,
                baseCost.Wood * count,
                baseCost.Iron * count,
                baseCost.Crystal * count,
                baseCost.ArcaneEssence * count,
                baseCost.Gems * count
            );
        }

        /// <summary>
        /// Calculate effective power of a troop squad
        /// </summary>
        public static int CalculatePower(Troop troop)
        {
            var def = Definitions[troop.Type];
            int basePower = def.BaseAttack + def.BaseDefense + (def.BaseHealth / 10);
            float levelMultiplier = 1f + (troop.Level - 1) * 0.1f;
            return Mathf.RoundToInt(basePower * troop.Count * levelMultiplier);
        }

        /// <summary>
        /// Calculate total power of a formation
        /// </summary>
        public static int CalculateFormationPower(List<Troop> troops)
        {
            int totalPower = 0;
            foreach (var troop in troops)
            {
                totalPower += CalculatePower(troop);
            }
            return totalPower;
        }
    }
}
