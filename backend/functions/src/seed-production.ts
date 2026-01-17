/**
 * Seed data for Firebase PRODUCTION
 * Run with: npx ts-node src/seed-production.ts (from backend/functions directory)
 * 
 * WARNING: This writes to production Firestore!
 */

import * as admin from 'firebase-admin';

// Initialize with default credentials (uses GOOGLE_APPLICATION_CREDENTIALS or gcloud auth)
admin.initializeApp({
  projectId: 'apex-citadels-dev'
});

const db = admin.firestore();

const seedTerritories = [
  // === San Francisco Test Area ===
  {
    id: 'territory-sf-downtown',
    name: 'SF Downtown',
    latitude: 37.7749,
    longitude: -122.4194,
    centerLatitude: 37.7749,
    centerLongitude: -122.4194,
    radius: 100,
    radiusMeters: 100,
    level: 3,
    ownerId: 'test-user-1',
    ownerName: 'TestPlayer1',
    claimedAt: admin.firestore.Timestamp.now(),
    health: 100,
    maxHealth: 100
  },
  {
    id: 'territory-sf-embarcadero',
    name: 'Embarcadero',
    latitude: 37.7955,
    longitude: -122.3937,
    centerLatitude: 37.7955,
    centerLongitude: -122.3937,
    radius: 100,
    radiusMeters: 100,
    level: 2,
    ownerId: 'test-user-2',
    ownerName: 'RivalPlayer',
    claimedAt: admin.firestore.Timestamp.now(),
    health: 80,
    maxHealth: 100
  },
  {
    id: 'territory-sf-mission',
    name: 'Mission District',
    latitude: 37.7599,
    longitude: -122.4148,
    centerLatitude: 37.7599,
    centerLongitude: -122.4148,
    radius: 100,
    radiusMeters: 100,
    level: 1,
    ownerId: null,
    ownerName: null,
    claimedAt: null,
    health: 100,
    maxHealth: 100
  },
  
  // === Vienna, VA Test Area ===
  {
    id: 'territory-vienna-downtown',
    name: 'Vienna Town Green',
    latitude: 38.9012,
    longitude: -77.2653,
    centerLatitude: 38.9012,
    centerLongitude: -77.2653,
    radius: 100,
    radiusMeters: 100,
    level: 3,
    ownerId: 'test-user-1',
    ownerName: 'TestPlayer1',
    claimedAt: admin.firestore.Timestamp.now(),
    health: 100,
    maxHealth: 100
  },
  {
    id: 'territory-vienna-metro',
    name: 'Vienna Metro Station',
    latitude: 38.8779,
    longitude: -77.2711,
    centerLatitude: 38.8779,
    centerLongitude: -77.2711,
    radius: 100,
    radiusMeters: 100,
    level: 2,
    ownerId: 'test-user-2',
    ownerName: 'RivalPlayer',
    claimedAt: admin.firestore.Timestamp.now(),
    health: 90,
    maxHealth: 100
  },
  {
    id: 'territory-vienna-maple',
    name: 'Maple Avenue',
    latitude: 38.9001,
    longitude: -77.2545,
    centerLatitude: 38.9001,
    centerLongitude: -77.2545,
    radius: 100,
    radiusMeters: 100,
    level: 2,
    ownerId: null,
    ownerName: null,
    claimedAt: null,
    health: 100,
    maxHealth: 100
  },
  {
    id: 'territory-tysons',
    name: 'Tysons Corner',
    latitude: 38.9187,
    longitude: -77.2311,
    centerLatitude: 38.9187,
    centerLongitude: -77.2311,
    radius: 150,
    radiusMeters: 150,
    level: 4,
    ownerId: 'test-user-2',
    ownerName: 'RivalPlayer',
    claimedAt: admin.firestore.Timestamp.now(),
    health: 100,
    maxHealth: 100
  },
  {
    id: 'territory-wolftrap',
    name: 'Wolf Trap',
    latitude: 38.9378,
    longitude: -77.2656,
    centerLatitude: 38.9378,
    centerLongitude: -77.2656,
    radius: 120,
    radiusMeters: 120,
    level: 2,
    ownerId: null,
    ownerName: null,
    claimedAt: null,
    health: 100,
    maxHealth: 100
  },
  {
    id: 'territory-meadowlark',
    name: 'Meadowlark Gardens',
    latitude: 38.9394,
    longitude: -77.2803,
    centerLatitude: 38.9394,
    centerLongitude: -77.2803,
    radius: 100,
    radiusMeters: 100,
    level: 1,
    ownerId: 'test-user-3',
    ownerName: 'AllianceMember',
    claimedAt: admin.firestore.Timestamp.now(),
    health: 100,
    maxHealth: 100
  },
  {
    id: 'territory-oakton',
    name: 'Oakton',
    latitude: 38.8809,
    longitude: -77.3006,
    centerLatitude: 38.8809,
    centerLongitude: -77.3006,
    radius: 100,
    radiusMeters: 100,
    level: 1,
    ownerId: null,
    ownerName: null,
    claimedAt: null,
    health: 100,
    maxHealth: 100
  }
];

async function seedProduction() {
  console.log('🌱 Seeding PRODUCTION Firestore...\n');
  console.log('⚠️  Project: apex-citadels-dev\n');

  // Create territories
  console.log('🏰 Creating territories...');
  for (const territory of seedTerritories) {
    try {
      await db.collection('territories').doc(territory.id).set(territory);
      console.log(`   ✓ ${territory.name} (${territory.latitude}, ${territory.longitude})`);
    } catch (e: any) {
      console.log(`   ✗ Failed ${territory.name}: ${e.message}`);
    }
  }

  console.log('\n✅ Production seed complete!');
  console.log(`   Total territories: ${seedTerritories.length}`);
  console.log('   - 3 in San Francisco');
  console.log('   - 7 in Vienna, VA area');
  
  process.exit(0);
}

seedProduction().catch(console.error);
