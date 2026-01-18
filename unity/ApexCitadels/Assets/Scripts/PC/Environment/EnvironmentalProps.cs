// ============================================================================
// APEX CITADELS - ENVIRONMENTAL PROPS SYSTEM
// Trees, rocks, roads, and decorations for world detail
// ============================================================================
using UnityEngine;
using System.Collections.Generic;

namespace ApexCitadels.PC.Environment
{
    /// <summary>
    /// Generates environmental props to fill the world with detail.
    /// Creates forests, rock formations, roads, and decorations.
    /// </summary>
    public class EnvironmentalProps : MonoBehaviour
    {
        public static EnvironmentalProps Instance { get; private set; }

        [Header("Tree Settings")]
        [SerializeField] private int treeCount = 300;
        [SerializeField] private float treeSpreadRadius = 600f;
        [SerializeField] private float minTreeScale = 0.8f;
        [SerializeField] private float maxTreeScale = 1.5f;

        [Header("Rock Settings")]
        [SerializeField] private int rockCount = 150;
        [SerializeField] private float rockSpreadRadius = 500f;

        [Header("Grass Patches")]
        [SerializeField] private int grassPatchCount = 100;

        [Header("Colors")]
        [SerializeField] private Color trunkColor = new Color(0.35f, 0.25f, 0.15f);
        [SerializeField] private Color foliageColor = new Color(0.15f, 0.4f, 0.15f);
        [SerializeField] private Color foliageColorAlt = new Color(0.2f, 0.5f, 0.2f);
        [SerializeField] private Color rockColor = new Color(0.4f, 0.4f, 0.38f);
        [SerializeField] private Color rockColorAlt = new Color(0.5f, 0.48f, 0.45f);

        // Containers
        private GameObject _treeContainer;
        private GameObject _rockContainer;
        private GameObject _grassContainer;
        private List<GameObject> _trees = new List<GameObject>();
        private List<GameObject> _rocks = new List<GameObject>();

        // Reference to terrain
        private ProceduralTerrain _terrain;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            _terrain = FindFirstObjectByType<ProceduralTerrain>();
            GenerateAllProps();
        }

        /// <summary>
        /// Generate all environmental props
        /// </summary>
        public void GenerateAllProps()
        {
            Debug.Log("[Props] Generating environmental props...");
            
            ClearExisting();
            
            CreateContainers();
            GenerateTrees();
            GenerateRocks();
            GenerateGrassPatches();
            
            Debug.Log($"[Props] Generated {_trees.Count} trees, {_rocks.Count} rocks");
        }

        private void ClearExisting()
        {
            if (_treeContainer != null) Destroy(_treeContainer);
            if (_rockContainer != null) Destroy(_rockContainer);
            if (_grassContainer != null) Destroy(_grassContainer);
            _trees.Clear();
            _rocks.Clear();
        }

        private void CreateContainers()
        {
            _treeContainer = new GameObject("Trees");
            _treeContainer.transform.parent = transform;
            
            _rockContainer = new GameObject("Rocks");
            _rockContainer.transform.parent = transform;
            
            _grassContainer = new GameObject("GrassPatches");
            _grassContainer.transform.parent = transform;
        }

        #region Trees

        private void GenerateTrees()
        {
            Material trunkMat = CreateMaterial(trunkColor, 0.1f);
            Material foliageMat = CreateMaterial(foliageColor, 0.3f);
            Material foliageMatAlt = CreateMaterial(foliageColorAlt, 0.3f);

            for (int i = 0; i < treeCount; i++)
            {
                Vector3 pos = GetRandomValidPosition(treeSpreadRadius);
                if (pos == Vector3.zero) continue;

                // Don't place trees in water or on very steep slopes
                if (_terrain != null && _terrain.IsWater(pos.x, pos.z)) continue;

                GameObject tree = CreateTree(i, pos, Random.value > 0.5f ? foliageMat : foliageMatAlt, trunkMat);
                _trees.Add(tree);
            }
        }

