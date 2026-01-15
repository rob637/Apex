/**
 * Apex Citadels - Real-time Territory Map API
 * 
 * The map is the core of the game:
 * - Real-time territory updates
 * - Nearby players/activities
 * - Map tile data
 * - WebSocket-style updates via Firestore listeners
 */

import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';

const db = admin.firestore();

// ============================================================================
// Types
// ============================================================================

export interface MapTile {
  id: string; // geohash
  geohash: string;
  bounds: {
    north: number;
    south: number;
    east: number;
    west: number;
  };
  center: {
    latitude: number;
    longitude: number;
  };
  
  // Territory data
  territories: TerritorySnapshot[];
  totalTerritories: number;
  
  // Activity
  recentActivity: MapActivity[];
  activePlayerCount: number;
  
  // Aggregates
  dominantAlliance?: {
    id: string;
    name: string;
    tag: string;
    territoryCount: number;
  };
  
  lastUpdated: admin.firestore.Timestamp;
}

export interface TerritorySnapshot {
  id: string;
  latitude: number;
  longitude: number;
  ownerId: string;
  ownerName: string;
  allianceId?: string;
  allianceName?: string;
  allianceTag?: string;
  allianceColor?: string;
  
  // Visual
  level: number;
  structureType: string;
  isContested: boolean;
  isShielded: boolean;
  
  // Stats
  defenseRating: number;
  totalBlocks: number;
}

export interface MapActivity {
  id: string;
  type: 'conquest' | 'defense' | 'claim' | 'attack' | 'world_event' | 'alliance_war';
  actorName: string;
  targetName?: string;
  description: string;
  latitude: number;
  longitude: number;
  timestamp: admin.firestore.Timestamp;
}

export interface NearbyPlayer {
  userId: string;
  username: string;
  level: number;
  avatarUrl?: string;
  alliance?: {
    id: string;
    tag: string;
  };
  distance: number;
  lastActive: admin.firestore.Timestamp;
  isOnline: boolean;
}

export interface MapFilter {
  showAlliances: boolean;
  showContested: boolean;
  showShielded: boolean;
  allianceFilter?: string;
  minLevel?: number;
  maxLevel?: number;
}

// ============================================================================
// Geohash Utilities
// ============================================================================

const BASE32 = '0123456789bcdefghjkmnpqrstuvwxyz';

function encodeGeohash(latitude: number, longitude: number, precision: number = 6): string {
  let latRange = [-90, 90];
  let lngRange = [-180, 180];
  let hash = '';
  let isEven = true;
  let bit = 0;
  let ch = 0;
  
  while (hash.length < precision) {
    if (isEven) {
      const mid = (lngRange[0] + lngRange[1]) / 2;
      if (longitude > mid) {
        ch |= (1 << (4 - bit));
        lngRange[0] = mid;
      } else {
        lngRange[1] = mid;
      }
    } else {
      const mid = (latRange[0] + latRange[1]) / 2;
      if (latitude > mid) {
        ch |= (1 << (4 - bit));
        latRange[0] = mid;
      } else {
        latRange[1] = mid;
      }
    }
    
    isEven = !isEven;
    bit++;
    
    if (bit === 5) {
      hash += BASE32[ch];
      bit = 0;
      ch = 0;
    }
  }
  
  return hash;
}

function getGeohashBounds(geohash: string): MapTile['bounds'] {
  let latRange = [-90, 90];
  let lngRange = [-180, 180];
  let isEven = true;
  
  for (const char of geohash) {
    const idx = BASE32.indexOf(char);
    for (let bit = 4; bit >= 0; bit--) {
      const bitN = (idx >> bit) & 1;
      if (isEven) {
        const mid = (lngRange[0] + lngRange[1]) / 2;
        if (bitN === 1) {
          lngRange[0] = mid;
        } else {
          lngRange[1] = mid;
        }
      } else {
        const mid = (latRange[0] + latRange[1]) / 2;
        if (bitN === 1) {
          latRange[0] = mid;
        } else {
          latRange[1] = mid;
        }
      }
      isEven = !isEven;
    }
  }
  
  return {
    north: latRange[1],
    south: latRange[0],
    east: lngRange[1],
    west: lngRange[0]
  };
}

function getNeighborGeohashes(geohash: string): string[] {
  // Simplified: return 8 neighbors + self
  const neighbors: string[] = [geohash];
  
  // This is a simplified version - in production, use a proper neighbor calculation
  const directions = [
    [-1, -1], [-1, 0], [-1, 1],
    [0, -1],          [0, 1],
    [1, -1], [1, 0], [1, 1]
  ];
  
  const bounds = getGeohashBounds(geohash);
  const centerLat = (bounds.north + bounds.south) / 2;
  const centerLng = (bounds.east + bounds.west) / 2;
  const latStep = bounds.north - bounds.south;
  const lngStep = bounds.east - bounds.west;
  
  for (const [dLat, dLng] of directions) {
    const neighborLat = centerLat + (dLat * latStep);
    const neighborLng = centerLng + (dLng * lngStep);
    
    if (neighborLat >= -90 && neighborLat <= 90 && 
        neighborLng >= -180 && neighborLng <= 180) {
      neighbors.push(encodeGeohash(neighborLat, neighborLng, geohash.length));
    }
  }
  
  return [...new Set(neighbors)];
}

