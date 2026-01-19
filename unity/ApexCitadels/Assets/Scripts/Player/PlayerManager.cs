using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using ApexCitadels.Data;
#if FIREBASE_ENABLED
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
#endif

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

#if FIREBASE_ENABLED
        private FirebaseAuth _auth;
        private FirebaseFirestore _db;
        private FirebaseUser _firebaseUser;
#endif

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
#if FIREBASE_ENABLED
            _auth = FirebaseAuth.DefaultInstance;
            _db = FirebaseFirestore.DefaultInstance;
            
            // Listen for auth state changes
            _auth.StateChanged += OnAuthStateChanged;
#endif
            // Try to auto-login with saved credentials
            TryAutoLogin();
        }

        private void OnDestroy()
        {
#if FIREBASE_ENABLED
            if (_auth != null)
            {
                _auth.StateChanged -= OnAuthStateChanged;
            }
#endif
        }

#if FIREBASE_ENABLED
        private void OnAuthStateChanged(object sender, EventArgs e)
        {
            if (_auth.CurrentUser != _firebaseUser)
            {
                bool signedIn = _auth.CurrentUser != null;
                if (!signedIn && _firebaseUser != null)
                {
                    Debug.Log("[PlayerManager] User signed out");
                    CurrentPlayer = null;
                    OnPlayerLoggedOut?.Invoke();
                }
                _firebaseUser = _auth.CurrentUser;
            }
        }
#endif

        #region Authentication

        /// <summary>
        /// Login with email/password (Firebase Auth)
        /// </summary>
        public async Task<LoginResult> Login(string email, string password)
        {
            Debug.Log($"[PlayerManager] Attempting login for {email}...");

#if FIREBASE_ENABLED
            try
            {
                var authResult = await _auth.SignInWithEmailAndPasswordAsync(email, password);
                _firebaseUser = authResult.User;
                
                Debug.Log($"[PlayerManager] Firebase Auth successful for {_firebaseUser.UserId}");
                
                // Load or create player profile from Firestore
                await LoadOrCreatePlayerProfile(_firebaseUser);
                
                SaveLoginCredentials();
                OnPlayerLoggedIn?.Invoke(CurrentPlayer);

                Debug.Log($"[PlayerManager] Login successful! Player: {CurrentPlayer.DisplayName}");
                return new LoginResult(true, "Login successful!");
            }
            catch (Firebase.FirebaseException ex)
            {
                Debug.LogError($"[PlayerManager] Firebase Auth failed: {ex.Message}");
                return new LoginResult(false, GetAuthErrorMessage(ex.ErrorCode), ex.ErrorCode.ToString());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerManager] Login failed: {ex.Message}");
                return new LoginResult(false, "Login failed. Please try again.", "UNKNOWN");
            }
#else
            // Fallback for non-Firebase builds (Editor testing without Firebase)
            await Task.Delay(500);
            CurrentPlayer = new PlayerProfile
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                DisplayName = email.Split('@')[0]
            };
            SaveLoginCredentials();
            OnPlayerLoggedIn?.Invoke(CurrentPlayer);
            Debug.Log($"[PlayerManager] Login successful (stub)! Player: {CurrentPlayer.DisplayName}");
            return new LoginResult(true, "Login successful!");
#endif
        }

        /// <summary>
        /// Register a new account
        /// </summary>
        public async Task<LoginResult> Register(string email, string password, string displayName)
        {
            Debug.Log($"[PlayerManager] Registering new account for {email}...");

#if FIREBASE_ENABLED
            try
            {
                var authResult = await _auth.CreateUserWithEmailAndPasswordAsync(email, password);
                _firebaseUser = authResult.User;
                
                // Update display name in Firebase Auth
                var profile = new UserProfile { DisplayName = displayName };
                await _firebaseUser.UpdateUserProfileAsync(profile);
                
                Debug.Log($"[PlayerManager] Firebase registration successful for {_firebaseUser.UserId}");
                
                // Create new player profile
                CurrentPlayer = new PlayerProfile
                {
                    Id = _firebaseUser.UserId,
                    Email = email,
                    DisplayName = displayName,
                    CreatedAt = DateTime.UtcNow
                };
                
                await SavePlayerToCloud();
                SaveLoginCredentials();
                OnPlayerLoggedIn?.Invoke(CurrentPlayer);

                Debug.Log($"[PlayerManager] Registration successful! Player: {CurrentPlayer.DisplayName}");
                return new LoginResult(true, "Account created!");
            }
            catch (Firebase.FirebaseException ex)
            {
                Debug.LogError($"[PlayerManager] Firebase registration failed: {ex.Message}");
                return new LoginResult(false, GetAuthErrorMessage(ex.ErrorCode), ex.ErrorCode.ToString());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerManager] Registration failed: {ex.Message}");
                return new LoginResult(false, "Registration failed. Please try again.", "UNKNOWN");
            }
