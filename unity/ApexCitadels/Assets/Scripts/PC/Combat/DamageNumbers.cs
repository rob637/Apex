using Camera = UnityEngine.Camera;
// ============================================================================
// APEX CITADELS - DAMAGE NUMBERS SYSTEM
// Floating damage/heal numbers that pop up during combat
// ============================================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace ApexCitadels.PC.Combat
{
    /// <summary>
    /// Displays floating damage numbers, heal amounts, and combat text.
    /// Uses object pooling for performance.
    /// </summary>
    public class DamageNumbers : MonoBehaviour
    {
        public static DamageNumbers Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int poolSize = 30;
        [SerializeField] private float floatSpeed = 2f;
        [SerializeField] private float floatDuration = 1.2f;
        [SerializeField] private float scatterRadius = 0.5f;
        [SerializeField] private float criticalSizeMultiplier = 1.5f;

        [Header("Colors")]
        [SerializeField] private Color damageColor = new Color(1f, 0.3f, 0.2f);
        [SerializeField] private Color criticalColor = new Color(1f, 0.85f, 0f);
        [SerializeField] private Color healColor = new Color(0.2f, 1f, 0.4f);
        [SerializeField] private Color blockColor = new Color(0.5f, 0.5f, 0.6f);
        [SerializeField] private Color resourceColor = new Color(0.3f, 0.7f, 1f);
        [SerializeField] private Color xpColor = new Color(0.8f, 0.4f, 1f);

        [Header("Fonts")]
        [SerializeField] private TMP_FontAsset combatFont;

        // Pool
        private Queue<DamageNumberInstance> _pool = new Queue<DamageNumberInstance>();
        private Canvas _canvas;
        private Camera _mainCamera;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupCanvas();
            InitializePool();
        }

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        private void SetupCanvas()
        {
            // Create world-space canvas for damage numbers
            GameObject canvasObj = new GameObject("DamageNumbersCanvas");
            canvasObj.transform.SetParent(transform);
            
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.sortingOrder = 100;

            canvasObj.AddComponent<CanvasScaler>();
        }

        private void InitializePool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                _pool.Enqueue(CreateDamageNumber());
            }
        }

        private DamageNumberInstance CreateDamageNumber()
        {
            GameObject obj = new GameObject("DamageNumber");
            obj.transform.SetParent(_canvas.transform);
            obj.SetActive(false);

            // Text component
            TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
            if (combatFont != null) text.font = combatFont;
            text.fontSize = 2;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;

            // Outline for readability
            text.outlineWidth = 0.2f;
            text.outlineColor = Color.black;

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(10, 3);

            return new DamageNumberInstance { Object = obj, Text = text, RectTransform = rect };
        }

        #region Public API

        /// <summary>
        /// Show damage number at world position
        /// </summary>
        public void ShowDamage(Vector3 worldPosition, int amount, bool isCritical = false)
        {
            var instance = GetFromPool();
            
            instance.Text.text = $"-{amount}";
            instance.Text.color = isCritical ? criticalColor : damageColor;
            instance.Text.fontSize = isCritical ? 3f : 2f;
            
            StartCoroutine(AnimateDamageNumber(instance, worldPosition, isCritical));
        }

        /// <summary>
        /// Show heal number at world position
        /// </summary>
        public void ShowHeal(Vector3 worldPosition, int amount)
        {
            var instance = GetFromPool();
            
            instance.Text.text = $"+{amount}";
            instance.Text.color = healColor;
            instance.Text.fontSize = 2f;
            
            StartCoroutine(AnimateDamageNumber(instance, worldPosition, false, true));
        }

        /// <summary>
        /// Show blocked damage
        /// </summary>
        public void ShowBlock(Vector3 worldPosition)
        {
            var instance = GetFromPool();
            
            instance.Text.text = "BLOCKED!";
            instance.Text.color = blockColor;
            instance.Text.fontSize = 1.8f;
            
            StartCoroutine(AnimateDamageNumber(instance, worldPosition, false));
        }

        /// <summary>
        /// Show miss
        /// </summary>
        public void ShowMiss(Vector3 worldPosition)
        {
            var instance = GetFromPool();
            
            instance.Text.text = "MISS";
            instance.Text.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            instance.Text.fontSize = 1.5f;
            
            StartCoroutine(AnimateDamageNumber(instance, worldPosition, false));
        }

        /// <summary>
        /// Show resource gain
        /// </summary>
        public void ShowResourceGain(Vector3 worldPosition, string resourceName, int amount)
        {
            var instance = GetFromPool();
            
            string icon = resourceName.ToLower() switch
            {
                "gold" => "🪙",
                "stone" => "🪨",
                "wood" => "🪵",
                "metal" => "⚙️",
                "crystal" => "💎",
                _ => ""
            };
            
            instance.Text.text = $"{icon}+{amount}";
            instance.Text.color = resourceColor;
            instance.Text.fontSize = 1.8f;
            
            StartCoroutine(AnimateDamageNumber(instance, worldPosition, false, true));
        }

        /// <summary>
        /// Show XP gain
        /// </summary>
        public void ShowXPGain(Vector3 worldPosition, int amount)
        {
            var instance = GetFromPool();
            
            instance.Text.text = $"+{amount} XP";
            instance.Text.color = xpColor;
            instance.Text.fontSize = 1.6f;
            
            StartCoroutine(AnimateDamageNumber(instance, worldPosition, false, true));
        }

        /// <summary>
        /// Show custom combat text
        /// </summary>
        public void ShowCombatText(Vector3 worldPosition, string text, Color color, float size = 2f, bool critical = false)
        {
            var instance = GetFromPool();
            
            instance.Text.text = text;
            instance.Text.color = color;
            instance.Text.fontSize = size;
            
            StartCoroutine(AnimateDamageNumber(instance, worldPosition, critical));
        }

        /// <summary>
        /// Show combo multiplier
        /// </summary>
        public void ShowCombo(Vector3 worldPosition, int comboCount)
        {
            var instance = GetFromPool();
            
            instance.Text.text = $"{comboCount}x COMBO!";
            instance.Text.color = Color.Lerp(criticalColor, Color.red, (comboCount - 2) / 8f);
            instance.Text.fontSize = 2f + (comboCount * 0.2f);
            
            StartCoroutine(AnimateDamageNumber(instance, worldPosition, true));
        }

        /// <summary>
        /// Show kill confirmation
        /// </summary>
        public void ShowKill(Vector3 worldPosition, string unitName = null)
        {
            var instance = GetFromPool();
            
            instance.Text.text = unitName != null ? $"☠ {unitName}" : "☠ KILL";
            instance.Text.color = new Color(1f, 0.2f, 0.2f);
            instance.Text.fontSize = 2.2f;
            
            StartCoroutine(AnimateDamageNumber(instance, worldPosition, true));
        }

        #endregion

        #region Animation

        private IEnumerator AnimateDamageNumber(DamageNumberInstance instance, Vector3 worldPos, bool isCritical, bool floatUp = false)
        {
            instance.Object.SetActive(true);
            
            // Random scatter
            Vector3 scatter = new Vector3(
                Random.Range(-scatterRadius, scatterRadius),
                Random.Range(0, scatterRadius * 0.5f),
                Random.Range(-scatterRadius, scatterRadius)
            );
            
            Vector3 startPos = worldPos + scatter;
            Vector3 endPos = startPos + Vector3.up * (floatUp ? 3f : 2f);
            
            float elapsed = 0f;
            float duration = floatDuration;
            
            // Critical numbers last longer and have different animation
            if (isCritical)
            {
                duration *= 1.3f;
            }
            
            // Initial scale punch for critical
            float startScale = isCritical ? 0.2f : 0.5f;
            float maxScale = isCritical ? criticalSizeMultiplier : 1f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Position - ease out
                float posT = 1f - Mathf.Pow(1f - t, 3f);
                Vector3 currentPos = Vector3.Lerp(startPos, endPos, posT);
                instance.Object.transform.position = currentPos;
                
                // Scale - punch then shrink
                float scale;
                if (t < 0.2f)
                {
                    // Quick scale up
                    scale = Mathf.Lerp(startScale, maxScale * 1.2f, t / 0.2f);
                }
                else if (t < 0.4f)
                {
                    // Settle to max
                    scale = Mathf.Lerp(maxScale * 1.2f, maxScale, (t - 0.2f) / 0.2f);
                }
                else
                {
                    // Shrink out
                    scale = Mathf.Lerp(maxScale, 0f, (t - 0.4f) / 0.6f);
                }
                instance.Object.transform.localScale = Vector3.one * scale;
                
                // Alpha fade
                float alpha = t < 0.7f ? 1f : 1f - ((t - 0.7f) / 0.3f);
                Color color = instance.Text.color;
                color.a = alpha;
                instance.Text.color = color;
                
                // Billboard - face camera
                if (_mainCamera != null)
                {
                    instance.Object.transform.rotation = _mainCamera.transform.rotation;
                }
                
                yield return null;
            }
            
            ReturnToPool(instance);
        }

        #endregion

        #region Pool Management

        private DamageNumberInstance GetFromPool()
        {
            DamageNumberInstance instance;
            if (_pool.Count > 0)
            {
                instance = _pool.Dequeue();
            }
            else
            {
                instance = CreateDamageNumber();
            }
            return instance;
        }

        private void ReturnToPool(DamageNumberInstance instance)
        {
            instance.Object.SetActive(false);
            instance.Object.transform.localScale = Vector3.one;
            _pool.Enqueue(instance);
        }

        #endregion

        private class DamageNumberInstance
        {
            public GameObject Object;
            public TextMeshProUGUI Text;
            public RectTransform RectTransform;
        }
    }
}