// ============================================================================
// Get Map Tiles
// ============================================================================

export const getMapTiles = functions.https.onCall(async (data, context) => {
  const { 
    latitude, 
    longitude, 
    precision = 5, // ~5km x 5km tiles
    filters
  } = data as {
    latitude: number;
    longitude: number;
    precision?: number;
    filters?: MapFilter;
  };
  
  const centerGeohash = encodeGeohash(latitude, longitude, precision);
  const geohashes = getNeighborGeohashes(centerGeohash);
  
  // Get all map tiles
  const tiles: MapTile[] = [];
  
  for (const geohash of geohashes) {
    const tileDoc = await db.collection('map_tiles').doc(geohash).get();
    
    if (tileDoc.exists) {
      let tile = tileDoc.data() as MapTile;
      
      // Apply filters
      if (filters) {
        tile = applyFilters(tile, filters);
      }
      
      tiles.push(tile);
    } else {
      // Create empty tile structure
      const bounds = getGeohashBounds(geohash);
      tiles.push({
        id: geohash,
        geohash,
        bounds,
        center: {
          latitude: (bounds.north + bounds.south) / 2,
          longitude: (bounds.east + bounds.west) / 2
        },
        territories: [],
        totalTerritories: 0,
        recentActivity: [],
        activePlayerCount: 0,
        lastUpdated: admin.firestore.Timestamp.now()
      });
    }
  }
  
  return { 
    tiles,
    centerGeohash,
    timestamp: admin.firestore.Timestamp.now()
  };
});

function applyFilters(tile: MapTile, filters: MapFilter): MapTile {
  let territories = [...tile.territories];
  
  if (!filters.showContested) {
    territories = territories.filter(t => !t.isContested);
  }
  
  if (!filters.showShielded) {
    territories = territories.filter(t => !t.isShielded);
  }
  
  if (filters.allianceFilter) {
    territories = territories.filter(t => t.allianceId === filters.allianceFilter);
  }
  
  if (filters.minLevel) {
    territories = territories.filter(t => t.level >= filters.minLevel!);
  }
  
  if (filters.maxLevel) {
    territories = territories.filter(t => t.level <= filters.maxLevel!);
  }
  
  return {
    ...tile,
    territories,
    totalTerritories: territories.length
  };
}

// ============================================================================
// Get Territories in Area
// ============================================================================

export const getTerritoriesInArea = functions.https.onCall(async (data, context) => {
  const { 
    north, 
    south, 
    east, 
    west,
    limit = 100
  } = data as {
    north: number;
    south: number;
    east: number;
    west: number;
    limit?: number;
  };
  
  // Query territories within bounds
  // Note: Firestore doesn't support 2D range queries directly
  // We use geohash prefix matching
  const centerLat = (north + south) / 2;
  const centerLng = (east + west) / 2;
  const geohash = encodeGeohash(centerLat, centerLng, 4);
  
  const territoriesSnapshot = await db.collection('territories')
    .where('geohash', '>=', geohash)
    .where('geohash', '<=', geohash + '\uf8ff')
    .limit(limit)
    .get();
  
  // Filter by actual bounds
  const territories = territoriesSnapshot.docs
    .map(doc => doc.data())
    .filter(t => {
      return t.latitude >= south && t.latitude <= north &&
             t.longitude >= west && t.longitude <= east;
    })
    .map(t => ({
      id: t.id,
      latitude: t.latitude,
      longitude: t.longitude,
      ownerId: t.ownerId,
      ownerName: t.ownerName,
      allianceId: t.allianceId,
      allianceName: t.allianceName,
      allianceTag: t.allianceTag,
      level: t.level || 1,
      structureType: t.structureType || 'basic',
      isContested: t.isContested || false,
      isShielded: t.shield?.active || false,
      defenseRating: t.defenseRating || 0,
      totalBlocks: t.totalBlocks || 0
    }));
  
  return { territories, count: territories.length };
});

// ============================================================================
// Get Nearby Players
// ============================================================================

