// ============================================================================
// APEX CITADELS - 3D CITADEL VISUAL SYSTEM
// Loads and displays 3D building models for territories
// ============================================================================
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace ApexCitadels.PC.Visual
{
    /// <summary>
    /// Manages 3D citadel representations on the world map.
    /// Replaces flat colored rectangles with actual 3D building models.
    /// </summary>
    public class CitadelVisualSystem : MonoBehaviour
    {
        public static CitadelVisualSystem Instance { get; private set; }

        [Header("Prefab References")]
        private Dictionary<string, GameObject> buildingPrefabs = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> towerPrefabs = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> wallPrefabs = new Dictionary<string, GameObject>();

        [Header("Active Citadels")]
        private Dictionary<string, CitadelVisual> activeCitadels = new Dictionary<string, CitadelVisual>();

        [Header("Settings")]
        public float citadelScale = 0.1f; // Scale of imported models
        public float heightOffset = 0.5f; // Offset from terrain

        // Building type configurations
        private readonly Dictionary<int, string[]> citadelLevelBuildings = new Dictionary<int, string[]>
        {
            { 1, new[] { "B09_Barracks_Basic" } },
            { 2, new[] { "B09_Barracks_Basic", "T01_Guard_Tower_Basic" } },
            { 3, new[] { "B09_Barracks_Basic", "T01_Guard_Tower_Basic", "B01_Gold_Mine", "W01_Stone_Wall_Straight_2m" } },
            { 4, new[] { "B10_Barracks_Advanced", "T06_Archer_Tower_Basic", "B01_Gold_Mine", "B03_Lumber_Mill" } },
            { 5, new[] { "B10_Barracks_Advanced", "T07_Archer_Tower_Advanced", "T09_Mage_Tower_Basic", "B11_Blacksmith" } },
        };

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            CreateFallbackPrefabs();
            Debug.Log("[CitadelVisual] ✅ Citadel visual system initialized");
        }

        /// <summary>
        /// Creates fallback prefabs for when GLB models aren't loaded
        /// </summary>
        private void CreateFallbackPrefabs()
        {
            // Create stylized building prefabs using primitives
            CreateBuildingPrefab("B09_Barracks_Basic", CreateBarracks);
            CreateBuildingPrefab("B10_Barracks_Advanced", CreateAdvancedBarracks);
            CreateBuildingPrefab("B01_Gold_Mine", CreateMine);
            CreateBuildingPrefab("B03_Lumber_Mill", CreateMill);
            CreateBuildingPrefab("B11_Blacksmith", CreateBlacksmith);
            CreateBuildingPrefab("B27_Tavern", CreateTavern);
            CreateBuildingPrefab("B18_Treasury", CreateTreasury);
            
            CreateTowerPrefab("T01_Guard_Tower_Basic", CreateGuardTower);
            CreateTowerPrefab("T06_Archer_Tower_Basic", CreateArcherTower);
            CreateTowerPrefab("T07_Archer_Tower_Advanced", CreateAdvancedArcherTower);
            CreateTowerPrefab("T09_Mage_Tower_Basic", CreateMageTower);
            
            CreateWallPrefab("W01_Stone_Wall_Straight_2m", CreateStoneWall);
            CreateWallPrefab("W10_Stone_Wall_Gate_Small", CreateGate);

            Debug.Log($"[CitadelVisual] Created {buildingPrefabs.Count} building prefabs, {towerPrefabs.Count} tower prefabs, {wallPrefabs.Count} wall prefabs");
        }

        #region Building Creation Methods

        private void CreateBuildingPrefab(string id, System.Func<GameObject> creator)
        {
            GameObject prefab = creator();
            prefab.name = id;
            prefab.SetActive(false);
            buildingPrefabs[id] = prefab;
        }

        private void CreateTowerPrefab(string id, System.Func<GameObject> creator)
        {
            GameObject prefab = creator();
            prefab.name = id;
            prefab.SetActive(false);
            towerPrefabs[id] = prefab;
        }

        private void CreateWallPrefab(string id, System.Func<GameObject> creator)
        {
            GameObject prefab = creator();
            prefab.name = id;
            prefab.SetActive(false);
            wallPrefabs[id] = prefab;
        }

        private GameObject CreateBarracks()
        {
            GameObject building = new GameObject("Barracks");
            
            // Main building body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(building.transform);
            body.transform.localScale = new Vector3(8f, 4f, 6f);
            body.transform.localPosition = new Vector3(0, 2f, 0);
            ApplyMaterial(body, new Color(0.6f, 0.5f, 0.4f)); // Stone color
            
            // Roof
            GameObject roof = CreateRoof(8.5f, 3f, 6.5f);
            roof.transform.SetParent(building.transform);
            roof.transform.localPosition = new Vector3(0, 5.5f, 0);
            ApplyMaterial(roof, new Color(0.4f, 0.25f, 0.15f)); // Wood color
            
            // Door
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.transform.SetParent(building.transform);
            door.transform.localScale = new Vector3(1.5f, 2.5f, 0.3f);
            door.transform.localPosition = new Vector3(0, 1.25f, 3f);
            ApplyMaterial(door, new Color(0.3f, 0.2f, 0.1f));
            
            // Windows
            CreateWindow(building.transform, new Vector3(-2.5f, 2.5f, 3f));
            CreateWindow(building.transform, new Vector3(2.5f, 2.5f, 3f));
            
            // Flag pole
            CreateFlagPole(building.transform, new Vector3(3f, 7f, 0));
            
            RemoveAllColliders(building);
            return building;
        }

        private GameObject CreateAdvancedBarracks()
        {
            GameObject building = CreateBarracks();
            building.name = "AdvancedBarracks";
            
            // Add second floor
            GameObject secondFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            secondFloor.transform.SetParent(building.transform);
            secondFloor.transform.localScale = new Vector3(6f, 3f, 5f);
            secondFloor.transform.localPosition = new Vector3(0, 6f, 0);
            ApplyMaterial(secondFloor, new Color(0.55f, 0.45f, 0.35f));
            
            // Update roof position
            Transform roof = building.transform.Find("Roof");
            if (roof != null)
            {
                roof.localPosition = new Vector3(0, 8.5f, 0);
            }
            
            RemoveAllColliders(building);
            return building;
        }

        private GameObject CreateMine()
        {
            GameObject building = new GameObject("Mine");
            
            // Mine entrance (cube as cave)
            GameObject entrance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            entrance.transform.SetParent(building.transform);
            entrance.transform.localScale = new Vector3(4f, 3f, 3f);
            entrance.transform.localPosition = new Vector3(0, 1.5f, 0);
            ApplyMaterial(entrance, new Color(0.35f, 0.3f, 0.25f));
            
            // Dark entrance hole
            GameObject hole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hole.transform.SetParent(building.transform);
            hole.transform.localScale = new Vector3(2f, 2.2f, 0.5f);
            hole.transform.localPosition = new Vector3(0, 1.1f, 1.5f);
            ApplyMaterial(hole, new Color(0.1f, 0.1f, 0.1f));
            
            // Support beams
            CreateBeam(building.transform, new Vector3(-1.8f, 2f, 1.5f));
            CreateBeam(building.transform, new Vector3(1.8f, 2f, 1.5f));
            
            // Mine cart track
            GameObject track = GameObject.CreatePrimitive(PrimitiveType.Cube);
            track.transform.SetParent(building.transform);
            track.transform.localScale = new Vector3(1f, 0.1f, 5f);
            track.transform.localPosition = new Vector3(0, 0.05f, 3f);
            ApplyMaterial(track, new Color(0.3f, 0.25f, 0.2f));
            
            // Mine cart
            GameObject cart = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cart.transform.SetParent(building.transform);
            cart.transform.localScale = new Vector3(0.8f, 0.6f, 1.2f);
            cart.transform.localPosition = new Vector3(0, 0.4f, 4f);
            ApplyMaterial(cart, new Color(0.5f, 0.45f, 0.4f));
            
            // Gold in cart
            GameObject gold = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gold.transform.SetParent(cart.transform);
            gold.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            gold.transform.localPosition = new Vector3(0, 0.4f, 0);
            ApplyMaterial(gold, new Color(1f, 0.85f, 0.2f));
            
            RemoveAllColliders(building);
            return building;
        }

        private GameObject CreateMill()
        {
            GameObject building = new GameObject("Mill");
            
            // Main body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(building.transform);
            body.transform.localScale = new Vector3(5f, 4f, 5f);
            body.transform.localPosition = new Vector3(0, 2f, 0);
            ApplyMaterial(body, new Color(0.5f, 0.35f, 0.2f)); // Wood
            
            // Roof
            GameObject roof = CreateRoof(5.5f, 2.5f, 5.5f);
            roof.transform.SetParent(building.transform);
            roof.transform.localPosition = new Vector3(0, 5f, 0);
            ApplyMaterial(roof, new Color(0.35f, 0.25f, 0.15f));
            
            // Saw blade (circle represented by cylinder)
            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            blade.transform.SetParent(building.transform);
            blade.transform.localScale = new Vector3(2f, 0.1f, 2f);
            blade.transform.localPosition = new Vector3(-2.5f, 2f, 0);
            blade.transform.rotation = Quaternion.Euler(0, 0, 90);
            ApplyMaterial(blade, new Color(0.6f, 0.6f, 0.6f));
            
            // Log pile
            for (int i = 0; i < 3; i++)
            {
                GameObject log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                log.transform.SetParent(building.transform);
                log.transform.localScale = new Vector3(0.5f, 2f, 0.5f);
                log.transform.localPosition = new Vector3(3f + i * 0.6f, 0.3f, 2f);
                log.transform.rotation = Quaternion.Euler(90, 0, 0);
                ApplyMaterial(log, new Color(0.45f, 0.3f, 0.15f));
            }
            
            RemoveAllColliders(building);
            return building;
        }

        private GameObject CreateBlacksmith()
        {
            GameObject building = new GameObject("Blacksmith");
            
            // Main body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(building.transform);
            body.transform.localScale = new Vector3(6f, 4f, 5f);
            body.transform.localPosition = new Vector3(0, 2f, 0);
            ApplyMaterial(body, new Color(0.5f, 0.4f, 0.35f));
            
            // Chimney
            GameObject chimney = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chimney.transform.SetParent(building.transform);
            chimney.transform.localScale = new Vector3(1.5f, 4f, 1.5f);
            chimney.transform.localPosition = new Vector3(2f, 6f, -1f);
            ApplyMaterial(chimney, new Color(0.4f, 0.35f, 0.3f));
            
            // Forge glow
            GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glow.transform.SetParent(building.transform);
            glow.transform.localScale = new Vector3(1f, 1f, 1f);
            glow.transform.localPosition = new Vector3(-1f, 1f, 2.5f);
            ApplyEmissiveMaterial(glow, new Color(1f, 0.4f, 0.1f), 2f);
            
            // Anvil
            GameObject anvil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            anvil.transform.SetParent(building.transform);
            anvil.transform.localScale = new Vector3(0.8f, 0.6f, 0.4f);
            anvil.transform.localPosition = new Vector3(-2f, 0.3f, 3f);
            ApplyMaterial(anvil, new Color(0.3f, 0.3f, 0.3f));
            
            // Roof
            GameObject roof = CreateRoof(6.5f, 2f, 5.5f);
            roof.transform.SetParent(building.transform);
            roof.transform.localPosition = new Vector3(0, 5f, 0);
            ApplyMaterial(roof, new Color(0.3f, 0.2f, 0.1f));
            
            RemoveAllColliders(building);
            return building;
        }

        private GameObject CreateTavern()
        {
            GameObject building = new GameObject("Tavern");
            
            // Main body - two floors
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(building.transform);
            body.transform.localScale = new Vector3(8f, 6f, 6f);
            body.transform.localPosition = new Vector3(0, 3f, 0);
            ApplyMaterial(body, new Color(0.55f, 0.4f, 0.25f));
            
            // Sign
            GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.transform.SetParent(building.transform);
            sign.transform.localScale = new Vector3(2f, 1f, 0.2f);
            sign.transform.localPosition = new Vector3(0, 5f, 3.1f);
            ApplyMaterial(sign, new Color(0.4f, 0.3f, 0.2f));
            
            // Roof
            GameObject roof = CreateRoof(8.5f, 3f, 6.5f);
            roof.transform.SetParent(building.transform);
            roof.transform.localPosition = new Vector3(0, 7.5f, 0);
            ApplyMaterial(roof, new Color(0.35f, 0.2f, 0.1f));
            
            // Lit windows
            CreateLitWindow(building.transform, new Vector3(-2.5f, 2f, 3f));
            CreateLitWindow(building.transform, new Vector3(2.5f, 2f, 3f));
            CreateLitWindow(building.transform, new Vector3(-2.5f, 4.5f, 3f));
            CreateLitWindow(building.transform, new Vector3(2.5f, 4.5f, 3f));
            
            RemoveAllColliders(building);
            return building;
        }

        private GameObject CreateTreasury()
        {
            GameObject building = new GameObject("Treasury");
            
            // Reinforced main body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(building.transform);
            body.transform.localScale = new Vector3(6f, 5f, 6f);
            body.transform.localPosition = new Vector3(0, 2.5f, 0);
            ApplyMaterial(body, new Color(0.5f, 0.45f, 0.4f));
            
            // Gold trim
            GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.transform.SetParent(building.transform);
            trim.transform.localScale = new Vector3(6.2f, 0.3f, 6.2f);
            trim.transform.localPosition = new Vector3(0, 4.9f, 0);
            ApplyMaterial(trim, new Color(0.85f, 0.7f, 0.2f));
            
            // Dome roof
            GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.transform.SetParent(building.transform);
            dome.transform.localScale = new Vector3(4f, 2.5f, 4f);
            dome.transform.localPosition = new Vector3(0, 6f, 0);
            ApplyMaterial(dome, new Color(0.8f, 0.65f, 0.15f));
            
            // Heavy door
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.transform.SetParent(building.transform);
            door.transform.localScale = new Vector3(2f, 3f, 0.3f);
            door.transform.localPosition = new Vector3(0, 1.5f, 3f);
            ApplyMaterial(door, new Color(0.35f, 0.3f, 0.25f));
            
            RemoveAllColliders(building);
            return building;
        }

        private GameObject CreateGuardTower()
        {
            GameObject tower = new GameObject("GuardTower");
            
            // Tower base
            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.transform.SetParent(tower.transform);
            baseObj.transform.localScale = new Vector3(3f, 6f, 3f);
            baseObj.transform.localPosition = new Vector3(0, 3f, 0);
            ApplyMaterial(baseObj, new Color(0.5f, 0.45f, 0.4f));
            
            // Battlements
            for (int i = 0; i < 4; i++)
            {
                GameObject battlement = GameObject.CreatePrimitive(PrimitiveType.Cube);
                battlement.transform.SetParent(tower.transform);
                battlement.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
                float angle = i * 90f * Mathf.Deg2Rad;
                battlement.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * 1.2f,
                    6.5f,
                    Mathf.Sin(angle) * 1.2f
                );
                ApplyMaterial(battlement, new Color(0.5f, 0.45f, 0.4f));
            }
            
            // Pointed roof
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            roof.transform.SetParent(tower.transform);
            roof.transform.localScale = new Vector3(2f, 2f, 2f);
            roof.transform.localPosition = new Vector3(0, 8f, 0);
            ApplyMaterial(roof, new Color(0.4f, 0.25f, 0.15f));
            
            RemoveAllColliders(tower);
            return tower;
        }

        private GameObject CreateArcherTower()
        {
            GameObject tower = CreateGuardTower();
            tower.name = "ArcherTower";
            
            // Add archer platform
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platform.transform.SetParent(tower.transform);
            platform.transform.localScale = new Vector3(4f, 0.3f, 4f);
            platform.transform.localPosition = new Vector3(0, 6.2f, 0);
            ApplyMaterial(platform, new Color(0.45f, 0.35f, 0.25f));
            
            RemoveAllColliders(tower);
            return tower;
        }

        private GameObject CreateAdvancedArcherTower()
        {
            GameObject tower = CreateArcherTower();
            tower.name = "AdvancedArcherTower";
            
            // Taller
            tower.transform.localScale = new Vector3(1.2f, 1.3f, 1.2f);
            
            // Add banner
            CreateBanner(tower.transform, new Vector3(0, 10f, 1.5f), new Color(0.7f, 0.1f, 0.1f));
            
            return tower;
        }

        private GameObject CreateMageTower()
        {
            GameObject tower = new GameObject("MageTower");
            
            // Tall spiraling tower
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.transform.SetParent(tower.transform);
            body.transform.localScale = new Vector3(2.5f, 8f, 2.5f);
            body.transform.localPosition = new Vector3(0, 4f, 0);
            ApplyMaterial(body, new Color(0.4f, 0.4f, 0.5f));
            
            // Magical glow at top
            GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glow.transform.SetParent(tower.transform);
            glow.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            glow.transform.localPosition = new Vector3(0, 9f, 0);
            ApplyEmissiveMaterial(glow, new Color(0.3f, 0.5f, 1f), 3f);
            
            // Pointed roof
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            roof.transform.SetParent(tower.transform);
            roof.transform.localScale = new Vector3(1.5f, 3f, 1.5f);
            roof.transform.localPosition = new Vector3(0, 11f, 0);
            ApplyMaterial(roof, new Color(0.2f, 0.2f, 0.4f));
            
            // Crystal top
            GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crystal.transform.SetParent(tower.transform);
            crystal.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
            crystal.transform.localPosition = new Vector3(0, 13f, 0);
            ApplyEmissiveMaterial(crystal, new Color(0.5f, 0.3f, 1f), 2f);
            
            RemoveAllColliders(tower);
            return tower;
        }

        private GameObject CreateStoneWall()
        {
            GameObject wall = new GameObject("StoneWall");
            
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(wall.transform);
            body.transform.localScale = new Vector3(4f, 3f, 1f);
            body.transform.localPosition = new Vector3(0, 1.5f, 0);
            ApplyMaterial(body, new Color(0.5f, 0.48f, 0.45f));
            
            // Battlements
            for (int i = 0; i < 3; i++)
            {
                GameObject battlement = GameObject.CreatePrimitive(PrimitiveType.Cube);
                battlement.transform.SetParent(wall.transform);
                battlement.transform.localScale = new Vector3(0.8f, 0.6f, 0.6f);
                battlement.transform.localPosition = new Vector3(-1.2f + i * 1.2f, 3.3f, 0);
                ApplyMaterial(battlement, new Color(0.5f, 0.48f, 0.45f));
            }
            
            RemoveAllColliders(wall);
            return wall;
        }

        private GameObject CreateGate()
        {
            GameObject gate = new GameObject("Gate");
            
            // Gate frame
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.transform.SetParent(gate.transform);
            frame.transform.localScale = new Vector3(5f, 5f, 1.5f);
            frame.transform.localPosition = new Vector3(0, 2.5f, 0);
            ApplyMaterial(frame, new Color(0.5f, 0.48f, 0.45f));
            
            // Gate opening
            GameObject opening = GameObject.CreatePrimitive(PrimitiveType.Cube);
            opening.transform.SetParent(gate.transform);
            opening.transform.localScale = new Vector3(3f, 4f, 2f);
            opening.transform.localPosition = new Vector3(0, 2f, 0);
            ApplyMaterial(opening, new Color(0.15f, 0.12f, 0.1f)); // Dark
            
            // Iron gate
            GameObject ironGate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ironGate.transform.SetParent(gate.transform);
            ironGate.transform.localScale = new Vector3(2.8f, 0.1f, 0.1f);
            ironGate.transform.localPosition = new Vector3(0, 3.5f, 0.5f);
            ApplyMaterial(ironGate, new Color(0.25f, 0.25f, 0.25f));
            
            RemoveAllColliders(gate);
            return gate;
        }

        #endregion

        #region Helper Methods

        private GameObject CreateRoof(float width, float height, float depth)
        {
            GameObject roof = new GameObject("Roof");
            
            // Simple angled roof using two planes
            GameObject roofL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofL.transform.SetParent(roof.transform);
            roofL.transform.localScale = new Vector3(width / 2f + 0.2f, 0.2f, depth + 0.4f);
            roofL.transform.localPosition = new Vector3(-width / 4f, height / 2f, 0);
            roofL.transform.rotation = Quaternion.Euler(0, 0, 25);
            
            GameObject roofR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofR.transform.SetParent(roof.transform);
            roofR.transform.localScale = new Vector3(width / 2f + 0.2f, 0.2f, depth + 0.4f);
            roofR.transform.localPosition = new Vector3(width / 4f, height / 2f, 0);
            roofR.transform.rotation = Quaternion.Euler(0, 0, -25);
            
            return roof;
        }

        private void CreateWindow(Transform parent, Vector3 position)
        {
            GameObject window = GameObject.CreatePrimitive(PrimitiveType.Cube);
            window.transform.SetParent(parent);
            window.transform.localScale = new Vector3(0.8f, 1f, 0.1f);
            window.transform.localPosition = position;
            ApplyMaterial(window, new Color(0.2f, 0.25f, 0.35f)); // Dark glass
        }

        private void CreateLitWindow(Transform parent, Vector3 position)
        {
            GameObject window = GameObject.CreatePrimitive(PrimitiveType.Cube);
            window.transform.SetParent(parent);
            window.transform.localScale = new Vector3(0.8f, 1f, 0.1f);
            window.transform.localPosition = position;
            ApplyEmissiveMaterial(window, new Color(1f, 0.8f, 0.4f), 1f); // Warm light
        }

        private void CreateBeam(Transform parent, Vector3 position)
        {
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.transform.SetParent(parent);
            beam.transform.localScale = new Vector3(0.3f, 3f, 0.3f);
            beam.transform.localPosition = position;
            ApplyMaterial(beam, new Color(0.4f, 0.3f, 0.2f));
        }

        private void CreateFlagPole(Transform parent, Vector3 position)
        {
            // Pole
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(parent);
            pole.transform.localScale = new Vector3(0.1f, 3f, 0.1f);
            pole.transform.localPosition = position;
            ApplyMaterial(pole, new Color(0.5f, 0.4f, 0.3f));
            
            // Flag
            GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.transform.SetParent(pole.transform);
            flag.transform.localScale = new Vector3(20f, 8f, 0.5f);
            flag.transform.localPosition = new Vector3(1f, 0.4f, 0);
            ApplyMaterial(flag, new Color(0.8f, 0.2f, 0.2f));
        }

        private void CreateBanner(Transform parent, Vector3 position, Color color)
        {
            // Pole
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(parent);
            pole.transform.localScale = new Vector3(0.1f, 2f, 0.1f);
            pole.transform.localPosition = position;
            pole.transform.rotation = Quaternion.Euler(0, 0, 30);
            ApplyMaterial(pole, new Color(0.4f, 0.35f, 0.3f));
            
            // Banner cloth
            GameObject banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            banner.transform.SetParent(pole.transform);
            banner.transform.localScale = new Vector3(10f, 15f, 0.5f);
            banner.transform.localPosition = new Vector3(0.8f, 0, 0);
            ApplyMaterial(banner, color);
        }

        private void ApplyMaterial(GameObject obj, Color color)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = color;
                renderer.material = mat;
            }
        }

        private void ApplyEmissiveMaterial(GameObject obj, Color color, float intensity)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = color;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * intensity);
                renderer.material = mat;
            }
        }

        private void RemoveAllColliders(GameObject obj)
        {
            Collider[] colliders = obj.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                Destroy(col);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Creates a 3D citadel at the given position
        /// </summary>
        public CitadelVisual CreateCitadel(string territoryId, Vector3 position, int level, Color ownerColor)
        {
            // Check terrain height
            if (TerrainVisualSystem.Instance != null)
            {
                position = TerrainVisualSystem.Instance.GetFlatPositionNear(position);
            }

            GameObject citadelRoot = new GameObject($"Citadel_{territoryId}");
            citadelRoot.transform.position = position;

            CitadelVisual visual = citadelRoot.AddComponent<CitadelVisual>();
            visual.Initialize(territoryId, level, ownerColor);

            // Add buildings based on level
            if (citadelLevelBuildings.TryGetValue(Mathf.Clamp(level, 1, 5), out string[] buildingIds))
            {
                float angle = 0f;
                float radius = 5f;
                
                foreach (string buildingId in buildingIds)
                {
                    Vector3 offset = new Vector3(
                        Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                        0,
                        Mathf.Sin(angle * Mathf.Deg2Rad) * radius
                    );
                    
                    AddBuildingToCitadel(citadelRoot.transform, buildingId, offset, ownerColor);
                    angle += 360f / buildingIds.Length;
                }
            }

            // Add central keep
            AddCentralKeep(citadelRoot.transform, level, ownerColor);

            activeCitadels[territoryId] = visual;
            return visual;
        }

        private void AddBuildingToCitadel(Transform parent, string buildingId, Vector3 offset, Color tint)
        {
            GameObject prefab = null;
            
            if (buildingPrefabs.TryGetValue(buildingId, out prefab) ||
                towerPrefabs.TryGetValue(buildingId, out prefab) ||
                wallPrefabs.TryGetValue(buildingId, out prefab))
            {
                GameObject instance = Instantiate(prefab, parent);
                instance.SetActive(true);
                instance.transform.localPosition = offset;
                instance.transform.localScale = Vector3.one * citadelScale * 10f;
                instance.transform.LookAt(parent.position);
            }
        }

        private void AddCentralKeep(Transform parent, int level, Color color)
        {
            GameObject keep = new GameObject("CentralKeep");
            keep.transform.SetParent(parent);
            keep.transform.localPosition = Vector3.zero;

            // Main tower scales with level
            float scale = 1f + level * 0.2f;
            
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tower.transform.SetParent(keep.transform);
            tower.transform.localScale = new Vector3(4f, 8f * scale, 4f);
            tower.transform.localPosition = new Vector3(0, 4f * scale, 0);
            ApplyMaterial(tower, new Color(0.5f, 0.45f, 0.4f));
            
            // Owner color banner
            CreateBanner(keep.transform, new Vector3(0, 9f * scale, 2f), color);
            
            RemoveAllColliders(keep);
        }

        /// <summary>
        /// Updates an existing citadel
        /// </summary>
        public void UpdateCitadel(string territoryId, int level, Color ownerColor)
        {
            if (activeCitadels.TryGetValue(territoryId, out CitadelVisual visual))
            {
                visual.UpdateVisual(level, ownerColor);
            }
        }

        /// <summary>
        /// Removes a citadel
        /// </summary>
        public void RemoveCitadel(string territoryId)
        {
            if (activeCitadels.TryGetValue(territoryId, out CitadelVisual visual))
            {
                Destroy(visual.gameObject);
                activeCitadels.Remove(territoryId);
            }
        }

        #endregion
    }

    /// <summary>
    /// Component attached to each citadel for management
    /// </summary>
    public class CitadelVisual : MonoBehaviour
    {
        public string TerritoryId { get; private set; }
        public int Level { get; private set; }
        public Color OwnerColor { get; private set; }

        public void Initialize(string id, int level, Color color)
        {
            TerritoryId = id;
            Level = level;
            OwnerColor = color;
        }

        public void UpdateVisual(int level, Color color)
        {
            Level = level;
            OwnerColor = color;
            // Could trigger rebuild of buildings here
        }
    }
}
