/**
 * Seed data for Firebase Emulator testing
 * Run with: npm run seed (from backend/ directory)
 */

import * as admin from 'firebase-admin';

// Connect to emulator
process.env.FIRESTORE_EMULATOR_HOST = 'localhost:8080';
process.env.FIREBASE_AUTH_EMULATOR_HOST = 'localhost:9099';

admin.initializeApp({
  projectId: 'apex-citadels-dev'
});

const db = admin.firestore();
const auth = admin.auth();

interface SeedUser {
  uid: string;
  email: string;
  displayName: string;
  level: number;
  xp: number;
  stone: number;
  wood: number;
  metal: number;
  crystal: number;
  gems: number;
}

const seedUsers: SeedUser[] = [
  {
    uid: 'test-user-1',
    email: 'player1@test.com',
    displayName: 'TestPlayer1',
    level: 10,
    xp: 5000,
    stone: 500,
    wood: 500,
    metal: 200,
    crystal: 50,
    gems: 100
  },
  {
    uid: 'test-user-2',
    email: 'player2@test.com',
    displayName: 'RivalPlayer',
    level: 8,
    xp: 3500,
    stone: 400,
    wood: 400,
    metal: 150,
    crystal: 30,
    gems: 50
  },
  {
    uid: 'test-user-3',
    email: 'alliance@test.com',
    displayName: 'AllianceMember',
    level: 5,
    xp: 1500,
    stone: 200,
    wood: 200,
    metal: 50,
    crystal: 10,
    gems: 25
  }
];

const seedTerritories = [
  {
    id: 'territory-sf-downtown',
    name: 'SF Downtown',
    latitude: 37.7749,
    longitude: -122.4194,
    radius: 100,
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
    radius: 100,
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
    radius: 100,
    level: 1,
    ownerId: null,
    ownerName: null,
    claimedAt: null,
    health: 100,
    maxHealth: 100
  }
];

const seedResourceNodes = [
  {
    id: 'node-stone-1',
    type: 'StoneMine',
    name: 'Stone Quarry',
    latitude: 37.7750,
    longitude: -122.4180,
    currentAmount: 500,
    maxAmount: 500,
    lastHarvestedAt: null
  },
  {
    id: 'node-wood-1',
    type: 'Forest',
    name: 'Golden Gate Park Forest',
    latitude: 37.7694,
    longitude: -122.4862,
    currentAmount: 400,
    maxAmount: 400,
    lastHarvestedAt: null
  },
  {
    id: 'node-metal-1',
    type: 'OreDeposit',
    name: 'Industrial Ore',
    latitude: 37.7850,
    longitude: -122.4000,
    currentAmount: 300,
    maxAmount: 300,
    lastHarvestedAt: null
  },
  {
    id: 'node-crystal-1',
    type: 'CrystalCave',
    name: 'Hidden Crystal Cave',
    latitude: 37.7900,
    longitude: -122.4100,
    currentAmount: 100,
    maxAmount: 100,
    lastHarvestedAt: null
  }
];

const seedAlliance = {
  id: 'alliance-test-1',
  name: 'Test Alliance',
  tag: 'TEST',
  description: 'A test alliance for development',
  leaderId: 'test-user-1',
  leaderName: 'TestPlayer1',
  createdAt: admin.firestore.Timestamp.now(),
  members: [
    {
      playerId: 'test-user-1',
      playerName: 'TestPlayer1',
      role: 'Leader',
      joinedAt: admin.firestore.Timestamp.now(),
      contributedXP: 1000
    },
    {
      playerId: 'test-user-3',
      playerName: 'AllianceMember',
      role: 'Member',
      joinedAt: admin.firestore.Timestamp.now(),
      contributedXP: 200
    }
  ],
  memberIds: ['test-user-1', 'test-user-3'],
  maxMembers: 50,
  totalTerritories: 1,
  totalAttacksWon: 5,
  totalDefensesWon: 3,
  weeklyXP: 500,
  allTimeXP: 2500,
  isOpen: true,
  minLevelToJoin: 3
};

