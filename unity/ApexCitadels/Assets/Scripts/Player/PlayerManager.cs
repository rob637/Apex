using System;
using System.Threading.Tasks;
using UnityEngine;

namespace ApexCitadels.Player
{
    /// <summary>
    /// Manages the current player's profile, authentication, and state.
    /// </summary>
    public class PlayerManager : MonoBehaviour
    {
        public static PlayerManager Instance { get; private set; }

        // Current player
        public PlayerProfile CurrentPlayer { get; private set; }
        public bool IsLoggedIn => CurrentPlayer != null;

        // Events
        public event Action<PlayerProfile> OnPlayerLoggedIn;
        public event Action OnPlayerLoggedOut;
        public event Action<int> OnExperienceGained;
        public event Action<int> OnLevelUp;
        public event Action<ResourceType, int> OnResourceChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Try to auto-login with saved credentials
            TryAutoLogin();
        }

        #region Authentication

        /// <summary>
        /// Login with email/password (Firebase Auth)
        /// </summary>
        public async Task<LoginResult> Login(string email, string password)
        {
            Debug.Log($"[PlayerManager] Attempting login for {email}...");

            // TODO: Implement Firebase Authentication
            // For now, create a mock login
            await Task.Delay(500); // Simulate network delay

            // Mock success
            CurrentPlayer = new PlayerProfile
            {
                Email = email,
                DisplayName = email.Split('@')[0]
            };

            SaveLoginCredentials();
            OnPlayerLoggedIn?.Invoke(CurrentPlayer);

            Debug.Log($"[PlayerManager] Login successful! Player: {CurrentPlayer.DisplayName}");
            return new LoginResult(true, "Login successful!");
        }

        /// <summary>
        /// Register a new account
        /// </summary>
        public async Task<LoginResult> Register(string email, string password, string displayName)
        {
            Debug.Log($"[PlayerManager] Registering new account for {email}...");

            // TODO: Implement Firebase Registration
            await Task.Delay(500);

            CurrentPlayer = new PlayerProfile
            {
                Email = email,
                DisplayName = displayName
            };

            await SavePlayerToCloud();
            SaveLoginCredentials();
            OnPlayerLoggedIn?.Invoke(CurrentPlayer);

            Debug.Log($"[PlayerManager] Registration successful! Player: {CurrentPlayer.DisplayName}");
            return new LoginResult(true, "Account created!");
        }

        /// <summary>
        /// Login as guest (anonymous)
        /// </summary>
        public async Task<LoginResult> LoginAsGuest()
        {
            Debug.Log("[PlayerManager] Logging in as guest...");

            // TODO: Implement Firebase Anonymous Auth
            await Task.Delay(300);

            CurrentPlayer = new PlayerProfile
            {
                DisplayName = $"Guest_{UnityEngine.Random.Range(1000, 9999)}"
            };

            OnPlayerLoggedIn?.Invoke(CurrentPlayer);

            Debug.Log($"[PlayerManager] Guest login successful! Player: {CurrentPlayer.DisplayName}");
            return new LoginResult(true, "Logged in as guest");
        }

        /// <summary>
        /// Logout current player
        /// </summary>
        public void Logout()
        {
            Debug.Log("[PlayerManager] Logging out...");

            ClearLoginCredentials();
            CurrentPlayer = null;
            OnPlayerLoggedOut?.Invoke();

            Debug.Log("[PlayerManager] Logged out");
        }

        private void TryAutoLogin()
        {
            // Check for saved login token
            string savedPlayerId = PlayerPrefs.GetString("SavedPlayerId", "");
            if (!string.IsNullOrEmpty(savedPlayerId))
            {
                // TODO: Validate token with Firebase
                Debug.Log("[PlayerManager] Found saved credentials, attempting auto-login...");
                
                // For now, just create a guest session
                _ = LoginAsGuest();
            }
        }

        private void SaveLoginCredentials()
        {
            if (CurrentPlayer != null)
            {
                PlayerPrefs.SetString("SavedPlayerId", CurrentPlayer.Id);
                PlayerPrefs.Save();
            }
        }

        private void ClearLoginCredentials()
        {
            PlayerPrefs.DeleteKey("SavedPlayerId");
            PlayerPrefs.Save();
        }

        #endregion

        #region Experience & Leveling

        /// <summary>
        /// Award experience to the player
        /// </summary>
        public void AwardExperience(int amount)
        {
            if (CurrentPlayer == null) return;

            bool leveledUp = CurrentPlayer.AddExperience(amount);
            OnExperienceGained?.Invoke(amount);

            if (leveledUp)
            {
                OnLevelUp?.Invoke(CurrentPlayer.Level);
                Debug.Log($"[PlayerManager] Level up! Now level {CurrentPlayer.Level}");
            }

            _ = SavePlayerToCloud();
        }

