// ============================================================================
// APEX CITADELS - ENHANCED TERRITORY VISUALS
// Creates impressive 3D citadel representations on the world map
// Now uses real 3D models from the asset database!
// ============================================================================
using UnityEngine;
using System.Collections.Generic;
using ApexCitadels.PC.Buildings;

// Import asset types
using AssetBuildingCategory = ApexCitadels.Core.Assets.BuildingCategory;
using AssetTowerType = ApexCitadels.Core.Assets.TowerType;
using AssetWallType = ApexCitadels.Core.Assets.WallType;
using AssetWallMaterial = ApexCitadels.Core.Assets.WallMaterial;

namespace ApexCitadels.PC.Environment
{
    /// <summary>
    /// Creates visually impressive territory representations.
    /// Includes citadel buildings, walls, flags, and effects.
    /// Now integrates real 3D models from Meshy!
    /// </summary>
    public class EnhancedTerritoryVisual : MonoBehaviour
    {
        [Header("Territory Info")]
        public string TerritoryId;
        public string OwnerName;
        public TerritoryOwnership Ownership = TerritoryOwnership.Neutral;
        public int Level = 1;

        [Header("Visual Components")]
        [SerializeField] private GameObject citadelBase;
        [SerializeField] private GameObject mainTower;
        [SerializeField] private GameObject[] wallSegments;
        [SerializeField] private GameObject flagPole;
        [SerializeField] private GameObject selectionRing;
        [SerializeField] private GameObject auraEffect;
        [SerializeField] private Light territoryLight;
        
        [Header("Model Settings")]
        [SerializeField] private bool useRealModels = true;
        [SerializeField] private float modelScale = 2f;

        [Header("Colors by Ownership")]
        private static readonly Color OwnedColor = new Color(0.2f, 0.8f, 0.3f);       // Green
        private static readonly Color AllianceColor = new Color(0.3f, 0.5f, 0.9f);    // Blue
        private static readonly Color EnemyColor = new Color(0.9f, 0.2f, 0.2f);       // Red
        private static readonly Color NeutralColor = new Color(0.6f, 0.6f, 0.6f);     // Gray
        private static readonly Color ContestedColor = new Color(0.9f, 0.6f, 0.1f);   // Orange

        // State
        private bool _isSelected = false;
        private bool _isHovered = false;
        private float _pulseTime = 0f;
        private Material _baseMaterial;
        private Material _glowMaterial;

        /// <summary>
        /// Build the territory visual from scratch
        /// </summary>
        public void BuildVisual()
        {
            // Clean existing
            foreach (Transform child in transform)
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            Color ownerColor = GetOwnershipColor();

            // Create base platform
            CreateBasePlatform(ownerColor);

            // Create main citadel tower
            CreateMainTower(ownerColor);

            // Create walls based on level
            if (Level >= 2)
            {
                CreateWalls(ownerColor);
            }

            // Create flag
            CreateFlag(ownerColor);

            // Create selection ring (hidden by default)
            CreateSelectionRing(ownerColor);

            // Create ambient light
            CreateTerritoryLight(ownerColor);

            // Create territory glow/aura
            if (Ownership == TerritoryOwnership.Owned || Ownership == TerritoryOwnership.Contested)
            {
                CreateAuraEffect(ownerColor);
            }
        }

        private void CreateBasePlatform(Color color)
        {
            // Try to use real foundation model
            if (useRealModels && BuildingModelProvider.Instance != null)
            {
                citadelBase = BuildingModelProvider.Instance.GetFoundationModel(Level, transform);
                if (citadelBase != null)
                {
                    citadelBase.name = "CitadelBase";
                    float baseScale = (15f + Level * 3f) * 0.2f * modelScale;
                    citadelBase.transform.localPosition = Vector3.zero;
                    citadelBase.transform.localScale = Vector3.one * baseScale;
                    ApplyOwnershipTint(citadelBase, color);
                    return;
                }
            }
            
            // Fallback to primitive
            citadelBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            citadelBase.name = "CitadelBase";
            citadelBase.transform.parent = transform;
            citadelBase.transform.localPosition = Vector3.zero;
            
            // Scale based on level
            float baseSize = 15f + Level * 3f;
            citadelBase.transform.localScale = new Vector3(baseSize, 2f, baseSize);

            // Material
            _baseMaterial = CreateMaterial(color, 0.3f);
            citadelBase.GetComponent<Renderer>().material = _baseMaterial;

            // Add collider for clicking
            citadelBase.GetComponent<Collider>().isTrigger = false;
        }

