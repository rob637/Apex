using Camera = UnityEngine.Camera;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using ApexCitadels.Core;

namespace ApexCitadels.PC.CameraControllers
{
    /// <summary>
    /// Cinematic Mode Controller for automated territory tours.
    /// Features:
    /// - Spline-based camera paths
    /// - Point-of-interest system
    /// - Smooth transitions
    /// - Speed control
    /// - Photo mode integration
    /// - Narration sync
    /// </summary>
    public class CinematicModeController : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private UnityEngine.Camera cinematicCamera;
        [SerializeField] private Transform cameraRig;
        [SerializeField] private float defaultFOV = 60f;
        [SerializeField] private float wideAngleFOV = 80f;
        [SerializeField] private float closeUpFOV = 40f;
        
        [Header("Movement")]
        [SerializeField] private float defaultSpeed = 5f;
        [SerializeField] private float minSpeed = 1f;
        [SerializeField] private float maxSpeed = 15f;
        [SerializeField] private float accelerationTime = 2f;
        [SerializeField] private AnimationCurve speedCurve;
        
        [Header("Look At")]
        [SerializeField] private float lookAtSmoothing = 3f;
        [SerializeField] private float lookAheadDistance = 10f;
        
        [Header("Depth of Field")]
        [SerializeField] private bool enableDOF = true;
        [SerializeField] private float dofFocusDistance = 15f;
        [SerializeField] private float dofAperture = 5.6f;
        
        [Header("Post Processing")]
        [SerializeField] private bool enableVignette = true;
        [SerializeField] private float vignetteIntensity = 0.3f;
        [SerializeField] private bool enableLetterbox = true;
        [SerializeField] private float letterboxAmount = 0.12f;
        
        [Header("Audio")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip cinematicMusic;
        [SerializeField] private float musicFadeTime = 2f;
        
        [Header("UI")]
        [SerializeField] private GameObject cinematicUI;
        [SerializeField] private GameObject letterboxTop;
        [SerializeField] private GameObject letterboxBottom;
        [SerializeField] private TMPro.TextMeshProUGUI locationTitleText;
        [SerializeField] private TMPro.TextMeshProUGUI narrationText;
        [SerializeField] private CanvasGroup controlsHintGroup;
        
        // Singleton
        private static CinematicModeController _instance;
        public static CinematicModeController Instance => _instance;
        
        // State
        private CinematicPath _currentPath;
        private int _currentWaypointIndex;
        private float _pathProgress;
        private float _currentSpeed;
        private bool _isPlaying;
        private bool _isPaused;
        private Transform _lookAtTarget;
        private Coroutine _tourCoroutine;
        
        // Input handled via legacy Input system in Update()
        
        // Events
        public event Action OnCinematicStart;
        public event Action OnCinematicEnd;
        public event Action<CinematicWaypoint> OnWaypointReached;
        public event Action OnPauseToggled;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            if (speedCurve == null || speedCurve.length == 0)
            {
                speedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            }
        }
        
        private void Update()
        {
            if (!_isPlaying) return;
            
            // Handle input via legacy Input system
            if (Input.GetKeyDown(KeyCode.Space)) TogglePause();
            if (Input.GetKeyDown(KeyCode.Return)) SkipToNext();
            if (Input.GetKeyDown(KeyCode.Escape)) StopTour();
            if (Input.GetKeyDown(KeyCode.RightBracket)) AdjustSpeed(1.5f);
            if (Input.GetKeyDown(KeyCode.LeftBracket)) AdjustSpeed(0.67f);
            if (Input.GetKeyDown(KeyCode.P)) EnterPhotoMode();
        }
        
        private void Start()
        {
            cinematicUI?.SetActive(false);
        }
        
        #region Public API
        
