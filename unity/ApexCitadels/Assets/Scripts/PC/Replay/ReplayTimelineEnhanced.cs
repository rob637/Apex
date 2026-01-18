using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace ApexCitadels.PC.Replay
{
    /// <summary>
    /// Enhanced Replay Timeline - Visual timeline with highlight markers
    /// Shows battle events, allows seeking, displays mini-map preview.
    /// </summary>
    public class ReplayTimelineEnhanced : MonoBehaviour
    {
        [Header("Timeline")]
        [SerializeField] private Slider timelineSlider;
        [SerializeField] private RectTransform markerContainer;
        [SerializeField] private GameObject markerPrefab;
        [SerializeField] private TMP_Text currentTimeText;
        [SerializeField] private TMP_Text totalTimeText;
        
        [Header("Marker Colors")]
        [SerializeField] private Color unitSpawnColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color unitDeathColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color buildingDestroyedColor = new Color(1f, 0.6f, 0f);
        [SerializeField] private Color specialAbilityColor = new Color(0.6f, 0.4f, 1f);
        [SerializeField] private Color criticalColor = new Color(1f, 0.8f, 0f);
        
        [Header("Mini-Map")]
        [SerializeField] private RawImage miniMapImage;
        [SerializeField] private RectTransform miniMapContainer;
        [SerializeField] private GameObject unitMarkerPrefab;
        [SerializeField] private float miniMapSize = 150f;
        [SerializeField] private float mapWorldSize = 100f;
        
        [Header("Playback Controls")]
        [SerializeField] private Button playPauseButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button prevHighlightButton;
        [SerializeField] private Button nextHighlightButton;
        [SerializeField] private TMP_Text playbackSpeedText;
        
        [Header("Icons")]
        [SerializeField] private Sprite playIcon;
        [SerializeField] private Sprite pauseIcon;
        
        // State
        private BattleReplaySystem _replaySystem;
        private List<GameObject> _timelineMarkers = new List<GameObject>();
        private List<GameObject> _miniMapMarkers = new List<GameObject>();
        private Dictionary<string, RectTransform> _unitMiniMapMarkers = new Dictionary<string, RectTransform>();
        private bool _isDraggingTimeline;
        private float _totalDuration;
        private List<float> _highlightTimestamps = new List<float>();
        private int _currentHighlightIndex = -1;
        
        // Events
        public event Action<float> OnTimelineSeeked;
        
        private void Awake()
        {
            _replaySystem = FindFirstObjectByType<BattleReplaySystem>();
            SetupEventHandlers();
        }
        
        private void SetupEventHandlers()
        {
            if (_replaySystem != null)
            {
                _replaySystem.OnReplayLoaded += OnReplayLoaded;
                _replaySystem.OnEventPlayed += OnEventPlayed;
            }
            
            // Timeline slider
            if (timelineSlider != null)
            {
                timelineSlider.onValueChanged.AddListener(OnTimelineValueChanged);
                
                // Detect drag start/end
                var trigger = timelineSlider.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                
                var pointerDown = new UnityEngine.EventSystems.EventTrigger.Entry();
                pointerDown.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
                pointerDown.callback.AddListener((data) => _isDraggingTimeline = true);
                trigger.triggers.Add(pointerDown);
                
                var pointerUp = new UnityEngine.EventSystems.EventTrigger.Entry();
                pointerUp.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
                pointerUp.callback.AddListener((data) => _isDraggingTimeline = false);
                trigger.triggers.Add(pointerUp);
            }
            
            // Buttons
            playPauseButton?.onClick.AddListener(OnPlayPauseClicked);
            restartButton?.onClick.AddListener(OnRestartClicked);
            prevHighlightButton?.onClick.AddListener(GoToPreviousHighlight);
            nextHighlightButton?.onClick.AddListener(GoToNextHighlight);
        }
        
        private void Update()
        {
            if (_replaySystem != null && !_isDraggingTimeline)
            {
                UpdateTimelinePosition();
            }
            
            UpdateMiniMapMarkers();
            
            // Keyboard shortcuts
            if (Input.GetKeyDown(KeyCode.LeftBracket) || Input.GetKeyDown(KeyCode.Comma))
            {
                GoToPreviousHighlight();
            }
            if (Input.GetKeyDown(KeyCode.RightBracket) || Input.GetKeyDown(KeyCode.Period))
            {
                GoToNextHighlight();
            }
        }
        
        private void OnDestroy()
        {
            if (_replaySystem != null)
            {
                _replaySystem.OnReplayLoaded -= OnReplayLoaded;
                _replaySystem.OnEventPlayed -= OnEventPlayed;
            }
        }
        
        #region Timeline
        
        private void OnReplayLoaded(BattleReplay replay)
        {
            _totalDuration = replay.Duration;
            
            // Clear existing markers
            ClearTimelineMarkers();
            ClearMiniMapMarkers();
            _highlightTimestamps.Clear();
            
            // Create markers for significant events
            foreach (var evt in replay.Events)
            {
                CreateTimelineMarker(evt);
            }
            
            // Update time display
            UpdateTimeDisplay(0f, _totalDuration);
            
            // Sort highlights
            _highlightTimestamps.Sort();
            
            // Setup mini-map with initial buildings
            SetupMiniMap(replay);
            
            Debug.Log($"[ReplayTimeline] Loaded {_timelineMarkers.Count} markers, {_highlightTimestamps.Count} highlights");
        }
        
        private void CreateTimelineMarker(BattleEvent evt)
        {
            // Only create markers for significant events
            Color? markerColor = null;
            bool isHighlight = false;
            
            switch (evt.Type)
            {
                case BattleEventType.UnitSpawned:
                    markerColor = unitSpawnColor;
                    break;
                    
                case BattleEventType.UnitDied:
                    markerColor = unitDeathColor;
                    isHighlight = true;
                    break;
                    
                case BattleEventType.BuildingDestroyed:
                    markerColor = buildingDestroyedColor;
                    isHighlight = true;
                    break;
                    
                case BattleEventType.SpecialAbilityUsed:
                    markerColor = specialAbilityColor;
                    isHighlight = true;
                    break;
                    
                case BattleEventType.UnitAttacked:
                    if (evt.AbilityType == "Critical" || evt.Value > 150)
                    {
                        markerColor = criticalColor;
                        isHighlight = true;
                    }
                    break;
            }
            
            if (!markerColor.HasValue) return;
            
            // Create marker
            if (markerContainer != null)
            {
                GameObject marker;
                
                if (markerPrefab != null)
                {
                    marker = Instantiate(markerPrefab, markerContainer);
                }
                else
                {
                    marker = CreateDefaultMarker(markerContainer);
                }
                
                // Position based on timestamp
                float normalizedPos = evt.Timestamp / _totalDuration;
                var rect = marker.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(normalizedPos, 0f);
                rect.anchorMax = new Vector2(normalizedPos, 1f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(4, 0);
                
                // Set color
                var image = marker.GetComponent<Image>();
                if (image != null)
                {
                    image.color = markerColor.Value;
                }
                
                // Add tooltip
                AddMarkerTooltip(marker, evt);
                
                _timelineMarkers.Add(marker);
            }
            
            // Track highlight
            if (isHighlight)
            {
                _highlightTimestamps.Add(evt.Timestamp);
            }
        }
        
        private GameObject CreateDefaultMarker(Transform parent)
        {
            var marker = new GameObject("Marker");
            marker.transform.SetParent(parent, false);
            
            var rect = marker.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            
            var image = marker.AddComponent<Image>();
            image.raycastTarget = true;
            
            return marker;
        }
        
        private void AddMarkerTooltip(GameObject marker, BattleEvent evt)
        {
            // Simple hover tooltip using EventTrigger
            var trigger = marker.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            
            var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) =>
            {
                ShowTooltip(marker.transform.position, GetEventDescription(evt));
            });
            trigger.triggers.Add(pointerEnter);
            
            var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => HideTooltip());
            trigger.triggers.Add(pointerExit);
            
            // Click to seek
            var click = new UnityEngine.EventSystems.EventTrigger.Entry();
            click.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
            click.callback.AddListener((data) =>
            {
                _replaySystem?.SeekTo(evt.Timestamp);
                OnTimelineSeeked?.Invoke(evt.Timestamp);
            });
            trigger.triggers.Add(click);
        }
        
        private string GetEventDescription(BattleEvent evt)
        {
            var time = TimeSpan.FromSeconds(evt.Timestamp);
            string timeStr = $"[{time:mm\\:ss}] ";
            
            return evt.Type switch
            {
                BattleEventType.UnitSpawned => $"{timeStr}{evt.UnitType} deployed",
                BattleEventType.UnitDied => $"{timeStr}{evt.UnitType} killed",
                BattleEventType.UnitAttacked => $"{timeStr}{evt.UnitType}: {evt.Value:N0} dmg",
                BattleEventType.BuildingDestroyed => $"{timeStr}Building destroyed",
                BattleEventType.SpecialAbilityUsed => $"{timeStr}Ability: {evt.AbilityType}",
                _ => $"{timeStr}{evt.Type}"
            };
        }
        
        private void ClearTimelineMarkers()
        {
            foreach (var marker in _timelineMarkers)
            {
                if (marker != null)
                {
                    Destroy(marker);
                }
            }
            _timelineMarkers.Clear();
        }
        
        private void UpdateTimelinePosition()
        {
            if (_replaySystem == null || _totalDuration <= 0) return;
            
            float currentTime = _replaySystem.ReplayTime;
            
            if (timelineSlider != null)
            {
                timelineSlider.SetValueWithoutNotify(currentTime / _totalDuration);
            }
            
            UpdateTimeDisplay(currentTime, _totalDuration);
        }
        
        private void UpdateTimeDisplay(float current, float total)
        {
            if (currentTimeText != null)
            {
                var currentSpan = TimeSpan.FromSeconds(current);
                currentTimeText.text = currentSpan.ToString(@"mm\:ss");
            }
            
            if (totalTimeText != null)
            {
                var totalSpan = TimeSpan.FromSeconds(total);
                totalTimeText.text = totalSpan.ToString(@"mm\:ss");
            }
        }
        
        private void OnTimelineValueChanged(float value)
        {
            if (_isDraggingTimeline)
            {
                float targetTime = value * _totalDuration;
                _replaySystem?.SeekTo(targetTime);
                UpdateTimeDisplay(targetTime, _totalDuration);
            }
        }
        
        #endregion
        
        #region Highlights Navigation
        
        /// <summary>
        /// Jump to next highlight moment
        /// </summary>
        public void GoToNextHighlight()
        {
            if (_highlightTimestamps.Count == 0) return;
            
            float currentTime = _replaySystem?.ReplayTime ?? 0f;
            
            // Find next highlight after current time
            for (int i = 0; i < _highlightTimestamps.Count; i++)
            {
                if (_highlightTimestamps[i] > currentTime + 0.1f)
                {
                    _currentHighlightIndex = i;
                    _replaySystem?.SeekTo(_highlightTimestamps[i]);
                    OnTimelineSeeked?.Invoke(_highlightTimestamps[i]);
                    return;
                }
            }
            
            // Wrap to first
            _currentHighlightIndex = 0;
            _replaySystem?.SeekTo(_highlightTimestamps[0]);
            OnTimelineSeeked?.Invoke(_highlightTimestamps[0]);
        }
        
        /// <summary>
        /// Jump to previous highlight moment
        /// </summary>
        public void GoToPreviousHighlight()
        {
            if (_highlightTimestamps.Count == 0) return;
            
            float currentTime = _replaySystem?.ReplayTime ?? 0f;
            
            // Find previous highlight before current time
            for (int i = _highlightTimestamps.Count - 1; i >= 0; i--)
            {
                if (_highlightTimestamps[i] < currentTime - 0.1f)
                {
                    _currentHighlightIndex = i;
                    _replaySystem?.SeekTo(_highlightTimestamps[i]);
                    OnTimelineSeeked?.Invoke(_highlightTimestamps[i]);
                    return;
                }
            }
            
            // Wrap to last
            _currentHighlightIndex = _highlightTimestamps.Count - 1;
            _replaySystem?.SeekTo(_highlightTimestamps[_currentHighlightIndex]);
            OnTimelineSeeked?.Invoke(_highlightTimestamps[_currentHighlightIndex]);
        }
        
        /// <summary>
        /// Get highlight timestamps for UI display
        /// </summary>
        public List<float> GetHighlightTimestamps()
        {
            return new List<float>(_highlightTimestamps);
        }
        
        #endregion
        
        #region Mini-Map
        
        private void SetupMiniMap(BattleReplay replay)
        {
            if (miniMapContainer == null) return;
            
            // Create building markers
            foreach (var building in replay.InitialBuildings)
            {
                CreateMiniMapBuildingMarker(building);
            }
        }
        
        private void CreateMiniMapBuildingMarker(BuildingSnapshot building)
        {
            if (miniMapContainer == null) return;
            
            var marker = new GameObject($"Building_{building.Id}");
            marker.transform.SetParent(miniMapContainer, false);
            
            var rect = marker.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(8, 8);
            
            var image = marker.AddComponent<Image>();
            image.color = GetBuildingMiniMapColor(building.Type);
            
            // Position on mini-map
            Vector2 miniMapPos = WorldToMiniMap(building.Position);
            rect.anchoredPosition = miniMapPos;
            
            _miniMapMarkers.Add(marker);
        }
        
        private void OnEventPlayed(BattleEvent evt)
        {
            switch (evt.Type)
            {
                case BattleEventType.UnitSpawned:
                    CreateUnitMiniMapMarker(evt.UnitId, evt.Position, evt.IsAttackerUnit);
                    break;
                    
                case BattleEventType.UnitDied:
                    RemoveUnitMiniMapMarker(evt.UnitId);
                    break;
                    
                case BattleEventType.UnitMoved:
                    UpdateUnitMiniMapMarker(evt.UnitId, evt.Position);
                    break;
                    
                case BattleEventType.BuildingDestroyed:
                    // Flash and remove building marker
                    RemoveBuildingMiniMapMarker(evt.TargetId);
                    break;
            }
        }
        
        private void CreateUnitMiniMapMarker(string unitId, Vector3 worldPos, bool isAttacker)
        {
            if (miniMapContainer == null || _unitMiniMapMarkers.ContainsKey(unitId)) return;
            
            var marker = new GameObject($"Unit_{unitId}");
            marker.transform.SetParent(miniMapContainer, false);
            
            var rect = marker.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(6, 6);
            
            var image = marker.AddComponent<Image>();
            image.color = isAttacker ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.2f, 0.2f);
            
            // Make circular
            image.sprite = CreateCircleSprite();
            
            Vector2 miniMapPos = WorldToMiniMap(worldPos);
            rect.anchoredPosition = miniMapPos;
            
            _unitMiniMapMarkers[unitId] = rect;
            _miniMapMarkers.Add(marker);
        }
        
        private void UpdateUnitMiniMapMarker(string unitId, Vector3 worldPos)
        {
            if (_unitMiniMapMarkers.TryGetValue(unitId, out var rect))
            {
                rect.anchoredPosition = WorldToMiniMap(worldPos);
            }
        }
        
        private void RemoveUnitMiniMapMarker(string unitId)
        {
            if (_unitMiniMapMarkers.TryGetValue(unitId, out var rect))
            {
                _miniMapMarkers.Remove(rect.gameObject);
                Destroy(rect.gameObject);
                _unitMiniMapMarkers.Remove(unitId);
            }
        }
        
        private void RemoveBuildingMiniMapMarker(string buildingId)
        {
            for (int i = _miniMapMarkers.Count - 1; i >= 0; i--)
            {
                if (_miniMapMarkers[i] != null && _miniMapMarkers[i].name == $"Building_{buildingId}")
                {
                    // Flash red before removing
                    StartCoroutine(FlashAndRemove(_miniMapMarkers[i]));
                    _miniMapMarkers.RemoveAt(i);
                    break;
                }
            }
        }
        
        private IEnumerator FlashAndRemove(GameObject obj)
        {
            var image = obj.GetComponent<Image>();
            if (image != null)
            {
                Color original = image.color;
                
                for (int i = 0; i < 3; i++)
                {
                    image.color = Color.red;
                    yield return new WaitForSeconds(0.1f);
                    image.color = original;
                    yield return new WaitForSeconds(0.1f);
                }
            }
            
            Destroy(obj);
        }
        
        private void UpdateMiniMapMarkers()
        {
            // Update positions from tracked units in replay system
            // This handles scrubbing/seeking updates
        }
        
        private void ClearMiniMapMarkers()
        {
            foreach (var marker in _miniMapMarkers)
            {
                if (marker != null)
                {
                    Destroy(marker);
                }
            }
            _miniMapMarkers.Clear();
            _unitMiniMapMarkers.Clear();
        }
        
        private Vector2 WorldToMiniMap(Vector3 worldPos)
        {
            // Convert world position to mini-map position
            float normalizedX = (worldPos.x + mapWorldSize / 2f) / mapWorldSize;
            float normalizedZ = (worldPos.z + mapWorldSize / 2f) / mapWorldSize;
            
            return new Vector2(
                (normalizedX - 0.5f) * miniMapSize,
                (normalizedZ - 0.5f) * miniMapSize
            );
        }
        
        private Color GetBuildingMiniMapColor(string buildingType)
        {
            return buildingType?.ToLower() switch
            {
                "wall" => Color.gray,
                "tower" => new Color(0.6f, 0.4f, 0.2f),
                "cannon" => Color.black,
                "citadel" => Color.yellow,
                "barracks" => new Color(0.4f, 0.6f, 0.4f),
                _ => Color.white
            };
        }
        
        private Sprite CreateCircleSprite()
        {
            // Create a simple circle texture
            int size = 32;
            Texture2D tex = new Texture2D(size, size);
            float center = size / 2f;
            float radius = size / 2f - 1;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
                }
            }
            
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
        
        #endregion
        
        #region Playback Controls
        
        private void OnPlayPauseClicked()
        {
            _replaySystem?.TogglePlayPause();
            UpdatePlayPauseButton();
        }
        
        private void OnRestartClicked()
        {
            _replaySystem?.Restart();
            ClearMiniMapMarkers();
        }
        
        private void UpdatePlayPauseButton()
        {
            if (playPauseButton == null) return;
            
            bool isPlaying = _replaySystem?.IsPlaying ?? false;
            
            var image = playPauseButton.GetComponentInChildren<Image>();
            if (image != null)
            {
                image.sprite = isPlaying ? pauseIcon : playIcon;
            }
        }
        
        /// <summary>
        /// Update speed display
        /// </summary>
        public void UpdateSpeedDisplay(float speed)
        {
            if (playbackSpeedText != null)
            {
                playbackSpeedText.text = speed == 1f ? "1x" : $"{speed:F2}x";
            }
        }
        
        #endregion
        
        #region Tooltip
        
        private GameObject _tooltipObj;
        private TMP_Text _tooltipText;
        
        private void ShowTooltip(Vector3 position, string text)
        {
            if (_tooltipObj == null)
            {
                CreateTooltip();
            }
            
            _tooltipObj.SetActive(true);
            _tooltipText.text = text;
            
            // Position near mouse
            _tooltipObj.transform.position = position + new Vector3(10, 20, 0);
        }
        
        private void HideTooltip()
        {
            if (_tooltipObj != null)
            {
                _tooltipObj.SetActive(false);
            }
        }
        
        private void CreateTooltip()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            
            _tooltipObj = new GameObject("Tooltip");
            _tooltipObj.transform.SetParent(canvas.transform, false);
            
            var rect = _tooltipObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 40);
            rect.pivot = new Vector2(0, 0);
            
            var bg = _tooltipObj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(_tooltipObj.transform, false);
            
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8, 4);
            textRect.offsetMax = new Vector2(-8, -4);
            
            _tooltipText = textObj.AddComponent<TextMeshProUGUI>();
            _tooltipText.fontSize = 14;
            _tooltipText.color = Color.white;
            _tooltipText.alignment = TextAlignmentOptions.Left;
            
            // Content size fitter
            var fitter = _tooltipObj.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            _tooltipObj.SetActive(false);
        }
        
        #endregion
    }
}
