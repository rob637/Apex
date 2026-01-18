// ============================================================================
// APEX CITADELS - VIP & PREMIUM PANEL
// Premium subscriptions, VIP benefits, and exclusive features
// ============================================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using ApexCitadels.PC;

namespace ApexCitadels.PC.UI
{
    // VIP tier levels
    public enum VIPTier
    {
        None,
        Bronze,
        Silver,
        Gold,
        Platinum,
        Diamond
    }

    // Premium feature categories
    public enum PremiumCategory
    {
        Boosts,
        Cosmetics,
        Convenience,
        Exclusive,
        Support
    }

    // Subscription types
    public enum SubscriptionType
    {
        Monthly,
        Quarterly,
        Annual,
        Lifetime
    }

    // Premium item data
    [System.Serializable]
    public class PremiumItem
    {
        public string itemId;
        public string name;
        public string description;
        public PremiumCategory category;
        public VIPTier requiredTier;
        public int gemCost;
        public bool isOwned;
        public bool isActive;
        public string iconEmoji;
        public float bonusValue;
        public string bonusType;
    }

    // VIP tier benefits
    [System.Serializable]
    public class VIPTierBenefits
    {
        public VIPTier tier;
        public string tierName;
        public Color tierColor;
        public int monthlyGems;
        public float xpBoost;
        public float resourceBoost;
        public float buildSpeedBoost;
        public int extraQueueSlots;
        public bool autoCollect;
        public bool skipAds;
        public bool prioritySupport;
        public bool exclusiveBadge;
        public bool exclusiveChat;
        public int monthlyPrice;
        public List<string> exclusiveItems = new List<string>();
    }

    // Player VIP status
    [System.Serializable]
    public class VIPStatus
    {
        public VIPTier currentTier;
        public DateTime subscriptionStart;
        public DateTime subscriptionEnd;
        public int totalGemsReceived;
        public int totalDaysPremium;
        public bool isAutoRenew;
        public List<string> ownedPremiumItems = new List<string>();
    }

    public class VIPPremiumPanel : MonoBehaviour
    {
        public static VIPPremiumPanel Instance { get; private set; }

        [Header("Runtime References")]
        private Canvas parentCanvas;
        private GameObject panelRoot;
        private bool isVisible = false;

        // UI References
        private RectTransform mainPanel;
        private RectTransform headerPanel;
        private RectTransform contentPanel;
        private RectTransform tierComparePanel;
        private RectTransform benefitsPanel;
        private RectTransform shopPanel;
        private ScrollRect shopScroll;
        private TextMeshProUGUI headerTitle;
        private TextMeshProUGUI currentTierText;
        private TextMeshProUGUI subscriptionText;
        private TextMeshProUGUI gemsText;

        // State
        private VIPStatus playerStatus;
        private List<VIPTierBenefits> allTiers = new List<VIPTierBenefits>();
        private List<PremiumItem> premiumItems = new List<PremiumItem>();
        private List<GameObject> tierCards = new List<GameObject>();
        private List<GameObject> shopItems = new List<GameObject>();
        private int currentTab = 0; // 0 = Tiers, 1 = Shop, 2 = My Benefits

        // Colors
        private readonly Color NONE_COLOR = new Color(0.5f, 0.5f, 0.5f);
        private readonly Color BRONZE_COLOR = new Color(0.8f, 0.5f, 0.2f);
        private readonly Color SILVER_COLOR = new Color(0.75f, 0.75f, 0.8f);
        private readonly Color GOLD_COLOR = new Color(1f, 0.85f, 0.2f);
        private readonly Color PLATINUM_COLOR = new Color(0.6f, 0.8f, 0.9f);
        private readonly Color DIAMOND_COLOR = new Color(0.7f, 0.9f, 1f);

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            parentCanvas = FindFirstObjectByType<Canvas>();
            if (parentCanvas != null)
            {
                InitTierData();
                InitPremiumItems();
                CreateUI();
                Hide();
            }
        }

        private void Update()
        {
            // Toggle with P key (for Premium)
            if (Input.GetKeyDown(KeyCode.P))
            {
                if (isVisible) Hide();
                else Show();
            }
        }

