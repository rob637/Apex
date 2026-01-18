// ============================================================================
// APEX CITADELS - TROOP TRAINING INTEGRATION
// Scene setup and component connection for troop system
// ============================================================================
using UnityEngine;
using UnityEngine.UI;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Integration component that sets up all troop training UI components
    /// </summary>
    public class TroopTrainingIntegration : MonoBehaviour
    {
        [Header("Keyboard Shortcuts")]
        [SerializeField] private KeyCode openPanelKey = KeyCode.T;

        [Header("Auto-Setup")]
        [SerializeField] private bool autoCreateCanvas = true;
        [SerializeField] private bool autoCreateEventSystem = true;

        private void Start()
        {
            EnsureRequiredComponents();
            SetupTroopSystem();
        }

        private void EnsureRequiredComponents()
        {
            // Ensure Canvas exists
            if (autoCreateCanvas && FindFirstObjectByType<Canvas>() == null)
            {
                CreateCanvas();
            }

            // Ensure EventSystem exists
            if (autoCreateEventSystem && FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                CreateEventSystem();
            }
        }

        private void CreateCanvas()
        {
            GameObject canvasObj = new GameObject("UICanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            Debug.Log("[TroopTrainingIntegration] Created UI Canvas");
        }

        private void CreateEventSystem()
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            Debug.Log("[TroopTrainingIntegration] Created Event System");
        }

        private void SetupTroopSystem()
        {
            // Create training queue manager (persistent)
            if (TrainingQueueManager.Instance == null)
            {
                GameObject queueMgr = new GameObject("TrainingQueueManager");
                queueMgr.AddComponent<TrainingQueueManager>();
                Debug.Log("[TroopTrainingIntegration] Created TrainingQueueManager");
            }

            // Create troop manager (persistent)
            if (TroopManager.Instance == null)
            {
                GameObject troopMgr = new GameObject("TroopManager");
                troopMgr.AddComponent<TroopManager>();
                Debug.Log("[TroopTrainingIntegration] Created TroopManager");
            }

            // Create training panel
            if (TroopTrainingPanel.Instance == null)
            {
                GameObject panelObj = new GameObject("TroopTrainingPanelRoot");
                panelObj.AddComponent<TroopTrainingPanel>();
                Debug.Log("[TroopTrainingIntegration] Created TroopTrainingPanel");
            }

            // Create quick bar
            if (TroopQuickBar.Instance == null)
            {
                GameObject quickBarObj = new GameObject("TroopQuickBarRoot");
                quickBarObj.AddComponent<TroopQuickBar>();
                Debug.Log("[TroopTrainingIntegration] Created TroopQuickBar");
            }

            Debug.Log("[TroopTrainingIntegration] Troop system setup complete!");
        }

        private void Update()
        {
            // Toggle training panel with keyboard shortcut
            if (Input.GetKeyDown(openPanelKey))
            {
                TroopTrainingPanel.Instance?.Toggle();
            }
        }

        /// <summary>
        /// Quick setup from menu
        /// </summary>
        #if UNITY_EDITOR
        [UnityEditor.MenuItem("Apex Citadels/Setup Troop Training System")]
        public static void SetupFromMenu()
        {
            if (FindFirstObjectByType<TroopTrainingIntegration>() != null)
            {
                Debug.Log("Troop Training System already exists in scene!");
                return;
            }

            GameObject integrationObj = new GameObject("TroopTrainingIntegration");
            integrationObj.AddComponent<TroopTrainingIntegration>();

            UnityEditor.Selection.activeGameObject = integrationObj;
            Debug.Log("Troop Training System created!");
        }
        #endif
    }
}
