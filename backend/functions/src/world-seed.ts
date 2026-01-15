/**
 * World Seed System - Cloud Functions
 * 
 * Pre-populates the game world with interesting content:
 * - NPC territories (Ancient Ruins, Abandoned Forts, Sacred Sites)
 * - Resource hotspots based on real-world POI data
 * - Discoverable landmarks that grant bonuses
 * - Starting content for new cities/regions
 * 
 * This creates a rich world from day one, rather than requiring
 * player-generated content to make the game interesting.
 */

import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';

const db = admin.firestore();

// ============================================================================
// Types
// ============================================================================

export enum SeedPointType {
    ANCIENT_RUINS = 'ancient_ruins',
    ABANDONED_FORT = 'abandoned_fort',
    SACRED_SITE = 'sacred_site',
    RESOURCE_HOTSPOT = 'resource_hotspot',
    LANDMARK = 'landmark',
    NPC_TERRITORY = 'npc_territory',
    MYSTERY_LOCATION = 'mystery_location',
    HISTORICAL_SITE = 'historical_site'
}

export enum ResourceType {
    STONE = 'stone',
    WOOD = 'wood',
    IRON = 'iron',
    CRYSTAL = 'crystal',
    ARCANE_ESSENCE = 'arcane_essence',
    ANCIENT_ARTIFACT = 'ancient_artifact',
    RARE_MATERIAL = 'rare_material'
}

export interface SeedPoint {
    id: string;
    type: SeedPointType;
    name: string;
    description: string;
    latitude: number;
    longitude: number;
    radius: number;
    level: number;
    resources?: ResourceReward[];
    bonuses?: SeedBonus[];
    discoveryReward?: DiscoveryReward;
    respawnHours?: number;
    lastClaimed?: admin.firestore.Timestamp;
    createdAt: admin.firestore.Timestamp;
    metadata?: Record<string, any>;
}

export interface ResourceReward {
    type: ResourceType;
    minAmount: number;
    maxAmount: number;
    probability: number;
}

export interface SeedBonus {
    type: string;  // e.g., 'attack_boost', 'defense_boost', 'resource_gather'
    value: number;
    durationHours?: number;
    radius?: number;
}

export interface DiscoveryReward {
    xp: number;
    resources?: { type: ResourceType; amount: number }[];
    achievement?: string;
}

export interface SeedRegion {
    id: string;
    name: string;
    centerLat: number;
    centerLon: number;
    radiusKm: number;
    seeded: boolean;
    seedCount: number;
    lastSeeded: admin.firestore.Timestamp;
}

export interface SeedTemplate {
    type: SeedPointType;
    nameTemplates: string[];
    descriptions: string[];
    levelRange: [number, number];
    radiusRange: [number, number];
    resources?: ResourceReward[];
    bonuses?: SeedBonus[];
    discoveryXp: number;
}

// ============================================================================
// Seed Templates
// ============================================================================