        /// <summary>
        /// Start a cinematic tour with a predefined path
        /// </summary>
        public void StartTour(CinematicPath path)
        {
            if (path == null || path.waypoints.Count < 2)
            {
                ApexLogger.LogWarning("Invalid cinematic path", LogCategory.General);
                return;
            }
            
            _currentPath = path;
            _currentWaypointIndex = 0;
            _pathProgress = 0;
            _currentSpeed = defaultSpeed;
            _isPlaying = true;
            _isPaused = false;
            
            // Enable cinematic camera
            cinematicCamera?.gameObject.SetActive(true);
            
            // Show UI
            cinematicUI?.SetActive(true);
            ShowLetterbox(true);
            
            // Start music
            if (musicSource != null && cinematicMusic != null)
            {
                StartCoroutine(FadeInMusic());
            }
            
            // Lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // Start tour
            _tourCoroutine = StartCoroutine(RunTour());
            
            OnCinematicStart?.Invoke();
        }
        
        /// <summary>
        /// Start an auto-tour of all territories
        /// </summary>
        public void StartTerritoryTour(List<Transform> territories)
        {
            if (territories == null || territories.Count < 2) return;
            
            // Generate path from territories
            var path = new CinematicPath
            {
                name = "Territory Tour",
                waypoints = new List<CinematicWaypoint>()
            };
            
            foreach (var territory in territories)
            {
                if (territory == null) continue;
                
                path.waypoints.Add(new CinematicWaypoint
                {
                    position = territory.position + Vector3.up * 10f + Vector3.back * 15f,
                    lookAtTarget = territory,
                    dwellTime = 3f,
                    title = territory.name,
                    fovOverride = defaultFOV
                });
            }
            
            StartTour(path);
        }
        
        /// <summary>
        /// Stop the current tour
        /// </summary>
        public void StopTour()
        {
            if (!_isPlaying) return;
            
            _isPlaying = false;
            
            if (_tourCoroutine != null)
            {
                StopCoroutine(_tourCoroutine);
            }
            
            // Fade out music
            if (musicSource != null)
            {
                StartCoroutine(FadeOutMusic());
            }
            
            // Hide UI
            ShowLetterbox(false);
            cinematicUI?.SetActive(false);
            
            // Unlock cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // Disable cinematic camera
            StartCoroutine(TransitionCameraOut());
            
            OnCinematicEnd?.Invoke();
        }
        
        /// <summary>
        /// Toggle pause state
        /// </summary>
        public void TogglePause()
        {
            if (!_isPlaying) return;
            
            _isPaused = !_isPaused;
            OnPauseToggled?.Invoke();
            
            // Show/hide controls hint
            if (controlsHintGroup != null)
            {
                controlsHintGroup.alpha = _isPaused ? 1 : 0;
            }
        }
        
        /// <summary>
        /// Skip to next waypoint
        /// </summary>
        public void SkipToNext()
        {
            if (!_isPlaying || _currentPath == null) return;
            
            _currentWaypointIndex++;
            if (_currentWaypointIndex >= _currentPath.waypoints.Count - 1)
            {
                StopTour();
            }
        }
        
        /// <summary>
        /// Adjust playback speed
        /// </summary>
        public void AdjustSpeed(float multiplier)
        {
            _currentSpeed = Mathf.Clamp(_currentSpeed * multiplier, minSpeed, maxSpeed);
        }
        
        /// <summary>
        /// Enter photo mode (freeze frame)
        /// </summary>
        public void EnterPhotoMode()
        {
            if (!_isPlaying) return;
            
            _isPaused = true;
            
            // Enable free camera movement
            ApexLogger.LogVerbose("Photo mode enabled - implement free camera controls", LogCategory.General);
        }
        
        #endregion
        
        #region Tour Execution
        
