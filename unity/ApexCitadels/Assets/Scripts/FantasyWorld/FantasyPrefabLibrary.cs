// ============================================================================
// APEX CITADELS - FANTASY PREFAB LIBRARY
// Central registry of all Synty prefabs organized by category
// ============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ApexCitadels.FantasyWorld
{
    /// <summary>
    /// Building size classification based on real-world footprint
    /// </summary>
    public enum BuildingSize
    {
        Tiny,       // < 50 sqm (sheds, outhouses)
        Small,      // 50-150 sqm (cottages, small homes)
        Medium,     // 150-400 sqm (houses, shops)
        Large,      // 400-1000 sqm (mansions, large stores)
        VeryLarge,  // 1000-3000 sqm (warehouses, schools)
        Huge        // > 3000 sqm (malls, factories)
    }
    
    /// <summary>
    /// Building type classification for fantasy conversion
    /// </summary>
    public enum FantasyBuildingType
    {
        // Residential
        PeasantHut,
        Cottage,
        House,
        Manor,
        NobleEstate,
        
        // Commercial
        Market,
        Tavern,
        Inn,
        Blacksmith,
        Bakery,
        GeneralStore,
        
        // Military
        Barracks,
        GuardTower,
        Fortress,
        Castle,
        
        // Religious/Civic
        Chapel,
        Church,
        Cathedral,
        TownHall,
        
        // Industrial
        Mill,
        Warehouse,
        Workshop,
        Barn,
        
        // Special
        MageTower,
        Ruins,
        Monument
    }
    
    /// <summary>
    /// Vegetation types
    /// </summary>
    public enum VegetationType
    {
        TreeOak,
        TreePine,
        TreeWillow,
        TreeFantasy,
        TreeDead,
        BushSmall,
        BushLarge,
        BushFlower,
        FlowerPatch,
        Grass,
        Rock,
        Boulder,
        Log,
        Mushroom
    }
    
    /// <summary>
    /// Road/path types
    /// </summary>
    public enum PathType
    {
        CobblestoneMain,
        CobblestoneSide,
        DirtPath,
        GrassPath,
        WoodBridge
    }
    
    /// <summary>
    /// Prop types for decoration
    /// </summary>
    public enum PropType
    {
        Barrel,
        Crate,
        Cart,
        Well,
        Fountain,
        Signpost,
        Lantern,
        Bench,
        Fence,
        Gate,
        Flag,
        Scarecrow,
        Campfire,
        Tent
    }
    
    /// <summary>
    /// A prefab entry with variations
    /// </summary>
    [Serializable]
    public class PrefabEntry
    {
        public string Name;
        public GameObject[] Variations;
        public Vector3 ScaleMultiplier = Vector3.one;
        public float YOffset = 0f;
        public bool RandomRotation = true;
        
        public GameObject GetRandom()
        {
            if (Variations == null || Variations.Length == 0) return null;
            return Variations[UnityEngine.Random.Range(0, Variations.Length)];
        }
    }
    
    /// <summary>
    /// Central library of all fantasy prefabs.
    /// Drag Synty prefabs here to register them for world generation.
    /// </summary>
    [CreateAssetMenu(fileName = "FantasyPrefabLibrary", menuName = "Apex Citadels/Fantasy Prefab Library")]
    public class FantasyPrefabLibrary : ScriptableObject
    {
        [Header("=== BUILDINGS ===")]
        
        [Header("Residential - Small")]
        public GameObject[] peasantHuts;
        public GameObject[] cottages;
        public GameObject[] smallHouses;
        
        [Header("Residential - Medium")]
        public GameObject[] houses;
        public GameObject[] townHouses;
        
        [Header("Residential - Large")]
        public GameObject[] manors;
        public GameObject[] nobleEstates;
        
        [Header("Commercial")]
        public GameObject[] marketStalls;
        public GameObject[] taverns;
        public GameObject[] inns;
        public GameObject[] blacksmiths;
        public GameObject[] bakeries;
        public GameObject[] generalStores;
        public GameObject[] shops;
        
        [Header("Military")]
        public GameObject[] guardTowers;
        public GameObject[] barracks;
        public GameObject[] fortresses;
        public GameObject[] castleParts;
        public GameObject[] walls;
        public GameObject[] gates;
        
        [Header("Religious/Civic")]
        public GameObject[] chapels;
        public GameObject[] churches;
        public GameObject[] cathedrals;
        public GameObject[] townHalls;
        
        [Header("Industrial/Farm")]
        public GameObject[] mills;
        public GameObject[] warehouses;
        public GameObject[] workshops;
        public GameObject[] barns;
        public GameObject[] farmhouses;
        public GameObject[] silos;
        
        [Header("Special")]
        public GameObject[] mageTowers;
        public GameObject[] ruins;
        public GameObject[] monuments;
        public GameObject[] fountains;
        public GameObject[] wells;
        
        [Header("=== NATURE ===")]
        
        [Header("Trees")]
        public GameObject[] treesOak;
        public GameObject[] treesPine;
        public GameObject[] treesWillow;
        public GameObject[] treesFantasy;
        public GameObject[] treesDead;
        
        [Header("Bushes & Plants")]
        public GameObject[] bushesSmall;
        public GameObject[] bushesLarge;
        public GameObject[] bushesFlower;
        public GameObject[] flowerPatches;
        public GameObject[] grassClumps;
        
        [Header("Rocks")]
        public GameObject[] rocksSmall;
        public GameObject[] rocksMedium;
        public GameObject[] rocksLarge;
        public GameObject[] boulders;
        
        [Header("Ground Details")]
        public GameObject[] logs;
        public GameObject[] mushrooms;
        public GameObject[] stumps;
        
        [Header("=== PATHS & ROADS ===")]
        public GameObject[] cobblestoneSegments;
        public GameObject[] dirtPathSegments;
        public GameObject[] bridges;
        public GameObject[] steps;
        
        [Header("=== PROPS ===")]
        
        [Header("Containers")]
        public GameObject[] barrels;
        public GameObject[] crates;
        public GameObject[] sacks;
        public GameObject[] chests;
        
        [Header("Vehicles")]
        public GameObject[] carts;
        public GameObject[] wagons;
        public GameObject[] boats;
        
        [Header("Furniture")]
        public GameObject[] benches;
        public GameObject[] tables;
        public GameObject[] chairs;
        public GameObject[] signs;
        
        [Header("Lighting")]
        public GameObject[] lanterns;
        public GameObject[] torches;
        public GameObject[] braziers;
        public GameObject[] campfires;
        
        [Header("Barriers")]
        public GameObject[] fenceWood;
        public GameObject[] fenceStone;
        public GameObject[] hedges;
        
        [Header("Decoration")]
        public GameObject[] flags;
        public GameObject[] banners;
        public GameObject[] statues;
        public GameObject[] scarecrows;
        public GameObject[] haystacks;
        
        [Header("=== CHARACTERS (Optional) ===")]
        public GameObject[] peasants;
        public GameObject[] merchants;
        public GameObject[] guards;
        public GameObject[] knights;
        public GameObject[] nobles;
        public GameObject[] children;
        
        [Header("=== ANIMALS (Optional) ===")]
        public GameObject[] chickens;
        public GameObject[] pigs;
        public GameObject[] cows;
        public GameObject[] horses;
        public GameObject[] dogs;
        public GameObject[] cats;
        
        /// <summary>
        /// Get a random building prefab based on size and type
        /// </summary>
        public GameObject GetBuilding(BuildingSize size, FantasyBuildingType type)
        {
            GameObject[] options = type switch
            {
                FantasyBuildingType.PeasantHut => peasantHuts,
                FantasyBuildingType.Cottage => cottages,
                FantasyBuildingType.House => houses ?? townHouses,
                FantasyBuildingType.Manor => manors,
                FantasyBuildingType.NobleEstate => nobleEstates ?? manors,
                FantasyBuildingType.Market => marketStalls,
                FantasyBuildingType.Tavern => taverns,
                FantasyBuildingType.Inn => inns ?? taverns,
                FantasyBuildingType.Blacksmith => blacksmiths,
                FantasyBuildingType.Bakery => bakeries ?? shops,
                FantasyBuildingType.GeneralStore => generalStores ?? shops,
                FantasyBuildingType.Barracks => barracks,
                FantasyBuildingType.GuardTower => guardTowers,
                FantasyBuildingType.Fortress => fortresses,
                FantasyBuildingType.Castle => castleParts,
                FantasyBuildingType.Chapel => chapels,
                FantasyBuildingType.Church => churches,
                FantasyBuildingType.Cathedral => cathedrals ?? churches,
                FantasyBuildingType.TownHall => townHalls,
                FantasyBuildingType.Mill => mills,
                FantasyBuildingType.Warehouse => warehouses,
                FantasyBuildingType.Workshop => workshops,
                FantasyBuildingType.Barn => barns,
                FantasyBuildingType.MageTower => mageTowers,
                FantasyBuildingType.Ruins => ruins,
                FantasyBuildingType.Monument => monuments,
                _ => houses
            };
            
            // Fallback chain
            if (options == null || options.Length == 0)
            {
                options = size switch
                {
                    BuildingSize.Tiny => peasantHuts ?? cottages,
                    BuildingSize.Small => cottages ?? smallHouses,
                    BuildingSize.Medium => houses ?? townHouses,
                    BuildingSize.Large => manors ?? houses,
                    BuildingSize.VeryLarge => warehouses ?? barns,
                    BuildingSize.Huge => fortresses ?? warehouses,
                    _ => houses
                };
            }
            
            if (options == null || options.Length == 0) return null;
            return options[UnityEngine.Random.Range(0, options.Length)];
        }
        
        /// <summary>
        /// Get a random tree
        /// </summary>
        public GameObject GetTree(bool preferFantasy = false)
        {
            List<GameObject[]> pools = new List<GameObject[]>();
            
            if (preferFantasy && treesFantasy != null && treesFantasy.Length > 0)
                pools.Add(treesFantasy);
            if (treesOak != null && treesOak.Length > 0) pools.Add(treesOak);
            if (treesPine != null && treesPine.Length > 0) pools.Add(treesPine);
            if (treesWillow != null && treesWillow.Length > 0) pools.Add(treesWillow);
            
            if (pools.Count == 0) return null;
            
            var pool = pools[UnityEngine.Random.Range(0, pools.Count)];
            return pool[UnityEngine.Random.Range(0, pool.Length)];
        }
        
        /// <summary>
        /// Get a random bush
        /// </summary>
        public GameObject GetBush()
        {
            List<GameObject> all = new List<GameObject>();
            if (bushesSmall != null) all.AddRange(bushesSmall);
            if (bushesLarge != null) all.AddRange(bushesLarge);
            if (bushesFlower != null) all.AddRange(bushesFlower);
            
            if (all.Count == 0) return null;
            return all[UnityEngine.Random.Range(0, all.Count)];
        }
        
        /// <summary>
        /// Get a random rock
        /// </summary>
        public GameObject GetRock(bool large = false)
        {
            GameObject[] pool = large ? (boulders ?? rocksLarge) : (rocksSmall ?? rocksMedium);
            if (pool == null || pool.Length == 0) return null;
            return pool[UnityEngine.Random.Range(0, pool.Length)];
        }
        
        /// <summary>
        /// Get a random prop
        /// </summary>
        public GameObject GetProp(PropType type)
        {
            GameObject[] pool = type switch
            {
                PropType.Barrel => barrels,
                PropType.Crate => crates,
                PropType.Cart => carts,
                PropType.Well => wells ?? fountains,
                PropType.Fountain => fountains,
                PropType.Signpost => signs,
                PropType.Lantern => lanterns,
                PropType.Bench => benches,
                PropType.Fence => fenceWood,
                PropType.Gate => gates,
                PropType.Flag => flags,
                PropType.Scarecrow => scarecrows,
                PropType.Campfire => campfires,
                PropType.Tent => null, // Add if available
                _ => null
            };
            
            if (pool == null || pool.Length == 0) return null;
            return pool[UnityEngine.Random.Range(0, pool.Length)];
        }
        
        /// <summary>
        /// Get a random NPC
        /// </summary>
        public GameObject GetNPC(bool isGuard = false)
        {
            if (isGuard)
            {
                if (guards != null && guards.Length > 0)
                    return guards[UnityEngine.Random.Range(0, guards.Length)];
                if (knights != null && knights.Length > 0)
                    return knights[UnityEngine.Random.Range(0, knights.Length)];
            }
            
            List<GameObject> all = new List<GameObject>();
            if (peasants != null) all.AddRange(peasants);
            if (merchants != null) all.AddRange(merchants);
            
            if (all.Count == 0) return null;
            return all[UnityEngine.Random.Range(0, all.Count)];
        }
    }
}
