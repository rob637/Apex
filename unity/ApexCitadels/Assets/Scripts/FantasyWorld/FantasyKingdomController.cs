// ============================================================================
// APEX CITADELS - FANTASY KINGDOM CONTROLLER
// Main scene controller for standalone Fantasy Kingdom experience
// ============================================================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.FantasyWorld
{
    /// <summary>
    /// Main controller for the Fantasy Kingdom scene
    /// Handles initialization, UI, and player management
    /// </summary>
    public class FantasyKingdomController : MonoBehaviour
    {
        [Header("=== REFERENCES ===")]
        [SerializeField] private StandaloneFantasyGenerator worldGenerator;
        [SerializeField] private FantasyPlayerController playerController;
        [SerializeField] private Camera mainCamera;
        
        [Header("=== UI REFERENCES ===")]
        [SerializeField] private Canvas loadingCanvas;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI statusText;
        
        [Header("=== GAMEPLAY UI ===")]
        [SerializeField] private Canvas gameplayCanvas;
        [SerializeField] private GameObject controlsHelp;
        
        [Header("=== SETTINGS ===")]
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private Vector3 playerSpawnOffset = new Vector3(0, 1, 50);
        
        private bool isReady = false;
        
        private void Awake()
        {
            // Find references if not assigned
            if (worldGenerator == null)
                worldGenerator = FindAnyObjectByType<StandaloneFantasyGenerator>();
            
            if (playerController == null)
                playerController = FindAnyObjectByType<FantasyPlayerController>();
            
            if (mainCamera == null)
                mainCamera = Camera.main;
        }
        
        private void Start()
        {
            // Setup UI
            if (loadingCanvas != null)
                loadingCanvas.gameObject.SetActive(true);
            
            if (gameplayCanvas != null)
                gameplayCanvas.gameObject.SetActive(false);
            
            // Disable player until world is ready
            if (playerController != null)
                playerController.SetEnabled(false);
            
            // Subscribe to generator events
            if (worldGenerator != null)
            {
                worldGenerator.OnGenerationStarted += OnGenerationStarted;
                worldGenerator.OnGenerationProgress += OnGenerationProgress;
                worldGenerator.OnGenerationComplete += OnGenerationComplete;
            }
            
            if (generateOnStart)
            {
                StartCoroutine(InitializeWorld());
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (worldGenerator != null)
            {
                worldGenerator.OnGenerationStarted -= OnGenerationStarted;
                worldGenerator.OnGenerationProgress -= OnGenerationProgress;
                worldGenerator.OnGenerationComplete -= OnGenerationComplete;
            }
        }
        
        private IEnumerator InitializeWorld()
        {
            UpdateStatus("Initializing Fantasy Kingdom...");
            yield return new WaitForSeconds(0.5f);
            
            // Clear any existing world
            if (worldGenerator != null)
            {
                worldGenerator.Clear();
                yield return null;
                
                // Start generation
                UpdateStatus("Generating your kingdom...");
                worldGenerator.Generate();
            }
            else
            {
                Debug.LogError("[FantasyKingdom] No world generator found!");
                UpdateStatus("Error: World generator not found");
            }
        }
        
        private void OnGenerationStarted()
        {
            UpdateStatus("Building your fantasy kingdom...");
            UpdateProgress(0);
        }
        
        private void OnGenerationProgress(float progress)
        {
            UpdateProgress(progress);
            
            // Update status based on progress
            if (progress < 0.15f)
                UpdateStatus("Shaping the terrain...");
            else if (progress < 0.25f)
                UpdateStatus("Planning roads...");
            else if (progress < 0.35f)
                UpdateStatus("Constructing castle walls...");
            else if (progress < 0.6f)
                UpdateStatus("Building homes and shops...");
            else if (progress < 0.75f)
                UpdateStatus("Laying cobblestone paths...");
            else if (progress < 0.9f)
                UpdateStatus("Planting the forest...");
            else
                UpdateStatus("Adding final touches...");
        }
        
        private void OnGenerationComplete()
        {
            UpdateStatus("Welcome to your Fantasy Kingdom!");
            UpdateProgress(1f);
            
            StartCoroutine(TransitionToGameplay());
        }
        
        private IEnumerator TransitionToGameplay()
        {
            yield return new WaitForSeconds(1f);
            
            // Fade out loading screen
            if (loadingCanvas != null)
            {
                var canvasGroup = loadingCanvas.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = loadingCanvas.gameObject.AddComponent<CanvasGroup>();
                
                float fadeTime = 0.5f;
                float elapsed = 0f;
                
                while (elapsed < fadeTime)
                {
                    elapsed += Time.deltaTime;
                    canvasGroup.alpha = 1f - (elapsed / fadeTime);
                    yield return null;
                }
                
                loadingCanvas.gameObject.SetActive(false);
            }
            
            // Enable gameplay UI
            if (gameplayCanvas != null)
                gameplayCanvas.gameObject.SetActive(true);
            
            // Position and enable player
            SpawnPlayer();
            
            // Show controls briefly
            if (controlsHelp != null)
            {
                controlsHelp.SetActive(true);
                yield return new WaitForSeconds(5f);
                controlsHelp.SetActive(false);
            }
            
            isReady = true;
            Debug.Log("[FantasyKingdom] Kingdom ready for exploration!");
        }
        
        private void SpawnPlayer()
        {
            if (playerController != null)
            {
                // Spawn player at offset from center (outside castle, on main road)
                playerController.transform.position = playerSpawnOffset;
                playerController.transform.rotation = Quaternion.Euler(0, 180, 0); // Face toward castle
                playerController.SetEnabled(true);
                
                Debug.Log($"[FantasyKingdom] Player spawned at {playerSpawnOffset}");
            }
            else
            {
                // Create simple placeholder player
                CreatePlaceholderPlayer();
            }
        }
        
        private void CreatePlaceholderPlayer()
        {
            var player = new GameObject("Player");
            player.transform.position = playerSpawnOffset;
            
            // Add character controller
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0, 0.9f, 0);
            
            // Add camera
            if (mainCamera != null)
            {
                mainCamera.transform.SetParent(player.transform);
                mainCamera.transform.localPosition = new Vector3(0, 1.6f, 0);
                mainCamera.transform.localRotation = Quaternion.identity;
            }
            
            // Add simple controller
            var controller = player.AddComponent<FantasyPlayerController>();
            controller.SetEnabled(true);
            
            playerController = controller;
            
            Debug.Log("[FantasyKingdom] Created placeholder player");
        }
        
        private void UpdateStatus(string status)
        {
            if (statusText != null)
                statusText.text = status;
        }
        
        private void UpdateProgress(float progress)
        {
            if (progressSlider != null)
                progressSlider.value = progress;
            
            if (progressText != null)
                progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }
        
        // ====================================================================
        // PUBLIC API
        // ====================================================================
        
        /// <summary>
        /// Regenerate the world with current settings
        /// </summary>
        public void RegenerateWorld()
        {
            if (worldGenerator == null || worldGenerator.IsGenerating) return;
            
            isReady = false;
            
            if (playerController != null)
                playerController.SetEnabled(false);
            
            if (loadingCanvas != null)
                loadingCanvas.gameObject.SetActive(true);
            
            if (gameplayCanvas != null)
                gameplayCanvas.gameObject.SetActive(false);
            
            StartCoroutine(InitializeWorld());
        }
        
        /// <summary>
        /// Teleport player to center of kingdom
        /// </summary>
        public void TeleportToCenter()
        {
            if (playerController != null)
            {
                playerController.transform.position = new Vector3(0, 1, 30);
            }
        }
        
        public bool IsReady => isReady;
    }
}
