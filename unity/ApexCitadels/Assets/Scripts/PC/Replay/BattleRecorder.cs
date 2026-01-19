using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApexCitadels.Core;
using ApexCitadels.PC.Combat;

#if FIREBASE_ENABLED
using Firebase.Firestore;
#endif

#pragma warning disable 0414

namespace ApexCitadels.PC.Replay
{
    /// <summary>
    /// Battle Recorder - Captures combat events for replay
    /// Records unit positions, attacks, deaths, and building damage
    /// for later playback in the Battle Replay System.
    /// </summary>
    public class BattleRecorder : MonoBehaviour
    {
        [Header("Recording Settings")]
        [SerializeField] private float positionSampleRate = 0.25f; // Sample positions every 250ms
        [SerializeField] private bool recordAllBattles = true;
        [SerializeField] private int maxEventsPerReplay = 5000;
        
        [Header("Auto-Save")]
        [SerializeField] private bool autoSaveToFirebase = true;
        [SerializeField] private bool keepLocalBackup = true;
        [SerializeField] private int maxLocalReplays = 50;
        
        // Singleton
        private static BattleRecorder _instance;
        public static BattleRecorder Instance => _instance;
        
        // Current recording
        private RecordingSession _currentSession;
        private float _recordingStartTime;
        private float _lastPositionSample;
        private bool _isRecording;
        
        // Tracked entities
        private Dictionary<string, TrackedUnit> _trackedUnits = new Dictionary<string, TrackedUnit>();
        private Dictionary<string, TrackedBuilding> _trackedBuildings = new Dictionary<string, TrackedBuilding>();
        
        // Events
        public event Action<RecordingSession> OnRecordingStarted;
        public event Action<RecordingSession> OnRecordingEnded;
        public event Action<BattleEvent> OnEventRecorded;
        
        // Properties
        public bool IsRecording => _isRecording;
        public float RecordingDuration => _isRecording ? Time.time - _recordingStartTime : 0f;
        public int EventCount => _currentSession?.Events.Count ?? 0;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        
        private void Update()
        {
            if (_isRecording)
            {
                // Sample unit positions periodically
                if (Time.time - _lastPositionSample >= positionSampleRate)
                {
                    SampleUnitPositions();
                    _lastPositionSample = Time.time;
                }
            }
        }
        
        #region Recording Control
        
        /// <summary>
        /// Start recording a new battle
        /// </summary>
        public void StartRecording(BattleContext context)
        {
            if (_isRecording)
            {
                ApexLogger.LogWarning("Already recording - stopping previous session", ApexLogger.LogCategory.Replay);
                _ = StopRecording(false); // Fire and forget, intentionally not awaited
            }
            
            _currentSession = new RecordingSession
            {
                Id = Guid.NewGuid().ToString(),
                TerritoryId = context.TerritoryId,
                TerritoryName = context.TerritoryName,
                AttackerId = context.AttackerId,
                AttackerName = context.AttackerName,
                DefenderId = context.DefenderId,
                DefenderName = context.DefenderName,
                StartTime = DateTime.UtcNow,
                InitialBuildings = new List<BuildingSnapshot>(),
                Events = new List<BattleEvent>()
            };
            
            // Capture initial building state
            foreach (var building in context.DefenderBuildings)
            {
                var snapshot = new BuildingSnapshot
                {
                    Id = building.Id,
                    Type = building.Type,
                    Position = building.Position,
                    Rotation = building.Rotation,
                    Scale = building.Scale,
                    Health = building.Health,
                    MaxHealth = building.MaxHealth
                };
                _currentSession.InitialBuildings.Add(snapshot);
                
                _trackedBuildings[building.Id] = new TrackedBuilding
                {
                    Id = building.Id,
                    CurrentHealth = building.Health
                };
            }
            
            _recordingStartTime = Time.time;
            _lastPositionSample = Time.time;
            _isRecording = true;
            
            // Record battle start event
            RecordEvent(new BattleEvent
            {
                Type = BattleEventType.BattleStarted,
                Timestamp = 0f
            });
            
            OnRecordingStarted?.Invoke(_currentSession);
            ApexLogger.Log($"Started recording: {context.TerritoryName}", ApexLogger.LogCategory.Replay);
        }
        
