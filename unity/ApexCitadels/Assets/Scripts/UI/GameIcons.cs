// ============================================================================
// APEX CITADELS - GAME ICONS SYSTEM
// Professional icon system using TMP Sprite Assets
// Provides consistent, stylized icons throughout the game
// ============================================================================
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using ApexCitadels.Core;

namespace ApexCitadels.UI
{
    /// <summary>
    /// Centralized icon management system for Apex Citadels.
    /// Uses TMP Sprite Assets for pixel-perfect, stylized icons.
    /// 
    /// Usage: GameIcons.Get(GameIcons.Icon.Gold) returns "<sprite name=\"gold\">"
    /// In TMP text: $"{GameIcons.Gold} 1,500" renders as [gold icon] 1,500
    /// </summary>
    public static class GameIcons
    {
        // ====================================================================
        // ICON DEFINITIONS
        // ====================================================================
        
        public enum Icon
        {
            // Resources
            Gold,
            Gems,
            Crystals,
            ApexCoins,
            Metal,
            Wood,
            Stone,
            Food,
            XP,
            Gem,
            Coin,
            Chest,
            
            // Combat & Military
            Sword,
            Shield,
            Crossed_Swords,
            Helmet,
            Arrow,
            Cannon,
            Battle,
            Victory,
            Defeat,
            Troops,
            
            // Territories & Buildings
            Flag,
            Castle,
            Tower,
            Wall,
            Construction,
            Home,
            Territory,
            Citadel,
            Build,
            Hammer,
            Paint,
            Globe,
            
            // UI & Status
            Star,
            Star_Filled,
            Trophy,
            Medal,
            Crown,
            Heart,
            Clock,
            Calendar,
            Lock,
            Unlock,
            Progress,
            Level,
            Rank,
            BronzeMedal,
            SilverMedal,
            GoldMedal,
            
            // Social
            Alliance,
            Handshake,
            Chat,
            Gift,
            Mail,
            Peace,
            
            // Actions
            Attack,
            Defend,
            Scout,
            Upgrade,
            Collect,
            Search,
            Close,
            
            // Notifications
            Alert,
            Info,
            Success,
            Warning,
            Bell,
            
            // Misc
            Settings,
            Map,
            Compass,
            Eye,
            Party,
            Fire,
            Lightning,
            Scroll,
            Target,
            Leaderboard,
            Statistics,
            History,
            Refresh,
            Play,
            Pause,
            Stop,
            Check,
            Cross
        }

        // ====================================================================
        // QUICK ACCESS PROPERTIES (Most commonly used icons)
        // ====================================================================
        
        // Resources
        public static string Gold => Get(Icon.Gold);
        public static string Gems => Get(Icon.Gems);
        public static string Crystals => Get(Icon.Crystals);
        public static string ApexCoins => Get(Icon.ApexCoins);
        public static string XP => Get(Icon.XP);
        public static string Gem => Get(Icon.Gem);
        public static string Chest => Get(Icon.Chest);
        public static string Stone => Get(Icon.Stone);
        public static string Metal => Get(Icon.Metal);
        public static string Wood => Get(Icon.Wood);
        public static string Food => Get(Icon.Food);
        
        // Combat
        public static string Sword => Get(Icon.Sword);
        public static string Shield => Get(Icon.Shield);
        public static string CrossedSwords => Get(Icon.Crossed_Swords);
        public static string Battle => Get(Icon.Battle);
        public static string Victory => Get(Icon.Victory);
        public static string Defeat => Get(Icon.Defeat);
        public static string Troops => Get(Icon.Troops);
        
        // Territory
        public static string Flag => Get(Icon.Flag);
        public static string Castle => Get(Icon.Castle);
        public static string Construction => Get(Icon.Construction);
        public static string Home => Get(Icon.Home);
        public static string Territory => Get(Icon.Territory);
        public static string Citadel => Get(Icon.Citadel);
        public static string Build => Get(Icon.Build);
        public static string Hammer => Get(Icon.Hammer);
        public static string Paint => Get(Icon.Paint);
        public static string Globe => Get(Icon.Globe);
        
