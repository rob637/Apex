// ============================================================================
// APEX CITADELS - UNIFIED MENU SYSTEM
// All menu items consolidated under "Apex Citadels" in the main menu bar
// ============================================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace ApexCitadels.Editor
{
    /// <summary>
    /// Unified menu system for Apex Citadels.
    /// All tools accessible from: Apex Citadels > [Category] > [Tool]
    /// 
    /// Menu Structure:
    /// ├── Apex Citadels
    /// │   ├── ★ Quick Start (priority 0-9)
    /// │   │   ├── One-Click Setup
    /// │   │   ├── Full Asset Setup  
    /// │   │   └── Scene Diagnostic
    /// │   ├── Scene Setup (priority 20-29)
    /// │   │   ├── AAA Scene Setup Wizard
    /// │   │   ├── Add AAA Environment
    /// │   │   ├── Auto-Wire References
    /// │   │   └── Scene Setup Helper
    /// │   ├── Assets (priority 40-49)
    /// │   │   ├── Refresh Asset Database
    /// │   │   ├── Generate SFX Library
    /// │   │   ├── Generate UI Sounds
    /// │   │   ├── Generate Animations
    /// │   │   └── Generate Icon Sprites
    /// │   ├── Environment (priority 60-69)
    /// │   │   ├── Time of Day submenu
    /// │   │   ├── Regenerate Terrain
    /// │   │   └── Toggle Grid
    /// │   ├── GeoMap (priority 80-89)
    /// │   │   ├── Create Real World Map
    /// │   │   ├── Quick Locations submenu
    /// │   │   └── Change Provider submenu
    /// │   ├── Build (priority 100-109)
    /// │   │   ├── Build WebGL
    /// │   │   ├── Build WebGL (Dev)
    /// │   │   └── Open Build Folder
    /// │   └── Utilities (priority 120+)
    /// │       └── Migrate Emojis
    /// </summary>
    public static class ApexCitadelsMenu
    {
        // This class serves as documentation for the menu structure.
        // Actual menu items are defined in their respective editor scripts.
        // 
        // The priority numbers determine menu order:
        // 0-9: Quick Start
        // 20-29: Scene Setup  
        // 40-49: Assets
        // 60-69: Environment
        // 80-89: GeoMap
        // 100-109: Build
        // 120+: Utilities
        //
        // Separators appear between groups (every 11 items in Unity)
    }
}
#endif
