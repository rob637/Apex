/**
 * In-App Purchases System Tests
 * Tests for purchase validation, entitlements, and store integration
 */

// IAP Types
interface Product {
    id: string;
    name: string;
    type: 'consumable' | 'non_consumable' | 'subscription';
    price: number;
    currency: string;
    contents: ProductContent[];
    isActive: boolean;
}

interface ProductContent {
    type: 'currency' | 'resource' | 'cosmetic' | 'vip_time';
    id: string;
    amount: number;
}

interface PurchaseReceipt {
    transactionId: string;
    productId: string;
    purchaseTime: Date;
    store: 'apple' | 'google' | 'test';
    receipt: string;
    status: 'pending' | 'valid' | 'invalid' | 'refunded';
}

interface Entitlement {
    playerId: string;
    productId: string;
    grantedAt: Date;
    expiresAt?: Date;
    source: 'purchase' | 'gift' | 'promotion' | 'admin';
}

interface Subscription {
    playerId: string;
    productId: string;
    status: 'active' | 'cancelled' | 'expired' | 'grace_period';
    startDate: Date;
    renewalDate?: Date;
    expiryDate: Date;
    autoRenew: boolean;
}

// IAP System logic
class IAPSystem {
    static readonly GRACE_PERIOD_DAYS = 7;

    static validateProductPurchase(product: Product, playerId: string): { valid: boolean; error?: string } {
        if (!product.isActive) {
            return { valid: false, error: 'Product not available' };
        }

        return { valid: true };
    }

    static isReceiptValid(receipt: PurchaseReceipt): boolean {
        return receipt.status === 'valid';
    }

    static calculateContents(product: Product, quantity: number = 1): ProductContent[] {
        return product.contents.map(content => ({
            ...content,
            amount: content.amount * quantity
        }));
    }

    static isSubscriptionActive(subscription: Subscription): boolean {
        if (subscription.status === 'active') return true;
        if (subscription.status === 'grace_period') {
            const gracePeriodEnd = new Date(subscription.expiryDate);
            gracePeriodEnd.setDate(gracePeriodEnd.getDate() + this.GRACE_PERIOD_DAYS);
            return new Date() < gracePeriodEnd;
        }
        return false;
    }

    static getSubscriptionDaysRemaining(subscription: Subscription): number {
        const now = new Date();
        const expiry = subscription.expiryDate;
        const diff = expiry.getTime() - now.getTime();
        return Math.max(0, Math.ceil(diff / (1000 * 60 * 60 * 24)));
    }

    static hasEntitlement(entitlements: Entitlement[], productId: string): boolean {
        const now = new Date();
        return entitlements.some(e => 
            e.productId === productId && 
            (!e.expiresAt || e.expiresAt > now)
        );
    }

    static canRestorePurchase(receipt: PurchaseReceipt, existingPurchases: string[]): boolean {
        // Only restore valid non-consumable/subscription purchases
        if (receipt.status !== 'valid') return false;
        if (existingPurchases.includes(receipt.transactionId)) return false;
        return true;
    }

    static calculateBonusForBundle(baseAmount: number, bundleMultiplier: number): { amount: number; bonus: number } {
        const bonus = Math.floor(baseAmount * (bundleMultiplier - 1));
        return {
            amount: baseAmount,
            bonus
        };
    }

    static getFirstPurchaseBonus(isFirstPurchase: boolean, baseAmount: number): number {
        return isFirstPurchase ? Math.floor(baseAmount * 0.5) : 0;
    }

    static validateAppleReceipt(receipt: string): { valid: boolean; productId?: string; error?: string } {
        // Simplified validation - in production this calls Apple's servers
        if (!receipt || receipt.length < 100) {
            return { valid: false, error: 'Invalid receipt format' };
        }

        // Check for test receipt markers
        if (receipt.startsWith('TEST_')) {
            return { valid: true, productId: receipt.split('_')[1] };
        }

        // In production, decode and verify with Apple
        return { valid: true };
    }

