/**
 * Cosmetics Shop System
 * 
 * Handles the in-game cosmetics shop including:
 * - Shop catalog with categories
 * - Block skins, territory effects, avatar frames
 * - Purchase/unlock tracking
 * - Time-limited/rotating shop items
 * - Currency management (gems/coins)
 */

import * as functions from "firebase-functions";
import * as admin from "firebase-admin";

const db = admin.firestore();

// ============================================================================
// Types
// ============================================================================

interface CosmeticItem {
  id: string;
  name: string;
  description: string;
  category: CosmeticCategory;
  rarity: CosmeticRarity;
  previewImage: string;
  preview3dModel?: string;
  priceGems: number;
  priceCoins: number;
  unlockCondition?: UnlockCondition;
  isLimited: boolean;
  availableFrom?: Date;
  availableUntil?: Date;
  isActive: boolean;
  sortOrder: number;
  tags: string[];
  createdAt: Date;
}

interface UnlockCondition {
  type: "achievement" | "level" | "alliance_rank" | "event" | "purchase";
  requirementId?: string;
  requirementValue?: number;
}

type CosmeticCategory = 
  | "block_skin"
  | "territory_effect"
  | "avatar_frame"
  | "avatar_background"
  | "victory_animation"
  | "building_effect"
  | "chat_bubble"
  | "title"
  | "emote"
  | "trail_effect";

type CosmeticRarity = "common" | "uncommon" | "rare" | "epic" | "legendary";

interface UserCosmetics {
  owned: string[];
  equipped: {
    [category: string]: string | null;
  };
  favorites: string[];
}

interface ShopRotation {
  id: string;
  name: string;
  startDate: Date;
  endDate: Date;
  featuredItems: string[];
  discountPercentage: number;
  isActive: boolean;
}

interface PurchaseResult {
  success: boolean;
  itemId?: string;
  newBalance?: {
    gems: number;
    coins: number;
  };
  message: string;
}

interface CurrencyBalance {
  gems: number;
  coins: number;
  premiumGems: number; // Purchased gems (separate from earned)
}

// ============================================================================
// Rarity Multipliers & Constants
// ============================================================================

const RARITY_MULTIPLIERS: Record<CosmeticRarity, number> = {
  common: 1,
  uncommon: 1.5,
  rare: 2.5,
  epic: 4,
  legendary: 8,
};

const BASE_PRICES = {
  block_skin: { gems: 100, coins: 1000 },
  territory_effect: { gems: 200, coins: 2000 },
  avatar_frame: { gems: 150, coins: 1500 },
  avatar_background: { gems: 75, coins: 750 },
  victory_animation: { gems: 250, coins: 2500 },
  building_effect: { gems: 300, coins: 3000 },
  chat_bubble: { gems: 50, coins: 500 },
  title: { gems: 100, coins: 1000 },
  emote: { gems: 75, coins: 750 },
  trail_effect: { gems: 200, coins: 2000 },
};

// ============================================================================
// Shop Catalog Functions
// ============================================================================

/**
 * Get shop catalog with all available items
 */
