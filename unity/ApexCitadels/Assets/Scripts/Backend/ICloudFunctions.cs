using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ApexCitadels.Data;

namespace ApexCitadels.Backend
{
    /// <summary>
    /// Interface for battle-related Cloud Functions
    /// Matches backend: battle.ts exports
    /// 
    /// Implementation should use Firebase.Functions.FirebaseFunctions.DefaultInstance
    /// to call the Cloud Functions.
    /// </summary>
    public interface IBattleService
    {
        // ============================================================================
        // Battle Scheduling
        // ============================================================================

        /// <summary>
        /// Schedule a battle against a territory
        /// Backend: scheduleBattle(attackerId, territoryId)
        /// </summary>
        /// <param name="territoryId">ID of territory to attack</param>
        /// <param name="participation">How player is participating (physical/nearby/remote)</param>
        /// <returns>Scheduled battle info including battleId and start time</returns>
        Task<ScheduledBattle> ScheduleBattleAsync(string territoryId, ParticipationType participation);

        /// <summary>
        /// Set troop formation for an upcoming battle
        /// Backend: setBattleFormation(battleId, formation)
        /// </summary>
        /// <param name="battleId">ID of the scheduled battle</param>
        /// <param name="formation">Troops to deploy and strategy</param>
        Task<BattleFormation> SetBattleFormationAsync(string battleId, BattleFormation formation);

        /// <summary>
        /// Get details of a scheduled battle
        /// </summary>
        Task<ScheduledBattle> GetBattleAsync(string battleId);

        /// <summary>
        /// Get all battles involving the current user
        /// </summary>
        /// <param name="status">Filter by status (null for all)</param>
        Task<List<ScheduledBattle>> GetMyBattlesAsync(BattleStatus? status = null);

        // ============================================================================
        // Battle Execution
        // ============================================================================

        /// <summary>
        /// Execute a battle that's ready
        /// Backend: executeBattle(battleId)
        /// Note: Usually called by scheduled trigger, but can be manual
        /// </summary>
        Task<BattleResult> ExecuteBattleAsync(string battleId);

        /// <summary>
        /// Report player's participation type for a battle
        /// Backend: reportBattleParticipation(battleId, participation)
        /// </summary>
        Task ReportParticipationAsync(string battleId, ParticipationType participation);

        // ============================================================================
        // Territory Reclaim
        // ============================================================================

        /// <summary>
        /// Reclaim a fallen territory (after 24hr cooldown)
        /// Backend: reclaimTerritory(territoryId, blueprintId?)
        /// </summary>
        /// <param name="territoryId">ID of the fallen territory</param>
        /// <param name="blueprintId">Optional blueprint to restore</param>
        Task<bool> ReclaimTerritoryAsync(string territoryId, string blueprintId = null);

        // ============================================================================
        // Troop Training
        // ============================================================================

        /// <summary>
        /// Start training troops
        /// Backend: trainTroops(troopType, count)
        /// </summary>
        Task<TrainingQueueItem> TrainTroopsAsync(TroopType troopType, int count);

        /// <summary>
        /// Collect completed trained troops
        /// Backend: collectTrainedTroops()
        /// </summary>
        Task<UserTroops> CollectTrainedTroopsAsync();

        /// <summary>
        /// Get current troop inventory and training queue
        /// </summary>
        Task<UserTroops> GetMyTroopsAsync();
    }

    /// <summary>
    /// Interface for protection-related Cloud Functions
    /// Matches backend: protection.ts exports
    /// </summary>
    public interface IProtectionService
    {
        /// <summary>
        /// Get current protection status (shield, activity bonus)
        /// Backend: getMyProtectionStatus()
        /// </summary>
        Task<ProtectionStatus> GetProtectionStatusAsync();

        /// <summary>
        /// Check if a territory can be attacked
        /// Backend: checkTerritoryAttackable(territoryId)
        /// </summary>
        /// <param name="territoryId">Territory to check</param>
        /// <returns>Whether attackable, and reason if not</returns>
        Task<(bool canAttack, string reason)> CheckTerritoryAttackableAsync(string territoryId);

