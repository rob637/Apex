using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Firebase.Functions;
using Newtonsoft.Json;

namespace ApexCitadels.Tutorial
{
    /// <summary>
    /// Tutorial step types
    /// </summary>
    public enum TutorialStepType
    {
        Welcome,
        Dialog,
        Highlight,
        Action,
        ARSetup,
        BuildCitadel,
        CollectResources,
        Attack,
        JoinAlliance,
        Reward,
        Complete
    }

    /// <summary>
    /// Tutorial step completion requirements
    /// </summary>
    public enum TutorialRequirement
    {
        None,
        Tap,
        Wait,
        CompleteAction,
        ARScan,
        PlaceObject,
        CollectItem,
        AttackTarget,
        OpenPanel,
        JoinAlliance
    }

    /// <summary>
    /// Character for dialog
    /// </summary>
    [Serializable]
    public class TutorialCharacter
    {
        public string Id;
        public string Name;
        public string Title;
        public Sprite Portrait;
        public Color NameColor = Color.white;
    }

    /// <summary>
    /// Tutorial step definition
    /// </summary>
    [Serializable]
    public class TutorialStep
    {
        public string Id;
        public int Index;
        public TutorialStepType Type;
        public TutorialRequirement Requirement;
        
        [Header("Content")]
        public string CharacterId;
        public string Title;
        [TextArea(2, 5)]
        public string Message;
        public string ButtonText;
        public AudioClip VoiceOver;
        
        [Header("Highlight")]
        public string HighlightTarget;
        public Vector2 HighlightOffset;
        public float HighlightRadius = 100f;
        public bool ShowArrow = true;
        public Vector2 ArrowDirection = Vector2.down;
        
        [Header("Action")]
        public string ActionId;
        public float TimeoutSeconds = 0f;
        public bool CanSkip = true;
        
        [Header("Rewards")]
        public List<TutorialReward> Rewards;
        
        [Header("Events")]
        public UnityEvent OnStepStart;
        public UnityEvent OnStepComplete;
    }

    /// <summary>
    /// Tutorial reward
    /// </summary>
    [Serializable]
    public class TutorialReward
    {
        public string Type;
        public string ItemId;
        public int Amount;
    }

    /// <summary>
    /// Tutorial progress data
    /// </summary>
    [Serializable]
    public class TutorialProgress
    {
        public bool IsComplete;
        public string CurrentStepId;
        public int CurrentStepIndex;
        public List<string> CompletedSteps;
        public DateTime StartedAt;
        public DateTime? CompletedAt;
        public bool Skipped;
    }

    /// <summary>
    /// Complete Onboarding Tutorial Manager
    /// Guides new players through the game mechanics
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private bool enableTutorial = true;
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool autoStartForNewUsers = true;
        [SerializeField] private float stepTransitionDelay = 0.3f;

        [Header("Characters")]
        [SerializeField] private List<TutorialCharacter> characters;

        [Header("Tutorial Steps")]
        [SerializeField] private List<TutorialStep> steps;

        [Header("References")]
        [SerializeField] private TutorialUI tutorialUI;

        // Events
        public event Action OnTutorialStarted;
        public event Action<TutorialStep> OnStepStarted;
        public event Action<TutorialStep> OnStepCompleted;
        public event Action<TutorialProgress> OnTutorialCompleted;
        public event Action OnTutorialSkipped;

        // State
        private FirebaseFunctions _functions;
        private TutorialProgress _progress;
        private TutorialStep _currentStep;
        private bool _isRunning;
        private bool _isWaitingForAction;
        private Dictionary<string, TutorialCharacter> _characterMap;

        public bool IsRunning => _isRunning;
        public bool IsComplete => _progress?.IsComplete ?? false;
        public TutorialProgress Progress => _progress;
        public TutorialStep CurrentStep => _currentStep;

        private const string PREFS_PROGRESS_KEY = "tutorial_progress";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Build character map
            _characterMap = new Dictionary<string, TutorialCharacter>();
            foreach (var character in characters)
            {
                _characterMap[character.Id] = character;
            }

            // Initialize steps with indices
            for (int i = 0; i < steps.Count; i++)
            {
                steps[i].Index = i;
                if (string.IsNullOrEmpty(steps[i].Id))
                {
                    steps[i].Id = $"step_{i}";
                }
            }
        }

#if FIREBASE_ENABLED
        private void Start()
        {
            _functions = FirebaseFunctions.DefaultInstance;
            
            // Load progress
            LoadProgress();

            // Auto-start for new users
            if (autoStartForNewUsers && enableTutorial && !_progress.IsComplete && _progress.CompletedSteps.Count == 0)
            {
                StartCoroutine(DelayedStart());
            }
        }
