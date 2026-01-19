using NUnit.Framework;
using UnityEngine;
using ApexCitadels.Territory;

namespace ApexCitadels.Tests.Editor.Territory
{
    /// <summary>
    /// Unit tests for the Territory data model.
    /// Tests GPS calculations, boundary detection, and territory state.
    /// </summary>
    [TestFixture]
    public class TerritoryTests : TestBase
    {
        #region Territory Creation Tests

        [Test]
        public void Territory_Creation_HasValidDefaults()
        {
            // Arrange & Act
            var territory = new ApexCitadels.Territory.Territory();

            // Assert
            Assert.IsNotNull(territory.Id, "Territory should have an ID");
            Assert.IsNotEmpty(territory.Id, "Territory ID should not be empty");
            Assert.AreEqual(1, territory.Level, "Default level should be 1");
            Assert.AreEqual(100, territory.Health, "Default health should be 100");
            Assert.AreEqual(100, territory.MaxHealth, "Default max health should be 100");
            Assert.AreEqual(10f, territory.RadiusMeters, "Default radius should be 10 meters");
            Assert.IsFalse(territory.IsContested, "Territory should not be contested by default");
        }

        [Test]
        public void Territory_Creation_GeneratesUniqueIds()
        {
            // Arrange & Act
            var territory1 = new ApexCitadels.Territory.Territory();
            var territory2 = new ApexCitadels.Territory.Territory();

            // Assert
            Assert.AreNotEqual(territory1.Id, territory2.Id, "Each territory should have a unique ID");
        }

        #endregion

        #region GPS Distance Calculation Tests

        [Test]
        public void CalculateDistance_SamePoint_ReturnsZero()
        {
            // Arrange
            var lat = TestLocations.ViennaCenter.Lat;
            var lon = TestLocations.ViennaCenter.Lon;

            // Act
            var distance = ApexCitadels.Territory.Territory.CalculateDistance(lat, lon, lat, lon);

            // Assert
            AssertApproximately(0f, distance, 0.001f, "Distance to same point should be 0");
        }

        [Test]
        public void CalculateDistance_KnownDistance_ReturnsApproximateValue()
        {
            // Arrange - Points approximately 100m apart
            var lat1 = TestLocations.Point100mA.Lat;
            var lon1 = TestLocations.Point100mA.Lon;
            var lat2 = TestLocations.Point100mB.Lat;
            var lon2 = TestLocations.Point100mB.Lon;

            // Act
            var distance = ApexCitadels.Territory.Territory.CalculateDistance(lat1, lon1, lat2, lon2);

            // Assert - Should be approximately 100m (within 10% tolerance for GPS math)
            Assert.That(distance, Is.InRange(90f, 110f), 
                $"Distance should be approximately 100m, got {distance}m");
        }

        [Test]
        public void CalculateDistance_LargeDistance_ReturnsReasonableValue()
        {
            // Arrange - Vienna to San Francisco (~3,800 km)
            var vienna = TestLocations.ViennaCenter;
            var sf = TestLocations.SFDowntown;

            // Act
            var distance = ApexCitadels.Territory.Territory.CalculateDistance(
                vienna.Lat, vienna.Lon, sf.Lat, sf.Lon);

            // Assert - Should be roughly 3,800 km (3,800,000 meters)
            Assert.That(distance, Is.InRange(3_700_000f, 3_900_000f),
                $"Distance Vienna to SF should be ~3,800km, got {distance / 1000}km");
        }

        [Test]
        public void CalculateDistance_IsSymmetric()
        {
            // Arrange
            var point1 = TestLocations.ViennaCenter;
            var point2 = TestLocations.ViennaPark;

            // Act
            var distance1to2 = ApexCitadels.Territory.Territory.CalculateDistance(
                point1.Lat, point1.Lon, point2.Lat, point2.Lon);
            var distance2to1 = ApexCitadels.Territory.Territory.CalculateDistance(
                point2.Lat, point2.Lon, point1.Lat, point1.Lon);

            // Assert
            AssertApproximately(distance1to2, distance2to1, 0.01f,
                "Distance should be symmetric");
        }

        #endregion

        #region ContainsLocation Tests

        [Test]
        public void ContainsLocation_PointAtCenter_ReturnsTrue()
        {
            // Arrange
            var territory = new ApexCitadels.Territory.Territory
            {
                CenterLatitude = TestLocations.ViennaCenter.Lat,
                CenterLongitude = TestLocations.ViennaCenter.Lon,
                RadiusMeters = 50f
            };

            // Act
            var contains = territory.ContainsLocation(
                TestLocations.ViennaCenter.Lat,
                TestLocations.ViennaCenter.Lon);

            // Assert
            Assert.IsTrue(contains, "Territory should contain its center point");
        }

        [Test]
        public void ContainsLocation_PointJustInside_ReturnsTrue()
        {
            // Arrange - Territory with 50m radius
            var territory = new ApexCitadels.Territory.Territory
            {
                CenterLatitude = TestLocations.Point100mA.Lat,
                CenterLongitude = TestLocations.Point100mA.Lon,
                RadiusMeters = 150f // Large enough to contain Point100mB
            };

            // Act
            var contains = territory.ContainsLocation(
                TestLocations.Point100mB.Lat,
                TestLocations.Point100mB.Lon);

            // Assert
            Assert.IsTrue(contains, "Territory should contain point within radius");
        }

