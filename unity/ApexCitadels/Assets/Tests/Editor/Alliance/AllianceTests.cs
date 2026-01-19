using NUnit.Framework;
using UnityEngine;
using ApexCitadels.Alliance;

namespace ApexCitadels.Tests.Editor.Alliance
{
    /// <summary>
    /// Unit tests for the Alliance data model.
    /// Tests creation, membership, permissions, and state management.
    /// </summary>
    [TestFixture]
    public class AllianceTests : TestBase
    {
        #region Alliance Creation Tests

        [Test]
        public void Alliance_DefaultConstructor_HasEmptyState()
        {
            // Arrange & Act
            var alliance = new ApexCitadels.Alliance.Alliance();

            // Assert
            Assert.IsNull(alliance.Id, "Default alliance should have null ID");
            Assert.IsNull(alliance.Name, "Default alliance should have null name");
            Assert.IsNotNull(alliance.Members, "Members list should be initialized");
            Assert.AreEqual(0, alliance.Members.Count, "Members list should be empty");
        }

        [Test]
        public void Alliance_ParameterizedConstructor_InitializesCorrectly()
        {
            // Arrange
            string name = "Test Alliance";
            string tag = "test";
            string leaderId = RandomId();
            string leaderName = "TestLeader";

            // Act
            var alliance = new ApexCitadels.Alliance.Alliance(name, tag, leaderId, leaderName);

            // Assert
            Assert.IsNotNull(alliance.Id, "Alliance should have an ID");
            Assert.AreEqual(name, alliance.Name, "Name should match");
            Assert.AreEqual("TEST", alliance.Tag, "Tag should be uppercase");
            Assert.AreEqual(leaderId, alliance.LeaderId, "Leader ID should match");
            Assert.AreEqual(leaderName, alliance.LeaderName, "Leader name should match");
            Assert.AreEqual(1, alliance.Members.Count, "Should have 1 member (the leader)");
        }

        [Test]
        public void Alliance_Creation_LeaderIsFirstMember()
        {
            // Arrange
            string leaderId = RandomId();
            string leaderName = "TestLeader";

            // Act
            var alliance = new ApexCitadels.Alliance.Alliance("Test", "TST", leaderId, leaderName);

            // Assert
            Assert.AreEqual(1, alliance.Members.Count, "Should have exactly 1 member");
            Assert.AreEqual(leaderId, alliance.Members[0].PlayerId, "First member should be the leader");
            Assert.AreEqual(AllianceRole.Leader, alliance.Members[0].Role, "Leader should have Leader role");
        }

        [Test]
        public void Alliance_Tag_ConvertedToUppercase()
        {
            // Arrange & Act
            var alliance = new ApexCitadels.Alliance.Alliance("Test", "abc", RandomId(), "Leader");

            // Assert
            Assert.AreEqual("ABC", alliance.Tag, "Tag should be converted to uppercase");
        }

        #endregion

        #region Membership Tests

        [Test]
        public void IsFull_UnderLimit_ReturnsFalse()
        {
            // Arrange
            var alliance = new ApexCitadels.Alliance.Alliance("Test", "TST", RandomId(), "Leader");
            alliance.MaxMembers = 50;

            // Act & Assert
            Assert.IsFalse(alliance.IsFull, "Alliance with 1 member should not be full");
        }

        [Test]
        public void IsFull_AtLimit_ReturnsTrue()
        {
            // Arrange
            var alliance = new ApexCitadels.Alliance.Alliance("Test", "TST", RandomId(), "Leader");
            alliance.MaxMembers = 1; // Limit to just the leader

            // Act & Assert
            Assert.IsTrue(alliance.IsFull, "Alliance at max capacity should be full");
        }

        [Test]
        public void GetMember_ExistingMember_ReturnsMember()
        {
            // Arrange
            string leaderId = RandomId();
            var alliance = new ApexCitadels.Alliance.Alliance("Test", "TST", leaderId, "Leader");

            // Act
            var member = alliance.GetMember(leaderId);

            // Assert
            Assert.IsNotNull(member, "Should find existing member");
            Assert.AreEqual(leaderId, member.PlayerId, "Should return correct member");
        }

        [Test]
        public void GetMember_NonExistentMember_ReturnsNull()
        {
            // Arrange
            var alliance = new ApexCitadels.Alliance.Alliance("Test", "TST", RandomId(), "Leader");

            // Act
            var member = alliance.GetMember("non_existent_id");

            // Assert
            Assert.IsNull(member, "Should return null for non-existent member");
        }

        #endregion

        #region Permission Tests