        private GameObject CreateTree(int index, Vector3 position, Material foliageMat, Material trunkMat)
        {
            GameObject tree = new GameObject($"Tree_{index}");
            tree.transform.parent = _treeContainer.transform;
            tree.transform.position = position;

            float scale = Random.Range(minTreeScale, maxTreeScale);
            tree.transform.localScale = Vector3.one * scale;

            // Random tree type
            int treeType = Random.Range(0, 3);
            
            switch (treeType)
            {
                case 0:
                    CreatePineTree(tree.transform, foliageMat, trunkMat);
                    break;
                case 1:
                    CreateOakTree(tree.transform, foliageMat, trunkMat);
                    break;
                default:
                    CreateBushyTree(tree.transform, foliageMat, trunkMat);
                    break;
            }

            // Random rotation
            tree.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            return tree;
        }

        private void CreatePineTree(Transform parent, Material foliageMat, Material trunkMat)
        {
            // Trunk
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.parent = parent;
            trunk.transform.localPosition = new Vector3(0, 4f, 0);
            trunk.transform.localScale = new Vector3(0.8f, 8f, 0.8f);
            trunk.GetComponent<Renderer>().material = trunkMat;
            Destroy(trunk.GetComponent<Collider>());

            // Pine layers (cones stacked)
            float[] heights = { 6f, 9f, 12f, 14f };
            float[] sizes = { 4f, 3f, 2f, 1f };
            
            for (int i = 0; i < heights.Length; i++)
            {
                GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                foliage.name = $"Foliage_{i}";
                foliage.transform.parent = parent;
                foliage.transform.localPosition = new Vector3(0, heights[i], 0);
                foliage.transform.localScale = new Vector3(sizes[i], sizes[i] * 0.7f, sizes[i]);
                foliage.GetComponent<Renderer>().material = foliageMat;
                Destroy(foliage.GetComponent<Collider>());
            }
        }