    static validateGoogleReceipt(purchaseToken: string, signature: string): { valid: boolean; productId?: string; error?: string } {
        // Simplified validation - in production this calls Google's servers
        if (!purchaseToken || !signature) {
            return { valid: false, error: 'Missing purchase token or signature' };
        }

        // Check for test purchase markers
        if (purchaseToken.startsWith('TEST_')) {
            return { valid: true, productId: purchaseToken.split('_')[1] };
        }

        // In production, verify signature and check with Google Play
        return { valid: true };
    }

    static calculateVIPBonus(vipLevel: number): { resourceBonus: number; xpBonus: number; exclusiveAccess: boolean } {
        return {
            resourceBonus: Math.min(100, vipLevel * 10), // Up to 100% bonus
            xpBonus: Math.min(50, vipLevel * 5), // Up to 50% bonus
            exclusiveAccess: vipLevel >= 5
        };
    }

    static processBundlePurchase(contents: ProductContent[], player: { resources: Record<string, number>; gems: number }): { resources: Record<string, number>; gems: number } {
        const result = { resources: { ...player.resources }, gems: player.gems };

        for (const content of contents) {
            if (content.type === 'currency' && content.id === 'gems') {
                result.gems += content.amount;
            } else if (content.type === 'resource') {
                result.resources[content.id] = (result.resources[content.id] || 0) + content.amount;
            }
        }

        return result;
    }
}

