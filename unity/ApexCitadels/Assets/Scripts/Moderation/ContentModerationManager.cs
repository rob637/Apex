using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Functions;
using Newtonsoft.Json;

namespace ApexCitadels.Moderation
{
    /// <summary>
    /// Content moderation result
    /// </summary>
    [Serializable]
    public class ModerationResult
    {
        public bool Approved;
        public string FilteredContent;
        public List<ModerationFlag> Flags = new List<ModerationFlag>();
        public float ToxicityScore;
        public string AutoAction;
        public bool RequiresReview;
        public string Message;
    }

    /// <summary>
    /// Moderation flag details
    /// </summary>
    [Serializable]
    public class ModerationFlag
    {
        public string Type;
        public string Severity;
        public string MatchedPattern;
        public float Confidence;
    }

    /// <summary>
    /// Report submission result
    /// </summary>
    [Serializable]
    public class ReportResult
    {
        public bool Success;
        public string ReportId;
        public string Message;
    }

    /// <summary>
    /// User ban status
    /// </summary>
    [Serializable]
    public class BanStatus
    {
        public bool Banned;
        public string Reason;
        public DateTime? ExpiresAt;
        public bool Permanent;
        public bool CanAppeal;
    }

    /// <summary>
    /// Client-side content moderation with server validation
    /// </summary>
    public class ContentModerationManager : MonoBehaviour
    {
        public static ContentModerationManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool enableClientSideFilter = true;
        [SerializeField] private bool enableServerValidation = true;
        [SerializeField] private bool autoFilterContent = true;
        [SerializeField] private char filterCharacter = '*';

        // Events
        public event Action<ModerationResult> OnContentModerated;
        public event Action<string> OnUserWarning;
        public event Action<string, DateTime?> OnUserMuted;
        public event Action<BanStatus> OnUserBanned;
        public event Action<ReportResult> OnReportSubmitted;

        // State
        private FirebaseFunctions _functions;
        private bool _isMuted;
        private DateTime? _muteExpiresAt;
        private BanStatus _banStatus;
        
        // Client-side filter patterns (quick local filter)
        private List<Regex> _profanityPatterns = new List<Regex>();
        private List<Regex> _slurPatterns = new List<Regex>();
        private List<string> _bannedWords = new List<string>();

        public bool IsMuted => _isMuted && (_muteExpiresAt == null || _muteExpiresAt > DateTime.Now);
        public BanStatus CurrentBanStatus => _banStatus;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePatterns();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            _functions = FirebaseFunctions.DefaultInstance;
            CheckBanStatus();
        }

        /// <summary>
        /// Initialize client-side filter patterns
        /// </summary>
        private void InitializePatterns()
        {
            // Basic profanity patterns (client-side quick filter)
            _profanityPatterns.Add(new Regex(@"\b(f+[u*@]+[c*@]+[k*@]+)\b", RegexOptions.IgnoreCase));
            _profanityPatterns.Add(new Regex(@"\b(s+h+[i*@]+[t*@]+)\b", RegexOptions.IgnoreCase));
            _profanityPatterns.Add(new Regex(@"\b(a+[s*@]+[s*@]+)\b", RegexOptions.IgnoreCase));
            _profanityPatterns.Add(new Regex(@"\b(b+[i*@]+[t*@]+c+h+)\b", RegexOptions.IgnoreCase));
            
            // Common banned words
            _bannedWords.AddRange(new[] { 
                "admin", "moderator", "staff", "official", "support",
                "free gems", "hack", "cheat", "exploit"
            });
        }

