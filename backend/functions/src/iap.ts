/**
 * In-App Purchases System
 * 
 * Complete server-side purchase validation and entitlement management
 * Supports iOS App Store and Google Play Store
 */

import { onCall, HttpsError } from 'firebase-functions/v2/https';
import { onSchedule } from 'firebase-functions/v2/scheduler';
import * as admin from 'firebase-admin';
import { google } from 'googleapis';
// JWT import for Apple App Store Server API (future use)
// import * as jwt from 'jsonwebtoken';

const db = admin.firestore();

// ============================================================================
// TYPES
// ============================================================================

export interface Product {
  id: string;
  storeProductId: string;
  name: string;
  description: string;
  type: ProductType;
  category: ProductCategory;
  priceUSD: number;
  rewards: ProductReward[];
  isActive: boolean;
  sortOrder: number;
  metadata: Record<string, any>;
}

export enum ProductType {
  CONSUMABLE = 'consumable',           // Gems, resource packs
  NON_CONSUMABLE = 'non_consumable',   // Cosmetics, permanent unlocks
  SUBSCRIPTION = 'subscription',        // Premium pass, VIP
  AUTO_RENEWABLE = 'auto_renewable'    // Monthly subscriptions
}

export enum ProductCategory {
  CURRENCY = 'currency',
  SEASON_PASS = 'season_pass',
  STARTER_PACK = 'starter_pack',
  RESOURCE_PACK = 'resource_pack',
  COSMETIC = 'cosmetic',
  SPECIAL_OFFER = 'special_offer',
  SUBSCRIPTION = 'subscription'
}

export interface ProductReward {
  type: RewardType;
  itemId?: string;
  amount: number;
}

export enum RewardType {
  GEMS = 'gems',
  COINS = 'coins',
  STONE = 'stone',
  WOOD = 'wood',
  IRON = 'iron',
  CRYSTAL = 'crystal',
  ARCANE_ESSENCE = 'arcane_essence',
  XP = 'xp',
  SEASON_XP = 'season_xp',
  PREMIUM_PASS = 'premium_pass',
  COSMETIC = 'cosmetic',
  CHEST = 'chest',
  BOOST = 'boost'
}

export interface Purchase {
  id: string;
  userId: string;
  productId: string;
  storeProductId: string;
  store: 'apple' | 'google';
  transactionId: string;
  originalTransactionId?: string;
  receiptData: string;
  status: PurchaseStatus;
  priceUSD: number;
  currency: string;
  localPrice: number;
  rewards: ProductReward[];
  rewardsGranted: boolean;
  purchasedAt: FirebaseFirestore.Timestamp;
  validatedAt?: FirebaseFirestore.Timestamp;
  expiresAt?: FirebaseFirestore.Timestamp;
  refundedAt?: FirebaseFirestore.Timestamp;
  environment: 'sandbox' | 'production';
  metadata: Record<string, any>;
}

export enum PurchaseStatus {
  PENDING = 'pending',
  COMPLETED = 'completed',
  FAILED = 'failed',
  REFUNDED = 'refunded',
  EXPIRED = 'expired',
  CANCELLED = 'cancelled'
}

export interface Entitlement {
  userId: string;
  type: EntitlementType;
  productId: string;
  grantedAt: FirebaseFirestore.Timestamp;
  expiresAt?: FirebaseFirestore.Timestamp;
  isActive: boolean;
  source: 'purchase' | 'gift' | 'promo' | 'admin';
  transactionId?: string;
}

export enum EntitlementType {
  PREMIUM_PASS = 'premium_pass',
  VIP_SUBSCRIPTION = 'vip_subscription',
  COSMETIC_UNLOCK = 'cosmetic_unlock',
  PERMANENT_BOOST = 'permanent_boost',
  AD_FREE = 'ad_free'
}

// Apple App Store receipt verification
interface AppleReceiptResponse {
  status: number;
  environment: 'Sandbox' | 'Production';
  receipt: {
    bundle_id: string;
    in_app: AppleInAppPurchase[];
  };
  latest_receipt_info?: AppleInAppPurchase[];
  pending_renewal_info?: ApplePendingRenewal[];
}

interface AppleInAppPurchase {
  product_id: string;
  transaction_id: string;
  original_transaction_id: string;
  purchase_date_ms: string;
  expires_date_ms?: string;
  cancellation_date_ms?: string;
  is_trial_period?: string;
  is_in_intro_offer_period?: string;
}

interface ApplePendingRenewal {
  product_id: string;
  auto_renew_status: string;
  expiration_intent?: string;
}

// Google Play receipt verification
interface GooglePlayPurchase {
  orderId: string;
  packageName: string;
  productId: string;
  purchaseTime: number;
  purchaseState: number;
  purchaseToken: string;
  acknowledged: boolean;
  consumptionState?: number;
}

interface GooglePlaySubscription {
  orderId: string;
  startTimeMillis: string;
  expiryTimeMillis: string;
  autoRenewing: boolean;
  priceCurrencyCode: string;
  priceAmountMicros: string;
  paymentState: number;
  cancelReason?: number;
  userCancellationTimeMillis?: string;
}

// ============================================================================
// PRODUCT CATALOG
// ============================================================================

