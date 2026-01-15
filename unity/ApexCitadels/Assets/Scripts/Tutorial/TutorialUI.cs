using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.Tutorial
{
    /// <summary>
    /// Tutorial UI Component
    /// Displays tutorial steps, highlights, and character dialog
    /// </summary>
    public class TutorialUI : MonoBehaviour
    {
        [Header("Main Container")]
        [SerializeField] private GameObject tutorialContainer;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Dialog Box")]
        [SerializeField] private GameObject dialogBox;
        [SerializeField] private RectTransform dialogBoxRect;
        [SerializeField] private Image characterPortrait;
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private TextMeshProUGUI dialogText;
        [SerializeField] private Button continueButton;
        [SerializeField] private TextMeshProUGUI continueButtonText;
        [SerializeField] private Button skipButton;

        [Header("Highlight")]
        [SerializeField] private GameObject highlightOverlay;
        [SerializeField] private RectTransform highlightMask;
        [SerializeField] private Image highlightRing;
        [SerializeField] private float highlightPulseSpeed = 2f;
        [SerializeField] private float highlightPulseAmount = 0.1f;

        [Header("Arrow")]
        [SerializeField] private GameObject arrowIndicator;
        [SerializeField] private RectTransform arrowRect;
        [SerializeField] private float arrowBobSpeed = 3f;
        [SerializeField] private float arrowBobAmount = 10f;

        [Header("Progress")]
        [SerializeField] private GameObject progressBar;
        [SerializeField] private Image progressFill;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private List<Image> stepIndicators;

        [Header("Rewards")]
        [SerializeField] private GameObject rewardsPanel;
        [SerializeField] private Transform rewardsContainer;
        [SerializeField] private GameObject rewardItemPrefab;

        [Header("Completion")]
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private TextMeshProUGUI completionTitle;
        [SerializeField] private TextMeshProUGUI completionMessage;
        [SerializeField] private ParticleSystem completionParticles;

        [Header("Typewriter Effect")]
        [SerializeField] private bool useTypewriterEffect = true;
        [SerializeField] private float typewriterSpeed = 0.03f;
        [SerializeField] private AudioClip typewriterSound;
        [SerializeField] private AudioSource audioSource;

        [Header("Animations")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.2f;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // State
        private TutorialStep _currentStep;
        private TutorialCharacter _currentCharacter;
        private Coroutine _typewriterCoroutine;
        private Coroutine _highlightCoroutine;
        private Coroutine _arrowCoroutine;
        private bool _isTyping;
        private string _fullText;

        private void Awake()
        {
            // Setup button listeners
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);

            if (skipButton != null)
                skipButton.onClick.AddListener(OnSkipClicked);

            // Hide initially
            if (tutorialContainer != null)
                tutorialContainer.SetActive(false);

            if (highlightOverlay != null)
                highlightOverlay.SetActive(false);

            if (arrowIndicator != null)
                arrowIndicator.SetActive(false);

            if (rewardsPanel != null)
                rewardsPanel.SetActive(false);

            if (completionPanel != null)
                completionPanel.SetActive(false);
        }

        /// <summary>
        /// Show the tutorial UI
        /// </summary>
        public void Show()
        {
            if (tutorialContainer != null)
            {
                tutorialContainer.SetActive(true);
                StartCoroutine(FadeIn());
            }
        }

        /// <summary>
        /// Hide the tutorial UI
        /// </summary>
        public void Hide()
        {
            StartCoroutine(FadeOutAndHide());
        }

        private IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;

            canvasGroup.alpha = 0;
            float elapsed = 0;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = fadeCurve.Evaluate(elapsed / fadeInDuration);
                yield return null;
            }

            canvasGroup.alpha = 1;
        }

        private IEnumerator FadeOutAndHide()
        {
            if (canvasGroup != null)
            {
                float elapsed = 0;

                while (elapsed < fadeOutDuration)
                {
                    elapsed += Time.deltaTime;
                    canvasGroup.alpha = 1 - fadeCurve.Evaluate(elapsed / fadeOutDuration);
                    yield return null;
                }

                canvasGroup.alpha = 0;
            }

            if (tutorialContainer != null)
                tutorialContainer.SetActive(false);
        }

        /// <summary>
        /// Display a tutorial step
        /// </summary>
        public void ShowStep(TutorialStep step, TutorialCharacter character)
        {
            _currentStep = step;
            _currentCharacter = character;

            // Hide highlight and arrow initially
            HideHighlight();
            HideArrow();
            HideRewards();

            // Update character
            if (character != null)
            {
                if (characterPortrait != null)
                {
                    characterPortrait.sprite = character.Portrait;
                    characterPortrait.gameObject.SetActive(character.Portrait != null);
                }

                if (characterNameText != null)
                {
                    characterNameText.text = character.Name;
                    characterNameText.color = character.NameColor;
                }
            }
            else
            {
                if (characterPortrait != null)
                    characterPortrait.gameObject.SetActive(false);

                if (characterNameText != null)
                    characterNameText.text = "";
            }

            // Update dialog text
            _fullText = step.Message;
            if (useTypewriterEffect && !string.IsNullOrEmpty(_fullText))
            {
                StartTypewriter(_fullText);
            }
            else if (dialogText != null)
            {
                dialogText.text = _fullText;
            }

            // Update continue button
            if (continueButtonText != null)
            {
                continueButtonText.text = string.IsNullOrEmpty(step.ButtonText) ? "Continue" : step.ButtonText;
            }

            // Show/hide skip button
            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(step.CanSkip);
            }

            // Update progress
            UpdateProgress();

            // Play voice over if available
            if (step.VoiceOver != null && audioSource != null)
            {
                audioSource.clip = step.VoiceOver;
                audioSource.Play();
            }

            // Show dialog box
            if (dialogBox != null)
                dialogBox.SetActive(true);
        }

        /// <summary>
        /// Show highlight on a UI element
        /// </summary>
        public void ShowHighlight(string targetName, float radius, Vector2 offset)
        {
            if (highlightOverlay == null || highlightMask == null) return;

            // Find target
            var target = FindTargetByName(targetName);
            if (target == null)
            {
                Debug.LogWarning($"[TutorialUI] Highlight target not found: {targetName}");
                return;
            }

            // Position highlight
            var targetRect = target.GetComponent<RectTransform>();
            if (targetRect != null)
            {
                Vector3 worldPos = targetRect.position;
                highlightMask.position = worldPos + (Vector3)offset;
                highlightMask.sizeDelta = new Vector2(radius * 2, radius * 2);
            }

            highlightOverlay.SetActive(true);

            // Start pulse animation
            if (_highlightCoroutine != null)
                StopCoroutine(_highlightCoroutine);
            _highlightCoroutine = StartCoroutine(PulseHighlight());
        }

        /// <summary>
        /// Hide highlight
        /// </summary>
        public void HideHighlight()
        {
            if (_highlightCoroutine != null)
            {
                StopCoroutine(_highlightCoroutine);
                _highlightCoroutine = null;
            }

            if (highlightOverlay != null)
                highlightOverlay.SetActive(false);
        }

        /// <summary>
        /// Show arrow pointing to target
        /// </summary>
        public void ShowArrow(string targetName, Vector2 direction)
        {
            if (arrowIndicator == null || arrowRect == null) return;

            var target = FindTargetByName(targetName);
            if (target == null) return;

            var targetRect = target.GetComponent<RectTransform>();
            if (targetRect == null) return;

            // Position arrow
            Vector3 worldPos = targetRect.position;
            arrowRect.position = worldPos + (Vector3)(direction.normalized * 100f);

            // Rotate arrow to point at target
            float angle = Mathf.Atan2(-direction.y, -direction.x) * Mathf.Rad2Deg;
            arrowRect.rotation = Quaternion.Euler(0, 0, angle);

            arrowIndicator.SetActive(true);

            // Start bob animation
            if (_arrowCoroutine != null)
                StopCoroutine(_arrowCoroutine);
            _arrowCoroutine = StartCoroutine(BobArrow(direction));
        }

        /// <summary>
        /// Hide arrow
        /// </summary>
        public void HideArrow()
        {
            if (_arrowCoroutine != null)
            {
                StopCoroutine(_arrowCoroutine);
                _arrowCoroutine = null;
            }

            if (arrowIndicator != null)
                arrowIndicator.SetActive(false);
        }

        /// <summary>
        /// Show rewards
        /// </summary>
        public void ShowRewards(List<TutorialReward> rewards)
        {
            if (rewardsPanel == null || rewardsContainer == null || rewardItemPrefab == null)
                return;

            // Clear existing
            foreach (Transform child in rewardsContainer)
            {
                Destroy(child.gameObject);
            }

            // Create reward items
            foreach (var reward in rewards)
            {
                var item = Instantiate(rewardItemPrefab, rewardsContainer);
                
                var text = item.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = $"+{reward.Amount} {FormatRewardType(reward.Type)}";
                }
            }

            rewardsPanel.SetActive(true);

            // Hide after delay
            StartCoroutine(HideRewardsAfterDelay());
        }

        /// <summary>
        /// Hide rewards
        /// </summary>
        public void HideRewards()
        {
            if (rewardsPanel != null)
                rewardsPanel.SetActive(false);
        }

        private IEnumerator HideRewardsAfterDelay()
        {
            yield return new WaitForSeconds(2f);
            HideRewards();
        }

        /// <summary>
        /// Show completion screen
        /// </summary>
        public void ShowCompletion()
        {
            if (dialogBox != null)
                dialogBox.SetActive(false);

            HideHighlight();
            HideArrow();

            if (completionPanel != null)
            {
                completionPanel.SetActive(true);

                if (completionTitle != null)
                    completionTitle.text = "Tutorial Complete!";

                if (completionMessage != null)
                    completionMessage.text = "You're ready to conquer the world!\nGood luck, Commander!";

                if (completionParticles != null)
                    completionParticles.Play();
            }
        }

        /// <summary>
        /// Update progress display
        /// </summary>
        private void UpdateProgress()
        {
            if (TutorialManager.Instance == null) return;

            float progress = TutorialManager.Instance.GetCompletionPercentage();
            int currentIndex = TutorialManager.Instance.Progress?.CurrentStepIndex ?? 0;
            int totalSteps = stepIndicators?.Count ?? 0;

            if (progressFill != null)
            {
                progressFill.fillAmount = progress / 100f;
            }

            if (progressText != null)
            {
                progressText.text = $"{Mathf.RoundToInt(progress)}%";
            }

            // Update step indicators
            if (stepIndicators != null)
            {
                for (int i = 0; i < stepIndicators.Count; i++)
                {
                    if (stepIndicators[i] != null)
                    {
                        if (i < currentIndex)
                        {
                            stepIndicators[i].color = Color.green; // Completed
                        }
                        else if (i == currentIndex)
                        {
                            stepIndicators[i].color = Color.yellow; // Current
                        }
                        else
                        {
                            stepIndicators[i].color = Color.gray; // Future
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Start typewriter effect
        /// </summary>
        private void StartTypewriter(string text)
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
            }
            _typewriterCoroutine = StartCoroutine(TypewriterEffect(text));
        }

        private IEnumerator TypewriterEffect(string text)
        {
            _isTyping = true;
            dialogText.text = "";

            foreach (char c in text)
            {
                dialogText.text += c;

                // Play sound for each character (skip spaces and punctuation)
                if (typewriterSound != null && audioSource != null && char.IsLetterOrDigit(c))
                {
                    audioSource.PlayOneShot(typewriterSound, 0.3f);
                }

                yield return new WaitForSeconds(typewriterSpeed);
            }

            _isTyping = false;
        }

        /// <summary>
        /// Skip typewriter effect
        /// </summary>
        private void SkipTypewriter()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            if (dialogText != null)
                dialogText.text = _fullText;

            _isTyping = false;
        }

        /// <summary>
        /// Highlight pulse animation
        /// </summary>
        private IEnumerator PulseHighlight()
        {
            if (highlightRing == null) yield break;

            float baseScale = highlightRing.transform.localScale.x;

            while (true)
            {
                float scale = baseScale + Mathf.Sin(Time.time * highlightPulseSpeed) * highlightPulseAmount;
                highlightRing.transform.localScale = Vector3.one * scale;
                yield return null;
            }
        }

        /// <summary>
        /// Arrow bob animation
        /// </summary>
        private IEnumerator BobArrow(Vector2 direction)
        {
            Vector3 basePos = arrowRect.position;

            while (true)
            {
                float offset = Mathf.Sin(Time.time * arrowBobSpeed) * arrowBobAmount;
                arrowRect.position = basePos + (Vector3)(direction.normalized * offset);
                yield return null;
            }
        }

        /// <summary>
        /// Find UI element by name
        /// </summary>
        private GameObject FindTargetByName(string name)
        {
            // First try to find by tag
            var byTag = GameObject.FindGameObjectWithTag(name);
            if (byTag != null) return byTag;

            // Then try to find by name
            var byName = GameObject.Find(name);
            if (byName != null) return byName;

            // Finally try to find in UI hierarchy
            var allCanvases = FindObjectsOfType<Canvas>();
            foreach (var canvas in allCanvases)
            {
                var found = FindChildByName(canvas.transform, name);
                if (found != null) return found;
            }

            return null;
        }

        private GameObject FindChildByName(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child.gameObject;

                var found = FindChildByName(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        /// <summary>
        /// Format reward type for display
        /// </summary>
        private string FormatRewardType(string type)
        {
            return type.ToLower() switch
            {
                "gems" => "Gems 💎",
                "coins" => "Coins 🪙",
                "stone" => "Stone 🪨",
                "wood" => "Wood 🪵",
                "metal" => "Metal ⚙️",
                "xp" => "XP ⭐",
                _ => type
            };
        }

        /// <summary>
        /// Continue button clicked
        /// </summary>
        private void OnContinueClicked()
        {
            if (_isTyping)
            {
                // Skip typewriter effect
                SkipTypewriter();
            }
            else
            {
                // Continue to next step
                TutorialManager.Instance?.OnContinuePressed();
            }
        }

        /// <summary>
        /// Skip button clicked
        /// </summary>
        private void OnSkipClicked()
        {
            TutorialManager.Instance?.SkipTutorial();
        }
    }
}
