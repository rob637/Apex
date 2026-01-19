using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using ApexCitadels.Data;
using ApexCitadels.Player;
#if FIREBASE_ENABLED
using Firebase.Firestore;
#endif

namespace ApexCitadels.Alliance
{
    /// <summary>
    /// Manages alliances, invitations, and alliance wars
    /// </summary>
    public class AllianceManager : MonoBehaviour
    {
        public static AllianceManager Instance { get; private set; }

        [Header("Alliance Settings")]
        [SerializeField] private int createAllianceCost = 1000; // Gems
        [SerializeField] private int minLevelToCreate = 5;
        [SerializeField] private int warDeclarationCost = 500; // Gems

        // Events
        public event Action<Alliance> OnAllianceJoined;
        public event Action OnAllianceLeft;
        public event Action<AllianceInvitation> OnInvitationReceived;
        public event Action<AllianceWar> OnWarStarted;
        public event Action<AllianceWar> OnWarEnded;
        public event Action<string> OnAllianceChatMessage;

        // State
        private Alliance _currentAlliance;
        private List<AllianceInvitation> _pendingInvitations = new List<AllianceInvitation>();
        private AllianceWar _activeWar;

        public Alliance CurrentAlliance => _currentAlliance;
        public bool IsInAlliance => _currentAlliance != null;
        public AllianceWar ActiveWar => _activeWar;
        public bool IsInWar => _activeWar?.IsActive ?? false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Load alliance data for current player
            LoadPlayerAlliance();
        }

        #region Alliance Creation/Joining

        /// <summary>
        /// Create a new alliance
        /// </summary>
        public async Task<(bool Success, string Message)> CreateAlliance(string name, string tag)
        {
            if (IsInAlliance)
            {
                return (false, "You must leave your current alliance first!");
            }

            var player = PlayerManager.Instance?.CurrentPlayer;
            if (player == null)
            {
                return (false, "Not logged in!");
            }

            if (player.Level < minLevelToCreate)
            {
                return (false, $"You must be level {minLevelToCreate} to create an alliance!");
            }

            if (player.Gems < createAllianceCost)
            {
                return (false, $"Creating an alliance costs {createAllianceCost} gems!");
            }

            // Validate name
            if (string.IsNullOrWhiteSpace(name) || name.Length < 3 || name.Length > 20)
            {
                return (false, "Alliance name must be 3-20 characters!");
            }

            // Validate tag
            if (string.IsNullOrWhiteSpace(tag) || tag.Length < 2 || tag.Length > 4)
            {
                return (false, "Alliance tag must be 2-4 characters!");
            }

            // Create alliance
            _currentAlliance = new Alliance(name, tag, player.PlayerId, player.DisplayName);

            // Deduct gems
            PlayerManager.Instance.SpendResource(ResourceType.Gems, createAllianceCost);

            // Save to Firebase
            await SaveAllianceToCloud(_currentAlliance);

            Debug.Log($"[AllianceManager] Created alliance: {name} [{tag}]");
            OnAllianceJoined?.Invoke(_currentAlliance);

            return (true, $"Alliance '{name}' created successfully!");
        }

        /// <summary>
        /// Join an existing alliance (if open)
        /// </summary>
        public async Task<(bool Success, string Message)> JoinAlliance(string allianceId)
        {
            if (IsInAlliance)
            {
                return (false, "You must leave your current alliance first!");
            }

            // Load alliance from cloud
            var alliance = await LoadAllianceFromCloud(allianceId);
            if (alliance == null)
            {
                return (false, "Alliance not found!");
            }

            if (!alliance.IsOpen)
            {
                return (false, "This alliance is invite-only!");
            }

            if (alliance.IsFull)
            {
                return (false, "This alliance is full!");
            }

            var player = PlayerManager.Instance?.CurrentPlayer;
            if (player == null)
            {
                return (false, "Not logged in!");
            }

            if (player.Level < alliance.MinLevelToJoin)
            {
                return (false, $"You must be level {alliance.MinLevelToJoin} to join!");
            }

            // Add to alliance
            alliance.Members.Add(new AllianceMember
            {
                PlayerId = player.PlayerId,
                PlayerName = player.DisplayName,
                Role = AllianceRole.Member,
                JoinedAt = DateTime.UtcNow
            });

            _currentAlliance = alliance;

            // Save to cloud
            await SaveAllianceToCloud(alliance);

            Debug.Log($"[AllianceManager] Joined alliance: {alliance.Name}");
            OnAllianceJoined?.Invoke(_currentAlliance);

            return (true, $"Joined '{alliance.Name}' successfully!");
        }

