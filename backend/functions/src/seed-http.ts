/**
 * One-time seed function for production data
 * Call via: curl https://us-central1-apex-citadels-dev.cloudfunctions.net/seedTerritories
 */

import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';

const db = admin.firestore();

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

export const seedTerritories = functions.https.onRequest(async (req, res) => {
  try {
    const batch = db.batch();
    
    for (const t of territories) {
      const docRef = db.collection('territories').doc(t.id);
      batch.set(docRef, {
        ...t,
        centerLatitude: t.latitude,
        centerLongitude: t.longitude,
        radius: 100,
        radiusMeters: 100,
        health: 100,
        maxHealth: 100,
        claimedAt: t.ownerId ? admin.firestore.Timestamp.now() : null,
        createdAt: admin.firestore.Timestamp.now(),
      });
    }
    
    await batch.commit();
    
    res.json({
      success: true,
      message: `Seeded ${territories.length} territories`,
      territories: territories.map(t => ({ id: t.id, name: t.name, location: `${t.latitude}, ${t.longitude}` }))
    });
  } catch (error: any) {
    res.status(500).json({ success: false, error: error.message });
  }
});