export const getShopCatalog = functions.https.onCall(async (data, context) => {
  const userId = context.auth?.uid;
  const { category, includeOwned = false, showLimitedOnly = false } = data || {};

  try {
    // Build query
    let query: admin.firestore.Query = db.collection("cosmetics")
      .where("isActive", "==", true);

    if (category) {
      query = query.where("category", "==", category);
    }

    if (showLimitedOnly) {
      query = query.where("isLimited", "==", true);
    }

    query = query.orderBy("sortOrder", "asc");

    const snapshot = await query.get();
    const now = new Date();

    // Get user's owned cosmetics if authenticated
    let ownedItems: string[] = [];
    if (userId) {
      const userCosmeticsDoc = await db.collection("users").doc(userId)
        .collection("data").doc("cosmetics").get();
      if (userCosmeticsDoc.exists) {
        ownedItems = userCosmeticsDoc.data()?.owned || [];
      }
    }

    // Get current rotation for discounts
    const rotationSnapshot = await db.collection("shopRotations")
      .where("isActive", "==", true)
      .where("startDate", "<=", now)
      .where("endDate", ">=", now)
      .limit(1)
      .get();

    let currentRotation: ShopRotation | null = null;
    if (!rotationSnapshot.empty) {
      currentRotation = rotationSnapshot.docs[0].data() as ShopRotation;
    }

    // Process items
    const items: any[] = [];
    snapshot.docs.forEach(doc => {
      const item = doc.data() as CosmeticItem;
      
      // Check time availability for limited items
      if (item.isLimited) {
        if (item.availableFrom && new Date(item.availableFrom as any) > now) return;
        if (item.availableUntil && new Date(item.availableUntil as any) < now) return;
      }

      // Skip owned items if requested
      const isOwned = ownedItems.includes(doc.id);
      if (!includeOwned && isOwned) return;

      // Calculate prices with potential discount
      let finalPriceGems = item.priceGems;
      let finalPriceCoins = item.priceCoins;
      let discountPercentage = 0;

      if (currentRotation && currentRotation.featuredItems.includes(doc.id)) {
        discountPercentage = currentRotation.discountPercentage;
        finalPriceGems = Math.round(item.priceGems * (1 - discountPercentage / 100));
        finalPriceCoins = Math.round(item.priceCoins * (1 - discountPercentage / 100));
      }

      items.push({
        id: doc.id,
        name: item.name,
        description: item.description,
        category: item.category,
        rarity: item.rarity,
        previewImage: item.previewImage,
        preview3dModel: item.preview3dModel,
        priceGems: finalPriceGems,
        priceCoins: finalPriceCoins,
        originalPriceGems: item.priceGems,
        originalPriceCoins: item.priceCoins,
        discountPercentage,
        isLimited: item.isLimited,
        availableUntil: item.availableUntil,
        isOwned,
        unlockCondition: item.unlockCondition,
        tags: item.tags,
      });
    });

    return {
      items,
      rotation: currentRotation ? {
        name: currentRotation.name,
        endDate: currentRotation.endDate,
        discountPercentage: currentRotation.discountPercentage,
      } : null,
      categories: Object.keys(BASE_PRICES),
    };
  } catch (error: any) {
    console.error("Error fetching shop catalog:", error);
    throw new functions.https.HttpsError("internal", "Failed to fetch shop catalog");
  }
});

/**
 * Get featured items for shop homepage
 */
export const getFeaturedItems = functions.https.onCall(async (data, context) => {
  try {
    const now = new Date();

    // Get current rotation
    const rotationSnapshot = await db.collection("shopRotations")
      .where("isActive", "==", true)
      .where("startDate", "<=", now)
      .where("endDate", ">=", now)
      .limit(1)
      .get();

    if (rotationSnapshot.empty) {
      // Default: return legendary items
      const legendarySnapshot = await db.collection("cosmetics")
        .where("isActive", "==", true)
        .where("rarity", "==", "legendary")
        .limit(4)
        .get();

      return {
        featured: legendarySnapshot.docs.map(doc => ({
          id: doc.id,
          ...doc.data(),
        })),
        rotationName: "Featured Legendaries",
        endsAt: null,
      };
    }

    const rotation = rotationSnapshot.docs[0].data() as ShopRotation;
    
    // Get featured items
    const itemsSnapshot = await db.collection("cosmetics")
      .where(admin.firestore.FieldPath.documentId(), "in", rotation.featuredItems.slice(0, 10))
      .get();

    return {
      featured: itemsSnapshot.docs.map(doc => ({
        id: doc.id,
        ...doc.data(),
        discountPercentage: rotation.discountPercentage,
      })),
      rotationName: rotation.name,
      endsAt: rotation.endDate,
      discountPercentage: rotation.discountPercentage,
    };
  } catch (error: any) {
    console.error("Error fetching featured items:", error);
    throw new functions.https.HttpsError("internal", "Failed to fetch featured items");
  }
});

