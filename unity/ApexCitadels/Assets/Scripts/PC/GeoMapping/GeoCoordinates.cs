// ============================================================================
// APEX CITADELS - GEO COORDINATES SYSTEM
// Core coordinate transformations between GPS and Unity world space
// ============================================================================
using System;
using UnityEngine;
using ApexCitadels.Core;

namespace ApexCitadels.PC.GeoMapping
{
    /// <summary>
    /// Geographic coordinate representation with conversion utilities.
    /// This is the foundation for the "One World - Two Ways to Access" vision.
    /// </summary>
    [Serializable]
    public struct GeoCoordinate
    {
        public double Latitude;
        public double Longitude;
        public double Altitude;

        public GeoCoordinate(double latitude, double longitude, double altitude = 0)
        {
            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
        }

        /// <summary>
        /// Parse from string format "lat,lon" or "lat,lon,alt"
        /// </summary>
        public static GeoCoordinate Parse(string coordString)
        {
            var parts = coordString.Split(',');
            return new GeoCoordinate(
                double.Parse(parts[0].Trim()),
                double.Parse(parts[1].Trim()),
                parts.Length > 2 ? double.Parse(parts[2].Trim()) : 0
            );
        }

        public override string ToString() => $"{Latitude:F6},{Longitude:F6}";
        
        public string ToFullString() => $"{Latitude:F6},{Longitude:F6},{Altitude:F2}";
    }

    /// <summary>
    /// Bounding box for geographic areas
    /// </summary>
    [Serializable]
    public struct GeoBounds
    {
        public double North; // Max latitude
        public double South; // Min latitude
        public double East;  // Max longitude
        public double West;  // Min longitude

        public double CenterLatitude => (North + South) / 2.0;
        public double CenterLongitude => (East + West) / 2.0;
        public double LatitudeSpan => North - South;
        public double LongitudeSpan => East - West;

        public GeoBounds(double north, double south, double east, double west)
        {
            North = north;
            South = south;
            East = east;
            West = west;
        }

        public static GeoBounds FromCenterAndRadius(GeoCoordinate center, double radiusMeters)
        {
            // Rough approximation: 1 degree latitude ~ 111,320 meters
            double latOffset = radiusMeters / 111320.0;
            // Longitude offset depends on latitude
            double lonOffset = radiusMeters / (111320.0 * Math.Cos(center.Latitude * Math.PI / 180.0));

            return new GeoBounds(
                center.Latitude + latOffset,
                center.Latitude - latOffset,
                center.Longitude + lonOffset,
                center.Longitude - lonOffset
            );
        }

        public bool Contains(GeoCoordinate coord)
        {
            return coord.Latitude >= South && coord.Latitude <= North &&
                   coord.Longitude >= West && coord.Longitude <= East;
        }
    }

    /// <summary>
    /// Converts between GPS coordinates and Unity world space.
    /// Uses Web Mercator projection (EPSG:3857) for compatibility with map tiles.
    /// </summary>
    public static class GeoProjection
    {
        // Earth's radius in meters (WGS84)
        public const double EarthRadius = 6378137.0;
        
        // Web Mercator bounds
        public const double MaxLatitude = 85.0511287798;
        public const double MinLatitude = -85.0511287798;
        
        // Scale factor: meters per Unity unit at the equator
        // This determines how detailed the world appears
        // 1 Unity unit = 1 meter at default scale
        private static double _metersPerUnit = 1.0;
        
        // Reference point (origin in Unity space = this GPS coordinate)
        private static GeoCoordinate _referencePoint = new GeoCoordinate(38.9072, -77.0369); // Washington DC default
        private static Vector3 _referenceOffset = Vector3.zero;

        /// <summary>
        /// Set the reference point for coordinate conversion.
        /// This GPS coordinate becomes the center of the Unity world (0,0,0).
        /// </summary>
        public static void SetReferencePoint(GeoCoordinate reference)
        {
            _referencePoint = reference;
            _referenceOffset = Vector3.zero;
            ApexLogger.Log($"Reference point set to {reference}", ApexLogger.LogCategory.Map);
        }

