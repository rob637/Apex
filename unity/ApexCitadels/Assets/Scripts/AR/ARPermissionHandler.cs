// ============================================================================
// APEX CITADELS - AR PERMISSIONS HANDLER
// Manages runtime permissions for camera and location on mobile devices
// ============================================================================
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.Core;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace ApexCitadels.AR
{
    /// <summary>
    /// Handles runtime permission requests for AR features on mobile devices.
    /// Shows user-friendly dialogs explaining why permissions are needed.
    /// </summary>
    public class ARPermissionHandler : MonoBehaviour
    {
        public static ARPermissionHandler Instance { get; private set; }

        [Header("Permission UI")]
        [SerializeField] private GameObject permissionPanel;
        [SerializeField] private TextMeshProUGUI permissionTitleText;
        [SerializeField] private TextMeshProUGUI permissionDescriptionText;
        [SerializeField] private Button grantPermissionButton;
        [SerializeField] private Button skipPermissionButton;
        [SerializeField] private Image permissionIcon;

        [Header("Permission Icons")]
        [SerializeField] private Sprite cameraIcon;
        [SerializeField] private Sprite locationIcon;

        // State
        private PermissionType currentPermissionRequest;
        private Action<bool> permissionCallback;
        private bool isRequestingPermission = false;

        // Events
        public event Action OnAllPermissionsGranted;
        public event Action<PermissionType> OnPermissionDenied;

        // Properties
        public bool HasCameraPermission => CheckPermission(PermissionType.Camera);
        public bool HasLocationPermission => CheckPermission(PermissionType.Location);
        public bool HasAllPermissions => HasCameraPermission && HasLocationPermission;

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
            SetupButtons();
            
            // Hide panel initially
            if (permissionPanel != null)
            {
                permissionPanel.SetActive(false);
            }
        }

        private void SetupButtons()
        {
            if (grantPermissionButton != null)
            {
                grantPermissionButton.onClick.AddListener(OnGrantPermissionClicked);
            }

            if (skipPermissionButton != null)
            {
                skipPermissionButton.onClick.AddListener(OnSkipPermissionClicked);
            }
        }

        #region Permission Checking

        /// <summary>
        /// Check all required permissions and request if needed
        /// </summary>
        public void CheckAndRequestAllPermissions(Action<bool> callback)
        {
            StartCoroutine(CheckAllPermissionsRoutine(callback));
        }

        private IEnumerator CheckAllPermissionsRoutine(Action<bool> callback)
        {
            // Check camera permission first
            if (!HasCameraPermission)
            {
                bool cameraGranted = false;
                RequestPermission(PermissionType.Camera, granted => cameraGranted = granted);
                yield return new WaitUntil(() => !isRequestingPermission);
                
                if (!cameraGranted)
                {
                    callback?.Invoke(false);
                    yield break;
                }
            }

            // Then check location
            if (!HasLocationPermission)
            {
                bool locationGranted = false;
                RequestPermission(PermissionType.Location, granted => locationGranted = granted);
                yield return new WaitUntil(() => !isRequestingPermission);
                
                if (!locationGranted)
                {
                    callback?.Invoke(false);
                    yield break;
                }
            }

            // All permissions granted!
            ApexLogger.Log(ApexLogger.LogCategory.AR, "[Permissions] All required permissions granted");
            OnAllPermissionsGranted?.Invoke();
            callback?.Invoke(true);
        }

        private bool CheckPermission(PermissionType type)
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            switch (type)
            {
                case PermissionType.Camera:
                    return Permission.HasUserAuthorizedPermission(Permission.Camera);
                case PermissionType.Location:
                    return Permission.HasUserAuthorizedPermission(Permission.FineLocation);
                default:
                    return true;
            }
            #elif UNITY_IOS && !UNITY_EDITOR
            // iOS permissions are checked through native APIs
            // For now, assume they'll be requested when needed
            return true;
            #else
            // In editor, always return true
            return true;
            #endif
        }

        #endregion

        #region Permission Requesting

        /// <summary>
        /// Request a specific permission with explanation UI
        /// </summary>
        public void RequestPermission(PermissionType type, Action<bool> callback)
        {
            if (isRequestingPermission)
            {
                ApexLogger.LogWarning(ApexLogger.LogCategory.AR, "[Permissions] Already requesting a permission");
                return;
            }

            // Check if already granted
            if (CheckPermission(type))
            {
                callback?.Invoke(true);
                return;
            }

            currentPermissionRequest = type;
            permissionCallback = callback;
            isRequestingPermission = true;

            ShowPermissionUI(type);
        }

        private void ShowPermissionUI(PermissionType type)
        {
            if (permissionPanel == null) return;

            permissionPanel.SetActive(true);

            switch (type)
            {
                case PermissionType.Camera:
                    if (permissionTitleText != null)
                        permissionTitleText.text = "Camera Access Required";
                    if (permissionDescriptionText != null)
                        permissionDescriptionText.text = "Apex Citadels needs camera access to show AR content in the real world. Your camera feed is processed locally and never uploaded.";
                    if (permissionIcon != null && cameraIcon != null)
                        permissionIcon.sprite = cameraIcon;
                    break;

                case PermissionType.Location:
                    if (permissionTitleText != null)
                        permissionTitleText.text = "Location Access Required";
                    if (permissionDescriptionText != null)
                        permissionDescriptionText.text = "Apex Citadels uses your GPS location to place your territory in the real world. Your exact location is only shared with your alliance members.";
                    if (permissionIcon != null && locationIcon != null)
                        permissionIcon.sprite = locationIcon;
                    break;
            }

            ApexLogger.Log(ApexLogger.LogCategory.AR, $"[Permissions] Showing request UI for: {type}");
        }

        private void OnGrantPermissionClicked()
        {
            if (permissionPanel != null)
            {
                permissionPanel.SetActive(false);
            }

            RequestSystemPermission(currentPermissionRequest);
        }

        private void OnSkipPermissionClicked()
        {
            if (permissionPanel != null)
            {
                permissionPanel.SetActive(false);
            }

            ApexLogger.LogWarning(ApexLogger.LogCategory.AR, $"[Permissions] User skipped: {currentPermissionRequest}");
            OnPermissionDenied?.Invoke(currentPermissionRequest);
            
            isRequestingPermission = false;
            permissionCallback?.Invoke(false);
        }

        private void RequestSystemPermission(PermissionType type)
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            string permission = type == PermissionType.Camera ? Permission.Camera : Permission.FineLocation;
            
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += (perm) => OnPermissionResult(true);
            callbacks.PermissionDenied += (perm) => OnPermissionResult(false);
            callbacks.PermissionDeniedAndDontAskAgain += (perm) => OnPermissionDeniedPermanently();
            
            Permission.RequestUserPermission(permission, callbacks);
            #else
            // In editor, simulate permission granted
            OnPermissionResult(true);
            #endif
        }

        private void OnPermissionResult(bool granted)
        {
            ApexLogger.Log(ApexLogger.LogCategory.AR, $"[Permissions] {currentPermissionRequest} result: {(granted ? "GRANTED" : "DENIED")}");
            
            if (!granted)
            {
                OnPermissionDenied?.Invoke(currentPermissionRequest);
            }

            isRequestingPermission = false;
            permissionCallback?.Invoke(granted);
        }

        private void OnPermissionDeniedPermanently()
        {
            ApexLogger.LogWarning(ApexLogger.LogCategory.AR, $"[Permissions] {currentPermissionRequest} denied permanently. User must enable in settings.");
            
            // Show dialog directing user to app settings
            ShowOpenSettingsDialog();
            
            isRequestingPermission = false;
            permissionCallback?.Invoke(false);
        }

        #endregion

        #region Settings Dialog

        private void ShowOpenSettingsDialog()
        {
            // TODO: Show a dialog with button to open app settings
            ApexLogger.Log(ApexLogger.LogCategory.UI, "[Permissions] Would show 'Open Settings' dialog");
            
            #if UNITY_ANDROID && !UNITY_EDITOR
            // Open Android app settings
            try
            {
                using (var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var intent = new AndroidJavaObject("android.content.Intent", "android.settings.APPLICATION_DETAILS_SETTINGS"))
                using (var uri = new AndroidJavaClass("android.net.Uri").CallStatic<AndroidJavaObject>("parse", "package:" + Application.identifier))
                {
                    intent.Call<AndroidJavaObject>("setData", uri);
                    currentActivity.Call("startActivity", intent);
                }
            }
            catch (Exception e)
            {
                ApexLogger.LogError(ApexLogger.LogCategory.AR, $"[Permissions] Failed to open settings: {e.Message}");
            }
            #endif
        }

        #endregion

        #region Public API

        /// <summary>
        /// Quick check if app has all permissions to run AR
        /// </summary>
        public static bool CanRunAR()
        {
            if (Instance == null) return false;
            return Instance.HasAllPermissions;
        }

        /// <summary>
        /// Show rationale for why a permission is needed
        /// </summary>
        public void ShowPermissionRationale(PermissionType type, Action onAcknowledged)
        {
            // TODO: Show educational UI about why permission is needed
            onAcknowledged?.Invoke();
        }

        #endregion
    }

    /// <summary>
    /// Types of permissions the app needs
    /// </summary>
    public enum PermissionType
    {
        Camera,
        Location
    }
}