        // Status
        public static string Star => Get(Icon.Star);
        public static string StarFilled => Get(Icon.Star_Filled);
        public static string Trophy => Get(Icon.Trophy);
        public static string Medal => Get(Icon.Medal);
        public static string Crown => Get(Icon.Crown);
        public static string Clock => Get(Icon.Clock);
        public static string Calendar => Get(Icon.Calendar);
        public static string Lock => Get(Icon.Lock);
        public static string BronzeMedal => Get(Icon.BronzeMedal);
        public static string SilverMedal => Get(Icon.SilverMedal);
        public static string GoldMedal => Get(Icon.GoldMedal);
        
        // Social
        public static string Alliance => Get(Icon.Alliance);
        public static string Handshake => Get(Icon.Handshake);
        public static string Gift => Get(Icon.Gift);
        public static string Mail => Get(Icon.Mail);
        public static string Chat => Get(Icon.Chat);
        public static string Peace => Get(Icon.Peace);
        
        // Notifications
        public static string Alert => Get(Icon.Alert);
        public static string Success => Get(Icon.Success);
        public static string Party => Get(Icon.Party);
        public static string Bell => Get(Icon.Bell);
        
        // Misc
        public static string Settings => Get(Icon.Settings);
        public static string Map => Get(Icon.Map);
        public static string Eye => Get(Icon.Eye);
        public static string Scroll => Get(Icon.Scroll);
        public static string Target => Get(Icon.Target);
        public static string Leaderboard => Get(Icon.Leaderboard);
        public static string Statistics => Get(Icon.Statistics);
        public static string Close => Get(Icon.Close);
        public static string Check => Get(Icon.Check);

        // ====================================================================
        // SPRITE ASSET CONFIGURATION
        // ====================================================================
        
        private static TMP_SpriteAsset _spriteAsset;
        private static bool _initialized = false;
        private static bool _useFallback = false;
        
