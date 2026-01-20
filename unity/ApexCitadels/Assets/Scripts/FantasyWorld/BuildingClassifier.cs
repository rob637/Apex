// ============================================================================
// APEX CITADELS - BUILDING CLASSIFIER
// Classifies real-world buildings into fantasy building types
// ============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ApexCitadels.FantasyWorld
{
    /// <summary>
    /// Classifies real-world OSM buildings into fantasy types based on
    /// size, location context, and OSM tags.
    /// </summary>
    public static class BuildingClassifier
    {
        // Area thresholds in square meters
        private const float TINY_MAX = 50f;
        private const float SMALL_MAX = 150f;
        private const float MEDIUM_MAX = 400f;
        private const float LARGE_MAX = 1000f;
        private const float VERY_LARGE_MAX = 3000f;
        
        /// <summary>
        /// Classify building size based on footprint area
        /// </summary>
        public static BuildingSize ClassifySize(float areaSquareMeters)
        {
            if (areaSquareMeters < TINY_MAX) return BuildingSize.Tiny;
            if (areaSquareMeters < SMALL_MAX) return BuildingSize.Small;
            if (areaSquareMeters < MEDIUM_MAX) return BuildingSize.Medium;
            if (areaSquareMeters < LARGE_MAX) return BuildingSize.Large;
            if (areaSquareMeters < VERY_LARGE_MAX) return BuildingSize.VeryLarge;
            return BuildingSize.Huge;
        }
        
        /// <summary>
        /// Classify a building into a fantasy type based on OSM data
        /// </summary>
        public static FantasyBuildingType ClassifyBuilding(OSMBuilding building, NeighborhoodContext context)
        {
            var size = ClassifySize(building.CalculateArea());
            var osmType = building.BuildingType?.ToLower() ?? "";
            
            // First, check explicit OSM building types
            var typeFromOSM = ClassifyFromOSMType(osmType, size);
            if (typeFromOSM.HasValue) return typeFromOSM.Value;
            
            // Use size-based classification with neighborhood context
            return ClassifyBySize(size, context);
        }
        
        /// <summary>
        /// Classify based on OSM building type tag
        /// </summary>
        private static FantasyBuildingType? ClassifyFromOSMType(string osmType, BuildingSize size)
        {
            return osmType switch
            {
                // Residential
                "house" => size >= BuildingSize.Large ? FantasyBuildingType.Manor : FantasyBuildingType.House,
                "residential" => FantasyBuildingType.House,
                "apartments" => FantasyBuildingType.Inn, // Multi-unit = inn
                "detached" => FantasyBuildingType.House,
                "semidetached_house" => FantasyBuildingType.House,
                "terrace" => FantasyBuildingType.House,
                "bungalow" => FantasyBuildingType.Cottage,
                "cabin" => FantasyBuildingType.Cottage,
                "hut" => FantasyBuildingType.PeasantHut,
                "shed" => FantasyBuildingType.PeasantHut,
                "garage" => FantasyBuildingType.Workshop,
                
                // Commercial
                "commercial" => FantasyBuildingType.GeneralStore,
                "retail" => FantasyBuildingType.Market,
                "supermarket" => FantasyBuildingType.Market,
                "kiosk" => FantasyBuildingType.Market,
                "shop" => FantasyBuildingType.GeneralStore,
                "restaurant" => FantasyBuildingType.Tavern,
                "bar" => FantasyBuildingType.Tavern,
                "pub" => FantasyBuildingType.Tavern,
                "cafe" => FantasyBuildingType.Tavern,
                "hotel" => FantasyBuildingType.Inn,
                "motel" => FantasyBuildingType.Inn,
                "hostel" => FantasyBuildingType.Inn,
                
                // Industrial
                "industrial" => size >= BuildingSize.Large ? FantasyBuildingType.Warehouse : FantasyBuildingType.Workshop,
                "warehouse" => FantasyBuildingType.Warehouse,
                "factory" => FantasyBuildingType.Mill,
                "manufacture" => FantasyBuildingType.Workshop,
                "farm" => FantasyBuildingType.Barn,
                "farm_auxiliary" => FantasyBuildingType.Barn,
                "barn" => FantasyBuildingType.Barn,
                "stable" => FantasyBuildingType.Barn,
                "sty" => FantasyBuildingType.PeasantHut,
                "greenhouse" => FantasyBuildingType.Workshop,
                
                // Civic
                "civic" => FantasyBuildingType.TownHall,
                "government" => FantasyBuildingType.TownHall,
                "public" => FantasyBuildingType.TownHall,
                "office" => FantasyBuildingType.TownHall,
                "hospital" => FantasyBuildingType.Chapel,
                "school" => FantasyBuildingType.TownHall,
                "university" => FantasyBuildingType.Cathedral,
                "college" => FantasyBuildingType.Church,
                "kindergarten" => FantasyBuildingType.Cottage,
                "library" => FantasyBuildingType.MageTower,
                
                // Religious
                "church" => size >= BuildingSize.Large ? FantasyBuildingType.Cathedral : FantasyBuildingType.Church,
                "chapel" => FantasyBuildingType.Chapel,
                "cathedral" => FantasyBuildingType.Cathedral,
                "mosque" => FantasyBuildingType.Cathedral,
                "synagogue" => FantasyBuildingType.Church,
                "temple" => FantasyBuildingType.Cathedral,
                "shrine" => FantasyBuildingType.Chapel,
                "monastery" => FantasyBuildingType.Church,
                "religious" => FantasyBuildingType.Church,
                
                // Military/Fortification
                "military" => FantasyBuildingType.Barracks,
                "barracks" => FantasyBuildingType.Barracks,
                "bunker" => FantasyBuildingType.Fortress,
                "castle" => FantasyBuildingType.Castle,
                "fortress" => FantasyBuildingType.Fortress,
                "tower" => FantasyBuildingType.GuardTower,
                "gatehouse" => FantasyBuildingType.GuardTower,
                "city_wall" => FantasyBuildingType.GuardTower,
                "fire_station" => FantasyBuildingType.Barracks,
                "police" => FantasyBuildingType.Barracks,
                
                // Special
                "train_station" => FantasyBuildingType.Market,
                "transportation" => FantasyBuildingType.Warehouse,
                "stadium" => FantasyBuildingType.Fortress,
                "sports_centre" => FantasyBuildingType.Barracks,
                "service" => FantasyBuildingType.Workshop,
                "storage_tank" => FantasyBuildingType.Mill,
                "transformer_tower" => FantasyBuildingType.MageTower,
                "water_tower" => FantasyBuildingType.GuardTower,
                "windmill" => FantasyBuildingType.Mill,
                "ruins" => FantasyBuildingType.Ruins,
                "historic" => FantasyBuildingType.Monument,
                "monument" => FantasyBuildingType.Monument,
                
                _ => null
            };
        }
        
        /// <summary>
        /// Classify purely by size using neighborhood context for flavor
        /// </summary>
        private static FantasyBuildingType ClassifyBySize(BuildingSize size, NeighborhoodContext context)
        {
            // Use weighted random selection based on context
            return size switch
            {
                BuildingSize.Tiny => SelectRandom(new[]
                {
                    (FantasyBuildingType.PeasantHut, 40),
                    (FantasyBuildingType.Workshop, 30),
                    (FantasyBuildingType.Market, 20),
                    (FantasyBuildingType.Chapel, 10)
                }),
                
                BuildingSize.Small => context.IsUrban ? SelectRandom(new[]
                {
                    (FantasyBuildingType.House, 50),
                    (FantasyBuildingType.Cottage, 20),
                    (FantasyBuildingType.GeneralStore, 15),
                    (FantasyBuildingType.Tavern, 15)
                }) : SelectRandom(new[]
                {
                    (FantasyBuildingType.Cottage, 60),
                    (FantasyBuildingType.PeasantHut, 25),
                    (FantasyBuildingType.Barn, 15)
                }),
                
                BuildingSize.Medium => context.IsUrban ? SelectRandom(new[]
                {
                    (FantasyBuildingType.House, 40),
                    (FantasyBuildingType.Inn, 20),
                    (FantasyBuildingType.GeneralStore, 20),
                    (FantasyBuildingType.Blacksmith, 10),
                    (FantasyBuildingType.Church, 10)
                }) : SelectRandom(new[]
                {
                    (FantasyBuildingType.House, 50),
                    (FantasyBuildingType.Manor, 20),
                    (FantasyBuildingType.Barn, 20),
                    (FantasyBuildingType.Mill, 10)
                }),
                
                BuildingSize.Large => context.IsUrban ? SelectRandom(new[]
                {
                    (FantasyBuildingType.Manor, 30),
                    (FantasyBuildingType.Inn, 25),
                    (FantasyBuildingType.TownHall, 20),
                    (FantasyBuildingType.Church, 15),
                    (FantasyBuildingType.Barracks, 10)
                }) : SelectRandom(new[]
                {
                    (FantasyBuildingType.Manor, 40),
                    (FantasyBuildingType.Barn, 30),
                    (FantasyBuildingType.Church, 20),
                    (FantasyBuildingType.Mill, 10)
                }),
                
                BuildingSize.VeryLarge => SelectRandom(new[]
                {
                    (FantasyBuildingType.Warehouse, 30),
                    (FantasyBuildingType.NobleEstate, 25),
                    (FantasyBuildingType.Cathedral, 20),
                    (FantasyBuildingType.Barracks, 15),
                    (FantasyBuildingType.MageTower, 10)
                }),
                
                BuildingSize.Huge => SelectRandom(new[]
                {
                    (FantasyBuildingType.Castle, 30),
                    (FantasyBuildingType.Fortress, 30),
                    (FantasyBuildingType.Cathedral, 20),
                    (FantasyBuildingType.Warehouse, 20)
                }),
                
                _ => FantasyBuildingType.House
            };
        }
        
        /// <summary>
        /// Select a random type based on weights
        /// </summary>
        private static FantasyBuildingType SelectRandom((FantasyBuildingType type, int weight)[] options)
        {
            int totalWeight = 0;
            foreach (var opt in options) totalWeight += opt.weight;
            
            int roll = UnityEngine.Random.Range(0, totalWeight);
            int cumulative = 0;
            
            foreach (var opt in options)
            {
                cumulative += opt.weight;
                if (roll < cumulative) return opt.type;
            }
            
            return options[0].type;
        }
        
        /// <summary>
        /// Estimate building height based on type
        /// </summary>
        public static float EstimateHeight(FantasyBuildingType type, BuildingSize size)
        {
            // Base heights by type
            float baseHeight = type switch
            {
                FantasyBuildingType.PeasantHut => 3f,
                FantasyBuildingType.Cottage => 4f,
                FantasyBuildingType.House => 6f,
                FantasyBuildingType.Manor => 10f,
                FantasyBuildingType.NobleEstate => 12f,
                FantasyBuildingType.Market => 4f,
                FantasyBuildingType.Tavern => 6f,
                FantasyBuildingType.Inn => 8f,
                FantasyBuildingType.Blacksmith => 5f,
                FantasyBuildingType.Bakery => 5f,
                FantasyBuildingType.GeneralStore => 5f,
                FantasyBuildingType.Barracks => 8f,
                FantasyBuildingType.GuardTower => 15f,
                FantasyBuildingType.Fortress => 20f,
                FantasyBuildingType.Castle => 30f,
                FantasyBuildingType.Chapel => 8f,
                FantasyBuildingType.Church => 15f,
                FantasyBuildingType.Cathedral => 25f,
                FantasyBuildingType.TownHall => 12f,
                FantasyBuildingType.Mill => 12f,
                FantasyBuildingType.Warehouse => 8f,
                FantasyBuildingType.Workshop => 5f,
                FantasyBuildingType.Barn => 7f,
                FantasyBuildingType.MageTower => 20f,
                FantasyBuildingType.Ruins => 5f,
                FantasyBuildingType.Monument => 8f,
                _ => 6f
            };
            
            // Scale by size
            float sizeMultiplier = size switch
            {
                BuildingSize.Tiny => 0.7f,
                BuildingSize.Small => 0.85f,
                BuildingSize.Medium => 1f,
                BuildingSize.Large => 1.2f,
                BuildingSize.VeryLarge => 1.4f,
                BuildingSize.Huge => 1.6f,
                _ => 1f
            };
            
            return baseHeight * sizeMultiplier;
        }
    }
    
    /// <summary>
    /// Context about the neighborhood for classification decisions
    /// </summary>
    public class NeighborhoodContext
    {
        public bool IsUrban { get; set; } = true;
        public bool NearWater { get; set; } = false;
        public bool NearForest { get; set; } = false;
        public bool NearMainRoad { get; set; } = false;
        public float BuildingDensity { get; set; } = 0.5f; // 0-1
        public int NearbyBuildingCount { get; set; } = 0;
        
        /// <summary>
        /// Analyze OSM data to determine neighborhood context
        /// </summary>
        public static NeighborhoodContext Analyze(
            Vector2 position,
            List<OSMBuilding> nearbyBuildings,
            List<OSMRoad> nearbyRoads,
            List<OSMArea> nearbyAreas,
            float analysisRadius = 200f)
        {
            var context = new NeighborhoodContext();
            
            // Count nearby buildings for density
            int count = 0;
            foreach (var b in nearbyBuildings)
            {
                var centroid = b.CalculateCentroid();
                if (Vector2.Distance(position, new Vector2(centroid.x, centroid.z)) < analysisRadius)
                    count++;
            }
            context.NearbyBuildingCount = count;
            
            // Density: number of buildings per area
            float areaKm2 = (analysisRadius * 2f / 1000f) * (analysisRadius * 2f / 1000f);
            float buildingsPerKm2 = count / areaKm2;
            
            // Urban: > 100 buildings per km², Rural: < 30
            context.IsUrban = buildingsPerKm2 > 50;
            context.BuildingDensity = Mathf.Clamp01(buildingsPerKm2 / 200f);
            
            // Check for nearby water/forest
            foreach (var area in nearbyAreas)
            {
                if (area.AreaType == "water") context.NearWater = true;
                if (area.AreaType == "forest") context.NearForest = true;
            }
            
            // Check for main roads
            foreach (var road in nearbyRoads)
            {
                if (road.RoadType == "primary" || road.RoadType == "secondary" || road.RoadType == "tertiary")
                {
                    context.NearMainRoad = true;
                    break;
                }
            }
            
            return context;
        }
        
        /// <summary>
        /// Create a default suburban context (for Vienna/US suburbs)
        /// </summary>
        public static NeighborhoodContext DefaultSuburban()
        {
            return new NeighborhoodContext
            {
                IsUrban = false,
                NearWater = false,
                NearForest = false,
                NearMainRoad = true,
                BuildingDensity = 0.3f,
                NearbyBuildingCount = 20
            };
        }
    }
}