const PRODUCT_CATALOG: Product[] = [
  // Currency Packs
  {
    id: 'gems_100',
    storeProductId: 'com.apexcitadels.gems100',
    name: 'Handful of Gems',
    description: '100 Gems',
    type: ProductType.CONSUMABLE,
    category: ProductCategory.CURRENCY,
    priceUSD: 0.99,
    rewards: [{ type: RewardType.GEMS, amount: 100 }],
    isActive: true,
    sortOrder: 1,
    metadata: { bonus: 0 }
  },
  {
    id: 'gems_500',
    storeProductId: 'com.apexcitadels.gems500',
    name: 'Pouch of Gems',
    description: '500 Gems + 50 Bonus',
    type: ProductType.CONSUMABLE,
    category: ProductCategory.CURRENCY,
    priceUSD: 4.99,
    rewards: [{ type: RewardType.GEMS, amount: 550 }],
    isActive: true,
    sortOrder: 2,
    metadata: { bonus: 50 }
  },
  {
    id: 'gems_1200',
    storeProductId: 'com.apexcitadels.gems1200',
    name: 'Chest of Gems',
    description: '1200 Gems + 200 Bonus',
    type: ProductType.CONSUMABLE,
    category: ProductCategory.CURRENCY,
    priceUSD: 9.99,
    rewards: [{ type: RewardType.GEMS, amount: 1400 }],
    isActive: true,
    sortOrder: 3,
    metadata: { bonus: 200, popular: true }
  },
  {
    id: 'gems_2500',
    storeProductId: 'com.apexcitadels.gems2500',
    name: 'Vault of Gems',
    description: '2500 Gems + 500 Bonus',
    type: ProductType.CONSUMABLE,
    category: ProductCategory.CURRENCY,
    priceUSD: 19.99,
    rewards: [{ type: RewardType.GEMS, amount: 3000 }],
    isActive: true,
    sortOrder: 4,
    metadata: { bonus: 500 }
  },
  {
    id: 'gems_6500',
    storeProductId: 'com.apexcitadels.gems6500',
    name: 'Treasury of Gems',
    description: '6500 Gems + 1500 Bonus',
    type: ProductType.CONSUMABLE,
    category: ProductCategory.CURRENCY,
    priceUSD: 49.99,
    rewards: [{ type: RewardType.GEMS, amount: 8000 }],
    isActive: true,
    sortOrder: 5,
    metadata: { bonus: 1500, bestValue: true }
  },
  {
    id: 'gems_14000',
    storeProductId: 'com.apexcitadels.gems14000',
    name: 'Kingdom of Gems',
    description: '14000 Gems + 4000 Bonus',
    type: ProductType.CONSUMABLE,
    category: ProductCategory.CURRENCY,
    priceUSD: 99.99,
    rewards: [{ type: RewardType.GEMS, amount: 18000 }],
    isActive: true,
    sortOrder: 6,
    metadata: { bonus: 4000 }
  },

  // Season Pass
  {
    id: 'season_pass_premium',
    storeProductId: 'com.apexcitadels.seasonpass',
    name: 'Premium Battle Pass',
    description: 'Unlock all premium rewards this season',
    type: ProductType.NON_CONSUMABLE,
    category: ProductCategory.SEASON_PASS,
    priceUSD: 9.99,
    rewards: [{ type: RewardType.PREMIUM_PASS, amount: 1 }],
    isActive: true,
    sortOrder: 10,
    metadata: { seasonNumber: 1 }
  },
  {
    id: 'season_pass_bundle',
    storeProductId: 'com.apexcitadels.seasonpassbundle',
    name: 'Premium Pass + 25 Tiers',
    description: 'Premium pass with instant 25 tier boost',
    type: ProductType.NON_CONSUMABLE,
    category: ProductCategory.SEASON_PASS,
    priceUSD: 24.99,
    rewards: [
      { type: RewardType.PREMIUM_PASS, amount: 1 },
      { type: RewardType.SEASON_XP, amount: 62500 } // 25 tiers worth
    ],
    isActive: true,
    sortOrder: 11,
    metadata: { seasonNumber: 1, tierBoost: 25 }
  },

  // Starter Packs (one-time purchases)
  {
    id: 'starter_pack',
    storeProductId: 'com.apexcitadels.starterpack',
    name: 'Starter Pack',
    description: 'Best value for new players!',
    type: ProductType.NON_CONSUMABLE,
    category: ProductCategory.STARTER_PACK,
    priceUSD: 4.99,
    rewards: [
      { type: RewardType.GEMS, amount: 500 },
      { type: RewardType.COINS, amount: 50000 },
      { type: RewardType.STONE, amount: 1000 },
      { type: RewardType.WOOD, amount: 1000 },
      { type: RewardType.COSMETIC, itemId: 'avatar_frame_gold', amount: 1 }
    ],
    isActive: true,
    sortOrder: 20,
    metadata: { oneTimePurchase: true, minLevel: 1, maxLevel: 10 }
  },
  {
    id: 'growth_pack',
    storeProductId: 'com.apexcitadels.growthpack',
    name: 'Growth Pack',
    description: 'Accelerate your progress!',
    type: ProductType.NON_CONSUMABLE,
    category: ProductCategory.STARTER_PACK,
    priceUSD: 9.99,
    rewards: [
      { type: RewardType.GEMS, amount: 1000 },
      { type: RewardType.COINS, amount: 100000 },
      { type: RewardType.XP, amount: 50000 },
      { type: RewardType.BOOST, itemId: 'xp_boost_24h', amount: 3 }
    ],
    isActive: true,
    sortOrder: 21,
    metadata: { oneTimePurchase: true, minLevel: 5, maxLevel: 20 }
  },
  {
    id: 'conqueror_pack',
    storeProductId: 'com.apexcitadels.conquerorpack',
    name: 'Conqueror Pack',
    description: 'Dominate the battlefield!',
    type: ProductType.NON_CONSUMABLE,
    category: ProductCategory.STARTER_PACK,
    priceUSD: 19.99,
    rewards: [
      { type: RewardType.GEMS, amount: 2500 },
      { type: RewardType.COINS, amount: 250000 },
      { type: RewardType.IRON, amount: 2000 },
      { type: RewardType.CRYSTAL, amount: 500 },
      { type: RewardType.COSMETIC, itemId: 'territory_skin_flame', amount: 1 }
    ],
    isActive: true,
    sortOrder: 22,
    metadata: { oneTimePurchase: true, minLevel: 15 }
  },

  // Resource Packs
  {
    id: 'resource_pack_basic',
    storeProductId: 'com.apexcitadels.resourcebasic',
    name: 'Resource Cache',
    description: 'Basic building materials',
    type: ProductType.CONSUMABLE,
    category: ProductCategory.RESOURCE_PACK,
    priceUSD: 2.99,
    rewards: [
      { type: RewardType.STONE, amount: 2000 },
      { type: RewardType.WOOD, amount: 2000 },
      { type: RewardType.COINS, amount: 25000 }
    ],
    isActive: true,
    sortOrder: 30,
    metadata: {}
  },
  {
    id: 'resource_pack_advanced',
    storeProductId: 'com.apexcitadels.resourceadvanced',
    name: 'Resource Stockpile',
    description: 'Advanced building materials',
    type: ProductType.CONSUMABLE,
    category: ProductCategory.RESOURCE_PACK,
    priceUSD: 7.99,
    rewards: [
      { type: RewardType.STONE, amount: 5000 },
      { type: RewardType.WOOD, amount: 5000 },
      { type: RewardType.IRON, amount: 1000 },
      { type: RewardType.COINS, amount: 75000 }
    ],
    isActive: true,
    sortOrder: 31,
    metadata: {}
  },
  {
    id: 'resource_pack_premium',
    storeProductId: 'com.apexcitadels.resourcepremium',
    name: 'Resource Bonanza',
    description: 'All materials including crystal',
    type: ProductType.CONSUMABLE,
    category: ProductCategory.RESOURCE_PACK,
    priceUSD: 14.99,
    rewards: [
      { type: RewardType.STONE, amount: 10000 },
      { type: RewardType.WOOD, amount: 10000 },
      { type: RewardType.IRON, amount: 3000 },
      { type: RewardType.CRYSTAL, amount: 500 },
      { type: RewardType.COINS, amount: 150000 }
    ],
    isActive: true,
    sortOrder: 32,
    metadata: {}
  },

  // Special Offers (time-limited)
  {
    id: 'daily_deal',
    storeProductId: 'com.apexcitadels.dailydeal',
    name: 'Daily Deal',
    description: '5x value! Limited time only!',
    type: ProductType.CONSUMABLE,
    category: ProductCategory.SPECIAL_OFFER,
    priceUSD: 0.99,
    rewards: [
      { type: RewardType.GEMS, amount: 100 },
      { type: RewardType.COINS, amount: 10000 }
    ],
    isActive: true,
    sortOrder: 40,
    metadata: { dailyLimit: 1, refreshHours: 24 }
  },
  {
    id: 'weekend_special',
    storeProductId: 'com.apexcitadels.weekendspecial',
    name: 'Weekend Warrior Pack',
    description: '3x gems + resources!',
    type: ProductType.CONSUMABLE,
    category: ProductCategory.SPECIAL_OFFER,
    priceUSD: 4.99,
    rewards: [
      { type: RewardType.GEMS, amount: 600 },
      { type: RewardType.STONE, amount: 3000 },
      { type: RewardType.WOOD, amount: 3000 },
      { type: RewardType.BOOST, itemId: 'resource_boost_4h', amount: 2 }
    ],
    isActive: false, // Activated on weekends
    sortOrder: 41,
    metadata: { weekendOnly: true }
  },

  // Subscriptions
  {
    id: 'vip_monthly',
    storeProductId: 'com.apexcitadels.vipmonthly',
    name: 'VIP Membership',
    description: 'Daily gems, XP boost, exclusive perks',
    type: ProductType.AUTO_RENEWABLE,
    category: ProductCategory.SUBSCRIPTION,
    priceUSD: 9.99,
    rewards: [
      { type: RewardType.GEMS, amount: 100 }, // Daily
      { type: RewardType.BOOST, itemId: 'xp_boost_permanent', amount: 1 }
    ],
    isActive: true,
    sortOrder: 50,
    metadata: { 
      dailyGems: 100,
      xpBoostPercent: 20,
      resourceBoostPercent: 10,
      exclusiveCosmetics: true,
      noAds: true,
      period: 'monthly'
    }
  }
];

