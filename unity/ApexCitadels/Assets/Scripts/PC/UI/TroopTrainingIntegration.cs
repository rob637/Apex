// ============================================================================
// APEX CITADELS - TROOP TRAINING INTEGRATION
// Scene setup and component connection for troop system
// ============================================================================
using UnityEngine;
using UnityEngine.UI;
using ApexCitadels.Core;

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

            ApexLogger.Log("Created UI Canvas", ApexLogger.LogCategory.UI);
        }

        private void CreateEventSystem()
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            ApexLogger.Log("Created Event System", ApexLogger.LogCategory.UI);
        }

        private void SetupTroopSystem()
        {
            // Create training queue manager (persistent)
            if (TrainingQueueManager.Instance == null)
            {
                GameObject queueMgr = new GameObject("TrainingQueueManager");
                queueMgr.AddComponent<TrainingQueueManager>();
                ApexLogger.Log("Created TrainingQueueManager", ApexLogger.LogCategory.Combat);
            }

            // Create troop manager (persistent)
            if (TroopManager.Instance == null)
            {
                GameObject troopMgr = new GameObject("TroopManager");
                troopMgr.AddComponent<TroopManager>();
                ApexLogger.Log("Created TroopManager", ApexLogger.LogCategory.Combat);
            }

            // Create training panel
            if (TroopTrainingPanel.Instance == null)
            {
                GameObject panelObj = new GameObject("TroopTrainingPanelRoot");
                panelObj.AddComponent<TroopTrainingPanel>();
                ApexLogger.Log("Created TroopTrainingPanel", ApexLogger.LogCategory.UI);
            }

            // Create quick bar
            if (TroopQuickBar.Instance == null)
            {
                GameObject quickBarObj = new GameObject("TroopQuickBarRoot");
                quickBarObj.AddComponent<TroopQuickBar>();
                ApexLogger.Log("Created TroopQuickBar", ApexLogger.LogCategory.UI);
            }

            ApexLogger.Log("Troop system setup complete!", ApexLogger.LogCategory.General);
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
                ApexLogger.Log("Troop Training System already exists in scene!", ApexLogger.LogCategory.General);
                return;
            }

            GameObject integrationObj = new GameObject("TroopTrainingIntegration");
            integrationObj.AddComponent<TroopTrainingIntegration>();

            UnityEditor.Selection.activeGameObject = integrationObj;
            ApexLogger.Log("Troop Training System created!", ApexLogger.LogCategory.General);
        }
        #endif
    }
}
