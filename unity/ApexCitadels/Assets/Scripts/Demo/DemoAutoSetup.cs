using UnityEngine;
using UnityEngine.EventSystems;
using ApexCitadels.Core;

namespace ApexCitadels.Demo
{
    /// <summary>
    /// Automatically sets up the demo scene with required components.
    /// Attach this to any GameObject in the scene, or it will auto-run via RuntimeInitializeOnLoadMethod.
    /// </summary>
    public class DemoAutoSetup : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoSetup()
        {
            var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            ApexLogger.Log($"Current scene: '{sceneName}'", ApexLogger.LogCategory.General);
            
            // Run in any scene that contains "Persistent" or "Demo" (case insensitive)
            if (!sceneName.ToLower().Contains("persistent") && !sceneName.ToLower().Contains("demo"))
            {
                ApexLogger.LogVerbose("Not a demo scene, skipping setup", ApexLogger.LogCategory.General);
                return;
            }

            ApexLogger.Log("Setting up demo scene...", ApexLogger.LogCategory.General);

            // Fix EventSystem for new Input System
            FixEventSystem();

            // Find or create Managers object
            var managersGO = GameObject.Find("Managers");
            if (managersGO == null)
            {
                managersGO = new GameObject("Managers");
            }

            // Add PersistentCubeDemo if missing
            if (managersGO.GetComponent<PersistentCubeDemo>() == null)
            {
                managersGO.AddComponent<PersistentCubeDemo>();
                ApexLogger.LogVerbose("Added PersistentCubeDemo component", ApexLogger.LogCategory.General);
            }

            // Add SpatialAnchorManager if missing
            var samGO = GameObject.Find("SpatialAnchorManager");
            if (samGO == null)
            {
                samGO = new GameObject("SpatialAnchorManager");
                samGO.transform.SetParent(managersGO.transform);
            }
            if (samGO.GetComponent<AR.SpatialAnchorManager>() == null)
            {
                samGO.AddComponent<AR.SpatialAnchorManager>();
                ApexLogger.LogVerbose("Added SpatialAnchorManager component", ApexLogger.LogCategory.General);
            }

            // Add AnchorPersistenceService if missing
            var apsGO = GameObject.Find("AnchorPersistenceService");
            if (apsGO == null)
            {
                apsGO = new GameObject("AnchorPersistenceService");
                apsGO.transform.SetParent(managersGO.transform);
            }
            if (apsGO.GetComponent<Backend.AnchorPersistenceService>() == null)
            {
                apsGO.AddComponent<Backend.AnchorPersistenceService>();
                ApexLogger.LogVerbose("Added AnchorPersistenceService component", ApexLogger.LogCategory.General);
            }

            ApexLogger.Log("Demo scene setup complete!", ApexLogger.LogCategory.General);
        }

        private static void FixEventSystem()
        {
            var eventSystem = GameObject.Find("EventSystem");
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                ApexLogger.LogVerbose("Created EventSystem", ApexLogger.LogCategory.General);
            }

            // Ensure EventSystem component exists
            if (eventSystem.GetComponent<EventSystem>() == null)
            {
                eventSystem.AddComponent<EventSystem>();
            }

            // Try to add the new Input System module (Unity 6 default)
            try
            {
                var moduleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (moduleType != null)
                {
                    var existingModule = eventSystem.GetComponent(moduleType);
                    if (existingModule == null)
                    {
                        eventSystem.AddComponent(moduleType);
                        ApexLogger.LogVerbose("Added InputSystemUIInputModule", ApexLogger.LogCategory.General);
                    }
                }
                else
                {
                    // Fallback to StandaloneInputModule
                    if (eventSystem.GetComponent<StandaloneInputModule>() == null)
                    {
                        eventSystem.AddComponent<StandaloneInputModule>();
                        ApexLogger.LogVerbose("Added StandaloneInputModule (fallback)", ApexLogger.LogCategory.General);
                    }
                }
            }
            catch (System.Exception e)
            {
                ApexLogger.LogWarning($"Could not add input module: {e.Message}", ApexLogger.LogCategory.General);
                // Fallback
                if (eventSystem.GetComponent<StandaloneInputModule>() == null)
                {
                    eventSystem.AddComponent<StandaloneInputModule>();
                }
            }
        }
    }
}