        /// <summary>
        /// Leave current alliance
        /// </summary>
        public async Task<(bool Success, string Message)> LeaveAlliance()
        {
            if (!IsInAlliance)
            {
                return (false, "You're not in an alliance!");
            }

            var playerId = PlayerManager.Instance?.GetCurrentPlayerId();
            
            // Check if leader
            if (_currentAlliance.LeaderId == playerId)
            {
                if (_currentAlliance.Members.Count > 1)
                {
                    return (false, "You must promote someone else to leader first!");
                }
                // Last member leaving - disband
                await DisbandAlliance();
                return (true, "Alliance disbanded!");
            }

            // Remove from alliance
            _currentAlliance.Members.RemoveAll(m => m.PlayerId == playerId);
            await SaveAllianceToCloud(_currentAlliance);

            _currentAlliance = null;
            OnAllianceLeft?.Invoke();

            return (true, "Left alliance successfully!");
        }

        #endregion

        #region Invitations

        /// <summary>
        /// Invite a player to the alliance
        /// </summary>
        public async Task<(bool Success, string Message)> InvitePlayer(string inviteeId, string inviteeName)
        {
            if (!IsInAlliance)
            {
                return (false, "You're not in an alliance!");
            }

            var playerId = PlayerManager.Instance?.GetCurrentPlayerId();
            if (!_currentAlliance.HasPermission(playerId, AlliancePermission.InvitePlayers))
            {
                return (false, "You don't have permission to invite players!");
            }

            if (_currentAlliance.IsFull)
            {
                return (false, "Alliance is full!");
            }

            // Check if already a member
            if (_currentAlliance.Members.Any(m => m.PlayerId == inviteeId))
            {
                return (false, "Player is already a member!");
            }

            var invitation = new AllianceInvitation(
                _currentAlliance.Id,
                _currentAlliance.Name,
                playerId,
                PlayerManager.Instance?.CurrentPlayer?.DisplayName ?? "Unknown",
                inviteeId
            );

            // Save invitation to cloud
            await SaveInvitationToCloud(invitation);

            Debug.Log($"[AllianceManager] Sent invitation to {inviteeName}");
            return (true, $"Invitation sent to {inviteeName}!");
        }

        /// <summary>
        /// Accept an alliance invitation
        /// </summary>
        public async Task<(bool Success, string Message)> AcceptInvitation(string invitationId)
        {
            var invitation = _pendingInvitations.Find(i => i.Id == invitationId);
            if (invitation == null)
            {
                return (false, "Invitation not found!");
            }

            if (invitation.IsExpired)
            {
                _pendingInvitations.Remove(invitation);
                return (false, "Invitation has expired!");
            }

            if (IsInAlliance)
            {
                return (false, "You must leave your current alliance first!");
            }

            // Load and join alliance
            var alliance = await LoadAllianceFromCloud(invitation.AllianceId);
            if (alliance == null)
            {
                return (false, "Alliance no longer exists!");
            }

            var player = PlayerManager.Instance?.CurrentPlayer;
            alliance.Members.Add(new AllianceMember
            {
                PlayerId = player.PlayerId,
                PlayerName = player.DisplayName,
                Role = AllianceRole.Member,
                JoinedAt = DateTime.UtcNow
            });

            _currentAlliance = alliance;
            invitation.Accepted = true;

            await SaveAllianceToCloud(alliance);
            await SaveInvitationToCloud(invitation);
            _pendingInvitations.Remove(invitation);

            OnAllianceJoined?.Invoke(_currentAlliance);

            return (true, $"Joined '{alliance.Name}'!");
        }

        /// <summary>
        /// Decline an alliance invitation
        /// </summary>
        public async Task DeclineInvitation(string invitationId)
        {
            var invitation = _pendingInvitations.Find(i => i.Id == invitationId);
            if (invitation != null)
            {
                invitation.Declined = true;
                await SaveInvitationToCloud(invitation);
                _pendingInvitations.Remove(invitation);
            }
        }

        #endregion

        #region Member Management

