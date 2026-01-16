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
            // Only run in the PersistentCubeDemo scene
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "PersistentCubeDemo")
                return;

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
            }

            // Ensure EventSystem component exists
            if (eventSystem.GetComponent<EventSystem>() == null)
            {
                eventSystem.AddComponent<EventSystem>();
            }

            // Remove old StandaloneInputModule if present
            var oldModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (oldModule != null)
            {
                Object.Destroy(oldModule);
                Debug.Log("[DemoAutoSetup] Removed old StandaloneInputModule");
            }

            // Add new Input System UI module
            #if ENABLE_INPUT_SYSTEM
            var newModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (newModule == null)
            {
                eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Debug.Log("[DemoAutoSetup] Added InputSystemUIInputModule for new Input System");
            }
            #else
            // Fallback for old input system
            if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystem.AddComponent<StandaloneInputModule>();
            }
            #endif
        }
    }
}
