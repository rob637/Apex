using System;
using UnityEngine;

namespace ApexCitadels.PC
{
    /// <summary>
    /// Detects and manages platform-specific behavior.
    /// Determines whether the game is running on AR (mobile) or PC.
    /// </summary>
    public static class PlatformManager
    {
        /// <summary>
        /// Returns true if running on an AR-capable mobile device
        /// </summary>
        public static bool IsAR =>
            Application.platform == RuntimePlatform.Android ||
            Application.platform == RuntimePlatform.IPhonePlayer;

        /// <summary>
        /// Returns true if running on PC (Windows, macOS, Linux)
        /// </summary>
        public static bool IsPC =>
            Application.platform == RuntimePlatform.WindowsPlayer ||
            Application.platform == RuntimePlatform.WindowsEditor ||
            Application.platform == RuntimePlatform.OSXPlayer ||
            Application.platform == RuntimePlatform.OSXEditor ||
            Application.platform == RuntimePlatform.LinuxPlayer ||
            Application.platform == RuntimePlatform.LinuxEditor;

        /// <summary>
        /// Returns true if running in the Unity Editor
        /// </summary>
        public static bool IsEditor => Application.isEditor;

        /// <summary>
        /// Returns true if running on WebGL (future lite client)
        /// </summary>
        public static bool IsWebGL => Application.platform == RuntimePlatform.WebGLPlayer;

        /// <summary>
        /// Get the current platform type as an enum
        /// </summary>
        public static PlatformType CurrentPlatform
        {
            get
            {
                if (IsAR) return PlatformType.AR;
                if (IsPC) return PlatformType.PC;
                if (IsWebGL) return PlatformType.WebGL;
                return PlatformType.Unknown;
            }
        }

        /// <summary>
        /// Check if a feature is available on the current platform
        /// </summary>
        public static bool IsFeatureAvailable(GameFeature feature)
        {
            return feature switch
            {
                // AR-Exclusive Features (must be physically present)
                GameFeature.ClaimNewTerritory => IsAR,
                GameFeature.DiscoverResourceNodes => IsAR,
                GameFeature.PlaceARAnchors => IsAR,
                GameFeature.FirstTimeBuildingPlacement => IsAR,
                GameFeature.HarvestWildResources => IsAR,
                GameFeature.ScoutEnemyTerritory => IsAR,
                GameFeature.CaptureTerritory => IsAR,
                GameFeature.DropGeospatialBeacons => IsAR,

                // PC-Exclusive Features (command center)
                GameFeature.DetailedBaseEditor => IsPC,
                GameFeature.AllianceWarRoom => IsPC,
                GameFeature.CraftingWorkshop => IsPC,
                GameFeature.MarketTradingPost => IsPC,
                GameFeature.ReplayBattles => IsPC,
                GameFeature.StatisticsDashboard => IsPC,
                GameFeature.TerritoryNetworkView => IsPC,
                GameFeature.BlueprintDesigner => IsPC,

                // Shared Features (both platforms)
                GameFeature.ViewWorldMap => true,
                GameFeature.ManageBuildings => true,
                GameFeature.AllianceChat => true,
                GameFeature.DefendTerritories => true,
                GameFeature.ViewLeaderboards => true,
                GameFeature.CollectPassiveIncome => true,
                GameFeature.DailyRewards => true,
                GameFeature.Achievements => true,
                GameFeature.ProfileSettings => true,

                _ => false
            };
        }

        /// <summary>
        /// Get a user-friendly platform name
        /// </summary>
        public static string GetPlatformName()
        {
            return CurrentPlatform switch
            {
                PlatformType.AR => "Mobile AR",
                PlatformType.PC => "PC Command Center",
                PlatformType.WebGL => "Web Lite",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Check if GPS/Location services are available
        /// </summary>
        public static bool HasLocationServices => IsAR;

        /// <summary>
        /// Check if AR capabilities are available
        /// </summary>
        public static bool HasARCapabilities => IsAR;

        /// <summary>
        /// Check if keyboard/mouse input is primary
        /// </summary>
        public static bool HasKeyboardMouse => IsPC || IsEditor || IsWebGL;

        /// <summary>
        /// Check if touch input is primary
        /// </summary>
        public static bool HasTouchInput => IsAR;
    }

    /// <summary>
    /// Platform types supported by the game
    /// </summary>
    public enum PlatformType
    {
        Unknown,
        AR,     // Mobile AR (Android/iOS)
        PC,     // Desktop (Windows/Mac/Linux)
        WebGL   // Browser lite client
    }

    /// <summary>
    /// Game features that may be platform-specific
    /// </summary>
    public enum GameFeature
    {
        // AR-Exclusive
        ClaimNewTerritory,
        DiscoverResourceNodes,
        PlaceARAnchors,
        FirstTimeBuildingPlacement,
        HarvestWildResources,
        ScoutEnemyTerritory,
        CaptureTerritory,
        DropGeospatialBeacons,

        // PC-Exclusive
        DetailedBaseEditor,
        AllianceWarRoom,
        CraftingWorkshop,
        MarketTradingPost,
        ReplayBattles,
        StatisticsDashboard,
        TerritoryNetworkView,
        BlueprintDesigner,

        // Shared
        ViewWorldMap,
        ManageBuildings,
        AllianceChat,
        DefendTerritories,
        ViewLeaderboards,
        CollectPassiveIncome,
        DailyRewards,
        Achievements,
        ProfileSettings
    }
}
