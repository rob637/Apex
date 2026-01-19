using Camera = UnityEngine.Camera;
// ============================================================================
// APEX CITADELS - COMBAT CAMERA EFFECTS
// Screen shake, flash, slow-motion, focus effects for AAA combat feel
// ============================================================================
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

namespace ApexCitadels.PC.Combat
{
    /// <summary>
    /// Camera effects that enhance combat feedback.
    /// Works with both standard cameras and URP post-processing.
    /// </summary>
    public class CombatCameraEffects : MonoBehaviour
    {
        public static CombatCameraEffects Instance { get; private set; }

        [Header("Screen Shake")]
        [SerializeField] private float shakeDecay = 8f;
        [SerializeField] private float maxShakeIntensity = 0.5f;

        [Header("Hit Flash")]
        [SerializeField] private float flashDuration = 0.1f;
        [SerializeField] private Color damageFlashColor = new Color(1f, 0.2f, 0.1f, 0.3f);
        [SerializeField] private Color healFlashColor = new Color(0.2f, 1f, 0.3f, 0.2f);

        [Header("Slow Motion")]
        [SerializeField] private float hitPauseScale = 0.1f;
        [SerializeField] private float hitPauseDuration = 0.05f;

        [Header("Focus Effect")]
        [SerializeField] private float focusZoomAmount = 0.9f;
        [SerializeField] private float focusDuration = 0.3f;

        [Header("Chromatic Aberration")]
        [SerializeField] private float chromaticIntensity = 0.5f;
        [SerializeField] private float chromaticDuration = 0.2f;

        // State
        private Camera _camera;
        private Transform _cameraTransform;
        private Vector3 _originalPosition;
        private float _currentShake = 0f;
        private bool _isFlashing = false;
        
        // Post-processing
        private Volume _postProcessVolume;
        private Vignette _vignette;
        private ChromaticAberration _chromaticAberration;
        private ColorAdjustments _colorAdjustments;
        
        // Flash overlay
        private GameObject _flashOverlay;
        private CanvasGroup _flashCanvasGroup;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            _camera = Camera.main;
            if (_camera != null)
            {
                _cameraTransform = _camera.transform;
                _originalPosition = _cameraTransform.localPosition;
            }

            SetupPostProcessing();
            SetupFlashOverlay();
        }

        private void Update()
        {
            ApplyShake();
        }

        private void SetupPostProcessing()
        {
            // Try to find existing volume or create one
            _postProcessVolume = FindFirstObjectByType<Volume>();
            
            if (_postProcessVolume == null)
            {
                GameObject volumeObj = new GameObject("CombatPostProcess");
                volumeObj.transform.SetParent(transform);
                _postProcessVolume = volumeObj.AddComponent<Volume>();
                _postProcessVolume.isGlobal = true;
                _postProcessVolume.priority = 10;
                _postProcessVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            }

            // Get or add effects
            if (!_postProcessVolume.profile.TryGet(out _vignette))
            {
                _vignette = _postProcessVolume.profile.Add<Vignette>();
            }
            
            if (!_postProcessVolume.profile.TryGet(out _chromaticAberration))
            {
                _chromaticAberration = _postProcessVolume.profile.Add<ChromaticAberration>();
            }
            
            if (!_postProcessVolume.profile.TryGet(out _colorAdjustments))
            {
                _colorAdjustments = _postProcessVolume.profile.Add<ColorAdjustments>();
            }
        }

        private void SetupFlashOverlay()
        {
            // Create screen flash overlay
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("CombatEffectsCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999;
            }

            _flashOverlay = new GameObject("FlashOverlay");
            _flashOverlay.transform.SetParent(canvas.transform, false);

            RectTransform rect = _flashOverlay.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            UnityEngine.UI.Image image = _flashOverlay.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.clear;
            image.raycastTarget = false;

            _flashCanvasGroup = _flashOverlay.AddComponent<CanvasGroup>();
            _flashCanvasGroup.alpha = 0f;
            _flashCanvasGroup.blocksRaycasts = false;
            _flashCanvasGroup.interactable = false;
        }

        #region Public API

        /// <summary>
        /// Shake the camera (hit impact, explosion)
        /// </summary>
        public void Shake(float intensity = 0.3f)
        {
            _currentShake = Mathf.Min(_currentShake + intensity, maxShakeIntensity);
        }