        /// <summary>
        /// Promote a member to officer
        /// </summary>
        public async Task<(bool Success, string Message)> PromoteMember(string memberId)
        {
            if (!IsInAlliance) return (false, "Not in alliance!");

            var playerId = PlayerManager.Instance?.GetCurrentPlayerId();
            if (!_currentAlliance.HasPermission(playerId, AlliancePermission.PromoteToOfficer))
            {
                return (false, "No permission!");
            }

            var member = _currentAlliance.GetMember(memberId);
            if (member == null) return (false, "Member not found!");

            if (member.Role == AllianceRole.Officer)
            {
                return (false, "Already an officer!");
            }

            member.Role = AllianceRole.Officer;
            await SaveAllianceToCloud(_currentAlliance);

            return (true, $"{member.PlayerName} promoted to Officer!");
        }

        /// <summary>
        /// Transfer leadership to another member
        /// </summary>
        public async Task<(bool Success, string Message)> TransferLeadership(string newLeaderId)
        {
            if (!IsInAlliance) return (false, "Not in alliance!");

            var playerId = PlayerManager.Instance?.GetCurrentPlayerId();
            if (_currentAlliance.LeaderId != playerId)
            {
                return (false, "Only the leader can transfer leadership!");
            }

            var newLeader = _currentAlliance.GetMember(newLeaderId);
            if (newLeader == null) return (false, "Member not found!");

            var oldLeader = _currentAlliance.GetMember(playerId);

            // Swap roles
            oldLeader.Role = AllianceRole.Officer;
            newLeader.Role = AllianceRole.Leader;
            _currentAlliance.LeaderId = newLeaderId;
            _currentAlliance.LeaderName = newLeader.PlayerName;

            await SaveAllianceToCloud(_currentAlliance);

            return (true, $"{newLeader.PlayerName} is now the leader!");
        }

        /// <summary>
        /// Kick a member from the alliance
        /// </summary>
        public async Task<(bool Success, string Message)> KickMember(string memberId)
        {
            if (!IsInAlliance) return (false, "Not in alliance!");

            var playerId = PlayerManager.Instance?.GetCurrentPlayerId();
            if (!_currentAlliance.HasPermission(playerId, AlliancePermission.KickMembers))
            {
                return (false, "No permission!");
            }

            var member = _currentAlliance.GetMember(memberId);
            if (member == null) return (false, "Member not found!");

            if (member.Role == AllianceRole.Leader)
            {
                return (false, "Cannot kick the leader!");
            }

            // Cannot kick higher or equal rank
            var kicker = _currentAlliance.GetMember(playerId);
            if (member.Role >= kicker.Role)
            {
                return (false, "Cannot kick members of equal or higher rank!");
            }

            _currentAlliance.Members.Remove(member);
            await SaveAllianceToCloud(_currentAlliance);

            return (true, $"{member.PlayerName} has been removed!");
        }

        #endregion

        #region Alliance Wars

        /// <summary>
        /// Declare war on another alliance
        /// </summary>
        public async Task<(bool Success, string Message)> DeclareWar(string targetAllianceId)
        {
            if (!IsInAlliance) return (false, "Not in alliance!");

            var playerId = PlayerManager.Instance?.GetCurrentPlayerId();
            if (!_currentAlliance.HasPermission(playerId, AlliancePermission.StartAllianceWar))
            {
                return (false, "No permission to declare war!");
            }

            if (IsInWar)
            {
                return (false, "Already in a war!");
            }

            var player = PlayerManager.Instance?.CurrentPlayer;
            if (player.Gems < warDeclarationCost)
            {
                return (false, $"War declaration costs {warDeclarationCost} gems!");
            }

            // Load target alliance
            var targetAlliance = await LoadAllianceFromCloud(targetAllianceId);
            if (targetAlliance == null)
            {
                return (false, "Target alliance not found!");
            }

            // Create war
            _activeWar = new AllianceWar(
                _currentAlliance.Id,
                _currentAlliance.Name,
                targetAlliance.Id,
                targetAlliance.Name
            );

            PlayerManager.Instance.SpendResource(ResourceType.Gems, warDeclarationCost);
            await SaveWarToCloud(_activeWar);

            OnWarStarted?.Invoke(_activeWar);

            return (true, $"War declared on {targetAlliance.Name}!");
        }

