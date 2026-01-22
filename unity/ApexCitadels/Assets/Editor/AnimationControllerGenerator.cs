// ============================================================================
// APEX CITADELS - ANIMATION CONTROLLER GENERATOR
// Editor tool to generate Animator Controllers from Mixamo FBX clips
// Creates ready-to-use character animation systems
// ============================================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace ApexCitadels.Editor
{
    /// <summary>
    /// Generates Animator Controllers from Mixamo animation FBX files.
    /// Creates standard character controllers with locomotion, combat, and interaction states.
    /// </summary>
    public class AnimationControllerGenerator : EditorWindow
    {
        private string animationsFolder = "Assets/Animations/Mixamo";
        private string outputFolder = "Assets/Animations/Controllers";
        private bool generateLocomotion = true;
        private bool generateCombat = true;
        private bool generateInteractions = true;
        
        [MenuItem("Apex Citadels/Advanced/Assets/Generate Animation Controllers", false, 46)]
        public static void ShowWindow()
        {
            var window = GetWindow<AnimationControllerGenerator>("Animation Generator");
            window.minSize = new Vector2(450, 500);
            window.Show();
        }

        [MenuItem("Apex Citadels/Advanced/Assets/Quick Generate Humanoid Controller", false, 47)]
        public static void QuickGenerate()
        {
            GenerateHumanoidController();
            Debug.Log("[Animations] Quick generation complete! HumanoidController created.");
        }

        private void OnGUI()
        {
            GUILayout.Label("Animation Controller Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            animationsFolder = EditorGUILayout.TextField("Animations Folder", animationsFolder);
            outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
            
            GUILayout.Space(10);
            
            // Count animations
            int animCount = 0;
            if (Directory.Exists(animationsFolder))
            {
                animCount = Directory.GetFiles(animationsFolder, "*.fbx").Length;
            }
            EditorGUILayout.LabelField($"FBX Files Found: {animCount}");
            
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("Controller Options:", EditorStyles.boldLabel);
            generateLocomotion = EditorGUILayout.Toggle("Locomotion Layer", generateLocomotion);
            generateCombat = EditorGUILayout.Toggle("Combat Layer", generateCombat);
            generateInteractions = EditorGUILayout.Toggle("Interactions Layer", generateInteractions);

            GUILayout.Space(15);

            if (GUILayout.Button("GENERATE HUMANOID CONTROLLER", GUILayout.Height(40)))
            {
                GenerateHumanoidController();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Generate Simple Locomotion Controller", GUILayout.Height(30)))
            {
                GenerateSimpleLocomotionController();
            }

            if (GUILayout.Button("Generate Combat Controller", GUILayout.Height(30)))
            {
                GenerateCombatController();
            }

            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Animation Naming Convention:\n" +
                "###_AnimationName.fbx\n\n" +
                "Examples:\n" +
                "• 001_Walking.fbx → Walk\n" +
                "• 015_Sword_Swing.fbx → SwordSwing\n" +
                "• 045_Death_Forward.fbx → DeathForward\n\n" +
                "The generator auto-categorizes based on names.",
                MessageType.Info);
        }

        #region Controller Generators

        private static void GenerateHumanoidController()
        {
            Debug.Log("[Animations] Generating Humanoid Controller...");
            
            string outputPath = "Assets/Animations/Controllers";
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            string controllerPath = $"{outputPath}/HumanoidController.controller";
            
            // Create controller
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            
            // Add parameters
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Direction", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsJumping", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsFalling", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsAttacking", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
            controller.AddParameter("AttackType", AnimatorControllerParameterType.Int);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dodge", AnimatorControllerParameterType.Trigger);
            
            // Load animation clips
            var clips = LoadAnimationClips("Assets/Animations/Mixamo");
            
            // Create Base Layer with locomotion
            var rootStateMachine = controller.layers[0].stateMachine;
            rootStateMachine.name = "Locomotion";
            
            CreateLocomotionStates(rootStateMachine, clips);
            
            // Add Combat layer
            controller.AddLayer("Combat");
            var combatLayer = controller.layers[1];
            combatLayer.defaultWeight = 1f;
            
            CreateCombatStates(combatLayer.stateMachine, clips);
            
            // Add Hit Reactions layer
            controller.AddLayer("HitReactions");
            var hitLayer = controller.layers[2];
            hitLayer.defaultWeight = 1f;
            
            CreateHitReactionStates(hitLayer.stateMachine, clips);
            
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[Animations] Created HumanoidController at {controllerPath}");
        }

        private static void GenerateSimpleLocomotionController()
        {
            Debug.Log("[Animations] Generating Simple Locomotion Controller...");
            
            string outputPath = "Assets/Animations/Controllers";
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            string controllerPath = $"{outputPath}/SimpleLocomotion.controller";
            
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            
            var clips = LoadAnimationClips("Assets/Animations/Mixamo");
            var rootStateMachine = controller.layers[0].stateMachine;
            
            // Find idle and walk clips
            AnimationClip idleClip = FindClipByKeyword(clips, "idle", "breathing");
            AnimationClip walkClip = FindClipByKeyword(clips, "walking", "walk");
            AnimationClip runClip = FindClipByKeyword(clips, "running", "run", "fast");
            
            // Create states
            var idleState = rootStateMachine.AddState("Idle", new Vector3(200, 0, 0));
            if (idleClip != null) idleState.motion = idleClip;
            
            var walkState = rootStateMachine.AddState("Walk", new Vector3(400, 0, 0));
            if (walkClip != null) walkState.motion = walkClip;
            
            var runState = rootStateMachine.AddState("Run", new Vector3(400, 100, 0));
            if (runClip != null) runState.motion = runClip;
            
            rootStateMachine.defaultState = idleState;
            
            // Transitions
            var idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.15f;
            
            var walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.15f;
            
            var walkToRun = walkState.AddTransition(runState);
            walkToRun.AddCondition(AnimatorConditionMode.Greater, 0.6f, "Speed");
            walkToRun.hasExitTime = false;
            walkToRun.duration = 0.1f;
            
            var runToWalk = runState.AddTransition(walkState);
            runToWalk.AddCondition(AnimatorConditionMode.Less, 0.6f, "Speed");
            runToWalk.hasExitTime = false;
            runToWalk.duration = 0.1f;
            
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[Animations] Created SimpleLocomotion at {controllerPath}");
        }

        private static void GenerateCombatController()
        {
            Debug.Log("[Animations] Generating Combat Controller...");
            
            string outputPath = "Assets/Animations/Controllers";
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            string controllerPath = $"{outputPath}/CombatController.controller";
            
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            
            controller.AddParameter("IsInCombat", AnimatorControllerParameterType.Bool);
            controller.AddParameter("AttackType", AnimatorControllerParameterType.Int);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Block", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Dodge", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
            
            var clips = LoadAnimationClips("Assets/Animations/Mixamo");
            var rootStateMachine = controller.layers[0].stateMachine;
            
            // Combat idle
            var combatIdleClip = FindClipByKeyword(clips, "swordidle", "combatidle", "alertidle");
            var combatIdle = rootStateMachine.AddState("CombatIdle", new Vector3(200, 0, 0));
            if (combatIdleClip != null) combatIdle.motion = combatIdleClip;
            rootStateMachine.defaultState = combatIdle;
            
            // Sword attacks
            var swordSlash = FindClipByKeyword(clips, "swordslash", "swordswing");
            var slashState = rootStateMachine.AddState("SwordSlash", new Vector3(400, -50, 0));
            if (swordSlash != null) slashState.motion = swordSlash;
            
            // Shield block
            var shieldBlock = FindClipByKeyword(clips, "shieldblock");
            var blockState = rootStateMachine.AddState("Block", new Vector3(400, 50, 0));
            if (shieldBlock != null) blockState.motion = shieldBlock;
            
            // Death
            var deathClip = FindClipByKeyword(clips, "death");
            var deathState = rootStateMachine.AddState("Death", new Vector3(400, 150, 0));
            if (deathClip != null) deathState.motion = deathClip;
            
            // Transitions
            var toSlash = combatIdle.AddTransition(slashState);
            toSlash.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            toSlash.AddCondition(AnimatorConditionMode.Equals, 0, "AttackType");
            toSlash.hasExitTime = false;
            toSlash.duration = 0.1f;
            
            var slashToIdle = slashState.AddTransition(combatIdle);
            slashToIdle.hasExitTime = true;
            slashToIdle.exitTime = 0.9f;
            slashToIdle.duration = 0.1f;
            
            var toBlock = combatIdle.AddTransition(blockState);
            toBlock.AddCondition(AnimatorConditionMode.If, 0, "Block");
            toBlock.hasExitTime = false;
            toBlock.duration = 0.05f;
            
            var blockToIdle = blockState.AddTransition(combatIdle);
            blockToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "Block");
            blockToIdle.hasExitTime = false;
            blockToIdle.duration = 0.15f;
            
            var toDeath = rootStateMachine.AddAnyStateTransition(deathState);
            toDeath.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
            toDeath.hasExitTime = false;
            toDeath.duration = 0.1f;
            
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[Animations] Created CombatController at {controllerPath}");
        }

        #endregion

        #region State Machine Builders

        private static void CreateLocomotionStates(AnimatorStateMachine stateMachine, Dictionary<string, AnimationClip> clips)
        {
            // Idle
            var idleClip = FindClipByKeyword(clips, "breathingidle", "idle");
            var idleState = stateMachine.AddState("Idle", new Vector3(200, 0, 0));
            if (idleClip != null) idleState.motion = idleClip;
            stateMachine.defaultState = idleState;
            
            // Walk
            var walkClip = FindClipByKeyword(clips, "walking");
            var walkState = stateMachine.AddState("Walk", new Vector3(400, 0, 0));
            if (walkClip != null) walkState.motion = walkClip;
            
            // Run
            var runClip = FindClipByKeyword(clips, "running", "fastrun");
            var runState = stateMachine.AddState("Run", new Vector3(600, 0, 0));
            if (runClip != null) runState.motion = runClip;
            
            // Jump
            var jumpClip = FindClipByKeyword(clips, "jump", "runningjump");
            var jumpState = stateMachine.AddState("Jump", new Vector3(400, -100, 0));
            if (jumpClip != null) jumpState.motion = jumpClip;
            
            // Fall
            var fallClip = FindClipByKeyword(clips, "falling", "fallingidle");
            var fallState = stateMachine.AddState("Fall", new Vector3(500, -100, 0));
            if (fallClip != null) fallState.motion = fallClip;
            
            // Land
            var landClip = FindClipByKeyword(clips, "landing", "hardlanding");
            var landState = stateMachine.AddState("Land", new Vector3(600, -100, 0));
            if (landClip != null) landState.motion = landClip;
            
            // Transitions: Idle <-> Walk
            var idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.15f;
            
            var walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.15f;
            
            // Transitions: Walk <-> Run
            var walkToRun = walkState.AddTransition(runState);
            walkToRun.AddCondition(AnimatorConditionMode.Greater, 0.6f, "Speed");
            walkToRun.hasExitTime = false;
            walkToRun.duration = 0.1f;
            
            var runToWalk = runState.AddTransition(walkState);
            runToWalk.AddCondition(AnimatorConditionMode.Less, 0.6f, "Speed");
            runToWalk.hasExitTime = false;
            runToWalk.duration = 0.1f;
            
            // Jump transition from any grounded state
            var toJump = stateMachine.AddAnyStateTransition(jumpState);
            toJump.AddCondition(AnimatorConditionMode.If, 0, "IsJumping");
            toJump.hasExitTime = false;
            toJump.duration = 0.05f;
            
            // Jump -> Fall
            var jumpToFall = jumpState.AddTransition(fallState);
            jumpToFall.AddCondition(AnimatorConditionMode.If, 0, "IsFalling");
            jumpToFall.hasExitTime = false;
            jumpToFall.duration = 0.1f;
            
            // Fall -> Land
            var fallToLand = fallState.AddTransition(landState);
            fallToLand.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            fallToLand.hasExitTime = false;
            fallToLand.duration = 0.05f;
            
            // Land -> Idle
            var landToIdle = landState.AddTransition(idleState);
            landToIdle.hasExitTime = true;
            landToIdle.exitTime = 0.8f;
            landToIdle.duration = 0.1f;
        }

        private static void CreateCombatStates(AnimatorStateMachine stateMachine, Dictionary<string, AnimationClip> clips)
        {
            // Empty state (allows base layer to play)
            var emptyState = stateMachine.AddState("Empty", new Vector3(200, 0, 0));
            stateMachine.defaultState = emptyState;
            
            // Attack states
            var swordSlash = FindClipByKeyword(clips, "swordslash");
            var slashState = stateMachine.AddState("SwordSlash", new Vector3(400, -50, 0));
            if (swordSlash != null) slashState.motion = swordSlash;
            
            var swordThrust = FindClipByKeyword(clips, "swordandshieldattack");
            var thrustState = stateMachine.AddState("SwordThrust", new Vector3(400, 50, 0));
            if (swordThrust != null) thrustState.motion = swordThrust;
            
            var heavyAttack = FindClipByKeyword(clips, "greatswordslash", "twohandswordattack");
            var heavyState = stateMachine.AddState("HeavyAttack", new Vector3(400, 150, 0));
            if (heavyAttack != null) heavyState.motion = heavyAttack;
            
            // Transitions
            var toSlash = emptyState.AddTransition(slashState);
            toSlash.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
            toSlash.AddCondition(AnimatorConditionMode.Equals, 0, "AttackType");
            toSlash.hasExitTime = false;
            toSlash.duration = 0.05f;
            
            var slashToEmpty = slashState.AddTransition(emptyState);
            slashToEmpty.hasExitTime = true;
            slashToEmpty.exitTime = 0.85f;
            slashToEmpty.duration = 0.1f;
            
            var toThrust = emptyState.AddTransition(thrustState);
            toThrust.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
            toThrust.AddCondition(AnimatorConditionMode.Equals, 1, "AttackType");
            toThrust.hasExitTime = false;
            toThrust.duration = 0.05f;
            
            var thrustToEmpty = thrustState.AddTransition(emptyState);
            thrustToEmpty.hasExitTime = true;
            thrustToEmpty.exitTime = 0.85f;
            thrustToEmpty.duration = 0.1f;
            
            var toHeavy = emptyState.AddTransition(heavyState);
            toHeavy.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
            toHeavy.AddCondition(AnimatorConditionMode.Equals, 2, "AttackType");
            toHeavy.hasExitTime = false;
            toHeavy.duration = 0.05f;
            
            var heavyToEmpty = heavyState.AddTransition(emptyState);
            heavyToEmpty.hasExitTime = true;
            heavyToEmpty.exitTime = 0.9f;
            heavyToEmpty.duration = 0.15f;
        }

        private static void CreateHitReactionStates(AnimatorStateMachine stateMachine, Dictionary<string, AnimationClip> clips)
        {
            // Empty state
            var emptyState = stateMachine.AddState("Empty", new Vector3(200, 0, 0));
            stateMachine.defaultState = emptyState;
            
            // Hit reactions
            var hitClip = FindClipByKeyword(clips, "hitreaction");
            var hitState = stateMachine.AddState("Hit", new Vector3(400, 0, 0));
            if (hitClip != null) hitState.motion = hitClip;
            
            var knockbackClip = FindClipByKeyword(clips, "knockback", "heavyhit");
            var knockbackState = stateMachine.AddState("Knockback", new Vector3(400, 100, 0));
            if (knockbackClip != null) knockbackState.motion = knockbackClip;
            
            var dodgeClip = FindClipByKeyword(clips, "dodge", "combatroll");
            var dodgeState = stateMachine.AddState("Dodge", new Vector3(400, -100, 0));
            if (dodgeClip != null) dodgeState.motion = dodgeClip;
            
            // Death
            var deathClip = FindClipByKeyword(clips, "deathforward", "death");
            var deathState = stateMachine.AddState("Death", new Vector3(600, 0, 0));
            if (deathClip != null) deathState.motion = deathClip;
            
            // Transitions
            var toHit = emptyState.AddTransition(hitState);
            toHit.AddCondition(AnimatorConditionMode.If, 0, "Hit");
            toHit.hasExitTime = false;
            toHit.duration = 0.05f;
            
            var hitToEmpty = hitState.AddTransition(emptyState);
            hitToEmpty.hasExitTime = true;
            hitToEmpty.exitTime = 0.8f;
            hitToEmpty.duration = 0.1f;
            
            var toDodge = emptyState.AddTransition(dodgeState);
            toDodge.AddCondition(AnimatorConditionMode.If, 0, "Dodge");
            toDodge.hasExitTime = false;
            toDodge.duration = 0.05f;
            
            var dodgeToEmpty = dodgeState.AddTransition(emptyState);
            dodgeToEmpty.hasExitTime = true;
            dodgeToEmpty.exitTime = 0.9f;
            dodgeToEmpty.duration = 0.1f;
            
            var toDeath = stateMachine.AddAnyStateTransition(deathState);
            toDeath.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
            toDeath.hasExitTime = false;
            toDeath.duration = 0.1f;
        }

        #endregion

        #region Helpers

        private static Dictionary<string, AnimationClip> LoadAnimationClips(string folder)
        {
            var clips = new Dictionary<string, AnimationClip>();
            
            if (!Directory.Exists(folder))
            {
                Debug.LogWarning($"[Animations] Folder not found: {folder}");
                return clips;
            }
            
            string[] fbxFiles = Directory.GetFiles(folder, "*.fbx");
            
            foreach (var file in fbxFiles)
            {
                string assetPath = file.Replace("\\", "/");
                var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                
                foreach (var asset in assets)
                {
                    if (asset is AnimationClip clip && !clip.name.StartsWith("__"))
                    {
                        string fileName = Path.GetFileNameWithoutExtension(file);
                        // Remove number prefix
                        if (fileName.Length > 4 && fileName[3] == '_')
                        {
                            fileName = fileName.Substring(4);
                        }
                        
                        string key = fileName.ToLower().Replace("_", "").Replace(" ", "");
                        clips[key] = clip;
                        break;
                    }
                }
            }
            
            Debug.Log($"[Animations] Loaded {clips.Count} animation clips");
            return clips;
        }

        private static AnimationClip FindClipByKeyword(Dictionary<string, AnimationClip> clips, params string[] keywords)
        {
            foreach (var keyword in keywords)
            {
                string searchKey = keyword.ToLower().Replace("_", "").Replace(" ", "");
                
                // Exact match
                if (clips.TryGetValue(searchKey, out var exactMatch))
                {
                    return exactMatch;
                }
                
                // Partial match
                foreach (var kvp in clips)
                {
                    if (kvp.Key.Contains(searchKey))
                    {
                        return kvp.Value;
                    }
                }
            }
            
            return null;
        }

        #endregion
    }
}
#endif
