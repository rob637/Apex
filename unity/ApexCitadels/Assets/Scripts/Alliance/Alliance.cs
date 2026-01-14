using System;
using System.Collections.Generic;
using UnityEngine;

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