// ============================================================================
// CONFIGURATION
// ============================================================================

// Apple App Store configuration
const APPLE_SHARED_SECRET = process.env.APPLE_SHARED_SECRET || '';
const APPLE_BUNDLE_ID = 'com.apexcitadels.game';
const APPLE_VERIFY_URL_PRODUCTION = 'https://buy.itunes.apple.com/verifyReceipt';
const APPLE_VERIFY_URL_SANDBOX = 'https://sandbox.itunes.apple.com/verifyReceipt';

// Google Play configuration
const GOOGLE_PACKAGE_NAME = 'com.apexcitadels.game';
const GOOGLE_SERVICE_ACCOUNT_EMAIL = process.env.GOOGLE_SERVICE_ACCOUNT_EMAIL || '';
const GOOGLE_SERVICE_ACCOUNT_KEY = process.env.GOOGLE_SERVICE_ACCOUNT_KEY || '';

// ============================================================================
// HELPER FUNCTIONS
// ============================================================================

function getProduct(productId: string): Product | undefined {
  return PRODUCT_CATALOG.find(p => p.id === productId || p.storeProductId === productId);
}

function getProductByStoreId(storeProductId: string): Product | undefined {
  return PRODUCT_CATALOG.find(p => p.storeProductId === storeProductId);
}

async function getGoogleAuthClient() {
  const auth = new google.auth.JWT({
    email: GOOGLE_SERVICE_ACCOUNT_EMAIL,
    key: GOOGLE_SERVICE_ACCOUNT_KEY.replace(/\\n/g, '\n'),
    scopes: ['https://www.googleapis.com/auth/androidpublisher']
  });
  await auth.authorize();
  return auth;
}

// ============================================================================
// APPLE APP STORE VALIDATION
// ============================================================================

async function verifyAppleReceipt(
  receiptData: string,
  excludeOldTransactions: boolean = true
): Promise<AppleReceiptResponse> {
  const fetch = (await import('node-fetch')).default;
  
  // Try production first
  let response = await fetch(APPLE_VERIFY_URL_PRODUCTION, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      'receipt-data': receiptData,
      'password': APPLE_SHARED_SECRET,
      'exclude-old-transactions': excludeOldTransactions
    })
  });

  let result = await response.json() as AppleReceiptResponse;

  // Status 21007 means it's a sandbox receipt
  if (result.status === 21007) {
    response = await fetch(APPLE_VERIFY_URL_SANDBOX, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        'receipt-data': receiptData,
        'password': APPLE_SHARED_SECRET,
        'exclude-old-transactions': excludeOldTransactions
      })
    });
    result = await response.json() as AppleReceiptResponse;
  }

  return result;
}

function parseAppleReceiptStatus(status: number): { valid: boolean; error?: string } {
  const statusMessages: Record<number, string> = {
    0: 'Valid',
    21000: 'The App Store could not read the JSON object',
    21002: 'The data in the receipt-data property was malformed',
    21003: 'The receipt could not be authenticated',
    21004: 'The shared secret does not match',
    21005: 'The receipt server is not currently available',
    21006: 'This receipt is valid but the subscription has expired',
    21007: 'This receipt is from the test environment (sandbox)',
    21008: 'This receipt is from the production environment',
    21009: 'Internal data access error',
    21010: 'The user account cannot be found or has been deleted'
  };

  return {
    valid: status === 0,
    error: status !== 0 ? statusMessages[status] || `Unknown error: ${status}` : undefined
  };
}

