using UnityEngine;
using UnityEngine.EventSystems;

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
            Debug.Log($"[DemoAutoSetup] Current scene: '{sceneName}'");
            
            // Run in any scene that contains "Persistent" or "Demo" (case insensitive)
            if (!sceneName.ToLower().Contains("persistent") && !sceneName.ToLower().Contains("demo"))
            {
                Debug.Log("[DemoAutoSetup] Not a demo scene, skipping setup");
                return;
            }

            Debug.Log("[DemoAutoSetup] Setting up demo scene...");

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
                Debug.Log("[DemoAutoSetup] Added PersistentCubeDemo component");
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
                Debug.Log("[DemoAutoSetup] Added SpatialAnchorManager component");
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
                Debug.Log("[DemoAutoSetup] Added AnchorPersistenceService component");
            }

            Debug.Log("[DemoAutoSetup] Demo scene setup complete!");
        }

        private static void FixEventSystem()
        {
            var eventSystem = GameObject.Find("EventSystem");
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                Debug.Log("[DemoAutoSetup] Created EventSystem");
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
                        Debug.Log("[DemoAutoSetup] Added InputSystemUIInputModule");
                    }
                }
                else
                {
                    // Fallback to StandaloneInputModule
                    if (eventSystem.GetComponent<StandaloneInputModule>() == null)
                    {
                        eventSystem.AddComponent<StandaloneInputModule>();
                        Debug.Log("[DemoAutoSetup] Added StandaloneInputModule (fallback)");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[DemoAutoSetup] Could not add input module: {e.Message}");
                // Fallback
                if (eventSystem.GetComponent<StandaloneInputModule>() == null)
                {
                    eventSystem.AddComponent<StandaloneInputModule>();
                }
            }
        }
    }
}