#else
            await Task.Delay(500);
            CurrentPlayer = new PlayerProfile
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                DisplayName = displayName,
                CreatedAt = DateTime.UtcNow
            };
            await SavePlayerToCloud();
            SaveLoginCredentials();
            OnPlayerLoggedIn?.Invoke(CurrentPlayer);
            Debug.Log($"[PlayerManager] Registration successful (stub)! Player: {CurrentPlayer.DisplayName}");
            return new LoginResult(true, "Account created!");
#endif
        }

        /// <summary>
        /// Login as guest (anonymous)
        /// </summary>
        public async Task<LoginResult> LoginAsGuest()
        {
            Debug.Log("[PlayerManager] Logging in as guest...");

#if FIREBASE_ENABLED
            try
            {
                var authResult = await _auth.SignInAnonymouslyAsync();
                _firebaseUser = authResult.User;
                
                Debug.Log($"[PlayerManager] Anonymous auth successful for {_firebaseUser.UserId}");
                
                // Load or create player profile
                await LoadOrCreatePlayerProfile(_firebaseUser, isAnonymous: true);
                
                OnPlayerLoggedIn?.Invoke(CurrentPlayer);

                Debug.Log($"[PlayerManager] Guest login successful! Player: {CurrentPlayer.DisplayName}");
                return new LoginResult(true, "Logged in as guest");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerManager] Anonymous auth failed: {ex.Message}");
                return new LoginResult(false, "Guest login failed. Please try again.", "UNKNOWN");
            }
#else
            await Task.Delay(300);
            CurrentPlayer = new PlayerProfile
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = $"Guest_{UnityEngine.Random.Range(1000, 9999)}",
                CreatedAt = DateTime.UtcNow
            };
            OnPlayerLoggedIn?.Invoke(CurrentPlayer);
            Debug.Log($"[PlayerManager] Guest login successful (stub)! Player: {CurrentPlayer.DisplayName}");
            return new LoginResult(true, "Logged in as guest");
#endif
        }

        /// <summary>
        /// Logout current player
        /// </summary>
        public void Logout()
        {
            Debug.Log("[PlayerManager] Logging out...");

#if FIREBASE_ENABLED
            _auth.SignOut();
            _firebaseUser = null;
#endif
            ClearLoginCredentials();
            CurrentPlayer = null;
            OnPlayerLoggedOut?.Invoke();

            Debug.Log("[PlayerManager] Logged out");
        }

#if FIREBASE_ENABLED
        private async Task LoadOrCreatePlayerProfile(FirebaseUser user, bool isAnonymous = false)
        {
            var docRef = _db.Collection("users").Document(user.UserId);
            var snapshot = await docRef.GetSnapshotAsync();
            
            if (snapshot.Exists)
            {
                // Load existing profile
                CurrentPlayer = PlayerProfile.FromFirestore(snapshot);
                CurrentPlayer.LastLoginAt = DateTime.UtcNow;
                
                // Update last active timestamp
                await docRef.UpdateAsync(new Dictionary<string, object>
                {
                    { "lastLoginAt", FieldValue.ServerTimestamp },
                    { "lastActiveAt", FieldValue.ServerTimestamp }
                });
                
                Debug.Log($"[PlayerManager] Loaded existing profile for {CurrentPlayer.DisplayName}");
            }
            else
            {
                // Create new profile
                CurrentPlayer = new PlayerProfile
                {
                    Id = user.UserId,
                    Email = user.Email ?? "",
                    DisplayName = isAnonymous 
                        ? $"Guest_{UnityEngine.Random.Range(1000, 9999)}" 
                        : (user.DisplayName ?? user.Email?.Split('@')[0] ?? "Player"),
                    IsAnonymous = isAnonymous,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };
                
                await SavePlayerToCloud();
                Debug.Log($"[PlayerManager] Created new profile for {CurrentPlayer.DisplayName}");
            }
        }
        
        private string GetAuthErrorMessage(int errorCode)
        {
            // Firebase Auth error codes
            return errorCode switch
            {
                // Email errors
                17008 => "Invalid email address format.",
                17011 => "No account found with this email.",
                17009 => "Incorrect password.",
                17026 => "Password must be at least 6 characters.",
                
                // Account errors  
                17007 => "An account already exists with this email.",
                17010 => "Account has been disabled.",
                
                // Network errors
                17020 => "Network error. Please check your connection.",
                
                // Rate limiting
                17029 => "Too many attempts. Please try again later.",
                
                _ => "Authentication failed. Please try again."
            };
        }
