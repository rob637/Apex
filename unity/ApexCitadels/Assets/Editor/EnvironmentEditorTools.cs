#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ApexCitadels.PC.Environment;
using ApexCitadels.Map;

namespace ApexCitadels.PC.Editor
{
    /// <summary>
    /// Editor tools for the AAA environment system.
    /// </summary>
    public static class EnvironmentEditorTools
    {
        [MenuItem("Apex Citadels/Environment/Add AAA Environment (Mapbox)", false, 59)]
        public static void AddAAAEnvironmentMapbox()
        {
            AddAAAEnvironmentWithMode(WorldEnvironmentManager.TerrainMode.Mapbox);
        }

        [MenuItem("Apex Citadels/Environment/Add AAA Environment (Procedural)", false, 60)]
        public static void AddAAAEnvironmentProcedural()
        {
            AddAAAEnvironmentWithMode(WorldEnvironmentManager.TerrainMode.Procedural);
        }

        private static void AddAAAEnvironmentWithMode(WorldEnvironmentManager.TerrainMode mode)
        {
            // Check if Mapbox is configured when using Mapbox mode
            if (mode == WorldEnvironmentManager.TerrainMode.Mapbox)
            {
                var config = UnityEngine.Resources.Load<MapboxConfiguration>("MapboxConfig");
                if (config == null || !config.IsValid)
                {
                    if (!EditorUtility.DisplayDialog("Mapbox Not Configured",
                        "Mapbox API is not configured yet.\n\n" +
                        "Would you like to set it up now?\n\n" +
                        "Go to: Apex Citadels > PC > Setup Mapbox (Auto)",
                        "Setup Now", "Use Procedural Instead"))
                    {
                        mode = WorldEnvironmentManager.TerrainMode.Procedural;
                    }
                    else
                    {
                        MapboxAutoSetup.CreateMapboxConfig();
                        return;
                    }
                }
            }

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
            var manager = envObj.AddComponent<WorldEnvironmentManager>();
            
            // Set terrain mode via SerializedObject
            var so = new SerializedObject(manager);
            var terrainModeProp = so.FindProperty("terrainMode");
            if (terrainModeProp != null)
            {
                terrainModeProp.enumValueIndex = (int)mode;
                so.ApplyModifiedProperties();
            }

            string terrainDescription = mode == WorldEnvironmentManager.TerrainMode.Mapbox
                ? "• Real-world Mapbox map tiles"
                : "• Procedural terrain with hills & water";

            Debug.Log($"[Editor] AAA Environment added with {mode} terrain mode!");
            
            EditorUtility.DisplayDialog($"✅ AAA Environment Added ({mode})",
                $"The World Environment Manager has been added!\n\n" +
                "Press PLAY to see:\n" +
                $"{terrainDescription}\n" +
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

        [MenuItem("Apex Citadels/Environment/Time of Day/Dawn", false, 61)]
        public static void SetTimeDawn()
        {
            SetTime(TimePreset.Dawn);
        }

        [MenuItem("Apex Citadels/Environment/Time of Day/Morning", false, 62)]
        public static void SetTimeMorning()
        {
            SetTime(TimePreset.Morning);
        }

        [MenuItem("Apex Citadels/Environment/Time of Day/Noon", false, 63)]
        public static void SetTimeNoon()
        {
            SetTime(TimePreset.Noon);
        }

        [MenuItem("Apex Citadels/Environment/Time of Day/Afternoon", false, 64)]
        public static void SetTimeAfternoon()
        {
            SetTime(TimePreset.Afternoon);
        }

        [MenuItem("Apex Citadels/Environment/Time of Day/Sunset", false, 65)]
        public static void SetTimeSunset()
        {
            SetTime(TimePreset.Sunset);
        }

        [MenuItem("Apex Citadels/Environment/Time of Day/Night", false, 66)]
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

        [MenuItem("Apex Citadels/Environment/Regenerate Terrain", false, 67)]
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
                // Check if environment manager exists but terrain hasn't spawned yet (not in play mode)
                var envManager = Object.FindFirstObjectByType<WorldEnvironmentManager>();
                if (envManager != null)
                {
                    EditorUtility.DisplayDialog("Enter Play Mode",
                        "The terrain is created at runtime.\n\nPress PLAY first, then use this menu option to regenerate terrain.",
                        "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("No Terrain System",
                        "Add the AAA Environment first!\n\nGo to: Apex Citadels > Environment > Add AAA Environment",
                        "OK");
                }
            }
        }

        [MenuItem("Apex Citadels/Environment/Toggle Grid", false, 68)]
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
