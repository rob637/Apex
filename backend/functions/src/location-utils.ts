/**
 * Location Utilities
 * 
 * Handles population density detection for dynamic territory radius sizing.
 * 
 * Territory Radius by Density:
 * - Urban (dense city): 25m - packed areas need smaller territories
 * - Suburban: 35m - moderate density
 * - Rural: 50m - spread out areas get larger territories
 * 
 * Detection Methods (in order of preference):
 * 1. OpenStreetMap landuse data via Overpass API
 * 2. Cached density grid (for performance)
 * 3. Fallback to suburban as default
 */

import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';
import {
  LocationDensity,
  TERRITORY_RADIUS_BY_DENSITY
} from './types';

const db = admin.firestore();

// Cache duration for density lookups (7 days)
const DENSITY_CACHE_DAYS = 7;

// ============================================================================
// DENSITY DETECTION
// ============================================================================

/**
 * Get population density for a location
 * Uses cached data if available, otherwise queries OSM
 */
export async function getLocationDensity(
  latitude: number,
  longitude: number
): Promise<LocationDensity> {
  // Round to ~100m grid for caching
  const gridLat = Math.round(latitude * 1000) / 1000;
  const gridLon = Math.round(longitude * 1000) / 1000;
  const cacheKey = `${gridLat}_${gridLon}`;

  // Check cache first
  const cached = await getCachedDensity(cacheKey);
  if (cached) {
    return cached;
  }

  // Query OpenStreetMap
  try {
    const density = await queryOsmDensity(latitude, longitude);
    
    // Cache the result
    await cacheDensity(cacheKey, density, latitude, longitude);
    
    return density;
  } catch (error) {
    console.error('Failed to query OSM density:', error);
    // Default to suburban on error
    return 'suburban';
  }
}

/**
 * Get territory radius based on location
 */
export async function getTerritoryRadius(
  latitude: number,
  longitude: number
): Promise<{ radius: number; density: LocationDensity }> {
  const density = await getLocationDensity(latitude, longitude);
  const radius = TERRITORY_RADIUS_BY_DENSITY[density];
  
  return { radius, density };
}

// ============================================================================
// OPENSTREETMAP INTEGRATION
// ============================================================================

/**
 * Query OpenStreetMap Overpass API for landuse data
 * Determines density based on nearby landuse tags
 */
async function queryOsmDensity(
  latitude: number,
  longitude: number
): Promise<LocationDensity> {
  const radius = 200; // Check 200m radius
  
  // Overpass QL query for landuse and building density
  const query = `
    [out:json][timeout:10];
    (
      way["landuse"](around:${radius},${latitude},${longitude});
      way["building"](around:${radius},${latitude},${longitude});
      node["place"](around:${radius},${latitude},${longitude});
    );
    out tags;
  `;

  const response = await fetch('https://overpass-api.de/api/interpreter', {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: `data=${encodeURIComponent(query)}`
  });

  if (!response.ok) {
    throw new Error(`Overpass API error: ${response.status}`);
  }

  const data = await response.json() as OverpassResponse;
  
  return classifyDensityFromOsm(data);
}

interface OverpassResponse {
  elements: Array<{
    type: string;
    tags?: Record<string, string>;
  }>;
}

/**
 * Classify density based on OSM data
 */
function classifyDensityFromOsm(data: OverpassResponse): LocationDensity {
  let urbanScore = 0;
  let ruralScore = 0;
  let buildingCount = 0;

  for (const element of data.elements) {
    const tags = element.tags || {};
    
    // Count buildings
    if (tags.building) {
      buildingCount++;
    }

    // Urban indicators
    if (tags.landuse === 'commercial' || 
        tags.landuse === 'retail' ||
        tags.landuse === 'industrial') {
      urbanScore += 3;
    }
    
    if (tags.landuse === 'residential' && tags['residential'] === 'apartments') {
      urbanScore += 2;
    }

    if (tags.place === 'city' || tags.place === 'town') {
      urbanScore += 5;
    }

    if (tags.place === 'suburb' || tags.place === 'neighbourhood') {
      urbanScore += 2;
    }

    // Rural indicators
    if (tags.landuse === 'farmland' ||
        tags.landuse === 'forest' ||
        tags.landuse === 'meadow' ||
        tags.landuse === 'grass') {
      ruralScore += 3;
    }

    if (tags.place === 'village' || tags.place === 'hamlet') {
      ruralScore += 3;
    }

    if (tags.natural) {
      ruralScore += 2;
    }
  }

  // Building density factor
  if (buildingCount > 20) urbanScore += 3;
  else if (buildingCount > 10) urbanScore += 1;
  else if (buildingCount < 3) ruralScore += 2;

  // Classify based on scores
  const netScore = urbanScore - ruralScore;
  
  if (netScore > 5) return 'urban';
  if (netScore < -3) return 'rural';
  return 'suburban';
}

// ============================================================================
// CACHING
// ============================================================================

interface DensityCache {
  density: LocationDensity;
  latitude: number;
  longitude: number;
  queriedAt: admin.firestore.Timestamp;
  expiresAt: admin.firestore.Timestamp;
}

/**
 * Get cached density for a grid cell
 */
async function getCachedDensity(cacheKey: string): Promise<LocationDensity | null> {
  const cacheDoc = await db.collection('density_cache').doc(cacheKey).get();
  
  if (!cacheDoc.exists) {
    return null;
  }

  const cache = cacheDoc.data() as DensityCache;
  
  // Check if cache is expired
  if (cache.expiresAt.toDate() < new Date()) {
    return null;
  }

  return cache.density;
}