// ============================================================================
// Purchase Functions
// ============================================================================

/**
 * Purchase a cosmetic item
 */
export const purchaseCosmetic = functions.https.onCall(async (data, context): Promise<PurchaseResult> => {
  if (!context.auth) {
    throw new functions.https.HttpsError("unauthenticated", "Must be logged in");
  }

  const userId = context.auth.uid;
  const { itemId, currency = "gems" } = data;

  if (!itemId) {
    throw new functions.https.HttpsError("invalid-argument", "Item ID required");
  }

  if (!["gems", "coins"].includes(currency)) {
    throw new functions.https.HttpsError("invalid-argument", "Invalid currency type");
  }

  try {
    // Run as transaction
    return await db.runTransaction(async (transaction) => {
      // Get item
      const itemRef = db.collection("cosmetics").doc(itemId);
      const itemDoc = await transaction.get(itemRef);

      if (!itemDoc.exists) {
        return { success: false, message: "Item not found" };
      }

      const item = itemDoc.data() as CosmeticItem;

      if (!item.isActive) {
        return { success: false, message: "Item is no longer available" };
      }

      // Check time availability for limited items
      const now = new Date();
      if (item.isLimited) {
        if (item.availableFrom && new Date(item.availableFrom as any) > now) {
          return { success: false, message: "Item not yet available" };
        }
        if (item.availableUntil && new Date(item.availableUntil as any) < now) {
          return { success: false, message: "Item is no longer available" };
        }
      }

      // Get user data
      const userRef = db.collection("users").doc(userId);
      const userDoc = await transaction.get(userRef);

      if (!userDoc.exists) {
        return { success: false, message: "User not found" };
      }

      const userData = userDoc.data()!;

      // Get user cosmetics
      const cosmeticsRef = db.collection("users").doc(userId)
        .collection("data").doc("cosmetics");
      const cosmeticsDoc = await transaction.get(cosmeticsRef);
      const userCosmetics: UserCosmetics = cosmeticsDoc.exists 
        ? cosmeticsDoc.data() as UserCosmetics
        : { owned: [], equipped: {}, favorites: [] };

      // Check if already owned
      if (userCosmetics.owned.includes(itemId)) {
        return { success: false, message: "You already own this item" };
      }

      // Check unlock condition
      if (item.unlockCondition && item.unlockCondition.type !== "purchase") {
        const conditionMet = await checkUnlockCondition(userId, item.unlockCondition, transaction);
        if (!conditionMet) {
          return { success: false, message: "You haven't met the unlock requirements for this item" };
        }
      }

      // Calculate price (check for rotation discount)
      let price = currency === "gems" ? item.priceGems : item.priceCoins;

      const rotationSnapshot = await db.collection("shopRotations")
        .where("isActive", "==", true)
        .where("startDate", "<=", now)
        .where("endDate", ">=", now)
        .limit(1)
        .get();

      if (!rotationSnapshot.empty) {
        const rotation = rotationSnapshot.docs[0].data() as ShopRotation;
        if (rotation.featuredItems.includes(itemId)) {
          price = Math.round(price * (1 - rotation.discountPercentage / 100));
        }
      }

      // Check balance
      const currentBalance: CurrencyBalance = {
        gems: userData.gems || 0,
        coins: userData.coins || 0,
        premiumGems: userData.premiumGems || 0,
      };

      if (currency === "gems") {
        // Use premium gems first, then regular gems
        const totalGems = currentBalance.gems + currentBalance.premiumGems;
        if (totalGems < price) {
          return { 
            success: false, 
            message: `Not enough gems. Need ${price}, have ${totalGems}` 
          };
        }

        // Deduct from premium first
        let remaining = price;
        let newPremiumGems = currentBalance.premiumGems;
        let newGems = currentBalance.gems;

        if (currentBalance.premiumGems >= remaining) {
          newPremiumGems -= remaining;
          remaining = 0;
        } else {
          remaining -= currentBalance.premiumGems;
          newPremiumGems = 0;
          newGems -= remaining;
        }

        transaction.update(userRef, {
          gems: newGems,
          premiumGems: newPremiumGems,
        });
      } else {
        if (currentBalance.coins < price) {
          return { 
            success: false, 
            message: `Not enough coins. Need ${price}, have ${currentBalance.coins}` 
          };
        }

        transaction.update(userRef, {
          coins: admin.firestore.FieldValue.increment(-price),
        });
      }

      // Add item to owned
      userCosmetics.owned.push(itemId);
      transaction.set(cosmeticsRef, userCosmetics, { merge: true });

      // Record purchase
      const purchaseRef = db.collection("users").doc(userId)
        .collection("purchases").doc();
      transaction.set(purchaseRef, {
        itemId,
        itemName: item.name,
        category: item.category,
        rarity: item.rarity,
        currency,
        price,
        purchasedAt: admin.firestore.FieldValue.serverTimestamp(),
      });

      // Update item purchase count
      transaction.update(itemRef, {
        purchaseCount: admin.firestore.FieldValue.increment(1),
      });

      // Get updated balance
      const newUserDoc = await transaction.get(userRef);
      const newUserData = newUserDoc.data()!;

      return {
        success: true,
        itemId,
        newBalance: {
          gems: (newUserData.gems || 0) + (newUserData.premiumGems || 0),
          coins: newUserData.coins || 0,
        },
        message: `Successfully purchased ${item.name}!`,
      };
    });
  } catch (error: any) {
    console.error("Error purchasing cosmetic:", error);
    throw new functions.https.HttpsError("internal", "Purchase failed");
  }
});