        private void CreateMainTower(Color color)
        {
            mainTower = new GameObject("MainTower");
            mainTower.transform.parent = transform;
            mainTower.transform.localPosition = new Vector3(0, 2f, 0);

            // Try to use real tower models
            if (useRealModels && BuildingModelProvider.Instance != null)
            {
                // Choose tower type based on level
                AssetTowerType towerType = Level switch
                {
                    >= 5 => AssetTowerType.Mage,
                    >= 3 => AssetTowerType.Archer, 
                    _ => AssetTowerType.Guard
                };
                
                GameObject realTower = BuildingModelProvider.Instance.GetTowerModel(towerType, mainTower.transform);
                if (realTower != null)
                {
                    realTower.name = "TowerBody";
                    float towerScale = (1f + Level * 0.3f) * modelScale;
                    realTower.transform.localPosition = Vector3.zero;
                    realTower.transform.localScale = Vector3.one * towerScale;
                    ApplyOwnershipTint(realTower, color);
                    
                    // Add smaller towers for higher level citadels
                    if (Level >= 3)
                    {
                        CreateSideTowerFromModel(mainTower.transform, new Vector3(6, -2, 0), color, 0.6f);
                        CreateSideTowerFromModel(mainTower.transform, new Vector3(-6, -2, 0), color, 0.6f);
                    }
                    if (Level >= 5)
                    {
                        CreateSideTowerFromModel(mainTower.transform, new Vector3(0, -2, 6), color, 0.5f);
                        CreateSideTowerFromModel(mainTower.transform, new Vector3(0, -2, -6), color, 0.5f);
                    }
                    return;
                }
            }

            // Fallback to primitives
            GameObject towerBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            towerBody.name = "TowerBody";
            towerBody.transform.parent = mainTower.transform;
            towerBody.transform.localPosition = Vector3.zero;
            
            float towerHeight = 10f + Level * 5f;
            float towerWidth = 4f + Level * 0.5f;
            towerBody.transform.localScale = new Vector3(towerWidth, towerHeight, towerWidth);

            Material towerMat = CreateMaterial(Color.Lerp(color, Color.white, 0.3f), 0.2f);
            towerBody.GetComponent<Renderer>().material = towerMat;
            Destroy(towerBody.GetComponent<Collider>()); // Don't need collision on tower

            // Tower top (cone for roof)
            GameObject towerRoof = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            towerRoof.name = "TowerRoof";
            towerRoof.transform.parent = mainTower.transform;
            towerRoof.transform.localPosition = new Vector3(0, towerHeight, 0);
            towerRoof.transform.localScale = new Vector3(towerWidth * 1.5f, 3f, towerWidth * 1.5f);

            Material roofMat = CreateMaterial(Color.Lerp(color, new Color(0.3f, 0.2f, 0.1f), 0.5f), 0.1f);
            towerRoof.GetComponent<Renderer>().material = roofMat;
            Destroy(towerRoof.GetComponent<Collider>());

            // Add smaller towers for higher level citadels
            if (Level >= 3)
            {
                CreateSideTower(mainTower.transform, new Vector3(6, -2, 0), color, towerHeight * 0.6f);
                CreateSideTower(mainTower.transform, new Vector3(-6, -2, 0), color, towerHeight * 0.6f);
            }
            if (Level >= 5)
            {
                CreateSideTower(mainTower.transform, new Vector3(0, -2, 6), color, towerHeight * 0.5f);
                CreateSideTower(mainTower.transform, new Vector3(0, -2, -6), color, towerHeight * 0.5f);
            }
        }
        
        private void CreateSideTowerFromModel(Transform parent, Vector3 pos, Color color, float scaleMult)
        {
            if (BuildingModelProvider.Instance == null) return;
            
            GameObject tower = BuildingModelProvider.Instance.GetTowerModel(AssetTowerType.Guard, parent);
            if (tower != null)
            {
                tower.name = "SideTower";
                tower.transform.localPosition = pos;
                tower.transform.localScale = Vector3.one * scaleMult * modelScale;
                ApplyOwnershipTint(tower, color);
            }
        }

        private void CreateSideTower(Transform parent, Vector3 pos, Color color, float height)
        {
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tower.name = "SideTower";
            tower.transform.parent = parent;
            tower.transform.localPosition = pos;
            tower.transform.localScale = new Vector3(2f, height, 2f);

            Material mat = CreateMaterial(Color.Lerp(color, Color.white, 0.4f), 0.2f);
            tower.GetComponent<Renderer>().material = mat;
            Destroy(tower.GetComponent<Collider>());
        }