        private IEnumerator RunTour()
        {
            // Initial setup
            if (_currentPath.waypoints.Count > 0)
            {
                var firstWaypoint = _currentPath.waypoints[0];
                cameraRig.position = firstWaypoint.position;
                
                if (firstWaypoint.lookAtTarget != null)
                {
                    cameraRig.LookAt(firstWaypoint.lookAtTarget);
                }
            }
            
            while (_isPlaying && _currentWaypointIndex < _currentPath.waypoints.Count - 1)
            {
                // Wait if paused
                while (_isPaused)
                {
                    yield return null;
                }
                
                var currentWaypoint = _currentPath.waypoints[_currentWaypointIndex];
                var nextWaypoint = _currentPath.waypoints[_currentWaypointIndex + 1];
                
                // Show title/narration
                ShowWaypointInfo(currentWaypoint);
                OnWaypointReached?.Invoke(currentWaypoint);
                
                // Dwell at waypoint
                if (currentWaypoint.dwellTime > 0)
                {
                    yield return new WaitForSeconds(currentWaypoint.dwellTime);
                }
                
                // Move to next waypoint
                yield return StartCoroutine(MoveToWaypoint(currentWaypoint, nextWaypoint));
                
                _currentWaypointIndex++;
            }
            
            // Tour complete
            StopTour();
        }
        
        private IEnumerator MoveToWaypoint(CinematicWaypoint from, CinematicWaypoint to)
        {
            Vector3 startPos = from.position;
            Vector3 endPos = to.position;
            float distance = Vector3.Distance(startPos, endPos);
            float duration = distance / _currentSpeed;
            
            // FOV transition
            float startFOV = from.fovOverride > 0 ? from.fovOverride : defaultFOV;
            float endFOV = to.fovOverride > 0 ? to.fovOverride : defaultFOV;
            
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                // Wait if paused
                while (_isPaused)
                {
                    yield return null;
                }
                
                elapsed += Time.deltaTime;
                float t = speedCurve.Evaluate(elapsed / duration);
                
                // Position
                Vector3 newPos;
                if (from.curveControlPoint != Vector3.zero || to.curveControlPoint != Vector3.zero)
                {
                    // Bezier curve
                    Vector3 control = (from.curveControlPoint + to.curveControlPoint) * 0.5f;
                    if (control == Vector3.zero)
                    {
                        control = (startPos + endPos) * 0.5f + Vector3.up * 5f;
                    }
                    newPos = CalculateBezierPoint(t, startPos, control, endPos);
                }
                else
                {
                    // Linear
                    newPos = Vector3.Lerp(startPos, endPos, t);
                }
                
                cameraRig.position = newPos;
                
                // Look at
                Transform lookTarget = to.lookAtTarget ?? from.lookAtTarget;
                if (lookTarget != null)
                {
                    Vector3 lookDir = lookTarget.position - cameraRig.position;
                    Quaternion targetRot = Quaternion.LookRotation(lookDir);
                    cameraRig.rotation = Quaternion.Slerp(cameraRig.rotation, targetRot, lookAtSmoothing * Time.deltaTime);
                }
                else
                {
                    // Look ahead
                    Vector3 lookAhead = Vector3.Lerp(startPos, endPos, Mathf.Min(t + 0.1f, 1f));
                    Vector3 lookDir = lookAhead - cameraRig.position;
                    if (lookDir != Vector3.zero)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(lookDir);
                        cameraRig.rotation = Quaternion.Slerp(cameraRig.rotation, targetRot, lookAtSmoothing * Time.deltaTime);
                    }
                }
                
                // FOV
                if (cinematicCamera != null)
                {
                    cinematicCamera.fieldOfView = Mathf.Lerp(startFOV, endFOV, t);
                }
                