        [Test]
        public void ContainsLocation_PointFarAway_ReturnsFalse()
        {
            // Arrange - Small territory in Vienna
            var territory = new ApexCitadels.Territory.Territory
            {
                CenterLatitude = TestLocations.ViennaCenter.Lat,
                CenterLongitude = TestLocations.ViennaCenter.Lon,
                RadiusMeters = 100f
            };

            // Act - Check if San Francisco is in Vienna territory (obviously not)
            var contains = territory.ContainsLocation(
                TestLocations.SFDowntown.Lat,
                TestLocations.SFDowntown.Lon);

            // Assert
            Assert.IsFalse(contains, "Territory should not contain point far outside radius");
        }

        #endregion

        #region Territory Overlap Tests

        [Test]
        public void OverlapsWith_SameLocation_ReturnsTrue()
        {
            // Arrange
            var territory1 = new ApexCitadels.Territory.Territory
            {
                CenterLatitude = TestLocations.ViennaCenter.Lat,
                CenterLongitude = TestLocations.ViennaCenter.Lon,
                RadiusMeters = 50f
            };
            var territory2 = new ApexCitadels.Territory.Territory
            {
                CenterLatitude = TestLocations.ViennaCenter.Lat,
                CenterLongitude = TestLocations.ViennaCenter.Lon,
                RadiusMeters = 50f
            };

            // Act
            var overlaps = territory1.OverlapsWith(territory2);

            // Assert
            Assert.IsTrue(overlaps, "Territories at same location should overlap");
        }

        [Test]
        public void OverlapsWith_FarApart_ReturnsFalse()
        {
            // Arrange
            var territory1 = new ApexCitadels.Territory.Territory
            {
                CenterLatitude = TestLocations.ViennaCenter.Lat,
                CenterLongitude = TestLocations.ViennaCenter.Lon,
                RadiusMeters = 50f
            };
            var territory2 = new ApexCitadels.Territory.Territory
            {
                CenterLatitude = TestLocations.SFDowntown.Lat,
                CenterLongitude = TestLocations.SFDowntown.Lon,
                RadiusMeters = 50f
            };

            // Act
            var overlaps = territory1.OverlapsWith(territory2);

            // Assert
            Assert.IsFalse(overlaps, "Territories far apart should not overlap");
        }

        [Test]
        public void OverlapsWith_Adjacent_ReturnsFalse()
        {
            // Arrange - Two territories ~100m apart with 40m radius each
            var territory1 = new ApexCitadels.Territory.Territory
            {
                CenterLatitude = TestLocations.Point100mA.Lat,
                CenterLongitude = TestLocations.Point100mA.Lon,
                RadiusMeters = 40f
            };
            var territory2 = new ApexCitadels.Territory.Territory
            {
                CenterLatitude = TestLocations.Point100mB.Lat,
                CenterLongitude = TestLocations.Point100mB.Lon,
                RadiusMeters = 40f
            };

            // Act
            var overlaps = territory1.OverlapsWith(territory2);

            // Assert - Combined radius (80m) is less than distance (~100m)
            Assert.IsFalse(overlaps, "Adjacent territories with gap should not overlap");
        }

        [Test]
        public void OverlapsWith_IsSymmetric()
        {
            // Arrange
            var territory1 = new ApexCitadels.Territory.Territory
            {
                CenterLatitude = TestLocations.ViennaCenter.Lat,
                CenterLongitude = TestLocations.ViennaCenter.Lon,
                RadiusMeters = 50f
            };
            var territory2 = new ApexCitadels.Territory.Territory
            {
                CenterLatitude = TestLocations.ViennaPark.Lat,
                CenterLongitude = TestLocations.ViennaPark.Lon,
                RadiusMeters = 50f
            };

            // Act
            var overlaps1to2 = territory1.OverlapsWith(territory2);
            var overlaps2to1 = territory2.OverlapsWith(territory1);

            // Assert
            Assert.AreEqual(overlaps1to2, overlaps2to1, "Overlap should be symmetric");
        }

        #endregion

        #region Territory State Tests

        [Test]
        public void Territory_Name_FallbackToIdPrefix()
        {
            // Arrange
            var territory = new ApexCitadels.Territory.Territory();
            territory.TerritoryName = null;

            // Act
            var name = territory.Name;

            // Assert
            Assert.IsNotNull(name, "Name should have a fallback value");
            Assert.That(name, Does.StartWith("Territory "), "Fallback name should start with 'Territory '");
        }

        [Test]
        public void Territory_PropertyAliases_WorkCorrectly()
        {
            // Arrange
            var territory = new ApexCitadels.Territory.Territory();
            
            // Act
            territory.Latitude = 38.9012;
            territory.Longitude = -77.2654;
            territory.Radius = 25f;

            // Assert
            Assert.AreEqual(38.9012, territory.CenterLatitude, "Latitude alias should work");
            Assert.AreEqual(-77.2654, territory.CenterLongitude, "Longitude alias should work");
            Assert.AreEqual(25f, territory.RadiusMeters, "Radius alias should work");
        }

        #endregion
    }
}