        private void CreateWalls(Color color)
        {
            int wallCount = 8;
            float wallRadius = 12f + Level * 2f;
            float wallHeight = 3f + Level;

            wallSegments = new GameObject[wallCount];

            for (int i = 0; i < wallCount; i++)
            {
                float angle = i * (360f / wallCount) * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * wallRadius,
                    wallHeight / 2f,
                    Mathf.Sin(angle) * wallRadius
                );

                GameObject wall;
                
                // Try to use real wall model
                if (useRealModels && BuildingModelProvider.Instance != null)
                {
                    // Alternate between straight walls and gate on entry
                    AssetWallType wallType = (i == 0) ? AssetWallType.GateSmall : AssetWallType.Straight;
                    wall = BuildingModelProvider.Instance.GetWallModel(wallType, AssetWallMaterial.Stone, transform);
                    
                    if (wall != null)
                    {
                        wall.name = $"Wall_{i}";
                        wall.transform.localPosition = pos;
                        wall.transform.localScale = Vector3.one * modelScale;
                        wall.transform.LookAt(transform.position);
                        ApplyOwnershipTint(wall, color);
                        wallSegments[i] = wall;
                        continue;
                    }
                }
                
                // Fallback to primitive
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"Wall_{i}";
                wall.transform.parent = transform;
                wall.transform.localPosition = pos;
                wall.transform.localScale = new Vector3(8f, wallHeight, 1.5f);
                wall.transform.LookAt(transform.position);

                Material wallMat = CreateMaterial(Color.Lerp(color, new Color(0.5f, 0.5f, 0.5f), 0.6f), 0.1f);
                wall.GetComponent<Renderer>().material = wallMat;
                Destroy(wall.GetComponent<Collider>());

                wallSegments[i] = wall;
            }
        }
        
