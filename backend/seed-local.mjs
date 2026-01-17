#!/usr/bin/env node
/**
 * Local seeding script using Firebase Admin SDK
 * 
 * SETUP:
 * 1. Go to Firebase Console → Project Settings → Service Accounts
 * 2. Click "Generate new private key" 
 * 3. Save the file as: backend/serviceAccountKey.json
 * 4. Run: node seed-local.mjs
 * 
 * The serviceAccountKey.json is in .gitignore so it won't be committed.
 */

import { initializeApp, cert } from 'firebase-admin/app';
import { getFirestore } from 'firebase-admin/firestore';
import { readFileSync, existsSync } from 'fs';

// Check for service account key
const keyPath = './serviceAccountKey.json';
if (!existsSync(keyPath)) {
  console.error('❌ Service account key not found!');
  console.error('');
  console.error('To get the key:');
  console.error('1. Go to: https://console.firebase.google.com/project/apex-citadels-dev/settings/serviceaccounts/adminsdk');
  console.error('2. Click "Generate new private key"');
  console.error('3. Save the downloaded file as: backend/serviceAccountKey.json');
  console.error('4. Run this script again: node seed-local.mjs');
  process.exit(1);
}

// Initialize Firebase Admin
const serviceAccount = JSON.parse(readFileSync(keyPath, 'utf8'));
initializeApp({
  credential: cert(serviceAccount),
  projectId: 'apex-citadels-dev'
});

const db = getFirestore();

// Simple geohash encoder (base32)
function encodeGeohash(lat, lng, precision = 9) {
  const base32 = '0123456789bcdefghjkmnpqrstuvwxyz';
  let minLat = -90, maxLat = 90;
  let minLng = -180, maxLng = 180;
  let hash = '';
  let isEven = true;
  let bit = 0;
  let ch = 0;

  while (hash.length < precision) {
    if (isEven) {
      const mid = (minLng + maxLng) / 2;
      if (lng > mid) {
        ch |= (1 << (4 - bit));
        minLng = mid;
      } else {
        maxLng = mid;
      }
    } else {
      const mid = (minLat + maxLat) / 2;
      if (lat > mid) {
        ch |= (1 << (4 - bit));
        minLat = mid;
      } else {
        maxLat = mid;
      }
    }
    isEven = !isEven;
    if (bit < 4) {
      bit++;
    } else {
      hash += base32[ch];
      bit = 0;
      ch = 0;
    }
  }
  return hash;
}

// Vienna, VA Territories (near user's location)
const viennaVaTerritories = [
  {
    id: 'vienna-town-green',
    name: 'Vienna Town Green',
    latitude: 38.9012,
    longitude: -77.2653,
    geohash: encodeGeohash(38.9012, -77.2653),
    region: 'vienna-va',
    resourceType: 'gold',
    resourceOutput: 100,
    controlledBy: null,
    controllerName: 'Unclaimed',
    level: 1,
    totalBattles: 0,
    lastConquered: null,
    createdAt: new Date()
  },
  {
    id: 'freeman-house',
    name: 'Freeman House',
    latitude: 38.9018,
    longitude: -77.2639,
    geohash: encodeGeohash(38.9018, -77.2639),
    region: 'vienna-va',
    resourceType: 'wood',
    resourceOutput: 80,
    controlledBy: null,
    controllerName: 'Unclaimed',
    level: 1,
    totalBattles: 0,
    lastConquered: null,
    createdAt: new Date()
  },
  {
    id: 'vienna-presbyterian',
    name: 'Vienna Presbyterian',
    latitude: 38.8998,
    longitude: -77.2621,
    geohash: encodeGeohash(38.8998, -77.2621),
    region: 'vienna-va',
    resourceType: 'stone',
    resourceOutput: 120,
    controlledBy: null,
    controllerName: 'Unclaimed',
    level: 2,
    totalBattles: 0,
    lastConquered: null,
    createdAt: new Date()
  },
  {
    id: 'wolftrap-park',
    name: 'Wolf Trap Park',
    latitude: 38.9358,
    longitude: -77.2650,
    geohash: encodeGeohash(38.9358, -77.2650),
    region: 'vienna-va',
    resourceType: 'gold',
    resourceOutput: 200,
    controlledBy: null,
    controllerName: 'Unclaimed',
    level: 3,
    totalBattles: 0,
    lastConquered: null,
    createdAt: new Date()
  },
  {
    id: 'meadowlark-gardens',
    name: 'Meadowlark Gardens',
    latitude: 38.9394,
    longitude: -77.2794,
    geohash: encodeGeohash(38.9394, -77.2794),
    region: 'vienna-va',
    resourceType: 'wood',
    resourceOutput: 150,
    controlledBy: null,
    controllerName: 'Unclaimed',
    level: 2,
    totalBattles: 0,
    lastConquered: null,
    createdAt: new Date()
  },
  {
    id: 'vienna-metro-station',
    name: 'Vienna Metro Station',
    latitude: 38.8776,
    longitude: -77.2714,
    geohash: encodeGeohash(38.8776, -77.2714),
    region: 'vienna-va',
    resourceType: 'gold',
    resourceOutput: 180,
    controlledBy: null,
    controllerName: 'Unclaimed',
    level: 2,
    totalBattles: 0,
    lastConquered: null,
    createdAt: new Date()
  },
  {
    id: 'nottoway-park',
    name: 'Nottoway Park',
    latitude: 38.8869,
    longitude: -77.2942,
    geohash: encodeGeohash(38.8869, -77.2942),
    region: 'vienna-va',
    resourceType: 'wood',
    resourceOutput: 90,
    controlledBy: null,
    controllerName: 'Unclaimed',
    level: 2,
    totalBattles: 0,
    lastConquered: null,
    createdAt: new Date()
  }
];