        /// <summary>
        /// Stop recording and finalize the replay
        /// </summary>
        public async Task<string> StopRecording(bool attackerWon)
        {
            if (!_isRecording || _currentSession == null)
            {
                ApexLogger.LogWarning("Not currently recording", ApexLogger.LogCategory.Replay);
                return null;
            }
            
            _isRecording = false;
            
            // Record battle end event
            RecordEvent(new BattleEvent
            {
                Type = BattleEventType.BattleEnded,
                Timestamp = RecordingDuration,
                Value = attackerWon ? 1f : 0f
            });
            
            // Finalize session
            _currentSession.Duration = Time.time - _recordingStartTime;
            _currentSession.AttackerWon = attackerWon;
            _currentSession.EndTime = DateTime.UtcNow;
            
            // Calculate statistics
            CalculateStatistics();
            
            string replayId = _currentSession.Id;
            
            // Save replay
            if (autoSaveToFirebase)
            {
                await SaveToFirebase();
            }
            
            if (keepLocalBackup)
            {
                SaveLocally();
            }
            
            OnRecordingEnded?.Invoke(_currentSession);
            ApexLogger.Log($"Recording stopped. Duration: {_currentSession.Duration:F1}s, Events: {_currentSession.Events.Count}", ApexLogger.LogCategory.Replay);
            
            // Clear tracking
            _trackedUnits.Clear();
            _trackedBuildings.Clear();
            _currentSession = null;
            
            return replayId;
        }
        
        /// <summary>
        /// Cancel recording without saving
        /// </summary>
        public void CancelRecording()
        {
            _isRecording = false;
            _trackedUnits.Clear();
            _trackedBuildings.Clear();
            _currentSession = null;
            ApexLogger.Log("Recording cancelled", ApexLogger.LogCategory.Replay);
        }
        
        #endregion
        
        #region Event Recording
        
        /// <summary>
        /// Record a unit being spawned/deployed
        /// </summary>
        public void RecordUnitSpawned(string unitId, string unitType, Vector3 position, bool isAttacker, float health)
        {
            if (!_isRecording) return;
            
            _trackedUnits[unitId] = new TrackedUnit
            {
                Id = unitId,
                Type = unitType,
                LastPosition = position,
                IsAttacker = isAttacker
            };
            
            RecordEvent(new BattleEvent
            {
                Type = BattleEventType.UnitSpawned,
                Timestamp = GetRecordingTime(),
                UnitId = unitId,
                UnitType = unitType,
                Position = position,
                Value = health,
                IsAttackerUnit = isAttacker
            });
        }
        
        /// <summary>
        /// Record a unit attack
        /// </summary>
        public void RecordUnitAttack(string attackerId, string targetId, float damage, Vector3 position, bool isCritical = false)
        {
            if (!_isRecording) return;
            
            string unitType = _trackedUnits.TryGetValue(attackerId, out var attacker) ? attacker.Type : "Unknown";
            bool isAttacker = attacker?.IsAttacker ?? true;
            
            var evt = new BattleEvent
            {
                Type = BattleEventType.UnitAttacked,
                Timestamp = GetRecordingTime(),
                UnitId = attackerId,
                TargetId = targetId,
                UnitType = unitType,
                Position = position,
                Value = damage,
                IsAttackerUnit = isAttacker
            };
            
            if (isCritical)
            {
                evt.AbilityType = "Critical";
            }
            
            RecordEvent(evt);
            
            _currentSession.TotalDamageDealt += (int)damage;
            if (isAttacker)
                _currentSession.AttackerDamageDealt += (int)damage;
            else
                _currentSession.DefenderDamageDealt += (int)damage;
        }
        
        /// <summary>
        /// Record a unit death
        /// </summary>
        public void RecordUnitDeath(string unitId, Vector3 position, string killedBy = null)
        {
            if (!_isRecording) return;
            
            string unitType = _trackedUnits.TryGetValue(unitId, out var unit) ? unit.Type : "Unknown";
            bool isAttacker = unit?.IsAttacker ?? true;
            
            RecordEvent(new BattleEvent
            {
                Type = BattleEventType.UnitDied,
                Timestamp = GetRecordingTime(),
                UnitId = unitId,
                UnitType = unitType,
                Position = position,
                SourceId = killedBy,
                IsAttackerUnit = isAttacker
            });
            
            _trackedUnits.Remove(unitId);
            
            if (isAttacker)
                _currentSession.AttackerUnitsLost++;
            else
                _currentSession.DefenderUnitsLost++;
        }
        
