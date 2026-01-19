using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApexCitadels.Core;
using ApexCitadels.Combat;

#if FIREBASE_ENABLED
using Firebase.Firestore;
#endif

namespace ApexCitadels.PC
{
    /// <summary>
    /// PC-exclusive Battle Replay System
    /// Records and plays back territory attacks for strategic analysis
    /// </summary>
    public class BattleReplaySystem : MonoBehaviour
    {
        [Header("Replay Settings")]
        [SerializeField] private float playbackSpeed = 1f;
        [SerializeField] private float fastForwardSpeed = 4f;
        [SerializeField] private float slowMotionSpeed = 0.25f;
        
        [Header("Visualization")]
        [SerializeField] private GameObject attackerUnitPrefab;
        [SerializeField] private GameObject defenderUnitPrefab;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject explosionPrefab;
        [SerializeField] private LineRenderer trajectoryLine;
        
        [Header("UI References")]
        [SerializeField] private TMPro.TMP_Text timelineText;
        [SerializeField] private UnityEngine.UI.Slider timelineSlider;
        [SerializeField] private UnityEngine.UI.Button playPauseButton;
        [SerializeField] private GameObject replayPanel;
        
        // Singleton
        private static BattleReplaySystem _instance;
        public static BattleReplaySystem Instance => _instance;
        
        // Current replay state
        private BattleReplay _currentReplay;
        private float _replayTime;
        private bool _isPlaying;
        private int _currentEventIndex;
        private float _currentPlaybackSpeed;
        
        // Public properties for timeline integration
        public float ReplayTime => _replayTime;
        public float TotalDuration => _currentReplay?.Duration ?? 0f;
        public bool IsPlaying => _isPlaying;
        public BattleReplay CurrentReplay => _currentReplay;
        
        // Replay visualization
        private Dictionary<string, GameObject> _replayUnits = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> _replayBuildings = new Dictionary<string, GameObject>();
        private List<GameObject> _activeEffects = new List<GameObject>();
        
        // Events
        public event Action<BattleReplay> OnReplayLoaded;
        public event Action OnReplayStarted;
        public event Action OnReplayPaused;
        public event Action OnReplayEnded;
        public event Action<BattleEvent> OnEventPlayed;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            _currentPlaybackSpeed = playbackSpeed;
        }
        
        private void Update()
        {
            if (_isPlaying && _currentReplay != null)
            {
                UpdateReplayPlayback();
            }
        }
        
        #region Replay Loading
        