// ============================================================================
// GOOGLE PLAY VALIDATION
// ============================================================================

async function verifyGooglePlayPurchase(
  productId: string,
  purchaseToken: string,
  isSubscription: boolean = false
): Promise<GooglePlayPurchase | GooglePlaySubscription> {
  const auth = await getGoogleAuthClient();
  const androidPublisher = google.androidpublisher({ version: 'v3', auth });

  if (isSubscription) {
    const response = await androidPublisher.purchases.subscriptions.get({
      packageName: GOOGLE_PACKAGE_NAME,
      subscriptionId: productId,
      token: purchaseToken
    });
    return response.data as unknown as GooglePlaySubscription;
  } else {
    const response = await androidPublisher.purchases.products.get({
      packageName: GOOGLE_PACKAGE_NAME,
      productId: productId,
      token: purchaseToken
    });
    return response.data as unknown as GooglePlayPurchase;
  }
}

async function acknowledgeGooglePlayPurchase(
  productId: string,
  purchaseToken: string,
  isSubscription: boolean = false
): Promise<void> {
  const auth = await getGoogleAuthClient();
  const androidPublisher = google.androidpublisher({ version: 'v3', auth });

  if (isSubscription) {
    await androidPublisher.purchases.subscriptions.acknowledge({
      packageName: GOOGLE_PACKAGE_NAME,
      subscriptionId: productId,
      token: purchaseToken
    });
  } else {
    await androidPublisher.purchases.products.acknowledge({
      packageName: GOOGLE_PACKAGE_NAME,
      productId: productId,
      token: purchaseToken
    });
  }
}

async function consumeGooglePlayPurchase(
  productId: string,
  purchaseToken: string
): Promise<void> {
  const auth = await getGoogleAuthClient();
  const androidPublisher = google.androidpublisher({ version: 'v3', auth });

  await androidPublisher.purchases.products.consume({
    packageName: GOOGLE_PACKAGE_NAME,
    productId: productId,
    token: purchaseToken
  });
}

// ============================================================================
// REWARD GRANTING
// ============================================================================

async function grantRewards(
  userId: string,
  rewards: ProductReward[],
  purchaseId: string,
  transaction: FirebaseFirestore.Transaction
): Promise<void> {
  const userRef = db.collection('players').doc(userId);
  const userDoc = await transaction.get(userRef);
  
  if (!userDoc.exists) {
    throw new Error('User not found');
  }

  // User data available for future reward calculations
  // const userData = userDoc.data()!;
  const updates: Record<string, any> = {};

  for (const reward of rewards) {
    switch (reward.type) {
      case RewardType.GEMS:
        updates['resources.gems'] = admin.firestore.FieldValue.increment(reward.amount);
        break;
      case RewardType.COINS:
        updates['resources.coins'] = admin.firestore.FieldValue.increment(reward.amount);
        break;
      case RewardType.STONE:
        updates['resources.stone'] = admin.firestore.FieldValue.increment(reward.amount);
        break;
      case RewardType.WOOD:
        updates['resources.wood'] = admin.firestore.FieldValue.increment(reward.amount);
        break;
      case RewardType.IRON:
        updates['resources.iron'] = admin.firestore.FieldValue.increment(reward.amount);
        break;
      case RewardType.CRYSTAL:
        updates['resources.crystal'] = admin.firestore.FieldValue.increment(reward.amount);
        break;
      case RewardType.ARCANE_ESSENCE:
        updates['resources.arcaneEssence'] = admin.firestore.FieldValue.increment(reward.amount);
        break;
      case RewardType.XP:
        updates['xp'] = admin.firestore.FieldValue.increment(reward.amount);
        break;
      case RewardType.SEASON_XP:
        // Grant to season progress
        const seasonProgressRef = db.collection('season_progress').doc(userId);
        transaction.update(seasonProgressRef, {
          totalXP: admin.firestore.FieldValue.increment(reward.amount),
          updatedAt: admin.firestore.FieldValue.serverTimestamp()
        });
        break;
      case RewardType.PREMIUM_PASS:
        // Grant entitlement
        await grantEntitlement(
          userId,
          EntitlementType.PREMIUM_PASS,
          purchaseId,
          transaction
        );
        // Also update season progress
        const progressRef = db.collection('season_progress').doc(userId);
        transaction.update(progressRef, {
          hasPremium: true,
          premiumPurchasedAt: admin.firestore.FieldValue.serverTimestamp()
        });
        break;
      case RewardType.COSMETIC:
        // Add to user's cosmetics collection
        const cosmeticRef = db.collection('player_cosmetics')
          .doc(userId)
          .collection('items')
          .doc(reward.itemId!);
        transaction.set(cosmeticRef, {
          itemId: reward.itemId,
          unlockedAt: admin.firestore.FieldValue.serverTimestamp(),
          source: 'purchase',
          purchaseId: purchaseId
        });
        break;
      case RewardType.BOOST:
        // Add boost to user's inventory
        const boostRef = db.collection('player_boosts')
          .doc(userId)
          .collection('items')
          .doc();
        transaction.set(boostRef, {
          boostId: reward.itemId,
          quantity: reward.amount,
          grantedAt: admin.firestore.FieldValue.serverTimestamp(),
          source: 'purchase',
          purchaseId: purchaseId
        });
        break;
      case RewardType.CHEST:
        // Add chest to user's inventory
        const chestRef = db.collection('player_chests')
          .doc(userId)
          .collection('items')
          .doc();
        transaction.set(chestRef, {
          chestId: reward.itemId,
          quantity: reward.amount,
          grantedAt: admin.firestore.FieldValue.serverTimestamp(),
          source: 'purchase',
          purchaseId: purchaseId
        });
        break;
    }
  }

  if (Object.keys(updates).length > 0) {
    updates['updatedAt'] = admin.firestore.FieldValue.serverTimestamp();
    transaction.update(userRef, updates);
  }
}

