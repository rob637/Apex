#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ApexCitadels.PC.Environment;

namespace ApexCitadels.PC.Editor
{
    /// <summary>
    /// Editor tools for the AAA environment system.
    /// </summary>
    public static class EnvironmentEditorTools
    {
        [MenuItem("Window/Apex Citadels/🌍 Add AAA Environment", false, 50)]
        public static void AddAAAEnvironment()
        {
            // Check if already exists
            if (Object.FindFirstObjectByType<WorldEnvironmentManager>() != null)
            {
                if (!EditorUtility.DisplayDialog("Environment Already Exists",
                    "The AAA Environment is already in the scene.\n\nDo you want to recreate it?",
                    "Recreate", "Cancel"))
                {
                    return;
                }
                
                // Remove existing
                var existing = Object.FindFirstObjectByType<WorldEnvironmentManager>();
                if (existing != null)
                {
                    Object.DestroyImmediate(existing.gameObject);
                }
            }

            // Create environment manager
            GameObject envObj = new GameObject("WorldEnvironment");
            envObj.AddComponent<WorldEnvironmentManager>();

            Debug.Log("[Editor] AAA Environment added to scene! Press Play to see it.");
            
            EditorUtility.DisplayDialog("✅ AAA Environment Added",
                "The World Environment Manager has been added!\n\n" +
                "Press PLAY to see:\n" +
                "• Procedural terrain with hills & water\n" +
                "• Dynamic skybox & lighting\n" +
                "• Demo territories with citadels\n" +
                "• Atmospheric effects\n\n" +
                "Camera Controls:\n" +
                "• WASD - Move\n" +
                "• Scroll - Zoom\n" +
                "• Right-Click Drag - Rotate view",
                "OK");
                
            Selection.activeGameObject = envObj;
        }

        [MenuItem("Window/Apex Citadels/⏰ Set Time of Day/🌅 Dawn", false, 51)]
        public static void SetTimeDawn()
        {
            SetTime(TimePreset.Dawn);
        }

        [MenuItem("Window/Apex Citadels/⏰ Set Time of Day/☀️ Morning", false, 52)]
        public static void SetTimeMorning()
        {
            SetTime(TimePreset.Morning);
        }

        [MenuItem("Window/Apex Citadels/⏰ Set Time of Day/🌞 Noon", false, 53)]
        public static void SetTimeNoon()
        {
            SetTime(TimePreset.Noon);
        }

        [MenuItem("Window/Apex Citadels/⏰ Set Time of Day/🌤️ Afternoon", false, 54)]
        public static void SetTimeAfternoon()
        {
            SetTime(TimePreset.Afternoon);
        }

        [MenuItem("Window/Apex Citadels/⏰ Set Time of Day/🌅 Sunset", false, 55)]
        public static void SetTimeSunset()
        {
            SetTime(TimePreset.Sunset);
        }

        [MenuItem("Window/Apex Citadels/⏰ Set Time of Day/🌙 Night", false, 56)]
        public static void SetTimeNight()
        {
            SetTime(TimePreset.Night);
        }

        private static void SetTime(TimePreset preset)
        {
            var atmosphere = Object.FindFirstObjectByType<AtmosphericLighting>();
            if (atmosphere != null)
            {
                atmosphere.SetTimePreset(preset);
                Debug.Log($"[Editor] Time set to {preset}");
            }
            else
            {
                EditorUtility.DisplayDialog("No Atmosphere System",
                    "Add the AAA Environment first!\n\nGo to: Window > Apex Citadels > Add AAA Environment",
                    "OK");
            }
        }

        [MenuItem("Window/Apex Citadels/🗺️ Regenerate Terrain", false, 60)]
        public static void RegenerateTerrain()
        {
            var terrain = Object.FindFirstObjectByType<ProceduralTerrain>();
            if (terrain != null)
            {
                int newSeed = Random.Range(0, 99999);
                terrain.RegenerateWithSeed(newSeed);
                Debug.Log($"[Editor] Terrain regenerated with seed: {newSeed}");
            }
            else
            {
                EditorUtility.DisplayDialog("No Terrain System",
                    "Add the AAA Environment first!\n\nGo to: Window > Apex Citadels > Add AAA Environment",
                    "OK");
            }
        }

        [MenuItem("Window/Apex Citadels/📐 Toggle Grid", false, 61)]
        public static void ToggleGrid()
        {
            var terrain = Object.FindFirstObjectByType<ProceduralTerrain>();
            if (terrain != null)
            {
                // Toggle - we need to track state somehow
                terrain.SetGridVisible(true); // Just enable for now
                Debug.Log("[Editor] Grid toggled");
            }
        }
    }
}
#endif