        /// <summary>
        /// Load a battle replay from Firebase
        /// </summary>
        public async Task<bool> LoadReplay(string replayId)
        {
#if FIREBASE_ENABLED
            try
            {
                var db = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
                var doc = await db.Collection("battle_replays").Document(replayId).GetSnapshotAsync();
                
                if (!doc.Exists)
                {
                    Debug.LogWarning($"[BattleReplay] Replay not found: {replayId}");
                    return false;
                }
                
                _currentReplay = BattleReplay.FromFirestore(doc);
                
                if (_currentReplay != null)
                {
                    await SetupReplayScene();
                    OnReplayLoaded?.Invoke(_currentReplay);
                    Debug.Log($"[BattleReplay] Loaded replay: {_currentReplay.Id}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BattleReplay] Error loading replay: {ex.Message}");
            }
#endif
            return false;
        }
        
        /// <summary>
        /// Load replays for a specific territory
        /// </summary>
        public async Task<List<BattleReplaySummary>> GetTerritoryReplays(string territoryId, int limit = 20)
        {
            var summaries = new List<BattleReplaySummary>();
            
#if FIREBASE_ENABLED
            try
            {
                var db = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
                var query = db.Collection("battle_replays")
                    .WhereEqualTo("territoryId", territoryId)
                    .OrderByDescending("timestamp")
                    .Limit(limit);
                    
                var snapshot = await query.GetSnapshotAsync();
                
                foreach (var doc in snapshot.Documents)
                {
                    var summary = new BattleReplaySummary
                    {
                        Id = doc.Id,
                        TerritoryId = doc.GetValue<string>("territoryId"),
                        AttackerId = doc.GetValue<string>("attackerId"),
                        AttackerName = doc.GetValue<string>("attackerName"),
                        DefenderId = doc.GetValue<string>("defenderId"),
                        DefenderName = doc.GetValue<string>("defenderName"),
                        Timestamp = doc.GetValue<Firebase.Firestore.Timestamp>("timestamp").ToDateTime(),
                        AttackerWon = doc.GetValue<bool>("attackerWon"),
                        Duration = doc.GetValue<float>("duration"),
                        AttackerDamageDealt = doc.GetValue<int>("attackerDamageDealt"),
                        DefenderDamageDealt = doc.GetValue<int>("defenderDamageDealt")
                    };
                    summaries.Add(summary);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BattleReplay] Error getting territory replays: {ex.Message}");
            }
#endif
            
            return summaries;
        }
        
        /// <summary>
        /// Get replays where the player was involved
        /// </summary>
        public async Task<List<BattleReplaySummary>> GetPlayerReplays(string playerId, bool attacksOnly = false, bool defensesOnly = false, int limit = 50)
        {
            var summaries = new List<BattleReplaySummary>();
            
#if FIREBASE_ENABLED
            try
            {
                var db = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
                
                // Get attacks
                if (!defensesOnly)
                {
                    var attackQuery = db.Collection("battle_replays")
                        .WhereEqualTo("attackerId", playerId)
                        .OrderByDescending("timestamp")
                        .Limit(limit);
                        
                    var attackSnapshot = await attackQuery.GetSnapshotAsync();
                    foreach (var doc in attackSnapshot.Documents)
                    {
                        summaries.Add(ParseReplaySummary(doc, true));
                    }
                }
                
                // Get defenses
                if (!attacksOnly)
                {
                    var defenseQuery = db.Collection("battle_replays")
                        .WhereEqualTo("defenderId", playerId)
                        .OrderByDescending("timestamp")
                        .Limit(limit);
                        
                    var defenseSnapshot = await defenseQuery.GetSnapshotAsync();
                    foreach (var doc in defenseSnapshot.Documents)
                    {
                        summaries.Add(ParseReplaySummary(doc, false));
                    }
                }
                
                // Sort by timestamp
                summaries.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
                
                // Limit total results
                if (summaries.Count > limit)
                {
                    summaries = summaries.GetRange(0, limit);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BattleReplay] Error getting player replays: {ex.Message}");
            }
#endif
            
            return summaries;
        }
        
#if FIREBASE_ENABLED
        private BattleReplaySummary ParseReplaySummary(Firebase.Firestore.DocumentSnapshot doc, bool wasAttacker)
        {
            return new BattleReplaySummary
            {
                Id = doc.Id,
                TerritoryId = doc.GetValue<string>("territoryId"),
                AttackerId = doc.GetValue<string>("attackerId"),
                AttackerName = doc.GetValue<string>("attackerName"),
                DefenderId = doc.GetValue<string>("defenderId"),
                DefenderName = doc.GetValue<string>("defenderName"),
                Timestamp = doc.GetValue<Firebase.Firestore.Timestamp>("timestamp").ToDateTime(),
                AttackerWon = doc.GetValue<bool>("attackerWon"),
                Duration = doc.GetValue<float>("duration"),
                WasAttacker = wasAttacker
            };
        }
#endif
        
        #endregion
        
        #region Replay Playback
        
        /// <summary>
        /// Start or resume replay playback
        /// </summary>
        public void Play()
        {
            if (_currentReplay == null) return;
            
            _isPlaying = true;
            _currentPlaybackSpeed = playbackSpeed;
            OnReplayStarted?.Invoke();
            Debug.Log("[BattleReplay] Playing");
        }
        
        /// <summary>
        /// Pause replay playback
        /// </summary>
        public void Pause()
        {
            _isPlaying = false;
            OnReplayPaused?.Invoke();
            Debug.Log("[BattleReplay] Paused");
        }
        
        /// <summary>
        /// Toggle play/pause
        /// </summary>
        public void TogglePlayPause()
        {
            if (_isPlaying) Pause();
            else Play();
        }
        
        /// <summary>
        /// Fast forward playback
        /// </summary>
        public void FastForward()
        {
            _currentPlaybackSpeed = fastForwardSpeed;
            if (!_isPlaying) Play();
        }
        
        /// <summary>
        /// Slow motion playback
        /// </summary>
        public void SlowMotion()
        {
            _currentPlaybackSpeed = slowMotionSpeed;
            if (!_isPlaying) Play();
        }
        
        /// <summary>
        /// Set normal speed
        /// </summary>
        public void NormalSpeed()
        {
            _currentPlaybackSpeed = playbackSpeed;
        }
        
        /// <summary>
        /// Seek to a specific time in the replay
        /// </summary>
        public void SeekTo(float time)
        {
            if (_currentReplay == null) return;
            
            _replayTime = Mathf.Clamp(time, 0f, _currentReplay.Duration);
            
            // Find the event index at this time
            _currentEventIndex = 0;
            for (int i = 0; i < _currentReplay.Events.Count; i++)
            {
                if (_currentReplay.Events[i].Timestamp <= _replayTime)
                {
                    _currentEventIndex = i + 1;
                }
                else
                {
                    break;
                }
            }
            
            // Rebuild scene state at this time
            RebuildSceneState();
            UpdateUI();
        }
        
        /// <summary>
        /// Skip to next significant event
        /// </summary>
        public void SkipToNextEvent()
        {
            if (_currentReplay == null || _currentEventIndex >= _currentReplay.Events.Count) return;
            
            var nextEvent = _currentReplay.Events[_currentEventIndex];
            SeekTo(nextEvent.Timestamp);
        }
        
        /// <summary>
        /// Skip to previous significant event
        /// </summary>
        public void SkipToPreviousEvent()
        {
            if (_currentReplay == null || _currentEventIndex <= 0) return;
            
            var prevIndex = _currentEventIndex - 2;
            if (prevIndex < 0) prevIndex = 0;
            
            var prevEvent = _currentReplay.Events[prevIndex];
            SeekTo(prevEvent.Timestamp);
        }
        
        /// <summary>
        /// Restart replay from beginning
        /// </summary>
        public void Restart()
        {
            SeekTo(0f);
            Play();
        }
        
        /// <summary>
        /// Stop replay and unload
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
            _currentReplay = null;
            ClearReplayScene();
            OnReplayEnded?.Invoke();
        }
        
