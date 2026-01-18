using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.UI;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Hero/Commander Panel - Manage powerful hero units that lead armies and provide bonuses.
    /// Each hero has unique abilities, equipment, and skill trees.
    /// </summary>
    public class HeroPanel : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color accentColor = new Color(0.3f, 0.6f, 0.9f);
        [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color legendaryColor = new Color(0.9f, 0.6f, 0.2f);
        [SerializeField] private Color epicColor = new Color(0.6f, 0.3f, 0.9f);
        [SerializeField] private Color rareColor = new Color(0.3f, 0.6f, 0.9f);
        [SerializeField] private Color commonColor = new Color(0.5f, 0.5f, 0.5f);
        
        // UI Elements
        private GameObject _panel;
        private GameObject _heroListContainer;
        private GameObject _heroDetailContainer;
        private Hero _selectedHero;
        private HeroTab _selectedTab = HeroTab.Overview;
        
        // Hero data
        private List<Hero> _heroes = new List<Hero>();
        private int _maxHeroSlots = 5;
        
        public static HeroPanel Instance { get; private set; }
        
        public event Action<Hero> OnHeroSelected;
        public event Action<Hero, HeroSkill> OnSkillUnlocked;
        public event Action<Hero, EquipmentItem> OnEquipmentChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeHeroes();
        }

        private void Start()
        {
            CreatePanel();
            Hide();
        }

        private void InitializeHeroes()
        {
            // Starter heroes
            _heroes.Add(new Hero
            {
                HeroId = "HERO_001",
                Name = "Sir Aldric",
                Title = "Knight Commander",
                Rarity = HeroRarity.Epic,
                Class = HeroClass.Warrior,
                Level = 15,
                Experience = 4500,
                ExperienceToNext = 6000,
                Health = 2500,
                MaxHealth = 2500,
                Attack = 180,
                Defense = 220,
                Speed = 85,
                Leadership = 150,
                Portrait = "👤",
                IsDeployed = true,
                DeployedTerritory = "Northern Fortress",
                Backstory = "A noble knight who rose through the ranks through valor and honor.",
                Skills = new List<HeroSkill>
                {
                    new HeroSkill { SkillId = "SK_001", Name = "Shield Wall", Description = "+30% defense for nearby troops", Icon = "🛡️", Level = 3, MaxLevel = 5, IsUnlocked = true, IsPassive = true },
                    new HeroSkill { SkillId = "SK_002", Name = "Rally Cry", Description = "+20% attack for 10 seconds", Icon = "📣", Level = 2, MaxLevel = 5, IsUnlocked = true, IsPassive = false, Cooldown = 60 },
                    new HeroSkill { SkillId = "SK_003", Name = "Charge", Description = "Lead cavalry in devastating charge", Icon = "🐴", Level = 0, MaxLevel = 5, IsUnlocked = false, UnlockLevel = 20 },
                    new HeroSkill { SkillId = "SK_004", Name = "Fortress", Description = "+50% territory defense", Icon = "🏰", Level = 0, MaxLevel = 3, IsUnlocked = false, UnlockLevel = 30 }
                },
                Equipment = new HeroEquipment
                {
                    Weapon = new EquipmentItem { Name = "Crusader's Blade", Rarity = "Epic", Icon = "⚔️", BonusAttack = 45, BonusDefense = 15 },
                    Armor = new EquipmentItem { Name = "Plate Armor", Rarity = "Rare", Icon = "🛡️", BonusDefense = 60, BonusHealth = 200 },
                    Accessory = new EquipmentItem { Name = "Commander's Medal", Rarity = "Epic", Icon = "🎖️", BonusLeadership = 25 }
                }
            });
            
            _heroes.Add(new Hero
            {
                HeroId = "HERO_002",
                Name = "Lyra Shadowstep",
                Title = "Master Assassin",
                Rarity = HeroRarity.Legendary,
                Class = HeroClass.Assassin,
                Level = 22,
                Experience = 12000,
                ExperienceToNext = 15000,
                Health = 1800,
                MaxHealth = 1800,
                Attack = 320,
                Defense = 90,
                Speed = 180,
                Leadership = 80,
                Portrait = "🗡️",
                IsDeployed = false,
                Backstory = "A legendary assassin who can strike from the shadows and vanish without a trace.",
                Skills = new List<HeroSkill>
                {
                    new HeroSkill { SkillId = "SK_011", Name = "Shadowstrike", Description = "Critical hit deals 3x damage", Icon = "🌙", Level = 4, MaxLevel = 5, IsUnlocked = true, IsPassive = false, Cooldown = 45 },
                    new HeroSkill { SkillId = "SK_012", Name = "Vanish", Description = "Become invisible for 15 seconds", Icon = "👻", Level = 3, MaxLevel = 5, IsUnlocked = true, IsPassive = false, Cooldown = 90 },
                    new HeroSkill { SkillId = "SK_013", Name = "Poison Blade", Description = "+50% damage over time", Icon = "☠️", Level = 2, MaxLevel = 5, IsUnlocked = true, IsPassive = true },
                    new HeroSkill { SkillId = "SK_014", Name = "Death Mark", Description = "Mark enemy for instant kill", Icon = "💀", Level = 1, MaxLevel = 3, IsUnlocked = true, IsPassive = false, Cooldown = 180 }
                },
                Equipment = new HeroEquipment
                {
                    Weapon = new EquipmentItem { Name = "Nightfall Daggers", Rarity = "Legendary", Icon = "🗡️", BonusAttack = 85, BonusSpeed = 20 },
                    Armor = new EquipmentItem { Name = "Shadow Cloak", Rarity = "Epic", Icon = "🧥", BonusSpeed = 30, BonusDefense = 25 },
                    Accessory = new EquipmentItem { Name = "Ring of Shadows", Rarity = "Legendary", Icon = "💍", BonusAttack = 25, BonusSpeed = 15 }
                }
            });
            
            _heroes.Add(new Hero
            {
                HeroId = "HERO_003",
                Name = "Archon Malakar",
                Title = "Archmage",
                Rarity = HeroRarity.Epic,
                Class = HeroClass.Mage,
                Level = 18,
                Experience = 7200,
                ExperienceToNext = 9000,
                Health = 1600,
                MaxHealth = 1600,
                Attack = 280,
                Defense = 100,
                Speed = 95,
                Leadership = 120,
                Portrait = "🧙",
                IsDeployed = true,
                DeployedTerritory = "Crystal Tower",
                Backstory = "An ancient mage who has mastered the elemental arts and commands fearsome magical power.",
                Skills = new List<HeroSkill>
                {
                    new HeroSkill { SkillId = "SK_021", Name = "Fireball", Description = "AOE fire damage to enemies", Icon = "🔥", Level = 3, MaxLevel = 5, IsUnlocked = true, IsPassive = false, Cooldown = 30 },
                    new HeroSkill { SkillId = "SK_022", Name = "Ice Shield", Description = "+40% defense, slows attackers", Icon = "❄️", Level = 2, MaxLevel = 5, IsUnlocked = true, IsPassive = false, Cooldown = 60 },
                    new HeroSkill { SkillId = "SK_023", Name = "Lightning Storm", Description = "Massive AOE damage", Icon = "⚡", Level = 1, MaxLevel = 5, IsUnlocked = true, IsPassive = false, Cooldown = 120 },
                    new HeroSkill { SkillId = "SK_024", Name = "Arcane Mastery", Description = "+25% all magic damage", Icon = "✨", Level = 0, MaxLevel = 3, IsUnlocked = false, UnlockLevel = 25, IsPassive = true }
                },
                Equipment = new HeroEquipment
                {
                    Weapon = new EquipmentItem { Name = "Staff of Elements", Rarity = "Epic", Icon = "🪄", BonusAttack = 65 },
                    Armor = new EquipmentItem { Name = "Arcane Robes", Rarity = "Rare", Icon = "👘", BonusDefense = 30, BonusHealth = 150 },
                    Accessory = new EquipmentItem { Name = "Crystal Orb", Rarity = "Epic", Icon = "🔮", BonusAttack = 35, BonusLeadership = 15 }
                }
            });
            
            _heroes.Add(new Hero
            {
                HeroId = "HERO_004",
                Name = "Grimjaw",
                Title = "Warlord",
                Rarity = HeroRarity.Rare,
                Class = HeroClass.Berserker,
                Level = 10,
                Experience = 2100,
                ExperienceToNext = 3500,
                Health = 3200,
                MaxHealth = 3200,
                Attack = 250,
                Defense = 150,
                Speed = 70,
                Leadership = 100,
                Portrait = "🪓",
                IsDeployed = false,
                Backstory = "A brutal warrior from the northern wastes who revels in combat.",
                Skills = new List<HeroSkill>
                {
                    new HeroSkill { SkillId = "SK_031", Name = "Rage", Description = "+50% attack, -20% defense", Icon = "😤", Level = 2, MaxLevel = 5, IsUnlocked = true, IsPassive = false, Cooldown = 45 },
                    new HeroSkill { SkillId = "SK_032", Name = "Cleave", Description = "Attack hits multiple enemies", Icon = "🪓", Level = 1, MaxLevel = 5, IsUnlocked = true, IsPassive = false, Cooldown = 20 },
                    new HeroSkill { SkillId = "SK_033", Name = "War Cry", Description = "Fear nearby enemies", Icon = "😱", Level = 0, MaxLevel = 5, IsUnlocked = false, UnlockLevel = 15 },
                    new HeroSkill { SkillId = "SK_034", Name = "Bloodlust", Description = "Heal on kill", Icon = "🩸", Level = 0, MaxLevel = 3, IsUnlocked = false, UnlockLevel = 25, IsPassive = true }
                },
                Equipment = new HeroEquipment
                {
                    Weapon = new EquipmentItem { Name = "Battle Axe", Rarity = "Rare", Icon = "🪓", BonusAttack = 40 },
                    Armor = new EquipmentItem { Name = "Fur Armor", Rarity = "Common", Icon = "🦺", BonusDefense = 35, BonusHealth = 100 }
                }
            });
        }

        private void CreatePanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Main panel
            _panel = new GameObject("HeroPanel");
            _panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, 0.05f);
            rect.anchorMax = new Vector2(0.92f, 0.95f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Image bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.06f, 0.1f, 0.98f);
            
            HorizontalLayoutGroup hlayout = _panel.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(10, 10, 10, 10);
            
            // Left side - Hero list
            CreateHeroList();
            
            // Right side - Hero details
            CreateHeroDetails();
        }

        private void CreateHeroList()
        {
            _heroListContainer = new GameObject("HeroList");
            _heroListContainer.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = _heroListContainer.AddComponent<LayoutElement>();
            le.preferredWidth = 280;
            le.flexibleHeight = 1;
            
            Image bg = _heroListContainer.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f);
            
            VerticalLayoutGroup vlayout = _heroListContainer.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 8;
            vlayout.padding = new RectOffset(10, 10, 10, 10);
            
            // Header
            CreateListHeader();
            
            // Hero cards
            RefreshHeroList();
        }

        private void CreateListHeader()
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(_heroListContainer.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            VerticalLayoutGroup vlayout = header.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            
            CreateText(header.transform, "⚔️ YOUR HEROES", 18, TextAlignmentOptions.Center, accentColor);
            CreateText(header.transform, $"{_heroes.Count}/{_maxHeroSlots} slots", 11, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
        }

        private void RefreshHeroList()
        {
            // Clear existing cards (keep header)
            for (int i = _heroListContainer.transform.childCount - 1; i > 0; i--)
            {
                Destroy(_heroListContainer.transform.GetChild(i).gameObject);
            }
            
            foreach (var hero in _heroes)
            {
                CreateHeroCard(hero);
            }
            
            // Recruit button
            if (_heroes.Count < _maxHeroSlots)
            {
                CreateRecruitButton();
            }
        }

        private void CreateHeroCard(Hero hero)
        {
            GameObject card = new GameObject($"Hero_{hero.HeroId}");
            card.transform.SetParent(_heroListContainer.transform, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            
            Color rarityColor = GetRarityColor(hero.Rarity);
            bool isSelected = _selectedHero?.HeroId == hero.HeroId;
            
            Image bg = card.AddComponent<Image>();
            bg.color = isSelected ? new Color(rarityColor.r * 0.3f, rarityColor.g * 0.3f, rarityColor.b * 0.3f) 
                                 : new Color(0.08f, 0.08f, 0.12f);
            
            if (isSelected)
            {
                UnityEngine.UI.Outline outline = card.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = rarityColor;
                outline.effectDistance = new Vector2(2, 2);
            }
            
            Button btn = card.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectHero(hero));
            
            HorizontalLayoutGroup hlayout = card.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(10, 10, 8, 8);
            
            // Portrait
            GameObject portrait = new GameObject("Portrait");
            portrait.transform.SetParent(card.transform, false);
            
            LayoutElement portraitLE = portrait.AddComponent<LayoutElement>();
            portraitLE.preferredWidth = 50;
            portraitLE.preferredHeight = 50;
            
            Image portraitBg = portrait.AddComponent<Image>();
            portraitBg.color = new Color(0.15f, 0.15f, 0.2f);
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(portrait.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI portraitText = textObj.AddComponent<TextMeshProUGUI>();
            portraitText.text = hero.Portrait;
            portraitText.fontSize = 32;
            portraitText.alignment = TextAlignmentOptions.Center;
            
            // Info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(card.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoVL = info.AddComponent<VerticalLayoutGroup>();
            infoVL.childAlignment = TextAnchor.MiddleLeft;
            infoVL.spacing = 2;
            
            CreateText(info.transform, $"<b>{hero.Name}</b>", 13, TextAlignmentOptions.Left, Color.white);
            CreateText(info.transform, $"Lv.{hero.Level} {hero.Class}", 10, TextAlignmentOptions.Left, GetClassColor(hero.Class));
            CreateText(info.transform, hero.Title, 9, TextAlignmentOptions.Left, rarityColor);
            
            // Status
            if (hero.IsDeployed)
            {
                CreateText(info.transform, $"📍 {hero.DeployedTerritory}", 8, TextAlignmentOptions.Left, new Color(0.4f, 0.7f, 0.4f));
            }
            
            // Stats mini
            GameObject stats = new GameObject("Stats");
            stats.transform.SetParent(card.transform, false);
            
            VerticalLayoutGroup statsVL = stats.AddComponent<VerticalLayoutGroup>();
            statsVL.childAlignment = TextAnchor.MiddleRight;
            
            CreateText(stats.transform, $"⚔️{hero.Attack + (hero.Equipment.Weapon?.BonusAttack ?? 0)}", 10, TextAlignmentOptions.Right, new Color(0.9f, 0.5f, 0.5f));
            CreateText(stats.transform, $"🛡️{hero.Defense + (hero.Equipment.Armor?.BonusDefense ?? 0)}", 10, TextAlignmentOptions.Right, new Color(0.5f, 0.7f, 0.9f));
            CreateText(stats.transform, $"❤️{hero.Health}", 10, TextAlignmentOptions.Right, new Color(0.5f, 0.9f, 0.5f));
        }

        private void CreateRecruitButton()
        {
            GameObject btn = new GameObject("RecruitBtn");
            btn.transform.SetParent(_heroListContainer.transform, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.2f, 0.15f);
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(RecruitHero);
            
            VerticalLayoutGroup vlayout = btn.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            
            CreateText(btn.transform, "➕ Recruit Hero", 14, TextAlignmentOptions.Center, new Color(0.4f, 0.8f, 0.4f));
            CreateText(btn.transform, "5000 Gold", 10, TextAlignmentOptions.Center, goldColor);
        }

        private void CreateHeroDetails()
        {
            _heroDetailContainer = new GameObject("HeroDetails");
            _heroDetailContainer.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = _heroDetailContainer.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.flexibleHeight = 1;
            
            Image bg = _heroDetailContainer.AddComponent<Image>();
            bg.color = new Color(0.07f, 0.07f, 0.1f);
            
            RefreshHeroDetails();
        }

        private void RefreshHeroDetails()
        {
            foreach (Transform child in _heroDetailContainer.transform)
            {
                Destroy(child.gameObject);
            }
            
            if (_selectedHero == null)
            {
                VerticalLayoutGroup vlayout = _heroDetailContainer.AddComponent<VerticalLayoutGroup>();
                vlayout.childAlignment = TextAnchor.MiddleCenter;
                
                CreateText(_heroDetailContainer.transform, "Select a hero to view details", 16, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
                return;
            }
            
            // Remove previous layout if exists
            var existingLayout = _heroDetailContainer.GetComponent<VerticalLayoutGroup>();
            if (existingLayout != null) Destroy(existingLayout);
            
            VerticalLayoutGroup newLayout = _heroDetailContainer.AddComponent<VerticalLayoutGroup>();
            newLayout.childAlignment = TextAnchor.UpperCenter;
            newLayout.childForceExpandWidth = true;
            newLayout.childForceExpandHeight = false;
            newLayout.spacing = 10;
            newLayout.padding = new RectOffset(15, 15, 15, 15);
            
            // Header with close button
            CreateDetailHeader();
            
            // Hero header
            CreateHeroHeader();
            
            // Tabs
            CreateDetailTabs();
            
            // Tab content
            CreateTabContent();
        }

        private void CreateDetailHeader()
        {
            GameObject header = new GameObject("DetailHeader");
            header.transform.SetParent(_heroDetailContainer.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 30;
            
            HorizontalLayoutGroup hlayout = header.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleRight;
            
            // Close button
            GameObject closeBtn = new GameObject("CloseBtn");
            closeBtn.transform.SetParent(header.transform, false);
            
            LayoutElement closele = closeBtn.AddComponent<LayoutElement>();
            closele.preferredWidth = 30;
            closele.preferredHeight = 30;
            
            Image closeBg = closeBtn.AddComponent<Image>();
            closeBg.color = new Color(0.5f, 0.2f, 0.2f, 0.8f);
            
            Button btn = closeBtn.AddComponent<Button>();
            btn.onClick.AddListener(Hide);
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(closeBtn.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI x = textObj.AddComponent<TextMeshProUGUI>();
            x.text = "✕";
            x.fontSize = 18;
            x.alignment = TextAlignmentOptions.Center;
        }

        private void CreateHeroHeader()
        {
            GameObject header = new GameObject("HeroHeader");
            header.transform.SetParent(_heroDetailContainer.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 100;
            
            HorizontalLayoutGroup hlayout = header.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 20;
            hlayout.padding = new RectOffset(20, 20, 10, 10);
            
            // Large portrait
            GameObject portrait = new GameObject("Portrait");
            portrait.transform.SetParent(header.transform, false);
            
            LayoutElement portraitLE = portrait.AddComponent<LayoutElement>();
            portraitLE.preferredWidth = 80;
            portraitLE.preferredHeight = 80;
            
            Color rarityColor = GetRarityColor(_selectedHero.Rarity);
            
            Image portraitBg = portrait.AddComponent<Image>();
            portraitBg.color = new Color(rarityColor.r * 0.3f, rarityColor.g * 0.3f, rarityColor.b * 0.3f);
            
            UnityEngine.UI.Outline portraitOutline = portrait.AddComponent<UnityEngine.UI.Outline>();
            portraitOutline.effectColor = rarityColor;
            portraitOutline.effectDistance = new Vector2(3, 3);
            
            TextMeshProUGUI portraitText = portrait.AddComponent<TextMeshProUGUI>();
            portraitText.text = _selectedHero.Portrait;
            portraitText.fontSize = 48;
            portraitText.alignment = TextAlignmentOptions.Center;
            
            // Info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(header.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoVL = info.AddComponent<VerticalLayoutGroup>();
            infoVL.childAlignment = TextAnchor.MiddleLeft;
            infoVL.spacing = 4;
            
            CreateText(info.transform, $"<b>{_selectedHero.Name}</b>", 22, TextAlignmentOptions.Left, Color.white);
            CreateText(info.transform, _selectedHero.Title, 14, TextAlignmentOptions.Left, rarityColor);
            CreateText(info.transform, $"Level {_selectedHero.Level} {_selectedHero.Rarity} {_selectedHero.Class}", 12, TextAlignmentOptions.Left, new Color(0.7f, 0.7f, 0.7f));
            
            // XP bar
            CreateXPBar(info.transform);
            
            // Quick stats
            GameObject stats = new GameObject("QuickStats");
            stats.transform.SetParent(header.transform, false);
            
            VerticalLayoutGroup statsVL = stats.AddComponent<VerticalLayoutGroup>();
            statsVL.childAlignment = TextAnchor.MiddleRight;
            
            int totalAtk = _selectedHero.Attack + (_selectedHero.Equipment.Weapon?.BonusAttack ?? 0);
            int totalDef = _selectedHero.Defense + (_selectedHero.Equipment.Armor?.BonusDefense ?? 0);
            
            CreateText(stats.transform, $"⚔️ {totalAtk}", 16, TextAlignmentOptions.Right, new Color(0.9f, 0.5f, 0.5f));
            CreateText(stats.transform, $"🛡️ {totalDef}", 16, TextAlignmentOptions.Right, new Color(0.5f, 0.7f, 0.9f));
            CreateText(stats.transform, $"⚡ {_selectedHero.Speed}", 16, TextAlignmentOptions.Right, new Color(0.9f, 0.9f, 0.5f));
            CreateText(stats.transform, $"👑 {_selectedHero.Leadership}", 16, TextAlignmentOptions.Right, goldColor);
        }

        private void CreateXPBar(Transform parent)
        {
            GameObject xpContainer = new GameObject("XPBar");
            xpContainer.transform.SetParent(parent, false);
            
            LayoutElement le = xpContainer.AddComponent<LayoutElement>();
            le.preferredHeight = 15;
            
            HorizontalLayoutGroup hlayout = xpContainer.AddComponent<HorizontalLayoutGroup>();
            hlayout.spacing = 10;
            
            // Bar
            GameObject bar = new GameObject("Bar");
            bar.transform.SetParent(xpContainer.transform, false);
            
            LayoutElement barLE = bar.AddComponent<LayoutElement>();
            barLE.preferredWidth = 200;
            barLE.preferredHeight = 12;
            
            Image barBg = bar.AddComponent<Image>();
            barBg.color = new Color(0.15f, 0.15f, 0.2f);
            
            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(bar.transform, false);
            
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            float xpPct = (float)_selectedHero.Experience / _selectedHero.ExperienceToNext;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(xpPct, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = accentColor;
            
            // Text
            CreateText(xpContainer.transform, $"{_selectedHero.Experience:N0} / {_selectedHero.ExperienceToNext:N0} XP", 10, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
        }

        private void CreateDetailTabs()
        {
            GameObject tabs = new GameObject("Tabs");
            tabs.transform.SetParent(_heroDetailContainer.transform, false);
            
            LayoutElement le = tabs.AddComponent<LayoutElement>();
            le.preferredHeight = 35;
            
            HorizontalLayoutGroup hlayout = tabs.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 5;
            
            CreateDetailTab(tabs.transform, HeroTab.Overview, "📊 Overview");
            CreateDetailTab(tabs.transform, HeroTab.Skills, "⚡ Skills");
            CreateDetailTab(tabs.transform, HeroTab.Equipment, "⚔️ Equipment");
            CreateDetailTab(tabs.transform, HeroTab.Deploy, "📍 Deploy");
        }

        private void CreateDetailTab(Transform parent, HeroTab tab, string label)
        {
            GameObject tabObj = new GameObject($"Tab_{tab}");
            tabObj.transform.SetParent(parent, false);
            
            LayoutElement le = tabObj.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            
            Color bgColor = tab == _selectedTab ? accentColor : new Color(0.15f, 0.15f, 0.2f);
            
            Image bg = tabObj.AddComponent<Image>();
            bg.color = bgColor;
            
            Button btn = tabObj.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                _selectedTab = tab;
                RefreshHeroDetails();
            });
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(tabObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 12;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateTabContent()
        {
            GameObject content = new GameObject("TabContent");
            content.transform.SetParent(_heroDetailContainer.transform, false);
            
            LayoutElement le = content.AddComponent<LayoutElement>();
            le.flexibleHeight = 1;
            
            Image bg = content.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.04f, 0.06f);
            
            VerticalLayoutGroup vlayout = content.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            switch (_selectedTab)
            {
                case HeroTab.Overview:
                    CreateOverviewContent(content.transform);
                    break;
                case HeroTab.Skills:
                    CreateSkillsContent(content.transform);
                    break;
                case HeroTab.Equipment:
                    CreateEquipmentContent(content.transform);
                    break;
                case HeroTab.Deploy:
                    CreateDeployContent(content.transform);
                    break;
            }
        }

        private void CreateOverviewContent(Transform parent)
        {
            // Backstory
            CreateSectionLabel(parent, "📜 BACKSTORY");
            CreateText(parent, _selectedHero.Backstory, 12, TextAlignmentOptions.Left, new Color(0.8f, 0.8f, 0.8f));
            
            // All stats
            CreateSectionLabel(parent, "📊 STATISTICS");
            
            GameObject stats = new GameObject("Stats");
            stats.transform.SetParent(parent, false);
            
            GridLayoutGroup grid = stats.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(150, 30);
            grid.spacing = new Vector2(10, 5);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            
            CreateStatItem(stats.transform, "❤️ Health", $"{_selectedHero.Health:N0} / {_selectedHero.MaxHealth:N0}");
            CreateStatItem(stats.transform, "⚔️ Attack", $"{_selectedHero.Attack}");
            CreateStatItem(stats.transform, "🛡️ Defense", $"{_selectedHero.Defense}");
            CreateStatItem(stats.transform, "⚡ Speed", $"{_selectedHero.Speed}");
            CreateStatItem(stats.transform, "👑 Leadership", $"{_selectedHero.Leadership}");
            CreateStatItem(stats.transform, "⭐ Level", $"{_selectedHero.Level}");
            
            // Action buttons
            CreateSectionLabel(parent, "⚡ ACTIONS");
            
            GameObject actions = new GameObject("Actions");
            actions.transform.SetParent(parent, false);
            
            HorizontalLayoutGroup actionsHL = actions.AddComponent<HorizontalLayoutGroup>();
            actionsHL.childAlignment = TextAnchor.MiddleCenter;
            actionsHL.spacing = 15;
            
            CreateActionButton(actions.transform, "⬆️ Level Up", "1000 Gold", LevelUpHero, accentColor);
            CreateActionButton(actions.transform, "❤️ Heal", "500 Gold", HealHero, new Color(0.3f, 0.7f, 0.3f));
            CreateActionButton(actions.transform, "🔄 Reset Skills", "2000 Gold", ResetSkills, new Color(0.7f, 0.5f, 0.2f));
        }

        private void CreateStatItem(Transform parent, string label, string value)
        {
            GameObject item = new GameObject("Stat");
            item.transform.SetParent(parent, false);
            
            HorizontalLayoutGroup hlayout = item.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleLeft;
            hlayout.spacing = 5;
            
            CreateText(item.transform, label, 11, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
            CreateText(item.transform, value, 12, TextAlignmentOptions.Left, Color.white);
        }

        private void CreateSkillsContent(Transform parent)
        {
            CreateText(parent, $"Skill Points Available: {GetAvailableSkillPoints()}", 14, TextAlignmentOptions.Center, goldColor);
            
            foreach (var skill in _selectedHero.Skills)
            {
                CreateSkillCard(parent, skill);
            }
        }

        private int GetAvailableSkillPoints()
        {
            int totalUsed = 0;
            foreach (var skill in _selectedHero.Skills)
            {
                if (skill.IsUnlocked)
                    totalUsed += skill.Level;
            }
            return _selectedHero.Level - totalUsed;
        }

        private void CreateSkillCard(Transform parent, HeroSkill skill)
        {
            GameObject card = new GameObject($"Skill_{skill.SkillId}");
            card.transform.SetParent(parent, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 70;
            
            Image bg = card.AddComponent<Image>();
            bg.color = skill.IsUnlocked ? new Color(0.1f, 0.12f, 0.18f) : new Color(0.08f, 0.08f, 0.1f);
            
            if (!skill.IsUnlocked)
            {
                CanvasGroup cg = card.AddComponent<CanvasGroup>();
                cg.alpha = 0.5f;
            }
            
            HorizontalLayoutGroup hlayout = card.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 10, 10);
            
            // Icon
            CreateText(card.transform, skill.Icon, 32, TextAlignmentOptions.Center);
            
            // Info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(card.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoVL = info.AddComponent<VerticalLayoutGroup>();
            infoVL.childAlignment = TextAnchor.MiddleLeft;
            infoVL.spacing = 3;
            
            string typeTag = skill.IsPassive ? "[Passive]" : $"[Active - {skill.Cooldown}s CD]";
            CreateText(info.transform, $"<b>{skill.Name}</b> {typeTag}", 13, TextAlignmentOptions.Left, Color.white);
            CreateText(info.transform, skill.Description, 10, TextAlignmentOptions.Left, new Color(0.7f, 0.7f, 0.7f));
            
            // Level indicator
            string levelStr = skill.IsUnlocked 
                ? $"Level {skill.Level}/{skill.MaxLevel}" 
                : $"Unlocks at Hero Lv.{skill.UnlockLevel}";
            CreateText(info.transform, levelStr, 10, TextAlignmentOptions.Left, skill.IsUnlocked ? accentColor : new Color(0.5f, 0.5f, 0.5f));
            
            // Upgrade button
            if (skill.IsUnlocked && skill.Level < skill.MaxLevel && GetAvailableSkillPoints() > 0)
            {
                CreateSmallButton(card.transform, "⬆️ Upgrade", () => UpgradeSkill(skill), accentColor);
            }
        }

        private void CreateEquipmentContent(Transform parent)
        {
            CreateSectionLabel(parent, "⚔️ EQUIPPED ITEMS");
            
            CreateEquipmentSlot(parent, "Weapon", _selectedHero.Equipment.Weapon, "🗡️");
            CreateEquipmentSlot(parent, "Armor", _selectedHero.Equipment.Armor, "🛡️");
            CreateEquipmentSlot(parent, "Accessory", _selectedHero.Equipment.Accessory, "💍");
            
            CreateSectionLabel(parent, "📊 EQUIPMENT BONUSES");
            
            GameObject bonuses = new GameObject("Bonuses");
            bonuses.transform.SetParent(parent, false);
            
            HorizontalLayoutGroup hlayout = bonuses.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 20;
            
            int totalAtkBonus = (_selectedHero.Equipment.Weapon?.BonusAttack ?? 0) +
                               (_selectedHero.Equipment.Armor?.BonusAttack ?? 0) +
                               (_selectedHero.Equipment.Accessory?.BonusAttack ?? 0);
            int totalDefBonus = (_selectedHero.Equipment.Weapon?.BonusDefense ?? 0) +
                               (_selectedHero.Equipment.Armor?.BonusDefense ?? 0) +
                               (_selectedHero.Equipment.Accessory?.BonusDefense ?? 0);
            
            CreateText(bonuses.transform, $"+{totalAtkBonus} Attack", 14, TextAlignmentOptions.Center, new Color(0.9f, 0.5f, 0.5f));
            CreateText(bonuses.transform, $"+{totalDefBonus} Defense", 14, TextAlignmentOptions.Center, new Color(0.5f, 0.7f, 0.9f));
        }

        private void CreateEquipmentSlot(Transform parent, string slotName, EquipmentItem item, string emptyIcon)
        {
            GameObject slot = new GameObject($"Slot_{slotName}");
            slot.transform.SetParent(parent, false);
            
            LayoutElement le = slot.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
            
            Image bg = slot.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f);
            
            HorizontalLayoutGroup hlayout = slot.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 10, 10);
            
            // Slot type
            CreateText(slot.transform, slotName, 12, TextAlignmentOptions.Left, new Color(0.5f, 0.5f, 0.5f));
            
            if (item != null)
            {
                CreateText(slot.transform, item.Icon, 24, TextAlignmentOptions.Center);
                
                GameObject info = new GameObject("Info");
                info.transform.SetParent(slot.transform, false);
                
                LayoutElement infoLE = info.AddComponent<LayoutElement>();
                infoLE.flexibleWidth = 1;
                
                VerticalLayoutGroup infoVL = info.AddComponent<VerticalLayoutGroup>();
                infoVL.childAlignment = TextAnchor.MiddleLeft;
                
                CreateText(info.transform, item.Name, 13, TextAlignmentOptions.Left, GetItemRarityColor(item.Rarity));
                
                string bonusStr = "";
                if (item.BonusAttack > 0) bonusStr += $"+{item.BonusAttack} ATK ";
                if (item.BonusDefense > 0) bonusStr += $"+{item.BonusDefense} DEF ";
                if (item.BonusHealth > 0) bonusStr += $"+{item.BonusHealth} HP ";
                if (item.BonusSpeed > 0) bonusStr += $"+{item.BonusSpeed} SPD ";
                if (item.BonusLeadership > 0) bonusStr += $"+{item.BonusLeadership} LDR ";
                
                CreateText(info.transform, bonusStr.Trim(), 10, TextAlignmentOptions.Left, new Color(0.5f, 0.8f, 0.5f));
                
                CreateSmallButton(slot.transform, "Change", () => ChangeEquipment(slotName), accentColor);
            }
            else
            {
                CreateText(slot.transform, emptyIcon, 24, TextAlignmentOptions.Center, new Color(0.3f, 0.3f, 0.3f));
                CreateText(slot.transform, "Empty", 12, TextAlignmentOptions.Center, new Color(0.4f, 0.4f, 0.4f));
                CreateSmallButton(slot.transform, "Equip", () => ChangeEquipment(slotName), new Color(0.3f, 0.5f, 0.3f));
            }
        }

        private void CreateDeployContent(Transform parent)
        {
            if (_selectedHero.IsDeployed)
            {
                CreateText(parent, $"Currently deployed at: {_selectedHero.DeployedTerritory}", 14, TextAlignmentOptions.Center, new Color(0.4f, 0.8f, 0.4f));
                
                CreateActionButton(parent, $"{GameIcons.Flag} Recall Hero", "Free", RecallHero, new Color(0.6f, 0.4f, 0.2f));
            }
            else
            {
                CreateText(parent, "Select a territory to deploy this hero:", 14, TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f));
            }
            
            CreateSectionLabel(parent, $"{GameIcons.Map} AVAILABLE TERRITORIES");
            
            // Sample territories
            CreateTerritoryOption(parent, "Northern Fortress", false);
            CreateTerritoryOption(parent, "Crystal Tower", _selectedHero.HeroId == "HERO_003");
            CreateTerritoryOption(parent, "Eastern Watchtower", false);
            CreateTerritoryOption(parent, "Southern Stronghold", false);
        }

        private void CreateTerritoryOption(Transform parent, string territoryName, bool hasHero)
        {
            GameObject option = new GameObject($"Territory_{territoryName}");
            option.transform.SetParent(parent, false);
            
            LayoutElement le = option.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            Image bg = option.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.1f, 0.12f);
            
            HorizontalLayoutGroup hlayout = option.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 5, 5);
            
            CreateText(option.transform, "🏰", 20, TextAlignmentOptions.Center);
            
            GameObject info = new GameObject("Info");
            info.transform.SetParent(option.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            CreateText(info.transform, territoryName, 13, TextAlignmentOptions.Left, Color.white);
            
            if (hasHero)
            {
                CreateText(option.transform, "Hero deployed", 10, TextAlignmentOptions.Right, new Color(0.4f, 0.8f, 0.4f));
            }
            else if (!_selectedHero.IsDeployed)
            {
                CreateSmallButton(option.transform, "Deploy", () => DeployHero(territoryName), accentColor);
            }
        }

        #region UI Helpers

        private void CreateSectionLabel(Transform parent, string text)
        {
            GameObject label = new GameObject("Label");
            label.transform.SetParent(parent, false);
            
            LayoutElement le = label.AddComponent<LayoutElement>();
            le.preferredHeight = 25;
            
            TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 12;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = accentColor;
        }

        private GameObject CreateText(Transform parent, string text, int fontSize, TextAlignmentOptions alignment, Color? color = null)
        {
            GameObject obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color ?? Color.white;
            tmp.enableWordWrapping = true;
            
            return obj;
        }

        private void CreateSmallButton(Transform parent, string label, Action onClick, Color color)
        {
            GameObject btn = new GameObject($"Btn_{label}");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 70;
            le.preferredHeight = 28;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = color;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btn.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 11;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateActionButton(Transform parent, string label, string cost, Action onClick, Color color)
        {
            GameObject btn = new GameObject($"ActionBtn_{label}");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 120;
            le.preferredHeight = 50;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = color;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            
            VerticalLayoutGroup vlayout = btn.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            
            CreateText(btn.transform, label, 12, TextAlignmentOptions.Center, Color.white);
            CreateText(btn.transform, cost, 10, TextAlignmentOptions.Center, goldColor);
        }

        private Color GetRarityColor(HeroRarity rarity)
        {
            return rarity switch
            {
                HeroRarity.Common => commonColor,
                HeroRarity.Rare => rareColor,
                HeroRarity.Epic => epicColor,
                HeroRarity.Legendary => legendaryColor,
                _ => Color.white
            };
        }

        private Color GetClassColor(HeroClass heroClass)
        {
            return heroClass switch
            {
                HeroClass.Warrior => new Color(0.8f, 0.4f, 0.3f),
                HeroClass.Mage => new Color(0.5f, 0.4f, 0.9f),
                HeroClass.Assassin => new Color(0.3f, 0.3f, 0.3f),
                HeroClass.Berserker => new Color(0.9f, 0.3f, 0.3f),
                HeroClass.Ranger => new Color(0.3f, 0.7f, 0.3f),
                HeroClass.Healer => new Color(0.3f, 0.8f, 0.8f),
                _ => Color.white
            };
        }

        private Color GetItemRarityColor(string rarity)
        {
            return rarity?.ToLower() switch
            {
                "common" => commonColor,
                "rare" => rareColor,
                "epic" => epicColor,
                "legendary" => legendaryColor,
                _ => Color.white
            };
        }

        #endregion

        #region Hero Actions

        private void SelectHero(Hero hero)
        {
            _selectedHero = hero;
            _selectedTab = HeroTab.Overview;
            
            RefreshHeroList();
            RefreshHeroDetails();
            
            OnHeroSelected?.Invoke(hero);
            Debug.Log($"[Hero] Selected: {hero.Name}");
        }

        private void RecruitHero()
        {
            Debug.Log("[Hero] Opening hero recruitment...");
            // TODO: Implement hero recruitment gacha/shop
        }

        private void LevelUpHero()
        {
            if (_selectedHero == null) return;
            
            _selectedHero.Level++;
            _selectedHero.Attack += 5;
            _selectedHero.Defense += 3;
            _selectedHero.MaxHealth += 50;
            _selectedHero.Health = _selectedHero.MaxHealth;
            
            RefreshHeroDetails();
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowSuccess($"{_selectedHero.Name} leveled up to {_selectedHero.Level}!");
            }
            
            Debug.Log($"[Hero] {_selectedHero.Name} leveled up to {_selectedHero.Level}");
        }

        private void HealHero()
        {
            if (_selectedHero == null) return;
            
            _selectedHero.Health = _selectedHero.MaxHealth;
            RefreshHeroDetails();
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowSuccess($"{_selectedHero.Name} fully healed!");
            }
        }

        private void ResetSkills()
        {
            if (_selectedHero == null) return;
            
            foreach (var skill in _selectedHero.Skills)
            {
                if (skill.IsUnlocked)
                    skill.Level = 0;
            }
            
            RefreshHeroDetails();
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowInfo($"{_selectedHero.Name}'s skills have been reset.");
            }
        }

        private void UpgradeSkill(HeroSkill skill)
        {
            if (GetAvailableSkillPoints() <= 0 || skill.Level >= skill.MaxLevel) return;
            
            skill.Level++;
            RefreshHeroDetails();
            
            OnSkillUnlocked?.Invoke(_selectedHero, skill);
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowSuccess($"{skill.Name} upgraded to level {skill.Level}!");
            }
        }

        private void ChangeEquipment(string slotName)
        {
            Debug.Log($"[Hero] Opening equipment selection for {slotName}...");
            // TODO: Open equipment inventory
        }

        private void DeployHero(string territoryName)
        {
            if (_selectedHero == null) return;
            
            _selectedHero.IsDeployed = true;
            _selectedHero.DeployedTerritory = territoryName;
            
            RefreshHeroList();
            RefreshHeroDetails();
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowSuccess($"{_selectedHero.Name} deployed to {territoryName}!");
            }
        }

        private void RecallHero()
        {
            if (_selectedHero == null) return;
            
            _selectedHero.IsDeployed = false;
            _selectedHero.DeployedTerritory = null;
            
            RefreshHeroList();
            RefreshHeroDetails();
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowInfo($"{_selectedHero.Name} recalled from deployment.");
            }
        }

        #endregion

        #region Public API

        public void Show()
        {
            _panel.SetActive(true);
            RefreshHeroList();
            RefreshHeroDetails();
        }

        public void Hide()
        {
            _panel.SetActive(false);
        }

        public void Toggle()
        {
            if (_panel.activeSelf)
                Hide();
            else
                Show();
        }

        public List<Hero> GetAllHeroes() => _heroes;
        public Hero GetSelectedHero() => _selectedHero;

        public Hero GetHeroById(string heroId)
        {
            return _heroes.Find(h => h.HeroId == heroId);
        }

        #endregion
    }

    #region Data Classes

    public enum HeroTab
    {
        Overview,
        Skills,
        Equipment,
        Deploy
    }

    public enum HeroRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    public enum HeroClass
    {
        Warrior,
        Mage,
        Assassin,
        Berserker,
        Ranger,
        Healer
    }

    public class Hero
    {
        public string HeroId;
        public string Name;
        public string Title;
        public HeroRarity Rarity;
        public HeroClass Class;
        public int Level;
        public int Experience;
        public int ExperienceToNext;
        public int Health;
        public int MaxHealth;
        public int Attack;
        public int Defense;
        public int Speed;
        public int Leadership;
        public string Portrait;
        public bool IsDeployed;
        public string DeployedTerritory;
        public string Backstory;
        public List<HeroSkill> Skills;
        public HeroEquipment Equipment;
    }

    public class HeroSkill
    {
        public string SkillId;
        public string Name;
        public string Description;
        public string Icon;
        public int Level;
        public int MaxLevel;
        public bool IsUnlocked;
        public int UnlockLevel;
        public bool IsPassive;
        public int Cooldown;
    }

    public class HeroEquipment
    {
        public EquipmentItem Weapon;
        public EquipmentItem Armor;
        public EquipmentItem Accessory;
    }

    public class EquipmentItem
    {
        public string Name;
        public string Rarity;
        public string Icon;
        public int BonusAttack;
        public int BonusDefense;
        public int BonusHealth;
        public int BonusSpeed;
        public int BonusLeadership;
    }

    #endregion
}