/**
 * Check if unlock condition is met
 */
async function checkUnlockCondition(
  userId: string, 
  condition: UnlockCondition,
  transaction: admin.firestore.Transaction
): Promise<boolean> {
  switch (condition.type) {
    case "achievement":
      const achievementsRef = db.collection("users").doc(userId)
        .collection("data").doc("achievements");
      const achievementsDoc = await transaction.get(achievementsRef);
      if (!achievementsDoc.exists) return false;
      const achievements = achievementsDoc.data()?.completed || [];
      return achievements.includes(condition.requirementId);

    case "level":
      const userRef = db.collection("users").doc(userId);
      const userDoc = await transaction.get(userRef);
      if (!userDoc.exists) return false;
      const level = userDoc.data()?.level || 1;
      return level >= (condition.requirementValue || 1);

    case "alliance_rank":
      const allianceRef = db.collection("users").doc(userId)
        .collection("data").doc("alliance");
      const allianceDoc = await transaction.get(allianceRef);
      if (!allianceDoc.exists) return false;
      const rank = allianceDoc.data()?.rank || 0;
      return rank >= (condition.requirementValue || 1);

    case "event":
      const eventsRef = db.collection("users").doc(userId)
        .collection("data").doc("events");
      const eventsDoc = await transaction.get(eventsRef);
      if (!eventsDoc.exists) return false;
      const completedEvents = eventsDoc.data()?.completed || [];
      return completedEvents.includes(condition.requirementId);

    case "purchase":
      return true; // Can always purchase

    default:
      return false;
  }
}

// ============================================================================
// User Cosmetics Management
// ============================================================================

/**
 * Get user's owned cosmetics
 */
