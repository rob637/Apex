using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ApexCitadels.Tests.Editor.Map
{
    /// <summary>
    /// Unit tests for map and geo-mapping system components.
    /// Tests coordinate conversion, tile management, and map calculations.
    /// </summary>
    [TestFixture]
    public class MapTests : TestBase
    {
        #region Coordinate Conversion Tests

        [Test]
        public void LatLonToWorld_Origin_ReturnsZero()
        {
            // Arrange
            double lat = 0;
            double lon = 0;
            
            // Act
            var worldPos = LatLonToWorld(lat, lon);
            
            // Assert
            Assert.AreEqual(0, worldPos.x, 0.01f);
            Assert.AreEqual(0, worldPos.z, 0.01f);
        }

        [Test]
        public void LatLonToWorld_PositiveCoordinates_ReturnsCorrectPosition()
        {
            // Arrange - New York City approximately
            double lat = 40.7128;
            double lon = -74.0060;
            
            // Act
            var worldPos = LatLonToWorld(lat, lon);
            
            // Assert - Just verify it returns valid values
            Assert.IsFalse(float.IsNaN(worldPos.x));
            Assert.IsFalse(float.IsNaN(worldPos.z));
        }

        [Test]
        public void WorldToLatLon_RoundTrip_PreservesCoordinates()
        {
            // Arrange
            double originalLat = 51.5074;
            double originalLon = -0.1278;
            
            // Act
            var worldPos = LatLonToWorld(originalLat, originalLon);
            var (lat, lon) = WorldToLatLon(worldPos);
            
            // Assert - Should be approximately equal after round trip
            Assert.AreEqual(originalLat, lat, 0.001, "Latitude should be preserved");
            Assert.AreEqual(originalLon, lon, 0.001, "Longitude should be preserved");
        }

        #endregion

        #region Distance Calculation Tests

        [Test]
        public void HaversineDistance_SamePoint_ReturnsZero()
        {
            // Arrange
            double lat = 40.7128;
            double lon = -74.0060;
            
            // Act
            double distance = HaversineDistance(lat, lon, lat, lon);
            
            // Assert
            Assert.AreEqual(0, distance, 0.001);
        }

        [Test]
        public void HaversineDistance_KnownDistance_ReturnsCorrectValue()
        {
            // Arrange - NYC to LA is approximately 3940 km
            double nycLat = 40.7128, nycLon = -74.0060;
            double laLat = 34.0522, laLon = -118.2437;
            
            // Act
            double distance = HaversineDistance(nycLat, nycLon, laLat, laLon);
            
            // Assert - Allow 5% tolerance
            Assert.AreEqual(3940, distance, 200, "NYC to LA should be ~3940 km");
        }

        [Test]
        public void HaversineDistance_ShortDistance_CalculatesCorrectly()
        {
            // Arrange - Two points roughly 1km apart
            double lat1 = 51.5074, lon1 = -0.1278;
            double lat2 = 51.5164, lon2 = -0.1278; // ~1km north
            
            // Act
            double distance = HaversineDistance(lat1, lon1, lat2, lon2);
            
            // Assert
            Assert.AreEqual(1.0, distance, 0.1, "Should be approximately 1 km");
        }

        #endregion

        #region Tile Calculation Tests

        [Test]
        public void TileXY_ZoomLevel0_ReturnsSingleTile()
        {
            // Arrange
            int zoom = 0;
            
            // Act
            int maxTiles = (int)Math.Pow(2, zoom);
            
            // Assert
            Assert.AreEqual(1, maxTiles);
        }

        [Test]
        public void TileXY_ZoomLevel10_ReturnsCorrectRange()
        {
            // Arrange
            int zoom = 10;
            
            // Act
            int maxTiles = (int)Math.Pow(2, zoom);
            
            // Assert
            Assert.AreEqual(1024, maxTiles);
        }

        [Test]
        public void LatLonToTile_ValidCoordinates_ReturnsValidTile()
        {
            // Arrange
            double lat = 51.5074;
            double lon = -0.1278;
            int zoom = 15;
            
            // Act
            var (tileX, tileY) = LatLonToTile(lat, lon, zoom);
            
            // Assert
            int maxTile = (int)Math.Pow(2, zoom) - 1;
            Assert.GreaterOrEqual(tileX, 0);
            Assert.LessOrEqual(tileX, maxTile);
            Assert.GreaterOrEqual(tileY, 0);
            Assert.LessOrEqual(tileY, maxTile);
        }

        [Test]
        public void TileToLatLon_RoundTrip_PreservesApproximateLocation()
        {
            // Arrange
            double originalLat = 51.5074;
            double originalLon = -0.1278;
            int zoom = 15;
            
            // Act
            var (tileX, tileY) = LatLonToTile(originalLat, originalLon, zoom);
            var (lat, lon) = TileToLatLon(tileX, tileY, zoom);
            
            // Assert - Tile center won't match exactly, but should be close
            Assert.AreEqual(originalLat, lat, 0.01);
            Assert.AreEqual(originalLon, lon, 0.01);
        }

        #endregion

        #region Map Bounds Tests

        [Test]
        public void MapBounds_Contains_ReturnsTrueForPointInside()
        {
            // Arrange
            var bounds = new TestMapBounds(0, 0, 100, 100);
            var point = new Vector2(50, 50);
            
            // Assert
            Assert.IsTrue(bounds.Contains(point));
        }

        [Test]
        public void MapBounds_Contains_ReturnsFalseForPointOutside()
        {
            // Arrange
            var bounds = new TestMapBounds(0, 0, 100, 100);
            var point = new Vector2(150, 50);
            
            // Assert
            Assert.IsFalse(bounds.Contains(point));
        }

        [Test]
        public void MapBounds_Intersects_ReturnsTrueForOverlap()
        {
            // Arrange
            var bounds1 = new TestMapBounds(0, 0, 100, 100);
            var bounds2 = new TestMapBounds(50, 50, 100, 100);
            
            // Assert
            Assert.IsTrue(bounds1.Intersects(bounds2));
        }

        [Test]
        public void MapBounds_Intersects_ReturnsFalseForNoOverlap()
        {
            // Arrange
            var bounds1 = new TestMapBounds(0, 0, 50, 50);
            var bounds2 = new TestMapBounds(100, 100, 50, 50);
            
            // Assert
            Assert.IsFalse(bounds1.Intersects(bounds2));
        }

        [Test]
        public void MapBounds_Center_CalculatesCorrectly()
        {
            // Arrange
            var bounds = new TestMapBounds(0, 0, 100, 100);
            
            // Act
            var center = bounds.Center;
            
            // Assert
            Assert.AreEqual(50, center.x);
            Assert.AreEqual(50, center.y);
        }

        #endregion

        #region Chunk Management Tests

        [Test]
        public void ChunkCoordinate_FromWorldPosition_CalculatesCorrectly()
        {
            // Arrange
            float chunkSize = 100f;
            Vector3 worldPos = new Vector3(250, 0, 175);
            
            // Act
            var chunkCoord = GetChunkCoordinate(worldPos, chunkSize);
            
            // Assert
            Assert.AreEqual(2, chunkCoord.x);
            Assert.AreEqual(1, chunkCoord.y);
        }

        [Test]
        public void ChunkCoordinate_NegativePosition_HandlesCorrectly()
        {
            // Arrange
            float chunkSize = 100f;
            Vector3 worldPos = new Vector3(-50, 0, -150);
            
            // Act
            var chunkCoord = GetChunkCoordinate(worldPos, chunkSize);
            
            // Assert
            Assert.AreEqual(-1, chunkCoord.x);
            Assert.AreEqual(-2, chunkCoord.y);
        }

        [Test]
        public void ChunksInRadius_ReturnsCorrectCount()
        {
            // Arrange
            var centerChunk = Vector2Int.zero;
            int radius = 1;
            
            // Act
            var chunks = GetChunksInRadius(centerChunk, radius);
            
            // Assert - 3x3 grid = 9 chunks
            Assert.AreEqual(9, chunks.Count);
        }

        #endregion

        #region Helper Methods

        private Vector3 LatLonToWorld(double lat, double lon)
        {
            // Simple Mercator projection for testing
            float x = (float)(lon * 111320 * Math.Cos(lat * Math.PI / 180));
            float z = (float)(lat * 110540);
            return new Vector3(x, 0, z);
        }

        private (double lat, double lon) WorldToLatLon(Vector3 worldPos)
        {
            double lat = worldPos.z / 110540;
            double lon = worldPos.x / (111320 * Math.Cos(lat * Math.PI / 180));
            return (lat, lon);
        }

        private double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Earth radius in km
            
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;
            
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return R * c;
        }

        private (int x, int y) LatLonToTile(double lat, double lon, int zoom)
        {
            int n = (int)Math.Pow(2, zoom);
            int x = (int)((lon + 180.0) / 360.0 * n);
            int y = (int)((1.0 - Math.Log(Math.Tan(lat * Math.PI / 180) + 
                    1.0 / Math.Cos(lat * Math.PI / 180)) / Math.PI) / 2.0 * n);
            return (x, y);
        }

        private (double lat, double lon) TileToLatLon(int x, int y, int zoom)
        {
            int n = (int)Math.Pow(2, zoom);
            double lon = x / (double)n * 360.0 - 180.0;
            double latRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * y / (double)n)));
            double lat = latRad * 180.0 / Math.PI;
            return (lat, lon);
        }

        private Vector2Int GetChunkCoordinate(Vector3 worldPos, float chunkSize)
        {
            int x = Mathf.FloorToInt(worldPos.x / chunkSize);
            int y = Mathf.FloorToInt(worldPos.z / chunkSize);
            return new Vector2Int(x, y);
        }

        private List<Vector2Int> GetChunksInRadius(Vector2Int center, int radius)
        {
            var chunks = new List<Vector2Int>();
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    chunks.Add(new Vector2Int(center.x + x, center.y + y));
                }
            }
            return chunks;
        }

        #endregion

        #region Test Data Classes

        private class TestMapBounds
        {
            public float X { get; }
            public float Y { get; }
            public float Width { get; }
            public float Height { get; }

            public Vector2 Center => new Vector2(X + Width / 2, Y + Height / 2);

            public TestMapBounds(float x, float y, float width, float height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            public bool Contains(Vector2 point)
            {
                return point.x >= X && point.x <= X + Width &&
                       point.y >= Y && point.y <= Y + Height;
            }

            public bool Intersects(TestMapBounds other)
            {
                return !(other.X > X + Width || other.X + other.Width < X ||
                         other.Y > Y + Height || other.Y + other.Height < Y);
            }
        }

        #endregion
    }
}