#endif

        private void TryAutoLogin()
        {
#if FIREBASE_ENABLED
            // Firebase Auth persists sessions automatically
            if (_auth?.CurrentUser != null)
            {
                Debug.Log("[PlayerManager] Found existing Firebase session, restoring...");
                _firebaseUser = _auth.CurrentUser;
                _ = LoadOrCreatePlayerProfile(_firebaseUser, _firebaseUser.IsAnonymous);
            }
#else
            // Check for saved login token (non-Firebase builds)
            string savedPlayerId = PlayerPrefs.GetString("SavedPlayerId", "");
            if (!string.IsNullOrEmpty(savedPlayerId))
            {
                Debug.Log("[PlayerManager] Found saved credentials, attempting auto-login...");
                _ = LoginAsGuest();
            }
#endif
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
        /// Spend a specific resource type
        /// </summary>
        public bool SpendResource(ResourceType type, int amount)
        {
            if (CurrentPlayer == null || amount <= 0) return false;

            switch (type)
            {
                case ResourceType.Stone:
                    if (CurrentPlayer.Stone >= amount)
                    {
                        CurrentPlayer.Stone -= amount;
                        OnResourceChanged?.Invoke(type, -amount);
                        _ = SavePlayerToCloud();
                        return true;
                    }
                    break;
                case ResourceType.Wood:
                    if (CurrentPlayer.Wood >= amount)
                    {
                        CurrentPlayer.Wood -= amount;
                        OnResourceChanged?.Invoke(type, -amount);
                        _ = SavePlayerToCloud();
                        return true;
                    }
                    break;
                case ResourceType.Metal:
                    if (CurrentPlayer.Metal >= amount)
                    {
                        CurrentPlayer.Metal -= amount;
                        OnResourceChanged?.Invoke(type, -amount);
                        _ = SavePlayerToCloud();
                        return true;
                    }
                    break;
                case ResourceType.Crystal:
                    if (CurrentPlayer.Crystal >= amount)
                    {
                        CurrentPlayer.Crystal -= amount;
                        OnResourceChanged?.Invoke(type, -amount);
                        _ = SavePlayerToCloud();
                        return true;
                    }
                    break;
                case ResourceType.Gems:
                    if (CurrentPlayer.Gems >= amount)
                    {
                        CurrentPlayer.Gems -= amount;
                        OnResourceChanged?.Invoke(type, -amount);
                        _ = SavePlayerToCloud();
                        return true;
                    }
                    break;
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

        /// <summary>
        /// Get current player ID
        /// </summary>
        public string GetCurrentPlayerId()
        {
            return CurrentPlayer?.Id;
        }

        /// <summary>
        /// Add a specific resource type
        /// </summary>
        public void AddResource(ResourceType type, int amount)
        {
            if (CurrentPlayer == null || amount <= 0) return;

            switch (type)
            {
                case ResourceType.Stone:
                    CurrentPlayer.Stone += amount;
                    break;
                case ResourceType.Wood:
                    CurrentPlayer.Wood += amount;
                    break;
                case ResourceType.Metal:
                    CurrentPlayer.Metal += amount;
                    break;
                case ResourceType.Crystal:
                    CurrentPlayer.Crystal += amount;
                    break;
                case ResourceType.Gems:
                    CurrentPlayer.Gems += amount;
                    break;
            }
            OnResourceChanged?.Invoke(type, amount);
            _ = SavePlayerToCloud();
        }

        #endregion

        #region Cloud Sync

        private async Task SavePlayerToCloud()
        {
            if (CurrentPlayer == null) return;

            Debug.Log("[PlayerManager] Saving player data to cloud...");
            
#if FIREBASE_ENABLED
            try
            {
                var docRef = _db.Collection("users").Document(CurrentPlayer.Id);
                var data = CurrentPlayer.ToFirestoreData();
                data["lastActiveAt"] = FieldValue.ServerTimestamp;
                
                await docRef.SetAsync(data, SetOptions.MergeAll);
                Debug.Log("[PlayerManager] Player data saved to Firestore");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerManager] Failed to save player data: {ex.Message}");
            }
#else
            await Task.Delay(100);
            Debug.Log("[PlayerManager] Player data saved (stub)");
#endif
        }

        private async Task LoadPlayerFromCloud(string playerId)
        {
            Debug.Log($"[PlayerManager] Loading player {playerId} from cloud...");

#if FIREBASE_ENABLED
            try
            {
                var docRef = _db.Collection("users").Document(playerId);
                var snapshot = await docRef.GetSnapshotAsync();
                
                if (snapshot.Exists)
                {
                    CurrentPlayer = PlayerProfile.FromFirestore(snapshot);
                    Debug.Log($"[PlayerManager] Player data loaded: {CurrentPlayer.DisplayName}");
                }
                else
                {
                    Debug.LogWarning($"[PlayerManager] No player data found for {playerId}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerManager] Failed to load player data: {ex.Message}");
            }
#else
            await Task.Delay(100);
            Debug.Log("[PlayerManager] Player data loaded (stub)");
#endif
        }

        /// <summary>
        /// Force sync player data to cloud
        /// </summary>
        public async Task SyncToCloud()
        {
            await SavePlayerToCloud();
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