                yield return null;
            }
            
            cameraRig.position = endPos;
        }
        
        private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            float u = 1 - t;
            return u * u * p0 + 2 * u * t * p1 + t * t * p2;
        }
        
        #endregion
        
        #region UI
        
        private void ShowWaypointInfo(CinematicWaypoint waypoint)
        {
            if (!string.IsNullOrEmpty(waypoint.title) && locationTitleText != null)
            {
                locationTitleText.text = waypoint.title;
                StartCoroutine(AnimateTitle());
            }
            
            if (!string.IsNullOrEmpty(waypoint.narration) && narrationText != null)
            {
                narrationText.text = waypoint.narration;
                StartCoroutine(AnimateNarration());
            }
        }
        
        private IEnumerator AnimateTitle()
        {
            if (locationTitleText == null) yield break;
            
            var canvasGroup = locationTitleText.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = locationTitleText.gameObject.AddComponent<CanvasGroup>();
            }
            
            // Fade in
            canvasGroup.alpha = 0;
            float elapsed = 0;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = elapsed / 0.5f;
                yield return null;
            }
            
            // Hold
            yield return new WaitForSeconds(2f);
            
            // Fade out
            elapsed = 0;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1 - (elapsed / 0.5f);
                yield return null;
            }
        }
        
        private IEnumerator AnimateNarration()
        {
            if (narrationText == null) yield break;
            
            var canvasGroup = narrationText.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = narrationText.gameObject.AddComponent<CanvasGroup>();
            }
            
            // Fade in
            canvasGroup.alpha = 0;
            float elapsed = 0;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = elapsed / 0.3f;
                yield return null;
            }
            
            // Hold
            yield return new WaitForSeconds(4f);
            
            // Fade out
            elapsed = 0;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1 - (elapsed / 0.3f);
                yield return null;
            }
        }
        
        private void ShowLetterbox(bool show)
        {
            if (!enableLetterbox) return;
            
            StartCoroutine(AnimateLetterbox(show));
        }
        
        private IEnumerator AnimateLetterbox(bool show)
        {
            var topRect = letterboxTop?.GetComponent<RectTransform>();
            var bottomRect = letterboxBottom?.GetComponent<RectTransform>();
            
            if (topRect == null || bottomRect == null) yield break;
            
            float startHeight = show ? 0 : letterboxAmount * Screen.height;
            float endHeight = show ? letterboxAmount * Screen.height : 0;
            
            float elapsed = 0;
            float duration = 0.5f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float height = Mathf.Lerp(startHeight, endHeight, t);
                
                topRect.sizeDelta = new Vector2(topRect.sizeDelta.x, height);
                bottomRect.sizeDelta = new Vector2(bottomRect.sizeDelta.x, height);
                
                yield return null;
            }
        }
        
        #endregion
        
        #region Audio
        
        private IEnumerator FadeInMusic()
        {
            if (musicSource == null || cinematicMusic == null) yield break;
            
            musicSource.clip = cinematicMusic;
            musicSource.volume = 0;
            musicSource.Play();
            
            float elapsed = 0;
            while (elapsed < musicFadeTime)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = elapsed / musicFadeTime;
                yield return null;
            }
            
            musicSource.volume = 1;
        }
        
        private IEnumerator FadeOutMusic()
        {
            if (musicSource == null) yield break;
            
            float startVolume = musicSource.volume;
            float elapsed = 0;
            
            while (elapsed < musicFadeTime)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0, elapsed / musicFadeTime);
                yield return null;
            }
            
            musicSource.Stop();
        }
        
        #endregion
        
        #region Transitions
        
        private IEnumerator TransitionCameraOut()
        {
            // Would smoothly transition back to strategic camera
            cinematicCamera?.gameObject.SetActive(false);
            yield return null;
        }
        
        #endregion
    }
    
    #region Data Classes
    
    [Serializable]
    public class CinematicPath
    {
        public string name;
        public List<CinematicWaypoint> waypoints = new List<CinematicWaypoint>();
        public bool loop;
        public float defaultSpeed = 5f;
    }
    
    [Serializable]
    public class CinematicWaypoint
    {
        public Vector3 position;
        public Vector3 curveControlPoint;
        public Transform lookAtTarget;
        public float dwellTime;
        public float fovOverride;
        public string title;
        public string narration;
        public AudioClip ambientSound;
        public CinematicShotType shotType;
    }
    
    public enum CinematicShotType
    {
        Wide,
        Medium,
        CloseUp,
        Aerial,
        LowAngle,
        HighAngle,
        Dutch,
        POV
    }
    
    #endregion
    
    /// <summary>
    /// ScriptableObject for storing cinematic paths
    /// </summary>
    [CreateAssetMenu(fileName = "CinematicPath", menuName = "Apex Citadels/Cinematic Path")]
    public class CinematicPathAsset : ScriptableObject
    {
        public string pathName;
        public List<WaypointData> waypoints = new List<WaypointData>();
        public bool loop;
        public float defaultSpeed = 5f;
        public AudioClip musicTrack;
        
        [Serializable]
        public class WaypointData
        {
            public Vector3 position;
            public Vector3 controlPoint;
            public string lookAtTargetName;
            public float dwellTime;
            public float fov;
            public string title;
            [TextArea] public string narration;
            public CinematicShotType shotType;
        }
        
        public CinematicPath ToRuntimePath(Transform[] targetLookup)
        {
            var path = new CinematicPath
            {
                name = pathName,
                loop = loop,
                defaultSpeed = defaultSpeed,
                waypoints = new List<CinematicWaypoint>()
            };
            
            foreach (var wp in waypoints)
            {
                Transform lookAt = null;
                if (!string.IsNullOrEmpty(wp.lookAtTargetName) && targetLookup != null)
                {
                    foreach (var t in targetLookup)
                    {
                        if (t != null && t.name == wp.lookAtTargetName)
                        {
                            lookAt = t;
                            break;
                        }
                    }
                }
                
                path.waypoints.Add(new CinematicWaypoint
                {
                    position = wp.position,
                    curveControlPoint = wp.controlPoint,
                    lookAtTarget = lookAt,
                    dwellTime = wp.dwellTime,
                    fovOverride = wp.fov,
                    title = wp.title,
                    narration = wp.narration,
                    shotType = wp.shotType
                });
            }
            
            return path;
        }
    }
    
    /// <summary>
    /// Editor helper for creating cinematic paths
    /// </summary>
    public class CinematicPathCreator : MonoBehaviour
    {
        [Header("Path Settings")]
        public string pathName = "New Path";
        public bool showGizmos = true;
        public Color pathColor = Color.cyan;
        
        [Header("Waypoints")]
        public List<Transform> waypointTransforms = new List<Transform>();
        
        [Header("Output")]
        public CinematicPathAsset outputAsset;
        
        private void OnDrawGizmos()
        {
            if (!showGizmos || waypointTransforms.Count < 2) return;
            
            Gizmos.color = pathColor;
            
            for (int i = 0; i < waypointTransforms.Count - 1; i++)
            {
                if (waypointTransforms[i] == null || waypointTransforms[i + 1] == null)
                    continue;
                
                Gizmos.DrawLine(waypointTransforms[i].position, waypointTransforms[i + 1].position);
                Gizmos.DrawWireSphere(waypointTransforms[i].position, 0.5f);
            }
            
            if (waypointTransforms[waypointTransforms.Count - 1] != null)
            {
                Gizmos.DrawWireSphere(waypointTransforms[waypointTransforms.Count - 1].position, 0.5f);
            }
        }
        
        /// <summary>
        /// Generate path asset from transforms
        /// </summary>
        [ContextMenu("Generate Path Asset")]
        public void GeneratePathAsset()
        {
            if (outputAsset == null)
            {
                ApexLogger.LogError("No output asset assigned", LogCategory.General);
                return;
            }
            
            outputAsset.pathName = pathName;
            outputAsset.waypoints.Clear();
            
            foreach (var wp in waypointTransforms)
            {
                if (wp == null) continue;
                
                outputAsset.waypoints.Add(new CinematicPathAsset.WaypointData
                {
                    position = wp.position,
                    title = wp.name
                });
            }
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(outputAsset);
#endif
            
            ApexLogger.Log($"Generated path with {outputAsset.waypoints.Count} waypoints", LogCategory.General);
        }
    }
    
    /// <summary>
    /// Photo mode for cinematic captures
    /// </summary>
    public class CinematicPhotoMode : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private UnityEngine.Camera photoCamera;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotateSpeed = 2f;
        
        [Header("Effects")]
        [SerializeField] private bool enableDOF = true;
        [SerializeField] private float dofMinDistance = 1f;
        [SerializeField] private float dofMaxDistance = 100f;
        [SerializeField] private Slider dofSlider;
        
        [Header("Filters")]
        [SerializeField] private Material[] filterMaterials;
        [SerializeField] private int currentFilterIndex = -1;
        
        [Header("UI")]
        [SerializeField] private GameObject photoModeUI;
        [SerializeField] private UnityEngine.UI.Button captureButton;
        [SerializeField] private TMPro.TextMeshProUGUI filterNameText;
        
        // Input handled via legacy Input system
        private Vector3 _moveInput;
        private Vector2 _lookInput;
        
        private bool _isActive;
        
        private void Awake()
        {
            // Nothing to setup - using legacy Input
        }
        
        private void Update()
        {
            if (!_isActive) return;
            
            // Handle input
            if (Input.GetKeyDown(KeyCode.F)) CapturePhoto();
            if (Input.GetKeyDown(KeyCode.Tab)) CycleFilter();
            if (Input.GetKeyDown(KeyCode.Escape)) Exit();
            
            HandleMovement();
            HandleLook();
        }
        
        public void Enter()
        {
            _isActive = true;
            photoModeUI?.SetActive(true);
            
            Time.timeScale = 0;
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        public void Exit()
        {
            _isActive = false;
            photoModeUI?.SetActive(false);
            
            Time.timeScale = 1;
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        private void HandleMovement()
        {
            // Get input via legacy system
            float x = 0, y = 0, z = 0;
            if (Input.GetKey(KeyCode.D)) x = 1;
            else if (Input.GetKey(KeyCode.A)) x = -1;
            if (Input.GetKey(KeyCode.E)) y = 1;
            else if (Input.GetKey(KeyCode.Q)) y = -1;
            if (Input.GetKey(KeyCode.W)) z = 1;
            else if (Input.GetKey(KeyCode.S)) z = -1;
            
            Vector3 input = new Vector3(x, y, z);
            Vector3 move = transform.right * input.x + transform.up * input.y + transform.forward * input.z;
            transform.position += move * moveSpeed * Time.unscaledDeltaTime;
        }
        
        private void HandleLook()
        {
            Vector2 delta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            transform.Rotate(Vector3.up, delta.x * rotateSpeed * Time.unscaledDeltaTime, Space.World);
            transform.Rotate(Vector3.right, -delta.y * rotateSpeed * Time.unscaledDeltaTime, Space.Self);
        }
        
        private void CapturePhoto()
        {
            StartCoroutine(CaptureScreenshot());
        }
        
        private IEnumerator CaptureScreenshot()
        {
            // Hide UI
            photoModeUI?.SetActive(false);
            
            yield return new WaitForEndOfFrame();
            
            // Capture using ReadPixels (works without ScreenCapture module)
            string filename = $"ApexCitadels_Photo_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            string path = System.IO.Path.Combine(Application.persistentDataPath, "Screenshots", filename);
            
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            
            // Manual screenshot capture
            Texture2D screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            screenshot.Apply();
            
            byte[] bytes = screenshot.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, bytes);
            UnityEngine.Object.Destroy(screenshot);
            
            ApexLogger.Log($"Screenshot saved: {path}", LogCategory.General);
            
            // Show UI
            photoModeUI?.SetActive(true);
            
            // Flash effect
            yield return StartCoroutine(FlashEffect());
        }
        
        private IEnumerator FlashEffect()
        {
            // Would show white flash overlay
            yield return new WaitForSecondsRealtime(0.1f);
        }
        
        private void CycleFilter()
        {
            if (filterMaterials == null || filterMaterials.Length == 0) return;
            
            currentFilterIndex = (currentFilterIndex + 1) % (filterMaterials.Length + 1);
            
            if (currentFilterIndex == filterMaterials.Length)
            {
                // No filter - reset to default renderer
                // Note: URP renderer switching removed for compatibility
                if (filterNameText != null) filterNameText.text = "No Filter";
            }
            else
            {
                // Apply filter (would use post-processing volume in practice)
                if (filterNameText != null) filterNameText.text = filterMaterials[currentFilterIndex].name;
            }
        }
    }
}