async function grantEntitlement(
  userId: string,
  type: EntitlementType,
  purchaseId: string,
  transaction: FirebaseFirestore.Transaction,
  expiresAt?: Date
): Promise<void> {
  const entitlementRef = db.collection('entitlements').doc(`${userId}_${type}`);
  
  transaction.set(entitlementRef, {
    userId,
    type,
    productId: purchaseId,
    grantedAt: admin.firestore.FieldValue.serverTimestamp(),
    expiresAt: expiresAt ? admin.firestore.Timestamp.fromDate(expiresAt) : null,
    isActive: true,
    source: 'purchase',
    transactionId: purchaseId
  }, { merge: true });
}

// ============================================================================
// CLOUD FUNCTIONS
// ============================================================================

/**
 * Get product catalog
 */
export const getProductCatalog = onCall(async (request) => {
  const userId = request.auth?.uid;
  
  // Filter active products
  let products = PRODUCT_CATALOG.filter(p => p.isActive);

  // If user is logged in, filter based on their state
  if (userId) {
    const userDoc = await db.collection('players').doc(userId).get();
    const userData = userDoc.data();
    const userLevel = userData?.level || 1;

    // Check one-time purchase status
    const purchasesSnapshot = await db.collection('purchases')
      .where('userId', '==', userId)
      .where('status', '==', PurchaseStatus.COMPLETED)
      .get();
    
    const purchasedProductIds = new Set(
      purchasesSnapshot.docs.map(d => d.data().productId)
    );

    // Filter products based on user state
    products = products.filter(p => {
      // Check if one-time purchase already bought
      if (p.metadata.oneTimePurchase && purchasedProductIds.has(p.id)) {
        return false;
      }
      
      // Check level requirements
      if (p.metadata.minLevel && userLevel < p.metadata.minLevel) {
        return false;
      }
      if (p.metadata.maxLevel && userLevel > p.metadata.maxLevel) {
        return false;
      }

      return true;
    });
  }

  // Sort by category and order
  products.sort((a, b) => {
    if (a.category !== b.category) {
      return a.category.localeCompare(b.category);
    }
    return a.sortOrder - b.sortOrder;
  });

  return { products };
});

/**
 * Verify and process iOS App Store purchase
 */
export const verifyApplePurchase = onCall(async (request) => {
  const userId = request.auth?.uid;
  if (!userId) {
    throw new HttpsError('unauthenticated', 'Must be logged in');
  }

  const { receiptData, productId, transactionId } = request.data;

  if (!receiptData || !productId) {
    throw new HttpsError('invalid-argument', 'Missing receipt data or product ID');
  }

  // Verify receipt with Apple
  const appleResponse = await verifyAppleReceipt(receiptData);
  const receiptStatus = parseAppleReceiptStatus(appleResponse.status);

  if (!receiptStatus.valid) {
    // Log failed attempt
    await db.collection('purchase_failures').add({
      userId,
      productId,
      store: 'apple',
      error: receiptStatus.error,
      receiptData: receiptData.substring(0, 100), // Store partial for debugging
      timestamp: admin.firestore.FieldValue.serverTimestamp()
    });

    throw new HttpsError('invalid-argument', receiptStatus.error || 'Invalid receipt');
  }

  // Verify bundle ID
  if (appleResponse.receipt.bundle_id !== APPLE_BUNDLE_ID) {
    throw new HttpsError('invalid-argument', 'Invalid bundle ID');
  }

  // Find the transaction in the receipt
  const allPurchases = [
    ...(appleResponse.receipt.in_app || []),
    ...(appleResponse.latest_receipt_info || [])
  ];

  const matchingPurchase = allPurchases.find(p => 
    p.product_id === productId || 
    (transactionId && p.transaction_id === transactionId)
  );

  if (!matchingPurchase) {
    throw new HttpsError('not-found', 'Transaction not found in receipt');
  }

  // Check for duplicate transaction
  const existingPurchase = await db.collection('purchases')
    .where('transactionId', '==', matchingPurchase.transaction_id)
    .limit(1)
    .get();

  if (!existingPurchase.empty) {
    // Already processed - return success but don't grant again
    return {
      success: true,
      alreadyProcessed: true,
      purchaseId: existingPurchase.docs[0].id
    };
  }

  // Get product details
  const product = getProductByStoreId(matchingPurchase.product_id);
  if (!product) {
    throw new HttpsError('not-found', 'Product not found in catalog');
  }

  // Process purchase in transaction
  const purchaseId = db.collection('purchases').doc().id;
  
  await db.runTransaction(async (transaction) => {
    // Create purchase record
    const purchaseRef = db.collection('purchases').doc(purchaseId);
    const purchaseData: Purchase = {
      id: purchaseId,
      userId,
      productId: product.id,
      storeProductId: matchingPurchase.product_id,
      store: 'apple',
      transactionId: matchingPurchase.transaction_id,
      originalTransactionId: matchingPurchase.original_transaction_id,
      receiptData: receiptData,
      status: PurchaseStatus.COMPLETED,
      priceUSD: product.priceUSD,
      currency: 'USD',
      localPrice: product.priceUSD,
      rewards: product.rewards,
      rewardsGranted: true,
      purchasedAt: admin.firestore.Timestamp.fromMillis(
        parseInt(matchingPurchase.purchase_date_ms)
      ),
      validatedAt: admin.firestore.Timestamp.now(),
      expiresAt: matchingPurchase.expires_date_ms 
        ? admin.firestore.Timestamp.fromMillis(parseInt(matchingPurchase.expires_date_ms))
        : undefined,
      environment: appleResponse.environment === 'Sandbox' ? 'sandbox' : 'production',
      metadata: {
        is_trial_period: matchingPurchase.is_trial_period,
        is_in_intro_offer_period: matchingPurchase.is_in_intro_offer_period
      }
    };

    transaction.set(purchaseRef, purchaseData);

    // Grant rewards
    await grantRewards(userId, product.rewards, purchaseId, transaction);

    // Update user stats
    const userRef = db.collection('players').doc(userId);
    transaction.update(userRef, {
      'stats.totalSpent': admin.firestore.FieldValue.increment(product.priceUSD),
      'stats.purchaseCount': admin.firestore.FieldValue.increment(1),
      'stats.lastPurchaseAt': admin.firestore.FieldValue.serverTimestamp()
    });
  });

  // Track analytics
  await db.collection('analytics_events').add({
    eventName: 'purchase_completed',
    userId,
    properties: {
      product_id: product.id,
      price_usd: product.priceUSD,
      store: 'apple',
      environment: appleResponse.environment
    },
    timestamp: admin.firestore.FieldValue.serverTimestamp()
  });

  return {
    success: true,
    purchaseId,
    rewards: product.rewards
  };
});

