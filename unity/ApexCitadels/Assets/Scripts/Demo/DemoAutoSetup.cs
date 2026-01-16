using UnityEngine;

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
    }
}