        /// <summary>
        /// Record a battle result during a war
        /// </summary>
        public async Task RecordWarBattle(string territoryId, string territoryName, 
                                          string defenderId, bool attackerWon)
        {
            if (!IsInWar) return;

            int points = attackerWon ? 100 : 50; // Points for winning vs defending

            var battle = new WarBattle
            {
                TerritoryId = territoryId,
                TerritoryName = territoryName,
                AttackerId = PlayerManager.Instance?.GetCurrentPlayerId(),
                DefenderId = defenderId,
                AttackerWon = attackerWon,
                PointsAwarded = points,
                Timestamp = DateTime.UtcNow
            };

            _activeWar.Battles.Add(battle);

            // Update scores
            if (attackerWon)
            {
                if (_activeWar.AttackingAllianceId == _currentAlliance.Id)
                    _activeWar.AttackerScore += points;
                else
                    _activeWar.DefenderScore += points;
            }
            else
            {
                if (_activeWar.DefendingAllianceId == _currentAlliance.Id)
                    _activeWar.DefenderScore += points;
                else
                    _activeWar.AttackerScore += points;
            }

            await SaveWarToCloud(_activeWar);
        }

        #endregion

        #region Cloud Operations

#if FIREBASE_ENABLED
        private FirebaseFirestore _db;

        private FirebaseFirestore GetFirestore()
        {
            if (_db == null)
            {
                _db = FirebaseFirestore.DefaultInstance;
            }
            return _db;
        }
#endif

