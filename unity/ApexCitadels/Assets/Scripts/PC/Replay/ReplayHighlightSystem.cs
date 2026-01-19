using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace ApexCitadels.PC.Replay
{
    /// <summary>
    /// Replay Highlight System - Detects and emphasizes key moments
    /// Auto slow-mo on kills, camera shake on big hits, highlight reel generation.
    /// </summary>
    public class ReplayHighlightSystem : MonoBehaviour
    {
        [Header("Auto Slow-Mo Settings")]
        [SerializeField] private bool autoSlowMoEnabled = true;
        [SerializeField] private float slowMoTimeScale = 0.3f;
        [SerializeField] private float slowMoDuration = 1.5f;
        [SerializeField] private float slowMoCooldown = 3f;
        
        [Header("Kill Cam")]
        [SerializeField] private bool killCamEnabled = true;
        [SerializeField] private float killCamZoom = 0.8f;
        [SerializeField] private int killCamMinStreak = 2;
        
        [Header("Camera Effects")]
        [SerializeField] private float shakeIntensityMultiplier = 1f;
        [SerializeField] private float screenFlashDuration = 0.15f;
        
        [Header("UI")]
        [SerializeField] private Canvas highlightCanvas;
        [SerializeField] private TMP_FontAsset highlightFont;
        
        // Singleton
        private static ReplayHighlightSystem _instance;
        public static ReplayHighlightSystem Instance => _instance;
        
        // State
        private float _lastSlowMoTime = -10f;
        private int _currentKillStreak;
        private float _lastKillTime;
        private bool _isInSlowMo;
        private List<HighlightMoment> _capturedHighlights = new List<HighlightMoment>();
        
        // References
        private ReplayCameraController _camera;
        private BattleReplaySystem _replaySystem;
        
        // UI Elements
        private GameObject _highlightTextObj;
        private TMP_Text _highlightText;
        private CanvasGroup _highlightCanvasGroup;
        private Image _screenFlash;
        private GameObject _killStreakIndicator;
        private TMP_Text _killStreakText;
        
        // Events
        public event Action<HighlightMoment> OnHighlightTriggered;
        public event Action<int> OnKillStreakUpdated;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            CreateUI();
        }
        
        private void Start()
        {
            _camera = FindFirstObjectByType<ReplayCameraController>();
            _replaySystem = FindFirstObjectByType<BattleReplaySystem>();
            
            // Subscribe to replay events
            if (_replaySystem != null)
            {
                _replaySystem.OnEventPlayed += OnBattleEvent;
            }
        }
        
        private void OnDestroy()
        {
            if (_replaySystem != null)
            {
                _replaySystem.OnEventPlayed -= OnBattleEvent;
            }
        }
        
        #region UI Creation
        
        private void CreateUI()
        {
            // Create canvas if needed
            if (highlightCanvas == null)
            {
                var canvasObj = new GameObject("HighlightCanvas");
                canvasObj.transform.SetParent(transform);
                
                highlightCanvas = canvasObj.AddComponent<Canvas>();
                highlightCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                highlightCanvas.sortingOrder = 100;
                
                canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            // Screen flash overlay
            CreateScreenFlash();
            
            // Highlight text
            CreateHighlightText();
            
            // Kill streak indicator
            CreateKillStreakIndicator();
        }
        
        private void CreateScreenFlash()
        {
            var flashObj = new GameObject("ScreenFlash");
            flashObj.transform.SetParent(highlightCanvas.transform, false);
            
            RectTransform rect = flashObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            _screenFlash = flashObj.AddComponent<Image>();
            _screenFlash.color = new Color(1, 1, 1, 0);
            _screenFlash.raycastTarget = false;
            
            flashObj.SetActive(false);
        }
        
        private void CreateHighlightText()
        {
            _highlightTextObj = new GameObject("HighlightText");
            _highlightTextObj.transform.SetParent(highlightCanvas.transform, false);
            
            RectTransform rect = _highlightTextObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.7f);
            rect.anchorMax = new Vector2(0.5f, 0.7f);
            rect.sizeDelta = new Vector2(800, 150);
            
            _highlightText = _highlightTextObj.AddComponent<TextMeshProUGUI>();
            _highlightText.fontSize = 72;
            _highlightText.fontStyle = FontStyles.Bold;
            _highlightText.alignment = TextAlignmentOptions.Center;
            _highlightText.enableAutoSizing = true;
            _highlightText.fontSizeMin = 36;
            _highlightText.fontSizeMax = 72;
            _highlightText.color = Color.white;
            _highlightText.text = "";
            
            if (highlightFont != null)
            {
                _highlightText.font = highlightFont;
            }
            
            // Add shadow/outline
            var outline = _highlightTextObj.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);
            
            _highlightCanvasGroup = _highlightTextObj.AddComponent<CanvasGroup>();
            _highlightCanvasGroup.alpha = 0;
            
            _highlightTextObj.SetActive(true);
        }
        
        private void CreateKillStreakIndicator()
        {
            _killStreakIndicator = new GameObject("KillStreakIndicator");
            _killStreakIndicator.transform.SetParent(highlightCanvas.transform, false);
            
            RectTransform rect = _killStreakIndicator.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(200, 80);
            
            // Background
            Image bg = _killStreakIndicator.AddComponent<Image>();
            bg.color = new Color(0.8f, 0.1f, 0.1f, 0.9f);
            
            // Text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(_killStreakIndicator.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            _killStreakText = textObj.AddComponent<TextMeshProUGUI>();
            _killStreakText.fontSize = 36;
            _killStreakText.fontStyle = FontStyles.Bold;
            _killStreakText.alignment = TextAlignmentOptions.Center;
            _killStreakText.color = Color.white;
            _killStreakText.text = "DOUBLE KILL!";
            
            if (highlightFont != null)
            {
                _killStreakText.font = highlightFont;
            }
            
            _killStreakIndicator.SetActive(false);
        }
        
        #endregion
        
        #region Event Handling
        
        private void OnBattleEvent(BattleEvent evt)
        {
            switch (evt.Type)
            {
                case BattleEventType.UnitDied:
                    HandleUnitDeath(evt);
                    break;
                    
                case BattleEventType.UnitAttacked:
                    HandleAttack(evt);
                    break;
                    
                case BattleEventType.BuildingDestroyed:
                    HandleBuildingDestroyed(evt);
                    break;
                    
                case BattleEventType.SpecialAbilityUsed:
                    HandleSpecialAbility(evt);
                    break;
                    
                case BattleEventType.BattleEnded:
                    HandleBattleEnd(evt);
                    break;
            }
        }
        
        private void HandleUnitDeath(BattleEvent evt)
        {
            // Update kill streak
            float timeSinceLastKill = Time.time - _lastKillTime;
            
            if (timeSinceLastKill < 2f)
            {
                _currentKillStreak++;
            }
            else
            {
                _currentKillStreak = 1;
            }
            
            _lastKillTime = Time.time;
            OnKillStreakUpdated?.Invoke(_currentKillStreak);
            
            // Trigger kill streak effects
            if (_currentKillStreak >= killCamMinStreak && killCamEnabled)
            {
                TriggerKillStreak(_currentKillStreak, evt.Position);
            }
            
            // Regular death effect
            if (_camera != null)
            {
                _camera.Shake(0.2f * shakeIntensityMultiplier, 0.2f);
            }
        }
        
        private void HandleAttack(BattleEvent evt)
        {
            // Big damage effects
            if (evt.Value > 100)
            {
                float shakeIntensity = Mathf.Clamp(evt.Value / 200f, 0.2f, 1f) * shakeIntensityMultiplier;
                
                if (_camera != null)
                {
                    _camera.Shake(shakeIntensity, 0.15f);
                }
                
                // Critical hit slow-mo
                if (evt.AbilityType == "Critical" && autoSlowMoEnabled)
                {
                    TriggerAutoSlowMo("CRITICAL HIT!", new Color(1f, 0.8f, 0f));
                }
            }
            
            // Massive damage highlight
            if (evt.Value > 200)
            {
                TriggerHighlight(new HighlightMoment
                {
                    Type = HighlightType.MassiveDamage,
                    Timestamp = evt.Timestamp,
                    Description = $"{evt.Value:N0} DAMAGE!",
                    Importance = 3
                });
            }
        }
        
        private void HandleBuildingDestroyed(BattleEvent evt)
        {
            if (_camera != null)
            {
                _camera.Shake(0.6f * shakeIntensityMultiplier, 0.4f);
                _camera.ZoomPunch(3f, 0.3f);
            }
            
            FlashScreen(new Color(1f, 0.5f, 0f, 0.3f));
            ShowHighlightText("BUILDING DESTROYED!", new Color(1f, 0.6f, 0f));
            
            if (autoSlowMoEnabled)
            {
                TriggerAutoSlowMo(null, Color.clear);
            }
            
            TriggerHighlight(new HighlightMoment
            {
                Type = HighlightType.BuildingDestroyed,
                Timestamp = evt.Timestamp,
                Description = "Building Destroyed!",
                Importance = 3
            });
        }
        
        private void HandleSpecialAbility(BattleEvent evt)
        {
            ShowHighlightText($"[!] {evt.AbilityType.ToUpper()} [!]", new Color(0.6f, 0.4f, 1f));
            
            if (_camera != null)
            {
                _camera.ZoomPunch(5f, 0.4f);
            }
            
            TriggerHighlight(new HighlightMoment
            {
                Type = HighlightType.SpecialAbility,
                Timestamp = evt.Timestamp,
                Description = evt.AbilityType,
                Importance = 4
            });
        }
        
        private void HandleBattleEnd(BattleEvent evt)
        {
            bool victory = evt.Value > 0;
            
            string text = victory ? "[T] VICTORY! [T]" : "[X] DEFEAT [X]";
            Color color = victory ? new Color(1f, 0.84f, 0f) : new Color(0.6f, 0.2f, 0.2f);
            
            ShowHighlightText(text, color, 3f);
            
            if (_camera != null)
            {
                _camera.Shake(0.8f * shakeIntensityMultiplier, 0.5f);
            }
            
            FlashScreen(victory ? new Color(1f, 0.84f, 0f, 0.5f) : new Color(0.6f, 0f, 0f, 0.5f));
            
            // Slow-mo for ending
            StartCoroutine(SlowMoSequence(0.2f, 2f));
        }
        
        #endregion
        
        #region Kill Streak
        
        private void TriggerKillStreak(int streak, Vector3 position)
        {
            string streakText = streak switch
            {
                2 => "DOUBLE KILL!",
                3 => "TRIPLE KILL!",
                4 => "MEGA KILL!",
                5 => "ULTRA KILL!",
                _ => $"{streak}x KILL STREAK!"
            };
            
            Color streakColor = streak switch
            {
                2 => new Color(1f, 0.6f, 0f),
                3 => new Color(1f, 0.3f, 0f),
                4 => new Color(1f, 0f, 0f),
                5 => new Color(1f, 0f, 0.5f),
                _ => new Color(0.8f, 0f, 0.8f)
            };
            
            // Show kill streak popup
            ShowKillStreakPopup(streakText, streakColor);
            
            // Effects
            if (_camera != null)
            {
                _camera.Shake(0.3f + streak * 0.1f, 0.3f);
                _camera.ZoomPunch(2f + streak, 0.25f);
            }
            
            FlashScreen(new Color(streakColor.r, streakColor.g, streakColor.b, 0.3f));
            
            // Slow-mo on triple+
            if (streak >= 3 && autoSlowMoEnabled)
            {
                TriggerAutoSlowMo(null, Color.clear);
            }
            
            TriggerHighlight(new HighlightMoment
            {
                Type = HighlightType.MultiKill,
                Timestamp = Time.time,
                Description = streakText,
                Importance = Mathf.Min(streak, 5)
            });
        }
        
        private void ShowKillStreakPopup(string text, Color color)
        {
            if (_killStreakIndicator == null || _killStreakText == null) return;
            
            _killStreakText.text = text;
            _killStreakIndicator.GetComponent<Image>().color = new Color(color.r * 0.3f, color.g * 0.3f, color.b * 0.3f, 0.9f);
            _killStreakText.color = color;
            
            StartCoroutine(AnimateKillStreakPopup());
        }
        
        private IEnumerator AnimateKillStreakPopup()
        {
            _killStreakIndicator.SetActive(true);
            
            RectTransform rect = _killStreakIndicator.GetComponent<RectTransform>();
            Vector2 startPos = new Vector2(0, -200);
            Vector2 endPos = Vector2.zero;
            
            // Slide in
            float elapsed = 0f;
            float inDuration = 0.2f;
            
            while (elapsed < inDuration)
            {
                float t = elapsed / inDuration;
                t = 1 - Mathf.Pow(1 - t, 3); // Ease out
                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            
            rect.anchoredPosition = endPos;
            
            // Hold
            yield return new WaitForSecondsRealtime(1.5f);
            
            // Slide out
            elapsed = 0f;
            float outDuration = 0.3f;
            
            while (elapsed < outDuration)
            {
                float t = elapsed / outDuration;
                t = t * t; // Ease in
                rect.anchoredPosition = Vector2.Lerp(endPos, startPos + new Vector2(0, 100), t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            
            _killStreakIndicator.SetActive(false);
        }
        
        #endregion
        
        #region Slow Motion
        
        /// <summary>
        /// Trigger automatic slow-mo for highlights
        /// </summary>
        public void TriggerAutoSlowMo(string text = null, Color textColor = default)
        {
            if (!autoSlowMoEnabled) return;
            if (Time.time - _lastSlowMoTime < slowMoCooldown) return;
            if (_isInSlowMo) return;
            
            _lastSlowMoTime = Time.time;
            StartCoroutine(SlowMoSequence(slowMoTimeScale, slowMoDuration, text, textColor));
        }
        
        /// <summary>
        /// Force slow-mo regardless of cooldown
        /// </summary>
        public void ForceSlowMo(float timeScale = 0.3f, float duration = 1.5f, string text = null)
        {
            StartCoroutine(SlowMoSequence(timeScale, duration, text, Color.white));
        }
        
        private IEnumerator SlowMoSequence(float targetScale, float duration, string text = null, Color textColor = default)
        {
            _isInSlowMo = true;
            float originalScale = Time.timeScale;
            
            // Show text if provided
            if (!string.IsNullOrEmpty(text))
            {
                ShowHighlightText(text, textColor, duration);
            }
            
            // Ease into slow-mo
            float elapsed = 0f;
            float transitionDuration = 0.1f;
            
            while (elapsed < transitionDuration)
            {
                float t = elapsed / transitionDuration;
                Time.timeScale = Mathf.Lerp(originalScale, targetScale, t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            
            Time.timeScale = targetScale;
            
            // Hold
            yield return new WaitForSecondsRealtime(duration);
            
            // Ease out of slow-mo
            elapsed = 0f;
            
            while (elapsed < transitionDuration)
            {
                float t = elapsed / transitionDuration;
                Time.timeScale = Mathf.Lerp(targetScale, originalScale, t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            
            Time.timeScale = originalScale;
            _isInSlowMo = false;
        }
        
        #endregion
        
        #region Visual Effects
        
        /// <summary>
        /// Show highlight text on screen
        /// </summary>
        public void ShowHighlightText(string text, Color color, float duration = 1.5f)
        {
            if (_highlightText == null || _highlightCanvasGroup == null) return;
            
            StopCoroutine(nameof(AnimateHighlightText));
            _highlightText.text = text;
            _highlightText.color = color;
            StartCoroutine(AnimateHighlightText(duration));
        }
        
        private IEnumerator AnimateHighlightText(float duration)
        {
            // Scale pop-in
            RectTransform rect = _highlightTextObj.GetComponent<RectTransform>();
            Vector3 originalScale = Vector3.one;
            rect.localScale = Vector3.one * 1.5f;
            
            // Fade in
            float fadeIn = 0.15f;
            float elapsed = 0f;
            
            while (elapsed < fadeIn)
            {
                float t = elapsed / fadeIn;
                _highlightCanvasGroup.alpha = t;
                rect.localScale = Vector3.Lerp(Vector3.one * 1.5f, originalScale, t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            
            _highlightCanvasGroup.alpha = 1;
            rect.localScale = originalScale;
            
            // Hold
            yield return new WaitForSecondsRealtime(duration);
            
            // Fade out
            float fadeOut = 0.3f;
            elapsed = 0f;
            
            while (elapsed < fadeOut)
            {
                float t = elapsed / fadeOut;
                _highlightCanvasGroup.alpha = 1 - t;
                rect.localScale = Vector3.Lerp(originalScale, Vector3.one * 0.8f, t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            
            _highlightCanvasGroup.alpha = 0;
        }
        
        /// <summary>
        /// Flash the screen with a color
        /// </summary>
        public void FlashScreen(Color color)
        {
            if (_screenFlash == null) return;
            
            StartCoroutine(DoScreenFlash(color));
        }
        
        private IEnumerator DoScreenFlash(Color color)
        {
            _screenFlash.gameObject.SetActive(true);
            _screenFlash.color = color;
            
            float elapsed = 0f;
            
            while (elapsed < screenFlashDuration)
            {
                float t = elapsed / screenFlashDuration;
                _screenFlash.color = new Color(color.r, color.g, color.b, color.a * (1 - t));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            
            _screenFlash.gameObject.SetActive(false);
        }
        
        #endregion
        
        #region Highlight Tracking
        
        private void TriggerHighlight(HighlightMoment highlight)
        {
            _capturedHighlights.Add(highlight);
            OnHighlightTriggered?.Invoke(highlight);
        }
        
        /// <summary>
        /// Get all captured highlights
        /// </summary>
        public List<HighlightMoment> GetHighlights()
        {
            return new List<HighlightMoment>(_capturedHighlights);
        }
        
        /// <summary>
        /// Get top highlights sorted by importance
        /// </summary>
        public List<HighlightMoment> GetTopHighlights(int count = 5)
        {
            var sorted = new List<HighlightMoment>(_capturedHighlights);
            sorted.Sort((a, b) => b.Importance.CompareTo(a.Importance));
            
            if (sorted.Count > count)
            {
                sorted = sorted.GetRange(0, count);
            }
            
            return sorted;
        }
        
        /// <summary>
        /// Clear captured highlights
        /// </summary>
        public void ClearHighlights()
        {
            _capturedHighlights.Clear();
            _currentKillStreak = 0;
        }
        
        #endregion
        
        #region Settings
        
        /// <summary>
        /// Enable/disable auto slow-mo
        /// </summary>
        public void SetAutoSlowMo(bool enabled)
        {
            autoSlowMoEnabled = enabled;
        }
        
        /// <summary>
        /// Enable/disable kill cam
        /// </summary>
        public void SetKillCam(bool enabled)
        {
            killCamEnabled = enabled;
        }
        
        /// <summary>
        /// Set camera shake intensity (0-2)
        /// </summary>
        public void SetShakeIntensity(float intensity)
        {
            shakeIntensityMultiplier = Mathf.Clamp(intensity, 0f, 2f);
        }
        
        #endregion
    }
}