export const getUserCosmetics = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError("unauthenticated", "Must be logged in");
  }

  const userId = context.auth.uid;

  try {
    const cosmeticsDoc = await db.collection("users").doc(userId)
      .collection("data").doc("cosmetics").get();

    if (!cosmeticsDoc.exists) {
      return {
        owned: [],
        equipped: {},
        favorites: [],
        items: [],
      };
    }

    const userCosmetics = cosmeticsDoc.data() as UserCosmetics;

    // Get full item details for owned items
    const items: any[] = [];
    if (userCosmetics.owned.length > 0) {
      // Batch get in chunks of 10 (Firestore limitation)
      const chunks = [];
      for (let i = 0; i < userCosmetics.owned.length; i += 10) {
        chunks.push(userCosmetics.owned.slice(i, i + 10));
      }

      for (const chunk of chunks) {
        const snapshot = await db.collection("cosmetics")
          .where(admin.firestore.FieldPath.documentId(), "in", chunk)
          .get();

        snapshot.docs.forEach(doc => {
          items.push({
            id: doc.id,
            ...doc.data(),
            isEquipped: userCosmetics.equipped[doc.data().category] === doc.id,
            isFavorite: userCosmetics.favorites.includes(doc.id),
          });
        });
      }
    }

    return {
      owned: userCosmetics.owned,
      equipped: userCosmetics.equipped,
      favorites: userCosmetics.favorites,
      items,
    };
  } catch (error: any) {
    console.error("Error fetching user cosmetics:", error);
    throw new functions.https.HttpsError("internal", "Failed to fetch cosmetics");
  }
});

/**
 * Equip a cosmetic item
 */
export const equipCosmetic = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError("unauthenticated", "Must be logged in");
  }

  const userId = context.auth.uid;
  const { itemId } = data;

  if (!itemId) {
    throw new functions.https.HttpsError("invalid-argument", "Item ID required");
  }

  try {
    // Get item to determine category
    const itemDoc = await db.collection("cosmetics").doc(itemId).get();
    if (!itemDoc.exists) {
      throw new functions.https.HttpsError("not-found", "Item not found");
    }

    const item = itemDoc.data() as CosmeticItem;

    // Check ownership
    const cosmeticsDoc = await db.collection("users").doc(userId)
      .collection("data").doc("cosmetics").get();

    if (!cosmeticsDoc.exists) {
      throw new functions.https.HttpsError("not-found", "No cosmetics owned");
    }

    const userCosmetics = cosmeticsDoc.data() as UserCosmetics;

    if (!userCosmetics.owned.includes(itemId)) {
      throw new functions.https.HttpsError("permission-denied", "You don't own this item");
    }

    // Equip item
    userCosmetics.equipped[item.category] = itemId;

    await db.collection("users").doc(userId)
      .collection("data").doc("cosmetics")
      .update({
        equipped: userCosmetics.equipped,
      });

    // Update user profile if visible cosmetic
    if (["avatar_frame", "avatar_background", "title"].includes(item.category)) {
      await db.collection("users").doc(userId).update({
        [`cosmetics.${item.category}`]: itemId,
      });
    }

    return {
      success: true,
      equipped: userCosmetics.equipped,
    };
  } catch (error: any) {
    console.error("Error equipping cosmetic:", error);
    throw new functions.https.HttpsError("internal", "Failed to equip item");
  }
});

/**
 * Unequip a cosmetic item
 */
export const unequipCosmetic = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError("unauthenticated", "Must be logged in");
  }

  const userId = context.auth.uid;
  const { category } = data;

  if (!category) {
    throw new functions.https.HttpsError("invalid-argument", "Category required");
  }

  try {
    const cosmeticsRef = db.collection("users").doc(userId)
      .collection("data").doc("cosmetics");

    await cosmeticsRef.update({
      [`equipped.${category}`]: null,
    });

    // Update user profile if visible cosmetic
    if (["avatar_frame", "avatar_background", "title"].includes(category)) {
      await db.collection("users").doc(userId).update({
        [`cosmetics.${category}`]: null,
      });
    }

    return { success: true };
  } catch (error: any) {
    console.error("Error unequipping cosmetic:", error);
    throw new functions.https.HttpsError("internal", "Failed to unequip item");
  }
});

/**
 * Toggle favorite status
 */