        /// <summary>
        /// Check if user is banned on startup
        /// </summary>
        public async void CheckBanStatus()
        {
            try
            {
                var function = _functions.GetHttpsCallable("checkBanStatus");
                var result = await function.CallAsync();
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());

                _banStatus = new BanStatus
                {
                    Banned = (bool)response.GetValueOrDefault("banned", false)
                };

                if (_banStatus.Banned)
                {
                    _banStatus.Reason = response.GetValueOrDefault("reason", "")?.ToString();
                    _banStatus.Permanent = (bool)response.GetValueOrDefault("permanent", false);
                    _banStatus.CanAppeal = (bool)response.GetValueOrDefault("canAppeal", false);

                    if (response.ContainsKey("expiresAt") && response["expiresAt"] != null)
                    {
                        DateTime.TryParse(response["expiresAt"].ToString(), out DateTime expires);
                        _banStatus.ExpiresAt = expires;
                    }

                    OnUserBanned?.Invoke(_banStatus);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Moderation] Failed to check ban status: {ex.Message}");
            }
        }

        /// <summary>
        /// Moderate content before sending (chat, names, etc.)
        /// </summary>
        public async Task<ModerationResult> ModerateContent(string content, string contentType = "chat")
        {
            // Quick client-side check first
            if (enableClientSideFilter)
            {
                var quickResult = QuickClientFilter(content);
                if (!quickResult.Approved && !enableServerValidation)
                {
                    OnContentModerated?.Invoke(quickResult);
                    return quickResult;
                }
            }

            // Server validation for comprehensive check
            if (enableServerValidation)
            {
                try
                {
                    var function = _functions.GetHttpsCallable("moderateContent");
                    var data = new Dictionary<string, object>
                    {
                        ["content"] = content,
                        ["contentType"] = contentType
                    };

                    var result = await function.CallAsync(data);
                    var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());

                    var moderationResult = new ModerationResult
                    {
                        Approved = (bool)response.GetValueOrDefault("approved", false),
                        FilteredContent = response.GetValueOrDefault("filteredContent", "")?.ToString(),
                        ToxicityScore = Convert.ToSingle(response.GetValueOrDefault("toxicityScore", 0)),
                        AutoAction = response.GetValueOrDefault("autoAction", "none")?.ToString(),
                        RequiresReview = (bool)response.GetValueOrDefault("requiresReview", false),
                        Message = response.GetValueOrDefault("message", "")?.ToString()
                    };

                    // Parse flags
                    if (response.ContainsKey("flags"))
                    {
                        var flagsData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                            response["flags"].ToString()
                        );
                        foreach (var flag in flagsData)
                        {
                            moderationResult.Flags.Add(new ModerationFlag
                            {
                                Type = flag.GetValueOrDefault("type", "")?.ToString(),
                                Severity = flag.GetValueOrDefault("severity", "")?.ToString(),
                                Confidence = Convert.ToSingle(flag.GetValueOrDefault("confidence", 0))
                            });
                        }
                    }

                    // Handle auto-actions
                    HandleAutoAction(moderationResult);

                    OnContentModerated?.Invoke(moderationResult);
                    return moderationResult;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Moderation] Server validation failed: {ex.Message}");
                    
                    // Fall back to client-side result
                    if (enableClientSideFilter)
                    {
                        return QuickClientFilter(content);
                    }
                }
            }

            // Default: approve if no validation
            return new ModerationResult { Approved = true };
        }

        /// <summary>
        /// Quick client-side filter for immediate feedback
        /// </summary>
        private ModerationResult QuickClientFilter(string content)
        {
            var result = new ModerationResult
            {
                Approved = true,
                ToxicityScore = 0
            };

            string filtered = content;

            // Check profanity patterns
            foreach (var pattern in _profanityPatterns)
            {
                if (pattern.IsMatch(content))
                {
                    result.Flags.Add(new ModerationFlag
                    {
                        Type = "profanity",
                        Severity = "medium",
                        Confidence = 0.8f
                    });
                    result.ToxicityScore += 20;
                    
                    if (autoFilterContent)
                    {
                        filtered = pattern.Replace(filtered, match => 
                            new string(filterCharacter, match.Length));
                    }
                }
            }

            // Check slur patterns (more severe)
            foreach (var pattern in _slurPatterns)
            {
                if (pattern.IsMatch(content))
                {
                    result.Flags.Add(new ModerationFlag
                    {
                        Type = "slur",
                        Severity = "critical",
                        Confidence = 0.9f
                    });
                    result.ToxicityScore += 50;
                    result.Approved = false;
                }
            }

            // Check banned words
            string lowerContent = content.ToLower();
            foreach (var word in _bannedWords)
            {
                if (lowerContent.Contains(word.ToLower()))
                {
                    result.Flags.Add(new ModerationFlag
                    {
                        Type = "inappropriate",
                        Severity = "low",
                        Confidence = 0.7f
                    });
                    result.ToxicityScore += 10;
                }
            }

            // Set filtered content if changes were made
            if (filtered != content)
            {
                result.FilteredContent = filtered;
            }

            // Block if too toxic
            if (result.ToxicityScore >= 50)
            {
                result.Approved = false;
            }

            return result;
        }

        /// <summary>
        /// Handle auto-actions from moderation
        /// </summary>
        private void HandleAutoAction(ModerationResult result)
        {
            switch (result.AutoAction)
            {
                case "warn":
                    OnUserWarning?.Invoke(result.Message ?? "Your message contained inappropriate content.");
                    break;
                case "mute":
                    _isMuted = true;
                    _muteExpiresAt = DateTime.Now.AddMinutes(30); // Default mute duration
                    OnUserMuted?.Invoke(result.Message ?? "You have been muted.", _muteExpiresAt);
                    break;
                case "block":
                    // Message was blocked, no additional action needed
                    break;
            }
        }

        /// <summary>
        /// Filter content locally (for display of received messages)
        /// </summary>
        public string FilterDisplayContent(string content)
        {
            if (!enableClientSideFilter) return content;

            string filtered = content;
            
            foreach (var pattern in _profanityPatterns)
            {
                filtered = pattern.Replace(filtered, match => 
                    new string(filterCharacter, match.Length));
            }

            foreach (var pattern in _slurPatterns)
            {
                filtered = pattern.Replace(filtered, match => 
                    new string(filterCharacter, match.Length));
            }

            return filtered;
        }

        /// <summary>
        /// Report content to moderators
        /// </summary>
        public async Task<ReportResult> ReportContent(
            string reportedUserId,
            string contentType,
            string contentId,
            string content,
            string reason,
            string description = null)
        {
            try
            {
                var function = _functions.GetHttpsCallable("reportContent");
                var data = new Dictionary<string, object>
                {
                    ["reportedUserId"] = reportedUserId,
                    ["contentType"] = contentType,
                    ["contentId"] = contentId,
                    ["content"] = content,
                    ["reason"] = reason,
                    ["description"] = description
                };

                var result = await function.CallAsync(data);
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());

                var reportResult = new ReportResult
                {
                    Success = (bool)response.GetValueOrDefault("success", false),
                    ReportId = response.GetValueOrDefault("reportId", "")?.ToString(),
                    Message = response.GetValueOrDefault("message", "")?.ToString()
                };

                OnReportSubmitted?.Invoke(reportResult);
                return reportResult;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Moderation] Failed to submit report: {ex.Message}");
                return new ReportResult
                {
                    Success = false,
                    Message = "Failed to submit report. Please try again later."
                };
            }
        }

        /// <summary>
        /// Appeal a ban
        /// </summary>
        public async Task<bool> AppealBan(string banId, string reason)
        {
            try
            {
                var function = _functions.GetHttpsCallable("appealBan");
                var data = new Dictionary<string, object>
                {
                    ["banId"] = banId,
                    ["reason"] = reason
                };

                var result = await function.CallAsync(data);
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());

                return (bool)response.GetValueOrDefault("success", false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Moderation] Failed to submit appeal: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if content is safe (quick sync check)
        /// </summary>
        public bool IsContentSafe(string content)
        {
            var result = QuickClientFilter(content);
            return result.Approved;
        }

        /// <summary>
        /// Get filtered version of content
        /// </summary>
        public string GetFilteredContent(string content)
        {
            var result = QuickClientFilter(content);
            return result.FilteredContent ?? content;
        }

        /// <summary>
        /// Add custom banned word (for user-specific blocks)
        /// </summary>
        public void AddBannedWord(string word)
        {
            if (!_bannedWords.Contains(word.ToLower()))
            {
                _bannedWords.Add(word.ToLower());
            }
        }

        /// <summary>
        /// Remove custom banned word
        /// </summary>
        public void RemoveBannedWord(string word)
        {
            _bannedWords.Remove(word.ToLower());
        }
    }

    /// <summary>
    /// Extension methods
    /// </summary>
    public static class ModerationExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(
            this Dictionary<TKey, TValue> dictionary,
            TKey key,
            TValue defaultValue = default)
        {
            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
        }
    }
}
