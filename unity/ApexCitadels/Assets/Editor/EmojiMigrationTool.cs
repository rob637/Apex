// ============================================================================
// APEX CITADELS - EMOJI MIGRATION TOOL
// Editor tool to replace emoji characters with GameIcons references
// Run from: Window > Apex Citadels > Migrate Emojis
// ============================================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace ApexCitadels.Editor
{
    public class EmojiMigrationTool : EditorWindow
    {
        private Vector2 _scrollPos;
        private bool _dryRun = true;
        private string _targetFolder = "Assets/Scripts";
        private List<FileReport> _reports = new List<FileReport>();
        private bool _scanned = false;
        
        // Emoji to GameIcons mapping
        private static readonly Dictionary<string, string> EmojiToGameIcons = new Dictionary<string, string>
        {
            // Resources
            { "💰", "GameIcons.Gold" },
            { "💎", "GameIcons.Gem" },
            { "💵", "GameIcons.Gold" },
            { "🪙", "GameIcons.Coin" },
            { "📦", "GameIcons.Chest" },
            
            // Combat
            { "⚔️", "GameIcons.Battle" },
            { "⚔", "GameIcons.Battle" },
            { "🗡️", "GameIcons.Sword" },
            { "🗡", "GameIcons.Sword" },
            { "🛡️", "GameIcons.Shield" },
            { "🛡", "GameIcons.Shield" },
            { "🏆", "GameIcons.Trophy" },
            { "👑", "GameIcons.Crown" },
            { "⭐", "GameIcons.Star" },
            { "🌟", "GameIcons.StarFilled" },
            { "💯", "GameIcons.Victory" },
            { "🐕", "GameIcons.Shield" }, // Underdog
            
            // Territories
            { "🏠", "GameIcons.Home" },
            { "🏘️", "GameIcons.Territory" },
            { "🏘", "GameIcons.Territory" },
            { "🏰", "GameIcons.Citadel" },
            { "🏯", "GameIcons.Citadel" },
            { "🌍", "GameIcons.Globe" },
            { "🌎", "GameIcons.Globe" },
            { "🌏", "GameIcons.Globe" },
            { "🧭", "GameIcons.Map" },
            { "🗺️", "GameIcons.Map" },
            { "🗺", "GameIcons.Map" },
            
            // Building
            { "🔨", "GameIcons.Hammer" },
            { "📐", "GameIcons.Build" },
            { "🏗️", "GameIcons.Build" },
            { "🏗", "GameIcons.Build" },
            { "🎨", "GameIcons.Paint" },
            
            // Social
            { "🤝", "GameIcons.Alliance" },
            { "👔", "GameIcons.Crown" },
            { "🕊️", "GameIcons.Peace" },
            { "🕊", "GameIcons.Peace" },
            { "💬", "GameIcons.Chat" },
            { "🎁", "GameIcons.Gift" },
            { "👥", "GameIcons.Alliance" },
            
            // Collection
            { "🤑", "GameIcons.Gem" },
            { "🎖️", "GameIcons.Medal" },
            { "🎖", "GameIcons.Medal" },
            { "🏅", "GameIcons.Medal" },
            { "📅", "GameIcons.Calendar" },
            { "🥉", "GameIcons.BronzeMedal" },
            { "🥈", "GameIcons.SilverMedal" },
            { "🥇", "GameIcons.GoldMedal" },
            
            // Status
            { "✅", "GameIcons.Success" },
            { "❌", "GameIcons.Cross" },
            { "⚠️", "GameIcons.Warning" },
            { "⚠", "GameIcons.Warning" },
            { "🔔", "GameIcons.Bell" },
            { "🔒", "GameIcons.Lock" },
            { "🔓", "GameIcons.Unlock" },
            
            // Misc
            { "⚙️", "GameIcons.Settings" },
            { "⚙", "GameIcons.Settings" },
            { "👁️", "GameIcons.Eye" },
            { "👁", "GameIcons.Eye" },
            { "🎉", "GameIcons.Party" },
            { "🔥", "GameIcons.Fire" },
            { "⚡", "GameIcons.Lightning" },
            { "📜", "GameIcons.Scroll" },
            { "🎯", "GameIcons.Target" },
            { "📊", "GameIcons.Statistics" },
            { "⏰", "GameIcons.Clock" },
            { "🕐", "GameIcons.Clock" },
            { "❤️", "GameIcons.Heart" },
            { "❤", "GameIcons.Heart" },
            { "🏠", "GameIcons.Home" },
            
            // UI Elements
            { "✕", "X" }, // Close button - just use X
            { "▶", ">" }, // Play
            { "⏸", "||" }, // Pause
            { "⏹", "[]" }, // Stop
        };

        [MenuItem("Apex Citadels/Utilities/Migrate Emojis to GameIcons", false, 120)]
        public static void ShowWindow()
        {
            GetWindow<EmojiMigrationTool>("Emoji Migration");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Emoji to GameIcons Migration Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "This tool scans C# files for emoji characters and shows how they would be " +
                "replaced with GameIcons references. Run in dry-run mode first to preview changes.",
                MessageType.Info);
            
            EditorGUILayout.Space();
            
            _targetFolder = EditorGUILayout.TextField("Target Folder", _targetFolder);
            _dryRun = EditorGUILayout.Toggle("Dry Run (Preview Only)", _dryRun);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan Files"))
            {
                ScanFiles();
            }
            
            GUI.enabled = _scanned && !_dryRun;
            if (GUILayout.Button("Apply Changes"))
            {
                ApplyChanges();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Results
            if (_reports.Count > 0)
            {
                int totalEmojis = _reports.Sum(r => r.EmojiCount);
                EditorGUILayout.LabelField($"Found {totalEmojis} emojis in {_reports.Count} files", EditorStyles.boldLabel);
                
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(400));
                
                foreach (var report in _reports.OrderByDescending(r => r.EmojiCount))
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"{report.FileName} ({report.EmojiCount} emojis)", EditorStyles.boldLabel);
                    
                    foreach (var emoji in report.Emojis.Take(10))
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"  Line {emoji.Line}: \"{emoji.Original}\"", GUILayout.Width(200));
                        EditorGUILayout.LabelField("→", GUILayout.Width(20));
                        EditorGUILayout.LabelField(emoji.Replacement);
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    if (report.Emojis.Count > 10)
                    {
                        EditorGUILayout.LabelField($"  ... and {report.Emojis.Count - 10} more");
                    }
                    
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Supported Emoji Mappings:", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            int shown = 0;
            foreach (var kvp in EmojiToGameIcons)
            {
                if (shown++ > 15) break;
                EditorGUILayout.LabelField($"  {kvp.Key} → {kvp.Value}");
            }
            EditorGUILayout.LabelField($"  ... and {EmojiToGameIcons.Count - 15} more mappings");
            EditorGUILayout.EndVertical();
        }

        private void ScanFiles()
        {
            _reports.Clear();
            _scanned = true;
            
            string fullPath = Path.Combine(Application.dataPath, _targetFolder.Replace("Assets/", ""));
            if (!Directory.Exists(fullPath))
            {
                Debug.LogError($"Directory not found: {fullPath}");
                return;
            }
            
            string[] csFiles = Directory.GetFiles(fullPath, "*.cs", SearchOption.AllDirectories);
            
            foreach (string file in csFiles)
            {
                // Skip already migrated files and GameIcons itself
                if (file.Contains("GameIcons.cs") || file.Contains("EmojiMigrationTool"))
                    continue;
                    
                string content = File.ReadAllText(file);
                var report = new FileReport
                {
                    FilePath = file,
                    FileName = Path.GetFileName(file),
                    Emojis = new List<EmojiOccurrence>()
                };
                
                string[] lines = content.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    foreach (var kvp in EmojiToGameIcons)
                    {
                        if (line.Contains(kvp.Key))
                        {
                            report.Emojis.Add(new EmojiOccurrence
                            {
                                Line = i + 1,
                                Original = kvp.Key,
                                Replacement = kvp.Value,
                                Context = line.Trim()
                            });
                        }
                    }
                }
                
                if (report.Emojis.Count > 0)
                {
                    report.EmojiCount = report.Emojis.Count;
                    _reports.Add(report);
                }
            }
            
            Debug.Log($"Scan complete. Found {_reports.Sum(r => r.EmojiCount)} emojis in {_reports.Count} files.");
        }

        private void ApplyChanges()
        {
            int filesModified = 0;
            int emojisReplaced = 0;
            
            foreach (var report in _reports)
            {
                string content = File.ReadAllText(report.FilePath);
                string modified = content;
                
                // Check if file already has the using statement
                bool hasUsing = content.Contains("using ApexCitadels.UI;");
                
                foreach (var kvp in EmojiToGameIcons)
                {
                    // Smart replacement - handle both direct strings and interpolated strings
                    if (modified.Contains(kvp.Key))
                    {
                        // Count replacements
                        int count = Regex.Matches(modified, Regex.Escape(kvp.Key)).Count;
                        emojisReplaced += count;
                        
                        // Replace in string literals
                        // Pattern: "text⚔️text" -> $"text{GameIcons.Battle}text"
                        // But be careful with already interpolated strings
                        modified = modified.Replace($"\"{kvp.Key}", $"$\"{{{kvp.Value}}}");
                        modified = modified.Replace($"{kvp.Key}\"", $"{{{kvp.Value}}}\"");
                        modified = modified.Replace($"{kvp.Key}", $"{{{kvp.Value}}}");
                        
                        // Fix double interpolation markers
                        modified = modified.Replace("$$\"", "$\"");
                        modified = modified.Replace("{{{{", "{{");
                        modified = modified.Replace("}}}}", "}}");
                    }
                }
                
                // Add using statement if needed and file was modified
                if (!hasUsing && modified != content && !report.FilePath.Contains("Editor"))
                {
                    // Find the last using statement
                    int lastUsing = modified.LastIndexOf("using ");
                    if (lastUsing >= 0)
                    {
                        int lineEnd = modified.IndexOf('\n', lastUsing);
                        if (lineEnd >= 0)
                        {
                            modified = modified.Insert(lineEnd + 1, "using ApexCitadels.UI;\n");
                        }
                    }
                }
                
                if (modified != content)
                {
                    File.WriteAllText(report.FilePath, modified);
                    filesModified++;
                    Debug.Log($"Modified: {report.FileName}");
                }
            }
            
            AssetDatabase.Refresh();
            Debug.Log($"Migration complete. Modified {filesModified} files, replaced {emojisReplaced} emojis.");
            EditorUtility.DisplayDialog("Migration Complete", 
                $"Modified {filesModified} files\nReplaced {emojisReplaced} emojis\n\nPlease review changes and fix any compilation errors.", 
                "OK");
        }
        
        private class FileReport
        {
            public string FilePath;
            public string FileName;
            public int EmojiCount;
            public List<EmojiOccurrence> Emojis;
        }
        
        private class EmojiOccurrence
        {
            public int Line;
            public string Original;
            public string Replacement;
            public string Context;
        }
    }
}
#endif