        // Fallback emoji mapping (used when sprite asset not loaded)
        private static readonly Dictionary<Icon, string> FallbackEmoji = new Dictionary<Icon, string>
        {
            // Resources
            { Icon.Gold, "<color=#FFD700>$</color>" },
            { Icon.Gems, "<color=#00BFFF>D</color>" },
            { Icon.Crystals, "<color=#9370DB>C</color>" },
            { Icon.ApexCoins, "<color=#DA70D6>A</color>" },
            { Icon.Metal, "<color=#A9A9A9>M</color>" },
            { Icon.Wood, "<color=#8B4513>W</color>" },
            { Icon.Stone, "<color=#696969>S</color>" },
            { Icon.Food, "<color=#32CD32>F</color>" },
            { Icon.XP, "<color=#FFD700>*</color>" },
            { Icon.Gem, "<color=#E040FB>*</color>" },
            { Icon.Coin, "<color=#FFD700>o</color>" },
            { Icon.Chest, "<color=#8B4513>[]</color>" },
            
            // Combat
            { Icon.Sword, "<color=#C0C0C0>/</color>" },
            { Icon.Shield, "<color=#4169E1>O</color>" },
            { Icon.Crossed_Swords, "<color=#DC143C>X</color>" },
            { Icon.Helmet, "<color=#708090>^</color>" },
            { Icon.Arrow, "<color=#8B4513>></color>" },
            { Icon.Cannon, "<color=#2F4F4F>=</color>" },
            { Icon.Battle, "<color=#DC143C>X</color>" },
            { Icon.Victory, "<color=#32CD32>V</color>" },
            { Icon.Defeat, "<color=#FF4500>L</color>" },
            { Icon.Troops, "<color=#4169E1>i</color>" },
            
            // Territories
            { Icon.Flag, "<color=#FF4500>P</color>" },
            { Icon.Castle, "<color=#8B4513>#</color>" },
            { Icon.Tower, "<color=#696969>T</color>" },
            { Icon.Wall, "<color=#A9A9A9>|</color>" },
            { Icon.Construction, "<color=#FFA500>+</color>" },
            { Icon.Home, "<color=#8B4513>n</color>" },
            { Icon.Territory, "<color=#228B22>~</color>" },
            { Icon.Citadel, "<color=#FFD700>#</color>" },
            { Icon.Build, "<color=#FFA500>+</color>" },
            { Icon.Hammer, "<color=#A9A9A9>T</color>" },
            { Icon.Paint, "<color=#FF69B4>~</color>" },
            { Icon.Globe, "<color=#4169E1>O</color>" },
            
            // Status
            { Icon.Star, "<color=#FFD700>*</color>" },
            { Icon.Star_Filled, "<color=#FFD700>*</color>" },
            { Icon.Trophy, "<color=#FFD700>Y</color>" },
            { Icon.Medal, "<color=#CD853F>@</color>" },
            { Icon.Crown, "<color=#FFD700>^</color>" },
            { Icon.Heart, "<color=#FF69B4><3</color>" },
            { Icon.Clock, "<color=#87CEEB>o</color>" },
            { Icon.Calendar, "<color=#778899>[]</color>" },
            { Icon.Lock, "<color=#A9A9A9>O</color>" },
            { Icon.Unlock, "<color=#32CD32>O</color>" },
            { Icon.Progress, "<color=#4169E1>=</color>" },
            { Icon.Level, "<color=#FFD700>L</color>" },
            { Icon.Rank, "<color=#DA70D6>#</color>" },
            { Icon.BronzeMedal, "<color=#CD7F32>3</color>" },
            { Icon.SilverMedal, "<color=#C0C0C0>2</color>" },
            { Icon.GoldMedal, "<color=#FFD700>1</color>" },
            
            // Social
            { Icon.Alliance, "<color=#4169E1>&</color>" },
            { Icon.Handshake, "<color=#32CD32>%</color>" },
            { Icon.Chat, "<color=#87CEEB>\"</color>" },
            { Icon.Gift, "<color=#FF69B4>*</color>" },
            { Icon.Mail, "<color=#DDA0DD>@</color>" },
            { Icon.Peace, "<color=#87CEEB>^</color>" },
            
            // Actions
            { Icon.Attack, "<color=#FF4500>!</color>" },
            { Icon.Defend, "<color=#4169E1>O</color>" },
            { Icon.Scout, "<color=#228B22>?</color>" },
            { Icon.Upgrade, "<color=#32CD32>^</color>" },
            { Icon.Collect, "<color=#FFD700>+</color>" },
            { Icon.Search, "<color=#87CEEB>?</color>" },
            { Icon.Close, "<color=#FF4500>X</color>" },
            
            // Notifications
            { Icon.Alert, "<color=#FF4500>!</color>" },
            { Icon.Info, "<color=#4169E1>i</color>" },
            { Icon.Success, "<color=#32CD32>v</color>" },
            { Icon.Warning, "<color=#FFA500>!</color>" },
            { Icon.Bell, "<color=#FFD700>o</color>" },
            
            // Misc
            { Icon.Settings, "<color=#808080>*</color>" },
            { Icon.Map, "<color=#228B22>~</color>" },
            { Icon.Compass, "<color=#4169E1>+</color>" },
            { Icon.Eye, "<color=#87CEEB>o</color>" },
            { Icon.Party, "<color=#FF69B4>!</color>" },
            { Icon.Fire, "<color=#FF4500>~</color>" },
            { Icon.Lightning, "<color=#FFD700>/</color>" },
            { Icon.Scroll, "<color=#DEB887>]</color>" },
            { Icon.Target, "<color=#FF4500>@</color>" },
            { Icon.Leaderboard, "<color=#FFD700>#</color>" },
            { Icon.Statistics, "<color=#4169E1>=</color>" },
            { Icon.History, "<color=#808080>~</color>" },
            { Icon.Refresh, "<color=#4169E1>@</color>" },
            { Icon.Play, "<color=#32CD32>></color>" },
            { Icon.Pause, "<color=#FFA500>||</color>" },
            { Icon.Stop, "<color=#FF4500>[]</color>" },
            { Icon.Check, "<color=#32CD32>v</color>" },
            { Icon.Cross, "<color=#FF4500>X</color>" }
        };

