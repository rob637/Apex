using UnityEngine;
using UnityEditor;
using ApexCitadels.PC.UI;

namespace ApexCitadels.PC.Editor
{
    public class PCAutoWirer
    {
        [MenuItem("Apex/PC/Auto-Wire Scene References")]
        public static void WireScene()
        {
            Debug.Log("--- Starting Auto-Wire ---");

            // 1. Setup World Map Renderer
            var worldMap = Object.FindFirstObjectByType<WorldMapRenderer>();
            if (worldMap != null)
            {
                // Assign Prefabs
                AssignAsset(ref worldMap, "territoryMarkerPrefab", "Assets/Prefabs/PC/TerritoryMarker.prefab");
                AssignAsset(ref worldMap, "playerMarkerPrefab", "Assets/Prefabs/PC/MinimapMarker.prefab"); // Using minimap marker as placeholder if needed

                // Assign Materials
                AssignMaterial(ref worldMap, "ownedTerritoryMaterial", "TerritoryOwned");
                AssignMaterial(ref worldMap, "enemyTerritoryMaterial", "TerritoryEnemy");
                AssignMaterial(ref worldMap, "allianceTerritoryMaterial", "TerritoryAllied");
                AssignMaterial(ref worldMap, "neutralTerritoryMaterial", "TerritoryNeutral");
                AssignMaterial(ref worldMap, "contestedTerritoryMaterial", "TerritoryContested");
                AssignMaterial(ref worldMap, "groundMaterial", "GridLine"); // Placeholder

                EditorUtility.SetDirty(worldMap);
                Debug.Log("[AutoWire] Configured WorldMapRenderer");
            }
            else
            {
                Debug.LogError("[AutoWire] Could not find WorldMapRenderer in scene!");
            }

            // 2. Setup UI Manager
            var uiManager = Object.FindFirstObjectByType<PCUIManager>();
            if (uiManager != null)
            {
                // Assign Generic Prefabs if slots are empty, 
                // but PCUIManager has a "CreateMissingPanels" function that runs at runtime,
                // so we mainly need to ensure it exists.
                Debug.Log("[AutoWire] PCUIManager found. It will auto-generate panels on Play.");
            }

            Debug.Log("--- Auto-Wire Complete ---");
        }

        private static void AssignAsset(ref WorldMapRenderer target, string fieldName, string updatePath)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            
            if (prop != null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(updatePath);
                if (prefab != null)
                {
                    prop.objectReferenceValue = prefab;
                    so.ApplyModifiedProperties();
                }
                else
                {
                    Debug.LogWarning($"[AutoWire] Could not find prefab at {updatePath}");
                }
            }
        }

        private static void AssignMaterial(ref WorldMapRenderer target, string fieldName, string matName)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            
            if (prop != null)
            {
                string path = $"Assets/Materials/PC/{matName}.mat";
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                {
                    prop.objectReferenceValue = mat;
                    so.ApplyModifiedProperties();
                }
                else
                {
                    Debug.LogWarning($"[AutoWire] Could not find material at {path}");
                }
            }
        }
    }
}