        private void CreateOakTree(Transform parent, Material foliageMat, Material trunkMat)
        {
            // Trunk
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.parent = parent;
            trunk.transform.localPosition = new Vector3(0, 3f, 0);
            trunk.transform.localScale = new Vector3(1f, 6f, 1f);
            trunk.GetComponent<Renderer>().material = trunkMat;
            Destroy(trunk.GetComponent<Collider>());

            // Round canopy (multiple overlapping spheres)
            Vector3[] offsets = {
                new Vector3(0, 8f, 0),
                new Vector3(1.5f, 7f, 0),
                new Vector3(-1.5f, 7f, 0),
                new Vector3(0, 7f, 1.5f),
                new Vector3(0, 7f, -1.5f),
                new Vector3(0, 9f, 0)
            };
            float[] sizes = { 4f, 3f, 3f, 3f, 3f, 3f };
            
            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                foliage.name = $"Canopy_{i}";
                foliage.transform.parent = parent;
                foliage.transform.localPosition = offsets[i];
                foliage.transform.localScale = Vector3.one * sizes[i];
                foliage.GetComponent<Renderer>().material = foliageMat;
                Destroy(foliage.GetComponent<Collider>());
            }
        }

        private void CreateBushyTree(Transform parent, Material foliageMat, Material trunkMat)
        {
            // Short trunk
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.parent = parent;
            trunk.transform.localPosition = new Vector3(0, 1.5f, 0);
            trunk.transform.localScale = new Vector3(0.6f, 3f, 0.6f);
            trunk.GetComponent<Renderer>().material = trunkMat;
            Destroy(trunk.GetComponent<Collider>());

            // Bushy canopy
            int puffCount = Random.Range(5, 8);
            for (int i = 0; i < puffCount; i++)
            {
                GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puff.name = $"Bush_{i}";
                puff.transform.parent = parent;
                puff.transform.localPosition = new Vector3(
                    Random.Range(-2f, 2f),
                    Random.Range(3f, 5f),
                    Random.Range(-2f, 2f)
                );
                float puffSize = Random.Range(1.5f, 2.5f);
                puff.transform.localScale = Vector3.one * puffSize;
                puff.GetComponent<Renderer>().material = foliageMat;
                Destroy(puff.GetComponent<Collider>());
            }
        }

        #endregion

        #region Rocks

        private void GenerateRocks()
        {
            Material rockMat = CreateMaterial(rockColor, 0.15f);
            Material rockMatAlt = CreateMaterial(rockColorAlt, 0.1f);

            for (int i = 0; i < rockCount; i++)
            {
                Vector3 pos = GetRandomValidPosition(rockSpreadRadius);
                if (pos == Vector3.zero) continue;

                // Rocks can be near water
                GameObject rock = CreateRock(i, pos, Random.value > 0.5f ? rockMat : rockMatAlt);
                _rocks.Add(rock);
            }
        }

        private GameObject CreateRock(int index, Vector3 position, Material mat)
        {
            GameObject rock = new GameObject($"Rock_{index}");
            rock.transform.parent = _rockContainer.transform;
            rock.transform.position = position;

            // Create rock cluster
            int stoneCount = Random.Range(1, 4);
            
            for (int i = 0; i < stoneCount; i++)
            {
                GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                stone.name = $"Stone_{i}";
                stone.transform.parent = rock.transform;
                stone.transform.localPosition = new Vector3(
                    Random.Range(-1f, 1f) * i,
                    Random.Range(0f, 0.5f),
                    Random.Range(-1f, 1f) * i
                );
                
                // Irregular rock shape
                float scaleX = Random.Range(1f, 3f);
                float scaleY = Random.Range(0.5f, 1.5f);
                float scaleZ = Random.Range(1f, 3f);
                stone.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
                stone.transform.localRotation = Quaternion.Euler(
                    Random.Range(-20f, 20f),
                    Random.Range(0f, 360f),
                    Random.Range(-20f, 20f)
                );
                
                stone.GetComponent<Renderer>().material = mat;
                Destroy(stone.GetComponent<Collider>());
            }

            return rock;
        }

        #endregion

        #region Grass Patches

        private void GenerateGrassPatches()
        {
            Material grassMat = CreateMaterial(new Color(0.2f, 0.55f, 0.2f), 0.4f);

            for (int i = 0; i < grassPatchCount; i++)
            {
                Vector3 pos = GetRandomValidPosition(treeSpreadRadius);
                if (pos == Vector3.zero) continue;
                if (_terrain != null && _terrain.IsWater(pos.x, pos.z)) continue;

                CreateGrassPatch(i, pos, grassMat);
            }
        }

        private void CreateGrassPatch(int index, Vector3 position, Material mat)
        {
            GameObject patch = new GameObject($"GrassPatch_{index}");
            patch.transform.parent = _grassContainer.transform;
            patch.transform.position = position;

            // Create several grass blade clusters
            int bladeCount = Random.Range(5, 15);
            
            for (int i = 0; i < bladeCount; i++)
            {
                GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name = "GrassBlade";
                blade.transform.parent = patch.transform;
                blade.transform.localPosition = new Vector3(
                    Random.Range(-2f, 2f),
                    0.5f,
                    Random.Range(-2f, 2f)
                );
                blade.transform.localScale = new Vector3(0.1f, Random.Range(0.8f, 1.5f), 0.05f);
                blade.transform.localRotation = Quaternion.Euler(
                    Random.Range(-10f, 10f),
                    Random.Range(0f, 360f),
                    Random.Range(-10f, 10f)
                );
                blade.GetComponent<Renderer>().material = mat;
                Destroy(blade.GetComponent<Collider>());
            }
        }

        #endregion

        #region Helpers

        private Vector3 GetRandomValidPosition(float radius)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(50f, radius);
            
            float x = Mathf.Cos(angle) * dist;
            float z = Mathf.Sin(angle) * dist;
            
            float y = 0f;
            if (_terrain != null)
            {
                y = _terrain.GetTerrainHeight(x, z);
            }
            
            return new Vector3(x, y, z);
        }

        private Material CreateMaterial(Color color, float smoothness)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
            {
                mat = new Material(Shader.Find("Standard"));
            }
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0f);
            return mat;
        }

        /// <summary>
        /// Check if position is near any prop (for spacing)
        /// </summary>
        public bool IsNearProp(Vector3 position, float minDistance)
        {
            foreach (var tree in _trees)
            {
                if (tree != null && Vector3.Distance(tree.transform.position, position) < minDistance)
                    return true;
            }
            foreach (var rock in _rocks)
            {
                if (rock != null && Vector3.Distance(rock.transform.position, position) < minDistance)
                    return true;
            }
            return false;
        }

        #endregion
    }
}
