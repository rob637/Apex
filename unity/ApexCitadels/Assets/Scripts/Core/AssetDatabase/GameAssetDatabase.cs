// ============================================================================
// APEX CITADELS - GAME ASSET DATABASE
// Central registry for all game assets (models, sounds, animations)
// Auto-populated by editor tools, easily extensible for new assets
// ============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ApexCitadels.Core.Assets
{
    /// <summary>
    /// Central database of all game assets. ScriptableObject that persists references.
    /// When you add new assets, run the "Refresh Asset Database" tool to update.
    /// </summary>
    [CreateAssetMenu(fileName = "GameAssetDatabase", menuName = "Apex Citadels/Game Asset Database")]
    public class GameAssetDatabase : ScriptableObject
    {
        public static GameAssetDatabase Instance { get; private set; }

        [Header("3D Models - Buildings")]
        public List<BuildingModelEntry> BuildingModels = new List<BuildingModelEntry>();

        [Header("3D Models - Towers")]
        public List<TowerModelEntry> TowerModels = new List<TowerModelEntry>();

        [Header("3D Models - Walls")]
        public List<WallModelEntry> WallModels = new List<WallModelEntry>();

        [Header("3D Models - Foundations")]
        public List<ModelEntry> FoundationModels = new List<ModelEntry>();

        [Header("3D Models - Roofs")]
        public List<ModelEntry> RoofModels = new List<ModelEntry>();

        [Header("Skyboxes")]
        public List<SkyboxEntry> Skyboxes = new List<SkyboxEntry>();

        [Header("Sound Effects")]
        public SoundEffectLibrary SFX = new SoundEffectLibrary();

        [Header("Animations")]
        public AnimationLibrary Animations = new AnimationLibrary();

        [Header("Prefabs (Auto-generated)")]
        public List<PrefabEntry> GeneratedPrefabs = new List<PrefabEntry>();

        /// <summary>
        /// Load the database at runtime
        /// </summary>
        public static GameAssetDatabase Load()
        {
            if (Instance != null) return Instance;
            
            Instance = UnityEngine.Resources.Load<GameAssetDatabase>("GameAssetDatabase");
            if (Instance == null)
            {
                Debug.LogWarning("[AssetDB] GameAssetDatabase not found in Resources. Create one via Assets > Create > Apex Citadels > Game Asset Database");
            }
            return Instance;
        }

        #region Model Lookup Methods

        public GameObject GetBuildingModel(string buildingId)
        {
            var entry = BuildingModels.Find(b => b.Id == buildingId || b.Name.Contains(buildingId));
            return entry?.Prefab ?? entry?.Model;
        }

        public BuildingModelEntry GetBuilding(string buildingId)
        {
            return BuildingModels.Find(b => b.Id == buildingId || b.Name.Contains(buildingId));
        }

        public BuildingModelEntry GetRandomBuilding(BuildingCategory category)
        {
            var matches = BuildingModels.FindAll(b => b.Category == category);
            if (matches.Count == 0) return null;
            return matches[UnityEngine.Random.Range(0, matches.Count)];
        }

        public GameObject GetTowerModel(string towerId)
        {
            var entry = TowerModels.Find(t => t.Id == towerId || t.Name.Contains(towerId));
            return entry?.Prefab ?? entry?.Model;
        }

        public TowerModelEntry GetRandomTower(TowerType type)
        {
            var matches = TowerModels.FindAll(t => t.Type == type);
            if (matches.Count == 0) return null;
            return matches[UnityEngine.Random.Range(0, matches.Count)];
        }

        public GameObject GetWallModel(string wallId)
        {
            var entry = WallModels.Find(w => w.Id == wallId || w.Name.Contains(wallId));
            return entry?.Prefab ?? entry?.Model;
        }

        public WallModelEntry GetWall(WallType type, WallMaterial material)
        {
            var entry = WallModels.Find(w => w.Type == type && w.WallMaterial == material);
            if (entry == null) entry = WallModels.Find(w => w.Type == type);
            return entry;
        }

        public List<BuildingModelEntry> GetBuildingsByCategory(BuildingCategory category)
        {
            return BuildingModels.FindAll(b => b.Category == category);
        }

        public List<TowerModelEntry> GetTowersByType(TowerType type)
        {
            return TowerModels.FindAll(t => t.Type == type);
        }

        #endregion

        #region Sound Lookup Methods

        public AudioClip GetSFX(string sfxId)
        {
            return SFX.GetClip(sfxId);
        }

        public AudioClip GetRandomSFX(SFXCategory category)
        {
            return SFX.GetRandomClip(category);
        }

        public List<AudioClip> GetAllSFX(SFXCategory category)
        {
            return SFX.GetClips(category);
        }

        #endregion

        #region Animation Lookup Methods

        public AnimationClip GetAnimation(string animId)
        {
            return Animations.GetClip(animId);
        }

        public AnimationClip GetAnimation(AnimationType type)
        {
            return Animations.GetClip(type);
        }

        #endregion

        #region Skybox Methods

        public Material GetRandomSkybox()
        {
            if (Skyboxes.Count == 0) return null;
            return Skyboxes[UnityEngine.Random.Range(0, Skyboxes.Count)].Material;
        }

        public Material GetSkybox(string name)
        {
            var entry = Skyboxes.Find(s => s.Name.Contains(name));
            return entry?.Material;
        }

        #endregion
    }

    #region Entry Types

    [Serializable]
    public class ModelEntry
    {
        public string Id;
        public string Name;
        public GameObject Model;      // Raw imported model
        public GameObject Prefab;     // Configured prefab with components
        public Sprite Icon;
        public string[] Tags;
    }

    [Serializable]
    public class BuildingModelEntry : ModelEntry
    {
        public BuildingCategory Category;
        public int Level = 1;
        public bool IsVariant;
        public string VariantType; // "Advanced", "Damaged", "Upgraded", etc.
    }

    [Serializable]
    public class TowerModelEntry : ModelEntry
    {
        public TowerType Type;
        public int Level = 1;
        public bool IsVariant;
        public string VariantType;
    }

    [Serializable]
    public class WallModelEntry : ModelEntry
    {
        public WallType Type;
        public WallMaterial WallMaterial;
        public float Length = 2f;
        public bool IsCorner;
        public bool IsGate;
        public bool IsVariant;
        public string VariantType;
    }

    [Serializable]
    public class SkyboxEntry
    {
        public string Name;
        public Texture2D Texture;
        public Material Material;
        public SkyboxTime TimeOfDay;
    }

    [Serializable]
    public class PrefabEntry
    {
        public string Id;
        public string Category;
        public GameObject Prefab;
    }

    #endregion

    #region Enums

    public enum BuildingCategory
    {
        Resource,       // Gold Mine, Lumber Mill, Quarry, Farm
        Military,       // Barracks, Armory, Siege Workshop, Stable
        Economic,       // Market, Treasury, Bank, Warehouse
        Magic,          // Magic Academy, Alchemist Lab, Portal Nexus
        Support,        // Tavern, Hospital, Prison, Library
        Decoration
    }

    public enum TowerType
    {
        Guard,
        Archer,
        Mage,
        Siege,
        Utility,  // Bell, Clock, Lighthouse, Windmill
        Special
    }

    public enum WallType
    {
        Straight,
        Corner,
        TJunction,
        EndCap,
        Gate,
        Window,
        Half,
        Fence,
        Barricade,
        Trench,
        Magic
    }

    public enum WallMaterial
    {
        Stone,
        Wood,
        Iron,
        Magic,
        Ice,
        Bone,
        Other
    }

    public enum SkyboxTime
    {
        Day,
        Sunset,
        Night,
        Dawn,
        Stormy,
        Any
    }

    public enum SFXCategory
    {
        Building,       // BLD - construction, placement
        Character,      // CHR - footsteps, crowds
        Combat,         // CMB - weapons, spells
        Environment,    // ENV - weather, ambient
        Effects,        // FX - magic, explosions
        UI              // UI - buttons, notifications
    }

    public enum AnimationType
    {
        // Locomotion
        Walk, WalkBackward, WalkLeft, WalkRight, WalkInjured, Sneak,
        Run, RunBackward, RunFast, RunCombat,
        Jump, Fall, Land, HardLand,
        Dodge, DodgeLeft, DodgeRight, DodgeBack, Roll,
        Turn, TurnLeft, TurnRight,
        
        // Idle
        Idle, IdleAlert, IdleTired, IdleHappy, IdleSad, IdleBreathing,
        IdleSword, IdleTwoHandSword, IdleBow, IdleShield, IdleSpear,
        LookAround, ScratchHead, Stretch, Yawn, CheckWatch,
        
        // Combat - Melee
        SwordSlash, SwordBlock, SwordParry, SwordDraw, SwordSheathe,
        SwordShieldSlash, SwordShieldAttack,
        GreatSwordSlash, GreatSwordAttack, GreatSwordOverhead,
        TwoHandAttack, TwoHandBlock,
        AxeSlash, AxeAttack,
        SpearThrust, SpearSwing, SpearBlock,
        ShieldBlock, ShieldBlockHigh, ShieldBash,
        Throw,
        
        // Combat - Ranged
        DrawArrow, AimIdle, BowFire, BowFireRunning,
        CrossbowReload, CrossbowFire,
        
        // Combat - Magic
        CastSpell, MagicAttack, Channel, Bless, MagicAOE, Summon,
        
        // Hit Reactions
        HitReaction, HitFromBack, HitLeft, HitRight, HeavyHit,
        Knockback, KnockedDown, GetUp,
        DeathForward, DeathBackward, DeathLeft, DeathRight, DeathDramatic,
        RiseFromGround,
        
        // Interaction
        PickUp, ReachUp, PutDown, Push, Pull,
        OpenChest, OpenDoor, PullLever,
        
        // Social
        Wave, Bow, Salute, Clap, Cheer,
        HeadShake, HeadNod, Point,
        
        // Work
        Hammer
    }

    #endregion

    #region Sound Effect Library

    [Serializable]
    public class SoundEffectLibrary
    {
        public List<SFXEntry> AllClips = new List<SFXEntry>();

        // Quick lookup caches (populated at runtime)
        private Dictionary<string, AudioClip> _clipLookup;
        private Dictionary<SFXCategory, List<AudioClip>> _categoryLookup;

        public AudioClip GetClip(string id)
        {
            BuildCache();
            if (_clipLookup.TryGetValue(id, out var clip)) return clip;
            
            // Try partial match
            foreach (var entry in AllClips)
            {
                if (entry.Id.Contains(id) || entry.Name.Contains(id))
                    return entry.Clip;
            }
            return null;
        }

        public AudioClip GetRandomClip(SFXCategory category)
        {
            BuildCache();
            if (_categoryLookup.TryGetValue(category, out var clips) && clips.Count > 0)
            {
                return clips[UnityEngine.Random.Range(0, clips.Count)];
            }
            return null;
        }

        public List<AudioClip> GetClips(SFXCategory category)
        {
            BuildCache();
            if (_categoryLookup.TryGetValue(category, out var clips))
                return clips;
            return new List<AudioClip>();
        }

        private void BuildCache()
        {
            if (_clipLookup != null) return;
            
            _clipLookup = new Dictionary<string, AudioClip>();
            _categoryLookup = new Dictionary<SFXCategory, List<AudioClip>>();

            foreach (SFXCategory cat in Enum.GetValues(typeof(SFXCategory)))
            {
                _categoryLookup[cat] = new List<AudioClip>();
            }

            foreach (var entry in AllClips)
            {
                if (entry.Clip == null) continue;
                
                _clipLookup[entry.Id] = entry.Clip;
                if (_categoryLookup.ContainsKey(entry.Category))
                {
                    _categoryLookup[entry.Category].Add(entry.Clip);
                }
            }
        }

        public void ClearCache()
        {
            _clipLookup = null;
            _categoryLookup = null;
        }
    }

    [Serializable]
    public class SFXEntry
    {
        public string Id;           // e.g., "CMB01"
        public string Name;         // e.g., "sword_swing"
        public SFXCategory Category;
        public AudioClip Clip;
        public float DefaultVolume = 1f;
        public float PitchVariation = 0.1f;
    }

    #endregion

    #region Animation Library

    [Serializable]
    public class AnimationLibrary
    {
        public List<AnimationEntry> AllClips = new List<AnimationEntry>();

        private Dictionary<string, AnimationClip> _idLookup;
        private Dictionary<AnimationType, AnimationClip> _typeLookup;

        public AnimationClip GetClip(string id)
        {
            BuildCache();
            if (_idLookup.TryGetValue(id, out var clip)) return clip;
            return null;
        }

        public AnimationClip GetClip(AnimationType type)
        {
            BuildCache();
            if (_typeLookup.TryGetValue(type, out var clip)) return clip;
            return null;
        }

        private void BuildCache()
        {
            if (_idLookup != null) return;
            
            _idLookup = new Dictionary<string, AnimationClip>();
            _typeLookup = new Dictionary<AnimationType, AnimationClip>();

            foreach (var entry in AllClips)
            {
                if (entry.Clip == null) continue;
                _idLookup[entry.Id] = entry.Clip;
                if (!_typeLookup.ContainsKey(entry.Type))
                {
                    _typeLookup[entry.Type] = entry.Clip;
                }
            }
        }

        public void ClearCache()
        {
            _idLookup = null;
            _typeLookup = null;
        }
    }

    [Serializable]
    public class AnimationEntry
    {
        public string Id;           // e.g., "001"
        public string Name;         // e.g., "Walking"
        public AnimationType Type;
        public AnimationClip Clip;
        public bool IsLooping;
    }

    #endregion
}
