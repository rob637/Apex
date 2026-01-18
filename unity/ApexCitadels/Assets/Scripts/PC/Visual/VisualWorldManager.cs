// ============================================================================
// APEX CITADELS - VISUAL WORLD MANAGER
// Master controller that initializes all visual systems
// ============================================================================
using UnityEngine;

namespace ApexCitadels.PC.Visual
{
    /// <summary>
    /// Master manager for all visual systems.
    /// Initializes terrain, skybox, citadels, audio, and post-processing.
    /// This transforms the flat UI-only game into a beautiful 3D experience.
    /// </summary>
    public class VisualWorldManager : MonoBehaviour
    {
        public static VisualWorldManager Instance { get; private set; }

        [Header("Visual Systems")]
        private TerrainVisualSystem terrainSystem;
        private SkyboxEnvironmentSystem skyboxSystem;
        private CitadelVisualSystem citadelSystem;
        private AudioManager audioManager;
        private PostProcessingSetup postProcessing;

        [Header("Settings")]
        public bool enableTerrain = true;
        public bool enableSkybox = true;
        public bool enableCitadels = true;
        public bool enableAudio = true;
        public bool enablePostProcessing = true;

        [Header("Territory Visuals")]
        private bool territoriesInitialized = false;

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
            Debug.Log("═══════════════════════════════════════════════════════════════════════");
            Debug.Log("  APEX CITADELS - VISUAL WORLD MANAGER STARTING");
            Debug.Log("═══════════════════════════════════════════════════════════════════════");

            InitializeVisualSystems();
            
            // Subscribe to territory updates
            Invoke(nameof(InitializeTerritoryVisuals), 2f); // Delay to let Firebase load

            Debug.Log("═══════════════════════════════════════════════════════════════════════");
            Debug.Log("  ✅ VISUAL SYSTEMS INITIALIZED - World should now look amazing!");
            Debug.Log("═══════════════════════════════════════════════════════════════════════");
        }

        private void InitializeVisualSystems()
        {
            // Create container for all visual systems
            GameObject visualSystems = new GameObject("VisualSystems");
            visualSystems.transform.SetParent(transform);

            // 1. Terrain System - Creates 3D heightmapped terrain
            if (enableTerrain)
            {
                GameObject terrainObj = new GameObject("TerrainSystem");
                terrainObj.transform.SetParent(visualSystems.transform);
                terrainSystem = terrainObj.AddComponent<TerrainVisualSystem>();
                Debug.Log("[VisualWorld] ✅ Terrain system created");
            }

            // 2. Skybox & Environment - Dynamic sky, day/night, clouds
            if (enableSkybox)
            {
                GameObject skyboxObj = new GameObject("SkyboxSystem");
                skyboxObj.transform.SetParent(visualSystems.transform);
                skyboxSystem = skyboxObj.AddComponent<SkyboxEnvironmentSystem>();
                Debug.Log("[VisualWorld] ✅ Skybox system created");
            }

            // 3. Citadel Visuals - 3D buildings for territories
            if (enableCitadels)
            {
                GameObject citadelObj = new GameObject("CitadelSystem");
                citadelObj.transform.SetParent(visualSystems.transform);
                citadelSystem = citadelObj.AddComponent<CitadelVisualSystem>();
                Debug.Log("[VisualWorld] ✅ Citadel system created");
            }

            // 4. Audio Manager - Music, SFX, ambient sounds
            if (enableAudio)
            {
                GameObject audioObj = new GameObject("AudioManager");
                audioObj.transform.SetParent(visualSystems.transform);
                audioManager = audioObj.AddComponent<AudioManager>();
                Debug.Log("[VisualWorld] ✅ Audio manager created");
            }

            // 5. Post-Processing - Bloom, color grading, etc.
            if (enablePostProcessing)
            {
                GameObject ppObj = new GameObject("PostProcessing");
                ppObj.transform.SetParent(visualSystems.transform);
                postProcessing = ppObj.AddComponent<PostProcessingSetup>();
                Debug.Log("[VisualWorld] ✅ Post-processing created");
            }

            // 6. Disable old flat ground if it exists
            DisableFlatGround();

            // 7. Adjust camera settings for 3D world
            ConfigureCamera();
        }

