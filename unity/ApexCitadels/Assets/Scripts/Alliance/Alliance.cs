using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;

namespace ApexCitadels.Alliance
{
    /// <summary>
    /// Alliance data model
    /// </summary>
    [Serializable]
    public class Alliance
    {
        public string Id;
        public string Name;
        public string Tag; // 3-4 letter tag like [APEX]
        public string Description;
        public string LeaderId;
        public string LeaderName;
        public DateTime CreatedAt;

        // Members
        public List<AllianceMember> Members = new List<AllianceMember>();
        public int MaxMembers = 50;

        // Stats
        public int TotalTerritories;
        public int TotalAttacksWon;
        public int TotalDefensesWon;
        public int WeeklyXP;
        public int AllTimeXP;

        // Settings
        public bool IsOpen; // Open to join without invitation
        public int MinLevelToJoin = 1;

        public Alliance() { }

        public Alliance(string name, string tag, string leaderId, string leaderName)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            Tag = tag.ToUpper();
            LeaderId = leaderId;
            LeaderName = leaderName;
            CreatedAt = DateTime.UtcNow;
            IsOpen = false;

            // Add leader as first member
            Members.Add(new AllianceMember
            {
                PlayerId = leaderId,
                PlayerName = leaderName,
                Role = AllianceRole.Leader,
                JoinedAt = DateTime.UtcNow
            });
        }

        public bool IsFull => Members.Count >= MaxMembers;

        public AllianceMember GetMember(string playerId)
        {
            return Members.Find(m => m.PlayerId == playerId);
        }

        public bool HasPermission(string playerId, AlliancePermission permission)
        {
            var member = GetMember(playerId);
            if (member == null) return false;

            return member.Role switch
            {
                AllianceRole.Leader => true,
                AllianceRole.Officer => permission != AlliancePermission.DisbandAlliance &&
                                        permission != AlliancePermission.PromoteToOfficer,
                AllianceRole.Member => permission == AlliancePermission.ParticipateInWars ||
                                       permission == AlliancePermission.ViewAllianceChat,
                _ => false
            };
        }

        /// <summary>
        /// Convert alliance to Firestore-compatible dictionary
        /// </summary>
        public Dictionary<string, object> ToFirestoreData()
        {
            var membersList = new List<Dictionary<string, object>>();
            foreach (var member in Members)
            {
                membersList.Add(new Dictionary<string, object>
                {
                    { "playerId", member.PlayerId },
                    { "playerName", member.PlayerName },
                    { "role", member.Role.ToString() },
                    { "joinedAt", Timestamp.FromDateTime(member.JoinedAt.ToUniversalTime()) },
                    { "contributedXP", member.ContributedXP },
                    { "territoriesCaptured", member.TerritoriesCaptured },
                    { "territoriesDefended", member.TerritoriesDefended },
                    { "lastActive", Timestamp.FromDateTime(member.LastActive.ToUniversalTime()) }
                });
            }

            return new Dictionary<string, object>
            {
                { "id", Id },
                { "name", Name },
                { "tag", Tag },
                { "description", Description ?? "" },
                { "leaderId", LeaderId },
                { "leaderName", LeaderName },
                { "createdAt", Timestamp.FromDateTime(CreatedAt.ToUniversalTime()) },
                { "members", membersList },
                { "memberIds", Members.ConvertAll(m => m.PlayerId) }, // For array-contains queries
                { "maxMembers", MaxMembers },
                { "totalTerritories", TotalTerritories },
                { "totalAttacksWon", TotalAttacksWon },
                { "totalDefensesWon", TotalDefensesWon },
                { "weeklyXP", WeeklyXP },
                { "allTimeXP", AllTimeXP },
                { "isOpen", IsOpen },
                { "minLevelToJoin", MinLevelToJoin }
            };
        }