        private void UpdateReplayPlayback()
        {
            _replayTime += Time.deltaTime * _currentPlaybackSpeed;
            
            // Process events up to current time
            while (_currentEventIndex < _currentReplay.Events.Count)
            {
                var evt = _currentReplay.Events[_currentEventIndex];
                if (evt.Timestamp <= _replayTime)
                {
                    ProcessEvent(evt);
                    OnEventPlayed?.Invoke(evt);
                    _currentEventIndex++;
                }
                else
                {
                    break;
                }
            }
            
            // Update continuous animations (unit movement, etc.)
            UpdateUnitPositions();
            
            // Check if replay ended
            if (_replayTime >= _currentReplay.Duration)
            {
                Pause();
                OnReplayEnded?.Invoke();
            }
            
            UpdateUI();
        }
        
        private void ProcessEvent(BattleEvent evt)
        {
            switch (evt.Type)
            {
                case BattleEventType.UnitSpawned:
                    SpawnReplayUnit(evt);
                    break;
                    
                case BattleEventType.UnitMoved:
                    // Movement is handled continuously
                    break;
                    
                case BattleEventType.UnitAttacked:
                    PlayAttackEffect(evt);
                    break;
                    
                case BattleEventType.UnitDied:
                    HandleUnitDeath(evt);
                    break;
                    
                case BattleEventType.BuildingDamaged:
                    PlayBuildingDamageEffect(evt);
                    break;
                    
                case BattleEventType.BuildingDestroyed:
                    HandleBuildingDestroyed(evt);
                    break;
                    
                case BattleEventType.DefenseFired:
                    PlayDefenseFireEffect(evt);
                    break;
                    
                case BattleEventType.SpecialAbilityUsed:
                    PlaySpecialAbilityEffect(evt);
                    break;
                    
                case BattleEventType.BattleEnded:
                    // Show battle result overlay
                    break;
            }
        }
        
        #endregion
        
        #region Scene Management
        
        private async Task SetupReplayScene()
        {
            ClearReplayScene();
            
            if (_currentReplay == null) return;
            
            // Load territory buildings
            foreach (var building in _currentReplay.InitialBuildings)
            {
                var buildingObj = CreateBuildingVisual(building);
                if (buildingObj != null)
                {
                    _replayBuildings[building.Id] = buildingObj;
                }
            }
            
            _replayTime = 0f;
            _currentEventIndex = 0;
            
            UpdateUI();
        }
        