/**
 * Cache density result
 */
async function cacheDensity(
  cacheKey: string,
  density: LocationDensity,
  latitude: number,
  longitude: number
): Promise<void> {
  const now = new Date();
  const expires = new Date(now.getTime() + DENSITY_CACHE_DAYS * 24 * 60 * 60 * 1000);

  const cache: DensityCache = {
    density,
    latitude,
    longitude,
    queriedAt: admin.firestore.Timestamp.fromDate(now),
    expiresAt: admin.firestore.Timestamp.fromDate(expires)
  };

  await db.collection('density_cache').doc(cacheKey).set(cache);
}

// ============================================================================
// MANUAL OVERRIDE
// ============================================================================

/**
 * Admin function to manually set density for an area
 * Useful for correcting misclassifications
 */
export const setAreaDensity = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  // Verify admin
  const adminDoc = await db.collection('admins').doc(context.auth.uid).get();
  if (!adminDoc.exists) {
    throw new functions.https.HttpsError('permission-denied', 'Admin access required');
  }

  const { latitude, longitude, density, reason } = data as {
    latitude: number;
    longitude: number;
    density: LocationDensity;
    reason: string;
  };

  if (!latitude || !longitude || !density || !reason) {
    throw new functions.https.HttpsError('invalid-argument', 'All fields required');
  }

  if (!['urban', 'suburban', 'rural'].includes(density)) {
    throw new functions.https.HttpsError('invalid-argument', 'Invalid density value');
  }

  // Create cache key
  const gridLat = Math.round(latitude * 1000) / 1000;
  const gridLon = Math.round(longitude * 1000) / 1000;
  const cacheKey = `${gridLat}_${gridLon}`;

  // Set extended expiry for manual overrides (1 year)
  const expires = new Date(Date.now() + 365 * 24 * 60 * 60 * 1000);

  await db.collection('density_cache').doc(cacheKey).set({
    density,
    latitude: gridLat,
    longitude: gridLon,
    queriedAt: admin.firestore.Timestamp.now(),
    expiresAt: admin.firestore.Timestamp.fromDate(expires),
    manualOverride: true,
    overrideBy: context.auth.uid,
    overrideReason: reason
  });

  // Log admin action
  await db.collection('admin_logs').add({
    action: 'set_area_density',
    adminId: context.auth.uid,
    latitude: gridLat,
    longitude: gridLon,
    density,
    reason,
    createdAt: admin.firestore.Timestamp.now()
  });

  return {
    success: true,
    message: `Area density set to ${density}`,
    territoryRadius: TERRITORY_RADIUS_BY_DENSITY[density]
  };
});

/**
 * Get density info for a location (for display)
 */
export const getLocationInfo = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  const { latitude, longitude } = data as { latitude: number; longitude: number };

  if (typeof latitude !== 'number' || typeof longitude !== 'number') {
    throw new functions.https.HttpsError('invalid-argument', 'Valid coordinates required');
  }

  const { radius, density } = await getTerritoryRadius(latitude, longitude);

  return {
    success: true,
    location: { latitude, longitude },
    density,
    territoryRadius: radius,
    description: getDensityDescription(density)
  };
});

function getDensityDescription(density: LocationDensity): string {
  switch (density) {
    case 'urban':
      return 'Urban area - smaller territory radius (25m) due to high density';
    case 'suburban':
      return 'Suburban area - moderate territory radius (35m)';
    case 'rural':
      return 'Rural area - larger territory radius (50m) for spread-out regions';
  }
}

// ============================================================================
// BATCH OPERATIONS
// ============================================================================

/**
 * Pre-compute density for a city/region
 * Run offline to build cache
 */
export const precomputeRegionDensity = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be logged in');
  }

  // Verify admin
  const adminDoc = await db.collection('admins').doc(context.auth.uid).get();
  if (!adminDoc.exists) {
    throw new functions.https.HttpsError('permission-denied', 'Admin access required');
  }

  const { 
    centerLat, 
    centerLon, 
    radiusKm, 
    gridSizeMeters 
  } = data as {
    centerLat: number;
    centerLon: number;
    radiusKm: number;
    gridSizeMeters: number;
  };

  // Limit scope to prevent abuse
  if (radiusKm > 10) {
    throw new functions.https.HttpsError('invalid-argument', 'Max radius is 10km');
  }

  const gridSize = gridSizeMeters || 100;
  const gridStep = gridSize / 111000; // Approx meters to degrees

  let queriedCount = 0;
  const startTime = Date.now();
  const maxRuntime = 300000; // 5 minutes max

  // Generate grid points
  for (let lat = centerLat - (radiusKm / 111); lat <= centerLat + (radiusKm / 111); lat += gridStep) {
    for (let lon = centerLon - (radiusKm / 85); lon <= centerLon + (radiusKm / 85); lon += gridStep) {
      // Check runtime
      if (Date.now() - startTime > maxRuntime) {
        return {
          success: true,
          message: `Partial completion: ${queriedCount} points cached (timeout)`,
          queriedCount
        };
      }

      try {
        await getLocationDensity(lat, lon);
        queriedCount++;
        
        // Rate limit to avoid overloading Overpass API
        await new Promise(resolve => setTimeout(resolve, 100));
      } catch (error) {
        console.error(`Failed to query ${lat}, ${lon}:`, error);
      }
    }
  }

  return {
    success: true,
    message: `Pre-computed ${queriedCount} density points`,
    queriedCount
  };
});
