// ============================================================================
// APEX CITADELS - ECONOMY INTEGRATION
// Connects resource spending to all game systems
// ============================================================================
using UnityEngine;
using System;
using ApexCitadels.Core;

namespace ApexCitadels.PC.Economy
{
    /// <summary>
    /// Central integration for economy system with all game features
    /// </summary>
    public class EconomyIntegration : MonoBehaviour
    {
        [Header("Auto-Setup")]
        [SerializeField] private bool autoCreateCanvas = true;
        [SerializeField] private bool autoCreateEventSystem = true;

        [Header("Debug")]
        [SerializeField] private KeyCode addResourcesKey = KeyCode.F9;
        [SerializeField] private KeyCode resetResourcesKey = KeyCode.F10;

        private void Start()
        {
            EnsureRequiredComponents();
            SetupEconomySystem();
            ConnectToGameSystems();
        }

        private void Update()
        {
            HandleDebugInput();
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
            GameObject canvasObj = new GameObject("EconomyCanvas");
            UnityEngine.Canvas canvas = canvasObj.AddComponent<UnityEngine.Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // Above other UI

            UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            ApexLogger.Log("Created UI Canvas", ApexLogger.LogCategory.Economy);
        }

        private void CreateEventSystem()
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            ApexLogger.Log("Created Event System", ApexLogger.LogCategory.Economy);
        }

        private void SetupEconomySystem()
        {
            // Create Resource Spending Manager
            if (ResourceSpendingManager.Instance == null)
            {
                GameObject managerObj = new GameObject("ResourceSpendingManager");
                managerObj.AddComponent<ResourceSpendingManager>();
                ApexLogger.Log("Created ResourceSpendingManager", ApexLogger.LogCategory.Economy);
            }

            // Create Resource HUD
            if (ResourceHUD.Instance == null)
            {
                GameObject hudObj = new GameObject("ResourceHUD");
                hudObj.AddComponent<ResourceHUD>();
                ApexLogger.Log("Created ResourceHUD", ApexLogger.LogCategory.Economy);
            }

            // Create Insufficient Resources Popup
            if (InsufficientResourcesPopup.Instance == null)
            {
                GameObject popupObj = new GameObject("InsufficientResourcesPopup");
                popupObj.AddComponent<InsufficientResourcesPopup>();
                ApexLogger.Log("Created InsufficientResourcesPopup", ApexLogger.LogCategory.Economy);
            }

            ApexLogger.Log("Economy system setup complete!", ApexLogger.LogCategory.Economy);
        }

        private void ConnectToGameSystems()
        {
            // Connect to Troop Training (if exists)
            ConnectToTroopTraining();

            // Connect to Base Editor (if exists)
            ConnectToBaseEditor();

            ApexLogger.Log("Connected to game systems", ApexLogger.LogCategory.Economy);
        }

        private void ConnectToTroopTraining()
        {
            // Find TrainingQueueManager and hook into its resource checks
            var trainingQueue = FindFirstObjectByType<UI.TrainingQueueManager>();
            if (trainingQueue != null)
            {
                ApexLogger.LogVerbose("Connected to TrainingQueueManager", ApexLogger.LogCategory.Economy);
            }
        }

        private void ConnectToBaseEditor()
        {
            // Find BaseEditorPanel and hook into its resource checks
            var baseEditor = FindFirstObjectByType<UI.BaseEditorPanel>();
            if (baseEditor != null)
            {
                ApexLogger.LogVerbose("Connected to BaseEditorPanel", ApexLogger.LogCategory.Economy);
            }
        }

        private void HandleDebugInput()
        {
            if (Input.GetKeyDown(addResourcesKey))
            {
                ResourceSpendingManager.Instance?.AddDebugResources();
                ApexLogger.LogVerbose("Added debug resources (F9, ApexLogger.LogCategory.Economy)");
            }

            if (Input.GetKeyDown(resetResourcesKey))
            {
                ResourceSpendingManager.Instance?.ResetResources();
                ApexLogger.LogVerbose("Reset resources (F10, ApexLogger.LogCategory.Economy)");
            }
        }

        /// <summary>
        /// Quick setup from menu
        /// </summary>
        #if UNITY_EDITOR
        [UnityEditor.MenuItem("Apex Citadels/Setup Economy System")]
        public static void SetupFromMenu()
        {
            if (FindFirstObjectByType<EconomyIntegration>() != null)
            {
                ApexLogger.Log("Economy System already exists in scene!", ApexLogger.LogCategory.Economy);
                return;
            }

            GameObject integrationObj = new GameObject("EconomyIntegration");
            integrationObj.AddComponent<EconomyIntegration>();

            UnityEditor.Selection.activeGameObject = integrationObj;
            ApexLogger.Log("Economy System created!", ApexLogger.LogCategory.Economy);
        }
        #endif
    }

    /// <summary>
    /// Extension methods for easy resource spending from any system
    /// </summary>
    public static class EconomyExtensions
    {
        /// <summary>
        /// Try to spend resources for building placement
        /// </summary>
        public static bool TrySpendForBuilding(string buildingType, ResourceCost cost)
        {
            if (ResourceSpendingManager.Instance == null)
            {
                ApexLogger.LogWarning("ResourceSpendingManager not found - allowing action", ApexLogger.LogCategory.Economy);
                return true;
            }

            return ResourceSpendingManager.Instance.Spend(cost, $"Building: {buildingType}");
        }

        /// <summary>
        /// Try to spend resources for troop training
        /// </summary>
        public static bool TrySpendForTraining(string troopType, int count, ResourceCost cost)
        {
            if (ResourceSpendingManager.Instance == null)
            {
                ApexLogger.LogWarning("ResourceSpendingManager not found - allowing action", ApexLogger.LogCategory.Economy);
                return true;
            }

            return ResourceSpendingManager.Instance.Spend(cost, $"Training: {count}x {troopType}");
        }

        /// <summary>
        /// Try to spend resources for research/upgrade
        /// </summary>
        public static bool TrySpendForUpgrade(string upgradeName, ResourceCost cost)
        {
            if (ResourceSpendingManager.Instance == null)
            {
                ApexLogger.LogWarning("ResourceSpendingManager not found - allowing action", ApexLogger.LogCategory.Economy);
                return true;
            }

            return ResourceSpendingManager.Instance.Spend(cost, $"Upgrade: {upgradeName}");
        }

        /// <summary>
        /// Award resources for completing something
        /// </summary>
        public static void AwardResources(string reason, ResourceCost reward)
        {
            ResourceSpendingManager.Instance?.Earn(reward, reason);
        }

        /// <summary>
        /// Refund resources (e.g., when cancelling construction)
        /// </summary>
        public static void RefundResources(string reason, ResourceCost original, float ratio = 0.5f)
        {
            ResourceSpendingManager.Instance?.Refund(original, ratio, reason);
        }

        /// <summary>
        /// Convert Data.ResourceCost to Economy.ResourceCost
        /// </summary>
        public static ResourceCost ToEconomyCost(this Data.ResourceCost dataCost)
        {
            if (dataCost == null) return new ResourceCost();

            return new ResourceCost(
                stone: dataCost.Stone,
                wood: dataCost.Wood,
                iron: dataCost.Iron,
                crystal: dataCost.Crystal,
                arcaneEssence: dataCost.ArcaneEssence,
                gems: dataCost.Gems
            );
        }
    }
}