export const getNearbyPlayers = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { 
    latitude, 
    longitude, 
    radiusMeters = 1000,
    limit = 50
  } = data as {
    latitude: number;
    longitude: number;
    radiusMeters?: number;
    limit?: number;
  };
  
  const userId = context.auth.uid;
  const geohash = encodeGeohash(latitude, longitude, 6);
  const geohashes = getNeighborGeohashes(geohash);
  
  // Update our own location
  await db.collection('player_locations').doc(userId).set({
    userId,
    latitude,
    longitude,
    geohash,
    lastUpdated: admin.firestore.Timestamp.now()
  }, { merge: true });
  
  // Find nearby players
  const fiveMinutesAgo = Date.now() - 5 * 60 * 1000;
  
  const locationPromises = geohashes.map(gh =>
    db.collection('player_locations')
      .where('geohash', '==', gh)
      .where('lastUpdated', '>', admin.firestore.Timestamp.fromMillis(fiveMinutesAgo))
      .get()
  );
  
  const locationSnapshots = await Promise.all(locationPromises);
  
  const nearbyLocations: Array<{
    userId: string;
    latitude: number;
    longitude: number;
    lastUpdated: admin.firestore.Timestamp;
  }> = [];
  
  locationSnapshots.forEach(snapshot => {
    snapshot.docs.forEach(doc => {
      const data = doc.data();
      if (data.userId !== userId) {
        nearbyLocations.push(data as typeof nearbyLocations[0]);
      }
    });
  });
  
  // Calculate distances and filter
  const playersWithDistance = nearbyLocations
    .map(loc => ({
      ...loc,
      distance: calculateDistance(latitude, longitude, loc.latitude, loc.longitude)
    }))
    .filter(p => p.distance <= radiusMeters)
    .sort((a, b) => a.distance - b.distance)
    .slice(0, limit);
  
  // Get user profiles
  if (playersWithDistance.length === 0) {
    return { players: [], count: 0 };
  }
  
  const userIds = playersWithDistance.map(p => p.userId);
  const userDocs = await Promise.all(
    userIds.map(id => db.collection('users').doc(id).get())
  );
  
  const now = Date.now();
  
  const players: NearbyPlayer[] = userDocs
    .filter(doc => doc.exists)
    .map((doc, index) => {
      const data = doc.data()!;
      const playerLoc = playersWithDistance[index];
      
      return {
        userId: doc.id,
        username: data.username,
        level: data.level || 1,
        avatarUrl: data.avatarUrl,
        alliance: data.allianceId ? {
          id: data.allianceId,
          tag: data.allianceTag
        } : undefined,
        distance: Math.round(playerLoc.distance),
        lastActive: playerLoc.lastUpdated,
        isOnline: now - playerLoc.lastUpdated.toMillis() < 5 * 60 * 1000
      };
    });
  
  return { players, count: players.length };
});

// ============================================================================
// Get Recent Activity
// ============================================================================

export const getRecentActivity = functions.https.onCall(async (data, context) => {
  const { 
    latitude, 
    longitude, 
    radiusMeters = 5000,
    limit = 50
  } = data as {
    latitude: number;
    longitude: number;
    radiusMeters?: number;
    limit?: number;
  };
  
  const geohash = encodeGeohash(latitude, longitude, 5);
  const oneHourAgo = Date.now() - 60 * 60 * 1000;
  
  const activitySnapshot = await db.collection('map_activities')
    .where('geohash', '>=', geohash.substring(0, 4))
    .where('geohash', '<=', geohash.substring(0, 4) + '\uf8ff')
    .where('timestamp', '>', admin.firestore.Timestamp.fromMillis(oneHourAgo))
    .orderBy('timestamp', 'desc')
    .limit(limit * 2)
    .get();
  
  // Filter by actual distance
  const activities = activitySnapshot.docs
    .map(doc => doc.data() as MapActivity)
    .filter(a => {
      const distance = calculateDistance(latitude, longitude, a.latitude, a.longitude);
      return distance <= radiusMeters;
    })
    .slice(0, limit);
  
  return { activities, count: activities.length };
});

// ============================================================================
// Record Map Activity
// ============================================================================

export async function recordMapActivity(
  type: MapActivity['type'],
  actorName: string,
  latitude: number,
  longitude: number,
  description: string,
  targetName?: string
): Promise<void> {
  const geohash = encodeGeohash(latitude, longitude, 6);
  
  const activityRef = db.collection('map_activities').doc();
  const activity: MapActivity = {
    id: activityRef.id,
    type,
    actorName,
    targetName,
    description,
    latitude,
    longitude,
    timestamp: admin.firestore.Timestamp.now()
  };
  
  await activityRef.set({
    ...activity,
    geohash
  });
  
  // Update map tile
  const tileGeohash = geohash.substring(0, 5);
  await db.collection('map_tiles').doc(tileGeohash).set({
    recentActivity: admin.firestore.FieldValue.arrayUnion(activity),
    lastUpdated: admin.firestore.Timestamp.now()
  }, { merge: true });
}