        private void ClearReplayScene()
        {
            // Destroy units
            foreach (var unit in _replayUnits.Values)
            {
                if (unit != null) Destroy(unit);
            }
            _replayUnits.Clear();
            
            // Destroy buildings
            foreach (var building in _replayBuildings.Values)
            {
                if (building != null) Destroy(building);
            }
            _replayBuildings.Clear();
            
            // Destroy effects
            foreach (var effect in _activeEffects)
            {
                if (effect != null) Destroy(effect);
            }
            _activeEffects.Clear();
        }
        
        private void RebuildSceneState()
        {
            ClearReplayScene();
            
            if (_currentReplay == null) return;
            
            // Restore initial buildings
            var activeBuildings = new Dictionary<string, BuildingSnapshot>(_currentReplay.InitialBuildings.Count);
            foreach (var building in _currentReplay.InitialBuildings)
            {
                activeBuildings[building.Id] = building;
            }
            
            // Track spawned units
            var activeUnits = new Dictionary<string, UnitSnapshot>();
            
            // Apply all events up to current time
            for (int i = 0; i < _currentEventIndex; i++)
            {
                var evt = _currentReplay.Events[i];
                
                switch (evt.Type)
                {
                    case BattleEventType.UnitSpawned:
                        activeUnits[evt.UnitId] = new UnitSnapshot
                        {
                            Id = evt.UnitId,
                            Position = evt.Position,
                            Type = evt.UnitType,
                            Health = evt.Value
                        };
                        break;
                        
                    case BattleEventType.UnitMoved:
                        if (activeUnits.TryGetValue(evt.UnitId, out var unit))
                        {
                            unit.Position = evt.Position;
                        }
                        break;
                        
                    case BattleEventType.UnitDied:
                        activeUnits.Remove(evt.UnitId);
                        break;
                        
                    case BattleEventType.BuildingDestroyed:
                        activeBuildings.Remove(evt.TargetId);
                        break;
                        
                    case BattleEventType.BuildingDamaged:
                        if (activeBuildings.TryGetValue(evt.TargetId, out var bld))
                        {
                            bld.Health -= (int)evt.Value;
                        }
                        break;
                }
            }
            
            // Create building visuals
            foreach (var building in activeBuildings.Values)
            {
                var buildingObj = CreateBuildingVisual(building);
                if (buildingObj != null)
                {
                    _replayBuildings[building.Id] = buildingObj;
                }
            }
            
            // Create unit visuals
            foreach (var unit in activeUnits.Values)
            {
                var unitObj = CreateUnitVisual(unit);
                if (unitObj != null)
                {
                    _replayUnits[unit.Id] = unitObj;
                }
            }
        }
        
        private GameObject CreateBuildingVisual(BuildingSnapshot building)
        {
            // Create a simple visual representation
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = $"Building_{building.Id}";
            obj.transform.position = building.Position;
            obj.transform.rotation = building.Rotation;
            obj.transform.localScale = Vector3.one * building.Scale;
            
            // Set color based on type
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = GetBuildingColor(building.Type);
            }
            