/**
 * Verify and process Google Play purchase
 */
export const verifyGooglePurchase = onCall(async (request) => {
  const userId = request.auth?.uid;
  if (!userId) {
    throw new HttpsError('unauthenticated', 'Must be logged in');
  }

  const { productId, purchaseToken, isSubscription } = request.data;

  if (!productId || !purchaseToken) {
    throw new HttpsError('invalid-argument', 'Missing product ID or purchase token');
  }

  // Verify with Google Play
  let googlePurchase: GooglePlayPurchase | GooglePlaySubscription;
  try {
    googlePurchase = await verifyGooglePlayPurchase(productId, purchaseToken, isSubscription);
  } catch (error: any) {
    await db.collection('purchase_failures').add({
      userId,
      productId,
      store: 'google',
      error: error.message,
      timestamp: admin.firestore.FieldValue.serverTimestamp()
    });
    throw new HttpsError('invalid-argument', 'Failed to verify purchase with Google');
  }

  // Get order ID
  const orderId = googlePurchase.orderId;
  
  // Check for duplicate
  const existingPurchase = await db.collection('purchases')
    .where('transactionId', '==', orderId)
    .limit(1)
    .get();

  if (!existingPurchase.empty) {
    return {
      success: true,
      alreadyProcessed: true,
      purchaseId: existingPurchase.docs[0].id
    };
  }

  // Validate purchase state
  if (!isSubscription) {
    const productPurchase = googlePurchase as GooglePlayPurchase;
    if (productPurchase.purchaseState !== 0) { // 0 = purchased
      throw new HttpsError('failed-precondition', 'Purchase not completed');
    }
  } else {
    const subPurchase = googlePurchase as GooglePlaySubscription;
    if (subPurchase.paymentState !== 1) { // 1 = received
      throw new HttpsError('failed-precondition', 'Subscription payment not received');
    }
  }

  // Get product
  const product = getProductByStoreId(productId);
  if (!product) {
    throw new HttpsError('not-found', 'Product not found');
  }

  // Process purchase
  const purchaseId = db.collection('purchases').doc().id;

  await db.runTransaction(async (transaction) => {
    const purchaseRef = db.collection('purchases').doc(purchaseId);
    
    let purchasedAt: FirebaseFirestore.Timestamp;
    let expiresAt: FirebaseFirestore.Timestamp | undefined;
    let localPrice = product.priceUSD;
    let currency = 'USD';

    if (isSubscription) {
      const sub = googlePurchase as GooglePlaySubscription;
      purchasedAt = admin.firestore.Timestamp.fromMillis(parseInt(sub.startTimeMillis));
      expiresAt = admin.firestore.Timestamp.fromMillis(parseInt(sub.expiryTimeMillis));
      localPrice = parseInt(sub.priceAmountMicros) / 1000000;
      currency = sub.priceCurrencyCode;
    } else {
      const prod = googlePurchase as GooglePlayPurchase;
      purchasedAt = admin.firestore.Timestamp.fromMillis(prod.purchaseTime);
    }

    const purchaseData: Purchase = {
      id: purchaseId,
      userId,
      productId: product.id,
      storeProductId: productId,
      store: 'google',
      transactionId: orderId,
      receiptData: purchaseToken,
      status: PurchaseStatus.COMPLETED,
      priceUSD: product.priceUSD,
      currency,
      localPrice,
      rewards: product.rewards,
      rewardsGranted: true,
      purchasedAt,
      validatedAt: admin.firestore.Timestamp.now(),
      expiresAt,
      environment: 'production',
      metadata: { isSubscription }
    };

    transaction.set(purchaseRef, purchaseData);

    // Grant rewards
    await grantRewards(userId, product.rewards, purchaseId, transaction);

    // Update user stats
    const userRef = db.collection('players').doc(userId);
    transaction.update(userRef, {
      'stats.totalSpent': admin.firestore.FieldValue.increment(product.priceUSD),
      'stats.purchaseCount': admin.firestore.FieldValue.increment(1),
      'stats.lastPurchaseAt': admin.firestore.FieldValue.serverTimestamp()
    });
  });

  // Acknowledge/consume the purchase
  try {
    if (product.type === ProductType.CONSUMABLE) {
      await consumeGooglePlayPurchase(productId, purchaseToken);
    } else {
      await acknowledgeGooglePlayPurchase(productId, purchaseToken, isSubscription);
    }
  } catch (error) {
    console.error('Failed to acknowledge/consume purchase:', error);
    // Don't fail - we've already granted rewards
  }

  // Track analytics
  await db.collection('analytics_events').add({
    eventName: 'purchase_completed',
    userId,
    properties: {
      product_id: product.id,
      price_usd: product.priceUSD,
      store: 'google'
    },
    timestamp: admin.firestore.FieldValue.serverTimestamp()
  });

  return {
    success: true,
    purchaseId,
    rewards: product.rewards
  };
});

/**
 * Restore purchases (for iOS restore button and app reinstalls)
 */