        private void InitTierData()
        {
            allTiers.Clear();

            // Bronze tier
            allTiers.Add(new VIPTierBenefits
            {
                tier = VIPTier.Bronze,
                tierName = "Bronze",
                tierColor = BRONZE_COLOR,
                monthlyGems = 100,
                xpBoost = 10f,
                resourceBoost = 5f,
                buildSpeedBoost = 5f,
                extraQueueSlots = 1,
                autoCollect = false,
                skipAds = true,
                prioritySupport = false,
                exclusiveBadge = true,
                exclusiveChat = false,
                monthlyPrice = 499,
                exclusiveItems = new List<string> { "bronze_frame", "bronze_title" }
            });

            // Silver tier
            allTiers.Add(new VIPTierBenefits
            {
                tier = VIPTier.Silver,
                tierName = "Silver",
                tierColor = SILVER_COLOR,
                monthlyGems = 300,
                xpBoost = 20f,
                resourceBoost = 15f,
                buildSpeedBoost = 10f,
                extraQueueSlots = 2,
                autoCollect = true,
                skipAds = true,
                prioritySupport = false,
                exclusiveBadge = true,
                exclusiveChat = true,
                monthlyPrice = 999,
                exclusiveItems = new List<string> { "silver_frame", "silver_title", "silver_pet" }
            });

            // Gold tier
            allTiers.Add(new VIPTierBenefits
            {
                tier = VIPTier.Gold,
                tierName = "Gold",
                tierColor = GOLD_COLOR,
                monthlyGems = 600,
                xpBoost = 30f,
                resourceBoost = 25f,
                buildSpeedBoost = 20f,
                extraQueueSlots = 3,
                autoCollect = true,
                skipAds = true,
                prioritySupport = true,
                exclusiveBadge = true,
                exclusiveChat = true,
                monthlyPrice = 1999,
                exclusiveItems = new List<string> { "gold_frame", "gold_title", "gold_pet", "gold_mount", "gold_effects" }
            });

            // Platinum tier
            allTiers.Add(new VIPTierBenefits
            {
                tier = VIPTier.Platinum,
                tierName = "Platinum",
                tierColor = PLATINUM_COLOR,
                monthlyGems = 1000,
                xpBoost = 50f,
                resourceBoost = 40f,
                buildSpeedBoost = 30f,
                extraQueueSlots = 5,
                autoCollect = true,
                skipAds = true,
                prioritySupport = true,
                exclusiveBadge = true,
                exclusiveChat = true,
                monthlyPrice = 3999,
                exclusiveItems = new List<string> { "platinum_frame", "platinum_title", "platinum_pet", "platinum_mount", "platinum_effects", "platinum_emotes" }
            });

            // Diamond tier
            allTiers.Add(new VIPTierBenefits
            {
                tier = VIPTier.Diamond,
                tierName = "Diamond",
                tierColor = DIAMOND_COLOR,
                monthlyGems = 2000,
                xpBoost = 100f,
                resourceBoost = 75f,
                buildSpeedBoost = 50f,
                extraQueueSlots = 10,
                autoCollect = true,
                skipAds = true,
                prioritySupport = true,
                exclusiveBadge = true,
                exclusiveChat = true,
                monthlyPrice = 9999,
                exclusiveItems = new List<string> { "diamond_everything" }
            });

            // Mock player status
            playerStatus = new VIPStatus
            {
                currentTier = VIPTier.Silver,
                subscriptionStart = DateTime.Now.AddDays(-45),
                subscriptionEnd = DateTime.Now.AddDays(15),
                totalGemsReceived = 900,
                totalDaysPremium = 45,
                isAutoRenew = true,
                ownedPremiumItems = new List<string> { "silver_frame", "silver_title" }
            };
        }

        private void InitPremiumItems()
        {
            premiumItems.Clear();

            // Boosts
            premiumItems.Add(new PremiumItem
            {
                itemId = "boost_xp_24h",
                name = "24-Hour XP Boost",
                description = "Double XP for 24 hours",
                category = PremiumCategory.Boosts,
                requiredTier = VIPTier.None,
                gemCost = 50,
                iconEmoji = "⚡",
                bonusValue = 100f,
                bonusType = "xp"
            });

            premiumItems.Add(new PremiumItem
            {
                itemId = "boost_resource_24h",
                name = "24-Hour Resource Boost",
                description = "+50% resource production for 24 hours",
                category = PremiumCategory.Boosts,
                requiredTier = VIPTier.None,
                gemCost = 75,
                iconEmoji = "💎",
                bonusValue = 50f,
                bonusType = "resource"
            });

            premiumItems.Add(new PremiumItem
            {
                itemId = "boost_build_speed",
                name = "Instant Build",
                description = "Complete one building instantly",
                category = PremiumCategory.Boosts,
                requiredTier = VIPTier.None,
                gemCost = 100,
                iconEmoji = "🏗️",
                bonusValue = 100f,
                bonusType = "build"
            });

            // Cosmetics
            premiumItems.Add(new PremiumItem
            {
                itemId = "cosmetic_legendary_frame",
                name = "Legendary Frame",
                description = "Epic animated profile frame",
                category = PremiumCategory.Cosmetics,
                requiredTier = VIPTier.Gold,
                gemCost = 500,
                iconEmoji = "🖼️"
            });

            premiumItems.Add(new PremiumItem
            {
                itemId = "cosmetic_dragon_pet",
                name = "Baby Dragon Pet",
                description = "A fierce companion that follows you",
                category = PremiumCategory.Cosmetics,
                requiredTier = VIPTier.Platinum,
                gemCost = 1000,
                iconEmoji = "🐉"
            });

            premiumItems.Add(new PremiumItem
            {
                itemId = "cosmetic_particle_aura",
                name = "Celestial Aura",
                description = "Glowing particles surround your avatar",
                category = PremiumCategory.Cosmetics,
                requiredTier = VIPTier.Gold,
                gemCost = 300,
                iconEmoji = "✨"
            });

            // Convenience
            premiumItems.Add(new PremiumItem
            {
                itemId = "convenience_auto_collect",
                name = "Auto-Collector (7 days)",
                description = "Automatically collect resources",
                category = PremiumCategory.Convenience,
                requiredTier = VIPTier.Bronze,
                gemCost = 150,
                iconEmoji = "🤖"
            });

            premiumItems.Add(new PremiumItem
            {
                itemId = "convenience_extra_queue",
                name = "Extra Build Queue",
                description = "Permanent +1 building queue slot",
                category = PremiumCategory.Convenience,
                requiredTier = VIPTier.Silver,
                gemCost = 500,
                iconEmoji = "📋"
            });

            premiumItems.Add(new PremiumItem
            {
                itemId = "convenience_teleport",
                name = "Teleport Scroll (x5)",
                description = "Instantly relocate your citadel",
                category = PremiumCategory.Convenience,
                requiredTier = VIPTier.None,
                gemCost = 250,
                iconEmoji = "🌀"
            });

            // Exclusive
            premiumItems.Add(new PremiumItem
            {
                itemId = "exclusive_legendary_hero",
                name = "Legendary Hero: Phoenix Knight",
                description = "Exclusive premium hero with unique abilities",
                category = PremiumCategory.Exclusive,
                requiredTier = VIPTier.Diamond,
                gemCost = 2500,
                iconEmoji = "🦸"
            });

            premiumItems.Add(new PremiumItem
            {
                itemId = "exclusive_founders_pack",
                name = "Founder's Pack",
                description = "Exclusive items only for early supporters",
                category = PremiumCategory.Exclusive,
                requiredTier = VIPTier.None,
                gemCost = 9999,
                iconEmoji = "👑"
            });
        }