        /// <summary>
        /// Record building taking damage
        /// </summary>
        public void RecordBuildingDamaged(string buildingId, float damage, Vector3 position, string attackerId = null)
        {
            if (!_isRecording) return;
            
            RecordEvent(new BattleEvent
            {
                Type = BattleEventType.BuildingDamaged,
                Timestamp = GetRecordingTime(),
                TargetId = buildingId,
                Position = position,
                Value = damage,
                UnitId = attackerId
            });
            
            if (_trackedBuildings.TryGetValue(buildingId, out var building))
            {
                building.CurrentHealth -= (int)damage;
            }
        }
        
        /// <summary>
        /// Record building destruction
        /// </summary>
        public void RecordBuildingDestroyed(string buildingId, Vector3 position, string destroyedBy = null)
        {
            if (!_isRecording) return;
            
            RecordEvent(new BattleEvent
            {
                Type = BattleEventType.BuildingDestroyed,
                Timestamp = GetRecordingTime(),
                TargetId = buildingId,
                Position = position,
                UnitId = destroyedBy
            });
            
            _trackedBuildings.Remove(buildingId);
            _currentSession.BuildingsDestroyed++;
        }
        
        /// <summary>
        /// Record a defense structure firing
        /// </summary>
        public void RecordDefenseFired(string defenseId, Vector3 defensePosition, Vector3 targetPosition, float damage)
        {
            if (!_isRecording) return;
            
            RecordEvent(new BattleEvent
            {
                Type = BattleEventType.DefenseFired,
                Timestamp = GetRecordingTime(),
                SourceId = defenseId,
                Position = targetPosition,
                Value = damage
            });
        }
        
        /// <summary>
        /// Record a special ability being used
        /// </summary>
        public void RecordSpecialAbility(string unitId, string abilityType, Vector3 position, float value = 0f)
        {
            if (!_isRecording) return;
            
            RecordEvent(new BattleEvent
            {
                Type = BattleEventType.SpecialAbilityUsed,
                Timestamp = GetRecordingTime(),
                UnitId = unitId,
                AbilityType = abilityType,
                Position = position,
                Value = value
            });
        }
        
        /// <summary>
        /// Record a resource being captured
        /// </summary>
        public void RecordResourceCaptured(string resourceType, Vector3 position, float amount)
        {
            if (!_isRecording) return;
            
            RecordEvent(new BattleEvent
            {
                Type = BattleEventType.ResourceCaptured,
                Timestamp = GetRecordingTime(),
                AbilityType = resourceType,
                Position = position,
                Value = amount
            });
        }
        
        private void RecordEvent(BattleEvent evt)
        {
            if (_currentSession == null || _currentSession.Events.Count >= maxEventsPerReplay)
                return;
            
            _currentSession.Events.Add(evt);
            OnEventRecorded?.Invoke(evt);
        }
        
        private void SampleUnitPositions()
        {
            foreach (var kvp in _trackedUnits)
            {
                var unit = kvp.Value;
                if (unit.Transform != null)
                {
                    Vector3 newPos = unit.Transform.position;
                    
                    // Only record if position changed significantly
                    if (Vector3.Distance(newPos, unit.LastPosition) > 0.5f)
                    {
                        RecordEvent(new BattleEvent
                        {
                            Type = BattleEventType.UnitMoved,
                            Timestamp = GetRecordingTime(),
                            UnitId = unit.Id,
                            Position = newPos,
                            IsAttackerUnit = unit.IsAttacker
                        });
                        
                        unit.LastPosition = newPos;
                    }
                }
            }
        }
        
        private float GetRecordingTime()
        {
            return Time.time - _recordingStartTime;
        }
        
        #endregion
        
        #region Statistics
        
        private void CalculateStatistics()
        {
            if (_currentSession == null) return;
            
            // Count units deployed
            int attackerUnits = 0;
            int defenderUnits = 0;
            
            foreach (var evt in _currentSession.Events)
            {
                if (evt.Type == BattleEventType.UnitSpawned)
                {
                    if (evt.IsAttackerUnit)
                        attackerUnits++;
                    else
                        defenderUnits++;
                }
            }
            
            _currentSession.AttackerUnitsDeployed = attackerUnits;
            _currentSession.DefenderUnitsDeployed = defenderUnits;
            
            // Calculate DPS
            if (_currentSession.Duration > 0)
            {
                _currentSession.AttackerDPS = _currentSession.AttackerDamageDealt / _currentSession.Duration;
                _currentSession.DefenderDPS = _currentSession.DefenderDamageDealt / _currentSession.Duration;
            }
            
            // Find highlight moments
            FindHighlightMoments();
        }
        