export const restorePurchases = onCall(async (request) => {
  const userId = request.auth?.uid;
  if (!userId) {
    throw new HttpsError('unauthenticated', 'Must be logged in');
  }

  const { store, receiptData, purchaseToken: _purchaseToken } = request.data;
  const restoredItems: string[] = [];

  if (store === 'apple' && receiptData) {
    // Verify receipt
    const appleResponse = await verifyAppleReceipt(receiptData, false);
    if (appleResponse.status !== 0) {
      throw new HttpsError('invalid-argument', 'Invalid receipt');
    }

    // Find all non-consumable purchases
    const allPurchases = [
      ...(appleResponse.receipt.in_app || []),
      ...(appleResponse.latest_receipt_info || [])
    ];

    for (const purchase of allPurchases) {
      const product = getProductByStoreId(purchase.product_id);
      if (!product) continue;

      // Only restore non-consumables and active subscriptions
      if (product.type !== ProductType.NON_CONSUMABLE && 
          product.type !== ProductType.AUTO_RENEWABLE) {
        continue;
      }

      // Check if subscription is still active
      if (product.type === ProductType.AUTO_RENEWABLE) {
        const expiresMs = parseInt(purchase.expires_date_ms || '0');
        if (expiresMs < Date.now()) continue;
      }

      // Check if already restored
      const existing = await db.collection('purchases')
        .where('userId', '==', userId)
        .where('originalTransactionId', '==', purchase.original_transaction_id)
        .limit(1)
        .get();

      if (existing.empty) {
        // Create restored purchase record
        await db.runTransaction(async (transaction) => {
          const purchaseId = db.collection('purchases').doc().id;
          const purchaseRef = db.collection('purchases').doc(purchaseId);
          
          transaction.set(purchaseRef, {
            id: purchaseId,
            userId,
            productId: product.id,
            storeProductId: purchase.product_id,
            store: 'apple',
            transactionId: purchase.transaction_id,
            originalTransactionId: purchase.original_transaction_id,
            receiptData,
            status: PurchaseStatus.COMPLETED,
            priceUSD: product.priceUSD,
            currency: 'USD',
            localPrice: product.priceUSD,
            rewards: product.rewards,
            rewardsGranted: true,
            purchasedAt: admin.firestore.Timestamp.fromMillis(
              parseInt(purchase.purchase_date_ms)
            ),
            validatedAt: admin.firestore.Timestamp.now(),
            environment: appleResponse.environment === 'Sandbox' ? 'sandbox' : 'production',
            metadata: { restored: true }
          });

          // Re-grant entitlements (not consumables)
          for (const reward of product.rewards) {
            if (reward.type === RewardType.PREMIUM_PASS) {
              await grantEntitlement(userId, EntitlementType.PREMIUM_PASS, purchaseId, transaction);
            } else if (reward.type === RewardType.COSMETIC) {
              const cosmeticRef = db.collection('player_cosmetics')
                .doc(userId)
                .collection('items')
                .doc(reward.itemId!);
              transaction.set(cosmeticRef, {
                itemId: reward.itemId,
                unlockedAt: admin.firestore.FieldValue.serverTimestamp(),
                source: 'restore'
              }, { merge: true });
            }
          }
        });

        restoredItems.push(product.id);
      }
    }
  }

  // Google Play restore handled by querying purchase history
  // The client should handle this through Google Play Billing Library

  return {
    success: true,
    restoredCount: restoredItems.length,
    restoredItems
  };
});

/**
 * Get user's purchase history
 */
export const getPurchaseHistory = onCall(async (request) => {
  const userId = request.auth?.uid;
  if (!userId) {
    throw new HttpsError('unauthenticated', 'Must be logged in');
  }

  const purchasesSnapshot = await db.collection('purchases')
    .where('userId', '==', userId)
    .orderBy('purchasedAt', 'desc')
    .limit(100)
    .get();

  const purchases = purchasesSnapshot.docs.map(doc => {
    const data = doc.data();
    return {
      id: data.id,
      productId: data.productId,
      store: data.store,
      status: data.status,
      priceUSD: data.priceUSD,
      currency: data.currency,
      localPrice: data.localPrice,
      purchasedAt: data.purchasedAt.toDate().toISOString(),
      rewards: data.rewards
    };
  });

  return { purchases };
});

/**
 * Get user's active entitlements
 */
export const getEntitlements = onCall(async (request) => {
  const userId = request.auth?.uid;
  if (!userId) {
    throw new HttpsError('unauthenticated', 'Must be logged in');
  }

  const entitlementsSnapshot = await db.collection('entitlements')
    .where('userId', '==', userId)
    .where('isActive', '==', true)
    .get();

  const entitlements = entitlementsSnapshot.docs.map(doc => {
    const data = doc.data();
    return {
      type: data.type,
      grantedAt: data.grantedAt.toDate().toISOString(),
      expiresAt: data.expiresAt?.toDate().toISOString(),
      isActive: data.isActive,
      source: data.source
    };
  });

  // Check if user has premium pass
  const hasPremiumPass = entitlements.some(e => e.type === EntitlementType.PREMIUM_PASS);
  const hasVIP = entitlements.some(e => 
    e.type === EntitlementType.VIP_SUBSCRIPTION && 
    (!e.expiresAt || new Date(e.expiresAt) > new Date())
  );

  return {
    entitlements,
    hasPremiumPass,
    hasVIP
  };
});

/**
 * Admin: Grant product to user (for support/testing)
 */
export const adminGrantProduct = onCall(async (request) => {
  const adminId = request.auth?.uid;
  if (!adminId) {
    throw new HttpsError('unauthenticated', 'Must be logged in');
  }

  // Check admin status
  const adminDoc = await db.collection('admins').doc(adminId).get();
  if (!adminDoc.exists) {
    throw new HttpsError('permission-denied', 'Not an admin');
  }

  const { targetUserId, productId, reason } = request.data;
  if (!targetUserId || !productId || !reason) {
    throw new HttpsError('invalid-argument', 'Missing required fields');
  }

  const product = getProduct(productId);
  if (!product) {
    throw new HttpsError('not-found', 'Product not found');
  }

  const purchaseId = db.collection('purchases').doc().id;

  await db.runTransaction(async (transaction) => {
    const purchaseRef = db.collection('purchases').doc(purchaseId);
    
    transaction.set(purchaseRef, {
      id: purchaseId,
      userId: targetUserId,
      productId: product.id,
      storeProductId: product.storeProductId,
      store: 'admin',
      transactionId: `admin_${purchaseId}`,
      receiptData: '',
      status: PurchaseStatus.COMPLETED,
      priceUSD: 0,
      currency: 'USD',
      localPrice: 0,
      rewards: product.rewards,
      rewardsGranted: true,
      purchasedAt: admin.firestore.Timestamp.now(),
      validatedAt: admin.firestore.Timestamp.now(),
      environment: 'production',
      metadata: {
        grantedBy: adminId,
        reason
      }
    });

    await grantRewards(targetUserId, product.rewards, purchaseId, transaction);
  });

  // Log admin action
  await db.collection('admin_actions').add({
    adminId,
    action: 'grant_product',
    targetUserId,
    productId,
    reason,
    timestamp: admin.firestore.FieldValue.serverTimestamp()
  });

  return { success: true, purchaseId };
});

/**
 * Handle Apple App Store server notifications (webhooks)
 */