// ============================================================================
// Update Map Tile (Called when territory changes)
// ============================================================================

export async function updateMapTile(
  territoryId: string,
  territoryData: Partial<TerritorySnapshot>
): Promise<void> {
  if (!territoryData.latitude || !territoryData.longitude) return;
  
  const geohash = encodeGeohash(territoryData.latitude, territoryData.longitude, 5);
  const tileRef = db.collection('map_tiles').doc(geohash);
  
  const tileDoc = await tileRef.get();
  
  if (tileDoc.exists) {
    const tile = tileDoc.data() as MapTile;
    
    // Update or add territory
    const existingIndex = tile.territories.findIndex(t => t.id === territoryId);
    
    if (existingIndex >= 0) {
      tile.territories[existingIndex] = {
        ...tile.territories[existingIndex],
        ...territoryData
      } as TerritorySnapshot;
    } else {
      tile.territories.push({
        id: territoryId,
        ...territoryData
      } as TerritorySnapshot);
    }
    
    // Recalculate dominant alliance
    const allianceCounts: Record<string, { count: number; name: string; tag: string }> = {};
    tile.territories.forEach(t => {
      if (t.allianceId) {
        if (!allianceCounts[t.allianceId]) {
          allianceCounts[t.allianceId] = { count: 0, name: t.allianceName || '', tag: t.allianceTag || '' };
        }
        allianceCounts[t.allianceId].count++;
      }
    });
    
    let dominantAlliance: MapTile['dominantAlliance'] | undefined;
    let maxCount = 0;
    
    for (const [allianceId, data] of Object.entries(allianceCounts)) {
      if (data.count > maxCount) {
        maxCount = data.count;
        dominantAlliance = {
          id: allianceId,
          name: data.name,
          tag: data.tag,
          territoryCount: data.count
        };
      }
    }
    
    await tileRef.update({
      territories: tile.territories,
      totalTerritories: tile.territories.length,
      dominantAlliance,
      lastUpdated: admin.firestore.Timestamp.now()
    });
  } else {
    // Create new tile
    const bounds = getGeohashBounds(geohash);
    
    await tileRef.set({
      id: geohash,
      geohash,
      bounds,
      center: {
        latitude: (bounds.north + bounds.south) / 2,
        longitude: (bounds.east + bounds.west) / 2
      },
      territories: [{
        id: territoryId,
        ...territoryData
      }],
      totalTerritories: 1,
      recentActivity: [],
      activePlayerCount: 0,
      lastUpdated: admin.firestore.Timestamp.now()
    });
  }
}

// ============================================================================
// Heatmap Data
// ============================================================================

export const getHeatmapData = functions.https.onCall(async (data, context) => {
  const { 
    north, 
    south, 
    east, 
    west,
    dataType = 'activity' // 'activity', 'territories', 'battles'
  } = data as {
    north: number;
    south: number;
    east: number;
    west: number;
    dataType?: string;
  };
  
  const centerLat = (north + south) / 2;
  const centerLng = (east + west) / 2;
  const geohash = encodeGeohash(centerLat, centerLng, 3);
  
  // Get aggregated heatmap data
  const heatmapSnapshot = await db.collection('heatmap_data')
    .where('geohash', '>=', geohash.substring(0, 2))
    .where('geohash', '<=', geohash.substring(0, 2) + '\uf8ff')
    .where('dataType', '==', dataType)
    .get();
  
  const points = heatmapSnapshot.docs.map(doc => {
    const d = doc.data();
    return {
      latitude: d.latitude,
      longitude: d.longitude,
      intensity: d.intensity
    };
  });
  
  return { points, dataType };
});

// ============================================================================
// Helper Functions
// ============================================================================

function calculateDistance(lat1: number, lng1: number, lat2: number, lng2: number): number {
  const R = 6371000; // Earth's radius in meters
  const dLat = (lat2 - lat1) * Math.PI / 180;
  const dLng = (lng2 - lng1) * Math.PI / 180;
  const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
    Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
    Math.sin(dLng / 2) * Math.sin(dLng / 2);
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  return R * c;
}

// ============================================================================
// Cleanup Old Data (Scheduled)
// ============================================================================

export const cleanupMapData = functions.pubsub
  .schedule('every 1 hours')
  .onRun(async () => {
    const oneHourAgo = Date.now() - 60 * 60 * 1000;
    
    // Clean old activities
    const oldActivities = await db.collection('map_activities')
      .where('timestamp', '<', admin.firestore.Timestamp.fromMillis(oneHourAgo))
      .limit(500)
      .get();
    
    const batch = db.batch();
    oldActivities.docs.forEach(doc => {
      batch.delete(doc.ref);
    });
    
    await batch.commit();
    
    functions.logger.info(`Cleaned up ${oldActivities.size} old map activities`);
  });
