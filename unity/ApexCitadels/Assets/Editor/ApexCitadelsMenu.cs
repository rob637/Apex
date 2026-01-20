// ============================================================================
// APEX CITADELS - UNIFIED MENU SYSTEM
// All editor tools consolidated with clear categories and documentation.
// All menu items are IDEMPOTENT (safe to run multiple times).
// ============================================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace ApexCitadels.Editor
{
    /// <summary>
    /// Unified menu system for Apex Citadels.
    /// 
    /// ═══════════════════════════════════════════════════════════════════════
    /// MENU STRUCTURE (Apex Citadels Menu)
    /// ═══════════════════════════════════════════════════════════════════════
    /// 
    /// ★ Quick Start (Priority 0-9)
    ///   ├── ONE-CLICK SETUP ★ ─────── Creates complete scene, safe to re-run
    ///   └── Setup Status Dashboard ── Shows what's configured, read-only
    /// 
    /// Scene Setup (Priority 20-29)
    ///   ├── AAA Scene Setup Wizard ── Interactive setup with options
    ///   ├── Complete PC Setup ─────── Window with granular controls
    ///   ├── Quick Setup (All) ─────── One-shot full setup, idempotent
    ///   └── Auto-Wire References ──── Links scene objects, safe to re-run
    /// 
    /// Assets (Priority 40-49)
    ///   ├── Refresh Asset Database ── Reimports assets, always safe
    ///   ├── Generate SFX Library ──── Creates library from audio files
    ///   ├── Generate UI Sounds ────── Creates UI sound references
    ///   └── Generate Humanoid ─────── Creates animator controller
    /// 
    /// Environment (Priority 60-69)
    ///   ├── Add AAA Environment ───── Adds environment systems, idempotent
    ///   ├── Time of Day ► ─────────── Submenu to preview time periods
    ///   ├── Regenerate Terrain ────── Rebuilds procedural terrain
    ///   └── Toggle Grid ───────────── Visual debugging aid
    /// 
    /// GeoMap (Priority 80-89)
    ///   ├── Create Real World Map ─── Adds Mapbox tile system
    ///   ├── Quick Locations ► ─────── Jump to test locations
    ///   ├── Change Provider ► ─────── Switch tile providers
    ///   └── Documentation ─────────── Opens docs
    /// 
    /// PC (Priority 100-109)
    ///   ├── Configure Mapbox API ──── Set API key (one-time)
    ///   └── Setup Mapbox ──────────── Auto-configures Mapbox
    /// 
    /// System Coordinator (Priority 100)
    ///   └── Opens diagnostic window for runtime system management
    /// 
    /// Setup (Priority 200)
    ///   └── Create Game Asset Database ─ One-time ScriptableObject creation
    /// 
    /// Debug (Priority 300)
    ///   └── Verify GameAssetDatabase ─── Validates all asset references
    /// 
    /// Utilities (Priority 120+)
    ///   └── Migrate Emojis ──────────── One-time migration tool
    /// 
    /// ═══════════════════════════════════════════════════════════════════════
    /// IDEMPOTENCY GUIDE
    /// ═══════════════════════════════════════════════════════════════════════
    /// 
    /// All menu items are designed to be SAFE TO RUN MULTIPLE TIMES:
    /// 
    /// ✅ ALWAYS SAFE (Run anytime):
    ///    - ONE-CLICK SETUP: Checks for existing objects, skips if present
    ///    - Setup Status Dashboard: Read-only status display
    ///    - AAA Scene Setup: Checks for components before adding
    ///    - Auto-Wire References: Re-links without duplicating
    ///    - Refresh Asset Database: Standard Unity operation
    ///    - Time of Day settings: Just changes values
    ///    - Quick Locations: Just teleports camera
    ///    - Change Provider: Just switches tile source
    ///    - System Coordinator: Just opens window
    /// 
    /// ⚠️ REGENERATIVE (Replaces existing):
    ///    - Generate SFX Library: Recreates ScriptableObject
    ///    - Generate UI Sounds: Recreates ScriptableObject
    ///    - Regenerate Terrain: Rebuilds terrain mesh
    ///    - Create Game Asset Database: Overwrites if exists
    /// 
    /// 📝 ONE-TIME (Usually run once):
    ///    - Configure Mapbox API: Sets API key
    ///    - Migrate Emojis: One-time data migration
    /// 
    /// ═══════════════════════════════════════════════════════════════════════
    /// RECOMMENDED WORKFLOW
    /// ═══════════════════════════════════════════════════════════════════════
    /// 
    /// First Time Setup:
    /// 1. Apex Citadels > ★ Quick Start > ONE-CLICK SETUP
    /// 2. Apex Citadels > PC > Configure Mapbox API (optional)
    /// 3. Hit Play!
    /// 
    /// After Pulling Updates:
    /// 1. Apex Citadels > Assets > Refresh Asset Database
    /// 2. Apex Citadels > ★ Quick Start > Setup Status Dashboard
    /// 3. Click "Fix All Issues" if needed
    /// 
    /// Testing Time of Day:
    /// - Apex Citadels > Environment > Time of Day > [Dawn/Noon/Night]
    /// 
    /// Testing Different Locations:
    /// - Apex Citadels > GeoMap > Quick Locations > [City]
    /// 
    /// </summary>
    public static class ApexCitadelsMenu
    {
        // This class provides documentation for the menu system.
        // Actual menu items are implemented in their respective editor scripts.
        //
        // Scripts implementing menus:
        // - PCCompleteSetup.cs: ONE-CLICK SETUP, Complete PC Setup
        // - SetupStatusDashboard.cs: Setup Status Dashboard
        // - AAASceneSetup.cs: AAA Scene Setup Wizard, Quick Setup
        // - PCAutoWirer.cs: Auto-Wire References
        // - AssetDatabaseRefreshTool.cs: Refresh Asset Database
        // - SFXLibraryGenerator.cs: Generate SFX Library
        // - UISoundLibraryGenerator.cs: Generate UI Sounds
        // - AnimationControllerGenerator.cs: Generate Humanoid Controller
        // - EnvironmentEditorTools.cs: Environment menu items
        // - GeoMapEditorTools.cs: GeoMap menu items
        // - MapboxConfigWindow.cs: Configure Mapbox API
        // - MapboxAutoSetup.cs: Setup Mapbox
        // - SystemCoordinatorWindow.cs: System Coordinator window
        // - GameAssetDatabaseSetup.cs: Create/Verify GameAssetDatabase
        // - EmojiMigrationTool.cs: Migrate Emojis
    }
}
#endif
