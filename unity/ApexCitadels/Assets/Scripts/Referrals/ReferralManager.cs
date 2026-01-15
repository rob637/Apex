using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Extensions;
using Firebase.Firestore;
using Firebase.Functions;
using Newtonsoft.Json;

namespace ApexCitadels.Referrals
{
    /// <summary>
    /// Referral code data
    /// </summary>
    [Serializable]
    public class ReferralCode
    {
        public string Code;
        public string OwnerId;
        public string OwnerName;
        public int TotalUses;
        public int TotalRewardsEarned;
        public DateTime CreatedAt;
        public bool IsActive;
    }

    /// <summary>
    /// Referral milestone
    /// </summary>
    [Serializable]
    public class ReferralMilestone
    {
        public int RequiredReferrals;
        public string RewardType;
        public int RewardAmount;
        public string RewardId;
        public string Description;
        public bool IsClaimed;
    }

    /// <summary>
    /// Referred user info
    /// </summary>
    [Serializable]
    public class ReferredUser
    {
        public string UserId;
        public string DisplayName;
        public int Level;
        public DateTime JoinedAt;
        public int RewardsGenerated;
    }

    /// <summary>
    /// Viral challenge data
    /// </summary>
    [Serializable]
    public class ViralChallenge
    {
        public string Id;
        public string Title;
        public string Description;
        public string Type;
        public int TargetCount;
        public int CurrentCount;
        public DateTime EndsAt;
        public bool IsCompleted;
        public List<ViralChallengeReward> Rewards;
    }

    /// <summary>
    /// Viral challenge reward
    /// </summary>
    [Serializable]
    public class ViralChallengeReward
    {
        public string Type;
        public int Amount;
        public string ItemId;
    }

    /// <summary>
    /// Share result tracking
    /// </summary>
    [Serializable]
    public class ShareResult
    {
        public string Platform;
        public bool Success;
        public DateTime Timestamp;
    }

    /// <summary>
    /// Manages referral system and viral growth
    /// </summary>
    public class ReferralManager : MonoBehaviour
    {
        public static ReferralManager Instance { get; private set; }

        // Events
        public event Action<ReferralCode> OnReferralCodeLoaded;
        public event Action<List<ReferredUser>> OnReferredUsersUpdated;
        public event Action<List<ReferralMilestone>> OnMilestonesUpdated;
        public event Action<ReferralMilestone> OnMilestoneReached;
        public event Action<List<ViralChallenge>> OnViralChallengesUpdated;
        public event Action<int> OnReferralRewardEarned;

        // State
        private FirebaseFirestore _db;
        private FirebaseFunctions _functions;
        private string _userId;

        private ReferralCode _myReferralCode;
        private List<ReferredUser> _referredUsers = new List<ReferredUser>();
        private List<ReferralMilestone> _milestones = new List<ReferralMilestone>();
        private List<ViralChallenge> _viralChallenges = new List<ViralChallenge>();
        private string _referredByCode;