        [Test]
        public void HasPermission_Leader_HasAllPermissions()
        {
            // Arrange
            string leaderId = RandomId();
            var alliance = new ApexCitadels.Alliance.Alliance("Test", "TST", leaderId, "Leader");

            // Act & Assert
            Assert.IsTrue(alliance.HasPermission(leaderId, AlliancePermission.DisbandAlliance),
                "Leader should have DisbandAlliance permission");
            Assert.IsTrue(alliance.HasPermission(leaderId, AlliancePermission.PromoteToOfficer),
                "Leader should have PromoteToOfficer permission");
            Assert.IsTrue(alliance.HasPermission(leaderId, AlliancePermission.ParticipateInWars),
                "Leader should have ParticipateInWars permission");
            Assert.IsTrue(alliance.HasPermission(leaderId, AlliancePermission.ViewAllianceChat),
                "Leader should have ViewAllianceChat permission");
        }

        [Test]
        public void HasPermission_Officer_HasLimitedPermissions()
        {
            // Arrange
            string leaderId = RandomId();
            string officerId = RandomId();
            var alliance = new ApexCitadels.Alliance.Alliance("Test", "TST", leaderId, "Leader");
            alliance.Members.Add(new AllianceMember
            {
                PlayerId = officerId,
                PlayerName = "Officer",
                Role = AllianceRole.Officer
            });

            // Act & Assert
            Assert.IsFalse(alliance.HasPermission(officerId, AlliancePermission.DisbandAlliance),
                "Officer should NOT have DisbandAlliance permission");
            Assert.IsFalse(alliance.HasPermission(officerId, AlliancePermission.PromoteToOfficer),
                "Officer should NOT have PromoteToOfficer permission");
            Assert.IsTrue(alliance.HasPermission(officerId, AlliancePermission.ParticipateInWars),
                "Officer should have ParticipateInWars permission");
        }

        [Test]
        public void HasPermission_Member_HasBasicPermissions()
        {
            // Arrange
            string leaderId = RandomId();
            string memberId = RandomId();
            var alliance = new ApexCitadels.Alliance.Alliance("Test", "TST", leaderId, "Leader");
            alliance.Members.Add(new AllianceMember
            {
                PlayerId = memberId,
                PlayerName = "Member",
                Role = AllianceRole.Member
            });

            // Act & Assert
            Assert.IsFalse(alliance.HasPermission(memberId, AlliancePermission.DisbandAlliance),
                "Member should NOT have DisbandAlliance permission");
            Assert.IsTrue(alliance.HasPermission(memberId, AlliancePermission.ParticipateInWars),
                "Member should have ParticipateInWars permission");
            Assert.IsTrue(alliance.HasPermission(memberId, AlliancePermission.ViewAllianceChat),
                "Member should have ViewAllianceChat permission");
        }

        [Test]
        public void HasPermission_NonMember_HasNoPermissions()
        {
            // Arrange
            var alliance = new ApexCitadels.Alliance.Alliance("Test", "TST", RandomId(), "Leader");
            string nonMemberId = "not_a_member";

            // Act & Assert
            Assert.IsFalse(alliance.HasPermission(nonMemberId, AlliancePermission.ViewAllianceChat),
                "Non-member should NOT have any permissions");
            Assert.IsFalse(alliance.HasPermission(nonMemberId, AlliancePermission.ParticipateInWars),
                "Non-member should NOT have any permissions");
        }

        #endregion

        #region Alliance Stats Tests

        [Test]
        public void Alliance_Stats_InitializeToZero()
        {
            // Arrange & Act
            var alliance = new ApexCitadels.Alliance.Alliance("Test", "TST", RandomId(), "Leader");

            // Assert
            Assert.AreEqual(0, alliance.TotalTerritories, "TotalTerritories should be 0");
            Assert.AreEqual(0, alliance.TotalAttacksWon, "TotalAttacksWon should be 0");
            Assert.AreEqual(0, alliance.TotalDefensesWon, "TotalDefensesWon should be 0");
            Assert.AreEqual(0, alliance.WeeklyXP, "WeeklyXP should be 0");
            Assert.AreEqual(0, alliance.AllTimeXP, "AllTimeXP should be 0");
        }

        #endregion
    }

    /// <summary>
    /// Tests for AllianceMember data structure
    /// </summary>
    [TestFixture]
    public class AllianceMemberTests : TestBase
    {
        [Test]
        public void AllianceMember_Creation_HasDefaults()
        {
            // Arrange & Act
            var member = new AllianceMember();

            // Assert
            Assert.AreEqual(0, member.ContributedXP, "ContributedXP should default to 0");
            Assert.AreEqual(0, member.TerritoriesCaptured, "TerritoriesCaptured should default to 0");
        }

        [Test]
        public void AllianceMember_SetProperties_WorkCorrectly()
        {
            // Arrange & Act
            var member = new AllianceMember
            {
                PlayerId = "test_id",
                PlayerName = "TestPlayer",
                Role = AllianceRole.Officer,
                ContributedXP = 1000,
                TerritoriesCaptured = 5
            };

            // Assert
            Assert.AreEqual("test_id", member.PlayerId);
            Assert.AreEqual("TestPlayer", member.PlayerName);
            Assert.AreEqual(AllianceRole.Officer, member.Role);
            Assert.AreEqual(1000, member.ContributedXP);
            Assert.AreEqual(5, member.TerritoriesCaptured);
        }
    }
}
