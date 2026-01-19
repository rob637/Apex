using UnityEngine;
using System.Collections;
using ApexCitadels.Core;

namespace ApexCitadels.PC
{
    /// <summary>
    /// Handles visual feedback for territory interaction:
    /// - Hover highlight
    /// - Selection outline
    /// - Click ripple effect
    /// - Damage flash
    /// </summary>
    public class TerritoryInteractionFeedback : MonoBehaviour
    {
        [Header("Hover Effect")]
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float hoverScaleSpeed = 8f;
        [SerializeField] private Color hoverTint = new Color(1.2f, 1.2f, 1.2f, 1f);
        
        [Header("Selection Effect")]
        [SerializeField] private Color selectionColor = new Color(1f, 0.9f, 0.3f, 1f);
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseIntensity = 0.3f;
        
        [Header("Click Effect")]
        [SerializeField] private float clickScalePunch = 0.1f;
        [SerializeField] private float clickDuration = 0.2f;
        
        // State
        private bool _isHovered;
        private bool _isSelected;
        private Vector3 _originalScale;
        private Vector3 _targetScale;
        private Renderer[] _renderers;
        private Color[] _originalColors;
        private MaterialPropertyBlock _propertyBlock;
        
        // Coroutine handles
        private Coroutine _pulseCoroutine;
        private Coroutine _clickCoroutine;

        private void Start()
        {
            _originalScale = transform.localScale;
            _targetScale = _originalScale;
            
            // Get all renderers for color effects
            _renderers = GetComponentsInChildren<Renderer>();
            _originalColors = new Color[_renderers.Length];
            
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i].material != null)
                {
                    _originalColors[i] = _renderers[i].material.color;
                }
            }
            
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            // Smooth scale interpolation
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * hoverScaleSpeed);
        }

        /// <summary>
        /// Called when mouse enters territory
        /// </summary>
        public void OnHoverEnter()
        {
            if (_isHovered) return;
            _isHovered = true;
            
            _targetScale = _originalScale * hoverScale;
            ApplyTint(hoverTint);
            
            // Show tooltip with territory name
            ShowTooltip();
        }

        /// <summary>
        /// Called when mouse exits territory
        /// </summary>
        public void OnHoverExit()
        {
            if (!_isHovered) return;
            _isHovered = false;
            
            if (!_isSelected)
            {
                _targetScale = _originalScale;
                RemoveTint();
            }
            
            HideTooltip();
        }

        /// <summary>
        /// Called when territory is selected (clicked)
        /// </summary>
        public void OnSelect()
        {
            _isSelected = true;
            
            // Play click punch effect
            if (_clickCoroutine != null)
                StopCoroutine(_clickCoroutine);
            _clickCoroutine = StartCoroutine(ClickPunchEffect());
            
            // Start selection pulse
            if (_pulseCoroutine != null)
                StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = StartCoroutine(SelectionPulse());
            
            // Play selection sound
            PlaySound("select");
        }

        /// <summary>
        /// Called when territory is deselected
        /// </summary>
        public void OnDeselect()
        {
            _isSelected = false;
            
            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = null;
            }
            
            _targetScale = _originalScale;
            RemoveTint();
        }

        /// <summary>
        /// Called when territory takes damage
        /// </summary>
        public void OnDamage()
        {
            StartCoroutine(DamageFlash());
            PlaySound("damage");
        }

        /// <summary>
        /// Called when territory is captured
        /// </summary>
        public void OnCaptured(bool byPlayer)
        {
            StartCoroutine(CaptureEffect(byPlayer));
            PlaySound(byPlayer ? "capture_win" : "capture_lose");
        }

        private IEnumerator ClickPunchEffect()
        {
            // Quick scale punch
            Vector3 punchScale = _originalScale * (1f + clickScalePunch);
            transform.localScale = punchScale;
            
            float elapsed = 0f;
            while (elapsed < clickDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / clickDuration;
                transform.localScale = Vector3.Lerp(punchScale, _isSelected ? _originalScale * hoverScale : _originalScale, t);
                yield return null;
            }
        }

        private IEnumerator SelectionPulse()
        {
            float time = 0f;
            
            while (_isSelected)
            {
                time += Time.deltaTime * pulseSpeed;
                float pulse = Mathf.Sin(time) * pulseIntensity;
                
                // Apply pulse to all renderers
                for (int i = 0; i < _renderers.Length; i++)
                {
                    if (_renderers[i] == null) continue;
                    
                    Color baseColor = _originalColors[i];
                    Color pulseColor = Color.Lerp(baseColor, selectionColor, 0.5f + pulse);
                    
                    _renderers[i].GetPropertyBlock(_propertyBlock);
                    _propertyBlock.SetColor("_Color", pulseColor);
                    _propertyBlock.SetColor("_BaseColor", pulseColor); // URP
                    _renderers[i].SetPropertyBlock(_propertyBlock);
                }
                
                yield return null;
            }
            
            // Reset colors
            RemoveTint();
        }

        private IEnumerator DamageFlash()
        {
            Color flashColor = Color.red;
            
            // Flash red
            ApplyTint(flashColor);
            yield return new WaitForSeconds(0.1f);
            
            // Flash white
            ApplyTint(Color.white);
            yield return new WaitForSeconds(0.1f);
            
            // Flash red again
            ApplyTint(flashColor);
            yield return new WaitForSeconds(0.1f);
            
            // Return to normal
            if (_isSelected)
            {
                // Restart pulse
                if (_pulseCoroutine != null)
                    StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = StartCoroutine(SelectionPulse());
            }
            else
            {
                RemoveTint();
            }
        }

        private IEnumerator CaptureEffect(bool victory)
        {
            Color effectColor = victory ? Color.green : Color.red;
            float duration = 1f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Scale up and fade
                transform.localScale = _originalScale * (1f + t * 0.2f);
                ApplyTint(Color.Lerp(effectColor, _originalColors[0], t));
                
                yield return null;
            }
            
            transform.localScale = _originalScale;
            RemoveTint();
        }

        private void ApplyTint(Color tint)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                
                Color tintedColor = _originalColors[i] * tint;
                
                _renderers[i].GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_Color", tintedColor);
                _propertyBlock.SetColor("_BaseColor", tintedColor); // URP
                _renderers[i].SetPropertyBlock(_propertyBlock);
            }
        }

        private void RemoveTint()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                
                _renderers[i].GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_Color", _originalColors[i]);
                _propertyBlock.SetColor("_BaseColor", _originalColors[i]); // URP
                _renderers[i].SetPropertyBlock(_propertyBlock);
            }
        }

        private void ShowTooltip()
        {
            // Get territory data
            var visual = GetComponent<TerritoryVisual>();
            if (visual != null)
            {
                string tooltipText = $"{visual.TerritoryName}\nLevel {visual.TerritoryLevel}";
                
                // TODO: Show via PCUIManager tooltip system
                ApexLogger.LogVerbose(LogCategory.UI, $"Tooltip: {tooltipText}");
            }
        }

        private void HideTooltip()
        {
            // TODO: Hide via PCUIManager
        }

        private void PlaySound(string soundName)
        {
            // TODO: Play via audio manager
            ApexLogger.LogVerbose(LogCategory.UI, $"Sound: {soundName}");
        }
    }
}