        /// <summary>
        /// Waive newcomer shield early to start attacking
        /// Backend: waiveNewcomerShield()
        /// </summary>
        Task<bool> WaiveNewcomerShieldAsync();

        /// <summary>
        /// Grant temporary shield (e.g., after territory loss)
        /// Backend: grantTemporaryShield(hours)
        /// </summary>
        Task GrantTemporaryShieldAsync(int hours);
    }

    /// <summary>
    /// Interface for blueprint-related Cloud Functions
    /// Matches backend: blueprint.ts exports
    /// </summary>
    public interface IBlueprintService
    {
        /// <summary>
        /// Save current territory as a blueprint
        /// Backend: saveBlueprint(territoryId, name, description?)
        /// </summary>
        Task<Blueprint> SaveBlueprintAsync(string territoryId, string name, string description = null);

        /// <summary>
        /// Get all user's blueprints
        /// Backend: getMyBlueprints()
        /// </summary>
        Task<BlueprintListResponse> GetMyBlueprintsAsync();

        /// <summary>
        /// Get full blueprint details including buildings
        /// Backend: getBlueprintDetails(blueprintId)
        /// </summary>
        Task<Blueprint> GetBlueprintDetailsAsync(string blueprintId);

        /// <summary>
        /// Delete a blueprint
        /// Backend: deleteBlueprint(blueprintId)
        /// </summary>
        Task<bool> DeleteBlueprintAsync(string blueprintId);

        /// <summary>
        /// Rename a blueprint
        /// Backend: renameBlueprint(blueprintId, name, description?)
        /// </summary>
        Task<bool> RenameBlueprintAsync(string blueprintId, string name, string description = null);

        /// <summary>
        /// Apply blueprint to rebuild a territory
        /// Backend: applyBlueprint(territoryId, blueprintId)
        /// </summary>
        Task<(bool success, int buildingsPlaced, ResourceCost spent)> ApplyBlueprintAsync(
            string territoryId, string blueprintId);

        /// <summary>
        /// Preview cost to apply a blueprint
        /// Backend: previewBlueprintCost(blueprintId)
        /// </summary>
        Task<BlueprintCostPreview> PreviewBlueprintCostAsync(string blueprintId);
    }

    /// <summary>
    /// Interface for alliance war-related Cloud Functions
    /// Matches backend: alliance.ts war exports
    /// </summary>
    public interface IAllianceWarService
    {
        /// <summary>
        /// Declare war on another alliance (starts 24hr warning)
        /// Backend: declareAllianceWar(targetAllianceId)
        /// </summary>
        Task<AllianceWar> DeclareWarAsync(string targetAllianceId);

        /// <summary>
        /// Cancel a declared war (only during warning phase)
        /// Backend: cancelAllianceWar(warId)
        /// </summary>
        Task<bool> CancelWarAsync(string warId);

        /// <summary>
        /// Get current war status for player's alliance
        /// Backend: getAllianceWarStatus()
        /// </summary>
        Task<AllianceWar> GetWarStatusAsync();

        /// <summary>
        /// Get all active/recent wars for an alliance
        /// </summary>
        Task<List<AllianceWar>> GetAllianceWarsAsync(string allianceId);
    }

    /// <summary>
    /// Interface for location-related Cloud Functions
    /// Matches backend: location-utils.ts exports
    /// </summary>
    public interface ILocationService
    {
        /// <summary>
        /// Get location info including density classification
        /// Backend: getLocationInfo(latitude, longitude)
        /// </summary>
        Task<(LocationDensity density, float territoryRadius)> GetLocationInfoAsync(
            double latitude, double longitude);

        /// <summary>
        /// Set area density for admin purposes
        /// Backend: setAreaDensity(geohash, density)
        /// </summary>
        Task SetAreaDensityAsync(string geohash, LocationDensity density);
    }
}