        /// <summary>
        /// Experience rewards for actions
        /// </summary>
        public static class ExperienceRewards
        {
            public const int PlaceBlock = 5;
            public const int ClaimTerritory = 50;
            public const int ConquerTerritory = 100;
            public const int DefendTerritory = 25;
            public const int HarvestResources = 2;
        }

        #endregion

        #region Resources

        /// <summary>
        /// Check if player has enough resources
        /// </summary>
        public bool HasResources(int stone = 0, int wood = 0, int metal = 0, int crystal = 0)
        {
            return CurrentPlayer?.HasResources(stone, wood, metal, crystal) ?? false;
        }

        /// <summary>
        /// Spend resources (returns false if not enough)
        /// </summary>
        public bool SpendResources(int stone = 0, int wood = 0, int metal = 0, int crystal = 0)
        {
            if (CurrentPlayer == null) return false;

            if (CurrentPlayer.SpendResources(stone, wood, metal, crystal))
            {
                if (stone > 0) OnResourceChanged?.Invoke(ResourceType.Stone, -stone);
                if (wood > 0) OnResourceChanged?.Invoke(ResourceType.Wood, -wood);
                if (metal > 0) OnResourceChanged?.Invoke(ResourceType.Metal, -metal);
                if (crystal > 0) OnResourceChanged?.Invoke(ResourceType.Crystal, -crystal);

                _ = SavePlayerToCloud();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Add resources to player inventory
        /// </summary>
        public void AddResources(int stone = 0, int wood = 0, int metal = 0, int crystal = 0)
        {
            if (CurrentPlayer == null) return;

            CurrentPlayer.AddResources(stone, wood, metal, crystal);

            if (stone > 0) OnResourceChanged?.Invoke(ResourceType.Stone, stone);
            if (wood > 0) OnResourceChanged?.Invoke(ResourceType.Wood, wood);
            if (metal > 0) OnResourceChanged?.Invoke(ResourceType.Metal, metal);
            if (crystal > 0) OnResourceChanged?.Invoke(ResourceType.Crystal, crystal);

            _ = SavePlayerToCloud();
        }

        /// <summary>
        /// Get current resource amount
        /// </summary>
        public int GetResourceAmount(ResourceType type)
        {
            if (CurrentPlayer == null) return 0;

            return type switch
            {
                ResourceType.Stone => CurrentPlayer.Stone,
                ResourceType.Wood => CurrentPlayer.Wood,
                ResourceType.Metal => CurrentPlayer.Metal,
                ResourceType.Crystal => CurrentPlayer.Crystal,
                ResourceType.Gems => CurrentPlayer.Gems,
                _ => 0
            };
        }

        #endregion

        #region Stats

        /// <summary>
        /// Increment a stat counter
        /// </summary>
        public void IncrementStat(string statName, int amount = 1)
        {
            if (CurrentPlayer == null) return;

            switch (statName)
            {
                case "TerritoriesOwned":
                    CurrentPlayer.TerritoriesOwned += amount;
                    break;
                case "TerritoriesConquered":
                    CurrentPlayer.TerritoresConquered += amount;
                    break;
                case "TerritoriesLost":
                    CurrentPlayer.TerritoriesLost += amount;
                    break;
                case "BlocksPlaced":
                    CurrentPlayer.BlocksPlaced += amount;
                    break;
                case "BlocksDestroyed":
                    CurrentPlayer.BlocksDestroyed += amount;
                    break;
            }

            _ = SavePlayerToCloud();
        }

        #endregion

        #region Cloud Sync

        private async Task SavePlayerToCloud()
        {
            if (CurrentPlayer == null) return;

            Debug.Log("[PlayerManager] Saving player data to cloud...");
            
            // TODO: Implement Firebase save
            await Task.Delay(100);

            Debug.Log("[PlayerManager] Player data saved");
        }

        private async Task LoadPlayerFromCloud(string playerId)
        {
            Debug.Log($"[PlayerManager] Loading player {playerId} from cloud...");

            // TODO: Implement Firebase load
            await Task.Delay(100);

            Debug.Log("[PlayerManager] Player data loaded");
        }

        #endregion
    }

    /// <summary>
    /// Result of a login attempt
    /// </summary>
    public class LoginResult
    {
        public bool Success;
        public string Message;
        public string ErrorCode;

        public LoginResult(bool success, string message, string errorCode = null)
        {
            Success = success;
            Message = message;
            ErrorCode = errorCode;
        }
    }
}
