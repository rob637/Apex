/**
 * Blueprint System - Save and Restore Citadel Layouts
 * 
 * Allows players to:
 * - Save their current citadel as a blueprint
 * - Auto-save blueprints when territory falls
 * - Load blueprints when relocating to a new territory
 * - Manage a library of saved blueprints
 * 
 * Blueprint Flow:
 * 1. Player builds citadel over time
 * 2. Territory falls after 3 battle losses
 * 3. System auto-saves blueprint of fallen citadel
 * 4. Player waits 24hr reclaim cooldown
 * 5. Player either reclaims original spot or relocates
 * 6. If relocating, can use saved blueprint to quickly rebuild
 */

import * as functions from 'firebase-functions/v2';
import * as admin from 'firebase-admin';
import {
  Blueprint,
  BuildingPlacement,
  Territory,
  UserResources,
  RESOURCE_CONFIG
} from './types';

const db = admin.firestore();

// ============================================================================
// Configuration
// ============================================================================

const BLUEPRINT_CONFIG = {
  MAX_BLUEPRINTS_PER_USER: 10,           // Maximum saved blueprints
  MAX_BLUEPRINT_NAME_LENGTH: 50,          // Name character limit
  MAX_BLUEPRINT_DESC_LENGTH: 200,         // Description character limit
  BLUEPRINT_REBUILD_DISCOUNT: 0.25,       // 25% discount when using blueprint
  AUTO_SAVE_NAME_PREFIX: 'Auto-Save: ',   // Prefix for auto-saved blueprints
};

// ============================================================================
// Save Blueprint from Territory
// ============================================================================

/**
 * Save current territory layout as a reusable blueprint
 */