        public ReferralCode MyReferralCode => _myReferralCode;
        public List<ReferredUser> ReferredUsers => _referredUsers;
        public List<ReferralMilestone> Milestones => _milestones;
        public List<ViralChallenge> ViralChallenges => _viralChallenges;
        public int TotalReferrals => _referredUsers.Count;

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
            }
        }

        private void Start()
        {
            _db = FirebaseFirestore.DefaultInstance;
            _functions = FirebaseFunctions.DefaultInstance;
            _userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;

            if (!string.IsNullOrEmpty(_userId))
            {
                LoadReferralData();
                LoadViralChallenges();
            }
        }

        /// <summary>
        /// Initialize referral data
        /// </summary>
        public async void LoadReferralData()
        {
            await LoadMyReferralCode();
            await LoadReferredUsers();
            await LoadMilestones();
        }

        /// <summary>
        /// Get or create user's referral code
        /// </summary>
        public async Task<ReferralCode> LoadMyReferralCode()
        {
            try
            {
                var callable = _functions.GetHttpsCallable("getOrCreateReferralCode");
                var result = await callable.CallAsync(null);

                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                _myReferralCode = JsonConvert.DeserializeObject<ReferralCode>(
                    JsonConvert.SerializeObject(response["referralCode"]));

                OnReferralCodeLoaded?.Invoke(_myReferralCode);
                return _myReferralCode;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load referral code: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load list of referred users
        /// </summary>
        public async Task LoadReferredUsers()
        {
            try
            {
                var callable = _functions.GetHttpsCallable("getReferredUsers");
                var result = await callable.CallAsync(null);

                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                _referredUsers = JsonConvert.DeserializeObject<List<ReferredUser>>(
                    JsonConvert.SerializeObject(response["referredUsers"]));

                OnReferredUsersUpdated?.Invoke(_referredUsers);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load referred users: {e.Message}");
            }
        }

        /// <summary>
        /// Load referral milestones
        /// </summary>
        public async Task LoadMilestones()
        {
            try
            {
                var callable = _functions.GetHttpsCallable("getReferralMilestones");
                var result = await callable.CallAsync(null);

                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                _milestones = JsonConvert.DeserializeObject<List<ReferralMilestone>>(
                    JsonConvert.SerializeObject(response["milestones"]));

                OnMilestonesUpdated?.Invoke(_milestones);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load milestones: {e.Message}");
            }
        }

        /// <summary>
        /// Apply a referral code (for new users)
        /// </summary>
        public async Task<bool> ApplyReferralCode(string code)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("applyReferralCode");
                var data = new Dictionary<string, object> { { "code", code.ToUpper() } };
                var result = await callable.CallAsync(data);

                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                
                if (response.ContainsKey("success") && (bool)response["success"])
                {
                    _referredByCode = code;
                    
                    // Track analytics
                    Analytics.AnalyticsManager.Instance?.TrackEvent(
                        Analytics.AnalyticsEvents.REFERRAL_CODE_USED,
                        new Dictionary<string, object> { { "code", code } });
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to apply referral code: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Claim a milestone reward
        /// </summary>
        public async Task<bool> ClaimMilestoneReward(int milestoneIndex)
        {
            if (milestoneIndex < 0 || milestoneIndex >= _milestones.Count)
                return false;

            var milestone = _milestones[milestoneIndex];
            if (milestone.IsClaimed || TotalReferrals < milestone.RequiredReferrals)
                return false;

            try
            {
                var callable = _functions.GetHttpsCallable("claimReferralMilestone");
                var data = new Dictionary<string, object> 
                { 
                    { "requiredReferrals", milestone.RequiredReferrals } 
                };
                var result = await callable.CallAsync(data);

                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                
                if (response.ContainsKey("success") && (bool)response["success"])
                {
                    milestone.IsClaimed = true;
                    OnMilestonesUpdated?.Invoke(_milestones);
                    OnMilestoneReached?.Invoke(milestone);
                    
                    // Track analytics
                    Analytics.AnalyticsManager.Instance?.TrackEvent(
                        Analytics.AnalyticsEvents.REFERRAL_REWARD_CLAIMED,
                        new Dictionary<string, object>
                        {
                            { "milestone", milestone.RequiredReferrals },
                            { "reward_type", milestone.RewardType },
                            { "reward_amount", milestone.RewardAmount }
                        });
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to claim milestone: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load viral challenges
        /// </summary>
        public async void LoadViralChallenges()
        {
            try
            {
                var callable = _functions.GetHttpsCallable("getViralChallenges");
                var result = await callable.CallAsync(null);

                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                _viralChallenges = JsonConvert.DeserializeObject<List<ViralChallenge>>(
                    JsonConvert.SerializeObject(response["challenges"]));

                OnViralChallengesUpdated?.Invoke(_viralChallenges);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load viral challenges: {e.Message}");
            }
        }

        /// <summary>
        /// Record a share action
        /// </summary>
        public async Task RecordShare(string platform, bool success)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("recordShare");
                var data = new Dictionary<string, object>
                {
                    { "platform", platform },
                    { "success", success }
                };
                await callable.CallAsync(data);

                // Update viral challenges
                LoadViralChallenges();
                
                // Track analytics
                Analytics.AnalyticsManager.Instance?.TrackEvent(
                    Analytics.AnalyticsEvents.SHARE_COMPLETED,
                    new Dictionary<string, object>
                    {
                        { "platform", platform },
                        { "success", success }
                    });
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to record share: {e.Message}");
            }
        }

        /// <summary>
        /// Share referral code via native sharing
        /// </summary>
        public void ShareReferralCode()
        {
            if (_myReferralCode == null)
            {
                Debug.LogWarning("Referral code not loaded");
                return;
            }

            string shareMessage = $"Join me in Apex Citadels! Use my code {_myReferralCode.Code} to get bonus rewards when you sign up! Download now: https://apexcitadels.app/invite/{_myReferralCode.Code}";

#if UNITY_ANDROID
            ShareOnAndroid(shareMessage);
#elif UNITY_IOS
            ShareOnIOS(shareMessage);
#else
            // Copy to clipboard for desktop
            GUIUtility.systemCopyBuffer = shareMessage;
            Debug.Log("Referral link copied to clipboard");
#endif
        }

        /// <summary>
        /// Share a screenshot with referral overlay
        /// </summary>
        public async void ShareScreenshot(string caption = null)
        {
            // Capture screenshot
            yield return new WaitForEndOfFrame();
            
            Texture2D screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            screenshot.Apply();

            // Add referral watermark (in production, use proper image overlay)
            string shareCaption = caption ?? $"Dominating in Apex Citadels! Join with code {_myReferralCode?.Code}";

#if UNITY_ANDROID || UNITY_IOS
            // Save temp file and share
            string path = System.IO.Path.Combine(Application.temporaryCachePath, "share_screenshot.png");
            System.IO.File.WriteAllBytes(path, screenshot.EncodeToPNG());
            
            // Use native share with image
            NativeShare share = new NativeShare();
            share.AddFile(path);
            share.SetText(shareCaption);
            share.Share();
            
            await RecordShare("screenshot", true);
#endif

            Destroy(screenshot);
        }

        /// <summary>
        /// Share achievement
        /// </summary>
        public void ShareAchievement(string achievementName, string achievementDescription)
        {
            string shareMessage = $"I just unlocked '{achievementName}' in Apex Citadels! {achievementDescription} Join with code {_myReferralCode?.Code}";
            
#if UNITY_ANDROID
            ShareOnAndroid(shareMessage);
#elif UNITY_IOS
            ShareOnIOS(shareMessage);
#else
            GUIUtility.systemCopyBuffer = shareMessage;
#endif
        }

        /// <summary>
        /// Share battle victory
        /// </summary>
        public void ShareVictory(string territoryName, int enemiesDefeated)
        {
            string shareMessage = $"Victory! Just conquered {territoryName} and defeated {enemiesDefeated} enemies in Apex Citadels! Think you can beat me? Code: {_myReferralCode?.Code}";
            
#if UNITY_ANDROID
            ShareOnAndroid(shareMessage);
#elif UNITY_IOS
            ShareOnIOS(shareMessage);
#else
            GUIUtility.systemCopyBuffer = shareMessage;
#endif
        }

        /// <summary>
        /// Get share URL for deep linking
        /// </summary>
        public string GetShareUrl()
        {
            return $"https://apexcitadels.app/invite/{_myReferralCode?.Code ?? "APEX"}";
        }

        /// <summary>
        /// Get the next unclaimed milestone
        /// </summary>
        public ReferralMilestone GetNextMilestone()
        {
            foreach (var milestone in _milestones)
            {
                if (!milestone.IsClaimed)
                    return milestone;
            }
            return null;
        }

        /// <summary>
        /// Get progress to next milestone (0-1)
        /// </summary>
        public float GetProgressToNextMilestone()
        {
            var next = GetNextMilestone();
            if (next == null) return 1f;

            // Find previous milestone threshold
            int previousThreshold = 0;
            foreach (var milestone in _milestones)
            {
                if (milestone == next) break;
                previousThreshold = milestone.RequiredReferrals;
            }

            float progress = (float)(TotalReferrals - previousThreshold) / 
                            (next.RequiredReferrals - previousThreshold);
            return Mathf.Clamp01(progress);
        }

        /// <summary>
        /// Check if user was referred
        /// </summary>
        public bool WasReferred()
        {
            return !string.IsNullOrEmpty(_referredByCode);
        }

#if UNITY_ANDROID
        private void ShareOnAndroid(string message)
        {
            using (AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent"))
            using (AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent"))
            {
                intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
                intentObject.Call<AndroidJavaObject>("setType", "text/plain");
                intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), message);

                using (AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    AndroidJavaObject chooser = intentClass.CallStatic<AndroidJavaObject>(
                        "createChooser", intentObject, "Share via");
                    currentActivity.Call("startActivity", chooser);
                }
            }
            
            _ = RecordShare("android_native", true);
        }
#endif

#if UNITY_IOS
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void _ShareText(string text);

        private void ShareOnIOS(string message)
        {
            _ShareText(message);
            _ = RecordShare("ios_native", true);
        }
#endif

        /// <summary>
        /// Handle deep link (called from app delegate/activity)
        /// </summary>
        public void HandleDeepLink(string url)
        {
            // Parse referral code from URL
            // Format: https://apexcitadels.app/invite/CODE
            if (url.Contains("/invite/"))
            {
                var parts = url.Split(new[] { "/invite/" }, StringSplitOptions.None);
                if (parts.Length > 1)
                {
                    string code = parts[1].Split('?')[0].Trim();
                    if (!string.IsNullOrEmpty(code))
                    {
                        // Store for new user registration
                        PlayerPrefs.SetString("pending_referral_code", code);
                        PlayerPrefs.Save();
                        
                        Debug.Log($"Referral code from deep link: {code}");
                    }
                }
            }
        }

        /// <summary>
        /// Apply pending referral code (call after user registration)
        /// </summary>
        public async Task ApplyPendingReferralCode()
        {
            string pendingCode = PlayerPrefs.GetString("pending_referral_code", "");
            if (!string.IsNullOrEmpty(pendingCode))
            {
                bool success = await ApplyReferralCode(pendingCode);
                if (success)
                {
                    PlayerPrefs.DeleteKey("pending_referral_code");
                    PlayerPrefs.Save();
                }
            }
        }
    }
}
