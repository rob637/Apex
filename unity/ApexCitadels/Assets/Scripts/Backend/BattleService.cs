using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ApexCitadels.Core;
using ApexCitadels.Data;

namespace ApexCitadels.Backend
{
    /// <summary>
    /// Implementation of IBattleService using Firebase Cloud Functions.
    /// Requires Firebase Unity SDK to be imported.
    /// </summary>
    public class BattleService : MonoBehaviour, IBattleService
    {
        public static BattleService Instance { get; private set; }

        private bool _firebaseInitialized = false;

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
            // Firebase initialization will be done by GameManager
            // This service will be ready once Firebase is initialized
#if FIREBASE_ENABLED
            _firebaseInitialized = true;
#else
            ApexLogger.LogWarning("Firebase SDK not imported. Running in stub mode.", ApexLogger.LogCategory.Combat);
#endif
        }

        #region Battle Scheduling

        public async Task<ScheduledBattle> ScheduleBattleAsync(string territoryId, ParticipationType participation)
        {
#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("scheduleBattle");
                var data = new Dictionary<string, object>
                {
                    { "territoryId", territoryId },
                    { "participation", participation.ToString().ToLower() }
                };
                var result = await callable.CallAsync(data);
                var json = result.Data.ToString();
                return JsonUtility.FromJson<ScheduledBattle>(json);
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"ScheduleBattle failed: {ex.Message}", ApexLogger.LogCategory.Combat);
                throw;
            }
#else
            ApexLogger.LogVerbose($"[STUB] ScheduleBattle called for territory: {territoryId}", ApexLogger.LogCategory.Combat);
            await Task.Delay(100);
            return new ScheduledBattle
            {
                BattleId = Guid.NewGuid().ToString(),
                TerritoryId = territoryId,
                AttackerId = "stub-attacker",
                DefenderId = "stub-defender",
                ScheduledTime = DateTime.UtcNow.AddMinutes(5),
                Status = BattleStatus.Scheduled
            };
#endif
        }

        public async Task<BattleFormation> SetBattleFormationAsync(string battleId, BattleFormation formation)
        {
#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("setBattleFormation");
                var troops = new List<object>();
                foreach (var troop in formation.Troops)
                {
                    troops.Add(new Dictionary<string, object>
                    {
                        { "type", troop.Type.ToString().ToLower() },
                        { "count", troop.Count }
                    });
                }
                var data = new Dictionary<string, object>
                {
                    { "battleId", battleId },
                    { "troops", troops },
                    { "strategy", formation.Strategy.ToString().ToLower() }
                };
                var result = await callable.CallAsync(data);
                var json = result.Data.ToString();
                return JsonUtility.FromJson<BattleFormation>(json);
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"SetBattleFormation failed: {ex.Message}", ApexLogger.LogCategory.Combat);
                throw;
            }
#else
            ApexLogger.LogVerbose($"[STUB] SetBattleFormation called for battle: {battleId}", ApexLogger.LogCategory.Combat);
            await Task.Delay(100);
            formation.IsReady = true;
            return formation;
#endif
        }

        public async Task<ScheduledBattle> GetBattleAsync(string battleId)
        {
#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("getBattle");
                var data = new Dictionary<string, object> { { "battleId", battleId } };
                var result = await callable.CallAsync(data);
                var json = result.Data.ToString();
                return JsonUtility.FromJson<ScheduledBattle>(json);
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"GetBattle failed: {ex.Message}", ApexLogger.LogCategory.Combat);
                throw;
            }
#else
            ApexLogger.LogVerbose($"[STUB] GetBattle called for: {battleId}", ApexLogger.LogCategory.Combat);
            await Task.Delay(100);
            return new ScheduledBattle { BattleId = battleId };
#endif
        }

        public async Task<List<ScheduledBattle>> GetMyBattlesAsync(BattleStatus? status = null)
        {
#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("getMyBattles");
                var data = new Dictionary<string, object>();
                if (status.HasValue)
                {
                    data["status"] = status.Value.ToString().ToLower();
                }
                var result = await callable.CallAsync(data);
                var json = result.Data.ToString();
                var wrapper = JsonUtility.FromJson<BattleListWrapper>(json);
                return wrapper.battles;
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"GetMyBattles failed: {ex.Message}", ApexLogger.LogCategory.Combat);
                throw;
            }
#else
            ApexLogger.LogVerbose("[STUB] GetMyBattles called", ApexLogger.LogCategory.Combat);
            await Task.Delay(100);
            return new List<ScheduledBattle>();
