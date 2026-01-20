// ============================================================================
// APEX CITADELS - SYSTEM COORDINATOR EDITOR
// Editor window for managing game systems and diagnosing conflicts
// ============================================================================
using UnityEngine;
using UnityEditor;
using ApexCitadels.Core;
using System.Collections.Generic;
using System.Linq;

namespace ApexCitadels.Editor
{
    public class SystemCoordinatorWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _showSingletons = true;
        private bool _showTerrain = true;
        private bool _showCameras = true;
        private bool _showAtmosphere = true;
        private bool _showAudio = true;
        
        [MenuItem("Apex Citadels/System Coordinator", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<SystemCoordinatorWindow>("System Coordinator");
            window.minSize = new Vector2(400, 500);
        }
        
        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            DrawHeader();
            
            if (Application.isPlaying)
            {
                DrawRuntimeControls();
            }
            
            EditorGUILayout.Space(10);
            DrawSystemAudit();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(5);
            
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("⚙️ System Coordinator", headerStyle);
            EditorGUILayout.LabelField("Manages all game systems to prevent conflicts", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("🔄 Refresh", GUILayout.Height(25)))
            {
                Repaint();
            }
        }
        
        private void DrawRuntimeControls()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
            
            var coordinator = SystemCoordinator.Instance;
            if (coordinator == null)
            {
                EditorGUILayout.HelpBox("SystemCoordinator not found in scene", MessageType.Warning);
                return;
            }
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Current State", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Mode: {coordinator.CurrentMode}");
                EditorGUILayout.LabelField($"Terrain: {coordinator.CurrentTerrain}");
                EditorGUILayout.LabelField($"Camera: {coordinator.CurrentCamera}");
                EditorGUILayout.LabelField($"Atmosphere: {coordinator.CurrentAtmosphere}");
            }
            
            EditorGUILayout.Space(5);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("WorldMap Mode"))
                {
                    coordinator.SetGameMode(GameMode.WorldMap);
                }
                if (GUILayout.Button("Citadel Mode"))
                {
                    coordinator.SetGameMode(GameMode.CitadelView);
                }
                if (GUILayout.Button("Build Mode"))
                {
                    coordinator.SetGameMode(GameMode.BuildMode);
                }
            }
            
            if (GUILayout.Button("Log Current State"))
            {
                coordinator.LogCurrentState();
            }
        }
        
        private void DrawSystemAudit()
        {
            EditorGUILayout.LabelField("System Audit", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This shows all detected systems that could potentially conflict. " +
                "Red items indicate duplicates that should be consolidated.",
                MessageType.Info
            );
            
            // Singletons
            _showSingletons = EditorGUILayout.Foldout(_showSingletons, "🔄 Singletons", true);
            if (_showSingletons)
            {
                DrawSingletonAudit();
            }
            
            // Terrain
            _showTerrain = EditorGUILayout.Foldout(_showTerrain, "🏔️ Terrain Systems", true);
            if (_showTerrain)
            {
                DrawTerrainAudit();
            }
            
            // Cameras
            _showCameras = EditorGUILayout.Foldout(_showCameras, "📷 Camera Controllers", true);
            if (_showCameras)
            {
                DrawCameraAudit();
            }
            
            // Atmosphere
            _showAtmosphere = EditorGUILayout.Foldout(_showAtmosphere, "🌅 Atmosphere Systems", true);
            if (_showAtmosphere)
            {
                DrawAtmosphereAudit();
            }
            
            // Audio
            _showAudio = EditorGUILayout.Foldout(_showAudio, "🔊 Audio Systems", true);
            if (_showAudio)
            {
                DrawAudioAudit();
            }
        }
        
        private void DrawSingletonAudit()
        {
            using (new EditorGUI.IndentLevelScope())
            {
                var types = new[] {
                    typeof(SystemCoordinator),
                    typeof(Map.MapboxTileRenderer),
                    typeof(PC.WorldMapRenderer),
                    typeof(PC.PCCameraController),
                    typeof(PC.PCGameController),
                };
                
                foreach (var type in types)
                {
                    var instances = FindObjectsByType(type, FindObjectsSortMode.None);
                    var count = instances.Length;
                    
                    var color = count == 0 ? Color.gray : (count == 1 ? Color.green : Color.red);
                    var icon = count == 0 ? "⚪" : (count == 1 ? "✅" : "❌");
                    
                    GUI.color = color;
                    EditorGUILayout.LabelField($"{icon} {type.Name}: {count} instance(s)");
                    GUI.color = Color.white;
                }
            }
        }
        
        private void DrawTerrainAudit()
        {
            using (new EditorGUI.IndentLevelScope())
            {
                DrawSystemStatus<Map.MapboxTileRenderer>("MapboxTileRenderer");
                DrawSystemStatus<PC.Environment.ProceduralTerrain>("ProceduralTerrain");
                
                // Check for conflicting objects
                var conflicts = new[] { "GroundPlane", "WorldTerrain", "FantasyTerrain", "GridOverlay" };
                foreach (var name in conflicts)
                {
                    var obj = GameObject.Find(name);
                    if (obj != null)
                    {
                        GUI.color = Color.yellow;
                        EditorGUILayout.LabelField($"⚠️ Found: {name}");
                        GUI.color = Color.white;
                    }
                }
            }
        }
        
        private void DrawCameraAudit()
        {
            using (new EditorGUI.IndentLevelScope())
            {
                DrawSystemStatus<PC.PCCameraController>("PCCameraController");
                
                var mainCam = Camera.main;
                if (mainCam != null)
                {
                    EditorGUILayout.LabelField($"✅ Main Camera: {mainCam.name}");
                }
                else
                {
                    GUI.color = Color.red;
                    EditorGUILayout.LabelField("❌ No Main Camera!");
                    GUI.color = Color.white;
                }
            }
        }
        
        private void DrawAtmosphereAudit()
        {
            using (new EditorGUI.IndentLevelScope())
            {
                DrawSystemStatus<PC.Environment.AtmosphericLighting>("AtmosphericLighting");
                DrawSystemStatus<PC.Visual.SkyboxEnvironmentSystem>("SkyboxEnvironmentSystem");
                
                // Check skybox
                if (RenderSettings.skybox != null)
                {
                    EditorGUILayout.LabelField($"✅ Skybox: {RenderSettings.skybox.name}");
                }
                else
                {
                    EditorGUILayout.LabelField("⚪ No Skybox material");
                }
            }
        }
        
        private void DrawAudioAudit()
        {
            using (new EditorGUI.IndentLevelScope())
            {
                // Check for duplicate audio managers
                var audioManagerTypes = new[] {
                    "ApexCitadels.Audio.AudioManager",
                    "ApexCitadels.PC.Audio.AudioManager",
                    "ApexCitadels.PC.Visual.AudioManager"
                };
                
                foreach (var typeName in audioManagerTypes)
                {
                    var type = System.Type.GetType(typeName + ", Assembly-CSharp");
                    if (type != null)
                    {
                        var count = FindObjectsByType(type, FindObjectsSortMode.None).Length;
                        var icon = count == 0 ? "⚪" : (count == 1 ? "✅" : "❌");
                        GUI.color = count > 1 ? Color.red : Color.white;
                        EditorGUILayout.LabelField($"{icon} {typeName.Split('.').Last()}: {count}");
                        GUI.color = Color.white;
                    }
                }
            }
        }
        
        private void DrawSystemStatus<T>(string name) where T : MonoBehaviour
        {
            var instances = FindObjectsByType<T>(FindObjectsSortMode.None);
            var count = instances.Length;
            
            var icon = count == 0 ? "⚪" : (count == 1 ? "✅" : "❌");
            GUI.color = count > 1 ? Color.red : Color.white;
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"{icon} {name}: {count}", GUILayout.Width(250));
                
                if (count == 1 && instances[0] != null)
                {
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeGameObject = instances[0].gameObject;
                    }
                }
            }
            
            GUI.color = Color.white;
        }
    }
}