        // Sprite name mapping
        private static readonly Dictionary<Icon, string> SpriteNames = new Dictionary<Icon, string>
        {
            // Resources
            { Icon.Gold, "gold" },
            { Icon.Gems, "gems" },
            { Icon.Crystals, "crystals" },
            { Icon.ApexCoins, "apex_coins" },
            { Icon.Metal, "metal" },
            { Icon.Wood, "wood" },
            { Icon.Stone, "stone" },
            { Icon.Food, "food" },
            { Icon.XP, "xp" },
            { Icon.Gem, "gem" },
            { Icon.Coin, "coin" },
            { Icon.Chest, "chest" },
            
            // Combat
            { Icon.Sword, "sword" },
            { Icon.Shield, "shield" },
            { Icon.Crossed_Swords, "crossed_swords" },
            { Icon.Helmet, "helmet" },
            { Icon.Arrow, "arrow" },
            { Icon.Cannon, "cannon" },
            { Icon.Battle, "battle" },
            { Icon.Victory, "victory" },
            { Icon.Defeat, "defeat" },
            { Icon.Troops, "troops" },
            
            // Territories
            { Icon.Flag, "flag" },
            { Icon.Castle, "castle" },
            { Icon.Tower, "tower" },
            { Icon.Wall, "wall" },
            { Icon.Construction, "construction" },
            { Icon.Home, "home" },
            { Icon.Territory, "territory" },
            { Icon.Citadel, "citadel" },
            { Icon.Build, "build" },
            { Icon.Hammer, "hammer" },
            { Icon.Paint, "paint" },
            { Icon.Globe, "globe" },
            
            // Status
            { Icon.Star, "star" },
            { Icon.Star_Filled, "star_filled" },
            { Icon.Trophy, "trophy" },
            { Icon.Medal, "medal" },
            { Icon.Crown, "crown" },
            { Icon.Heart, "heart" },
            { Icon.Clock, "clock" },
            { Icon.Calendar, "calendar" },
            { Icon.Lock, "lock" },
            { Icon.Unlock, "unlock" },
            { Icon.Progress, "progress" },
            { Icon.Level, "level" },
            { Icon.Rank, "rank" },
            { Icon.BronzeMedal, "bronze_medal" },
            { Icon.SilverMedal, "silver_medal" },
            { Icon.GoldMedal, "gold_medal" },
            
            // Social
            { Icon.Alliance, "alliance" },
            { Icon.Handshake, "handshake" },
            { Icon.Chat, "chat" },
            { Icon.Gift, "gift" },
            { Icon.Mail, "mail" },
            { Icon.Peace, "peace" },
            
            // Actions
            { Icon.Attack, "attack" },
            { Icon.Defend, "defend" },
            { Icon.Scout, "scout" },
            { Icon.Upgrade, "upgrade" },
            { Icon.Collect, "collect" },
            { Icon.Search, "search" },
            { Icon.Close, "close" },
            
            // Notifications
            { Icon.Alert, "alert" },
            { Icon.Info, "info" },
            { Icon.Success, "success" },
            { Icon.Warning, "warning" },
            { Icon.Bell, "bell" },
            
            // Misc
            { Icon.Settings, "settings" },
            { Icon.Map, "map" },
            { Icon.Compass, "compass" },
            { Icon.Eye, "eye" },
            { Icon.Party, "party" },
            { Icon.Fire, "fire" },
            { Icon.Lightning, "lightning" },
            { Icon.Scroll, "scroll" },
            { Icon.Target, "target" },
            { Icon.Leaderboard, "leaderboard" },
            { Icon.Statistics, "statistics" },
            { Icon.History, "history" },
            { Icon.Refresh, "refresh" },
            { Icon.Play, "play" },
            { Icon.Pause, "pause" },
            { Icon.Stop, "stop" },
            { Icon.Check, "check" },
            { Icon.Cross, "cross" }
        };

        // ====================================================================
        // PUBLIC API
        // ====================================================================
        
        /// <summary>
        /// Gets the TMP-compatible string for an icon.
        /// Returns sprite tag if sprite asset loaded, otherwise fallback.
        /// </summary>
        public static string Get(Icon icon)
        {
            Initialize();
            
            if (_useFallback)
            {
                return FallbackEmoji.TryGetValue(icon, out string fallback) ? fallback : "?";
            }
            
            if (SpriteNames.TryGetValue(icon, out string spriteName))
            {
                return $"<sprite name=\"{spriteName}\">";
            }
            
            return "?";
        }

        /// <summary>
        /// Gets icon with custom size
        /// </summary>
        public static string GetSized(Icon icon, float size)
        {
            Initialize();
            
            if (_useFallback)
            {
                string fallback = FallbackEmoji.TryGetValue(icon, out string fb) ? fb : "?";
                return $"<size={size}>{fallback}</size>";
            }
            
            if (SpriteNames.TryGetValue(icon, out string spriteName))
            {
                // TMP sprite size is relative to font size
                return $"<size={size}><sprite name=\"{spriteName}\"></size>";
            }
            
            return "?";
        }