#endif
        }

        #endregion

        #region Battle Execution

        public async Task<BattleResult> ExecuteBattleAsync(string battleId)
        {
#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("executeBattle");
                var data = new Dictionary<string, object> { { "battleId", battleId } };
                var result = await callable.CallAsync(data);
                var json = result.Data.ToString();
                return JsonUtility.FromJson<BattleResult>(json);
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"ExecuteBattle failed: {ex.Message}", ApexLogger.LogCategory.Combat);
                throw;
            }
#else
            ApexLogger.LogVerbose($"[STUB] ExecuteBattle called for: {battleId}", ApexLogger.LogCategory.Combat);
            await Task.Delay(500);
            return new BattleResult
            {
                BattleId = battleId,
                WinnerId = "stub-winner",
                AttackerCasualties = 5,
                DefenderCasualties = 8
            };
#endif
        }

        public async Task ReportParticipationAsync(string battleId, ParticipationType participation)
        {
#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("reportBattleParticipation");
                var data = new Dictionary<string, object>
                {
                    { "battleId", battleId },
                    { "participation", participation.ToString().ToLower() }
                };
                await callable.CallAsync(data);
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"ReportParticipation failed: {ex.Message}", ApexLogger.LogCategory.Combat);
                throw;
            }
#else
            ApexLogger.LogVerbose($"[STUB] ReportParticipation: {battleId} -> {participation}", ApexLogger.LogCategory.Combat);
            await Task.Delay(100);
#endif
        }

        #endregion

        #region Territory Reclaim

        public async Task<bool> ReclaimTerritoryAsync(string territoryId, string blueprintId = null)
        {
#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("reclaimTerritory");
                var data = new Dictionary<string, object> { { "territoryId", territoryId } };
                if (!string.IsNullOrEmpty(blueprintId))
                {
                    data["blueprintId"] = blueprintId;
                }
                var result = await callable.CallAsync(data);
                var json = result.Data.ToString();
                var response = JsonUtility.FromJson<ReclaimResponse>(json);
                return response.success;
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"ReclaimTerritory failed: {ex.Message}", ApexLogger.LogCategory.Combat);
                return false;
            }
#else
            ApexLogger.LogVerbose($"[STUB] ReclaimTerritory: {territoryId}", ApexLogger.LogCategory.Combat);
            await Task.Delay(100);
            return true;
#endif
        }

        #endregion

        #region Troop Training

        public async Task<TrainingQueueItem> TrainTroopsAsync(TroopType troopType, int count)
        {
#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("trainTroops");
                var data = new Dictionary<string, object>
                {
                    { "troopType", troopType.ToString().ToLower() },
                    { "count", count }
                };
                var result = await callable.CallAsync(data);
                var json = result.Data.ToString();
                return JsonUtility.FromJson<TrainingQueueItem>(json);
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"TrainTroops failed: {ex.Message}", ApexLogger.LogCategory.Combat);
                throw;
            }
#else
            ApexLogger.LogVerbose($"[STUB] TrainTroops: {count}x {troopType}", ApexLogger.LogCategory.Combat);
            await Task.Delay(100);
            return new TrainingQueueItem
            {
                TroopType = troopType,
                Count = count,
                CompletesAt = DateTime.UtcNow.AddMinutes(count)
            };
#endif
        }

        public async Task<UserTroops> CollectTrainedTroopsAsync()
        {
#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("collectTrainedTroops");
                var result = await callable.CallAsync(new Dictionary<string, object>());
                var json = result.Data.ToString();
                return JsonUtility.FromJson<UserTroops>(json);
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"CollectTrainedTroops failed: {ex.Message}", ApexLogger.LogCategory.Combat);
                throw;
            }
#else
            ApexLogger.LogVerbose("[STUB] CollectTrainedTroops", ApexLogger.LogCategory.Combat);
            await Task.Delay(100);
            return new UserTroops();
#endif
        }

        public async Task<UserTroops> GetMyTroopsAsync()
        {
#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("getMyTroops");
                var result = await callable.CallAsync(new Dictionary<string, object>());
                var json = result.Data.ToString();
                return JsonUtility.FromJson<UserTroops>(json);
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"GetMyTroops failed: {ex.Message}", ApexLogger.LogCategory.Combat);
                throw;
            }
#else
            ApexLogger.LogVerbose("[STUB] GetMyTroops", ApexLogger.LogCategory.Combat);
            await Task.Delay(100);
            return new UserTroops
            {
                Infantry = 10,
                Archer = 5,
                Cavalry = 3,
                Siege = 1,
                Mage = 2,
                Guardian = 2
            };
#endif
        }

        #endregion

        #region Helper Classes

        [Serializable]
        private class BattleListWrapper
        {
            public List<ScheduledBattle> battles;
        }

        [Serializable]
        private class ReclaimResponse
        {
            public bool success;
        }

        #endregion
    }
}