            return obj;
        }
        
        private GameObject CreateUnitVisual(UnitSnapshot unit)
        {
            GameObject obj;
            
            if (unit.IsAttacker && attackerUnitPrefab != null)
            {
                obj = Instantiate(attackerUnitPrefab);
            }
            else if (!unit.IsAttacker && defenderUnitPrefab != null)
            {
                obj = Instantiate(defenderUnitPrefab);
            }
            else
            {
                obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                var renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = unit.IsAttacker ? Color.red : Color.blue;
                }
            }
            
            obj.name = $"Unit_{unit.Id}";
            obj.transform.position = unit.Position;
            obj.transform.localScale = Vector3.one * 0.5f;
            
            return obj;
        }
        
        private void SpawnReplayUnit(BattleEvent evt)
        {
            var unit = new UnitSnapshot
            {
                Id = evt.UnitId,
                Position = evt.Position,
                Type = evt.UnitType,
                Health = evt.Value,
                IsAttacker = evt.IsAttackerUnit
            };
            
            var unitObj = CreateUnitVisual(unit);
            if (unitObj != null)
            {
                _replayUnits[evt.UnitId] = unitObj;
                
                // Play spawn effect
                // PlayEffect(spawnEffectPrefab, evt.Position);
            }
        }
        
        private void UpdateUnitPositions()
        {
            // Interpolate unit positions based on movement events
            // This provides smooth movement between keyframes
            
            foreach (var kvp in _replayUnits)
            {
                var unitId = kvp.Key;
                var unitObj = kvp.Value;
                
                if (unitObj == null) continue;
                
                // Find current and next movement events
                Vector3? currentPos = null;
                Vector3? nextPos = null;
                float currentTime = 0f;
                float nextTime = 0f;
                
                for (int i = 0; i < _currentReplay.Events.Count; i++)
                {
                    var evt = _currentReplay.Events[i];
                    if (evt.UnitId != unitId) continue;
                    
                    if (evt.Type == BattleEventType.UnitMoved || evt.Type == BattleEventType.UnitSpawned)
                    {
                        if (evt.Timestamp <= _replayTime)
                        {
                            currentPos = evt.Position;
                            currentTime = evt.Timestamp;
                        }
                        else if (!nextPos.HasValue)
                        {
                            nextPos = evt.Position;
                            nextTime = evt.Timestamp;
                            break;
                        }
                    }
                }
                
                // Interpolate position
                if (currentPos.HasValue && nextPos.HasValue && nextTime > currentTime)
                {
                    float t = (_replayTime - currentTime) / (nextTime - currentTime);
                    unitObj.transform.position = Vector3.Lerp(currentPos.Value, nextPos.Value, t);
                }
                else if (currentPos.HasValue)
                {
                    unitObj.transform.position = currentPos.Value;
                }
            }
        }
        
        private void HandleUnitDeath(BattleEvent evt)
        {
            if (_replayUnits.TryGetValue(evt.UnitId, out var unitObj))
            {
                // Play death effect
                if (explosionPrefab != null)
                {
                    var effect = Instantiate(explosionPrefab, unitObj.transform.position, Quaternion.identity);
                    _activeEffects.Add(effect);
                    Destroy(effect, 2f);
                }
                
                Destroy(unitObj);
                _replayUnits.Remove(evt.UnitId);
            }
        }
        
        private void PlayAttackEffect(BattleEvent evt)
        {
            // Show attack line or projectile
            if (trajectoryLine != null && _replayUnits.TryGetValue(evt.UnitId, out var attacker))
            {
                if (_replayBuildings.TryGetValue(evt.TargetId, out var target) ||
                    _replayUnits.TryGetValue(evt.TargetId, out target))
                {
                    // Brief attack line visualization
                    StartCoroutine(ShowAttackLine(attacker.transform.position, target.transform.position));
                }
            }
        }
        
        private System.Collections.IEnumerator ShowAttackLine(Vector3 start, Vector3 end)
        {
            if (trajectoryLine == null) yield break;
            
            trajectoryLine.enabled = true;
            trajectoryLine.SetPosition(0, start);
            trajectoryLine.SetPosition(1, end);
            
            yield return new WaitForSeconds(0.15f);
            
            trajectoryLine.enabled = false;
        }
        
        private void PlayBuildingDamageEffect(BattleEvent evt)
        {
            if (_replayBuildings.TryGetValue(evt.TargetId, out var building))
            {
                // Flash building red
                StartCoroutine(FlashObject(building, Color.red, 0.2f));
            }
        }
        
        private void HandleBuildingDestroyed(BattleEvent evt)
        {
            if (_replayBuildings.TryGetValue(evt.TargetId, out var building))
            {
                // Play destruction effect
                if (explosionPrefab != null)
                {
                    var effect = Instantiate(explosionPrefab, building.transform.position, Quaternion.identity);
                    effect.transform.localScale = Vector3.one * 2f;
                    _activeEffects.Add(effect);
                    Destroy(effect, 3f);
                }
                
                Destroy(building);
                _replayBuildings.Remove(evt.TargetId);
            }
        }
        
        private void PlayDefenseFireEffect(BattleEvent evt)
        {
            if (projectilePrefab != null && _replayBuildings.TryGetValue(evt.SourceId, out var defense))
            {
                Vector3 targetPos = evt.Position;
                
                // Spawn projectile
                var projectile = Instantiate(projectilePrefab, defense.transform.position, Quaternion.identity);
                _activeEffects.Add(projectile);
                
                // Animate to target
                StartCoroutine(AnimateProjectile(projectile, defense.transform.position, targetPos, 0.5f));
            }
        }
        
        private System.Collections.IEnumerator AnimateProjectile(GameObject projectile, Vector3 start, Vector3 end, float duration)
        {
            float elapsed = 0f;
            
            while (elapsed < duration && projectile != null)
            {
                float t = elapsed / duration;
                Vector3 pos = Vector3.Lerp(start, end, t);
                pos.y += Mathf.Sin(t * Mathf.PI) * 2f; // Arc
                projectile.transform.position = pos;
                
                elapsed += Time.deltaTime * _currentPlaybackSpeed;
                yield return null;
            }
            
            if (projectile != null)
            {
                // Impact effect
                if (explosionPrefab != null)
                {
                    var impact = Instantiate(explosionPrefab, end, Quaternion.identity);
                    impact.transform.localScale = Vector3.one * 0.5f;
                    _activeEffects.Add(impact);
                    Destroy(impact, 1f);
                }
                
                Destroy(projectile);
            }
        }
        
        private void PlaySpecialAbilityEffect(BattleEvent evt)
        {
            // Play ability-specific effect based on evt.AbilityType
            Debug.Log($"[BattleReplay] Special ability: {evt.AbilityType} at {evt.Position}");
        }
        
        private System.Collections.IEnumerator FlashObject(GameObject obj, Color flashColor, float duration)
        {
            var renderer = obj?.GetComponent<Renderer>();
            if (renderer == null) yield break;
            
            Color originalColor = renderer.material.color;
            renderer.material.color = flashColor;
            
            yield return new WaitForSeconds(duration);
            
            if (renderer != null)
            {
                renderer.material.color = originalColor;
            }
        }
        
        private Color GetBuildingColor(string buildingType)
        {
            // Return color based on building type
            switch (buildingType?.ToLower())
            {
                case "wall": return Color.gray;
                case "tower": return new Color(0.6f, 0.4f, 0.2f); // Brown
                case "cannon": return Color.black;
                case "citadel": return Color.yellow;
                case "barracks": return new Color(0.4f, 0.6f, 0.4f); // Green
                default: return Color.white;
            }
        }
        
        #endregion
        
        #region UI Updates
        
        private void UpdateUI()
        {
            if (_currentReplay == null) return;
            
            // Update timeline slider
            if (timelineSlider != null)
            {
                timelineSlider.value = _replayTime / _currentReplay.Duration;
            }
            
            // Update time text
            if (timelineText != null)
            {
                var current = TimeSpan.FromSeconds(_replayTime);
                var total = TimeSpan.FromSeconds(_currentReplay.Duration);
                timelineText.text = $"{current:mm\\:ss} / {total:mm\\:ss}";
                
                if (_currentPlaybackSpeed != playbackSpeed)
                {
                    timelineText.text += $" ({_currentPlaybackSpeed}x)";
                }
            }
        }
        
        /// <summary>
        /// Handle timeline slider changed by user
        /// </summary>
        public void OnTimelineSliderChanged(float value)
        {
            if (_currentReplay != null)
            {
                SeekTo(value * _currentReplay.Duration);
            }
        }
        
        #endregion
        
        #region Analysis Features
        
        /// <summary>
        /// Get damage breakdown by unit type
        /// </summary>
        public Dictionary<string, float> GetDamageByUnitType()
        {
            var breakdown = new Dictionary<string, float>();
            
            if (_currentReplay == null) return breakdown;
            
            foreach (var evt in _currentReplay.Events)
            {
                if (evt.Type == BattleEventType.UnitAttacked || evt.Type == BattleEventType.BuildingDamaged)
                {
                    string unitType = evt.UnitType ?? "Unknown";
                    if (!breakdown.ContainsKey(unitType))
                    {
                        breakdown[unitType] = 0f;
                    }
                    breakdown[unitType] += evt.Value;
                }
            }
            
            return breakdown;
        }
        
        /// <summary>
        /// Get timeline of damage over time
        /// </summary>
        public List<Vector2> GetDamageTimeline(float interval = 1f)
        {
            var timeline = new List<Vector2>();
            
            if (_currentReplay == null) return timeline;
            
            float currentDamage = 0f;
            float lastRecordTime = 0f;
            
            foreach (var evt in _currentReplay.Events)
            {
                if (evt.Timestamp >= lastRecordTime + interval)
                {
                    timeline.Add(new Vector2(lastRecordTime, currentDamage));
                    lastRecordTime = evt.Timestamp;
                    currentDamage = 0f;
                }
                
                if (evt.Type == BattleEventType.UnitAttacked || evt.Type == BattleEventType.BuildingDamaged)
                {
                    currentDamage += evt.Value;
                }
            }
            
            // Add final data point
            timeline.Add(new Vector2(_currentReplay.Duration, currentDamage));
            
            return timeline;
        }
        
        /// <summary>
        /// Get heatmap of attack locations
        /// </summary>
        public List<Vector3> GetAttackHeatmapPoints()
        {
            var points = new List<Vector3>();
            
            if (_currentReplay == null) return points;
            
            foreach (var evt in _currentReplay.Events)
            {
                if (evt.Type == BattleEventType.UnitAttacked || 
                    evt.Type == BattleEventType.DefenseFired ||
                    evt.Type == BattleEventType.BuildingDamaged)
                {
                    points.Add(evt.Position);
                }
            }
            
            return points;
        }
        
        #endregion
    }
    
    #region Data Classes
    
    /// <summary>
    /// Complete battle replay data
    /// </summary>
    [Serializable]
    public class BattleReplay
    {
        public string Id;
        public string TerritoryId;
        public string TerritoryName;
        
        public string AttackerId;
        public string AttackerName;
        public string DefenderId;
        public string DefenderName;
        
        public DateTime Timestamp;
        public float Duration;
        public bool AttackerWon;
        
        public List<BuildingSnapshot> InitialBuildings = new List<BuildingSnapshot>();
        public List<BattleEvent> Events = new List<BattleEvent>();
        
        public int AttackerTotalDamage;
        public int DefenderTotalDamage;
        public int BuildingsDestroyed;
        public int UnitsDeployed;
        public int UnitsLost;
        
#if FIREBASE_ENABLED
        public static BattleReplay FromFirestore(Firebase.Firestore.DocumentSnapshot doc)
        {
            try
            {
                var replay = new BattleReplay
                {
                    Id = doc.Id,
                    TerritoryId = doc.GetValue<string>("territoryId"),
                    TerritoryName = doc.GetValue<string>("territoryName"),
                    AttackerId = doc.GetValue<string>("attackerId"),
                    AttackerName = doc.GetValue<string>("attackerName"),
                    DefenderId = doc.GetValue<string>("defenderId"),
                    DefenderName = doc.GetValue<string>("defenderName"),
                    Timestamp = doc.GetValue<Firebase.Firestore.Timestamp>("timestamp").ToDateTime(),
                    Duration = doc.GetValue<float>("duration"),
                    AttackerWon = doc.GetValue<bool>("attackerWon")
                };
                
                // Parse initial buildings
                if (doc.ContainsField("initialBuildings"))
                {
                    var buildings = doc.GetValue<List<Dictionary<string, object>>>("initialBuildings");
                    foreach (var bld in buildings)
                    {
                        replay.InitialBuildings.Add(BuildingSnapshot.FromDictionary(bld));
                    }
                }
                
                // Parse events
                if (doc.ContainsField("events"))
                {
                    var events = doc.GetValue<List<Dictionary<string, object>>>("events");
                    foreach (var evt in events)
                    {
                        replay.Events.Add(BattleEvent.FromDictionary(evt));
                    }
                }
                
                return replay;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BattleReplay] Parse error: {ex.Message}");
                return null;
            }
        }