export const appleWebhook = onCall(async (request) => {
  // This would be an HTTP function in production
  const { signedPayload } = request.data;
  
  if (!signedPayload) {
    throw new HttpsError('invalid-argument', 'Missing signed payload');
  }

  // Decode and verify the JWS
  // In production, verify signature with Apple's public key
  const parts = signedPayload.split('.');
  if (parts.length !== 3) {
    throw new HttpsError('invalid-argument', 'Invalid JWS format');
  }

  const payloadJson = Buffer.from(parts[1], 'base64').toString('utf8');
  const payload = JSON.parse(payloadJson);

  const notificationType = payload.notificationType;
  const transactionInfo = payload.data?.signedTransactionInfo;

  console.log('Apple notification:', notificationType);

  switch (notificationType) {
    case 'DID_RENEW':
    case 'SUBSCRIBED':
      // Subscription renewed or started
      // Find user and extend entitlement
      break;
    case 'DID_FAIL_TO_RENEW':
    case 'EXPIRED':
      // Subscription expired
      // Revoke entitlement
      break;
    case 'REFUND':
      // Purchase refunded
      // Revoke rewards
      if (transactionInfo) {
        const purchase = await db.collection('purchases')
          .where('originalTransactionId', '==', transactionInfo.originalTransactionId)
          .limit(1)
          .get();
        
        if (!purchase.empty) {
          await purchase.docs[0].ref.update({
            status: PurchaseStatus.REFUNDED,
            refundedAt: admin.firestore.FieldValue.serverTimestamp()
          });
        }
      }
      break;
  }

  return { success: true };
});

/**
 * Scheduled job: Check and expire subscriptions
 */
export const checkSubscriptionExpiry = onSchedule('every 1 hours', async () => {
  const now = admin.firestore.Timestamp.now();
  
  // Find expired entitlements
  const expiredEntitlements = await db.collection('entitlements')
    .where('isActive', '==', true)
    .where('expiresAt', '<=', now)
    .get();

  const batch = db.batch();
  
  for (const doc of expiredEntitlements.docs) {
    batch.update(doc.ref, { isActive: false });
    
    // If it's VIP, remove daily gem delivery
    const data = doc.data();
    if (data.type === EntitlementType.VIP_SUBSCRIPTION) {
      console.log(`VIP expired for user ${data.userId}`);
    }
  }

  await batch.commit();
  console.log(`Expired ${expiredEntitlements.size} entitlements`);
});

/**
 * Scheduled job: Grant daily VIP rewards
 */
export const grantDailyVIPRewards = onSchedule('0 0 * * *', async () => {
  // Find all active VIP subscribers
  const vipEntitlements = await db.collection('entitlements')
    .where('type', '==', EntitlementType.VIP_SUBSCRIPTION)
    .where('isActive', '==', true)
    .get();

  const vipProduct = PRODUCT_CATALOG.find(p => p.id === 'vip_monthly');
  if (!vipProduct) return;

  const dailyGems = vipProduct.metadata.dailyGems || 100;

  for (const doc of vipEntitlements.docs) {
    const entitlement = doc.data();
    const userId = entitlement.userId;

    // Grant daily gems
    await db.collection('players').doc(userId).update({
      'resources.gems': admin.firestore.FieldValue.increment(dailyGems)
    });

    // Log delivery
    await db.collection('vip_deliveries').add({
      userId,
      gems: dailyGems,
      deliveredAt: admin.firestore.FieldValue.serverTimestamp()
    });
  }

  console.log(`Delivered daily VIP rewards to ${vipEntitlements.size} users`);
});

/**
 * Get revenue analytics
 */
export const getRevenueAnalytics = onCall(async (request) => {
  const adminId = request.auth?.uid;
  if (!adminId) {
    throw new HttpsError('unauthenticated', 'Must be logged in');
  }

  const adminDoc = await db.collection('admins').doc(adminId).get();
  if (!adminDoc.exists) {
    throw new HttpsError('permission-denied', 'Not an admin');
  }

  const { startDate, endDate } = request.data;
  const start = startDate ? new Date(startDate) : new Date(Date.now() - 30 * 24 * 60 * 60 * 1000);
  const end = endDate ? new Date(endDate) : new Date();

  // Get purchases in date range
  const purchasesSnapshot = await db.collection('purchases')
    .where('status', '==', PurchaseStatus.COMPLETED)
    .where('purchasedAt', '>=', admin.firestore.Timestamp.fromDate(start))
    .where('purchasedAt', '<=', admin.firestore.Timestamp.fromDate(end))
    .get();

  let totalRevenue = 0;
  let appleRevenue = 0;
  let googleRevenue = 0;
  const revenueByProduct: Record<string, number> = {};
  const revenueByCategory: Record<string, number> = {};
  const dailyRevenue: Record<string, number> = {};
  const purchaseCount = purchasesSnapshot.size;
  const uniqueBuyers = new Set<string>();

  for (const doc of purchasesSnapshot.docs) {
    const purchase = doc.data();
    const revenue = purchase.priceUSD || 0;
    
    totalRevenue += revenue;
    uniqueBuyers.add(purchase.userId);

    if (purchase.store === 'apple') {
      appleRevenue += revenue;
    } else if (purchase.store === 'google') {
      googleRevenue += revenue;
    }

    // By product
    const productId = purchase.productId;
    revenueByProduct[productId] = (revenueByProduct[productId] || 0) + revenue;

    // By category
    const product = getProduct(productId);
    if (product) {
      const category = product.category;
      revenueByCategory[category] = (revenueByCategory[category] || 0) + revenue;
    }

    // Daily
    const day = purchase.purchasedAt.toDate().toISOString().split('T')[0];
    dailyRevenue[day] = (dailyRevenue[day] || 0) + revenue;
  }

  // Calculate ARPU
  const totalUsers = (await db.collection('players').count().get()).data().count;
  const arpu = totalUsers > 0 ? totalRevenue / totalUsers : 0;
  const arppu = uniqueBuyers.size > 0 ? totalRevenue / uniqueBuyers.size : 0;

  return {
    totalRevenue,
    appleRevenue,
    googleRevenue,
    purchaseCount,
    uniqueBuyers: uniqueBuyers.size,
    arpu,
    arppu,
    revenueByProduct,
    revenueByCategory,
    dailyRevenue
  };
});