#else
        private void Start()
        {
            Debug.LogWarning("[TutorialManager] Firebase SDK not installed. Running in stub mode.");
            
            // Load progress
            LoadProgress();

            // Auto-start for new users
            if (autoStartForNewUsers && enableTutorial && !_progress.IsComplete && _progress.CompletedSteps.Count == 0)
            {
                StartCoroutine(DelayedStart());
            }
        }
#endif

        private IEnumerator DelayedStart()
        {
            // Wait for game systems to initialize
            yield return new WaitForSeconds(1f);
            
            if (!_isRunning)
            {
                StartTutorial();
            }
        }

        /// <summary>
        /// Start the tutorial from the beginning
        /// </summary>
        public void StartTutorial()
        {
            if (!enableTutorial || _isRunning) return;

            Log("Starting tutorial");

            _isRunning = true;
            _progress.StartedAt = DateTime.UtcNow;
            _progress.CompletedSteps = new List<string>();
            _progress.CurrentStepIndex = 0;
            _progress.IsComplete = false;
            _progress.Skipped = false;

            SaveProgress();

            OnTutorialStarted?.Invoke();

            // Show tutorial UI
            if (tutorialUI != null)
            {
                tutorialUI.Show();
            }

            // Start first step
            GoToStep(0);
        }

        /// <summary>
        /// Resume tutorial from current progress
        /// </summary>
        public void ResumeTutorial()
        {
            if (!enableTutorial || _isRunning || _progress.IsComplete) return;

            Log($"Resuming tutorial from step {_progress.CurrentStepIndex}");

            _isRunning = true;

            OnTutorialStarted?.Invoke();

            if (tutorialUI != null)
            {
                tutorialUI.Show();
            }

            GoToStep(_progress.CurrentStepIndex);
        }

        /// <summary>
        /// Go to a specific step
        /// </summary>
        public void GoToStep(int index)
        {
            if (index < 0 || index >= steps.Count)
            {
                Log($"Invalid step index: {index}");
                CompleteTutorial();
                return;
            }

            _currentStep = steps[index];
            _progress.CurrentStepIndex = index;
            _progress.CurrentStepId = _currentStep.Id;

            Log($"Starting step {index}: {_currentStep.Id} ({_currentStep.Type})");

            SaveProgress();

            // Get character for this step
            TutorialCharacter character = null;
            if (!string.IsNullOrEmpty(_currentStep.CharacterId) && _characterMap.ContainsKey(_currentStep.CharacterId))
            {
                character = _characterMap[_currentStep.CharacterId];
            }

            // Update UI
            if (tutorialUI != null)
            {
                tutorialUI.ShowStep(_currentStep, character);
            }

            // Fire step start event
            _currentStep.OnStepStart?.Invoke();
            OnStepStarted?.Invoke(_currentStep);

            // Handle step based on type
            HandleStepType(_currentStep);
        }

        /// <summary>
        /// Handle step based on its type
        /// </summary>
        private void HandleStepType(TutorialStep step)
        {
            switch (step.Type)
            {
                case TutorialStepType.Welcome:
                case TutorialStepType.Dialog:
                    // Wait for user tap
                    _isWaitingForAction = step.Requirement == TutorialRequirement.Tap;
                    break;

                case TutorialStepType.Highlight:
                    // Show highlight and wait for tap
                    if (tutorialUI != null && !string.IsNullOrEmpty(step.HighlightTarget))
                    {
                        tutorialUI.ShowHighlight(step.HighlightTarget, step.HighlightRadius, step.HighlightOffset);
                        if (step.ShowArrow)
                        {
                            tutorialUI.ShowArrow(step.HighlightTarget, step.ArrowDirection);
                        }
                    }
                    _isWaitingForAction = true;
                    break;

                case TutorialStepType.Action:
                    // Wait for specific action to be completed
                    _isWaitingForAction = true;
                    if (step.TimeoutSeconds > 0)
                    {
                        StartCoroutine(StepTimeout(step.TimeoutSeconds));
                    }
                    break;

                case TutorialStepType.ARSetup:
                    // Initiate AR setup flow
                    _isWaitingForAction = true;
                    // ARManager would call CompleteCurrentStep when ready
                    break;

                case TutorialStepType.BuildCitadel:
                    // Guide user to build their first citadel
                    _isWaitingForAction = true;
                    // BuildingManager would call CompleteCurrentStep when complete
                    break;

                case TutorialStepType.CollectResources:
                    // Guide user to collect resources
                    _isWaitingForAction = true;
                    // ResourceManager would call CompleteCurrentStep when collected
                    break;

                case TutorialStepType.Attack:
                    // Guide user through first attack
                    _isWaitingForAction = true;
                    // CombatManager would call CompleteCurrentStep when done
                    break;

                case TutorialStepType.JoinAlliance:
                    // Guide user to join an alliance
                    _isWaitingForAction = true;
                    // AllianceManager would call CompleteCurrentStep when joined
                    break;

                case TutorialStepType.Reward:
                    // Grant rewards and show them
                    GrantStepRewards(step);
                    break;

                case TutorialStepType.Complete:
                    // Tutorial complete
                    StartCoroutine(CompleteTutorialSequence());
                    break;
            }
        }

        /// <summary>
        /// Complete the current step and advance
        /// </summary>
        public void CompleteCurrentStep()
        {
            if (!_isRunning || _currentStep == null) return;

            Log($"Completing step: {_currentStep.Id}");

            _isWaitingForAction = false;

            // Mark step as completed
            if (!_progress.CompletedSteps.Contains(_currentStep.Id))
            {
                _progress.CompletedSteps.Add(_currentStep.Id);
            }

            // Fire completion event
            _currentStep.OnStepComplete?.Invoke();
            OnStepCompleted?.Invoke(_currentStep);

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent(
                "tutorial_step_complete",
                new Dictionary<string, object>
                {
                    { "step_id", _currentStep.Id },
                    { "step_index", _currentStep.Index },
                    { "step_type", _currentStep.Type.ToString() }
                });

            SaveProgress();

            // Go to next step after delay
            StartCoroutine(NextStepAfterDelay());
        }

        private IEnumerator NextStepAfterDelay()
        {
            yield return new WaitForSeconds(stepTransitionDelay);

            int nextIndex = _progress.CurrentStepIndex + 1;
            
            if (nextIndex < steps.Count)
            {
                GoToStep(nextIndex);
            }
            else
            {
                CompleteTutorial();
            }
        }

        /// <summary>
        /// Called by UI when user taps continue button
        /// </summary>
        public void OnContinuePressed()
        {
            if (_isWaitingForAction && _currentStep?.Requirement == TutorialRequirement.Tap)
            {
                CompleteCurrentStep();
            }
        }

        /// <summary>
        /// Called by other systems when tutorial action is completed
        /// </summary>
        public void OnActionCompleted(string actionId)
        {
            if (!_isRunning || _currentStep == null) return;

            if (_currentStep.ActionId == actionId || string.IsNullOrEmpty(_currentStep.ActionId))
            {
                CompleteCurrentStep();
            }
        }

        /// <summary>
        /// Skip the current step (if allowed)
        /// </summary>
        public void SkipStep()
        {
            if (!_isRunning || _currentStep == null || !_currentStep.CanSkip) return;

            Log($"Skipping step: {_currentStep.Id}");

            // Track skip
            Analytics.AnalyticsManager.Instance?.TrackEvent(
                "tutorial_step_skipped",
                new Dictionary<string, object>
                {
                    { "step_id", _currentStep.Id },
                    { "step_index", _currentStep.Index }
                });

            CompleteCurrentStep();
        }

        /// <summary>
        /// Skip the entire tutorial
        /// </summary>
        public void SkipTutorial()
        {
            if (!_isRunning) return;

            Log("Skipping entire tutorial");

            _progress.Skipped = true;
            _progress.IsComplete = true;
            _progress.CompletedAt = DateTime.UtcNow;

            SaveProgress();
            SyncProgressToServer();

            _isRunning = false;
            _isWaitingForAction = false;

            if (tutorialUI != null)
            {
                tutorialUI.Hide();
            }

            OnTutorialSkipped?.Invoke();

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent(
                "tutorial_skipped",
                new Dictionary<string, object>
                {
                    { "step_index", _progress.CurrentStepIndex },
                    { "steps_completed", _progress.CompletedSteps.Count }
                });
        }

        /// <summary>
        /// Complete the tutorial
        /// </summary>
        private void CompleteTutorial()
        {
            Log("Tutorial completed!");

            _progress.IsComplete = true;
            _progress.CompletedAt = DateTime.UtcNow;

            SaveProgress();
            SyncProgressToServer();

            _isRunning = false;
            _isWaitingForAction = false;

            OnTutorialCompleted?.Invoke(_progress);

            // Track analytics
            Analytics.AnalyticsManager.Instance?.TrackEvent(
                "tutorial_completed",
                new Dictionary<string, object>
                {
                    { "total_steps", steps.Count },
                    { "duration_minutes", (_progress.CompletedAt.Value - _progress.StartedAt).TotalMinutes }
                });
        }

        private IEnumerator CompleteTutorialSequence()
        {
            // Show completion animation/message
            if (tutorialUI != null)
            {
                tutorialUI.ShowCompletion();
            }

            yield return new WaitForSeconds(3f);

            if (tutorialUI != null)
            {
                tutorialUI.Hide();
            }

            CompleteTutorial();
        }

        /// <summary>
        /// Grant rewards for a step
        /// </summary>
        private async void GrantStepRewards(TutorialStep step)
        {
            if (step.Rewards == null || step.Rewards.Count == 0)
            {
                CompleteCurrentStep();
                return;
            }

            try
            {
                // Call server to grant rewards
                var callable = _functions.GetHttpsCallable("grantTutorialRewards");
                var data = new Dictionary<string, object>
                {
                    { "stepId", step.Id },
                    { "rewards", step.Rewards }
                };

                await callable.CallAsync(data);

                Log($"Granted rewards for step {step.Id}");

                // Show rewards in UI
                if (tutorialUI != null)
                {
                    tutorialUI.ShowRewards(step.Rewards);
                }

                // Wait a moment then complete
                await Task.Delay(2000);
                CompleteCurrentStep();
            }
            catch (Exception e)
            {
                LogError($"Failed to grant rewards: {e.Message}");
                CompleteCurrentStep();
            }
        }

        /// <summary>
        /// Timeout for action steps
        /// </summary>
        private IEnumerator StepTimeout(float seconds)
        {
            yield return new WaitForSeconds(seconds);

            if (_isWaitingForAction && _currentStep != null && _currentStep.CanSkip)
            {
                Log($"Step timed out: {_currentStep.Id}");
                CompleteCurrentStep();
            }
        }

        /// <summary>
        /// Save progress locally
        /// </summary>
        private void SaveProgress()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_progress);
                PlayerPrefs.SetString(PREFS_PROGRESS_KEY, json);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                LogError($"Failed to save progress: {e.Message}");
            }
        }

        /// <summary>
        /// Load progress from local storage
        /// </summary>
        private void LoadProgress()
        {
            try
            {
                if (PlayerPrefs.HasKey(PREFS_PROGRESS_KEY))
                {
                    string json = PlayerPrefs.GetString(PREFS_PROGRESS_KEY);
                    _progress = JsonConvert.DeserializeObject<TutorialProgress>(json);
                }
                else
                {
                    _progress = new TutorialProgress
                    {
                        IsComplete = false,
                        CurrentStepIndex = 0,
                        CompletedSteps = new List<string>(),
                        StartedAt = DateTime.UtcNow
                    };
                }
            }
            catch (Exception e)
            {
                LogError($"Failed to load progress: {e.Message}");
                _progress = new TutorialProgress
                {
                    IsComplete = false,
                    CurrentStepIndex = 0,
                    CompletedSteps = new List<string>(),
                    StartedAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Sync progress to server
        /// </summary>
        private async void SyncProgressToServer()
        {
            try
            {
                var callable = _functions.GetHttpsCallable("updateTutorialProgress");
                var data = new Dictionary<string, object>
                {
                    { "isComplete", _progress.IsComplete },
                    { "currentStepIndex", _progress.CurrentStepIndex },
                    { "completedSteps", _progress.CompletedSteps },
                    { "skipped", _progress.Skipped }
                };

                await callable.CallAsync(data);
                Log("Progress synced to server");
            }
            catch (Exception e)
            {
                LogError($"Failed to sync progress: {e.Message}");
            }
        }

        /// <summary>
        /// Reset tutorial progress (for testing)
        /// </summary>
        public void ResetProgress()
        {
            _progress = new TutorialProgress
            {
                IsComplete = false,
                CurrentStepIndex = 0,
                CompletedSteps = new List<string>(),
                StartedAt = DateTime.UtcNow
            };

            SaveProgress();
            Log("Tutorial progress reset");
        }

        /// <summary>
        /// Check if a specific step is completed
        /// </summary>
        public bool IsStepCompleted(string stepId)
        {
            return _progress?.CompletedSteps?.Contains(stepId) ?? false;
        }

        /// <summary>
        /// Get completion percentage
        /// </summary>
        public float GetCompletionPercentage()
        {
            if (steps.Count == 0) return 0;
            return (_progress?.CompletedSteps?.Count ?? 0) / (float)steps.Count * 100f;
        }

        /// <summary>
        /// Get character by ID
        /// </summary>
        public TutorialCharacter GetCharacter(string id)
        {
            return _characterMap.TryGetValue(id, out var character) ? character : null;
        }

        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[TutorialManager] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[TutorialManager] {message}");
        }
    }
}