        private void CreateUI()
        {
            // Panel root
            panelRoot = new GameObject("VIPPremiumPanel_Root");
            panelRoot.transform.SetParent(parentCanvas.transform, false);

            mainPanel = panelRoot.AddComponent<RectTransform>();
            mainPanel.anchorMin = new Vector2(0.1f, 0.08f);
            mainPanel.anchorMax = new Vector2(0.9f, 0.92f);
            mainPanel.offsetMin = Vector2.zero;
            mainPanel.offsetMax = Vector2.zero;

            // Background with gradient effect
            Image mainBg = panelRoot.AddComponent<Image>();
            mainBg.color = new Color(0.1f, 0.08f, 0.15f, 0.98f);

            // Add layout
            VerticalLayoutGroup layout = panelRoot.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(15, 15, 15, 15);
            layout.spacing = 10;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            CreateHeader();
            CreateStatusBar();
            CreateTabBar();
            CreateContentArea();
            CreateFooter();

            RefreshUI();
        }

        private void CreateHeader()
        {
            // Header container
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(panelRoot.transform, false);

            headerPanel = headerObj.AddComponent<RectTransform>();
            LayoutElement headerLE = headerObj.AddComponent<LayoutElement>();
            headerLE.preferredHeight = 70;
            headerLE.flexibleWidth = 1;

            // Gradient background
            Image headerBg = headerObj.AddComponent<Image>();
            headerBg.color = new Color(0.2f, 0.15f, 0.3f, 0.9f);

            HorizontalLayoutGroup headerLayout = headerObj.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(20, 20, 10, 10);
            headerLayout.spacing = 20;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;

            // Crown icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(headerObj.transform, false);
            TextMeshProUGUI iconTMP = iconObj.AddComponent<TextMeshProUGUI>();
            iconTMP.text = "👑";
            iconTMP.fontSize = 40;
            iconTMP.alignment = TextAlignmentOptions.Center;
            LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 50;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerObj.transform, false);
            headerTitle = titleObj.AddComponent<TextMeshProUGUI>();
            headerTitle.text = "VIP & PREMIUM";
            headerTitle.fontSize = 32;
            headerTitle.fontStyle = FontStyles.Bold;
            headerTitle.color = GOLD_COLOR;
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.flexibleWidth = 1;

            // Gem count
            GameObject gemsObj = new GameObject("Gems");
            gemsObj.transform.SetParent(headerObj.transform, false);
            gemsText = gemsObj.AddComponent<TextMeshProUGUI>();
            gemsText.text = "💎 1,250";
            gemsText.fontSize = 20;
            gemsText.fontStyle = FontStyles.Bold;
            gemsText.color = new Color(0.5f, 0.8f, 1f);
            gemsText.alignment = TextAlignmentOptions.Right;
            LayoutElement gemsLE = gemsObj.AddComponent<LayoutElement>();
            gemsLE.preferredWidth = 120;

            // Close button
            GameObject closeObj = new GameObject("CloseBtn");
            closeObj.transform.SetParent(headerObj.transform, false);
            Image closeBg = closeObj.AddComponent<Image>();
            closeBg.color = new Color(0.8f, 0.2f, 0.2f);
            Button closeBtn = closeObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(Hide);
            LayoutElement closeLE = closeObj.AddComponent<LayoutElement>();
            closeLE.preferredWidth = 45;
            closeLE.preferredHeight = 45;