        private void FindHighlightMoments()
        {
            if (_currentSession == null) return;
            
            _currentSession.HighlightMoments = new List<HighlightMoment>();
            
            int consecutiveKills = 0;
            float lastKillTime = -10f;
            float highestDamageValue = 0f;
            BattleEvent highestDamageEvent = null;
            
            foreach (var evt in _currentSession.Events)
            {
                switch (evt.Type)
                {
                    case BattleEventType.UnitDied:
                        // Check for multi-kill
                        if (evt.Timestamp - lastKillTime < 2f)
                        {
                            consecutiveKills++;
                            if (consecutiveKills >= 3)
                            {
                                _currentSession.HighlightMoments.Add(new HighlightMoment
                                {
                                    Type = HighlightType.MultiKill,
                                    Timestamp = evt.Timestamp,
                                    Description = $"{consecutiveKills}x Kill Streak!",
                                    Importance = Mathf.Min(consecutiveKills, 5)
                                });
                            }
                        }
                        else
                        {
                            consecutiveKills = 1;
                        }
                        lastKillTime = evt.Timestamp;
                        break;
                        
                    case BattleEventType.UnitAttacked:
                        // Track highest damage
                        if (evt.Value > highestDamageValue)
                        {
                            highestDamageValue = evt.Value;
                            highestDamageEvent = evt;
                        }
                        
                        // Critical hit highlight
                        if (evt.AbilityType == "Critical")
                        {
                            _currentSession.HighlightMoments.Add(new HighlightMoment
                            {
                                Type = HighlightType.CriticalHit,
                                Timestamp = evt.Timestamp,
                                Description = $"Critical! {evt.Value:N0} damage",
                                Importance = 2
                            });
                        }
                        break;
                        
                    case BattleEventType.BuildingDestroyed:
                        _currentSession.HighlightMoments.Add(new HighlightMoment
                        {
                            Type = HighlightType.BuildingDestroyed,
                            Timestamp = evt.Timestamp,
                            Description = "Building Destroyed!",
                            Importance = 3
                        });
                        break;
                        
                    case BattleEventType.SpecialAbilityUsed:
                        _currentSession.HighlightMoments.Add(new HighlightMoment
                        {
                            Type = HighlightType.SpecialAbility,
                            Timestamp = evt.Timestamp,
                            Description = $"Special: {evt.AbilityType}",
                            Importance = 4
                        });
                        break;
                        
                    case BattleEventType.BattleEnded:
                        _currentSession.HighlightMoments.Add(new HighlightMoment
                        {
                            Type = evt.Value > 0 ? HighlightType.Victory : HighlightType.Defeat,
                            Timestamp = evt.Timestamp,
                            Description = evt.Value > 0 ? "VICTORY!" : "DEFEAT",
                            Importance = 5
                        });
                        break;
                }
            }
            
            // Add highest damage highlight
            if (highestDamageEvent != null && highestDamageValue > 100)
            {
                _currentSession.HighlightMoments.Add(new HighlightMoment
                {
                    Type = HighlightType.MassiveDamage,
                    Timestamp = highestDamageEvent.Timestamp,
                    Description = $"Massive Hit! {highestDamageValue:N0} damage",
                    Importance = 3
                });
            }
            
            // Sort by importance
            _currentSession.HighlightMoments.Sort((a, b) => b.Importance.CompareTo(a.Importance));
        }
        
        #endregion
        
        #region Persistence
        