const seedBuildings = [
  {
    id: 'building-wall-1',
    type: 'Wall',
    ownerId: 'test-user-1',
    territoryId: 'territory-sf-downtown',
    latitude: 37.7749,
    longitude: -122.4194,
    altitude: 0,
    localPosition: { x: 1, y: 0, z: 0 },
    localRotation: { x: 0, y: 0, z: 0, w: 1 },
    localScale: { x: 1, y: 1, z: 1 },
    health: 200,
    maxHealth: 200,
    placedAt: admin.firestore.Timestamp.now()
  },
  {
    id: 'building-tower-1',
    type: 'Tower',
    ownerId: 'test-user-1',
    territoryId: 'territory-sf-downtown',
    latitude: 37.7750,
    longitude: -122.4195,
    altitude: 0,
    localPosition: { x: 0, y: 0, z: 2 },
    localRotation: { x: 0, y: 0, z: 0, w: 1 },
    localScale: { x: 1, y: 1, z: 1 },
    health: 300,
    maxHealth: 300,
    placedAt: admin.firestore.Timestamp.now()
  }
];

async function seedDatabase() {
  console.log('🌱 Starting database seed...\n');

  // Create auth users
  console.log('👤 Creating test users...');
  for (const user of seedUsers) {
    try {
      await auth.createUser({
        uid: user.uid,
        email: user.email,
        displayName: user.displayName,
        password: 'testpass123'
      });
      console.log(`   ✓ Created auth user: ${user.email}`);
    } catch (e: any) {
      if (e.code === 'auth/uid-already-exists') {
        console.log(`   ⚠ User already exists: ${user.email}`);
      } else {
        console.log(`   ✗ Failed: ${e.message}`);
      }
    }

    // Create Firestore user profile
    await db.collection('users').doc(user.uid).set({
      playerId: user.uid,
      email: user.email,
      displayName: user.displayName,
      level: user.level,
      xp: user.xp,
      stone: user.stone,
      wood: user.wood,
      metal: user.metal,
      crystal: user.crystal,
      gems: user.gems,
      createdAt: admin.firestore.Timestamp.now(),
      lastLoginAt: admin.firestore.Timestamp.now(),
      stats: {
        territoriesClaimed: 1,
        battlesWon: 5,
        battlesLost: 2,
        blocksPlaced: 10
      }
    });
    console.log(`   ✓ Created profile: ${user.displayName}`);
  }

  // Create territories
  console.log('\n🏰 Creating territories...');
  for (const territory of seedTerritories) {
    await db.collection('territories').doc(territory.id).set(territory);
    console.log(`   ✓ Created territory: ${territory.name}`);
  }

  // Create resource nodes
  console.log('\n💎 Creating resource nodes...');
  for (const node of seedResourceNodes) {
    await db.collection('resource_nodes').doc(node.id).set(node);
    console.log(`   ✓ Created node: ${node.name}`);
  }

  // Create alliance
  console.log('\n⚔️ Creating alliance...');
  await db.collection('alliances').doc(seedAlliance.id).set(seedAlliance);
  console.log(`   ✓ Created alliance: ${seedAlliance.name}`);

  // Update user alliance reference
  await db.collection('users').doc('test-user-1').update({
    allianceId: seedAlliance.id
  });
  await db.collection('users').doc('test-user-3').update({
    allianceId: seedAlliance.id
  });

  // Create buildings
  console.log('\n🧱 Creating buildings...');
  for (const building of seedBuildings) {
    await db.collection('buildings').doc(building.id).set(building);
    console.log(`   ✓ Created building: ${building.type}`);
  }

  // Create some daily metrics for dashboard
  console.log('\n📊 Creating daily metrics...');
  const today = new Date().toISOString().split('T')[0];
  await db.collection('daily_metrics').doc(today).set({
    date: today,
    activeUsers: 3,
    newUsers: 1,
    territoriesClaimed: 2,
    battlesStarted: 5,
    revenue: 0,
    topPlayers: ['test-user-1', 'test-user-2']
  });
  console.log(`   ✓ Created metrics for ${today}`);

  console.log('\n✅ Seed complete!\n');
  console.log('Test credentials:');
  console.log('  Email: player1@test.com');
  console.log('  Password: testpass123');
  console.log('\nEmulator UI: http://localhost:4000');
  
  process.exit(0);
}

seedDatabase().catch((error) => {
  console.error('Seed failed:', error);
  process.exit(1);
});