        /// <summary>
        /// Apply ownership color tint to all renderers in a model
        /// </summary>
        private void ApplyOwnershipTint(GameObject model, Color tintColor)
        {
            if (model == null) return;
            
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                if (renderer.material != null)
                {
                    // Create instance to avoid modifying shared materials
                    Material mat = new Material(renderer.material);
                    
                    // Blend the existing color with the ownership tint
                    if (mat.HasProperty("_Color"))
                    {
                        Color existingColor = mat.color;
                        mat.color = Color.Lerp(existingColor, tintColor, 0.3f);
                    }
                    else if (mat.HasProperty("_BaseColor"))
                    {
                        Color existingColor = mat.GetColor("_BaseColor");
                        mat.SetColor("_BaseColor", Color.Lerp(existingColor, tintColor, 0.3f));
                    }
                    
                    // Add a subtle emission for owned territories
                    if (Ownership == TerritoryOwnership.Owned || Ownership == TerritoryOwnership.Contested)
                    {
                        if (mat.HasProperty("_EmissionColor"))
                        {
                            mat.EnableKeyword("_EMISSION");
                            mat.SetColor("_EmissionColor", tintColor * 0.2f);
                        }
                    }
                    
                    renderer.material = mat;
                }
            }
        }

        private void CreateFlag(Color color)
        {
            flagPole = new GameObject("FlagPole");
            flagPole.transform.parent = transform;
            
            float towerHeight = 10f + Level * 5f;
            flagPole.transform.localPosition = new Vector3(0, towerHeight + 5f, 0);

            // Pole
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.parent = flagPole.transform;
            pole.transform.localPosition = Vector3.zero;
            pole.transform.localScale = new Vector3(0.2f, 4f, 0.2f);
            pole.GetComponent<Renderer>().material = CreateMaterial(new Color(0.4f, 0.3f, 0.2f), 0.1f);
            Destroy(pole.GetComponent<Collider>());

            // Flag (quad or cube)
            GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "Flag";
            flag.transform.parent = flagPole.transform;
            flag.transform.localPosition = new Vector3(1.5f, 3f, 0);
            flag.transform.localScale = new Vector3(3f, 2f, 0.1f);
            
            Material flagMat = CreateMaterial(color, 0.5f);
            flagMat.EnableKeyword("_EMISSION");
            flagMat.SetColor("_EmissionColor", color * 0.3f);
            flag.GetComponent<Renderer>().material = flagMat;
            Destroy(flag.GetComponent<Collider>());
        }

        private void CreateSelectionRing(Color color)
        {
            selectionRing = new GameObject("SelectionRing");
            selectionRing.transform.parent = transform;
            selectionRing.transform.localPosition = new Vector3(0, 0.5f, 0);

            // Create ring using torus approximation (multiple cubes in circle)
            int segments = 32;
            float ringRadius = 20f + Level * 3f;
            
            for (int i = 0; i < segments; i++)
            {
                float angle = i * (360f / segments) * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * ringRadius,
                    0,
                    Mathf.Sin(angle) * ringRadius
                );

                GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                segment.transform.parent = selectionRing.transform;
                segment.transform.localPosition = pos;
                segment.transform.localScale = new Vector3(2f, 0.5f, 1f);
                segment.transform.LookAt(selectionRing.transform.position);

                Material mat = CreateMaterial(color, 0.8f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 0.5f);
                segment.GetComponent<Renderer>().material = mat;
                Destroy(segment.GetComponent<Collider>());
            }

            selectionRing.SetActive(false);
        }

        private void CreateTerritoryLight(Color color)
        {
            GameObject lightObj = new GameObject("TerritoryLight");
            lightObj.transform.parent = transform;
            lightObj.transform.localPosition = new Vector3(0, 30f, 0);

            territoryLight = lightObj.AddComponent<Light>();
            territoryLight.type = LightType.Point;
            territoryLight.color = color;
            territoryLight.intensity = 0.5f;
            territoryLight.range = 40f;
            territoryLight.shadows = LightShadows.None;
        }

        private void CreateAuraEffect(Color color)
        {
            auraEffect = new GameObject("AuraEffect");
            auraEffect.transform.parent = transform;
            auraEffect.transform.localPosition = Vector3.zero;

            // Create glowing ground ring
            GameObject auraRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            auraRing.transform.parent = auraEffect.transform;
            auraRing.transform.localPosition = new Vector3(0, -0.5f, 0);
            
            float auraSize = 25f + Level * 4f;
            auraRing.transform.localScale = new Vector3(auraSize, 0.1f, auraSize);

            _glowMaterial = CreateMaterial(color, 0.9f);
            _glowMaterial.EnableKeyword("_EMISSION");
            _glowMaterial.SetColor("_EmissionColor", color * 0.2f);
            
            // Make semi-transparent
            _glowMaterial.SetFloat("_Surface", 1);
            _glowMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _glowMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _glowMaterial.renderQueue = 3000;
            
            Color transparentColor = color;
            transparentColor.a = 0.3f;
            _glowMaterial.SetColor("_BaseColor", transparentColor);
            
            auraRing.GetComponent<Renderer>().material = _glowMaterial;
            Destroy(auraRing.GetComponent<Collider>());
        }

        private Material CreateMaterial(Color color, float smoothness)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
            {
                mat = new Material(Shader.Find("Standard"));
            }
            
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color); // For Standard shader
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0.1f);
            
            return mat;
        }

        private Color GetOwnershipColor()
        {
            return Ownership switch
            {
                TerritoryOwnership.Owned => OwnedColor,
                TerritoryOwnership.Alliance => AllianceColor,
                TerritoryOwnership.Enemy => EnemyColor,
                TerritoryOwnership.Contested => ContestedColor,
                _ => NeutralColor
            };
        }

        private void Update()
        {
            // Pulse effect when selected or hovered
            if (_isSelected || _isHovered)
            {
                _pulseTime += Time.deltaTime * 3f;
                float pulse = (Mathf.Sin(_pulseTime) + 1f) / 2f * 0.3f + 0.7f;
                
                if (selectionRing != null && selectionRing.activeSelf)
                {
                    selectionRing.transform.Rotate(0, 20f * Time.deltaTime, 0);
                }
                
                if (territoryLight != null)
                {
                    territoryLight.intensity = pulse;
                }
            }
        }

        /// <summary>
        /// Set selection state
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            if (selectionRing != null)
            {
                selectionRing.SetActive(selected);
            }
        }

        /// <summary>
        /// Set hover state
        /// </summary>
        public void SetHovered(bool hovered)
        {
            _isHovered = hovered;
            
            // Scale up slightly on hover
            float scale = hovered ? 1.1f : 1f;
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * scale, Time.deltaTime * 10f);
        }

        /// <summary>
        /// Update ownership and refresh visuals
        /// </summary>
        public void SetOwnership(TerritoryOwnership ownership)
        {
            Ownership = ownership;
            BuildVisual();
        }
    }

    public enum TerritoryOwnership
    {
        Neutral,
        Owned,
        Alliance,
        Enemy,
        Contested
    }
}
