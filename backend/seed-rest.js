#!/usr/bin/env node
// Seed territories using Firestore REST API
// Run with: node seed-rest.js

const https = require('https');

const PROJECT_ID = 'apex-citadels-dev';
const API_KEY = 'AIzaSyA7ljLJjxoq8VCqV1EGFpO5nhk56H0B6oo';

const territories = [
  // San Francisco
  { id: 'territory-sf-downtown', name: 'SF Downtown', latitude: 37.7749, longitude: -122.4194, level: 3, ownerId: 'test-user-1', ownerName: 'TestPlayer1' },
  { id: 'territory-sf-embarcadero', name: 'Embarcadero', latitude: 37.7955, longitude: -122.3937, level: 2, ownerId: 'test-user-2', ownerName: 'RivalPlayer' },
  { id: 'territory-sf-mission', name: 'Mission District', latitude: 37.7599, longitude: -122.4148, level: 1, ownerId: null, ownerName: null },
  
  // Vienna, VA
  { id: 'territory-vienna-downtown', name: 'Vienna Town Green', latitude: 38.9012, longitude: -77.2653, level: 3, ownerId: 'test-user-1', ownerName: 'TestPlayer1' },
  { id: 'territory-vienna-metro', name: 'Vienna Metro Station', latitude: 38.8779, longitude: -77.2711, level: 2, ownerId: 'test-user-2', ownerName: 'RivalPlayer' },
  { id: 'territory-vienna-maple', name: 'Maple Avenue', latitude: 38.9001, longitude: -77.2545, level: 2, ownerId: null, ownerName: null },
  { id: 'territory-tysons', name: 'Tysons Corner', latitude: 38.9187, longitude: -77.2311, level: 4, ownerId: 'test-user-2', ownerName: 'RivalPlayer' },
  { id: 'territory-wolftrap', name: 'Wolf Trap', latitude: 38.9378, longitude: -77.2656, level: 2, ownerId: null, ownerName: null },
  { id: 'territory-meadowlark', name: 'Meadowlark Gardens', latitude: 38.9394, longitude: -77.2803, level: 1, ownerId: 'test-user-3', ownerName: 'AllianceMember' },
  { id: 'territory-oakton', name: 'Oakton', latitude: 38.8809, longitude: -77.3006, level: 1, ownerId: null, ownerName: null },
];

function toFirestoreValue(value) {
  if (value === null) return { nullValue: null };
  if (typeof value === 'string') return { stringValue: value };
  if (typeof value === 'number') {
    if (Number.isInteger(value)) return { integerValue: value.toString() };
    return { doubleValue: value };
  }
  if (typeof value === 'boolean') return { booleanValue: value };
  return { nullValue: null };
}

function toFirestoreDoc(obj) {
  const fields = {};
  for (const [key, value] of Object.entries(obj)) {
    fields[key] = toFirestoreValue(value);
  }
  return { fields };
}

async function createDocument(collectionPath, docId, data) {
  const url = `https://firestore.googleapis.com/v1/projects/${PROJECT_ID}/databases/(default)/documents/${collectionPath}/${docId}?key=${API_KEY}`;
  
  const firestoreDoc = toFirestoreDoc({
    ...data,
    centerLatitude: data.latitude,
    centerLongitude: data.longitude,
    radius: 100,
    radiusMeters: 100,
    health: 100,
    maxHealth: 100,
  });

  return new Promise((resolve, reject) => {
    const req = https.request(url, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' }
    }, (res) => {
      let body = '';
      res.on('data', chunk => body += chunk);
      res.on('end', () => {
        if (res.statusCode >= 200 && res.statusCode < 300) {
          resolve({ success: true });
        } else {
          resolve({ success: false, error: body });
        }
      });
    });
    
    req.on('error', reject);
    req.write(JSON.stringify(firestoreDoc));
    req.end();
  });
}

async function seed() {
  console.log('🌱 Seeding production Firestore via REST API...\n');
  console.log(`Project: ${PROJECT_ID}\n`);
  
  let success = 0;
  let failed = 0;
  
  for (const t of territories) {
    const result = await createDocument('territories', t.id, t);
    if (result.success) {
      console.log(`✓ ${t.name} (${t.latitude.toFixed(4)}, ${t.longitude.toFixed(4)})`);
      success++;
    } else {
      console.log(`✗ ${t.name}: ${result.error}`);
      failed++;
    }
  }
  
  console.log(`\n✅ Done! ${success} succeeded, ${failed} failed`);
}

seed().catch(console.error);