export const saveBlueprint = functions.https.onCall(async (request) => {
  const { auth, data } = request;
  
  if (!auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const userId = auth.uid;
  const { territoryId, name, description } = data as {
    territoryId: string;
    name: string;
    description?: string;
  };
  
  // Validate inputs
  if (!territoryId || !name) {
    throw new functions.https.HttpsError('invalid-argument', 'Territory ID and name required');
  }
  
  if (name.length > BLUEPRINT_CONFIG.MAX_BLUEPRINT_NAME_LENGTH) {
    throw new functions.https.HttpsError('invalid-argument', `Name must be under ${BLUEPRINT_CONFIG.MAX_BLUEPRINT_NAME_LENGTH} characters`);
  }
  
  if (description && description.length > BLUEPRINT_CONFIG.MAX_BLUEPRINT_DESC_LENGTH) {
    throw new functions.https.HttpsError('invalid-argument', `Description must be under ${BLUEPRINT_CONFIG.MAX_BLUEPRINT_DESC_LENGTH} characters`);
  }
  
  // Check blueprint limit
  const existingBlueprints = await db.collection('blueprints')
    .where('ownerId', '==', userId)
    .where('isAutoSaved', '==', false)
    .get();
  
  if (existingBlueprints.size >= BLUEPRINT_CONFIG.MAX_BLUEPRINTS_PER_USER) {
    throw new functions.https.HttpsError(
      'resource-exhausted',
      `Maximum ${BLUEPRINT_CONFIG.MAX_BLUEPRINTS_PER_USER} blueprints allowed. Delete some to save more.`
    );
  }
  
  // Get territory
  const territoryDoc = await db.collection('territories').doc(territoryId).get();
  if (!territoryDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Territory not found');
  }
  
  const territory = territoryDoc.data() as Territory;
  
  // Verify ownership
  if (territory.ownerId !== userId) {
    throw new functions.https.HttpsError('permission-denied', 'You can only save blueprints from your own territories');
  }
  
  // Check territory has buildings
  if (!territory.buildings || territory.buildings.length === 0) {
    throw new functions.https.HttpsError('failed-precondition', 'Territory has no buildings to save');
  }
  
  // Calculate total build cost
  const totalCost = calculateBuildCost(territory.buildings);
  
  // Create blueprint
  const blueprintRef = db.collection('blueprints').doc();
  const blueprint: Blueprint = {
    id: blueprintRef.id,
    ownerId: userId,
    name: name.trim(),
    description: description?.trim(),
    buildings: territory.buildings,
    totalBuildCost: totalCost,
    sourceTerritoryId: territoryId,
    createdAt: admin.firestore.Timestamp.now(),
    isAutoSaved: false
  };
  
  await blueprintRef.set(blueprint);
  
  return {
    success: true,
    blueprintId: blueprint.id,
    buildingCount: territory.buildings.length,
    totalCost
  };
});

// ============================================================================
// Auto-Save Blueprint (Called when territory falls)
// ============================================================================

/**
 * Automatically save territory layout when it falls
 * Called internally by the battle system
 */
export async function autoSaveBlueprint(
  userId: string,
  territory: Territory
): Promise<string | null> {
  // Don't save if no buildings
  if (!territory.buildings || territory.buildings.length === 0) {
    return null;
  }
  
  // Check for existing auto-save from this territory
  const existingAutoSave = await db.collection('blueprints')
    .where('ownerId', '==', userId)
    .where('sourceTerritoryId', '==', territory.id)
    .where('isAutoSaved', '==', true)
    .limit(1)
    .get();
  
  // If auto-save exists, update it
  if (!existingAutoSave.empty) {
    const existingDoc = existingAutoSave.docs[0];
    await existingDoc.ref.update({
      buildings: territory.buildings,
      totalBuildCost: calculateBuildCost(territory.buildings),
      createdAt: admin.firestore.Timestamp.now()
    });
    return existingDoc.id;
  }
  
  // Clean up old auto-saves if at limit (keep manual blueprints)
  const autoSaves = await db.collection('blueprints')
    .where('ownerId', '==', userId)
    .where('isAutoSaved', '==', true)
    .orderBy('createdAt', 'asc')
    .get();
  
  // Keep only 3 most recent auto-saves
  const MAX_AUTO_SAVES = 3;
  if (autoSaves.size >= MAX_AUTO_SAVES) {
    const toDelete = autoSaves.docs.slice(0, autoSaves.size - MAX_AUTO_SAVES + 1);
    const batch = db.batch();
    toDelete.forEach(doc => batch.delete(doc.ref));
    await batch.commit();
  }
  
  // Create new auto-save
  const blueprintRef = db.collection('blueprints').doc();
  const blueprint: Blueprint = {
    id: blueprintRef.id,
    ownerId: userId,
    name: `${BLUEPRINT_CONFIG.AUTO_SAVE_NAME_PREFIX}${territory.ownerName}'s Citadel`,
    description: `Auto-saved when territory fell on ${new Date().toLocaleDateString()}`,
    buildings: territory.buildings,
    totalBuildCost: calculateBuildCost(territory.buildings),
    sourceTerritoryId: territory.id,
    createdAt: admin.firestore.Timestamp.now(),
    isAutoSaved: true
  };
  
  await blueprintRef.set(blueprint);
  
  return blueprint.id;
}

// ============================================================================
// Get User Blueprints
// ============================================================================

/**
 * Get all blueprints owned by the user
 */
export const getMyBlueprints = functions.https.onCall(async (request) => {
  const { auth } = request;
  
  if (!auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const userId = auth.uid;
  
  const blueprintsSnap = await db.collection('blueprints')
    .where('ownerId', '==', userId)
    .orderBy('createdAt', 'desc')
    .get();
  
  const blueprints = blueprintsSnap.docs.map(doc => {
    const data = doc.data() as Blueprint;
    return {
      id: data.id,
      name: data.name,
      description: data.description,
      buildingCount: data.buildings.length,
      totalBuildCost: data.totalBuildCost,
      isAutoSaved: data.isAutoSaved,
      createdAt: data.createdAt.toDate().toISOString()
    };
  });
  
  // Separate auto-saves and manual saves
  const manualBlueprints = blueprints.filter(b => !b.isAutoSaved);
  const autoSavedBlueprints = blueprints.filter(b => b.isAutoSaved);
  
  return {
    blueprints: manualBlueprints,
    autoSaves: autoSavedBlueprints,
    totalCount: blueprints.length,
    maxAllowed: BLUEPRINT_CONFIG.MAX_BLUEPRINTS_PER_USER
  };
});

// ============================================================================
// Get Blueprint Details
// ============================================================================

/**
 * Get full details of a specific blueprint
 */
export const getBlueprintDetails = functions.https.onCall(async (request) => {
  const { auth, data } = request;
  
  if (!auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const userId = auth.uid;
  const { blueprintId } = data as { blueprintId: string };
  
  if (!blueprintId) {
    throw new functions.https.HttpsError('invalid-argument', 'Blueprint ID required');
  }
  
  const blueprintDoc = await db.collection('blueprints').doc(blueprintId).get();
  
  if (!blueprintDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Blueprint not found');
  }
  
  const blueprint = blueprintDoc.data() as Blueprint;
  
  // Verify ownership
  if (blueprint.ownerId !== userId) {
    throw new functions.https.HttpsError('permission-denied', 'Not your blueprint');
  }
  
  // Calculate rebuild cost with discount
  const rebuildCost = calculateRebuildCost(blueprint.totalBuildCost);
  
  return {
    id: blueprint.id,
    name: blueprint.name,
    description: blueprint.description,
    buildings: blueprint.buildings,
    totalBuildCost: blueprint.totalBuildCost,
    rebuildCost,
    rebuildDiscount: BLUEPRINT_CONFIG.BLUEPRINT_REBUILD_DISCOUNT,
    buildingCount: blueprint.buildings.length,
    isAutoSaved: blueprint.isAutoSaved,
    sourceTerritoryId: blueprint.sourceTerritoryId,
    createdAt: blueprint.createdAt.toDate().toISOString()
  };
});

// ============================================================================
// Delete Blueprint
// ============================================================================

/**
 * Delete a saved blueprint
 */
export const deleteBlueprint = functions.https.onCall(async (request) => {
  const { auth, data } = request;
  
  if (!auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const userId = auth.uid;
  const { blueprintId } = data as { blueprintId: string };
  
  if (!blueprintId) {
    throw new functions.https.HttpsError('invalid-argument', 'Blueprint ID required');
  }
  
  const blueprintDoc = await db.collection('blueprints').doc(blueprintId).get();
  
  if (!blueprintDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Blueprint not found');
  }
  
  const blueprint = blueprintDoc.data() as Blueprint;
  
  // Verify ownership
  if (blueprint.ownerId !== userId) {
    throw new functions.https.HttpsError('permission-denied', 'Not your blueprint');
  }
  
  await blueprintDoc.ref.delete();
  
  return { success: true, deletedId: blueprintId };
});

// ============================================================================
// Rename Blueprint
// ============================================================================

/**
 * Rename a saved blueprint
 */
export const renameBlueprint = functions.https.onCall(async (request) => {
  const { auth, data } = request;
  
  if (!auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const userId = auth.uid;
  const { blueprintId, name, description } = data as {
    blueprintId: string;
    name: string;
    description?: string;
  };
  
  if (!blueprintId || !name) {
    throw new functions.https.HttpsError('invalid-argument', 'Blueprint ID and name required');
  }
  
  if (name.length > BLUEPRINT_CONFIG.MAX_BLUEPRINT_NAME_LENGTH) {
    throw new functions.https.HttpsError('invalid-argument', `Name must be under ${BLUEPRINT_CONFIG.MAX_BLUEPRINT_NAME_LENGTH} characters`);
  }
  
  const blueprintDoc = await db.collection('blueprints').doc(blueprintId).get();
  
  if (!blueprintDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Blueprint not found');
  }
  
  const blueprint = blueprintDoc.data() as Blueprint;
  
  // Verify ownership
  if (blueprint.ownerId !== userId) {
    throw new functions.https.HttpsError('permission-denied', 'Not your blueprint');
  }
  
  const updates: Partial<Blueprint> = {
    name: name.trim(),
    isAutoSaved: false // Renaming converts auto-save to manual
  };
  
  if (description !== undefined) {
    updates.description = description.trim();
  }
  
  await blueprintDoc.ref.update(updates);
  
  return { success: true, blueprintId };
});

// ============================================================================
// Apply Blueprint to Territory (Relocate with Blueprint)
// ============================================================================

/**
 * Apply a blueprint to rebuild a territory
 * Used when relocating after territory loss
 */
export const applyBlueprint = functions.https.onCall(async (request) => {
  const { auth, data } = request;
  
  if (!auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const userId = auth.uid;
  const { territoryId, blueprintId } = data as {
    territoryId: string;
    blueprintId: string;
  };
  
  if (!territoryId || !blueprintId) {
    throw new functions.https.HttpsError('invalid-argument', 'Territory ID and Blueprint ID required');
  }
  
  // Get territory
  const territoryDoc = await db.collection('territories').doc(territoryId).get();
  if (!territoryDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Territory not found');
  }
  
  const territory = territoryDoc.data() as Territory;
  
  // Verify ownership
  if (territory.ownerId !== userId) {
    throw new functions.https.HttpsError('permission-denied', 'Not your territory');
  }
  
  // Check territory is empty/new (no buildings yet)
  if (territory.buildings && territory.buildings.length > 0) {
    throw new functions.https.HttpsError(
      'failed-precondition',
      'Territory already has buildings. Clear it first or apply to a new territory.'
    );
  }
  
  // Get blueprint
  const blueprintDoc = await db.collection('blueprints').doc(blueprintId).get();
  if (!blueprintDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Blueprint not found');
  }
  
  const blueprint = blueprintDoc.data() as Blueprint;
  
  // Verify blueprint ownership
  if (blueprint.ownerId !== userId) {
    throw new functions.https.HttpsError('permission-denied', 'Not your blueprint');
  }
  
  // Calculate rebuild cost with discount
  const rebuildCost = calculateRebuildCost(blueprint.totalBuildCost);
  
  // Get user resources
  const userDoc = await db.collection('users').doc(userId).get();
  if (!userDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'User not found');
  }
  
  const user = userDoc.data()!;
  const userResources = user.resources || {};
  
  // Check if user can afford rebuild
  const affordability = checkAffordability(userResources, rebuildCost);
  if (!affordability.canAfford) {
    throw new functions.https.HttpsError(
      'failed-precondition',
      `Insufficient resources. Missing: ${affordability.missing.join(', ')}`
    );
  }
  
  // Update timestamps on buildings
  const now = admin.firestore.Timestamp.now();
  const updatedBuildings: BuildingPlacement[] = blueprint.buildings.map(b => ({
    ...b,
    placedAt: now
  }));
  
  // Apply blueprint in transaction
  await db.runTransaction(async (transaction) => {
    // Deduct resources
    const resourceUpdates: Record<string, admin.firestore.FieldValue> = {};
    for (const [resource, amount] of Object.entries(rebuildCost)) {
      if (amount && amount > 0) {
        resourceUpdates[`resources.${resource}`] = admin.firestore.FieldValue.increment(-amount);
      }
    }
    
    transaction.update(db.collection('users').doc(userId), resourceUpdates);
    
    // Apply buildings to territory
    transaction.update(territoryDoc.ref, {
      buildings: updatedBuildings,
      blueprintId: blueprintId,
      level: Math.min(10, Math.floor(updatedBuildings.length / 5) + 1), // Recalculate level
      maxHealth: 100 + (updatedBuildings.length * 10),
      health: 100 + (updatedBuildings.length * 10)
    });
  });
  
  return {
    success: true,
    buildingsPlaced: updatedBuildings.length,
    resourcesSpent: rebuildCost,
    discountApplied: BLUEPRINT_CONFIG.BLUEPRINT_REBUILD_DISCOUNT
  };
});

// ============================================================================
// Preview Blueprint Cost
// ============================================================================

/**
 * Preview the cost to apply a blueprint without actually applying it
 */
export const previewBlueprintCost = functions.https.onCall(async (request) => {
  const { auth, data } = request;
  
  if (!auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const userId = auth.uid;
  const { blueprintId } = data as { blueprintId: string };
  
  if (!blueprintId) {
    throw new functions.https.HttpsError('invalid-argument', 'Blueprint ID required');
  }
  
  // Get blueprint
  const blueprintDoc = await db.collection('blueprints').doc(blueprintId).get();
  if (!blueprintDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Blueprint not found');
  }
  
  const blueprint = blueprintDoc.data() as Blueprint;
  
  // Verify blueprint ownership
  if (blueprint.ownerId !== userId) {
    throw new functions.https.HttpsError('permission-denied', 'Not your blueprint');
  }
  
  // Get user resources
  const userDoc = await db.collection('users').doc(userId).get();
  const userResources = userDoc.exists ? userDoc.data()!.resources || {} : {};
  
  // Calculate costs
  const originalCost = blueprint.totalBuildCost;
  const rebuildCost = calculateRebuildCost(originalCost);
  const affordability = checkAffordability(userResources, rebuildCost);
  
  return {
    blueprintName: blueprint.name,
    buildingCount: blueprint.buildings.length,
    originalCost,
    rebuildCost,
    discount: BLUEPRINT_CONFIG.BLUEPRINT_REBUILD_DISCOUNT,
    discountAmount: calculateDiscountAmount(originalCost),
    canAfford: affordability.canAfford,
    missing: affordability.missing,
    currentResources: userResources
  };
});

// ============================================================================
// Helper Functions
// ============================================================================

/**
 * Calculate total build cost from building placements
 * Uses a simple formula - in production would look up actual block costs
 */
function calculateBuildCost(buildings: BuildingPlacement[]): Partial<UserResources> {
  // Group buildings by type
  const typeCount: Record<string, number> = {};
  for (const building of buildings) {
    typeCount[building.blockType] = (typeCount[building.blockType] || 0) + 1;
  }
  
  // Calculate costs (simplified - would normally look up from block definitions)
  let stone = 0;
  let wood = 0;
  let iron = 0;
  let crystal = 0;
  let arcaneEssence = 0;
  
  for (const [blockType, count] of Object.entries(typeCount)) {
    // Basic blocks cost stone/wood
    if (blockType.includes('stone') || blockType.includes('wall')) {
      stone += count * 10;
    } else if (blockType.includes('wood') || blockType.includes('plank')) {
      wood += count * 10;
    } else if (blockType.includes('iron') || blockType.includes('metal') || blockType.includes('gate')) {
      iron += count * 15;
    } else if (blockType.includes('crystal') || blockType.includes('glass')) {
      crystal += count * 5;
    } else if (blockType.includes('arcane') || blockType.includes('magic') || blockType.includes('enchanted')) {
      arcaneEssence += count * 3;
      crystal += count * 2;
    } else {
      // Default: basic materials
      stone += count * 5;
      wood += count * 5;
    }
  }
  
  const cost: Partial<UserResources> = {};
  if (stone > 0) cost.stone = stone;
  if (wood > 0) cost.wood = wood;
  if (iron > 0) cost.iron = iron;
  if (crystal > 0) cost.crystal = crystal;
  if (arcaneEssence > 0) cost.arcaneEssence = arcaneEssence;
  
  return cost;
}

/**
 * Calculate rebuild cost with discount
 */
function calculateRebuildCost(originalCost: Partial<UserResources>): Partial<UserResources> {
  const discount = 1 - BLUEPRINT_CONFIG.BLUEPRINT_REBUILD_DISCOUNT;
  const rebuildCost: Partial<UserResources> = {};
  
  for (const [resource, amount] of Object.entries(originalCost)) {
    if (amount && amount > 0) {
      rebuildCost[resource as keyof UserResources] = Math.ceil(amount * discount);
    }
  }
  
  return rebuildCost;
}

/**
 * Calculate discount amount
 */
function calculateDiscountAmount(originalCost: Partial<UserResources>): Partial<UserResources> {
  const discount = BLUEPRINT_CONFIG.BLUEPRINT_REBUILD_DISCOUNT;
  const discountAmount: Partial<UserResources> = {};
  
  for (const [resource, amount] of Object.entries(originalCost)) {
    if (amount && amount > 0) {
      discountAmount[resource as keyof UserResources] = Math.floor(amount * discount);
    }
  }
  
  return discountAmount;
}

/**
 * Check if user can afford the cost
 */
function checkAffordability(
  userResources: Partial<UserResources>,
  cost: Partial<UserResources>
): { canAfford: boolean; missing: string[] } {
  const missing: string[] = [];
  
  for (const [resource, amount] of Object.entries(cost)) {
    if (amount && amount > 0) {
      const available = (userResources as Record<string, number>)[resource] || 0;
      if (available < amount) {
        const resourceName = RESOURCE_CONFIG[resource as keyof typeof RESOURCE_CONFIG]?.displayName || resource;
        missing.push(`${resourceName}: need ${amount}, have ${available}`);
      }
    }
  }
  
  return {
    canAfford: missing.length === 0,
    missing
  };
}