        /// <summary>
        /// Create Alliance from Firestore document
        /// </summary>
        public static Alliance FromFirestore(DocumentSnapshot doc)
        {
            if (!doc.Exists) return null;

            var alliance = new Alliance();
            
            alliance.Id = doc.GetValue<string>("id");
            alliance.Name = doc.GetValue<string>("name");
            alliance.Tag = doc.GetValue<string>("tag");
            alliance.Description = doc.TryGetValue("description", out string desc) ? desc : "";
            alliance.LeaderId = doc.GetValue<string>("leaderId");
            alliance.LeaderName = doc.GetValue<string>("leaderName");
            
            if (doc.TryGetValue("createdAt", out Timestamp createdAt))
            {
                alliance.CreatedAt = createdAt.ToDateTime();
            }

            alliance.MaxMembers = doc.GetValue<int>("maxMembers");
            alliance.TotalTerritories = doc.GetValue<int>("totalTerritories");
            alliance.TotalAttacksWon = doc.GetValue<int>("totalAttacksWon");
            alliance.TotalDefensesWon = doc.GetValue<int>("totalDefensesWon");
            alliance.WeeklyXP = doc.GetValue<int>("weeklyXP");
            alliance.AllTimeXP = doc.GetValue<int>("allTimeXP");
            alliance.IsOpen = doc.GetValue<bool>("isOpen");
            alliance.MinLevelToJoin = doc.GetValue<int>("minLevelToJoin");

            // Parse members
            if (doc.TryGetValue("members", out List<object> membersData))
            {
                alliance.Members.Clear();
                foreach (var memberObj in membersData)
                {
                    if (memberObj is Dictionary<string, object> memberDict)
                    {
                        var member = new AllianceMember
                        {
                            PlayerId = memberDict["playerId"]?.ToString(),
                            PlayerName = memberDict["playerName"]?.ToString(),
                            ContributedXP = Convert.ToInt32(memberDict["contributedXP"]),
                            TerritoriesCaptured = Convert.ToInt32(memberDict["territoriesCaptured"]),
                            TerritoriesDefended = Convert.ToInt32(memberDict["territoriesDefended"])
                        };

                        if (memberDict.TryGetValue("role", out object roleObj) &&
                            Enum.TryParse(roleObj.ToString(), out AllianceRole role))
                        {
                            member.Role = role;
                        }

                        if (memberDict.TryGetValue("joinedAt", out object joinedObj) && 
                            joinedObj is Timestamp joinedTs)
                        {
                            member.JoinedAt = joinedTs.ToDateTime();
                        }

                        if (memberDict.TryGetValue("lastActive", out object activeObj) &&
                            activeObj is Timestamp activeTs)
                        {
                            member.LastActive = activeTs.ToDateTime();
                        }

                        alliance.Members.Add(member);
                    }
                }
            }

            return alliance;
        }
    }

    /// <summary>
    /// Alliance member data
    /// </summary>
    [Serializable]
    public class AllianceMember
    {
        public string PlayerId;
        public string PlayerName;
        public AllianceRole Role;
        public DateTime JoinedAt;
        public int ContributedXP;
        public int TerritoriesCaptured;
        public int TerritoriesDefended;
        public DateTime LastActive;
    }

    /// <summary>
    /// Roles within an alliance
    /// </summary>
    public enum AllianceRole
    {
        Member,
        Officer,
        Leader
    }

    /// <summary>
    /// Permissions for alliance actions
    /// </summary>
    public enum AlliancePermission
    {
        InvitePlayers,
        KickMembers,
        PromoteToOfficer,
        StartAllianceWar,
        ParticipateInWars,
        ViewAllianceChat,
        EditAllianceInfo,
        DisbandAlliance
    }

    /// <summary>
    /// Alliance invitation
    /// </summary>
    [Serializable]
    public class AllianceInvitation
    {
        public string Id;
        public string AllianceId;
        public string AllianceName;
        public string InviterId;
        public string InviterName;
        public string InviteeId;
        public DateTime CreatedAt;
        public DateTime ExpiresAt;
        public bool Accepted;
        public bool Declined;

        public AllianceInvitation() { }

        public AllianceInvitation(string allianceId, string allianceName, 
                                  string inviterId, string inviterName, string inviteeId)
        {
            Id = Guid.NewGuid().ToString();
            AllianceId = allianceId;
            AllianceName = allianceName;
            InviterId = inviterId;
            InviterName = inviterName;
            InviteeId = inviteeId;
            CreatedAt = DateTime.UtcNow;
            ExpiresAt = DateTime.UtcNow.AddDays(7);
        }

        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        public bool IsPending => !Accepted && !Declined && !IsExpired;

        /// <summary>
        /// Convert invitation to Firestore-compatible dictionary
        /// </summary>
        public Dictionary<string, object> ToFirestoreData()
        {
            return new Dictionary<string, object>
            {
                { "id", Id },
                { "allianceId", AllianceId },
                { "allianceName", AllianceName },
                { "inviterId", InviterId },
                { "inviterName", InviterName },
                { "inviteeId", InviteeId },
                { "createdAt", Timestamp.FromDateTime(CreatedAt.ToUniversalTime()) },
                { "expiresAt", Timestamp.FromDateTime(ExpiresAt.ToUniversalTime()) },
                { "accepted", Accepted },
                { "declined", Declined }
            };
        }

        /// <summary>
        /// Create AllianceInvitation from Firestore document
        /// </summary>
        public static AllianceInvitation FromFirestore(DocumentSnapshot doc)
        {
            if (!doc.Exists) return null;

            var invitation = new AllianceInvitation();
            
            invitation.Id = doc.GetValue<string>("id");
            invitation.AllianceId = doc.GetValue<string>("allianceId");
            invitation.AllianceName = doc.GetValue<string>("allianceName");
            invitation.InviterId = doc.GetValue<string>("inviterId");
            invitation.InviterName = doc.GetValue<string>("inviterName");
            invitation.InviteeId = doc.GetValue<string>("inviteeId");
            invitation.Accepted = doc.GetValue<bool>("accepted");
            invitation.Declined = doc.GetValue<bool>("declined");

            if (doc.TryGetValue("createdAt", out Timestamp createdAt))
            {
                invitation.CreatedAt = createdAt.ToDateTime();
            }
            if (doc.TryGetValue("expiresAt", out Timestamp expiresAt))
            {
                invitation.ExpiresAt = expiresAt.ToDateTime();
            }

            return invitation;
        }
    }

    /// <summary>
    /// Alliance war between two alliances
    /// </summary>
    [Serializable]
    public class AllianceWar
    {
        public string Id;
        public string AttackingAllianceId;
        public string AttackingAllianceName;
        public string DefendingAllianceId;
        public string DefendingAllianceName;
        
        public DateTime StartTime;
        public DateTime EndTime;
        public TimeSpan Duration => EndTime - StartTime;

        public int AttackerScore;
        public int DefenderScore;
        
        public WarStatus Status;
        public string WinnerId;

        public List<WarBattle> Battles = new List<WarBattle>();

        public AllianceWar() { }

        public AllianceWar(string attackerId, string attackerName, 
                          string defenderId, string defenderName, int durationHours = 24)
        {
            Id = Guid.NewGuid().ToString();
            AttackingAllianceId = attackerId;
            AttackingAllianceName = attackerName;
            DefendingAllianceId = defenderId;
            DefendingAllianceName = defenderName;
            StartTime = DateTime.UtcNow;
            EndTime = StartTime.AddHours(durationHours);
            Status = WarStatus.Active;
        }

        public bool IsActive => Status == WarStatus.Active && DateTime.UtcNow < EndTime;

        /// <summary>
        /// Convert war to Firestore-compatible dictionary
        /// </summary>
        public Dictionary<string, object> ToFirestoreData()
        {
            var battlesList = new List<Dictionary<string, object>>();
            foreach (var battle in Battles)
            {
                battlesList.Add(new Dictionary<string, object>
                {
                    { "territoryId", battle.TerritoryId },
                    { "territoryName", battle.TerritoryName },
                    { "attackerId", battle.AttackerId },
                    { "defenderId", battle.DefenderId },
                    { "pointsAwarded", battle.PointsAwarded },
                    { "attackerWon", battle.AttackerWon },
                    { "timestamp", Timestamp.FromDateTime(battle.Timestamp.ToUniversalTime()) }
                });
            }

            return new Dictionary<string, object>
            {
                { "id", Id },
                { "attackingAllianceId", AttackingAllianceId },
                { "attackingAllianceName", AttackingAllianceName },
                { "defendingAllianceId", DefendingAllianceId },
                { "defendingAllianceName", DefendingAllianceName },
                { "startTime", Timestamp.FromDateTime(StartTime.ToUniversalTime()) },
                { "endTime", Timestamp.FromDateTime(EndTime.ToUniversalTime()) },
                { "attackerScore", AttackerScore },
                { "defenderScore", DefenderScore },
                { "status", Status.ToString() },
                { "winnerId", WinnerId ?? "" },
                { "battles", battlesList }
            };
        }

        /// <summary>
        /// Create AllianceWar from Firestore document
        /// </summary>
        public static AllianceWar FromFirestore(DocumentSnapshot doc)
        {
            if (!doc.Exists) return null;

            var war = new AllianceWar();
            
            war.Id = doc.GetValue<string>("id");
            war.AttackingAllianceId = doc.GetValue<string>("attackingAllianceId");
            war.AttackingAllianceName = doc.GetValue<string>("attackingAllianceName");
            war.DefendingAllianceId = doc.GetValue<string>("defendingAllianceId");
            war.DefendingAllianceName = doc.GetValue<string>("defendingAllianceName");
            war.AttackerScore = doc.GetValue<int>("attackerScore");
            war.DefenderScore = doc.GetValue<int>("defenderScore");
            war.WinnerId = doc.TryGetValue("winnerId", out string winner) ? winner : null;

            if (doc.TryGetValue("status", out string statusStr) &&
                Enum.TryParse(statusStr, out WarStatus status))
            {
                war.Status = status;
            }

            if (doc.TryGetValue("startTime", out Timestamp startTime))
            {
                war.StartTime = startTime.ToDateTime();
            }
            if (doc.TryGetValue("endTime", out Timestamp endTime))
            {
                war.EndTime = endTime.ToDateTime();
            }

            // Parse battles
            if (doc.TryGetValue("battles", out List<object> battlesData))
            {
                war.Battles.Clear();
                foreach (var battleObj in battlesData)
                {
                    if (battleObj is Dictionary<string, object> battleDict)
                    {
                        var battle = new WarBattle
                        {
                            TerritoryId = battleDict["territoryId"]?.ToString(),
                            TerritoryName = battleDict["territoryName"]?.ToString(),
                            AttackerId = battleDict["attackerId"]?.ToString(),
                            DefenderId = battleDict["defenderId"]?.ToString(),
                            PointsAwarded = Convert.ToInt32(battleDict["pointsAwarded"]),
                            AttackerWon = Convert.ToBoolean(battleDict["attackerWon"])
                        };

                        if (battleDict.TryGetValue("timestamp", out object tsObj) &&
                            tsObj is Timestamp ts)
                        {
                            battle.Timestamp = ts.ToDateTime();
                        }

                        war.Battles.Add(battle);
                    }
                }
            }

            return war;
        }
    }

    /// <summary>
    /// Individual battle in an alliance war
    /// </summary>
    [Serializable]
    public class WarBattle
    {
        public string TerritoryId;
        public string TerritoryName;
        public string AttackerId;
        public string DefenderId;
        public int PointsAwarded;
        public bool AttackerWon;
        public DateTime Timestamp;
    }

    public enum WarStatus
    {
        Pending,
        Active,
        Completed,
        Cancelled
    }
}