const SEED_TEMPLATES: Record<SeedPointType, SeedTemplate> = {
    [SeedPointType.ANCIENT_RUINS]: {
        type: SeedPointType.ANCIENT_RUINS,
        nameTemplates: [
            'Ancient Ruins of {adjective}',
            'Forgotten {noun} Ruins',
            '{adjective} Temple Remnants',
            'Crumbling {noun} Sanctuary',
            'Lost {adjective} Citadel'
        ],
        descriptions: [
            'Mysterious ruins from an ancient civilization. Who knows what treasures remain?',
            'These crumbling walls have witnessed countless ages. Ancient power still lingers here.',
            'Once a grand structure, now claimed by time. Explorers report strange phenomena.'
        ],
        levelRange: [3, 8],
        radiusRange: [50, 150],
        resources: [
            { type: ResourceType.STONE, minAmount: 50, maxAmount: 200, probability: 0.8 },
            { type: ResourceType.ANCIENT_ARTIFACT, minAmount: 1, maxAmount: 3, probability: 0.3 }
        ],
        bonuses: [
            { type: 'discovery_xp_boost', value: 1.5, radius: 100 }
        ],
        discoveryXp: 100
    },
    [SeedPointType.ABANDONED_FORT]: {
        type: SeedPointType.ABANDONED_FORT,
        nameTemplates: [
            'Fort {noun}',
            '{adjective} Stronghold',
            'Abandoned {noun} Keep',
            'The {adjective} Garrison',
            'Ruined Watchtower of {noun}'
        ],
        descriptions: [
            'A military outpost abandoned long ago. Its strategic value remains.',
            'These walls once held against great armies. Now only echoes remain.',
            'Claim this fort to gain a defensive advantage in the region.'
        ],
        levelRange: [5, 10],
        radiusRange: [75, 200],
        resources: [
            { type: ResourceType.IRON, minAmount: 30, maxAmount: 150, probability: 0.7 },
            { type: ResourceType.STONE, minAmount: 20, maxAmount: 100, probability: 0.6 }
        ],
        bonuses: [
            { type: 'defense_boost', value: 1.25, radius: 200 }
        ],
        discoveryXp: 150
    },
    [SeedPointType.SACRED_SITE]: {
        type: SeedPointType.SACRED_SITE,
        nameTemplates: [
            'Sacred Grove of {noun}',
            '{adjective} Shrine',
            'Holy {noun} Spring',
            'Blessed {adjective} Circle',
            'The {noun} Sanctum'
        ],
        descriptions: [
            'A place of ancient worship. Those who visit feel renewed.',
            'Mystical energies flow through this sacred ground.',
            'Legends speak of miracles occurring at this blessed site.'
        ],
        levelRange: [2, 6],
        radiusRange: [30, 100],
        resources: [
            { type: ResourceType.CRYSTAL, minAmount: 10, maxAmount: 50, probability: 0.5 },
            { type: ResourceType.ANCIENT_ARTIFACT, minAmount: 1, maxAmount: 2, probability: 0.2 }
        ],
        bonuses: [
            { type: 'healing_boost', value: 1.5, radius: 50 },
            { type: 'xp_boost', value: 1.2, durationHours: 2 }
        ],
        discoveryXp: 75
    },
    [SeedPointType.RESOURCE_HOTSPOT]: {
        type: SeedPointType.RESOURCE_HOTSPOT,
        nameTemplates: [
            '{adjective} Quarry',
            '{noun} Deposit',
            'Rich {adjective} Vein',
            'Ancient {noun} Mine',
            '{adjective} Resource Cache'
        ],
        descriptions: [
            'A rich deposit of valuable resources.',
            'Natural formations have concentrated resources here.',
            'Miners have long sought this location.'
        ],
        levelRange: [1, 5],
        radiusRange: [25, 75],
        resources: [
            { type: ResourceType.STONE, minAmount: 100, maxAmount: 500, probability: 0.4 },
            { type: ResourceType.WOOD, minAmount: 100, maxAmount: 500, probability: 0.4 },
            { type: ResourceType.IRON, minAmount: 50, maxAmount: 300, probability: 0.3 },
            { type: ResourceType.RARE_MATERIAL, minAmount: 5, maxAmount: 25, probability: 0.1 }
        ],
        bonuses: [
            { type: 'gather_speed', value: 1.5, radius: 50 }
        ],
        discoveryXp: 50
    },
    [SeedPointType.LANDMARK]: {
        type: SeedPointType.LANDMARK,
        nameTemplates: [
            'The {adjective} Monument',
            '{noun} Obelisk',
            'Great {adjective} Statue',
            'Ancient {noun} Pillar',
            'The {adjective} Spire'
        ],
        descriptions: [
            'A landmark visible for miles around. A meeting point for travelers.',
            'This monument has guided travelers for generations.',
            'Local legends say those who visit receive good fortune.'
        ],
        levelRange: [1, 3],
        radiusRange: [20, 50],
        resources: [],
        bonuses: [
            { type: 'map_reveal', value: 500, radius: 500 }
        ],
        discoveryXp: 25
    },
    [SeedPointType.NPC_TERRITORY]: {
        type: SeedPointType.NPC_TERRITORY,
        nameTemplates: [
            'Bandit Camp',
            'Goblin Warren',
            'Shadow Enclave',
            'Corrupted Outpost',
            'Wild Territory'
        ],
        descriptions: [
            'A territory claimed by hostile NPCs. Defeat them to claim it!',
            'Enemy forces have established a foothold here.',
            'Clear this territory to expand your influence.'
        ],
        levelRange: [3, 15],
        radiusRange: [100, 300],
        resources: [
            { type: ResourceType.IRON, minAmount: 50, maxAmount: 200, probability: 0.5 },
            { type: ResourceType.RARE_MATERIAL, minAmount: 10, maxAmount: 50, probability: 0.3 }
        ],
        bonuses: [],
        discoveryXp: 200
    },
    [SeedPointType.MYSTERY_LOCATION]: {
        type: SeedPointType.MYSTERY_LOCATION,
        nameTemplates: [
            '???',
            'Unknown Location',
            'Mysterious Signal',
            'Strange Anomaly',
            'Hidden {noun}'
        ],
        descriptions: [
            'Something unusual is happening here. Investigate to uncover its secrets.',
            'Your sensors detect an anomaly at this location.',
            'A mystery awaits those brave enough to explore.'
        ],
        levelRange: [5, 12],
        radiusRange: [30, 100],
        resources: [
            { type: ResourceType.ANCIENT_ARTIFACT, minAmount: 1, maxAmount: 5, probability: 0.6 },
            { type: ResourceType.RARE_MATERIAL, minAmount: 20, maxAmount: 100, probability: 0.4 }
        ],
        bonuses: [],
        discoveryXp: 300
    },
    [SeedPointType.HISTORICAL_SITE]: {
        type: SeedPointType.HISTORICAL_SITE,
        nameTemplates: [
            'Historic {noun} Site',
            '{adjective} Memorial',
            'Heritage {noun}',
            'Cultural {adjective} Center',
            '{noun} Historical Marker'
        ],
        descriptions: [
            'A place of historical significance. Learn about the past here.',
            'This location commemorates important events.',
            'History buffs will appreciate the stories this place holds.'
        ],
        levelRange: [1, 4],
        radiusRange: [30, 80],
        resources: [],
        bonuses: [
            { type: 'knowledge_boost', value: 1.3, durationHours: 1 }
        ],
        discoveryXp: 40
    }
};