#endif
    }
    
    /// <summary>
    /// Summary of a replay for listing
    /// </summary>
    [Serializable]
    public class BattleReplaySummary
    {
        public string Id;
        public string TerritoryId;
        public string AttackerId;
        public string AttackerName;
        public string DefenderId;
        public string DefenderName;
        public DateTime Timestamp;
        public bool AttackerWon;
        public float Duration;
        public bool WasAttacker;
        public int AttackerDamageDealt;
        public int DefenderDamageDealt;
    }
    
    /// <summary>
    /// Snapshot of a building at battle start
    /// </summary>
    [Serializable]
    public class BuildingSnapshot
    {
        public string Id;
        public string Type;
        public Vector3 Position;
        public Quaternion Rotation;
        public float Scale = 1f;
        public int Health;
        public int MaxHealth;
        
        public static BuildingSnapshot FromDictionary(Dictionary<string, object> dict)
        {
            var snapshot = new BuildingSnapshot
            {
                Id = dict.GetValueOrDefault("id")?.ToString() ?? "",
                Type = dict.GetValueOrDefault("type")?.ToString() ?? "",
                Health = Convert.ToInt32(dict.GetValueOrDefault("health", 100)),
                MaxHealth = Convert.ToInt32(dict.GetValueOrDefault("maxHealth", 100)),
                Scale = Convert.ToSingle(dict.GetValueOrDefault("scale", 1f))
            };
            
            // Parse position
            if (dict.TryGetValue("position", out var posObj) && posObj is Dictionary<string, object> pos)
            {
                snapshot.Position = new Vector3(
                    Convert.ToSingle(pos.GetValueOrDefault("x", 0)),
                    Convert.ToSingle(pos.GetValueOrDefault("y", 0)),
                    Convert.ToSingle(pos.GetValueOrDefault("z", 0))
                );
            }
            
            // Parse rotation
            if (dict.TryGetValue("rotation", out var rotObj) && rotObj is Dictionary<string, object> rot)
            {
                snapshot.Rotation = new Quaternion(
                    Convert.ToSingle(rot.GetValueOrDefault("x", 0)),
                    Convert.ToSingle(rot.GetValueOrDefault("y", 0)),
                    Convert.ToSingle(rot.GetValueOrDefault("z", 0)),
                    Convert.ToSingle(rot.GetValueOrDefault("w", 1))
                );
            }
            
            return snapshot;
        }
    }
    
    /// <summary>
    /// Snapshot of a unit during replay
    /// </summary>
    [Serializable]
    public class UnitSnapshot
    {
        public string Id;
        public string Type;
        public Vector3 Position;
        public float Health;
        public bool IsAttacker;
    }
    
    /// <summary>
    /// Single event in a battle replay
    /// </summary>
    [Serializable]
    public class BattleEvent
    {
        public BattleEventType Type;
        public float Timestamp;
        public string UnitId;
        public string SourceId;
        public string TargetId;
        public string UnitType;
        public string AbilityType;
        public Vector3 Position;
        public float Value;
        public bool IsAttackerUnit;
        
        public static BattleEvent FromDictionary(Dictionary<string, object> dict)
        {
            var evt = new BattleEvent
            {
                Timestamp = Convert.ToSingle(dict.GetValueOrDefault("timestamp", 0)),
                UnitId = dict.GetValueOrDefault("unitId")?.ToString() ?? "",
                SourceId = dict.GetValueOrDefault("sourceId")?.ToString() ?? "",
                TargetId = dict.GetValueOrDefault("targetId")?.ToString() ?? "",
                UnitType = dict.GetValueOrDefault("unitType")?.ToString() ?? "",
                AbilityType = dict.GetValueOrDefault("abilityType")?.ToString() ?? "",
                Value = Convert.ToSingle(dict.GetValueOrDefault("value", 0)),
                IsAttackerUnit = Convert.ToBoolean(dict.GetValueOrDefault("isAttacker", false))
            };
            
            // Parse type
            if (Enum.TryParse<BattleEventType>(dict.GetValueOrDefault("type")?.ToString(), out var type))
            {
                evt.Type = type;
            }
            
            // Parse position
            if (dict.TryGetValue("position", out var posObj) && posObj is Dictionary<string, object> pos)
            {
                evt.Position = new Vector3(
                    Convert.ToSingle(pos.GetValueOrDefault("x", 0)),
                    Convert.ToSingle(pos.GetValueOrDefault("y", 0)),
                    Convert.ToSingle(pos.GetValueOrDefault("z", 0))
                );
            }
            
            return evt;
        }
    }
    
    /// <summary>
    /// Types of events that can occur in battle
    /// </summary>
    public enum BattleEventType
    {
        BattleStarted,
        UnitSpawned,
        UnitMoved,
        UnitAttacked,
        UnitDied,
        BuildingDamaged,
        BuildingDestroyed,
        DefenseFired,
        SpecialAbilityUsed,
        ResourceCaptured,
        BattleEnded,
        // Combat action types
        MeleeClash,
        ArcherVolley,
        CavalryCharge,
        SiegeWeapon,
        DefenseHold,
        FireAttack,
        Breakthrough,
        Retreat,
        Advance
    }
    
    #endregion
}