        private void DisableFlatGround()
        {
            // Find and disable the old flat ground plane
            GameObject[] grounds = GameObject.FindGameObjectsWithTag("Untagged");
            foreach (var obj in grounds)
            {
                if (obj.name == "Ground" || obj.name == "GroundPlane" || obj.name == "Plane")
                {
                    obj.SetActive(false);
                    Debug.Log($"[VisualWorld] Disabled old ground: {obj.name}");
                }
            }

            // Also try by component
            MeshRenderer[] meshes = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            foreach (var mesh in meshes)
            {
                if (mesh.gameObject.name.Contains("Ground") && mesh.transform.localScale.x > 50)
                {
                    mesh.gameObject.SetActive(false);
                }
            }
        }

        private void ConfigureCamera()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                // Set clipping planes for the larger world
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 1000f;

                // Set field of view for strategy game
                cam.fieldOfView = 60f;

                // Clear to skybox
                cam.clearFlags = CameraClearFlags.Skybox;

                // Enable HDR for post-processing
                cam.allowHDR = true;
                cam.allowMSAA = true;

                Debug.Log("[VisualWorld] Camera configured for 3D world");
            }
        }

        /// <summary>
        /// Creates 3D citadel visuals for all loaded territories
        /// </summary>
        private void InitializeTerritoryVisuals()
        {
            if (territoriesInitialized) return;
            
            // Find WorldMapRenderer to get territory data
            var worldMap = Object.FindFirstObjectByType<WorldMapRenderer>();
            if (worldMap == null)
            {
                Debug.LogWarning("[VisualWorld] WorldMapRenderer not found, retrying...");
                Invoke(nameof(InitializeTerritoryVisuals), 2f);
                return;
            }

            // Get territories via reflection or public API
            // For now, create demo citadels at known positions
            CreateDemoCitadels();
            territoriesInitialized = true;
        }

        private void CreateDemoCitadels()
        {
            if (citadelSystem == null) return;

            // Create citadels at various positions on the terrain
            // These positions correspond roughly to where territories would be
            Vector3[] citadelPositions = new Vector3[]
            {
                new Vector3(0, 0, 0),          // Center
                new Vector3(50, 0, 30),        // Northeast
                new Vector3(-60, 0, 40),       // Northwest
                new Vector3(70, 0, -20),       // Southeast
                new Vector3(-50, 0, -50),      // Southwest
                new Vector3(30, 0, 80),        // Far north
                new Vector3(-80, 0, 0),        // West
                new Vector3(90, 0, 50),        // Far east
                new Vector3(0, 0, -70),        // South
                new Vector3(-30, 0, 70),       // North
            };

            Color[] factionColors = new Color[]
            {
                new Color(0.2f, 0.4f, 0.8f),   // Blue
                new Color(0.8f, 0.2f, 0.2f),   // Red
                new Color(0.2f, 0.7f, 0.3f),   // Green
                new Color(0.7f, 0.5f, 0.1f),   // Orange
                new Color(0.6f, 0.2f, 0.6f),   // Purple
                new Color(0.2f, 0.6f, 0.6f),   // Teal
                new Color(0.7f, 0.7f, 0.2f),   // Yellow
                new Color(0.5f, 0.3f, 0.2f),   // Brown
                new Color(0.8f, 0.4f, 0.6f),   // Pink
                new Color(0.4f, 0.4f, 0.5f),   // Gray
            };

            for (int i = 0; i < citadelPositions.Length; i++)
            {
                int level = Random.Range(1, 5);
                Color color = factionColors[i % factionColors.Length];
                
                citadelSystem.CreateCitadel(
                    $"territory_{i}",
                    citadelPositions[i],
                    level,
                    color
                );
            }

            Debug.Log($"[VisualWorld] Created {citadelPositions.Length} demo citadels");
        }

        #region Public API

        /// <summary>
        /// Sets the time of day (0-1, 0.5 = noon)
        /// </summary>
        public void SetTimeOfDay(float time)
        {
            if (skyboxSystem != null)
            {
                skyboxSystem.SetTimeOfDay(time);
                
                // Update audio ambient based on time
                if (audioManager != null)
                {
                    audioManager.SetAmbientForTimeOfDay(skyboxSystem.IsDaytime());
                }

                // Update post-processing
                if (postProcessing != null)
                {
                    postProcessing.ApplyPreset(skyboxSystem.IsDaytime() 
                        ? PostProcessingSetup.VisualPreset.Default 
                        : PostProcessingSetup.VisualPreset.Night);
                }
            }
        }

        /// <summary>
        /// Enters combat mode (visual effects)
        /// </summary>
        public void EnterCombatMode()
        {
            if (postProcessing != null)
            {
                postProcessing.TransitionToPreset(PostProcessingSetup.VisualPreset.Combat, 0.5f);
            }
            
            if (audioManager != null)
            {
                audioManager.SetAmbientForLocation("battle");
                audioManager.PlayWarHorn();
            }
        }

        /// <summary>
        /// Exits combat mode
        /// </summary>
        public void ExitCombatMode()
        {
            if (postProcessing != null)
            {
                postProcessing.TransitionToPreset(PostProcessingSetup.VisualPreset.Default, 1f);
            }
            
            if (audioManager != null)
            {
                bool isDaytime = skyboxSystem != null && skyboxSystem.IsDaytime();
                audioManager.SetAmbientForTimeOfDay(isDaytime);
            }
        }

        /// <summary>
        /// Plays victory celebration
        /// </summary>
        public void PlayVictory()
        {
            if (postProcessing != null)
            {
                postProcessing.TransitionToPreset(PostProcessingSetup.VisualPreset.Victory, 0.3f);
            }
            
            if (audioManager != null)
            {
                audioManager.PlayVictory();
            }

            // Reset after delay
            Invoke(nameof(ResetVisuals), 5f);
        }

        private void ResetVisuals()
        {
            if (postProcessing != null)
            {
                postProcessing.TransitionToPreset(PostProcessingSetup.VisualPreset.Default, 2f);
            }
        }

        /// <summary>
        /// Triggers damage flash effect
        /// </summary>
        public void TriggerDamage()
        {
            if (postProcessing != null)
            {
                postProcessing.DamageFlash();
            }
            
            if (audioManager != null)
            {
                audioManager.PlayCombat("CMB50_hit_reaction_heavy");
            }
        }

        /// <summary>
        /// Gets terrain height at a position
        /// </summary>
        public float GetTerrainHeight(Vector3 position)
        {
            if (terrainSystem != null)
            {
                return terrainSystem.GetHeightAt(position);
            }
            return 0f;
        }

        /// <summary>
        /// Creates a new citadel visual at runtime
        /// </summary>
        public void CreateCitadelAt(string id, Vector3 position, int level, Color color)
        {
            if (citadelSystem != null)
            {
                citadelSystem.CreateCitadel(id, position, level, color);
            }
        }

        #endregion

        private void Update()
        {
            // Debug key to cycle through visual presets
            if (Input.GetKeyDown(KeyCode.F5))
            {
                CycleVisualPreset();
            }

            // Debug key to trigger time of day changes
            if (Input.GetKeyDown(KeyCode.F6))
            {
                if (skyboxSystem != null)
                {
                    skyboxSystem.SetTimeOfDay(skyboxSystem.currentTimeOfDay + 0.1f);
                }
            }

            // Debug key to trigger combat effects
            if (Input.GetKeyDown(KeyCode.F7))
            {
                EnterCombatMode();
            }

            // Debug key to trigger victory
            if (Input.GetKeyDown(KeyCode.F8))
            {
                PlayVictory();
            }
        }

        private int currentPresetIndex = 0;
        private void CycleVisualPreset()
        {
            if (postProcessing == null) return;

            currentPresetIndex = (currentPresetIndex + 1) % 7;
            PostProcessingSetup.VisualPreset preset = (PostProcessingSetup.VisualPreset)currentPresetIndex;
            postProcessing.TransitionToPreset(preset, 0.5f);
            Debug.Log($"[VisualWorld] Switched to preset: {preset}");
        }
    }
}