// Name generation words
const ADJECTIVES = [
    'Ancient', 'Forgotten', 'Lost', 'Hidden', 'Mystic', 'Sacred', 'Dark',
    'Golden', 'Silver', 'Crystal', 'Shadow', 'Eternal', 'Cursed', 'Blessed',
    'Ruined', 'Abandoned', 'Haunted', 'Glorious', 'Silent', 'Whispering'
];

const NOUNS = [
    'Stone', 'Moon', 'Sun', 'Star', 'Dragon', 'Phoenix', 'Serpent',
    'King', 'Queen', 'Elder', 'Spirit', 'Guardian', 'Sentinel', 'Watcher',
    'Oak', 'Willow', 'Mountain', 'River', 'Storm', 'Thunder'
];

// ============================================================================
// Seed Generation Functions
// ============================================================================

/**
 * Generate a random seed point from a template
 */
function generateSeedPoint(
    template: SeedTemplate,
    lat: number,
    lon: number,
    existingIds: Set<string>
): SeedPoint {
    // Generate unique ID
    let id: string;
    do {
        id = `seed_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    } while (existingIds.has(id));

    // Generate name
    const nameTemplate = template.nameTemplates[Math.floor(Math.random() * template.nameTemplates.length)];
    const name = nameTemplate
        .replace('{adjective}', ADJECTIVES[Math.floor(Math.random() * ADJECTIVES.length)])
        .replace('{noun}', NOUNS[Math.floor(Math.random() * NOUNS.length)]);

    // Generate description
    const description = template.descriptions[Math.floor(Math.random() * template.descriptions.length)];

    // Generate level
    const level = Math.floor(
        Math.random() * (template.levelRange[1] - template.levelRange[0] + 1) + template.levelRange[0]
    );

    // Generate radius
    const radius = Math.floor(
        Math.random() * (template.radiusRange[1] - template.radiusRange[0] + 1) + template.radiusRange[0]
    );

    // Add some randomness to position (within 100m)
    const latOffset = (Math.random() - 0.5) * 0.002;  // ~100m
    const lonOffset = (Math.random() - 0.5) * 0.002;

    return {
        id,
        type: template.type,
        name,
        description,
        latitude: lat + latOffset,
        longitude: lon + lonOffset,
        radius,
        level,
        resources: template.resources,
        bonuses: template.bonuses,
        discoveryReward: {
            xp: template.discoveryXp * level
        },
        respawnHours: template.type === SeedPointType.RESOURCE_HOTSPOT ? 24 : undefined,
        createdAt: admin.firestore.Timestamp.now()
    };
}

/**
 * Generate a grid of seed points for an area
 */
function generateSeedGrid(
    centerLat: number,
    centerLon: number,
    radiusKm: number,
    density: number = 0.5  // Points per km²
): { lat: number; lon: number }[] {
    const points: { lat: number; lon: number }[] = [];

    // Convert km to degrees (approximate)
    const latDegPerKm = 1 / 111;
    const lonDegPerKm = 1 / (111 * Math.cos(centerLat * Math.PI / 180));

    const gridSizeKm = 1 / Math.sqrt(density);
    const gridSizeLat = gridSizeKm * latDegPerKm;
    const gridSizeLon = gridSizeKm * lonDegPerKm;

    const latSteps = Math.ceil(radiusKm * 2 / gridSizeKm);
    const lonSteps = Math.ceil(radiusKm * 2 / gridSizeKm);

    for (let i = -latSteps / 2; i <= latSteps / 2; i++) {
        for (let j = -lonSteps / 2; j <= lonSteps / 2; j++) {
            const lat = centerLat + i * gridSizeLat;
            const lon = centerLon + j * gridSizeLon;

            // Check if within radius
            const distKm = haversineDistance(centerLat, centerLon, lat, lon);
            if (distKm <= radiusKm) {
                // Add some randomness (up to half grid size)
                const jitterLat = (Math.random() - 0.5) * gridSizeLat * 0.8;
                const jitterLon = (Math.random() - 0.5) * gridSizeLon * 0.8;
                points.push({
                    lat: lat + jitterLat,
                    lon: lon + jitterLon
                });
            }
        }
    }

    return points;
}

/**
 * Calculate haversine distance between two points in km
 */
function haversineDistance(lat1: number, lon1: number, lat2: number, lon2: number): number {
    const R = 6371; // Earth's radius in km
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLon = (lon2 - lon1) * Math.PI / 180;
    const a =
        Math.sin(dLat / 2) * Math.sin(dLat / 2) +
        Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
        Math.sin(dLon / 2) * Math.sin(dLon / 2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    return R * c;
}

/**
 * Select a seed type based on weighted probabilities
 */
function selectSeedType(): SeedPointType {
    const weights: Record<SeedPointType, number> = {
        [SeedPointType.RESOURCE_HOTSPOT]: 30,
        [SeedPointType.LANDMARK]: 20,
        [SeedPointType.HISTORICAL_SITE]: 15,
        [SeedPointType.ANCIENT_RUINS]: 12,
        [SeedPointType.SACRED_SITE]: 10,
        [SeedPointType.NPC_TERRITORY]: 8,
        [SeedPointType.ABANDONED_FORT]: 3,
        [SeedPointType.MYSTERY_LOCATION]: 2
    };

    const totalWeight = Object.values(weights).reduce((a, b) => a + b, 0);
    let random = Math.random() * totalWeight;

    for (const [type, weight] of Object.entries(weights)) {
        random -= weight;
        if (random <= 0) {
            return type as SeedPointType;
        }
    }

    return SeedPointType.RESOURCE_HOTSPOT;
}

// ============================================================================
// Cloud Functions
// ============================================================================

/**
 * Seed a new region with content
 */
export const seedRegion = functions.https.onCall(async (data, context) => {
    // Admin only
    if (!context.auth) {
        throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
    }

    const adminDoc = await db.collection('admins').doc(context.auth.uid).get();
    if (!adminDoc.exists) {
        throw new functions.https.HttpsError('permission-denied', 'Admin access required');
    }

    const { centerLat, centerLon, radiusKm, density, regionName } = data;

    if (!centerLat || !centerLon || !radiusKm) {
        throw new functions.https.HttpsError('invalid-argument', 'Missing required parameters');
    }

    // Check if region already seeded
    const regionId = `region_${Math.round(centerLat * 100)}_${Math.round(centerLon * 100)}`;
    const regionRef = db.collection('seedRegions').doc(regionId);
    const existingRegion = await regionRef.get();

    if (existingRegion.exists && existingRegion.data()?.seeded) {
        throw new functions.https.HttpsError('already-exists', 'Region already seeded');
    }

    // Generate seed points
    const gridPoints = generateSeedGrid(centerLat, centerLon, radiusKm, density || 0.5);
    const existingIds = new Set<string>();
    const seedPoints: SeedPoint[] = [];

    for (const point of gridPoints) {
        const seedType = selectSeedType();
        const template = SEED_TEMPLATES[seedType];
        const seedPoint = generateSeedPoint(template, point.lat, point.lon, existingIds);
        existingIds.add(seedPoint.id);
        seedPoints.push(seedPoint);
    }

    // Batch write seed points
    const batch = db.batch();

    for (const seed of seedPoints) {
        const seedRef = db.collection('seedPoints').doc(seed.id);
        batch.set(seedRef, seed);
    }

    // Update region record
    batch.set(regionRef, {
        id: regionId,
        name: regionName || `Region ${centerLat.toFixed(2)}, ${centerLon.toFixed(2)}`,
        centerLat,
        centerLon,
        radiusKm,
        seeded: true,
        seedCount: seedPoints.length,
        lastSeeded: admin.firestore.FieldValue.serverTimestamp()
    });

    await batch.commit();

    return {
        success: true,
        regionId,
        seedCount: seedPoints.length,
        types: seedPoints.reduce((acc, s) => {
            acc[s.type] = (acc[s.type] || 0) + 1;
            return acc;
        }, {} as Record<string, number>)
    };
});

/**
 * Get seed points near a location
 */
export const getNearbySeeds = functions.https.onCall(async (data, context) => {
    if (!context.auth) {
        throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
    }

    const { latitude, longitude, radiusMeters } = data;

    if (!latitude || !longitude) {
        throw new functions.https.HttpsError('invalid-argument', 'Missing location');
    }

    const radiusKm = (radiusMeters || 1000) / 1000;

    // Query with bounding box (Firestore doesn't support geo queries natively)
    const latDelta = radiusKm / 111;
    const lonDelta = radiusKm / (111 * Math.cos(latitude * Math.PI / 180));

    const seedsSnapshot = await db.collection('seedPoints')
        .where('latitude', '>=', latitude - latDelta)
        .where('latitude', '<=', latitude + latDelta)
        .get();

    // Filter by longitude and exact distance
    const nearbySeeds: SeedPoint[] = [];

    seedsSnapshot.forEach(doc => {
        const seed = doc.data() as SeedPoint;
        if (seed.longitude >= longitude - lonDelta && seed.longitude <= longitude + lonDelta) {
            const distance = haversineDistance(latitude, longitude, seed.latitude, seed.longitude);
            if (distance <= radiusKm) {
                nearbySeeds.push(seed);
            }
        }
    });

    // Get player's discovered seeds
    const discoveredSnapshot = await db.collection('players').doc(context.auth.uid)
        .collection('discoveries').get();
    
    const discoveredIds = new Set(discoveredSnapshot.docs.map(d => d.id));

    return {
        seeds: nearbySeeds.map(seed => ({
            ...seed,
            discovered: discoveredIds.has(seed.id)
        }))
    };
});

/**
 * Discover a seed point (claim first-time discovery reward)
 */
export const discoverSeed = functions.https.onCall(async (data, context) => {
    if (!context.auth) {
        throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
    }

    const { seedId, latitude, longitude } = data;

    if (!seedId || !latitude || !longitude) {
        throw new functions.https.HttpsError('invalid-argument', 'Missing required parameters');
    }

    // Get seed point
    const seedRef = db.collection('seedPoints').doc(seedId);
    const seedDoc = await seedRef.get();

    if (!seedDoc.exists) {
        throw new functions.https.HttpsError('not-found', 'Seed point not found');
    }

    const seed = seedDoc.data() as SeedPoint;

    // Verify player is close enough
    const distance = haversineDistance(latitude, longitude, seed.latitude, seed.longitude);
    if (distance > 0.1) {  // 100m
        throw new functions.https.HttpsError('failed-precondition', 'Too far from seed point');
    }

    // Check if already discovered
    const discoveryRef = db.collection('players').doc(context.auth.uid)
        .collection('discoveries').doc(seedId);
    const existingDiscovery = await discoveryRef.get();

    if (existingDiscovery.exists) {
        throw new functions.https.HttpsError('already-exists', 'Already discovered');
    }

    // Award discovery
    const rewards: any = {
        xp: seed.discoveryReward?.xp || 10
    };

    const batch = db.batch();

    // Record discovery
    batch.set(discoveryRef, {
        seedId,
        type: seed.type,
        name: seed.name,
        discoveredAt: admin.firestore.FieldValue.serverTimestamp()
    });

    // Award XP
    const playerRef = db.collection('players').doc(context.auth.uid);
    batch.update(playerRef, {
        xp: admin.firestore.FieldValue.increment(rewards.xp),
        totalDiscoveries: admin.firestore.FieldValue.increment(1)
    });

    // Award resources if any
    if (seed.discoveryReward?.resources) {
        rewards.resources = seed.discoveryReward.resources;
        for (const resource of seed.discoveryReward.resources) {
            batch.update(playerRef, {
                [`resources.${resource.type}`]: admin.firestore.FieldValue.increment(resource.amount)
            });
        }
    }

    // Check for discovery achievement
    batch.set(db.collection('achievementChecks').doc(), {
        playerId: context.auth.uid,
        type: 'discovery',
        seedType: seed.type,
        timestamp: admin.firestore.FieldValue.serverTimestamp()
    });

    await batch.commit();

    return {
        success: true,
        rewards,
        seed: {
            id: seed.id,
            name: seed.name,
            type: seed.type,
            description: seed.description
        }
    };
});

/**
 * Claim resources from a resource hotspot
 */
export const claimSeedResources = functions.https.onCall(async (data, context) => {
    if (!context.auth) {
        throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
    }

    const { seedId, latitude, longitude } = data;

    if (!seedId || !latitude || !longitude) {
        throw new functions.https.HttpsError('invalid-argument', 'Missing required parameters');
    }

    // Get seed point
    const seedRef = db.collection('seedPoints').doc(seedId);
    const seedDoc = await seedRef.get();

    if (!seedDoc.exists) {
        throw new functions.https.HttpsError('not-found', 'Seed point not found');
    }

    const seed = seedDoc.data() as SeedPoint;

    // Verify it's a resource type
    if (!seed.resources || seed.resources.length === 0) {
        throw new functions.https.HttpsError('failed-precondition', 'No resources at this location');
    }

    // Verify player is close enough
    const distance = haversineDistance(latitude, longitude, seed.latitude, seed.longitude);
    if (distance > 0.1) {  // 100m
        throw new functions.https.HttpsError('failed-precondition', 'Too far from seed point');
    }

    // Check respawn timer
    if (seed.lastClaimed && seed.respawnHours) {
        const lastClaimedTime = seed.lastClaimed.toDate().getTime();
        const cooldownMs = seed.respawnHours * 60 * 60 * 1000;
        const now = Date.now();

        if (now - lastClaimedTime < cooldownMs) {
            const remainingMs = cooldownMs - (now - lastClaimedTime);
            const remainingHours = Math.ceil(remainingMs / (60 * 60 * 1000));
            throw new functions.https.HttpsError(
                'failed-precondition',
                `Resources respawn in ${remainingHours} hours`
            );
        }
    }

    // Calculate rewards
    const rewards: { type: ResourceType; amount: number }[] = [];

    for (const resource of seed.resources) {
        if (Math.random() < resource.probability) {
            const amount = Math.floor(
                Math.random() * (resource.maxAmount - resource.minAmount + 1) + resource.minAmount
            );
            rewards.push({ type: resource.type, amount });
        }
    }

    if (rewards.length === 0) {
        // Guarantee at least one resource
        const fallbackResource = seed.resources[0];
        rewards.push({
            type: fallbackResource.type,
            amount: fallbackResource.minAmount
        });
    }

    // Update database
    const batch = db.batch();

    // Update player resources
    const playerRef = db.collection('players').doc(context.auth.uid);
    for (const reward of rewards) {
        batch.update(playerRef, {
            [`resources.${reward.type}`]: admin.firestore.FieldValue.increment(reward.amount)
        });
    }

    // Update seed last claimed time
    batch.update(seedRef, {
        lastClaimed: admin.firestore.FieldValue.serverTimestamp()
    });

    // Record claim
    batch.set(db.collection('resourceClaims').doc(), {
        playerId: context.auth.uid,
        seedId,
        resources: rewards,
        timestamp: admin.firestore.FieldValue.serverTimestamp()
    });

    await batch.commit();

    return {
        success: true,
        rewards,
        nextClaimIn: seed.respawnHours || 24
    };
});

/**
 * Get all seeded regions (admin)
 */
export const getSeededRegions = functions.https.onCall(async (data, context) => {
    if (!context.auth) {
        throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
    }

    const adminDoc = await db.collection('admins').doc(context.auth.uid).get();
    if (!adminDoc.exists) {
        throw new functions.https.HttpsError('permission-denied', 'Admin access required');
    }

    const regionsSnapshot = await db.collection('seedRegions').get();

    const regions: SeedRegion[] = [];
    regionsSnapshot.forEach(doc => {
        regions.push(doc.data() as SeedRegion);
    });

    return { regions };
});

/**
 * Delete seeds in a region (admin)
 */
export const clearRegionSeeds = functions.https.onCall(async (data, context) => {
    if (!context.auth) {
        throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
    }

    const adminDoc = await db.collection('admins').doc(context.auth.uid).get();
    if (!adminDoc.exists) {
        throw new functions.https.HttpsError('permission-denied', 'Admin access required');
    }

    const { regionId } = data;

    if (!regionId) {
        throw new functions.https.HttpsError('invalid-argument', 'Missing regionId');
    }

    // Get region
    const regionRef = db.collection('seedRegions').doc(regionId);
    const regionDoc = await regionRef.get();

    if (!regionDoc.exists) {
        throw new functions.https.HttpsError('not-found', 'Region not found');
    }

    const region = regionDoc.data() as SeedRegion;

    // Query seeds in region
    const latDelta = region.radiusKm / 111;
    const lonDelta = region.radiusKm / (111 * Math.cos(region.centerLat * Math.PI / 180));

    const seedsSnapshot = await db.collection('seedPoints')
        .where('latitude', '>=', region.centerLat - latDelta)
        .where('latitude', '<=', region.centerLat + latDelta)
        .get();

    // Delete in batches
    let deleteCount = 0;
    let batch = db.batch();
    let batchCount = 0;

    for (const doc of seedsSnapshot.docs) {
        const seed = doc.data() as SeedPoint;
        if (seed.longitude >= region.centerLon - lonDelta && seed.longitude <= region.centerLon + lonDelta) {
            batch.delete(doc.ref);
            deleteCount++;
            batchCount++;

            if (batchCount >= 500) {
                await batch.commit();
                batch = db.batch();
                batchCount = 0;
            }
        }
    }

    // Commit remaining
    if (batchCount > 0) {
        await batch.commit();
    }

    // Update region
    await regionRef.update({
        seeded: false,
        seedCount: 0
    });

    return {
        success: true,
        deletedCount: deleteCount
    };
});

/**
 * Import POI data from external source (admin)
 */
export const importPOIData = functions.https.onCall(async (data, context) => {
    if (!context.auth) {
        throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
    }

    const adminDoc = await db.collection('admins').doc(context.auth.uid).get();
    if (!adminDoc.exists) {
        throw new functions.https.HttpsError('permission-denied', 'Admin access required');
    }

    const { pois } = data;

    if (!pois || !Array.isArray(pois)) {
        throw new functions.https.HttpsError('invalid-argument', 'Missing POI array');
    }

    const batch = db.batch();
    const existingIds = new Set<string>();
    let importCount = 0;

    for (const poi of pois) {
        if (!poi.latitude || !poi.longitude || !poi.name) {
            continue;
        }

        // Map POI category to seed type
        let seedType = SeedPointType.LANDMARK;
        if (poi.category) {
            const category = poi.category.toLowerCase();
            if (category.includes('historic') || category.includes('museum')) {
                seedType = SeedPointType.HISTORICAL_SITE;
            } else if (category.includes('park') || category.includes('nature')) {
                seedType = SeedPointType.SACRED_SITE;
            } else if (category.includes('ruin') || category.includes('castle')) {
                seedType = SeedPointType.ANCIENT_RUINS;
            } else if (category.includes('military') || category.includes('fort')) {
                seedType = SeedPointType.ABANDONED_FORT;
            }
        }

        const template = SEED_TEMPLATES[seedType];
        const seed: SeedPoint = {
            id: `poi_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
            type: seedType,
            name: poi.name,
            description: poi.description || template.descriptions[0],
            latitude: poi.latitude,
            longitude: poi.longitude,
            radius: poi.radius || 50,
            level: Math.floor(Math.random() * 5) + 1,
            resources: template.resources,
            bonuses: template.bonuses,
            discoveryReward: {
                xp: template.discoveryXp
            },
            createdAt: admin.firestore.Timestamp.now(),
            metadata: {
                importedFrom: poi.source || 'external',
                originalId: poi.id
            }
        };

        existingIds.add(seed.id);
        batch.set(db.collection('seedPoints').doc(seed.id), seed);
        importCount++;

        if (importCount % 500 === 0) {
            await batch.commit();
        }
    }

    if (importCount % 500 !== 0) {
        await batch.commit();
    }

    return {
        success: true,
        importedCount: importCount
    };
});
