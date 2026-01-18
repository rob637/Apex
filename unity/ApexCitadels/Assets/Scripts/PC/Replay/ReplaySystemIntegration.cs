using UnityEngine;
using System;
using System.Collections.Generic;
using ApexCitadels.PC.Combat;

namespace ApexCitadels.PC.Replay
{
    /// <summary>
    /// Replay System Integration - Connects all replay components
    /// Bridges CombatPanel → BattleRecorder → BattleReplaySystem → UI
    /// Provides unified API and keyboard shortcuts.
    /// </summary>
    public class ReplaySystemIntegration : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BattleRecorder recorder;
        [SerializeField] private BattleReplaySystem replaySystem;
        [SerializeField] private ReplayCameraController cameraController;
        [SerializeField] private ReplayHighlightSystem highlightSystem;
        [SerializeField] private ReplayTimelineEnhanced timeline;
        
        [Header("Keyboard Shortcuts")]
        [SerializeField] private KeyCode openReplayBrowserKey = KeyCode.F8;
        [SerializeField] private KeyCode togglePlayPauseKey = KeyCode.Space;
        [SerializeField] private KeyCode restartKey = KeyCode.R;
        [SerializeField] private KeyCode slowMotionKey = KeyCode.Z;
        [SerializeField] private KeyCode fastForwardKey = KeyCode.X;
        [SerializeField] private KeyCode normalSpeedKey = KeyCode.C;
        [SerializeField] private KeyCode toggleCinematicKey = KeyCode.V;
        [SerializeField] private KeyCode screenshotKey = KeyCode.F12;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugKeys = true;
        [SerializeField] private KeyCode debugRecordKey = KeyCode.F9;
        [SerializeField] private KeyCode debugStopKey = KeyCode.F10;
        
        // Singleton
        private static ReplaySystemIntegration _instance;
        public static ReplaySystemIntegration Instance => _instance;
        
        // State
        private bool _isReplayViewOpen;
        private float _lastScreenshotTime;
        