// San Francisco territories (original test data)
const sfTerritories = [
  {
    id: 'sf-financial-district',
    name: 'Financial District Citadel',
    latitude: 37.7946,
    longitude: -122.3999,
    geohash: encodeGeohash(37.7946, -122.3999),
    region: 'sf-downtown',
    resourceType: 'gold',
    resourceOutput: 500,
    controlledBy: null,
    controllerName: 'Unclaimed',
    level: 5,
    totalBattles: 0,
    lastConquered: null,
    createdAt: new Date()
  },
  {
    id: 'sf-mission-district',
    name: 'Mission District Fortress',
    latitude: 37.7599,
    longitude: -122.4148,
    geohash: encodeGeohash(37.7599, -122.4148),
    region: 'sf-mission',
    resourceType: 'crystal',
    resourceOutput: 300,
    controlledBy: null,
    controllerName: 'Unclaimed',
    level: 3,
    totalBattles: 0,
    lastConquered: null,
    createdAt: new Date()
  },
  {
    id: 'sf-golden-gate-park',
    name: 'Golden Gate Stronghold',
    latitude: 37.7694,
    longitude: -122.4862,
    geohash: encodeGeohash(37.7694, -122.4862),
    region: 'sf-richmond',
    resourceType: 'gold',
    resourceOutput: 200,
    controlledBy: null,
    controllerName: 'Unclaimed',
    level: 4,
    totalBattles: 0,
    lastConquered: null,
    createdAt: new Date()
  }
];

// Resource nodes for Vienna
const viennaResourceNodes = [
  {
    id: 'vienna-gold-1',
    name: 'Maple Avenue Gold Mine',
    latitude: 38.9025,
    longitude: -77.2680,
    geohash: encodeGeohash(38.9025, -77.2680),
    type: 'gold',
    yield: 50,
    region: 'vienna-va'
  },
  {
    id: 'vienna-wood-1',
    name: 'Difficult Run Lumber',
    latitude: 38.9150,
    longitude: -77.2850,
    geohash: encodeGeohash(38.9150, -77.2850),
    type: 'wood',
    yield: 80,
    region: 'vienna-va'
  },
  {
    id: 'vienna-stone-1',
    name: 'Hunter Mill Quarry',
    latitude: 38.9200,
    longitude: -77.2550,
    geohash: encodeGeohash(38.9200, -77.2550),
    type: 'stone',
    yield: 60,
    region: 'vienna-va'
  },
  {
    id: 'vienna-gold-2',
    name: 'Church Street Treasury',
    latitude: 38.8995,
    longitude: -77.2645,
    geohash: encodeGeohash(38.8995, -77.2645),
    type: 'gold',
    yield: 40,
    region: 'vienna-va'
  },
  {
    id: 'vienna-wood-2',
    name: 'W&OD Trail Forest',
    latitude: 38.8850,
    longitude: -77.2750,
    geohash: encodeGeohash(38.8850, -77.2750),
    type: 'wood',
    yield: 100,
    region: 'vienna-va'
  }
];

async function seedData() {
  console.log('🌱 Starting seed process...\n');
  
  const batch = db.batch();
  let count = 0;
  
  // Seed territories
  console.log('📍 Seeding Vienna, VA territories...');
  for (const territory of viennaVaTerritories) {
    const ref = db.collection('territories').doc(territory.id);
    batch.set(ref, territory);
    count++;
    console.log(`   ✓ ${territory.name}`);
  }
  
  console.log('\n📍 Seeding San Francisco territories...');
  for (const territory of sfTerritories) {
    const ref = db.collection('territories').doc(territory.id);
    batch.set(ref, territory);
    count++;
    console.log(`   ✓ ${territory.name}`);
  }
  
  console.log('\n💎 Seeding resource nodes...');
  for (const node of viennaResourceNodes) {
    const ref = db.collection('resource_nodes').doc(node.id);
    batch.set(ref, node);
    count++;
    console.log(`   ✓ ${node.name}`);
  }
  
  // Commit the batch
  console.log('\n⏳ Committing to Firestore...');
  await batch.commit();
  
  console.log(`\n✅ Successfully seeded ${count} documents!`);
  console.log('\nYou can view the data at:');
  console.log('https://console.firebase.google.com/project/apex-citadels-dev/firestore');
}

seedData().catch(error => {
  console.error('❌ Seeding failed:', error.message);
  process.exit(1);
});
