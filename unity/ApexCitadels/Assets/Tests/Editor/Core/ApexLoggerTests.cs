using NUnit.Framework;
using UnityEngine;
using ApexCitadels.Core;

namespace ApexCitadels.Tests.Editor.Core
{
    /// <summary>
    /// Unit tests for ApexLogger.
    /// Tests log level filtering, category formatting, and configuration.
    /// </summary>
    [TestFixture]
    public class ApexLoggerTests : TestBase
    {
        private ApexLogger.LogLevel _originalLevel;
        private ApexLogger.LogCategory _originalCategories;

        public override void SetUp()
        {
            base.SetUp();
            // Store original settings
            _originalLevel = ApexLogger.MinimumLevel;
            _originalCategories = ApexLogger.EnabledCategories;
            
            // Reset to known state for testing
            ApexLogger.MinimumLevel = ApexLogger.LogLevel.Verbose;
            ApexLogger.EnabledCategories = ApexLogger.LogCategory.All;
        }

        public override void TearDown()
        {
            // Restore original settings
            ApexLogger.MinimumLevel = _originalLevel;
            ApexLogger.EnabledCategories = _originalCategories;
            base.TearDown();
        }

        #region Log Level Tests

        [Test]
        public void LogLevel_Enum_HasCorrectHierarchy()
        {
            // Assert log levels are in correct order
            Assert.That(ApexLogger.LogLevel.Verbose, Is.LessThan(ApexLogger.LogLevel.Debug));
            Assert.That(ApexLogger.LogLevel.Debug, Is.LessThan(ApexLogger.LogLevel.Info));
            Assert.That(ApexLogger.LogLevel.Info, Is.LessThan(ApexLogger.LogLevel.Warning));
            Assert.That(ApexLogger.LogLevel.Warning, Is.LessThan(ApexLogger.LogLevel.Error));
            Assert.That(ApexLogger.LogLevel.Error, Is.LessThan(ApexLogger.LogLevel.Fatal));
        }

        [Test]
        public void MinimumLevel_CanBeSet()
        {
            // Arrange & Act
            ApexLogger.MinimumLevel = ApexLogger.LogLevel.Warning;

            // Assert
            Assert.AreEqual(ApexLogger.LogLevel.Warning, ApexLogger.MinimumLevel);
        }

        [Test]
        public void MinimumLevel_VerboseAllowsAll()
        {
            // Arrange
            ApexLogger.MinimumLevel = ApexLogger.LogLevel.Verbose;

            // Assert - Verbose is lowest, so all levels pass
            Assert.That((int)ApexLogger.LogLevel.Debug, Is.GreaterThanOrEqualTo((int)ApexLogger.MinimumLevel));
            Assert.That((int)ApexLogger.LogLevel.Info, Is.GreaterThanOrEqualTo((int)ApexLogger.MinimumLevel));
            Assert.That((int)ApexLogger.LogLevel.Warning, Is.GreaterThanOrEqualTo((int)ApexLogger.MinimumLevel));
            Assert.That((int)ApexLogger.LogLevel.Error, Is.GreaterThanOrEqualTo((int)ApexLogger.MinimumLevel));
        }

        [Test]
        public void MinimumLevel_WarningBlocksLowerLevels()
        {
            // Arrange
            ApexLogger.MinimumLevel = ApexLogger.LogLevel.Warning;

            // Assert
            Assert.That((int)ApexLogger.LogLevel.Verbose, Is.LessThan((int)ApexLogger.MinimumLevel));
            Assert.That((int)ApexLogger.LogLevel.Debug, Is.LessThan((int)ApexLogger.MinimumLevel));
            Assert.That((int)ApexLogger.LogLevel.Info, Is.LessThan((int)ApexLogger.MinimumLevel));
            Assert.That((int)ApexLogger.LogLevel.Warning, Is.GreaterThanOrEqualTo((int)ApexLogger.MinimumLevel));
            Assert.That((int)ApexLogger.LogLevel.Error, Is.GreaterThanOrEqualTo((int)ApexLogger.MinimumLevel));
        }

        #endregion

        #region Category Tests