        /// <summary>
        /// Set the scale factor (meters per Unity unit)
        /// </summary>
        public static void SetScale(double metersPerUnit)
        {
            _metersPerUnit = metersPerUnit;
            ApexLogger.Log($"Scale set to {metersPerUnit} meters/unit", ApexLogger.LogCategory.Map);
        }

        /// <summary>
        /// Convert GPS coordinates to Unity world position.
        /// Uses equirectangular projection for simplicity (good for city-scale).
        /// </summary>
        public static Vector3 GeoToWorld(GeoCoordinate coord)
        {
            // Calculate offset from reference point in meters
            double latDiff = coord.Latitude - _referencePoint.Latitude;
            double lonDiff = coord.Longitude - _referencePoint.Longitude;

            // Meters per degree of latitude (fairly constant)
            double metersPerDegreeLat = 111320.0;
            // Meters per degree of longitude (varies with latitude)
            double metersPerDegreeLon = 111320.0 * Math.Cos(_referencePoint.Latitude * Math.PI / 180.0);

            double xMeters = lonDiff * metersPerDegreeLon;
            double zMeters = latDiff * metersPerDegreeLat;

            // Convert to Unity units
            float x = (float)(xMeters / _metersPerUnit);
            float z = (float)(zMeters / _metersPerUnit);
            float y = (float)(coord.Altitude / _metersPerUnit);

            return new Vector3(x, y, z) + _referenceOffset;
        }

        /// <summary>
        /// Convert Unity world position to GPS coordinates.
        /// </summary>
        public static GeoCoordinate WorldToGeo(Vector3 worldPos)
        {
            Vector3 offset = worldPos - _referenceOffset;

            // Convert Unity units to meters
            double xMeters = offset.x * _metersPerUnit;
            double zMeters = offset.z * _metersPerUnit;
            double yMeters = offset.y * _metersPerUnit;

            // Meters per degree
            double metersPerDegreeLat = 111320.0;
            double metersPerDegreeLon = 111320.0 * Math.Cos(_referencePoint.Latitude * Math.PI / 180.0);

            double lat = _referencePoint.Latitude + (zMeters / metersPerDegreeLat);
            double lon = _referencePoint.Longitude + (xMeters / metersPerDegreeLon);

            return new GeoCoordinate(lat, lon, yMeters);
        }

        /// <summary>
        /// Calculate the distance in meters between two GPS coordinates.
        /// Uses Haversine formula for accuracy on a sphere.
        /// </summary>
        public static double DistanceMeters(GeoCoordinate a, GeoCoordinate b)
        {
            double dLat = ToRadians(b.Latitude - a.Latitude);
            double dLon = ToRadians(b.Longitude - a.Longitude);

            double lat1 = ToRadians(a.Latitude);
            double lat2 = ToRadians(b.Latitude);

            double aCalc = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                           Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * 
                           Math.Cos(lat1) * Math.Cos(lat2);
            double c = 2 * Math.Atan2(Math.Sqrt(aCalc), Math.Sqrt(1 - aCalc));

            return EarthRadius * c;
        }

        /// <summary>
        /// Calculate the bearing (direction) from point A to point B in degrees.
        /// 0 = North, 90 = East, 180 = South, 270 = West
        /// </summary>
        public static double Bearing(GeoCoordinate from, GeoCoordinate to)
        {
            double dLon = ToRadians(to.Longitude - from.Longitude);
            double lat1 = ToRadians(from.Latitude);
            double lat2 = ToRadians(to.Latitude);

            double y = Math.Sin(dLon) * Math.Cos(lat2);
            double x = Math.Cos(lat1) * Math.Sin(lat2) - 
                       Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);

            double bearing = Math.Atan2(y, x);
            return (ToDegrees(bearing) + 360) % 360;
        }

