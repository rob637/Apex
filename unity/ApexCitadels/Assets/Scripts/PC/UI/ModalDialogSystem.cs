using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Modal dialog system for confirmations, choices, and information.
    /// Features:
    /// - Confirmation dialogs
    /// - Input dialogs
    /// - Choice dialogs
    /// - Information/Alert dialogs
    /// - Queue system for multiple dialogs
    /// - Animation and backdrop management
    /// </summary>
    public class ModalDialogSystem : MonoBehaviour
    {
        [Header("Dialog Prefabs")]
        [SerializeField] private RectTransform confirmDialog;
        [SerializeField] private RectTransform inputDialog;
        [SerializeField] private RectTransform choiceDialog;
        [SerializeField] private RectTransform alertDialog;
        
        [Header("Backdrop")]
        [SerializeField] private Image backdrop;
        [SerializeField] private Color backdropColor = new Color(0, 0, 0, 0.7f);
        
        [Header("Animation Settings")]
        [SerializeField] private float showDuration = 0.3f;
        [SerializeField] private float hideDuration = 0.2f;
        [SerializeField] private AnimationCurve showCurve;
        [SerializeField] private AnimationCurve hideCurve;
        
        [Header("Sound Effects")]
        [SerializeField] private AudioClip showSound;
        [SerializeField] private AudioClip hideSound;
        [SerializeField] private AudioClip confirmSound;
        [SerializeField] private AudioClip cancelSound;
        
        // Singleton
        private static ModalDialogSystem _instance;
        public static ModalDialogSystem Instance => _instance;
        
        // State
        private bool _isDialogActive;
        private DialogInstance _currentDialog;
        private Queue<PendingDialog> _dialogQueue = new Queue<PendingDialog>();
        
        // Audio
        private AudioSource _audioSource;
        
        // Events
        public event Action<DialogResult> OnDialogClosed;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            InitializeAnimationCurves();
            SetupAudio();
            HideAllDialogs();
        }
        
        private void InitializeAnimationCurves()
        {
            if (showCurve == null || showCurve.length == 0)
            {
                // Ease out back
                showCurve = new AnimationCurve();
                showCurve.AddKey(0, 0);
                showCurve.AddKey(0.6f, 1.1f);
                showCurve.AddKey(1, 1);
            }
            
            if (hideCurve == null || hideCurve.length == 0)
            {
                hideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            }
        }
        
        private void SetupAudio()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _audioSource.playOnAwake = false;
        }
        
        private void HideAllDialogs()
        {
            if (confirmDialog != null) confirmDialog.gameObject.SetActive(false);
            if (inputDialog != null) inputDialog.gameObject.SetActive(false);
            if (choiceDialog != null) choiceDialog.gameObject.SetActive(false);
            if (alertDialog != null) alertDialog.gameObject.SetActive(false);
            if (backdrop != null)
            {
                backdrop.color = new Color(backdropColor.r, backdropColor.g, backdropColor.b, 0);
                backdrop.raycastTarget = false;
            }
        }
        
        #region Public API - Confirmation Dialog
        
        /// <summary>
        /// Show confirmation dialog with OK/Cancel buttons
        /// </summary>
        public void ShowConfirm(string title, string message, Action onConfirm, Action onCancel = null,
            string confirmText = "OK", string cancelText = "Cancel")
        {
            var data = new ConfirmDialogData
            {
                title = title,
                message = message,
                confirmText = confirmText,
                cancelText = cancelText,
                onConfirm = onConfirm,
                onCancel = onCancel
            };
            
            QueueDialog(DialogType.Confirm, data);
        }
        
        /// <summary>
        /// Show destructive confirmation (with red confirm button)
        /// </summary>
        public void ShowDestructiveConfirm(string title, string message, Action onConfirm, Action onCancel = null,
            string confirmText = "Delete", string cancelText = "Cancel")
        {
            var data = new ConfirmDialogData
            {
                title = title,
                message = message,
                confirmText = confirmText,
                cancelText = cancelText,
                onConfirm = onConfirm,
                onCancel = onCancel,
                isDestructive = true
            };
            
            QueueDialog(DialogType.Confirm, data);
        }
        
        #endregion
        
        #region Public API - Input Dialog
        
        /// <summary>
        /// Show input dialog for text entry
        /// </summary>
        public void ShowInput(string title, string placeholder, Action<string> onSubmit, Action onCancel = null,
            string submitText = "Submit", string cancelText = "Cancel", string defaultValue = "")
        {
            var data = new InputDialogData
            {
                title = title,
                placeholder = placeholder,
                defaultValue = defaultValue,
                submitText = submitText,
                cancelText = cancelText,
                onSubmit = onSubmit,
                onCancel = onCancel
            };
            
            QueueDialog(DialogType.Input, data);
        }
        
        /// <summary>
        /// Show number input dialog
        /// </summary>
        public void ShowNumberInput(string title, string placeholder, Action<float> onSubmit, Action onCancel = null,
            float minValue = float.MinValue, float maxValue = float.MaxValue, float defaultValue = 0)
        {
            var data = new InputDialogData
            {
                title = title,
                placeholder = placeholder,
                defaultValue = defaultValue.ToString(),
                submitText = "OK",
                cancelText = "Cancel",
                onSubmit = (text) =>
                {
                    if (float.TryParse(text, out float value))
                    {
                        value = Mathf.Clamp(value, minValue, maxValue);
                        onSubmit?.Invoke(value);
                    }
                },
                onCancel = onCancel,
                inputType = TMPro.TMP_InputField.ContentType.DecimalNumber
            };
            
            QueueDialog(DialogType.Input, data);
        }
        
        #endregion
        
        #region Public API - Choice Dialog
        
        /// <summary>
        /// Show choice dialog with multiple options
        /// </summary>
        public void ShowChoice(string title, string message, List<DialogChoice> choices, Action onCancel = null)
        {
            var data = new ChoiceDialogData
            {
                title = title,
                message = message,
                choices = choices,
                onCancel = onCancel
            };
            
            QueueDialog(DialogType.Choice, data);
        }
        
        /// <summary>
        /// Show simple two-choice dialog
        /// </summary>
        public void ShowTwoChoice(string title, string message, string option1Text, string option2Text,
            Action onOption1, Action onOption2)
        {
            var choices = new List<DialogChoice>
            {
                new DialogChoice { text = option1Text, onSelect = onOption1 },
                new DialogChoice { text = option2Text, onSelect = onOption2 }
            };
            
            ShowChoice(title, message, choices);
        }
        
        #endregion
        
        #region Public API - Alert Dialog
        
        /// <summary>
        /// Show alert/information dialog
        /// </summary>
        public void ShowAlert(string title, string message, Action onDismiss = null, string dismissText = "OK")
        {
            var data = new AlertDialogData
            {
                title = title,
                message = message,
                dismissText = dismissText,
                onDismiss = onDismiss
            };
            
            QueueDialog(DialogType.Alert, data);
        }
        
        /// <summary>
        /// Show error alert
        /// </summary>
        public void ShowError(string title, string message, Action onDismiss = null)
        {
            var data = new AlertDialogData
            {
                title = title,
                message = message,
                dismissText = "OK",
                onDismiss = onDismiss,
                isError = true
            };
            
            QueueDialog(DialogType.Alert, data);
        }
        
        /// <summary>
        /// Show success alert
        /// </summary>
        public void ShowSuccess(string title, string message, Action onDismiss = null)
        {
            var data = new AlertDialogData
            {
                title = title,
                message = message,
                dismissText = "OK",
                onDismiss = onDismiss,
                isSuccess = true
            };
            
            QueueDialog(DialogType.Alert, data);
        }
        
        #endregion
        
        #region Public API - Utilities
        
        /// <summary>
        /// Check if a dialog is currently active
        /// </summary>
        public bool IsDialogActive => _isDialogActive;
        
        /// <summary>
        /// Force close current dialog
        /// </summary>
        public void ForceClose()
        {
            if (_currentDialog != null)
            {
                CloseDialog(DialogResult.Cancelled);
            }
        }
        
        /// <summary>
        /// Clear dialog queue
        /// </summary>
        public void ClearQueue()
        {
            _dialogQueue.Clear();
        }
        
        #endregion
        
        #region Queue Management
        
        private void QueueDialog(DialogType type, object data)
        {
            var pending = new PendingDialog { type = type, data = data };
            
            if (_isDialogActive)
            {
                _dialogQueue.Enqueue(pending);
            }
            else
            {
                ShowDialogImmediate(pending);
            }
        }
        
        private void ShowDialogImmediate(PendingDialog pending)
        {
            _isDialogActive = true;
            
            // Get dialog rect
            RectTransform dialogRect = GetDialogRect(pending.type);
            if (dialogRect == null) return;
            
            // Configure dialog
            ConfigureDialog(pending.type, pending.data, dialogRect);
            
            // Create instance reference
            _currentDialog = new DialogInstance
            {
                type = pending.type,
                data = pending.data,
                dialogRect = dialogRect
            };
            
            // Show with animation
            StartCoroutine(AnimateDialogShow(dialogRect));
            
            // Play sound
            PlaySound(showSound);
        }
        
        private void ProcessNextInQueue()
        {
            if (_dialogQueue.Count > 0)
            {
                var next = _dialogQueue.Dequeue();
                ShowDialogImmediate(next);
            }
        }
        
        #endregion
        
        #region Dialog Configuration
        
        private RectTransform GetDialogRect(DialogType type)
        {
            switch (type)
            {
                case DialogType.Confirm: return confirmDialog;
                case DialogType.Input: return inputDialog;
                case DialogType.Choice: return choiceDialog;
                case DialogType.Alert: return alertDialog;
                default: return null;
            }
        }
        
        private void ConfigureDialog(DialogType type, object data, RectTransform dialogRect)
        {
            switch (type)
            {
                case DialogType.Confirm:
                    ConfigureConfirmDialog((ConfirmDialogData)data, dialogRect);
                    break;
                case DialogType.Input:
                    ConfigureInputDialog((InputDialogData)data, dialogRect);
                    break;
                case DialogType.Choice:
                    ConfigureChoiceDialog((ChoiceDialogData)data, dialogRect);
                    break;
                case DialogType.Alert:
                    ConfigureAlertDialog((AlertDialogData)data, dialogRect);
                    break;
            }
        }
        
        private void ConfigureConfirmDialog(ConfirmDialogData data, RectTransform dialogRect)
        {
            var title = dialogRect.Find("Title")?.GetComponent<TextMeshProUGUI>();
            var message = dialogRect.Find("Message")?.GetComponent<TextMeshProUGUI>();
            var confirmBtn = dialogRect.Find("ConfirmButton")?.GetComponent<Button>();
            var cancelBtn = dialogRect.Find("CancelButton")?.GetComponent<Button>();
            var confirmText = confirmBtn?.GetComponentInChildren<TextMeshProUGUI>();
            var cancelText = cancelBtn?.GetComponentInChildren<TextMeshProUGUI>();
            
            if (title != null) title.text = data.title;
            if (message != null) message.text = data.message;
            if (confirmText != null) confirmText.text = data.confirmText;
            if (cancelText != null) cancelText.text = data.cancelText;
            
            // Style destructive button
            if (data.isDestructive && confirmBtn != null)
            {
                var colors = confirmBtn.colors;
                colors.normalColor = new Color(0.8f, 0.2f, 0.2f);
                colors.highlightedColor = new Color(0.9f, 0.3f, 0.3f);
                confirmBtn.colors = colors;
            }
            
            // Setup button actions
            if (confirmBtn != null)
            {
                confirmBtn.onClick.RemoveAllListeners();
                confirmBtn.onClick.AddListener(() =>
                {
                    PlaySound(confirmSound);
                    data.onConfirm?.Invoke();
                    CloseDialog(DialogResult.Confirmed);
                });
            }
            
            if (cancelBtn != null)
            {
                cancelBtn.onClick.RemoveAllListeners();
                cancelBtn.onClick.AddListener(() =>
                {
                    PlaySound(cancelSound);
                    data.onCancel?.Invoke();
                    CloseDialog(DialogResult.Cancelled);
                });
            }
        }
        
        private void ConfigureInputDialog(InputDialogData data, RectTransform dialogRect)
        {
            var title = dialogRect.Find("Title")?.GetComponent<TextMeshProUGUI>();
            var inputField = dialogRect.Find("InputField")?.GetComponent<TMP_InputField>();
            var submitBtn = dialogRect.Find("SubmitButton")?.GetComponent<Button>();
            var cancelBtn = dialogRect.Find("CancelButton")?.GetComponent<Button>();
            var submitText = submitBtn?.GetComponentInChildren<TextMeshProUGUI>();
            var cancelText = cancelBtn?.GetComponentInChildren<TextMeshProUGUI>();
            
            if (title != null) title.text = data.title;
            if (inputField != null)
            {
                inputField.text = data.defaultValue;
                inputField.placeholder.GetComponent<TextMeshProUGUI>().text = data.placeholder;
                inputField.contentType = data.inputType;
                inputField.Select();
                inputField.ActivateInputField();
            }
            if (submitText != null) submitText.text = data.submitText;
            if (cancelText != null) cancelText.text = data.cancelText;
            
            if (submitBtn != null)
            {
                submitBtn.onClick.RemoveAllListeners();
                submitBtn.onClick.AddListener(() =>
                {
                    PlaySound(confirmSound);
                    string value = inputField != null ? inputField.text : "";
                    data.onSubmit?.Invoke(value);
                    CloseDialog(DialogResult.Confirmed);
                });
            }
            
            if (cancelBtn != null)
            {
                cancelBtn.onClick.RemoveAllListeners();
                cancelBtn.onClick.AddListener(() =>
                {
                    PlaySound(cancelSound);
                    data.onCancel?.Invoke();
                    CloseDialog(DialogResult.Cancelled);
                });
            }
            
            // Submit on enter
            if (inputField != null)
            {
                inputField.onSubmit.RemoveAllListeners();
                inputField.onSubmit.AddListener((text) =>
                {
                    submitBtn?.onClick.Invoke();
                });
            }
        }
        
        private void ConfigureChoiceDialog(ChoiceDialogData data, RectTransform dialogRect)
        {
            var title = dialogRect.Find("Title")?.GetComponent<TextMeshProUGUI>();
            var message = dialogRect.Find("Message")?.GetComponent<TextMeshProUGUI>();
            var buttonContainer = dialogRect.Find("ButtonContainer");
            var buttonPrefab = dialogRect.Find("ChoiceButtonPrefab")?.gameObject;
            var cancelBtn = dialogRect.Find("CancelButton")?.GetComponent<Button>();
            
            if (title != null) title.text = data.title;
            if (message != null) message.text = data.message;
            
            // Clear existing choice buttons
            if (buttonContainer != null)
            {
                for (int i = buttonContainer.childCount - 1; i >= 0; i--)
                {
                    var child = buttonContainer.GetChild(i).gameObject;
                    if (child != buttonPrefab)
                    {
                        Destroy(child);
                    }
                }
                
                // Create choice buttons
                foreach (var choice in data.choices)
                {
                    var btnGo = Instantiate(buttonPrefab, buttonContainer);
                    btnGo.SetActive(true);
                    
                    var btn = btnGo.GetComponent<Button>();
                    var text = btnGo.GetComponentInChildren<TextMeshProUGUI>();
                    
                    if (text != null) text.text = choice.text;
                    
                    if (btn != null)
                    {
                        var choiceCopy = choice; // Capture for closure
                        btn.onClick.AddListener(() =>
                        {
                            PlaySound(confirmSound);
                            choiceCopy.onSelect?.Invoke();
                            CloseDialog(DialogResult.OptionSelected);
                        });
                    }
                }
            }
            
            if (cancelBtn != null)
            {
                cancelBtn.onClick.RemoveAllListeners();
                cancelBtn.onClick.AddListener(() =>
                {
                    PlaySound(cancelSound);
                    data.onCancel?.Invoke();
                    CloseDialog(DialogResult.Cancelled);
                });
            }
        }
        
        private void ConfigureAlertDialog(AlertDialogData data, RectTransform dialogRect)
        {
            var title = dialogRect.Find("Title")?.GetComponent<TextMeshProUGUI>();
            var message = dialogRect.Find("Message")?.GetComponent<TextMeshProUGUI>();
            var icon = dialogRect.Find("Icon")?.GetComponent<Image>();
            var dismissBtn = dialogRect.Find("DismissButton")?.GetComponent<Button>();
            var dismissText = dismissBtn?.GetComponentInChildren<TextMeshProUGUI>();
            
            if (title != null) title.text = data.title;
            if (message != null) message.text = data.message;
            if (dismissText != null) dismissText.text = data.dismissText;
            
            // Set icon color based on type
            if (icon != null)
            {
                if (data.isError)
                {
                    icon.color = new Color(0.8f, 0.2f, 0.2f);
                }
                else if (data.isSuccess)
                {
                    icon.color = new Color(0.2f, 0.8f, 0.3f);
                }
                else
                {
                    icon.color = new Color(0.3f, 0.5f, 0.8f);
                }
            }
            
            if (dismissBtn != null)
            {
                dismissBtn.onClick.RemoveAllListeners();
                dismissBtn.onClick.AddListener(() =>
                {
                    PlaySound(confirmSound);
                    data.onDismiss?.Invoke();
                    CloseDialog(DialogResult.Dismissed);
                });
            }
        }
        
        #endregion
        
        #region Animation
        
        private IEnumerator AnimateDialogShow(RectTransform dialogRect)
        {
            // Show backdrop
            if (backdrop != null)
            {
                backdrop.raycastTarget = true;
            }
            
            // Prepare dialog
            dialogRect.gameObject.SetActive(true);
            var canvasGroup = GetOrAddCanvasGroup(dialogRect);
            canvasGroup.alpha = 0;
            dialogRect.localScale = Vector3.one * 0.8f;
            
            float elapsed = 0;
            while (elapsed < showDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = showCurve.Evaluate(elapsed / showDuration);
                
                // Backdrop fade
                if (backdrop != null)
                {
                    backdrop.color = new Color(backdropColor.r, backdropColor.g, backdropColor.b, 
                        backdropColor.a * Mathf.Clamp01(t * 2)); // Faster fade
                }
                
                // Dialog scale and fade
                canvasGroup.alpha = t;
                dialogRect.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, t);
                
                yield return null;
            }
            
            if (backdrop != null)
            {
                backdrop.color = backdropColor;
            }
            canvasGroup.alpha = 1;
            dialogRect.localScale = Vector3.one;
        }
        
        private IEnumerator AnimateDialogHide(RectTransform dialogRect, Action onComplete)
        {
            var canvasGroup = GetOrAddCanvasGroup(dialogRect);
            
            float elapsed = 0;
            while (elapsed < hideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = hideCurve.Evaluate(elapsed / hideDuration);
                
                // Backdrop fade
                if (backdrop != null)
                {
                    backdrop.color = new Color(backdropColor.r, backdropColor.g, backdropColor.b,
                        backdropColor.a * (1 - t));
                }
                
                // Dialog scale and fade
                canvasGroup.alpha = 1 - t;
                dialogRect.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.9f, t);
                
                yield return null;
            }
            
            // Hide
            if (backdrop != null)
            {
                backdrop.color = new Color(backdropColor.r, backdropColor.g, backdropColor.b, 0);
                backdrop.raycastTarget = false;
            }
            
            canvasGroup.alpha = 1;
            dialogRect.localScale = Vector3.one;
            dialogRect.gameObject.SetActive(false);
            
            onComplete?.Invoke();
        }
        
        #endregion
        
        #region Dialog Closing
        
        private void CloseDialog(DialogResult result)
        {
            if (_currentDialog == null) return;
            
            PlaySound(hideSound);
            
            StartCoroutine(AnimateDialogHide(_currentDialog.dialogRect, () =>
            {
                _isDialogActive = false;
                _currentDialog = null;
                
                OnDialogClosed?.Invoke(result);
                
                // Process next dialog in queue
                ProcessNextInQueue();
            }));
        }
        
        #endregion
        
        #region Helpers
        
        private CanvasGroup GetOrAddCanvasGroup(RectTransform target)
        {
            var group = target.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = target.gameObject.AddComponent<CanvasGroup>();
            }
            return group;
        }
        
        private void PlaySound(AudioClip clip)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }
        
        #endregion
        
        #region Escape Key Handling
        
        private void Update()
        {
            // Close dialog on escape
            if (_isDialogActive && Input.GetKeyDown(KeyCode.Escape))
            {
                if (_currentDialog != null)
                {
                    // Invoke cancel action based on dialog type
                    switch (_currentDialog.type)
                    {
                        case DialogType.Confirm:
                            ((ConfirmDialogData)_currentDialog.data).onCancel?.Invoke();
                            break;
                        case DialogType.Input:
                            ((InputDialogData)_currentDialog.data).onCancel?.Invoke();
                            break;
                        case DialogType.Choice:
                            ((ChoiceDialogData)_currentDialog.data).onCancel?.Invoke();
                            break;
                        case DialogType.Alert:
                            ((AlertDialogData)_currentDialog.data).onDismiss?.Invoke();
                            break;
                    }
                    
                    CloseDialog(DialogResult.Cancelled);
                }
            }
        }
        
        #endregion
    }
    
    #region Data Types
    
    public enum DialogType
    {
        Confirm,
        Input,
        Choice,
        Alert
    }
    
    public enum DialogResult
    {
        Confirmed,
        Cancelled,
        Dismissed,
        OptionSelected
    }
    
    public class ConfirmDialogData
    {
        public string title;
        public string message;
        public string confirmText = "OK";
        public string cancelText = "Cancel";
        public Action onConfirm;
        public Action onCancel;
        public bool isDestructive;
    }
    
    public class InputDialogData
    {
        public string title;
        public string placeholder;
        public string defaultValue = "";
        public string submitText = "Submit";
        public string cancelText = "Cancel";
        public Action<string> onSubmit;
        public Action onCancel;
        public TMP_InputField.ContentType inputType = TMP_InputField.ContentType.Standard;
    }
    
    public class ChoiceDialogData
    {
        public string title;
        public string message;
        public List<DialogChoice> choices;
        public Action onCancel;
    }
    
    public class DialogChoice
    {
        public string text;
        public Action onSelect;
        public Sprite icon;
        public bool isDestructive;
    }
    
    public class AlertDialogData
    {
        public string title;
        public string message;
        public string dismissText = "OK";
        public Action onDismiss;
        public bool isError;
        public bool isSuccess;
    }
    
    public class PendingDialog
    {
        public DialogType type;
        public object data;
    }
    
    public class DialogInstance
    {
        public DialogType type;
        public object data;
        public RectTransform dialogRect;
    }
    
    #endregion
}