        private async Task SaveToFirebase()
        {
            await Task.CompletedTask;
#if FIREBASE_ENABLED
            if (_currentSession == null) return;
            
            try
            {
                var db = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
                
                // Convert to Firestore document
                var data = new Dictionary<string, object>
                {
                    ["territoryId"] = _currentSession.TerritoryId,
                    ["territoryName"] = _currentSession.TerritoryName,
                    ["attackerId"] = _currentSession.AttackerId,
                    ["attackerName"] = _currentSession.AttackerName,
                    ["defenderId"] = _currentSession.DefenderId,
                    ["defenderName"] = _currentSession.DefenderName,
                    ["timestamp"] = Firebase.Firestore.Timestamp.FromDateTime(_currentSession.StartTime),
                    ["duration"] = _currentSession.Duration,
                    ["attackerWon"] = _currentSession.AttackerWon,
                    ["attackerDamageDealt"] = _currentSession.AttackerDamageDealt,
                    ["defenderDamageDealt"] = _currentSession.DefenderDamageDealt,
                    ["buildingsDestroyed"] = _currentSession.BuildingsDestroyed,
                    ["attackerUnitsDeployed"] = _currentSession.AttackerUnitsDeployed,
                    ["attackerUnitsLost"] = _currentSession.AttackerUnitsLost,
                    ["initialBuildings"] = SerializeBuildings(_currentSession.InitialBuildings),
                    ["events"] = SerializeEvents(_currentSession.Events),
                    ["highlights"] = SerializeHighlights(_currentSession.HighlightMoments)
                };
                
                await db.Collection("battle_replays")
                    .Document(_currentSession.Id)
                    .SetAsync(data);
                
                ApexLogger.Log($"Saved to Firebase: {_currentSession.Id}", ApexLogger.LogCategory.Replay);
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"Firebase save failed: {ex.Message}", ApexLogger.LogCategory.Replay);
            }
#endif
        }
        
        private List<Dictionary<string, object>> SerializeBuildings(List<BuildingSnapshot> buildings)
        {
            var list = new List<Dictionary<string, object>>();
            foreach (var b in buildings)
            {
                list.Add(new Dictionary<string, object>
                {
                    ["id"] = b.Id,
                    ["type"] = b.Type,
                    ["position"] = new Dictionary<string, object> { ["x"] = b.Position.x, ["y"] = b.Position.y, ["z"] = b.Position.z },
                    ["rotation"] = new Dictionary<string, object> { ["x"] = b.Rotation.x, ["y"] = b.Rotation.y, ["z"] = b.Rotation.z, ["w"] = b.Rotation.w },
                    ["scale"] = b.Scale,
                    ["health"] = b.Health,
                    ["maxHealth"] = b.MaxHealth
                });
            }
            return list;
        }
        
        private List<Dictionary<string, object>> SerializeEvents(List<BattleEvent> events)
        {
            var list = new List<Dictionary<string, object>>();
            foreach (var e in events)
            {
                list.Add(new Dictionary<string, object>
                {
                    ["type"] = e.Type.ToString(),
                    ["timestamp"] = e.Timestamp,
                    ["unitId"] = e.UnitId ?? "",
                    ["sourceId"] = e.SourceId ?? "",
                    ["targetId"] = e.TargetId ?? "",
                    ["unitType"] = e.UnitType ?? "",
                    ["abilityType"] = e.AbilityType ?? "",
                    ["position"] = new Dictionary<string, object> { ["x"] = e.Position.x, ["y"] = e.Position.y, ["z"] = e.Position.z },
                    ["value"] = e.Value,
                    ["isAttacker"] = e.IsAttackerUnit
                });
            }
            return list;
        }
        
        private List<Dictionary<string, object>> SerializeHighlights(List<HighlightMoment> highlights)
        {
            var list = new List<Dictionary<string, object>>();
            if (highlights == null) return list;
            
            foreach (var h in highlights)
            {
                list.Add(new Dictionary<string, object>
                {
                    ["type"] = h.Type.ToString(),
                    ["timestamp"] = h.Timestamp,
                    ["description"] = h.Description,
                    ["importance"] = h.Importance
                });
            }
            return list;
        }
        
        private void SaveLocally()
        {
            if (_currentSession == null) return;
            
            string json = JsonUtility.ToJson(new LocalReplayWrapper { Session = _currentSession }, true);
            string key = $"Replay_{_currentSession.Id}";
            PlayerPrefs.SetString(key, json);
            
            // Track saved replays
            string replayList = PlayerPrefs.GetString("ReplayList", "");
            var ids = new List<string>(replayList.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
            ids.Insert(0, _currentSession.Id);
            
            // Remove oldest if over limit
            while (ids.Count > maxLocalReplays)
            {
                string oldId = ids[ids.Count - 1];
                PlayerPrefs.DeleteKey($"Replay_{oldId}");
                ids.RemoveAt(ids.Count - 1);
            }
            
            PlayerPrefs.SetString("ReplayList", string.Join("|", ids));
            PlayerPrefs.Save();
            
            ApexLogger.Log($"Saved locally: {_currentSession.Id}", ApexLogger.LogCategory.Replay);
        }
        
        /// <summary>
        /// Load a replay from local storage
        /// </summary>
        public RecordingSession LoadLocalReplay(string replayId)
        {
            string key = $"Replay_{replayId}";
            string json = PlayerPrefs.GetString(key, "");
            
            if (string.IsNullOrEmpty(json))
            {
                ApexLogger.LogWarning($"Local replay not found: {replayId}", ApexLogger.LogCategory.Replay);
                return null;
            }
            
            try
            {
                var wrapper = JsonUtility.FromJson<LocalReplayWrapper>(json);
                return wrapper.Session;
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"Failed to load replay: {ex.Message}", ApexLogger.LogCategory.Replay);
                return null;
            }
        }
        