        // Events
        public event Action OnReplayViewOpened;
        public event Action OnReplayViewClosed;
        public event Action<string> OnScreenshotTaken;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            // Find or create components
            EnsureComponents();
        }
        
        private void Start()
        {
            // Subscribe to combat events
            SubscribeToCombatPanel();
        }
        
        private void Update()
        {
            HandleKeyboardShortcuts();
        }
        
        #region Component Setup
        
        private void EnsureComponents()
        {
            if (recorder == null)
            {
                recorder = GetComponentInChildren<BattleRecorder>();
                if (recorder == null)
                {
                    var obj = new GameObject("BattleRecorder");
                    obj.transform.SetParent(transform);
                    recorder = obj.AddComponent<BattleRecorder>();
                }
            }
            
            if (replaySystem == null)
            {
                replaySystem = FindFirstObjectByType<BattleReplaySystem>();
            }
            
            if (cameraController == null)
            {
                cameraController = GetComponentInChildren<ReplayCameraController>();
            }
            
            if (highlightSystem == null)
            {
                highlightSystem = GetComponentInChildren<ReplayHighlightSystem>();
                if (highlightSystem == null)
                {
                    var obj = new GameObject("HighlightSystem");
                    obj.transform.SetParent(transform);
                    highlightSystem = obj.AddComponent<ReplayHighlightSystem>();
                }
            }
            
            if (timeline == null)
            {
                timeline = GetComponentInChildren<ReplayTimelineEnhanced>();
            }
        }
        
        private void SubscribeToCombatPanel()
        {
            // Try to connect to CombatPanel for automatic recording
            var combatPanel = FindFirstObjectByType<UI.CombatPanel>();
            if (combatPanel != null)
            {
                combatPanel.OnAttackStarted += OnCombatStarted;
                combatPanel.OnBattleCompleted += OnCombatCompleted;
                Debug.Log("[ReplayIntegration] Connected to CombatPanel");
            }
        }
        
        #endregion
        
        #region Combat Integration
        
        private void OnCombatStarted(ApexCitadels.Territory.Territory territory)
        {
            if (recorder == null || territory == null) return;
            
            // Create battle context
            var context = new BattleContext
            {
                TerritoryId = territory.Id,
                TerritoryName = territory.Name ?? $"Territory {territory.Id}",
                AttackerId = GetCurrentPlayerId(),
                AttackerName = GetCurrentPlayerName(),
                DefenderId = territory.OwnerId ?? "neutral",
                DefenderName = territory.OwnerName ?? "Neutral",
                DefenderBuildings = GenerateMockBuildings(territory) // Would be real data in production
            };
            
            recorder.StartRecording(context);
            Debug.Log($"[ReplayIntegration] Started recording attack on {context.TerritoryName}");
        }
        
        private async void OnCombatCompleted(ApexCitadels.Territory.Territory territory, UI.BattleResult result)
        {
            if (recorder == null || !recorder.IsRecording) return;
            
            string replayId = await recorder.StopRecording(result.Victory);
            Debug.Log($"[ReplayIntegration] Recorded battle: {replayId}, Victory: {result.Victory}");
        }
        
        private string GetCurrentPlayerId()
        {
#if FIREBASE_ENABLED
            return Firebase.Auth.FirebaseAuth.DefaultInstance?.CurrentUser?.UserId ?? "local_player";
#else
            return "local_player";
#endif
        }
        
        private string GetCurrentPlayerName()
        {
#if FIREBASE_ENABLED
            return Firebase.Auth.FirebaseAuth.DefaultInstance?.CurrentUser?.DisplayName ?? "Player";
#else
            return "Player";
#endif
        }
        
        private List<BuildingInfo> GenerateMockBuildings(ApexCitadels.Territory.Territory territory)
        {
            // In production, this would come from actual territory data
            var buildings = new List<BuildingInfo>();
            
            int defenseLevel = territory.Level;
            
            // Citadel
            buildings.Add(new BuildingInfo
            {
                Id = $"{territory.Id}_citadel",
                Type = "citadel",
                Position = Vector3.zero,
                Rotation = Quaternion.identity,
                Scale = 2f,
                Health = 500 + defenseLevel * 100,
                MaxHealth = 500 + defenseLevel * 100
            });
            
            // Walls
            for (int i = 0; i < 4 + defenseLevel; i++)
            {
                float angle = (i / (4f + defenseLevel)) * Mathf.PI * 2;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * 15, 0, Mathf.Sin(angle) * 15);
                
                buildings.Add(new BuildingInfo
                {
                    Id = $"{territory.Id}_wall_{i}",
                    Type = "wall",
                    Position = pos,
                    Rotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0),
                    Scale = 1f,
                    Health = 100 + defenseLevel * 20,
                    MaxHealth = 100 + defenseLevel * 20
                });
            }
            
            // Towers
            for (int i = 0; i < defenseLevel; i++)
            {
                float angle = (i / (float)defenseLevel) * Mathf.PI * 2;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * 10, 0, Mathf.Sin(angle) * 10);
                
                buildings.Add(new BuildingInfo
                {
                    Id = $"{territory.Id}_tower_{i}",
                    Type = "tower",
                    Position = pos,
                    Rotation = Quaternion.identity,
                    Scale = 1.5f,
                    Health = 200 + defenseLevel * 30,
                    MaxHealth = 200 + defenseLevel * 30
                });
            }
            
            return buildings;
        }
        
        #endregion
        
        #region Keyboard Shortcuts
        
        private void HandleKeyboardShortcuts()
        {
            // Open replay browser
            if (Input.GetKeyDown(openReplayBrowserKey))
            {
                ToggleReplayBrowser();
            }
            
            // Only handle playback shortcuts when in replay mode
            if (_isReplayViewOpen && replaySystem != null)
            {
                // Play/Pause
                if (Input.GetKeyDown(togglePlayPauseKey))
                {
                    replaySystem.TogglePlayPause();
                }
                
                // Restart
                if (Input.GetKeyDown(restartKey))
                {
                    replaySystem.Restart();
                    highlightSystem?.ClearHighlights();
                }
                
                // Speed controls
                if (Input.GetKeyDown(slowMotionKey))
                {
                    replaySystem.SlowMotion();
                    timeline?.UpdateSpeedDisplay(0.25f);
                }
                
                if (Input.GetKeyDown(fastForwardKey))
                {
                    replaySystem.FastForward();
                    timeline?.UpdateSpeedDisplay(4f);
                }
                
                if (Input.GetKeyDown(normalSpeedKey))
                {
                    replaySystem.NormalSpeed();
                    timeline?.UpdateSpeedDisplay(1f);
                }
                
                // Cinematic mode
                if (Input.GetKeyDown(toggleCinematicKey))
                {
                    if (cameraController != null)
                    {
                        if (cameraController.CurrentMode == CameraMode.Cinematic)
                        {
                            cameraController.SetMode(CameraMode.FreeLook);
                        }
                        else
                        {
                            cameraController.StartCinematicMode();
                        }
                    }
                }
                
                // Screenshot
                if (Input.GetKeyDown(screenshotKey) && Time.time - _lastScreenshotTime > 0.5f)
                {
                    TakeScreenshot();
                    _lastScreenshotTime = Time.time;
                }
            }
            
            // Debug keys
            if (enableDebugKeys)
            {
                HandleDebugKeys();
            }
        }
        
        private void HandleDebugKeys()
        {
            // Start debug recording
            if (Input.GetKeyDown(debugRecordKey))
            {
                if (recorder != null && !recorder.IsRecording)
                {
                    var context = new BattleContext
                    {
                        TerritoryId = "debug_territory",
                        TerritoryName = "Debug Territory",
                        AttackerId = "debug_player",
                        AttackerName = "Debug Player",
                        DefenderId = "debug_enemy",
                        DefenderName = "Debug Enemy",
                        DefenderBuildings = new List<BuildingInfo>()
                    };
                    
                    recorder.StartRecording(context);
                    Debug.Log("[ReplayIntegration] DEBUG: Started recording");
                }
            }
            
            // Stop debug recording
            if (Input.GetKeyDown(debugStopKey))
            {
                if (recorder != null && recorder.IsRecording)
                {
                    _ = recorder.StopRecording(true);
                    Debug.Log("[ReplayIntegration] DEBUG: Stopped recording");
                }
            }
            
            // Simulate events during recording
            if (recorder != null && recorder.IsRecording)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    recorder.RecordUnitSpawned(Guid.NewGuid().ToString(), "Infantry", 
                        UnityEngine.Random.insideUnitSphere * 10f, true, 100);
                }
                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    recorder.RecordUnitAttack("unit1", "building1", UnityEngine.Random.Range(50f, 150f),
                        Vector3.zero, UnityEngine.Random.value < 0.2f);
                }
                if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    recorder.RecordBuildingDestroyed("building1", Vector3.zero, "unit1");
                }
            }
        }
        
        #endregion
        
        #region Replay Browser
        
        /// <summary>
        /// Toggle replay browser visibility
        /// </summary>
        public void ToggleReplayBrowser()
        {
            if (_isReplayViewOpen)
            {
                CloseReplayBrowser();
            }
            else
            {
                OpenReplayBrowser();
            }
        }
        
        /// <summary>
        /// Open replay browser
        /// </summary>
        public void OpenReplayBrowser()
        {
            _isReplayViewOpen = true;
            
            // Show replay UI panel
            var replayPanel = FindFirstObjectByType<UI.BattleReplayPanel>();
            if (replayPanel != null)
            {
                replayPanel.Show();
            }
            
            OnReplayViewOpened?.Invoke();
            Debug.Log("[ReplayIntegration] Opened replay browser");
        }
        
        /// <summary>
        /// Close replay browser
        /// </summary>
        public void CloseReplayBrowser()
        {
            _isReplayViewOpen = false;
            
            // Stop any active replay
            replaySystem?.Stop();
            
            // Hide replay UI panel
            var replayPanel = FindFirstObjectByType<UI.BattleReplayPanel>();
            if (replayPanel != null)
            {
                replayPanel.Hide();
            }
            
            // Reset time scale
            Time.timeScale = 1f;
            
            OnReplayViewClosed?.Invoke();
            Debug.Log("[ReplayIntegration] Closed replay browser");
        }
        
        /// <summary>
        /// Load and play a specific replay
        /// </summary>
        public async void PlayReplay(string replayId)
        {
            if (replaySystem == null) return;
            
            bool loaded = await replaySystem.LoadReplay(replayId);
            if (loaded)
            {
                _isReplayViewOpen = true;
                replaySystem.Play();
                
                // Reset highlight tracking
                highlightSystem?.ClearHighlights();
                
                OnReplayViewOpened?.Invoke();
            }
        }
        
        /// <summary>
        /// Play a locally saved replay
        /// </summary>
        public void PlayLocalReplay(string replayId)
        {
            if (recorder == null) return;
            
            var session = recorder.LoadLocalReplay(replayId);
            if (session != null)
            {
                // Convert RecordingSession to BattleReplay and load
                // This would require additional conversion logic
                Debug.Log($"[ReplayIntegration] Loaded local replay: {replayId}");
            }
        }
        
        #endregion
        
        #region Screenshot
        
        /// <summary>
        /// Take a screenshot of the replay
        /// </summary>
        public void TakeScreenshot()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filename = $"BattleReplay_{timestamp}.png";
            
            string path = System.IO.Path.Combine(Application.persistentDataPath, "Screenshots", filename);
            
            // Ensure directory exists
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            
            ScreenCapture.CaptureScreenshot(path);
            
            Debug.Log($"[ReplayIntegration] Screenshot saved: {path}");
            OnScreenshotTaken?.Invoke(path);
            
            // Show feedback
            highlightSystem?.ShowHighlightText("📷 Screenshot Saved!", Color.white);
        }
        
        #endregion
        
        #region API
        
        /// <summary>
        /// Check if a replay is currently active
        /// </summary>
        public bool IsReplayActive => _isReplayViewOpen && replaySystem != null;
        
        /// <summary>
        /// Check if recording is in progress
        /// </summary>
        public bool IsRecording => recorder?.IsRecording ?? false;
        
        /// <summary>
        /// Get current replay time
        /// </summary>
        public float CurrentReplayTime => replaySystem?.ReplayTime ?? 0f;
        
        /// <summary>
        /// Get replay duration
        /// </summary>
        public float ReplayDuration => replaySystem?.CurrentReplayDuration ?? 0f;
        
        /// <summary>
        /// Get list of local replay IDs
        /// </summary>
        public List<string> GetLocalReplays()
        {
            return recorder?.GetLocalReplayIds() ?? new List<string>();
        }
        
        /// <summary>
        /// Seek to time in current replay
        /// </summary>
        public void SeekTo(float time)
        {
            replaySystem?.SeekTo(time);
        }
        
        /// <summary>
        /// Set playback speed
        /// </summary>
        public void SetPlaybackSpeed(float speed)
        {
            if (replaySystem == null) return;
            
            if (speed < 0.5f)
                replaySystem.SlowMotion();
            else if (speed > 2f)
                replaySystem.FastForward();
            else
                replaySystem.NormalSpeed();
            
            timeline?.UpdateSpeedDisplay(speed);
        }
        
        /// <summary>
        /// Set camera mode
        /// </summary>
        public void SetCameraMode(CameraMode mode)
        {
            cameraController?.SetMode(mode);
        }
        
        /// <summary>
        /// Follow a unit
        /// </summary>
        public void FollowUnit(Transform unit)
        {
            if (cameraController == null) return;
            
            cameraController.SetFollowTarget(unit);
            cameraController.SetMode(CameraMode.Follow);
        }
        
        #endregion
    }
    
    #region Extension Classes
    
    /// <summary>
    /// Extensions to BattleReplaySystem for integration
    /// </summary>
    public static class BattleReplaySystemExtensions
    {
        public static float ReplayTime(this BattleReplaySystem system)
        {
            // Access internal replay time through reflection or public property
            // This is a placeholder - real implementation would need the property exposed
            return 0f;
        }
        
        public static float CurrentReplayDuration(this BattleReplaySystem system)
        {
            return 0f;
        }
        
        public static bool IsPlaying(this BattleReplaySystem system)
        {
            return false;
        }
    }
    
    #endregion
}