        [Test]
        public void LogCategory_HasExpectedCategories()
        {
            // This tests that key categories are properly defined
            Assert.IsTrue(ApexLogger.LogCategory.Territory != ApexLogger.LogCategory.None);
            Assert.IsTrue(ApexLogger.LogCategory.Combat != ApexLogger.LogCategory.None);
            Assert.IsTrue(ApexLogger.LogCategory.Alliance != ApexLogger.LogCategory.None);
            Assert.IsTrue(ApexLogger.LogCategory.UI != ApexLogger.LogCategory.None);
            Assert.IsTrue(ApexLogger.LogCategory.Firebase != ApexLogger.LogCategory.None);
            Assert.IsTrue(ApexLogger.LogCategory.AR != ApexLogger.LogCategory.None);
        }

        [Test]
        public void LogCategory_AllIncludesAllCategories()
        {
            // Arrange
            var allCategories = ApexLogger.LogCategory.All;

            // Assert - All should include every individual category
            Assert.IsTrue((allCategories & ApexLogger.LogCategory.General) != 0);
            Assert.IsTrue((allCategories & ApexLogger.LogCategory.Combat) != 0);
            Assert.IsTrue((allCategories & ApexLogger.LogCategory.Building) != 0);
            Assert.IsTrue((allCategories & ApexLogger.LogCategory.Territory) != 0);
        }

        [Test]
        public void EnabledCategories_CanBeSet()
        {
            // Arrange & Act
            ApexLogger.EnabledCategories = ApexLogger.LogCategory.Combat | ApexLogger.LogCategory.Territory;

            // Assert
            Assert.IsTrue((ApexLogger.EnabledCategories & ApexLogger.LogCategory.Combat) != 0);
            Assert.IsTrue((ApexLogger.EnabledCategories & ApexLogger.LogCategory.Territory) != 0);
            Assert.IsFalse((ApexLogger.EnabledCategories & ApexLogger.LogCategory.UI) != 0);
        }

        [Test]
        public void EnableCategory_AddsCategory()
        {
            // Arrange
            ApexLogger.EnabledCategories = ApexLogger.LogCategory.None;

            // Act
            ApexLogger.EnableCategory(ApexLogger.LogCategory.Combat);

            // Assert
            Assert.IsTrue((ApexLogger.EnabledCategories & ApexLogger.LogCategory.Combat) != 0);
        }

        [Test]
        public void DisableCategory_RemovesCategory()
        {
            // Arrange
            ApexLogger.EnabledCategories = ApexLogger.LogCategory.All;

            // Act
            ApexLogger.DisableCategory(ApexLogger.LogCategory.Combat);

            // Assert
            Assert.IsFalse((ApexLogger.EnabledCategories & ApexLogger.LogCategory.Combat) != 0);
            // Other categories should still be enabled
            Assert.IsTrue((ApexLogger.EnabledCategories & ApexLogger.LogCategory.Territory) != 0);
        }

        #endregion

        #region Configuration Tests

        [Test]
        public void Configure_SetsMultipleOptions()
        {
            // Arrange & Act
            ApexLogger.Configure(
                ApexLogger.LogLevel.Warning,
                ApexLogger.LogCategory.Combat | ApexLogger.LogCategory.Territory,
                timestamps: false
            );

            // Assert
            Assert.AreEqual(ApexLogger.LogLevel.Warning, ApexLogger.MinimumLevel);
            Assert.IsFalse(ApexLogger.IncludeTimestamps);
        }

        [Test]
        public void IncludeTimestamps_CanBeToggled()
        {
            // Arrange & Act
            ApexLogger.IncludeTimestamps = false;

            // Assert
            Assert.IsFalse(ApexLogger.IncludeTimestamps);

            // Reset
            ApexLogger.IncludeTimestamps = true;
            Assert.IsTrue(ApexLogger.IncludeTimestamps);
        }

        [Test]
        public void IncludeCategory_CanBeToggled()
        {
            // Arrange
            bool original = ApexLogger.IncludeCategory;

            // Act
            ApexLogger.IncludeCategory = !original;

            // Assert
            Assert.AreEqual(!original, ApexLogger.IncludeCategory);

            // Cleanup
            ApexLogger.IncludeCategory = original;
        }

        #endregion
    }
}
