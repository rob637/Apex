// ============================================================================
// APEX CITADELS - SFX LIBRARY AUTO-GENERATOR
// Editor tool to auto-populate CombatSFXLibrary from Audio/SFX files
// Run this whenever you add new audio files!
// ============================================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using ApexCitadels.PC.Audio;

namespace ApexCitadels.Editor
{
    /// <summary>
    /// Auto-generates CombatSFXLibrary from audio files in Assets/Audio/SFX
    /// </summary>
    public class SFXLibraryGenerator : EditorWindow
    {
        private CombatSFXLibrary library;
        
        // Stats
        private int buildingSfxCount;
        private int characterSfxCount;
        private int combatSfxCount;
        private int environmentSfxCount;
        private int effectsSfxCount;
        private int uiSfxCount;
        
        [MenuItem("Apex Citadels/Advanced/Assets/Generate SFX Library", false, 42)]
        public static void ShowWindow()
        {
            var window = GetWindow<SFXLibraryGenerator>("SFX Generator");
            window.minSize = new Vector2(400, 450);
            window.Show();
        }

        [MenuItem("Apex Citadels/Advanced/Assets/Quick Generate SFX Library", false, 43)]
        public static void QuickGenerate()
        {
            var lib = LoadOrCreateLibrary();
            if (lib == null) return;

            GenerateLibrary(lib);
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            
            Debug.Log("[SFX] Quick generation complete! CombatSFXLibrary updated.");
        }

        private void OnEnable()
        {
            library = LoadOrCreateLibrary();
        }

        private void OnGUI()
        {
            GUILayout.Label("SFX Library Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            library = (CombatSFXLibrary)EditorGUILayout.ObjectField("Library", library, typeof(CombatSFXLibrary), false);

            if (library == null)
            {
                EditorGUILayout.HelpBox("No CombatSFXLibrary found. Click 'Create Library' to create one.", MessageType.Warning);
                if (GUILayout.Button("Create Library", GUILayout.Height(30)))
                {
                    library = CreateLibrary();
                }
                return;
            }

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Audio Files Discovered:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"  Building (BLD): {CountSFXFiles("BLD")}");
            EditorGUILayout.LabelField($"  Character (CHR): {CountSFXFiles("CHR")}");
            EditorGUILayout.LabelField($"  Combat (CMB): {CountSFXFiles("CMB")}");
            EditorGUILayout.LabelField($"  Environment (ENV): {CountSFXFiles("ENV")}");
            EditorGUILayout.LabelField($"  Effects (FX): {CountSFXFiles("FX")}");
            EditorGUILayout.LabelField($"  UI (UI): {CountSFXFiles("UI")}");

            GUILayout.Space(15);

            if (GUILayout.Button("GENERATE SFX LIBRARY", GUILayout.Height(40)))
            {
                GenerateLibrary(library);
                EditorUtility.SetDirty(library);
                AssetDatabase.SaveAssets();
                Debug.Log("[SFX] Library generated and saved!");
            }

            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "File Naming Convention:\n" +
                "SFX-XXX##_description.mp3\n\n" +
                "Categories:\n" +
                "• BLD = Building sounds\n" +
                "• CHR = Character sounds\n" +
                "• CMB = Combat sounds\n" +
                "• ENV = Environment sounds\n" +
                "• FX = Effects\n" +
                "• UI = User Interface",
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("Save Library"))
            {
                EditorUtility.SetDirty(library);
                AssetDatabase.SaveAssets();
                Debug.Log("[SFX] Library saved!");
            }
        }

        private static CombatSFXLibrary LoadOrCreateLibrary()
        {
            // Try to find existing
            string[] guids = AssetDatabase.FindAssets("t:CombatSFXLibrary");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<CombatSFXLibrary>(path);
            }
            
