using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.Debugging
{
    /// <summary>
    /// In-game debug console for testing and development
    /// </summary>
    public class DebugConsole : MonoBehaviour
    {
        public static DebugConsole Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject consolePanel;
        [SerializeField] private TMP_InputField commandInput;
        [SerializeField] private TextMeshProUGUI logText;
        [SerializeField] private ScrollRect logScrollRect;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button clearButton;

        [Header("Settings")]
        [SerializeField] private int maxLogLines = 100;
        [SerializeField] private bool showTimestamps = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote;

        // Log storage
        private List<string> _logLines = new List<string>();
        private Dictionary<string, Action<string[]>> _commands = new Dictionary<string, Action<string[]>>();

        public bool IsVisible => consolePanel != null && consolePanel.activeSelf;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            RegisterDefaultCommands();
        }

        private void Start()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            if (clearButton != null)
                clearButton.onClick.AddListener(ClearLog);

            if (commandInput != null)
                commandInput.onSubmit.AddListener(OnCommandSubmit);

            // Subscribe to Unity logs
            Application.logMessageReceived += HandleLog;

            // Start hidden
            if (consolePanel != null)
                consolePanel.SetActive(false);
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLog;

            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            // Toggle console with key
            if (Input.GetKeyDown(toggleKey))
            {
                Toggle();
            }

            // Also support triple-tap for mobile
            if (Input.touchCount == 4)
            {
                Toggle();
            }
        }

        #region Visibility

        public void Show()
        {
            if (consolePanel != null)
            {
                consolePanel.SetActive(true);
                if (commandInput != null)
                    commandInput.ActivateInputField();
            }
        }

        public void Hide()
        {
            if (consolePanel != null)
                consolePanel.SetActive(false);
        }

        public void Toggle()
        {
            if (IsVisible)
                Hide();
            else
                Show();
        }

        #endregion

        #region Logging

        private void HandleLog(string message, string stackTrace, LogType type)
        {
            string prefix = type switch
            {
                LogType.Error => "<color=red>[ERROR]</color>",
                LogType.Warning => "<color=yellow>[WARN]</color>",
                LogType.Exception => "<color=red>[EXCEPTION]</color>",
                _ => "<color=white>[INFO]</color>"
            };

            string timestamp = showTimestamps ? $"[{DateTime.Now:HH:mm:ss}] " : "";
            string line = $"{timestamp}{prefix} {message}";

            AddLogLine(line);

            if (type == LogType.Exception)
            {
                AddLogLine($"<color=gray>{stackTrace}</color>");
            }
        }

        public void Log(string message)
        {
            string timestamp = showTimestamps ? $"[{DateTime.Now:HH:mm:ss}] " : "";
            AddLogLine($"{timestamp}<color=cyan>[CONSOLE]</color> {message}");
        }

        private void AddLogLine(string line)
        {
            _logLines.Add(line);

            // Trim old lines
            while (_logLines.Count > maxLogLines)
            {
                _logLines.RemoveAt(0);
            }

            UpdateLogDisplay();
        }

        private void UpdateLogDisplay()
        {
            if (logText != null)
            {
                logText.text = string.Join("\n", _logLines);
            }

            // Scroll to bottom
            if (logScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                logScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        public void ClearLog()
        {
            _logLines.Clear();
            UpdateLogDisplay();
            Log("Console cleared");
        }

        #endregion

        #region Commands

        public void RegisterCommand(string name, Action<string[]> handler, string description = "")
        {
            _commands[name.ToLower()] = handler;
            UnityEngine.Debug.Log($"[DebugConsole] Registered command: {name}");
        }

        private void OnCommandSubmit(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;

            Log($"> {input}");
            ExecuteCommand(input);

            if (commandInput != null)
            {
                commandInput.text = "";
                commandInput.ActivateInputField();
            }
        }

        public void ExecuteCommand(string input)
        {
            string[] parts = input.Trim().Split(' ');
            if (parts.Length == 0) return;

            string command = parts[0].ToLower();
            string[] args = new string[parts.Length - 1];
            Array.Copy(parts, 1, args, 0, args.Length);

            if (_commands.TryGetValue(command, out var handler))
            {
                try
                {
                    handler(args);
                }
                catch (Exception e)
                {
                    Log($"<color=red>Command error: {e.Message}</color>");
                }
            }
            else
            {
                Log($"<color=yellow>Unknown command: {command}. Type 'help' for available commands.</color>");
            }
        }

        private void RegisterDefaultCommands()
        {
            RegisterCommand("help", args => {
                Log("Available commands:");
                foreach (var cmd in _commands.Keys)
                {
                    Log($"  - {cmd}");
                }
            });

            RegisterCommand("clear", args => ClearLog());

            RegisterCommand("scene", args => {
                if (args.Length == 0)
                {
                    Log($"Current scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
                }
                else
                {
                    string sceneName = args[0];
                    if (Core.SceneLoader.Instance != null)
                    {
                        Core.SceneLoader.Instance.LoadScene(sceneName);
                        Log($"Loading scene: {sceneName}");
                    }
                    else
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
                    }
                }
            });

            RegisterCommand("fps", args => {
                Log($"FPS: {(1f / Time.deltaTime):F1}");
            });

            RegisterCommand("position", args => {
                var cam = Camera.main;
                if (cam != null)
                {
                    Log($"Camera Position: {cam.transform.position}");
                }
            });

            RegisterCommand("timescale", args => {
                if (args.Length == 0)
                {
                    Log($"Time scale: {Time.timeScale}");
                }
                else if (float.TryParse(args[0], out float scale))
                {
                    Time.timeScale = Mathf.Clamp(scale, 0f, 10f);
                    Log($"Time scale set to: {Time.timeScale}");
                }
            });

            RegisterCommand("resources", args => {
                if (Player.PlayerManager.Instance?.CurrentPlayer != null)
                {
                    var player = Player.PlayerManager.Instance.CurrentPlayer;
                    Log($"Stone: {player.Stone}");
                    Log($"Wood: {player.Wood}");
                    Log($"Metal: {player.Metal}");
                    Log($"Crystal: {player.Crystal}");
                    Log($"Gems: {player.Gems}");
                }
                else
                {
                    Log("No player logged in");
                }
            });

            RegisterCommand("give", args => {
                if (args.Length < 2)
                {
                    Log("Usage: give <resource> <amount>");
                    return;
                }
                
                string resource = args[0].ToLower();
                if (!int.TryParse(args[1], out int amount))
                {
                    Log("Invalid amount");
                    return;
                }

                // TODO: Add resources to player
                Log($"Gave {amount} {resource} (not implemented)");
            });

            RegisterCommand("teleport", args => {
                if (args.Length < 2)
                {
                    Log("Usage: teleport <lat> <lon>");
                    return;
                }

                if (double.TryParse(args[0], out double lat) && double.TryParse(args[1], out double lon))
                {
                    Log($"Teleporting to ({lat}, {lon}) - not implemented");
                }
            });

            RegisterCommand("ar", args => {
                if (args.Length == 0)
                {
                    Log("AR Subcommands: status, reset, desktop");
                    return;
                }

                switch (args[0].ToLower())
                {
                    case "status":
                        if (AR.SpatialAnchorManager.Instance != null)
                        {
                            var sam = AR.SpatialAnchorManager.Instance;
                            Log($"AR Initialized: {sam.IsInitialized}");
                            Log($"Tracking: {sam.IsTracking}");
                            Log($"Desktop Mode: {sam.IsDesktopMode}");
                            Log($"Active Anchors: {sam.ActiveAnchorCount}");
                        }
                        else
                        {
                            Log("SpatialAnchorManager not found");
                        }
                        break;

                    case "reset":
                        Log("AR reset not implemented");
                        break;

                    default:
                        Log($"Unknown AR subcommand: {args[0]}");
                        break;
                }
            });

            RegisterCommand("quit", args => {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
            });
        }

        #endregion
    }
}