        /// <summary>
        /// Gets icon with tint color
        /// </summary>
        public static string GetTinted(Icon icon, Color color)
        {
            Initialize();
            string hexColor = ColorUtility.ToHtmlStringRGB(color);
            
            if (_useFallback)
            {
                string fallback = FallbackEmoji.TryGetValue(icon, out string fb) ? fb : "?";
                return $"<color=#{hexColor}>{fallback}</color>";
            }
            
            if (SpriteNames.TryGetValue(icon, out string spriteName))
            {
                return $"<sprite name=\"{spriteName}\" color=#{hexColor}>";
            }
            
            return "?";
        }

        /// <summary>
        /// Formats a resource value with icon
        /// Example: FormatResource(Icon.Gold, 1500) => "[gold icon] 1,500"
        /// </summary>
        public static string FormatResource(Icon icon, long value)
        {
            return $"{Get(icon)} {value:N0}";
        }

        /// <summary>
        /// Formats a resource value with icon and color
        /// </summary>
        public static string FormatResource(Icon icon, long value, Color valueColor)
        {
            string hexColor = ColorUtility.ToHtmlStringRGB(valueColor);
            return $"{Get(icon)} <color=#{hexColor}>{value:N0}</color>";
        }

        /// <summary>
        /// Gets notification prefix based on type
        /// </summary>
        public static string GetNotificationIcon(string notificationType)
        {
            return notificationType.ToLower() switch
            {
                "attack" or "territory_attacked" => Get(Icon.Crossed_Swords),
                "capture" or "territory_captured" => Get(Icon.Trophy),
                "defense" or "defense_success" => Get(Icon.Shield),
                "resource" or "resources_collected" => Get(Icon.Gold),
                "level" or "level_up" => Get(Icon.Star_Filled),
                "achievement" => Get(Icon.Medal),
                "gift" => Get(Icon.Gift),
                "event" => Get(Icon.Party),
                "alliance" => Get(Icon.Alliance),
                "mail" or "message" => Get(Icon.Mail),
                "warning" => Get(Icon.Warning),
                "success" => Get(Icon.Success),
                _ => Get(Icon.Info)
            };
        }

        // ====================================================================
        // INITIALIZATION
        // ====================================================================
        
        private static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            
            // Try to load sprite asset from Resources (use full path to avoid namespace conflict)
            _spriteAsset = UnityEngine.Resources.Load<TMP_SpriteAsset>("UI/GameIconsSpriteAsset");
            
            if (_spriteAsset == null)
            {
                // Try alternative paths
                _spriteAsset = UnityEngine.Resources.Load<TMP_SpriteAsset>("Fonts/GameIconsSpriteAsset");
            }
            
            if (_spriteAsset == null)
            {
                _spriteAsset = UnityEngine.Resources.Load<TMP_SpriteAsset>("GameIconsSpriteAsset");
            }
            
            if (_spriteAsset != null)
            {
                // Register as default sprite asset for TMP
                if (TMP_Settings.defaultSpriteAsset == null)
                {
                    // Can't set directly, but we can add to fallback list
                    ApexLogger.LogVerbose("Sprite asset loaded successfully", ApexLogger.LogCategory.UI);
                }
                _useFallback = false;
            }
            else
            {
                ApexLogger.Log("Sprite asset not found, using styled text fallback. " +
                         "Run Window > Apex Citadels > Generate Icon Sprites to create the sprite sheet.", ApexLogger.LogCategory.UI);
                _useFallback = true;
            }
        }

        /// <summary>
        /// Forces reinitialization (call after generating sprite asset)
        /// </summary>
        public static void Reinitialize()
        {
            _initialized = false;
            _spriteAsset = null;
            Initialize();
        }

        /// <summary>
        /// Returns whether the sprite asset is loaded
        /// </summary>
        public static bool IsSpriteAssetLoaded => !_useFallback && _spriteAsset != null;

        /// <summary>
        /// Gets the sprite asset for manual assignment
        /// </summary>
        public static TMP_SpriteAsset SpriteAsset
        {
            get
            {
                Initialize();
                return _spriteAsset;
            }
        }
    }
}
