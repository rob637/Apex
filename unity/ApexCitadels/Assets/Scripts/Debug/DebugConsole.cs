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
                    Log("Resources: stone, wood, metal, crystal, gems, xp");
                    return;
                }
                
                string resource = args[0].ToLower();
                if (!int.TryParse(args[1], out int amount))
                {
                    Log("Invalid amount");
                    return;
                }

                var player = Player.PlayerManager.Instance?.CurrentPlayer;
                if (player == null)
                {
                    Log("No player logged in");
                    return;
                }

                switch (resource)
                {
                    case "stone":
                        player.Stone += amount;
                        Log($"Added {amount} stone. Total: {player.Stone}");
                        break;
                    case "wood":
                        player.Wood += amount;
                        Log($"Added {amount} wood. Total: {player.Wood}");
                        break;
                    case "metal":
                        player.Metal += amount;
                        Log($"Added {amount} metal. Total: {player.Metal}");
                        break;
                    case "crystal":
                        player.Crystal += amount;
                        Log($"Added {amount} crystal. Total: {player.Crystal}");
                        break;
                    case "gems":
                        player.Gems += amount;
                        Log($"Added {amount} gems. Total: {player.Gems}");
                        break;
                    case "xp":
                        Player.PlayerManager.Instance.AwardExperience(amount);
                        Log($"Added {amount} XP. Level: {player.Level}");
                        break;
                    default:
                        Log($"Unknown resource: {resource}");
                        break;
                }
            });

            RegisterCommand("teleport", args => {
                if (args.Length < 2)
                {
                    Log("Usage: teleport <lat> <lon>");
                    Log("Presets: sf, nyc, tokyo, london, sydney");
                    return;
                }

                double lat, lon;
                
                // Check for preset locations
                string preset = args[0].ToLower();
                switch (preset)
                {
                    case "sf":
                        lat = 37.7749; lon = -122.4194;
                        break;
                    case "nyc":
                        lat = 40.7128; lon = -74.0060;
                        break;
                    case "tokyo":
                        lat = 35.6762; lon = 139.6503;
                        break;
                    case "london":
                        lat = 51.5074; lon = -0.1278;
                        break;
                    case "sydney":
                        lat = -33.8688; lon = 151.2093;
                        break;
                    default:
                        if (!double.TryParse(args[0], out lat) || !double.TryParse(args[1], out lon))
                        {
                            Log("Invalid coordinates");
                            return;
                        }
                        break;
                }

                // Set mock GPS location in SpatialAnchorManager
                if (AR.SpatialAnchorManager.Instance != null)
                {
                    AR.SpatialAnchorManager.Instance.SetMockLocation(lat, lon, 0);
                    Log($"Teleported to ({lat:F4}, {lon:F4})");
                }
                else
                {
                    Log("SpatialAnchorManager not available");
                }
            });

            RegisterCommand("claim", args => {
                Log("Claiming territory at current location...");
                UI.GameUIController uiController = FindFirstObjectByType<UI.GameUIController>();
                if (uiController != null)
                {
                    // Trigger claim via UI
                    Log("Use the Claim button in game UI");
                }
            });

            RegisterCommand("territory", args => {
                if (args.Length == 0)
                {
                    Log("Territory Subcommands: list, info, mine");
                    return;
                }

                switch (args[0].ToLower())
                {
                    case "list":
                        if (Territory.TerritoryManager.Instance != null)
                        {
                            var territories = Territory.TerritoryManager.Instance.GetAllTerritories();
                            Log($"Total territories: {territories.Count}");
                            foreach (var t in territories)
                            {
                                Log($"  {t.Name} (Lv{t.Level}) - Owner: {t.OwnerId ?? "None"}");
                            }
                        }
                        break;

                    case "mine":
                        if (Territory.TerritoryManager.Instance != null)
                        {
                            var playerId = Player.PlayerManager.Instance?.GetCurrentPlayerId();
                            var myTerritories = Territory.TerritoryManager.Instance.GetPlayerTerritories(playerId);
                            Log($"Your territories: {myTerritories.Count}");
                            foreach (var t in myTerritories)
                            {
                                Log($"  {t.Name} (Lv{t.Level}) at ({t.Latitude:F4}, {t.Longitude:F4})");
                            }
                        }
                        break;

                    default:
                        Log($"Unknown territory subcommand: {args[0]}");
                        break;
                }
            });

            RegisterCommand("spawn", args => {
                if (args.Length == 0)
                {
                    Log("Usage: spawn <type> - spawns test entities");
                    Log("Types: enemy, resource, node");
                    return;
                }

                Log($"Spawning {args[0]}... (test feature)");
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