        /// <summary>
        /// Strong shake for big impacts
        /// </summary>
        public void HeavyShake()
        {
            Shake(0.5f);
        }

        /// <summary>
        /// Flash the screen red (player took damage)
        /// </summary>
        public void DamageFlash()
        {
            if (!_isFlashing)
            {
                StartCoroutine(FlashScreen(damageFlashColor));
            }
        }

        /// <summary>
        /// Flash the screen green (healed)
        /// </summary>
        public void HealFlash()
        {
            if (!_isFlashing)
            {
                StartCoroutine(FlashScreen(healFlashColor));
            }
        }

        /// <summary>
        /// Flash custom color
        /// </summary>
        public void Flash(Color color)
        {
            if (!_isFlashing)
            {
                StartCoroutine(FlashScreen(color));
            }
        }

        /// <summary>
        /// Brief time slowdown for big hits
        /// </summary>
        public void HitPause()
        {
            StartCoroutine(DoHitPause());
        }

        /// <summary>
        /// Focus zoom on target
        /// </summary>
        public void FocusOn(Vector3 targetPosition)
        {
            StartCoroutine(DoFocusZoom(targetPosition));
        }

        /// <summary>
        /// Chromatic aberration pulse (critical hit, ability)
        /// </summary>
        public void ChromaticPulse()
        {
            StartCoroutine(DoChromaticPulse());
        }

        /// <summary>
        /// Vignette pulse (tension, low health)
        /// </summary>
        public void VignettePulse(Color color)
        {
            StartCoroutine(DoVignettePulse(color));
        }

        /// <summary>
        /// Combined effects for critical hit
        /// </summary>
        public void CriticalHitEffect()
        {
            HitPause();
            Shake(0.4f);
            ChromaticPulse();
            Flash(new Color(1f, 0.9f, 0.2f, 0.2f)); // Gold flash
        }

        /// <summary>
        /// Combined effects for player damage
        /// </summary>
        public void PlayerDamageEffect(float damagePercent)
        {
            Shake(0.2f + damagePercent * 0.3f);
            DamageFlash();
            
            if (damagePercent > 0.3f)
            {
                VignettePulse(Color.red);
            }
        }

        /// <summary>
        /// Victory celebration effects
        /// </summary>
        public void VictoryEffect()
        {
            StartCoroutine(VictorySequence());
        }

        /// <summary>
        /// Defeat effects
        /// </summary>
        public void DefeatEffect()
        {
            StartCoroutine(DefeatSequence());
        }

        /// <summary>
        /// Explosion impact effects
        /// </summary>
        public void ExplosionImpact(float intensity = 0.5f)
        {
            HeavyShake();
            ChromaticPulse();
            Flash(new Color(1f, 0.6f, 0.2f, 0.25f)); // Orange flash
        }

        #endregion

        #region Implementation

        private void ApplyShake()
        {
            if (_cameraTransform == null) return;
            
            if (_currentShake > 0.01f)
            {
                Vector3 shake = Random.insideUnitSphere * _currentShake;
                shake.z = 0; // Don't move forward/back
                _cameraTransform.localPosition = _originalPosition + shake;
                
                _currentShake = Mathf.Lerp(_currentShake, 0f, Time.unscaledDeltaTime * shakeDecay);
            }
            else if (_cameraTransform.localPosition != _originalPosition)
            {
                _cameraTransform.localPosition = _originalPosition;
                _currentShake = 0f;
            }
        }

        private IEnumerator FlashScreen(Color color)
        {
            _isFlashing = true;
            
            var image = _flashOverlay.GetComponent<UnityEngine.UI.Image>();
            image.color = color;
            
            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / flashDuration;
                
                // Quick fade in, slow fade out
                float alpha;
                if (t < 0.3f)
                {
                    alpha = t / 0.3f;
                }
                else
                {
                    alpha = 1f - ((t - 0.3f) / 0.7f);
                }
                
                _flashCanvasGroup.alpha = alpha * color.a;
                yield return null;
            }
            