describe('In-App Purchases System', () => {
    describe('Product Validation', () => {
        const activeProduct: Product = {
            id: 'gems_100',
            name: '100 Gems',
            type: 'consumable',
            price: 0.99,
            currency: 'USD',
            contents: [{ type: 'currency', id: 'gems', amount: 100 }],
            isActive: true
        };

        it('should allow purchase of active product', () => {
            const result = IAPSystem.validateProductPurchase(activeProduct, 'player_1');
            expect(result.valid).toBe(true);
        });

        it('should reject inactive product', () => {
            const inactive = { ...activeProduct, isActive: false };
            const result = IAPSystem.validateProductPurchase(inactive, 'player_1');
            expect(result.valid).toBe(false);
            expect(result.error).toBe('Product not available');
        });
    });

    describe('Receipt Validation', () => {
        it('should accept valid receipt', () => {
            const receipt: PurchaseReceipt = {
                transactionId: 'txn_123',
                productId: 'gems_100',
                purchaseTime: new Date(),
                store: 'apple',
                receipt: 'valid_receipt_data',
                status: 'valid'
            };

            expect(IAPSystem.isReceiptValid(receipt)).toBe(true);
        });

        it('should reject invalid receipt', () => {
            const receipt: PurchaseReceipt = {
                transactionId: 'txn_123',
                productId: 'gems_100',
                purchaseTime: new Date(),
                store: 'apple',
                receipt: 'invalid_data',
                status: 'invalid'
            };

            expect(IAPSystem.isReceiptValid(receipt)).toBe(false);
        });

        it('should reject pending receipt', () => {
            const receipt: PurchaseReceipt = {
                transactionId: 'txn_123',
                productId: 'gems_100',
                purchaseTime: new Date(),
                store: 'apple',
                receipt: 'receipt_data',
                status: 'pending'
            };

            expect(IAPSystem.isReceiptValid(receipt)).toBe(false);
        });

        it('should reject refunded receipt', () => {
            const receipt: PurchaseReceipt = {
                transactionId: 'txn_123',
                productId: 'gems_100',
                purchaseTime: new Date(),
                store: 'apple',
                receipt: 'receipt_data',
                status: 'refunded'
            };

            expect(IAPSystem.isReceiptValid(receipt)).toBe(false);
        });
    });

    describe('Content Calculation', () => {
        const product: Product = {
            id: 'starter_pack',
            name: 'Starter Pack',
            type: 'consumable',
            price: 4.99,
            currency: 'USD',
            contents: [
                { type: 'currency', id: 'gems', amount: 100 },
                { type: 'resource', id: 'gold', amount: 5000 }
            ],
            isActive: true
        };

        it('should calculate single purchase contents', () => {
            const contents = IAPSystem.calculateContents(product);
            expect(contents[0].amount).toBe(100);
            expect(contents[1].amount).toBe(5000);
        });

        it('should scale contents for quantity', () => {
            const contents = IAPSystem.calculateContents(product, 3);
            expect(contents[0].amount).toBe(300);
            expect(contents[1].amount).toBe(15000);
        });
    });

    describe('Subscription Status', () => {
        it('should detect active subscription', () => {
            const sub: Subscription = {
                playerId: 'player_1',
                productId: 'vip_monthly',
                status: 'active',
                startDate: new Date(Date.now() - 86400000 * 15),
                renewalDate: new Date(Date.now() + 86400000 * 15),
                expiryDate: new Date(Date.now() + 86400000 * 15),
                autoRenew: true
            };

            expect(IAPSystem.isSubscriptionActive(sub)).toBe(true);
        });

        it('should detect expired subscription', () => {
            const sub: Subscription = {
                playerId: 'player_1',
                productId: 'vip_monthly',
                status: 'expired',
                startDate: new Date(Date.now() - 86400000 * 45),
                expiryDate: new Date(Date.now() - 86400000 * 15),
                autoRenew: false
            };

            expect(IAPSystem.isSubscriptionActive(sub)).toBe(false);
        });

        it('should honor grace period', () => {
            const sub: Subscription = {
                playerId: 'player_1',
                productId: 'vip_monthly',
                status: 'grace_period',
                startDate: new Date(Date.now() - 86400000 * 30),
                expiryDate: new Date(Date.now() - 86400000), // Expired 1 day ago
                autoRenew: true
            };

            // Should still be active (within 7-day grace period)
            expect(IAPSystem.isSubscriptionActive(sub)).toBe(true);
        });

        it('should expire after grace period', () => {
            const sub: Subscription = {
                playerId: 'player_1',
                productId: 'vip_monthly',
                status: 'grace_period',
                startDate: new Date(Date.now() - 86400000 * 40),
                expiryDate: new Date(Date.now() - 86400000 * 10), // Expired 10 days ago
                autoRenew: true
            };

            expect(IAPSystem.isSubscriptionActive(sub)).toBe(false);
        });

        it('should calculate days remaining', () => {
            const sub: Subscription = {
                playerId: 'player_1',
                productId: 'vip_monthly',
                status: 'active',
                startDate: new Date(),
                expiryDate: new Date(Date.now() + 86400000 * 15),
                autoRenew: true
            };

            const days = IAPSystem.getSubscriptionDaysRemaining(sub);
            expect(days).toBe(15);
        });
    });

    describe('Entitlements', () => {
        const entitlements: Entitlement[] = [
            {
                playerId: 'player_1',
                productId: 'premium_skin',
                grantedAt: new Date(Date.now() - 86400000),
                source: 'purchase'
            },
            {
                playerId: 'player_1',
                productId: 'vip_access',
                grantedAt: new Date(Date.now() - 86400000),
                expiresAt: new Date(Date.now() + 86400000 * 30),
                source: 'purchase'
            },
            {
                playerId: 'player_1',
                productId: 'expired_item',
                grantedAt: new Date(Date.now() - 86400000 * 60),
                expiresAt: new Date(Date.now() - 86400000 * 30),
                source: 'promotion'
            }
        ];

        it('should find permanent entitlement', () => {
            expect(IAPSystem.hasEntitlement(entitlements, 'premium_skin')).toBe(true);
        });

        it('should find active timed entitlement', () => {
            expect(IAPSystem.hasEntitlement(entitlements, 'vip_access')).toBe(true);
        });

        it('should not find expired entitlement', () => {
            expect(IAPSystem.hasEntitlement(entitlements, 'expired_item')).toBe(false);
        });

        it('should not find non-existent entitlement', () => {
            expect(IAPSystem.hasEntitlement(entitlements, 'does_not_exist')).toBe(false);
        });
    });

    describe('Purchase Restoration', () => {
        it('should allow restoring valid purchase', () => {
            const receipt: PurchaseReceipt = {
                transactionId: 'txn_123',
                productId: 'premium_skin',
                purchaseTime: new Date(),
                store: 'apple',
                receipt: 'receipt_data',
                status: 'valid'
            };

            expect(IAPSystem.canRestorePurchase(receipt, [])).toBe(true);
        });

        it('should prevent restoring already restored purchase', () => {
            const receipt: PurchaseReceipt = {
                transactionId: 'txn_123',
                productId: 'premium_skin',
                purchaseTime: new Date(),
                store: 'apple',
                receipt: 'receipt_data',
                status: 'valid'
            };

            expect(IAPSystem.canRestorePurchase(receipt, ['txn_123'])).toBe(false);
        });

        it('should prevent restoring invalid purchase', () => {
            const receipt: PurchaseReceipt = {
                transactionId: 'txn_123',
                productId: 'premium_skin',
                purchaseTime: new Date(),
                store: 'apple',
                receipt: 'receipt_data',
                status: 'invalid'
            };

            expect(IAPSystem.canRestorePurchase(receipt, [])).toBe(false);
        });
    });

    describe('Bundle Bonuses', () => {
        it('should calculate bundle bonus', () => {
            const result = IAPSystem.calculateBonusForBundle(100, 1.5);
            expect(result.amount).toBe(100);
            expect(result.bonus).toBe(50);
        });

        it('should handle 2x bundle', () => {
            const result = IAPSystem.calculateBonusForBundle(100, 2.0);
            expect(result.bonus).toBe(100);
        });

        it('should calculate first purchase bonus', () => {
            const bonus = IAPSystem.getFirstPurchaseBonus(true, 100);
            expect(bonus).toBe(50);
        });

        it('should not give bonus for repeat purchase', () => {
            const bonus = IAPSystem.getFirstPurchaseBonus(false, 100);
            expect(bonus).toBe(0);
        });
    });

    describe('Store-Specific Validation', () => {
        it('should validate Apple receipt format', () => {
            const result = IAPSystem.validateAppleReceipt('');
            expect(result.valid).toBe(false);
            expect(result.error).toBe('Invalid receipt format');
        });

        it('should accept test Apple receipt', () => {
            const result = IAPSystem.validateAppleReceipt('TEST_gems_100_' + 'x'.repeat(100));
            expect(result.valid).toBe(true);
            expect(result.productId).toBe('gems');
        });

        it('should validate Google receipt', () => {
            const result = IAPSystem.validateGoogleReceipt('', '');
            expect(result.valid).toBe(false);
            expect(result.error).toBe('Missing purchase token or signature');
        });

        it('should accept test Google receipt', () => {
            const result = IAPSystem.validateGoogleReceipt('TEST_gems_100', 'valid_sig');
            expect(result.valid).toBe(true);
            expect(result.productId).toBe('gems');
        });
    });

    describe('VIP System', () => {
        it('should calculate VIP level 1 bonus', () => {
            const bonus = IAPSystem.calculateVIPBonus(1);
            expect(bonus.resourceBonus).toBe(10);
            expect(bonus.xpBonus).toBe(5);
            expect(bonus.exclusiveAccess).toBe(false);
        });

        it('should calculate VIP level 5 bonus with exclusive access', () => {
            const bonus = IAPSystem.calculateVIPBonus(5);
            expect(bonus.resourceBonus).toBe(50);
            expect(bonus.xpBonus).toBe(25);
            expect(bonus.exclusiveAccess).toBe(true);
        });

        it('should cap bonuses at maximum', () => {
            const bonus = IAPSystem.calculateVIPBonus(15);
            expect(bonus.resourceBonus).toBe(100);
            expect(bonus.xpBonus).toBe(50);
        });
    });

    describe('Bundle Processing', () => {
        const player = {
            resources: { gold: 1000, stone: 500 },
            gems: 50
        };

        const contents: ProductContent[] = [
            { type: 'currency', id: 'gems', amount: 100 },
            { type: 'resource', id: 'gold', amount: 5000 },
            { type: 'resource', id: 'wood', amount: 2000 }
        ];

        it('should add gems correctly', () => {
            const result = IAPSystem.processBundlePurchase(contents, player);
            expect(result.gems).toBe(150);
        });

        it('should add existing resources', () => {
            const result = IAPSystem.processBundlePurchase(contents, player);
            expect(result.resources.gold).toBe(6000);
        });

        it('should add new resources', () => {
            const result = IAPSystem.processBundlePurchase(contents, player);
            expect(result.resources.wood).toBe(2000);
        });

        it('should not modify original player object', () => {
            IAPSystem.processBundlePurchase(contents, player);
            expect(player.gems).toBe(50);
            expect(player.resources.gold).toBe(1000);
        });
    });
});