            // Create new one if not found
            return CreateLibrary();
        }

        private static CombatSFXLibrary CreateLibrary()
        {
            string dir = "Assets/Audio";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var lib = ScriptableObject.CreateInstance<CombatSFXLibrary>();
            string path = $"{dir}/CombatSFXLibrary.asset";
            AssetDatabase.CreateAsset(lib, path);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[SFX] Created CombatSFXLibrary at {path}");
            return lib;
        }

        private int CountSFXFiles(string category)
        {
            string sfxPath = "Assets/Audio/SFX";
            if (!Directory.Exists(sfxPath)) return 0;
            
            return Directory.GetFiles(sfxPath, $"SFX-{category}*.mp3").Length;
        }

        private static void GenerateLibrary(CombatSFXLibrary library)
        {
            Debug.Log("[SFX] Generating library from audio files...");

            string sfxPath = "Assets/Audio/SFX";
            if (!Directory.Exists(sfxPath))
            {
                Debug.LogWarning($"[SFX] SFX folder not found: {sfxPath}");
                return;
            }

            // Load all audio files
            var allClips = new Dictionary<string, AudioClip>();
            var audioFiles = Directory.GetFiles(sfxPath, "*.mp3").Concat(
                           Directory.GetFiles(sfxPath, "*.wav")).Concat(
                           Directory.GetFiles(sfxPath, "*.ogg"));
            
            foreach (var file in audioFiles)
            {
                string assetPath = file.Replace("\\", "/");
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                if (clip != null)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    allClips[fileName] = clip;
                }
            }

            Debug.Log($"[SFX] Loaded {allClips.Count} audio clips");

            // Use SerializedObject to edit the ScriptableObject
            SerializedObject serialized = new SerializedObject(library);

            // Populate weapon sounds (CMB01-CMB12)
            PopulateWeaponSounds(serialized, allClips);

            // Populate impact sounds
            PopulateImpactSounds(serialized, allClips);

            // Populate projectile sounds
            PopulateProjectileSounds(serialized, allClips);

            // Populate ability sounds
            PopulateAbilitySounds(serialized, allClips);

            // Populate structure sounds
            PopulateStructureSounds(serialized, allClips);

            // Populate explosions
            PopulateExplosions(serialized, allClips);

            serialized.ApplyModifiedProperties();

            Debug.Log("[SFX] Library generation complete!");
        }

        private static void PopulateWeaponSounds(SerializedObject so, Dictionary<string, AudioClip> clips)
        {
            var weaponProp = so.FindProperty("weaponSounds");
            if (weaponProp == null) 
            {
                Debug.LogWarning("[SFX] weaponSounds property not found");
                return;
            }

            weaponProp.ClearArray();

            // Mapping: SFX file -> WeaponType
            var weaponMappings = new Dictionary<string, WeaponType>
            {
                { "SFX-CMB01_sword_swing", WeaponType.Sword },
                { "SFX-CMB07_axe_swing", WeaponType.Axe },
                { "SFX-CMB09_mace_swing", WeaponType.Mace },
                { "SFX-CMB11_spear_thrust", WeaponType.Spear },
                { "SFX-CMB13_bow_draw", WeaponType.Bow },
                { "SFX-CMB19_crossbow_load", WeaponType.Crossbow },
            };

            int index = 0;
            foreach (var mapping in weaponMappings)
            {
                if (clips.TryGetValue(mapping.Key, out AudioClip clip))
                {
                    weaponProp.InsertArrayElementAtIndex(index);
                    var element = weaponProp.GetArrayElementAtIndex(index);
                    element.FindPropertyRelative("weapon").enumValueIndex = (int)mapping.Value;
                    
                    var soundProp = element.FindPropertyRelative("sound");
                    if (soundProp != null)
                    {
                        // CombatSound uses 'clips' array, not 'mainClip'
                        var clipsProp = soundProp.FindPropertyRelative("clips");
                        if (clipsProp != null)
                        {
                            clipsProp.ClearArray();
                            clipsProp.InsertArrayElementAtIndex(0);
                            clipsProp.GetArrayElementAtIndex(0).objectReferenceValue = clip;
                        }
                        
                        var volumeProp = soundProp.FindPropertyRelative("volume");
                        if (volumeProp != null) volumeProp.floatValue = 1f;
                        
                        var pitchProp = soundProp.FindPropertyRelative("pitchVariation");
                        if (pitchProp != null) pitchProp.floatValue = 0.05f;
                    }
                    
                    index++;
                }
            }

            Debug.Log($"[SFX] Mapped {index} weapon sounds");
        }

        private static void PopulateImpactSounds(SerializedObject so, Dictionary<string, AudioClip> clips)
        {
            var impactProp = so.FindProperty("impactSounds");
            if (impactProp == null) return;

            impactProp.ClearArray();

            var impactMappings = new Dictionary<string, ImpactType>
            {
                { "SFX-CMB02_sword_hit_metal", ImpactType.MetalOnMetal },
                { "SFX-CMB03_sword_hit_flesh", ImpactType.MetalOnFlesh },
                { "SFX-CMB08_axe_hit", ImpactType.Blocked },
                { "SFX-CMB18_arrow_hit_flesh", ImpactType.ArrowHit },
                { "SFX-CMB16_arrow_hit_wood", ImpactType.MetalOnWood },
                { "SFX-CMB17_arrow_hit_stone", ImpactType.MetalOnStone },
                { "SFX-CMB24_catapult_impact", ImpactType.Critical },
                { "SFX-CMB36_armor_hit", ImpactType.Blocked },
            };

            int index = 0;
            foreach (var mapping in impactMappings)
            {
                if (clips.TryGetValue(mapping.Key, out AudioClip clip))
                {
                    impactProp.InsertArrayElementAtIndex(index);
                    var element = impactProp.GetArrayElementAtIndex(index);
                    element.FindPropertyRelative("impact").enumValueIndex = (int)mapping.Value;
                    
                    SetCombatSoundClip(element.FindPropertyRelative("sound"), clip, 1f, 0.08f);
                    index++;
                }
            }

            Debug.Log($"[SFX] Mapped {index} impact sounds");
        }

        private static void PopulateProjectileSounds(SerializedObject so, Dictionary<string, AudioClip> clips)
        {
            var projProp = so.FindProperty("projectileSounds");
            if (projProp == null) return;

            projProp.ClearArray();

            var projMappings = new Dictionary<string, ProjectileType>
            {
                { "SFX-CMB14_bow_release", ProjectileType.Arrow },
                { "SFX-CMB20_crossbow_fire", ProjectileType.Bolt },
                { "SFX-CMB23_catapult_fire", ProjectileType.Rock },
                { "SFX-CMB25_trebuchet_release", ProjectileType.CannonBall },
                { "SFX-CMB29_ballista_fire", ProjectileType.Bolt },
            };

            int index = 0;
            foreach (var mapping in projMappings)
            {
                if (clips.TryGetValue(mapping.Key, out AudioClip clip))
                {
                    projProp.InsertArrayElementAtIndex(index);
                    var element = projProp.GetArrayElementAtIndex(index);
                    element.FindPropertyRelative("projectile").enumValueIndex = (int)mapping.Value;
                    
                    SetCombatSoundClip(element.FindPropertyRelative("sound"), clip, 0.9f);
                    index++;
                }
            }

            Debug.Log($"[SFX] Mapped {index} projectile sounds");
        }

        private static void PopulateAbilitySounds(SerializedObject so, Dictionary<string, AudioClip> clips)
        {
            var abilityProp = so.FindProperty("abilitySounds");
            if (abilityProp == null) return;

            abilityProp.ClearArray();

            var abilityMappings = new Dictionary<string, AbilityType>
            {
                { "SFX-CMB38_spell_cast_fire", AbilityType.FireCast },
                { "SFX-CMB40_spell_cast_ice", AbilityType.IceCast },
                { "SFX-CMB42_spell_cast_lightning", AbilityType.LightningCast },
                { "SFX-CMB44_spell_cast_heal", AbilityType.HealCast },
                { "SFX-CMB45_spell_cast_buff", AbilityType.BuffCast },
                { "SFX-CMB46_spell_cast_debuff", AbilityType.DebuffCast },
            };

            int index = 0;
            foreach (var mapping in abilityMappings)
            {
                if (clips.TryGetValue(mapping.Key, out AudioClip clip))
                {
                    abilityProp.InsertArrayElementAtIndex(index);
                    var element = abilityProp.GetArrayElementAtIndex(index);
                    element.FindPropertyRelative("ability").enumValueIndex = (int)mapping.Value;
                    
                    SetCombatSoundClip(element.FindPropertyRelative("sound"), clip, 1f);
                    index++;
                }
            }

            Debug.Log($"[SFX] Mapped {index} ability sounds");
        }

        private static void PopulateStructureSounds(SerializedObject so, Dictionary<string, AudioClip> clips)
        {
            var structProp = so.FindProperty("structureSounds");
            if (structProp == null) return;

            structProp.ClearArray();

            var structMappings = new Dictionary<string, StructureSoundType>
            {
                { "SFX-BLD22_wall_breach", StructureSoundType.Damage },
                { "SFX-BLD23_tower_fall", StructureSoundType.Destroy },
                { "SFX-BLD27_gate_open", StructureSoundType.Activate },
                { "SFX-BLD28_gate_close", StructureSoundType.Deactivate },
                { "SFX-BLD13_building_complete", StructureSoundType.Build },
                { "SFX-BLD25_demolish_large", StructureSoundType.Destroy },
            };

            int index = 0;
            foreach (var mapping in structMappings)
            {
                if (clips.TryGetValue(mapping.Key, out AudioClip clip))
                {
                    structProp.InsertArrayElementAtIndex(index);
                    var element = structProp.GetArrayElementAtIndex(index);
                    element.FindPropertyRelative("structure").enumValueIndex = (int)mapping.Value;
                    
                    SetCombatSoundClip(element.FindPropertyRelative("sound"), clip, 1f);
                    index++;
                }
            }

            Debug.Log($"[SFX] Mapped {index} structure sounds");
        }

        private static void PopulateExplosions(SerializedObject so, Dictionary<string, AudioClip> clips)
        {
            // Small explosion
            if (clips.TryGetValue("SFX-FX09_explosion_small", out AudioClip smallExp))
            {
                SetCombatSoundClip(so.FindProperty("smallExplosion"), smallExp, 1f);
            }

            // Large explosion
            if (clips.TryGetValue("SFX-FX10_explosion_large", out AudioClip largeExp))
            {
                SetCombatSoundClip(so.FindProperty("largeExplosion"), largeExp, 1.2f);
            }

            // Debris
            if (clips.TryGetValue("SFX-BLD26_debris_fall", out AudioClip debris))
            {
                SetCombatSoundClip(so.FindProperty("debris"), debris, 0.8f);
            }

            Debug.Log("[SFX] Mapped explosion sounds");
        }

        /// <summary>
        /// Helper to set a CombatSound property with a single clip
        /// CombatSound uses 'clips' array, not 'mainClip'
        /// </summary>
        private static void SetCombatSoundClip(SerializedProperty soundProp, AudioClip clip, float volume, float pitchVariation = 0.1f)
        {
            if (soundProp == null) return;
            
            var clipsProp = soundProp.FindPropertyRelative("clips");
            if (clipsProp != null)
            {
                clipsProp.ClearArray();
                clipsProp.InsertArrayElementAtIndex(0);
                clipsProp.GetArrayElementAtIndex(0).objectReferenceValue = clip;
            }
            
            var volumeProp = soundProp.FindPropertyRelative("volume");
            if (volumeProp != null) volumeProp.floatValue = volume;
            
            var pitchProp = soundProp.FindPropertyRelative("pitchVariation");
            if (pitchProp != null) pitchProp.floatValue = pitchVariation;
        }
    }
}
#endif