        /// <summary>
        /// Calculate a new coordinate given a start point, bearing, and distance.
        /// </summary>
        public static GeoCoordinate DestinationPoint(GeoCoordinate start, double bearingDegrees, double distanceMeters)
        {
            double angularDistance = distanceMeters / EarthRadius;
            double bearing = ToRadians(bearingDegrees);
            double lat1 = ToRadians(start.Latitude);
            double lon1 = ToRadians(start.Longitude);

            double lat2 = Math.Asin(Math.Sin(lat1) * Math.Cos(angularDistance) +
                                     Math.Cos(lat1) * Math.Sin(angularDistance) * Math.Cos(bearing));
            double lon2 = lon1 + Math.Atan2(
                Math.Sin(bearing) * Math.Sin(angularDistance) * Math.Cos(lat1),
                Math.Cos(angularDistance) - Math.Sin(lat1) * Math.Sin(lat2));

            return new GeoCoordinate(ToDegrees(lat2), ToDegrees(lon2), start.Altitude);
        }

        /// <summary>
        /// Convert meters to Unity world units at the current scale.
        /// </summary>
        public static float MetersToUnits(double meters)
        {
            return (float)(meters / _metersPerUnit);
        }

        /// <summary>
        /// Convert Unity units to meters at the current scale.
        /// </summary>
        public static double UnitsToMeters(float units)
        {
            return units * _metersPerUnit;
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
        private static double ToDegrees(double radians) => radians * 180.0 / Math.PI;

        /// <summary>
        /// Get the reference point currently in use.
        /// </summary>
        public static GeoCoordinate ReferencePoint => _referencePoint;
    }

    /// <summary>
    /// Standard Slippy Map tile coordinates (used by OSM, Mapbox, Google Maps, etc.)
    /// </summary>
    public struct TileCoordinate
    {
        public int X;
        public int Y;
        public int Zoom;

        public TileCoordinate(int x, int y, int zoom)
        {
            X = x;
            Y = y;
            Zoom = zoom;
        }

        /// <summary>
        /// Convert GPS coordinate to tile coordinates at a given zoom level.
        /// </summary>
        public static TileCoordinate FromGeo(GeoCoordinate coord, int zoom)
        {
            int n = (int)Math.Pow(2, zoom);
            int x = (int)((coord.Longitude + 180.0) / 360.0 * n);
            double latRad = coord.Latitude * Math.PI / 180.0;
            int y = (int)((1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * n);
            
            // Clamp to valid range
            x = Math.Max(0, Math.Min(n - 1, x));
            y = Math.Max(0, Math.Min(n - 1, y));
            
            return new TileCoordinate(x, y, zoom);
        }

        /// <summary>
        /// Get the GPS coordinate of the tile's northwest corner.
        /// </summary>
        public GeoCoordinate ToGeoNW()
        {
            double n = Math.Pow(2, Zoom);
            double lon = X / n * 360.0 - 180.0;
            double latRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * Y / n)));
            double lat = latRad * 180.0 / Math.PI;
            return new GeoCoordinate(lat, lon);
        }

        /// <summary>
        /// Get the GPS coordinate of the tile's center.
        /// </summary>
        public GeoCoordinate ToGeoCenter()
        {
            double n = Math.Pow(2, Zoom);
            double lon = (X + 0.5) / n * 360.0 - 180.0;
            double latRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * (Y + 0.5) / n)));
            double lat = latRad * 180.0 / Math.PI;
            return new GeoCoordinate(lat, lon);
        }

        /// <summary>
        /// Get the ground resolution in meters/pixel at this tile's latitude.
        /// Assumes 256x256 pixel tiles.
        /// </summary>
        public double GroundResolution()
        {
            double lat = ToGeoCenter().Latitude;
            return 156543.03392 * Math.Cos(lat * Math.PI / 180.0) / Math.Pow(2, Zoom);
        }

        public override string ToString() => $"{Zoom}/{X}/{Y}";
    }
}