        private async void LoadPlayerAlliance()
        {
            var playerId = PlayerManager.Instance?.GetCurrentPlayerId();
            if (string.IsNullOrEmpty(playerId))
            {
                Debug.Log("[AllianceManager] No player logged in, skipping alliance load");
                return;
            }

            try
            {
                var db = GetFirestore();
                
                // Query for alliances where this player is a member
                var query = db.Collection("alliances")
                    .WhereArrayContains("memberIds", playerId)
                    .Limit(1);

                var snapshot = await query.GetSnapshotAsync();

                if (snapshot.Documents.Count() > 0)
                {
                    var doc = snapshot.Documents.First();
                    _currentAlliance = Alliance.FromFirestore(doc);
                    Debug.Log($"[AllianceManager] Loaded alliance: {_currentAlliance?.Name}");
                    
                    if (_currentAlliance != null)
                    {
                        OnAllianceJoined?.Invoke(_currentAlliance);
                        
                        // Also check for active wars
                        await LoadActiveWar();
                    }
                }
                else
                {
                    Debug.Log("[AllianceManager] Player is not in any alliance");
                }

                // Load pending invitations
                await LoadPendingInvitations();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AllianceManager] Failed to load alliance: {ex.Message}");
            }
        }

        private async Task LoadActiveWar()
        {
            if (_currentAlliance == null) return;

            try
            {
                var db = GetFirestore();
                
                // Check if our alliance is in an active war (as attacker or defender)
                var attackerQuery = db.Collection("alliance_wars")
                    .WhereEqualTo("attackingAllianceId", _currentAlliance.Id)
                    .WhereEqualTo("status", "Active")
                    .Limit(1);

                var attackerSnapshot = await attackerQuery.GetSnapshotAsync();
                if (attackerSnapshot.Documents.Count() > 0)
                {
                    _activeWar = AllianceWar.FromFirestore(attackerSnapshot.Documents.First());
                    OnWarStarted?.Invoke(_activeWar);
                    return;
                }

                var defenderQuery = db.Collection("alliance_wars")
                    .WhereEqualTo("defendingAllianceId", _currentAlliance.Id)
                    .WhereEqualTo("status", "Active")
                    .Limit(1);

                var defenderSnapshot = await defenderQuery.GetSnapshotAsync();
                if (defenderSnapshot.Documents.Count() > 0)
                {
                    _activeWar = AllianceWar.FromFirestore(defenderSnapshot.Documents.First());
                    OnWarStarted?.Invoke(_activeWar);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AllianceManager] Failed to load active war: {ex.Message}");
            }
        }

        private async Task LoadPendingInvitations()
        {
            var playerId = PlayerManager.Instance?.GetCurrentPlayerId();
            if (string.IsNullOrEmpty(playerId)) return;

            try
            {
                var db = GetFirestore();
                
                var query = db.Collection("alliance_invitations")
                    .WhereEqualTo("inviteeId", playerId)
                    .WhereEqualTo("accepted", false)
                    .WhereEqualTo("declined", false);

                var snapshot = await query.GetSnapshotAsync();

                _pendingInvitations.Clear();
                foreach (var doc in snapshot.Documents)
                {
                    var invitation = AllianceInvitation.FromFirestore(doc);
                    if (invitation != null && invitation.IsPending)
                    {
                        _pendingInvitations.Add(invitation);
                        OnInvitationReceived?.Invoke(invitation);
                    }
                }

                Debug.Log($"[AllianceManager] Loaded {_pendingInvitations.Count} pending invitations");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AllianceManager] Failed to load invitations: {ex.Message}");
            }
        }

        private async Task SaveAllianceToCloud(Alliance alliance)
        {
            try
            {
                var db = GetFirestore();
                var docRef = db.Collection("alliances").Document(alliance.Id);
                await docRef.SetAsync(alliance.ToFirestoreData());
                Debug.Log($"[AllianceManager] Alliance {alliance.Name} saved to cloud");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AllianceManager] Failed to save alliance: {ex.Message}");
            }
        }

        private async Task<Alliance> LoadAllianceFromCloud(string allianceId)
        {
            try
            {
                var db = GetFirestore();
                var docRef = db.Collection("alliances").Document(allianceId);
                var snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    return Alliance.FromFirestore(snapshot);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AllianceManager] Failed to load alliance {allianceId}: {ex.Message}");
            }

            return null;
        }

        private async Task SaveInvitationToCloud(AllianceInvitation invitation)
        {
            try
            {
                var db = GetFirestore();
                var docRef = db.Collection("alliance_invitations").Document(invitation.Id);
                await docRef.SetAsync(invitation.ToFirestoreData());
                Debug.Log($"[AllianceManager] Invitation {invitation.Id} saved");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AllianceManager] Failed to save invitation: {ex.Message}");
            }
        }

        private async Task SaveWarToCloud(AllianceWar war)
        {
            try
            {
                var db = GetFirestore();
                var docRef = db.Collection("alliance_wars").Document(war.Id);
                await docRef.SetAsync(war.ToFirestoreData());
                Debug.Log($"[AllianceManager] War {war.Id} saved");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AllianceManager] Failed to save war: {ex.Message}");
            }
        }

        private async Task DisbandAlliance()
        {
            if (_currentAlliance == null) return;

            try
            {
                var db = GetFirestore();
                var docRef = db.Collection("alliances").Document(_currentAlliance.Id);
                await docRef.DeleteAsync();
                Debug.Log($"[AllianceManager] Alliance {_currentAlliance.Name} disbanded");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AllianceManager] Failed to disband alliance: {ex.Message}");
            }
            finally
            {
                _currentAlliance = null;
                OnAllianceLeft?.Invoke();
            }
        }

        #endregion

        #region Search

        /// <summary>
        /// Search for alliances by name
        /// </summary>
        public async Task<List<Alliance>> SearchAlliances(string queryText)
        {
            var results = new List<Alliance>();

            try
            {
                var db = GetFirestore();
                
                // Firestore doesn't support full-text search, so we search by prefix
                // For better search, consider using Algolia or Firebase Extensions
                var query = db.Collection("alliances")
                    .WhereGreaterThanOrEqualTo("name", queryText)
                    .WhereLessThanOrEqualTo("name", queryText + "\uf8ff")
                    .Limit(20);

                var snapshot = await query.GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    var alliance = Alliance.FromFirestore(doc);
                    if (alliance != null)
                    {
                        results.Add(alliance);
                    }
                }

                Debug.Log($"[AllianceManager] Search returned {results.Count} alliances");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AllianceManager] Search failed: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Get top alliances by XP
        /// </summary>
        public async Task<List<Alliance>> GetLeaderboard(int limit = 100)
        {
            var results = new List<Alliance>();

            try
            {
                var db = GetFirestore();
                
                var query = db.Collection("alliances")
                    .OrderByDescending("allTimeXP")
                    .Limit(limit);

                var snapshot = await query.GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    var alliance = Alliance.FromFirestore(doc);
                    if (alliance != null)
                    {
                        results.Add(alliance);
                    }
                }

                Debug.Log($"[AllianceManager] Leaderboard loaded with {results.Count} alliances");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AllianceManager] Leaderboard failed: {ex.Message}");
            }

            return results;
        }

        #endregion
    }
}