            _flashCanvasGroup.alpha = 0f;
            _isFlashing = false;
        }

        private IEnumerator DoHitPause()
        {
            float originalTimeScale = Time.timeScale;
            Time.timeScale = hitPauseScale;
            
            yield return new WaitForSecondsRealtime(hitPauseDuration);
            
            Time.timeScale = originalTimeScale;
        }

        private IEnumerator DoFocusZoom(Vector3 target)
        {
            if (_camera == null) yield break;
            
            float originalFOV = _camera.fieldOfView;
            float targetFOV = originalFOV * focusZoomAmount;
            
            float elapsed = 0f;
            while (elapsed < focusDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / focusDuration;
                
                // Ease out and back
                float curve;
                if (t < 0.5f)
                {
                    curve = 1f - Mathf.Pow(1f - (t * 2f), 2f);
                }
                else
                {
                    curve = 1f - Mathf.Pow((t - 0.5f) * 2f, 2f);
                }
                
                _camera.fieldOfView = Mathf.Lerp(originalFOV, targetFOV, curve);
                yield return null;
            }
            
            _camera.fieldOfView = originalFOV;
        }

        private IEnumerator DoChromaticPulse()
        {
            if (_chromaticAberration == null) yield break;
            
            _chromaticAberration.active = true;
            _chromaticAberration.intensity.Override(0f);
            
            float elapsed = 0f;
            while (elapsed < chromaticDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / chromaticDuration;
                
                // Quick spike then decay
                float intensity;
                if (t < 0.2f)
                {
                    intensity = (t / 0.2f) * chromaticIntensity;
                }
                else
                {
                    intensity = Mathf.Lerp(chromaticIntensity, 0f, (t - 0.2f) / 0.8f);
                }
                
                _chromaticAberration.intensity.Override(intensity);
                yield return null;
            }
            
            _chromaticAberration.intensity.Override(0f);
        }

        private IEnumerator DoVignettePulse(Color color)
        {
            if (_vignette == null) yield break;
            
            _vignette.active = true;
            _vignette.color.Override(color);
            
            float elapsed = 0f;
            float duration = 0.5f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                
                // Pulse
                float intensity = Mathf.Sin(t * Mathf.PI) * 0.4f;
                _vignette.intensity.Override(0.3f + intensity);
                
                yield return null;
            }
            
            _vignette.intensity.Override(0.3f);
        }

        private IEnumerator VictorySequence()
        {
            // Slow motion
            Time.timeScale = 0.3f;
            yield return new WaitForSecondsRealtime(0.2f);
            
            // Golden flash
            Flash(new Color(1f, 0.85f, 0.2f, 0.35f));
            
            // Zoom
            if (_camera != null)
            {
                float startFOV = _camera.fieldOfView;
                float elapsed = 0f;
                while (elapsed < 0.5f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    _camera.fieldOfView = Mathf.Lerp(startFOV, startFOV * 0.85f, elapsed / 0.5f);
                    yield return null;
                }
            }
            
            // Return to normal
            yield return new WaitForSecondsRealtime(0.3f);
            Time.timeScale = 1f;
            
            // Restore camera
            if (_camera != null)
            {
                float startFOV = _camera.fieldOfView;
                float targetFOV = 60f; // Default
                float elapsed = 0f;
                while (elapsed < 0.3f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    _camera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, elapsed / 0.3f);
                    yield return null;
                }
            }
        }

        private IEnumerator DefeatSequence()
        {
            // Slow down
            Time.timeScale = 0.5f;
            
            // Red vignette
            if (_vignette != null)
            {
                _vignette.active = true;
                _vignette.color.Override(new Color(0.5f, 0f, 0f));
                _vignette.intensity.Override(0.5f);
            }
            
            // Desaturate
            if (_colorAdjustments != null)
            {
                _colorAdjustments.active = true;
                float elapsed = 0f;
                while (elapsed < 0.5f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    _colorAdjustments.saturation.Override(Mathf.Lerp(0f, -50f, elapsed / 0.5f));
                    yield return null;
                }
            }
            
            yield return new WaitForSecondsRealtime(0.5f);
            
            // Flash to black
            Flash(new Color(0f, 0f, 0f, 0.5f));
            
            yield return new WaitForSecondsRealtime(0.3f);
            
            // Restore
            Time.timeScale = 1f;
            
            if (_colorAdjustments != null)
            {
                float elapsed = 0f;
                while (elapsed < 0.5f)
                {
                    elapsed += Time.deltaTime;
                    _colorAdjustments.saturation.Override(Mathf.Lerp(-50f, 0f, elapsed / 0.5f));
                    yield return null;
                }
            }
            
            if (_vignette != null)
            {
                _vignette.intensity.Override(0.3f);
            }
        }

        #endregion

        private void OnDestroy()
        {
            // Ensure time scale is reset
            Time.timeScale = 1f;
        }
    }
}