export const toggleFavorite = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError("unauthenticated", "Must be logged in");
  }

  const userId = context.auth.uid;
  const { itemId } = data;

  if (!itemId) {
    throw new functions.https.HttpsError("invalid-argument", "Item ID required");
  }

  try {
    const cosmeticsRef = db.collection("users").doc(userId)
      .collection("data").doc("cosmetics");
    const cosmeticsDoc = await cosmeticsRef.get();

    if (!cosmeticsDoc.exists) {
      throw new functions.https.HttpsError("not-found", "No cosmetics owned");
    }

    const userCosmetics = cosmeticsDoc.data() as UserCosmetics;

    if (!userCosmetics.owned.includes(itemId)) {
      throw new functions.https.HttpsError("permission-denied", "You don't own this item");
    }

    const isFavorite = userCosmetics.favorites.includes(itemId);

    if (isFavorite) {
      await cosmeticsRef.update({
        favorites: admin.firestore.FieldValue.arrayRemove(itemId),
      });
    } else {
      await cosmeticsRef.update({
        favorites: admin.firestore.FieldValue.arrayUnion(itemId),
      });
    }

    return {
      success: true,
      isFavorite: !isFavorite,
    };
  } catch (error: any) {
    console.error("Error toggling favorite:", error);
    throw new functions.https.HttpsError("internal", "Failed to update favorite");
  }
});

// ============================================================================
// Currency Functions
// ============================================================================

/**
 * Get user's currency balance
 */
export const getCurrencyBalance = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError("unauthenticated", "Must be logged in");
  }

  const userId = context.auth.uid;

  try {
    const userDoc = await db.collection("users").doc(userId).get();

    if (!userDoc.exists) {
      throw new functions.https.HttpsError("not-found", "User not found");
    }

    const userData = userDoc.data()!;

    return {
      gems: (userData.gems || 0) + (userData.premiumGems || 0),
      earnedGems: userData.gems || 0,
      premiumGems: userData.premiumGems || 0,
      coins: userData.coins || 0,
    };
  } catch (error: any) {
    console.error("Error fetching balance:", error);
    throw new functions.https.HttpsError("internal", "Failed to fetch balance");
  }
});

/**
 * Award currency to user (internal use / rewards)
 */
export const awardCurrency = functions.https.onCall(async (data, context) => {
  // Admin only
  if (!context.auth?.token?.admin) {
    throw new functions.https.HttpsError("permission-denied", "Admin only");
  }

  const { userId, gems, coins, reason } = data;

  if (!userId) {
    throw new functions.https.HttpsError("invalid-argument", "User ID required");
  }

  try {
    const updates: any = {};
    if (gems) updates.gems = admin.firestore.FieldValue.increment(gems);
    if (coins) updates.coins = admin.firestore.FieldValue.increment(coins);

    await db.collection("users").doc(userId).update(updates);

    // Record transaction
    await db.collection("users").doc(userId)
      .collection("currencyTransactions").add({
        type: "award",
        gems: gems || 0,
        coins: coins || 0,
        reason,
        awardedBy: context.auth.uid,
        awardedAt: admin.firestore.FieldValue.serverTimestamp(),
      });

    return { success: true };
  } catch (error: any) {
    console.error("Error awarding currency:", error);
    throw new functions.https.HttpsError("internal", "Failed to award currency");
  }
});

// ============================================================================
// Admin Functions
// ============================================================================

/**
 * Create or update cosmetic item
 */