            GameObject closeText = new GameObject("X");
            closeText.transform.SetParent(closeObj.transform, false);
            TextMeshProUGUI closeTMP = closeText.AddComponent<TextMeshProUGUI>();
            closeTMP.text = "✕";
            closeTMP.fontSize = 26;
            closeTMP.alignment = TextAlignmentOptions.Center;
            closeTMP.color = Color.white;
            RectTransform closeTextRT = closeText.GetComponent<RectTransform>();
            closeTextRT.anchorMin = Vector2.zero;
            closeTextRT.anchorMax = Vector2.one;
            closeTextRT.offsetMin = Vector2.zero;
            closeTextRT.offsetMax = Vector2.zero;
        }

        private void CreateStatusBar()
        {
            GameObject statusObj = new GameObject("StatusBar");
            statusObj.transform.SetParent(panelRoot.transform, false);

            RectTransform statusRT = statusObj.AddComponent<RectTransform>();
            LayoutElement statusLE = statusObj.AddComponent<LayoutElement>();
            statusLE.preferredHeight = 50;
            statusLE.flexibleWidth = 1;

            Image statusBg = statusObj.AddComponent<Image>();
            statusBg.color = GetTierColor(playerStatus.currentTier) * 0.3f;

            HorizontalLayoutGroup statusLayout = statusObj.AddComponent<HorizontalLayoutGroup>();
            statusLayout.padding = new RectOffset(20, 20, 10, 10);
            statusLayout.spacing = 30;
            statusLayout.childAlignment = TextAnchor.MiddleLeft;
            statusLayout.childForceExpandWidth = false;

            // Current tier
            GameObject tierObj = new GameObject("Tier");
            tierObj.transform.SetParent(statusObj.transform, false);
            currentTierText = tierObj.AddComponent<TextMeshProUGUI>();
            currentTierText.text = $"Current Tier: {playerStatus.currentTier}";
            currentTierText.fontSize = 18;
            currentTierText.fontStyle = FontStyles.Bold;
            currentTierText.color = GetTierColor(playerStatus.currentTier);
            LayoutElement tierLE = tierObj.AddComponent<LayoutElement>();
            tierLE.preferredWidth = 200;

            // Subscription status
            GameObject subObj = new GameObject("Subscription");
            subObj.transform.SetParent(statusObj.transform, false);
            subscriptionText = subObj.AddComponent<TextMeshProUGUI>();
            
            int daysLeft = (playerStatus.subscriptionEnd - DateTime.Now).Days;
            subscriptionText.text = daysLeft > 0 
                ? $"📅 {daysLeft} days remaining" 
                : "⚠️ Subscription expired";
            subscriptionText.fontSize = 14;
            subscriptionText.color = daysLeft > 7 
                ? new Color(0.5f, 0.8f, 0.5f) 
                : new Color(1f, 0.6f, 0.3f);
            LayoutElement subLE = subObj.AddComponent<LayoutElement>();
            subLE.flexibleWidth = 1;

            // Auto-renew indicator
            if (playerStatus.isAutoRenew)
            {
                GameObject renewObj = new GameObject("AutoRenew");
                renewObj.transform.SetParent(statusObj.transform, false);
                TextMeshProUGUI renewTMP = renewObj.AddComponent<TextMeshProUGUI>();
                renewTMP.text = "🔄 Auto-renew ON";
                renewTMP.fontSize = 12;
                renewTMP.color = new Color(0.5f, 0.7f, 0.5f);
                LayoutElement renewLE = renewObj.AddComponent<LayoutElement>();
                renewLE.preferredWidth = 120;
            }
        }

        private void CreateTabBar()
        {
            GameObject tabBar = new GameObject("TabBar");
            tabBar.transform.SetParent(panelRoot.transform, false);

            RectTransform tabRT = tabBar.AddComponent<RectTransform>();
            LayoutElement tabLE = tabBar.AddComponent<LayoutElement>();
            tabLE.preferredHeight = 45;
            tabLE.flexibleWidth = 1;

            HorizontalLayoutGroup tabLayout = tabBar.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 5;
            tabLayout.childForceExpandWidth = true;
            tabLayout.childForceExpandHeight = true;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = true;

            string[] tabNames = { "👑 VIP Tiers", "🛒 Premium Shop", "🎁 My Benefits" };

            for (int i = 0; i < tabNames.Length; i++)
            {
                int index = i;
                CreateTabButton(tabBar.transform, tabNames[i], () => SwitchTab(index));
            }
        }

        private void CreateTabButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject(text + "Tab");
            btnObj.transform.SetParent(parent, false);

            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = new Color(0.2f, 0.15f, 0.25f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.2f, 0.15f, 0.25f);
            colors.highlightedColor = new Color(0.3f, 0.2f, 0.35f);
            colors.pressedColor = new Color(0.4f, 0.25f, 0.45f);
            colors.selectedColor = new Color(0.35f, 0.25f, 0.4f);
            btn.colors = colors;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
        }

        private void CreateContentArea()
        {
            // Content container
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(panelRoot.transform, false);

            contentPanel = contentObj.AddComponent<RectTransform>();
            LayoutElement contentLE = contentObj.AddComponent<LayoutElement>();
            contentLE.flexibleHeight = 1;
            contentLE.flexibleWidth = 1;

            Image contentBg = contentObj.AddComponent<Image>();
            contentBg.color = new Color(0.08f, 0.06f, 0.1f);

            // Scroll view for content
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(contentObj.transform, false);

            RectTransform scrollRT = scrollObj.AddComponent<RectTransform>();
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = new Vector2(10, 10);
            scrollRT.offsetMax = new Vector2(-10, -10);

            shopScroll = scrollObj.AddComponent<ScrollRect>();
            shopScroll.horizontal = false;
            shopScroll.vertical = true;
            shopScroll.movementType = ScrollRect.MovementType.Elastic;

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform viewportRT = viewport.AddComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero;
            viewportRT.offsetMax = Vector2.zero;

            Image viewportMask = viewport.AddComponent<Image>();
            viewportMask.color = Color.white;
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            shopScroll.viewport = viewportRT;

            // Content container for scroll
            GameObject scrollContent = new GameObject("ScrollContent");
            scrollContent.transform.SetParent(viewport.transform, false);
            RectTransform scrollContentRT = scrollContent.AddComponent<RectTransform>();
            scrollContentRT.anchorMin = new Vector2(0, 1);
            scrollContentRT.anchorMax = new Vector2(1, 1);
            scrollContentRT.pivot = new Vector2(0.5f, 1);
            scrollContentRT.offsetMin = Vector2.zero;
            scrollContentRT.offsetMax = Vector2.zero;

            VerticalLayoutGroup scrollLayout = scrollContent.AddComponent<VerticalLayoutGroup>();
            scrollLayout.padding = new RectOffset(10, 10, 10, 10);
            scrollLayout.spacing = 15;
            scrollLayout.childForceExpandWidth = true;
            scrollLayout.childForceExpandHeight = false;
            scrollLayout.childControlWidth = true;
            scrollLayout.childControlHeight = true;

            ContentSizeFitter csf = scrollContent.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            shopScroll.content = scrollContentRT;

            // Create tier comparison section
            tierComparePanel = CreateTierComparisonPanel(scrollContent.transform);

            // Create shop section (hidden initially)
            shopPanel = CreateShopPanel(scrollContent.transform);
            shopPanel.gameObject.SetActive(false);

            // Create benefits section (hidden initially)
            benefitsPanel = CreateBenefitsPanel(scrollContent.transform);
            benefitsPanel.gameObject.SetActive(false);
        }

        private RectTransform CreateTierComparisonPanel(Transform parent)
        {
            GameObject panelObj = new GameObject("TierComparison");
            panelObj.transform.SetParent(parent, false);

            RectTransform panelRT = panelObj.AddComponent<RectTransform>();
            LayoutElement panelLE = panelObj.AddComponent<LayoutElement>();
            panelLE.flexibleWidth = 1;

            VerticalLayoutGroup panelLayout = panelObj.AddComponent<VerticalLayoutGroup>();
            panelLayout.spacing = 15;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panelObj.transform, false);
            TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "👑 CHOOSE YOUR VIP TIER";
            titleTMP.fontSize = 22;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = GOLD_COLOR;
            titleTMP.alignment = TextAlignmentOptions.Center;
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 35;

            // Tier cards container
            GameObject cardsContainer = new GameObject("TierCards");
            cardsContainer.transform.SetParent(panelObj.transform, false);

            RectTransform cardsRT = cardsContainer.AddComponent<RectTransform>();
            LayoutElement cardsLE = cardsContainer.AddComponent<LayoutElement>();
            cardsLE.preferredHeight = 350;
            cardsLE.flexibleWidth = 1;

            HorizontalLayoutGroup cardsLayout = cardsContainer.AddComponent<HorizontalLayoutGroup>();
            cardsLayout.spacing = 15;
            cardsLayout.childForceExpandWidth = true;
            cardsLayout.childForceExpandHeight = true;
            cardsLayout.childControlWidth = true;
            cardsLayout.childControlHeight = true;

            // Create tier cards
            foreach (var tier in allTiers)
            {
                CreateTierCard(cardsContainer.transform, tier);
            }

            return panelRT;
        }

        private void CreateTierCard(Transform parent, VIPTierBenefits tier)
        {
            GameObject cardObj = new GameObject(tier.tierName + "Card");
            cardObj.transform.SetParent(parent, false);

            RectTransform cardRT = cardObj.AddComponent<RectTransform>();

            // Background with tier color
            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.color = tier.tierColor * 0.15f;

            // Border
            Outline cardOutline = cardObj.AddComponent<Outline>();
            cardOutline.effectColor = tier.tierColor;
            cardOutline.effectDistance = new Vector2(2, -2);

            VerticalLayoutGroup cardLayout = cardObj.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(10, 10, 10, 10);
            cardLayout.spacing = 8;
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            // Current indicator
            if (tier.tier == playerStatus.currentTier)
            {
                GameObject currentObj = new GameObject("Current");
                currentObj.transform.SetParent(cardObj.transform, false);
                TextMeshProUGUI currentTMP = currentObj.AddComponent<TextMeshProUGUI>();
                currentTMP.text = "★ CURRENT ★";
                currentTMP.fontSize = 11;
                currentTMP.fontStyle = FontStyles.Bold;
                currentTMP.color = new Color(0.3f, 1f, 0.3f);
                currentTMP.alignment = TextAlignmentOptions.Center;
                LayoutElement currentLE = currentObj.AddComponent<LayoutElement>();
                currentLE.preferredHeight = 18;
            }

            // Tier name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(cardObj.transform, false);
            TextMeshProUGUI nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
            nameTMP.text = tier.tierName;
            nameTMP.fontSize = 20;
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.color = tier.tierColor;
            nameTMP.alignment = TextAlignmentOptions.Center;
            LayoutElement nameLE = nameObj.AddComponent<LayoutElement>();
            nameLE.preferredHeight = 28;

            // Price
            GameObject priceObj = new GameObject("Price");
            priceObj.transform.SetParent(cardObj.transform, false);
            TextMeshProUGUI priceTMP = priceObj.AddComponent<TextMeshProUGUI>();
            priceTMP.text = $"${tier.monthlyPrice / 100f:F2}/mo";
            priceTMP.fontSize = 16;
            priceTMP.color = Color.white;
            priceTMP.alignment = TextAlignmentOptions.Center;
            LayoutElement priceLE = priceObj.AddComponent<LayoutElement>();
            priceLE.preferredHeight = 22;

            // Benefits list
            string benefits = $"💎 {tier.monthlyGems} gems/mo\n";
            benefits += $"⚡ +{tier.xpBoost}% XP\n";
            benefits += $"📦 +{tier.resourceBoost}% Resources\n";
            benefits += $"🏗️ +{tier.buildSpeedBoost}% Build Speed\n";
            benefits += $"📋 +{tier.extraQueueSlots} Queue Slots\n";
            if (tier.skipAds) benefits += "🚫 No Ads\n";
            if (tier.autoCollect) benefits += "🤖 Auto-Collect\n";
            if (tier.prioritySupport) benefits += "⭐ Priority Support\n";
            if (tier.exclusiveChat) benefits += "💬 VIP Chat\n";

            GameObject benefitsObj = new GameObject("Benefits");
            benefitsObj.transform.SetParent(cardObj.transform, false);
            TextMeshProUGUI benefitsTMP = benefitsObj.AddComponent<TextMeshProUGUI>();
            benefitsTMP.text = benefits;
            benefitsTMP.fontSize = 11;
            benefitsTMP.color = new Color(0.8f, 0.8f, 0.8f);
            benefitsTMP.alignment = TextAlignmentOptions.Left;
            LayoutElement benefitsLE = benefitsObj.AddComponent<LayoutElement>();
            benefitsLE.flexibleHeight = 1;

            // Subscribe button
            bool canUpgrade = (int)tier.tier > (int)playerStatus.currentTier;
            
            GameObject btnObj = new GameObject("SubscribeBtn");
            btnObj.transform.SetParent(cardObj.transform, false);
            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = canUpgrade ? tier.tierColor : new Color(0.3f, 0.3f, 0.3f);
            Button btn = btnObj.AddComponent<Button>();
            btn.interactable = canUpgrade;
            btn.onClick.AddListener(() => SubscribeToTier(tier));
            LayoutElement btnLE = btnObj.AddComponent<LayoutElement>();
            btnLE.preferredHeight = 35;

            GameObject btnText = new GameObject("Text");
            btnText.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI btnTMP = btnText.AddComponent<TextMeshProUGUI>();
            btnTMP.text = canUpgrade ? "UPGRADE" : "CURRENT";
            btnTMP.fontSize = 14;
            btnTMP.fontStyle = FontStyles.Bold;
            btnTMP.alignment = TextAlignmentOptions.Center;
            btnTMP.color = Color.white;
            RectTransform btnTextRT = btnText.GetComponent<RectTransform>();
            btnTextRT.anchorMin = Vector2.zero;
            btnTextRT.anchorMax = Vector2.one;
            btnTextRT.offsetMin = Vector2.zero;
            btnTextRT.offsetMax = Vector2.zero;

            tierCards.Add(cardObj);
        }

        private RectTransform CreateShopPanel(Transform parent)
        {
            GameObject panelObj = new GameObject("Shop");
            panelObj.transform.SetParent(parent, false);

            RectTransform panelRT = panelObj.AddComponent<RectTransform>();
            LayoutElement panelLE = panelObj.AddComponent<LayoutElement>();
            panelLE.flexibleWidth = 1;

            VerticalLayoutGroup panelLayout = panelObj.AddComponent<VerticalLayoutGroup>();
            panelLayout.spacing = 15;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panelObj.transform, false);
            TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "🛒 PREMIUM SHOP";
            titleTMP.fontSize = 22;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = new Color(0.5f, 0.8f, 1f);
            titleTMP.alignment = TextAlignmentOptions.Center;
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 35;

            // Create categories
            var categories = new[] { PremiumCategory.Boosts, PremiumCategory.Cosmetics, PremiumCategory.Convenience, PremiumCategory.Exclusive };
            
            foreach (var category in categories)
            {
                CreateShopCategory(panelObj.transform, category);
            }

            return panelRT;
        }

        private void CreateShopCategory(Transform parent, PremiumCategory category)
        {
            var items = premiumItems.FindAll(i => i.category == category);
            if (items.Count == 0) return;

            // Category header
            GameObject headerObj = new GameObject(category.ToString() + "Header");
            headerObj.transform.SetParent(parent, false);
            TextMeshProUGUI headerTMP = headerObj.AddComponent<TextMeshProUGUI>();
            headerTMP.text = $"── {category} ──";
            headerTMP.fontSize = 16;
            headerTMP.fontStyle = FontStyles.Bold;
            headerTMP.color = GetCategoryColor(category);
            headerTMP.alignment = TextAlignmentOptions.Center;
            LayoutElement headerLE = headerObj.AddComponent<LayoutElement>();
            headerLE.preferredHeight = 25;

            // Items container
            GameObject itemsContainer = new GameObject(category.ToString() + "Items");
            itemsContainer.transform.SetParent(parent, false);

            RectTransform itemsRT = itemsContainer.AddComponent<RectTransform>();
            LayoutElement itemsLE = itemsContainer.AddComponent<LayoutElement>();
            itemsLE.preferredHeight = 100 * Mathf.CeilToInt(items.Count / 3f);
            itemsLE.flexibleWidth = 1;

            GridLayoutGroup gridLayout = itemsContainer.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(200, 90);
            gridLayout.spacing = new Vector2(10, 10);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;

            foreach (var item in items)
            {
                CreateShopItem(itemsContainer.transform, item);
            }
        }

        private void CreateShopItem(Transform parent, PremiumItem item)
        {
            GameObject itemObj = new GameObject(item.name);
            itemObj.transform.SetParent(parent, false);

            RectTransform itemRT = itemObj.AddComponent<RectTransform>();

            bool canBuy = (int)item.requiredTier <= (int)playerStatus.currentTier && !item.isOwned;
            
            Image itemBg = itemObj.AddComponent<Image>();
            itemBg.color = canBuy ? new Color(0.15f, 0.12f, 0.2f) : new Color(0.1f, 0.1f, 0.1f);

            Button itemBtn = itemObj.AddComponent<Button>();
            itemBtn.interactable = canBuy;
            itemBtn.onClick.AddListener(() => PurchaseItem(item));

            VerticalLayoutGroup itemLayout = itemObj.AddComponent<VerticalLayoutGroup>();
            itemLayout.padding = new RectOffset(8, 8, 5, 5);
            itemLayout.spacing = 3;
            itemLayout.childForceExpandWidth = true;
            itemLayout.childForceExpandHeight = false;

            // Name row
            GameObject nameRow = new GameObject("NameRow");
            nameRow.transform.SetParent(itemObj.transform, false);
            LayoutElement nameRowLE = nameRow.AddComponent<LayoutElement>();
            nameRowLE.preferredHeight = 22;

            HorizontalLayoutGroup nameRowLayout = nameRow.AddComponent<HorizontalLayoutGroup>();
            nameRowLayout.spacing = 5;
            nameRowLayout.childForceExpandWidth = false;

            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(nameRow.transform, false);
            TextMeshProUGUI iconTMP = iconObj.AddComponent<TextMeshProUGUI>();
            iconTMP.text = item.iconEmoji;
            iconTMP.fontSize = 18;
            LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 25;

            // Name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(nameRow.transform, false);
            TextMeshProUGUI nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
            nameTMP.text = item.name;
            nameTMP.fontSize = 12;
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.color = Color.white;
            LayoutElement nameLE = nameObj.AddComponent<LayoutElement>();
            nameLE.flexibleWidth = 1;

            // Description
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(itemObj.transform, false);
            TextMeshProUGUI descTMP = descObj.AddComponent<TextMeshProUGUI>();
            descTMP.text = item.description;
            descTMP.fontSize = 10;
            descTMP.color = new Color(0.7f, 0.7f, 0.7f);
            LayoutElement descLE = descObj.AddComponent<LayoutElement>();
            descLE.preferredHeight = 25;

            // Price/status
            GameObject priceObj = new GameObject("Price");
            priceObj.transform.SetParent(itemObj.transform, false);
            TextMeshProUGUI priceTMP = priceObj.AddComponent<TextMeshProUGUI>();

            if (item.isOwned)
            {
                priceTMP.text = "✓ OWNED";
                priceTMP.color = new Color(0.3f, 0.8f, 0.3f);
            }
            else if ((int)item.requiredTier > (int)playerStatus.currentTier)
            {
                priceTMP.text = $"🔒 Requires {item.requiredTier}";
                priceTMP.color = new Color(0.6f, 0.4f, 0.4f);
            }
            else
            {
                priceTMP.text = $"💎 {item.gemCost}";
                priceTMP.color = new Color(0.5f, 0.8f, 1f);
            }
            
            priceTMP.fontSize = 12;
            priceTMP.fontStyle = FontStyles.Bold;
            LayoutElement priceLE = priceObj.AddComponent<LayoutElement>();
            priceLE.preferredHeight = 18;

            shopItems.Add(itemObj);
        }

        private RectTransform CreateBenefitsPanel(Transform parent)
        {
            GameObject panelObj = new GameObject("Benefits");
            panelObj.transform.SetParent(parent, false);

            RectTransform panelRT = panelObj.AddComponent<RectTransform>();
            LayoutElement panelLE = panelObj.AddComponent<LayoutElement>();
            panelLE.flexibleWidth = 1;

            VerticalLayoutGroup panelLayout = panelObj.AddComponent<VerticalLayoutGroup>();
            panelLayout.spacing = 15;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panelObj.transform, false);
            TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "🎁 YOUR ACTIVE BENEFITS";
            titleTMP.fontSize = 22;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = new Color(0.5f, 1f, 0.5f);
            titleTMP.alignment = TextAlignmentOptions.Center;
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 35;

            // Current benefits based on tier
            var currentTierBenefits = allTiers.Find(t => t.tier == playerStatus.currentTier);
            if (currentTierBenefits != null)
            {
                CreateBenefitRow(panelObj.transform, "💎 Monthly Gems", $"{currentTierBenefits.monthlyGems}/month");
                CreateBenefitRow(panelObj.transform, "⚡ XP Boost", $"+{currentTierBenefits.xpBoost}%");
                CreateBenefitRow(panelObj.transform, "📦 Resource Boost", $"+{currentTierBenefits.resourceBoost}%");
                CreateBenefitRow(panelObj.transform, "🏗️ Build Speed", $"+{currentTierBenefits.buildSpeedBoost}%");
                CreateBenefitRow(panelObj.transform, "📋 Queue Slots", $"+{currentTierBenefits.extraQueueSlots}");
                CreateBenefitRow(panelObj.transform, "🚫 Ad-Free", currentTierBenefits.skipAds ? "Active" : "Inactive");
                CreateBenefitRow(panelObj.transform, "🤖 Auto-Collect", currentTierBenefits.autoCollect ? "Active" : "Inactive");
            }

            // Stats
            GameObject statsObj = new GameObject("Stats");
            statsObj.transform.SetParent(panelObj.transform, false);
            TextMeshProUGUI statsTMP = statsObj.AddComponent<TextMeshProUGUI>();
            statsTMP.text = $"📊 Your VIP Stats:\n\n" +
                $"  • Total days as VIP: {playerStatus.totalDaysPremium}\n" +
                $"  • Total gems received: {playerStatus.totalGemsReceived}\n" +
                $"  • Premium items owned: {playerStatus.ownedPremiumItems.Count}";
            statsTMP.fontSize = 14;
            statsTMP.color = new Color(0.8f, 0.8f, 0.8f);
            LayoutElement statsLE = statsObj.AddComponent<LayoutElement>();
            statsLE.preferredHeight = 100;

            return panelRT;
        }

        private void CreateBenefitRow(Transform parent, string label, string value)
        {
            GameObject rowObj = new GameObject(label);
            rowObj.transform.SetParent(parent, false);

            RectTransform rowRT = rowObj.AddComponent<RectTransform>();
            LayoutElement rowLE = rowObj.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 30;
            rowLE.flexibleWidth = 1;

            Image rowBg = rowObj.AddComponent<Image>();
            rowBg.color = new Color(0.12f, 0.1f, 0.15f);

            HorizontalLayoutGroup rowLayout = rowObj.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(15, 15, 5, 5);

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(rowObj.transform, false);
            TextMeshProUGUI labelTMP = labelObj.AddComponent<TextMeshProUGUI>();
            labelTMP.text = label;
            labelTMP.fontSize = 14;
            labelTMP.color = Color.white;
            LayoutElement labelLE = labelObj.AddComponent<LayoutElement>();
            labelLE.flexibleWidth = 1;

            // Value
            GameObject valueObj = new GameObject("Value");
            valueObj.transform.SetParent(rowObj.transform, false);
            TextMeshProUGUI valueTMP = valueObj.AddComponent<TextMeshProUGUI>();
            valueTMP.text = value;
            valueTMP.fontSize = 14;
            valueTMP.fontStyle = FontStyles.Bold;
            valueTMP.color = GetTierColor(playerStatus.currentTier);
            valueTMP.alignment = TextAlignmentOptions.Right;
            LayoutElement valueLE = valueObj.AddComponent<LayoutElement>();
            valueLE.preferredWidth = 150;
        }

        private void CreateFooter()
        {
            GameObject footerObj = new GameObject("Footer");
            footerObj.transform.SetParent(panelRoot.transform, false);

            RectTransform footerRT = footerObj.AddComponent<RectTransform>();
            LayoutElement footerLE = footerObj.AddComponent<LayoutElement>();
            footerLE.preferredHeight = 50;
            footerLE.flexibleWidth = 1;

            Image footerBg = footerObj.AddComponent<Image>();
            footerBg.color = new Color(0.1f, 0.08f, 0.12f);

            HorizontalLayoutGroup footerLayout = footerObj.AddComponent<HorizontalLayoutGroup>();
            footerLayout.padding = new RectOffset(20, 20, 10, 10);
            footerLayout.spacing = 15;
            footerLayout.childAlignment = TextAnchor.MiddleCenter;
            footerLayout.childForceExpandWidth = false;

            // Buy gems button
            CreateFooterButton(footerObj.transform, "💎 Buy Gems", BuyGems, new Color(0.3f, 0.5f, 0.7f));
            
            // Redeem code button
            CreateFooterButton(footerObj.transform, "🎟️ Redeem Code", RedeemCode, new Color(0.4f, 0.4f, 0.5f));
            
            // Gift premium button
            CreateFooterButton(footerObj.transform, "🎁 Gift Premium", GiftPremium, new Color(0.5f, 0.4f, 0.5f));
            
            // Manage subscription
            CreateFooterButton(footerObj.transform, "⚙️ Manage", ManageSubscription, new Color(0.3f, 0.3f, 0.4f));
        }

        private void CreateFooterButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick, Color color)
        {
            GameObject btnObj = new GameObject(text);
            btnObj.transform.SetParent(parent, false);

            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = color;

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            LayoutElement btnLE = btnObj.AddComponent<LayoutElement>();
            btnLE.preferredWidth = 150;
            btnLE.preferredHeight = 35;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 13;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
        }

        private Color GetTierColor(VIPTier tier)
        {
            return tier switch
            {
                VIPTier.None => NONE_COLOR,
                VIPTier.Bronze => BRONZE_COLOR,
                VIPTier.Silver => SILVER_COLOR,
                VIPTier.Gold => GOLD_COLOR,
                VIPTier.Platinum => PLATINUM_COLOR,
                VIPTier.Diamond => DIAMOND_COLOR,
                _ => Color.white
            };
        }

        private Color GetCategoryColor(PremiumCategory category)
        {
            return category switch
            {
                PremiumCategory.Boosts => new Color(1f, 0.7f, 0.3f),
                PremiumCategory.Cosmetics => new Color(0.8f, 0.4f, 0.9f),
                PremiumCategory.Convenience => new Color(0.4f, 0.7f, 0.9f),
                PremiumCategory.Exclusive => new Color(1f, 0.85f, 0.2f),
                PremiumCategory.Support => new Color(0.5f, 0.8f, 0.5f),
                _ => Color.white
            };
        }

        private void RefreshUI()
        {
            currentTierText.text = $"Current Tier: {playerStatus.currentTier}";
            currentTierText.color = GetTierColor(playerStatus.currentTier);
        }

        private void SwitchTab(int tab)
        {
            currentTab = tab;
            
            tierComparePanel.gameObject.SetActive(tab == 0);
            shopPanel.gameObject.SetActive(tab == 1);
            benefitsPanel.gameObject.SetActive(tab == 2);
            
            Debug.Log($"[VIP] Switched to tab: {tab}");
        }

        // Action handlers
        private void SubscribeToTier(VIPTierBenefits tier)
        {
            Debug.Log($"[VIP] Subscribe to {tier.tierName} at ${tier.monthlyPrice / 100f:F2}/month");
        }

        private void PurchaseItem(PremiumItem item)
        {
            Debug.Log($"[VIP] Purchase {item.name} for {item.gemCost} gems");
        }

        private void BuyGems()
        {
            Debug.Log("[VIP] Opening gem purchase dialog");
        }

        private void RedeemCode()
        {
            Debug.Log("[VIP] Opening redeem code dialog");
        }

        private void GiftPremium()
        {
            Debug.Log("[VIP] Opening gift premium dialog");
        }

        private void ManageSubscription()
        {
            Debug.Log("[VIP] Opening subscription management");
        }

        // Public API
        public void Show()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                isVisible = true;
                RefreshUI();
            }
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
                isVisible = false;
            }
        }

        public void Toggle()
        {
            if (isVisible) Hide();
            else Show();
        }

        public VIPTier GetCurrentTier() => playerStatus.currentTier;
        public bool HasBenefit(string benefitId) => playerStatus.ownedPremiumItems.Contains(benefitId);
        
        public float GetXPBoost()
        {
            var tier = allTiers.Find(t => t.tier == playerStatus.currentTier);
            return tier?.xpBoost ?? 0f;
        }

        public float GetResourceBoost()
        {
            var tier = allTiers.Find(t => t.tier == playerStatus.currentTier);
            return tier?.resourceBoost ?? 0f;
        }

        public float GetBuildSpeedBoost()
        {
            var tier = allTiers.Find(t => t.tier == playerStatus.currentTier);
            return tier?.buildSpeedBoost ?? 0f;
        }
    }
}
