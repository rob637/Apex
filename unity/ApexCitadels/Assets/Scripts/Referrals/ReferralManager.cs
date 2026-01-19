using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ApexCitadels.Core;

namespace ApexCitadels.Referrals
{
    [Serializable]
    public class ReferralData
    {
        public string ReferralCode;
        public int TotalReferrals;
        public int SuccessfulReferrals;
        public List<ReferralReward> ClaimedRewards = new List<ReferralReward>();
        public List<ReferralReward> PendingRewards = new List<ReferralReward>();
    }

    [Serializable]
    public class ReferralReward
    {
        public string Type;
        public int Amount;
        public string ItemId;
        public bool Claimed;
    }

    [Serializable]
    public class ReferralMilestone
    {
        public int RequiredReferrals;
        public string RewardType;
        public int RewardAmount;
        public string RewardItemId;
        public string Description;
        public bool Claimed;
    }

    /// <summary>
    /// Referral Manager - requires Firebase SDK for full functionality
    /// </summary>
    public class ReferralManager : MonoBehaviour
    {
        public static ReferralManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool enableDebugLogs = false;

        public event Action OnReferralDataLoaded;
        public event Action<ReferralReward> OnRewardClaimed;
        public event Action<ReferralMilestone> OnMilestoneReached;
        public event Action<string> OnReferralCodeApplied;
        public event Action<string> OnError;

        private ReferralData _referralData = new ReferralData();
        private string _pendingReferralCode;
        private bool _isLoaded;

        public ReferralData Data => _referralData;
        public string ReferralCode => _referralData?.ReferralCode;
        public bool IsLoaded => _isLoaded;

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
            ApexLogger.LogWarning("[ReferralManager] Firebase SDK not installed. Running in stub mode.", ApexLogger.LogCategory.Social);
        }

        public void LoadReferralData()
        {
            _referralData.ReferralCode = "STUB_CODE";
            _isLoaded = true;
            OnReferralDataLoaded?.Invoke();
        }

        public async Task<bool> ApplyReferralCode(string code)
        {
            ApexLogger.Log($"[ReferralManager] Applying referral code: {code} (stub mode)", ApexLogger.LogCategory.Social);
            await Task.Delay(100);
            OnReferralCodeApplied?.Invoke(code);
            return true;
        }

        public async Task<bool> ApplyPendingReferralCode()
        {
            if (string.IsNullOrEmpty(_pendingReferralCode)) return false;
            return await ApplyReferralCode(_pendingReferralCode);
        }

        public void SetPendingReferralCode(string code)
        {
            _pendingReferralCode = code;
        }

        public async Task<bool> ClaimReward(ReferralReward reward)
        {
            ApexLogger.Log($"[ReferralManager] Claiming reward: {reward.Type} (stub mode)", ApexLogger.LogCategory.Social);
            await Task.Delay(100);
            reward.Claimed = true;
            OnRewardClaimed?.Invoke(reward);
            return true;
        }

        public string GetShareLink()
        {
            return $"https://apexcitadels.com/ref/{_referralData.ReferralCode}";
        }

        public void ShareReferralCode()
        {
            ApexLogger.Log($"[ReferralManager] Share link: {GetShareLink()}", ApexLogger.LogCategory.Social);
        }
    }
}