export const adminCreateCosmetic = functions.https.onCall(async (data, context) => {
  if (!context.auth?.token?.admin) {
    throw new functions.https.HttpsError("permission-denied", "Admin only");
  }

  const {
    id,
    name,
    description,
    category,
    rarity,
    previewImage,
    preview3dModel,
    customPriceGems,
    customPriceCoins,
    unlockCondition,
    isLimited,
    availableFrom,
    availableUntil,
    tags,
    sortOrder,
  } = data;

  if (!name || !category || !rarity || !previewImage) {
    throw new functions.https.HttpsError("invalid-argument", "Missing required fields");
  }

  try {
    // Calculate price based on category and rarity if not custom
    const basePrice = BASE_PRICES[category as keyof typeof BASE_PRICES] || { gems: 100, coins: 1000 };
    const multiplier = RARITY_MULTIPLIERS[rarity as CosmeticRarity] || 1;

    const itemData: Partial<CosmeticItem> = {
      name,
      description: description || "",
      category,
      rarity,
      previewImage,
      preview3dModel: preview3dModel || null,
      priceGems: customPriceGems || Math.round(basePrice.gems * multiplier),
      priceCoins: customPriceCoins || Math.round(basePrice.coins * multiplier),
      unlockCondition: unlockCondition || null,
      isLimited: isLimited || false,
      availableFrom: availableFrom ? new Date(availableFrom) : undefined,
      availableUntil: availableUntil ? new Date(availableUntil) : undefined,
      isActive: true,
      sortOrder: sortOrder || 0,
      tags: tags || [],
    };

    if (id) {
      // Update existing
      await db.collection("cosmetics").doc(id).update({
        ...itemData,
        updatedAt: admin.firestore.FieldValue.serverTimestamp(),
      });
      return { success: true, id };
    } else {
      // Create new
      const docRef = await db.collection("cosmetics").add({
        ...itemData,
        createdAt: admin.firestore.FieldValue.serverTimestamp(),
      });
      return { success: true, id: docRef.id };
    }
  } catch (error: any) {
    console.error("Error creating cosmetic:", error);
    throw new functions.https.HttpsError("internal", "Failed to create cosmetic");
  }
});

/**
 * Create shop rotation
 */
export const adminCreateRotation = functions.https.onCall(async (data, context) => {
  if (!context.auth?.token?.admin) {
    throw new functions.https.HttpsError("permission-denied", "Admin only");
  }

  const {
    name,
    startDate,
    endDate,
    featuredItems,
    discountPercentage,
  } = data;

  if (!name || !startDate || !endDate || !featuredItems) {
    throw new functions.https.HttpsError("invalid-argument", "Missing required fields");
  }

  try {
    const docRef = await db.collection("shopRotations").add({
      name,
      startDate: new Date(startDate),
      endDate: new Date(endDate),
      featuredItems,
      discountPercentage: discountPercentage || 0,
      isActive: true,
      createdAt: admin.firestore.FieldValue.serverTimestamp(),
      createdBy: context.auth.uid,
    });

    return { success: true, id: docRef.id };
  } catch (error: any) {
    console.error("Error creating rotation:", error);
    throw new functions.https.HttpsError("internal", "Failed to create rotation");
  }
});

/**
 * Grant cosmetic to user (gifting / promo)
 */
export const adminGrantCosmetic = functions.https.onCall(async (data, context) => {
  if (!context.auth?.token?.admin) {
    throw new functions.https.HttpsError("permission-denied", "Admin only");
  }

  const { userId, itemId, reason } = data;

  if (!userId || !itemId) {
    throw new functions.https.HttpsError("invalid-argument", "User ID and item ID required");
  }

  try {
    // Verify item exists
    const itemDoc = await db.collection("cosmetics").doc(itemId).get();
    if (!itemDoc.exists) {
      throw new functions.https.HttpsError("not-found", "Item not found");
    }

    const item = itemDoc.data() as CosmeticItem;

    // Add to user's owned
    const cosmeticsRef = db.collection("users").doc(userId)
      .collection("data").doc("cosmetics");

    await cosmeticsRef.set({
      owned: admin.firestore.FieldValue.arrayUnion(itemId),
    }, { merge: true });

    // Record grant
    await db.collection("users").doc(userId)
      .collection("grantedCosmetics").add({
        itemId,
        itemName: item.name,
        reason,
        grantedBy: context.auth.uid,
        grantedAt: admin.firestore.FieldValue.serverTimestamp(),
      });

    return { success: true };
  } catch (error: any) {
    console.error("Error granting cosmetic:", error);
    throw new functions.https.HttpsError("internal", "Failed to grant cosmetic");
  }
});