        /// <summary>
        /// Get list of locally saved replay IDs
        /// </summary>
        public List<string> GetLocalReplayIds()
        {
            string replayList = PlayerPrefs.GetString("ReplayList", "");
            return new List<string>(replayList.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
        }
        
        #endregion
        
        #region Unit Tracking
        
        /// <summary>
        /// Register a unit's transform for position tracking
        /// </summary>
        public void RegisterUnitTransform(string unitId, Transform transform)
        {
            if (_trackedUnits.TryGetValue(unitId, out var unit))
            {
                unit.Transform = transform;
            }
        }
        
        /// <summary>
        /// Unregister a unit from tracking
        /// </summary>
        public void UnregisterUnit(string unitId)
        {
            _trackedUnits.Remove(unitId);
        }
        
        #endregion
    }
    
    #region Data Classes
    
    /// <summary>
    /// Context for starting a battle recording
    /// </summary>
    [Serializable]
    public class BattleContext
    {
        public string TerritoryId;
        public string TerritoryName;
        public string AttackerId;
        public string AttackerName;
        public string DefenderId;
        public string DefenderName;
        public List<BuildingInfo> DefenderBuildings = new List<BuildingInfo>();
    }
    
    /// <summary>
    /// Building info for battle context
    /// </summary>
    [Serializable]
    public class BuildingInfo
    {
        public string Id;
        public string Type;
        public Vector3 Position;
        public Quaternion Rotation;
        public float Scale = 1f;
        public int Health;
        public int MaxHealth;
    }
    
    /// <summary>
    /// Complete recording session data
    /// </summary>
    [Serializable]
    public class RecordingSession
    {
        public string Id;
        public string TerritoryId;
        public string TerritoryName;
        public string AttackerId;
        public string AttackerName;
        public string DefenderId;
        public string DefenderName;
        
        public DateTime StartTime;
        public DateTime EndTime;
        public float Duration;
        public bool AttackerWon;
        
        public List<BuildingSnapshot> InitialBuildings = new List<BuildingSnapshot>();
        public List<BattleEvent> Events = new List<BattleEvent>();
        public List<HighlightMoment> HighlightMoments = new List<HighlightMoment>();
        
        // Statistics
        public int TotalDamageDealt;
        public int AttackerDamageDealt;
        public int DefenderDamageDealt;
        public int BuildingsDestroyed;
        public int AttackerUnitsDeployed;
        public int DefenderUnitsDeployed;
        public int AttackerUnitsLost;
        public int DefenderUnitsLost;
        public float AttackerDPS;
        public float DefenderDPS;
    }
    
    /// <summary>
    /// Highlight moment in battle
    /// </summary>
    [Serializable]
    public class HighlightMoment
    {
        public HighlightType Type;
        public float Timestamp;
        public string Description;
        public int Importance; // 1-5, higher = more important
    }
    
    /// <summary>
    /// Types of highlight moments
    /// </summary>
    public enum HighlightType
    {
        MultiKill,
        CriticalHit,
        MassiveDamage,
        BuildingDestroyed,
        SpecialAbility,
        Victory,
        Defeat
    }
    
    /// <summary>
    /// Tracked unit during recording
    /// </summary>
    internal class TrackedUnit
    {
        public string Id;
        public string Type;
        public Transform Transform;
        public Vector3 LastPosition;
        public bool IsAttacker;
    }
    
    /// <summary>
    /// Tracked building during recording
    /// </summary>
    internal class TrackedBuilding
    {
        public string Id;
        public int CurrentHealth;
    }
    
    /// <summary>
    /// Wrapper for JSON serialization
    /// </summary>
    [Serializable]
    internal class LocalReplayWrapper
    {
        public RecordingSession Session;
    }
    
    #endregion
}
