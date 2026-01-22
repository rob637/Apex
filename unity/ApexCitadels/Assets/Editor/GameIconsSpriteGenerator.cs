// ============================================================================
// APEX CITADELS - GAME ICONS SPRITE GENERATOR
// Editor tool to generate and configure TMP Sprite Assets
// ============================================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;
using System.Collections.Generic;

namespace ApexCitadels.Editor
{
    /// <summary>
    /// Editor window for generating game icon sprite sheets.
    /// Creates a TMP Sprite Asset with all game icons.
    /// </summary>
    public class GameIconsSpriteGenerator : EditorWindow
    {
        private const int ICON_SIZE = 64;
        private const int PADDING = 2;
        private const int ATLAS_SIZE = 512;
        
        private bool includeGlow = true;
        private Color glowColor = new Color(1f, 0.9f, 0.5f, 0.5f);
        private int glowRadius = 2;
        
        [MenuItem("Apex Citadels/Advanced/Assets/Generate Icon Sprites", false, 48)]
        public static void ShowWindow()
        {
            var window = GetWindow<GameIconsSpriteGenerator>("Icon Generator");
            window.minSize = new Vector2(400, 500);
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("🎨 Game Icons Sprite Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "This tool generates a sprite sheet with all game icons.\n\n" +
                "The generated sprites can be used with TextMeshPro:\n" +
                "• Pixel-perfect icons at any size\n" +
                "• Consistent styling across all UI\n" +
                "• Better performance than emoji fonts\n" +
                "• Can be customized with your own art",
                MessageType.Info);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Generation Settings", EditorStyles.boldLabel);
            includeGlow = EditorGUILayout.Toggle("Include Glow Effect", includeGlow);
            if (includeGlow)
            {
                glowColor = EditorGUILayout.ColorField("Glow Color", glowColor);
                glowRadius = EditorGUILayout.IntSlider("Glow Radius", glowRadius, 1, 5);
            }
            
            EditorGUILayout.Space(20);
            
            GUI.backgroundColor = new Color(0.3f, 0.9f, 0.3f);
            if (GUILayout.Button("Generate Sprite Sheet", GUILayout.Height(40)))
            {
                GenerateSpriteSheet();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space();
            
            GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
            if (GUILayout.Button("Create TMP Sprite Asset", GUILayout.Height(30)))
            {
                CreateTMPSpriteAsset();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Icon Preview", EditorStyles.boldLabel);
            
            // Show icon list
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            foreach (var icon in IconDefinitions)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(icon.name, GUILayout.Width(150));
                EditorGUILayout.ColorField(icon.color, GUILayout.Width(60));
                EditorGUILayout.LabelField(icon.symbol, GUILayout.Width(40));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "After generation, the sprite asset will be at:\n" +
                "Assets/Resources/UI/GameIconsSpriteAsset.asset\n\n" +
                "To use custom art, replace the generated PNG with your own sprite sheet " +
                "keeping the same icon positions.",
                MessageType.Info);
        }
        
        private Vector2 scrollPos;
        
        // Icon definitions with colors and symbols
        private static readonly List<IconDef> IconDefinitions = new List<IconDef>
        {
            // Resources (Row 0)
            new IconDef("gold", new Color(1f, 0.84f, 0f), "●", IconShape.Circle),
            new IconDef("gems", new Color(0f, 0.75f, 1f), "◆", IconShape.Diamond),
            new IconDef("crystals", new Color(0.58f, 0.44f, 0.86f), "◈", IconShape.Diamond),
            new IconDef("apex_coins", new Color(0.85f, 0.44f, 0.84f), "◉", IconShape.Circle),
            new IconDef("metal", new Color(0.66f, 0.66f, 0.66f), "▣", IconShape.Square),
            new IconDef("wood", new Color(0.55f, 0.27f, 0.07f), "▤", IconShape.Square),
            new IconDef("stone", new Color(0.41f, 0.41f, 0.41f), "▥", IconShape.Square),
            new IconDef("food", new Color(0.2f, 0.8f, 0.2f), "❀", IconShape.Flower),
            
            // Resources continued + Combat (Row 1)
            new IconDef("xp", new Color(1f, 0.84f, 0f), "★", IconShape.Star),
            new IconDef("sword", new Color(0.75f, 0.75f, 0.75f), "⚔", IconShape.Sword),
            new IconDef("shield", new Color(0.25f, 0.41f, 0.88f), "◐", IconShape.Shield),
            new IconDef("crossed_swords", new Color(0.86f, 0.08f, 0.24f), "⚔", IconShape.CrossedSwords),
            new IconDef("helmet", new Color(0.44f, 0.5f, 0.56f), "⛑", IconShape.Helmet),
            new IconDef("arrow", new Color(0.55f, 0.27f, 0.07f), "➤", IconShape.Arrow),
            new IconDef("cannon", new Color(0.18f, 0.31f, 0.31f), "◎", IconShape.Cannon),
            new IconDef("flag", new Color(1f, 0.27f, 0f), "⚑", IconShape.Flag),
            
            // Territory & Buildings (Row 2)
            new IconDef("castle", new Color(0.55f, 0.27f, 0.07f), "♜", IconShape.Castle),
            new IconDef("tower", new Color(0.41f, 0.41f, 0.41f), "▲", IconShape.Tower),
            new IconDef("wall", new Color(0.66f, 0.66f, 0.66f), "▬", IconShape.Wall),
            new IconDef("construction", new Color(1f, 0.65f, 0f), "⚒", IconShape.Construction),
            new IconDef("star", new Color(1f, 0.84f, 0f), "☆", IconShape.StarOutline),
            new IconDef("star_filled", new Color(1f, 0.84f, 0f), "★", IconShape.Star),
            new IconDef("trophy", new Color(1f, 0.84f, 0f), "🏆", IconShape.Trophy),
            new IconDef("medal", new Color(0.8f, 0.52f, 0.25f), "◉", IconShape.Medal),
            
            // Status (Row 3)
            new IconDef("crown", new Color(1f, 0.84f, 0f), "♕", IconShape.Crown),
            new IconDef("heart", new Color(1f, 0.41f, 0.71f), "♥", IconShape.Heart),
            new IconDef("clock", new Color(0.53f, 0.81f, 0.92f), "◔", IconShape.Clock),
            new IconDef("calendar", new Color(0.47f, 0.53f, 0.6f), "▦", IconShape.Calendar),
            new IconDef("alliance", new Color(0.25f, 0.41f, 0.88f), "⚐", IconShape.Alliance),
            new IconDef("handshake", new Color(0.2f, 0.8f, 0.2f), "⚌", IconShape.Handshake),
            new IconDef("chat", new Color(0.53f, 0.81f, 0.92f), "◯", IconShape.Chat),
            new IconDef("gift", new Color(1f, 0.41f, 0.71f), "◈", IconShape.Gift),
            
            // Social & Actions (Row 4)
            new IconDef("mail", new Color(0.87f, 0.63f, 0.87f), "✉", IconShape.Mail),
            new IconDef("attack", new Color(1f, 0.27f, 0f), "⚡", IconShape.Attack),
            new IconDef("defend", new Color(0.25f, 0.41f, 0.88f), "◐", IconShape.Shield),
            new IconDef("scout", new Color(0.13f, 0.55f, 0.13f), "◎", IconShape.Scout),
            new IconDef("upgrade", new Color(0.2f, 0.8f, 0.2f), "▲", IconShape.Upgrade),
            new IconDef("collect", new Color(1f, 0.84f, 0f), "◉", IconShape.Collect),
            new IconDef("alert", new Color(1f, 0.27f, 0f), "◬", IconShape.Alert),
            new IconDef("info", new Color(0.25f, 0.41f, 0.88f), "◉", IconShape.Info),
            
            // Notifications & Misc (Row 5)
            new IconDef("success", new Color(0.2f, 0.8f, 0.2f), "✓", IconShape.Success),
            new IconDef("warning", new Color(1f, 0.65f, 0f), "◬", IconShape.Warning),
            new IconDef("settings", new Color(0.5f, 0.5f, 0.5f), "⚙", IconShape.Settings),
            new IconDef("map", new Color(0.13f, 0.55f, 0.13f), "◫", IconShape.Map),
            new IconDef("compass", new Color(0.25f, 0.41f, 0.88f), "◎", IconShape.Compass),
            new IconDef("eye", new Color(0.53f, 0.81f, 0.92f), "◉", IconShape.Eye),
            new IconDef("party", new Color(1f, 0.41f, 0.71f), "✦", IconShape.Party),
            new IconDef("fire", new Color(1f, 0.27f, 0f), "◢", IconShape.Fire),
            
            // Extra (Row 6)
            new IconDef("lightning", new Color(1f, 0.84f, 0f), "⚡", IconShape.Lightning),
        };
        
        private enum IconShape
        {
            Circle, Diamond, Square, Star, StarOutline, Flower,
            Sword, Shield, CrossedSwords, Helmet, Arrow, Cannon, Flag,
            Castle, Tower, Wall, Construction, Trophy, Medal, Crown,
            Heart, Clock, Calendar, Alliance, Handshake, Chat, Gift,
            Mail, Attack, Scout, Upgrade, Collect, Alert, Info,
            Success, Warning, Settings, Map, Compass, Eye, Party, Fire, Lightning
        }
        
        private struct IconDef
        {
            public string name;
            public Color color;
            public string symbol;
            public IconShape shape;
            
            public IconDef(string name, Color color, string symbol, IconShape shape)
            {
                this.name = name;
                this.color = color;
                this.symbol = symbol;
                this.shape = shape;
            }
        }
        
        private void GenerateSpriteSheet()
        {
            // Calculate grid size
            int iconsPerRow = ATLAS_SIZE / (ICON_SIZE + PADDING);
            int rows = Mathf.CeilToInt(IconDefinitions.Count / (float)iconsPerRow);
            int atlasHeight = rows * (ICON_SIZE + PADDING);
            
            // Create texture
            Texture2D atlas = new Texture2D(ATLAS_SIZE, atlasHeight, TextureFormat.RGBA32, false);
            
            // Clear to transparent
            Color[] clearPixels = new Color[ATLAS_SIZE * atlasHeight];
            for (int i = 0; i < clearPixels.Length; i++)
                clearPixels[i] = Color.clear;
            atlas.SetPixels(clearPixels);
            
            // Draw each icon
            for (int i = 0; i < IconDefinitions.Count; i++)
            {
                int col = i % iconsPerRow;
                int row = i / iconsPerRow;
                
                int x = col * (ICON_SIZE + PADDING) + PADDING / 2;
                int y = atlasHeight - (row + 1) * (ICON_SIZE + PADDING) + PADDING / 2;
                
                DrawIcon(atlas, x, y, ICON_SIZE, IconDefinitions[i]);
            }
            
            atlas.Apply();
            
            // Ensure directory exists
            string dirPath = "Assets/Resources/UI";
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            
            // Save PNG
            string pngPath = Path.Combine(dirPath, "GameIconsSpriteSheet.png");
            byte[] pngBytes = atlas.EncodeToPNG();
            File.WriteAllBytes(pngPath, pngBytes);
            
            AssetDatabase.Refresh();
            
            // Configure texture import settings
            TextureImporter importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 1024;
                
                // Create sprite sheet meta data
                List<SpriteMetaData> spriteData = new List<SpriteMetaData>();
                int iconsPerRowActual = ATLAS_SIZE / (ICON_SIZE + PADDING);
                
                for (int i = 0; i < IconDefinitions.Count; i++)
                {
                    int col = i % iconsPerRowActual;
                    int row = i / iconsPerRowActual;
                    
                    SpriteMetaData meta = new SpriteMetaData();
                    meta.name = IconDefinitions[i].name;
                    meta.rect = new Rect(
                        col * (ICON_SIZE + PADDING) + PADDING / 2,
                        atlasHeight - (row + 1) * (ICON_SIZE + PADDING) + PADDING / 2,
                        ICON_SIZE,
                        ICON_SIZE
                    );
                    meta.pivot = new Vector2(0.5f, 0.5f);
                    meta.alignment = (int)SpriteAlignment.Center;
                    spriteData.Add(meta);
                }
                
                // Use SerializedObject to set spritesheet data (avoids deprecated API)
                var serializedImporter = new SerializedObject(importer);
                var spriteSheetProperty = serializedImporter.FindProperty("m_SpriteSheet.m_Sprites");
                spriteSheetProperty.ClearArray();
                
                for (int i = 0; i < spriteData.Count; i++)
                {
                    spriteSheetProperty.InsertArrayElementAtIndex(i);
                    var element = spriteSheetProperty.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("m_Name").stringValue = spriteData[i].name;
                    element.FindPropertyRelative("m_Rect").rectValue = spriteData[i].rect;
                    element.FindPropertyRelative("m_Pivot").vector2Value = spriteData[i].pivot;
                    element.FindPropertyRelative("m_Alignment").intValue = spriteData[i].alignment;
                    element.FindPropertyRelative("m_Border").vector4Value = Vector4.zero;
                }
                
                serializedImporter.ApplyModifiedPropertiesWithoutUndo();
                importer.SaveAndReimport();
            }
            
            Debug.Log($"[GameIcons] Sprite sheet generated at {pngPath} with {IconDefinitions.Count} icons");
            EditorUtility.DisplayDialog("Success", 
                $"Sprite sheet generated with {IconDefinitions.Count} icons!\n\n" +
                "Now click 'Create TMP Sprite Asset' to complete setup.", "OK");
        }
        
        private void DrawIcon(Texture2D atlas, int startX, int startY, int size, IconDef icon)
        {
            int centerX = startX + size / 2;
            int centerY = startY + size / 2;
            int radius = size / 2 - 4;
            
            // Draw glow first if enabled
            if (includeGlow)
            {
                Color glow = new Color(icon.color.r, icon.color.g, icon.color.b, 0.3f);
                DrawCircle(atlas, centerX, centerY, radius + glowRadius, glow);
            }
            
            // Draw based on shape
            switch (icon.shape)
            {
                case IconShape.Circle:
                case IconShape.Collect:
                    DrawCircle(atlas, centerX, centerY, radius, icon.color);
                    DrawCircle(atlas, centerX, centerY, radius - 4, icon.color * 0.7f);
                    break;
                    
                case IconShape.Diamond:
                case IconShape.Gift:
                    DrawDiamond(atlas, centerX, centerY, radius, icon.color);
                    break;
                    
                case IconShape.Square:
                case IconShape.Calendar:
                    DrawSquare(atlas, startX + 4, startY + 4, size - 8, icon.color);
                    break;
                    
                case IconShape.Star:
                case IconShape.Party:
                    DrawStar(atlas, centerX, centerY, radius, icon.color, true);
                    break;
                    
                case IconShape.StarOutline:
                    DrawStar(atlas, centerX, centerY, radius, icon.color, false);
                    break;
                    
                case IconShape.Shield:
                    DrawShield(atlas, centerX, centerY, radius, icon.color);
                    break;
                    
                case IconShape.Sword:
                case IconShape.CrossedSwords:
                    DrawSword(atlas, centerX, centerY, radius, icon.color);
                    break;
                    
                case IconShape.Flag:
                    DrawFlag(atlas, centerX, centerY, radius, icon.color);
                    break;
                    
                case IconShape.Trophy:
                    DrawTrophy(atlas, centerX, centerY, radius, icon.color);
                    break;
                    
                case IconShape.Crown:
                    DrawCrown(atlas, centerX, centerY, radius, icon.color);
                    break;
                    
                case IconShape.Heart:
                    DrawHeart(atlas, centerX, centerY, radius, icon.color);
                    break;
                    
                case IconShape.Arrow:
                case IconShape.Upgrade:
                    DrawArrow(atlas, centerX, centerY, radius, icon.color);
                    break;
                    
                case IconShape.Settings:
                    DrawGear(atlas, centerX, centerY, radius, icon.color);
                    break;
                    
                case IconShape.Alert:
                case IconShape.Warning:
                    DrawTriangle(atlas, centerX, centerY, radius, icon.color);
                    break;
                    
                case IconShape.Success:
                    DrawCheckmark(atlas, centerX, centerY, radius, icon.color);
                    break;
                    
                case IconShape.Lightning:
                case IconShape.Attack:
                    DrawLightning(atlas, centerX, centerY, radius, icon.color);
                    break;
                    
                case IconShape.Eye:
                case IconShape.Scout:
                    DrawEye(atlas, centerX, centerY, radius, icon.color);
                    break;
                    
                default:
                    // Default to filled circle
                    DrawCircle(atlas, centerX, centerY, radius, icon.color);
                    break;
            }
        }
        
        // Drawing primitives
        private void DrawCircle(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= radius * radius)
                    {
                        int px = cx + x;
                        int py = cy + y;
                        if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                        {
                            tex.SetPixel(px, py, color);
                        }
                    }
                }
            }
        }
        
        private void DrawDiamond(Texture2D tex, int cx, int cy, int size, Color color)
        {
            for (int y = -size; y <= size; y++)
            {
                for (int x = -size; x <= size; x++)
                {
                    if (Mathf.Abs(x) + Mathf.Abs(y) <= size)
                    {
                        int px = cx + x;
                        int py = cy + y;
                        if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                        {
                            tex.SetPixel(px, py, color);
                        }
                    }
                }
            }
        }
        
        private void DrawSquare(Texture2D tex, int x, int y, int size, Color color)
        {
            for (int py = y; py < y + size && py < tex.height; py++)
            {
                for (int px = x; px < x + size && px < tex.width; px++)
                {
                    if (px >= 0 && py >= 0)
                    {
                        tex.SetPixel(px, py, color);
                    }
                }
            }
        }
        
        private void DrawStar(Texture2D tex, int cx, int cy, int radius, Color color, bool filled)
        {
            // 5-pointed star
            float[] angles = { -90, -18, 54, 126, 198 };
            Vector2[] outer = new Vector2[5];
            Vector2[] inner = new Vector2[5];
            
            for (int i = 0; i < 5; i++)
            {
                float rad = angles[i] * Mathf.Deg2Rad;
                outer[i] = new Vector2(cx + radius * Mathf.Cos(rad), cy + radius * Mathf.Sin(rad));
                
                float innerRad = (angles[i] + 36) * Mathf.Deg2Rad;
                inner[i] = new Vector2(cx + radius * 0.4f * Mathf.Cos(innerRad), cy + radius * 0.4f * Mathf.Sin(innerRad));
            }
            
            if (filled)
            {
                // Fill the star
                for (int y = cy - radius; y <= cy + radius; y++)
                {
                    for (int x = cx - radius; x <= cx + radius; x++)
                    {
                        if (IsPointInStar(x, y, cx, cy, radius))
                        {
                            if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                            {
                                tex.SetPixel(x, y, color);
                            }
                        }
                    }
                }
            }
            else
            {
                // Draw outline
                for (int i = 0; i < 5; i++)
                {
                    DrawLine(tex, (int)outer[i].x, (int)outer[i].y, (int)inner[i].x, (int)inner[i].y, color);
                    DrawLine(tex, (int)inner[i].x, (int)inner[i].y, (int)outer[(i+1)%5].x, (int)outer[(i+1)%5].y, color);
                }
            }
        }
        
        private bool IsPointInStar(int x, int y, int cx, int cy, int radius)
        {
            float dx = x - cx;
            float dy = y - cy;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist > radius) return false;
            
            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            angle = (angle + 90 + 360) % 360;
            
            float segment = angle / 36f;
            float segmentAngle = (segment % 2) * 36f;
            
            float starRadius;
            if (segmentAngle < 36)
            {
                float t = segmentAngle / 36f;
                starRadius = Mathf.Lerp(radius, radius * 0.4f, t);
            }
            else
            {
                starRadius = radius * 0.4f;
            }
            
            return dist <= starRadius * 1.2f;
        }
        
        private void DrawShield(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                int dy = y - cy;
                float yNorm = dy / (float)radius;
                int width = (int)(radius * (1 - yNorm * yNorm * 0.3f));
                
                if (yNorm > 0.5f)
                {
                    width = (int)(width * (1 - (yNorm - 0.5f) * 2));
                }
                
                for (int x = cx - width; x <= cx + width; x++)
                {
                    if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                    {
                        tex.SetPixel(x, y, color);
                    }
                }
            }
        }
        
        private void DrawSword(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            // Blade
            DrawLine(tex, cx, cy - radius, cx, cy + radius / 2, color, 3);
            // Guard
            DrawLine(tex, cx - radius / 2, cy, cx + radius / 2, cy, color, 2);
            // Handle
            DrawLine(tex, cx, cy + radius / 2, cx, cy + radius, new Color(0.4f, 0.2f, 0.1f), 4);
        }
        
        private void DrawFlag(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            // Pole
            DrawLine(tex, cx - radius / 2, cy - radius, cx - radius / 2, cy + radius, new Color(0.4f, 0.2f, 0.1f), 3);
            // Flag triangle
            for (int y = cy - radius; y <= cy; y++)
            {
                int width = (int)((y - (cy - radius)) * 0.8f);
                for (int x = cx - radius / 2; x <= cx - radius / 2 + width; x++)
                {
                    if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                    {
                        tex.SetPixel(x, y, color);
                    }
                }
            }
        }
        
        private void DrawTrophy(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            // Cup body
            int cupTop = cy - radius / 2;
            int cupBot = cy + radius / 3;
            for (int y = cupTop; y <= cupBot; y++)
            {
                float t = (y - cupTop) / (float)(cupBot - cupTop);
                int width = (int)(radius * (1 - t * 0.3f));
                for (int x = cx - width; x <= cx + width; x++)
                {
                    if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                    {
                        tex.SetPixel(x, y, color);
                    }
                }
            }
            // Base
            DrawSquare(tex, cx - radius / 2, cy + radius / 2, radius, color);
            // Handles
            DrawCircle(tex, cx - radius, cy - radius / 4, radius / 4, color);
            DrawCircle(tex, cx + radius, cy - radius / 4, radius / 4, color);
        }
        
        private void DrawCrown(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            // Base
            DrawSquare(tex, cx - radius, cy, radius * 2, radius / 2, color);
            // Points
            DrawTriangle(tex, cx - radius / 2, cy - radius / 2, radius / 3, color);
            DrawTriangle(tex, cx, cy - radius / 2, radius / 3, color);
            DrawTriangle(tex, cx + radius / 2, cy - radius / 2, radius / 3, color);
        }
        
        private void DrawHeart(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    float dx = (x - cx) / (float)radius;
                    float dy = (y - cy) / (float)radius;
                    
                    // Heart equation
                    float heartVal = dx * dx + (dy - 0.3f * Mathf.Sqrt(Mathf.Abs(dx))) * (dy - 0.3f * Mathf.Sqrt(Mathf.Abs(dx)));
                    
                    if (heartVal <= 0.5f)
                    {
                        if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                        {
                            tex.SetPixel(x, y, color);
                        }
                    }
                }
            }
        }
        
        private void DrawArrow(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            // Shaft
            DrawLine(tex, cx, cy + radius, cx, cy - radius / 2, color, 3);
            // Head
            DrawLine(tex, cx, cy - radius, cx - radius / 2, cy - radius / 2, color, 3);
            DrawLine(tex, cx, cy - radius, cx + radius / 2, cy - radius / 2, color, 3);
        }
        
        private void DrawGear(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            // Outer circle with teeth
            for (int angle = 0; angle < 360; angle += 5)
            {
                float rad = angle * Mathf.Deg2Rad;
                int toothRadius = (angle / 45) % 2 == 0 ? radius : radius - 4;
                int x = (int)(cx + toothRadius * Mathf.Cos(rad));
                int y = (int)(cy + toothRadius * Mathf.Sin(rad));
                DrawCircle(tex, x, y, 2, color);
            }
            // Inner circle
            DrawCircle(tex, cx, cy, radius / 3, new Color(0.2f, 0.2f, 0.2f));
        }
        
        private void DrawTriangle(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                float t = (y - (cy - radius)) / (float)(radius * 2);
                int width = (int)(t * radius);
                for (int x = cx - width; x <= cx + width; x++)
                {
                    if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                    {
                        tex.SetPixel(x, y, color);
                    }
                }
            }
        }
        
        private void DrawCheckmark(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            DrawLine(tex, cx - radius / 2, cy, cx, cy + radius / 2, color, 4);
            DrawLine(tex, cx, cy + radius / 2, cx + radius, cy - radius, color, 4);
        }
        
        private void DrawLightning(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            // Lightning bolt shape
            DrawLine(tex, cx + radius / 3, cy - radius, cx - radius / 4, cy, color, 4);
            DrawLine(tex, cx - radius / 4, cy, cx + radius / 4, cy, color, 4);
            DrawLine(tex, cx + radius / 4, cy, cx - radius / 3, cy + radius, color, 4);
        }
        
        private void DrawEye(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            // Eye shape (oval)
            for (int y = cy - radius / 2; y <= cy + radius / 2; y++)
            {
                float t = Mathf.Abs(y - cy) / (float)(radius / 2);
                int width = (int)(radius * Mathf.Sqrt(1 - t * t));
                for (int x = cx - width; x <= cx + width; x++)
                {
                    if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                }
            }
            // Pupil
            DrawCircle(tex, cx, cy, radius / 3, color);
            DrawCircle(tex, cx, cy, radius / 6, Color.black);
        }
        
        private void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color color, int thickness = 1)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            
            while (true)
            {
                for (int tx = -thickness / 2; tx <= thickness / 2; tx++)
                {
                    for (int ty = -thickness / 2; ty <= thickness / 2; ty++)
                    {
                        int px = x0 + tx;
                        int py = y0 + ty;
                        if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                        {
                            tex.SetPixel(px, py, color);
                        }
                    }
                }
                
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }
        
        private void DrawSquare(Texture2D tex, int x, int y, int width, int height, Color color)
        {
            for (int py = y; py < y + height && py < tex.height; py++)
            {
                for (int px = x; px < x + width && px < tex.width; px++)
                {
                    if (px >= 0 && py >= 0)
                    {
                        tex.SetPixel(px, py, color);
                    }
                }
            }
        }
        
        private void CreateTMPSpriteAsset()
        {
            string spritePath = "Assets/Resources/UI/GameIconsSpriteSheet.png";
            
            if (!File.Exists(spritePath))
            {
                EditorUtility.DisplayDialog("Error", 
                    "Sprite sheet not found. Click 'Generate Sprite Sheet' first.", "OK");
                return;
            }
            
            // Load the sprite sheet
            Texture2D spriteSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
            if (spriteSheet == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not load sprite sheet.", "OK");
                return;
            }
            
            // Create TMP Sprite Asset
            string assetPath = "Assets/Resources/UI/GameIconsSpriteAsset.asset";
            TMP_SpriteAsset spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(assetPath);
            
            if (spriteAsset == null)
            {
                spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                AssetDatabase.CreateAsset(spriteAsset, assetPath);
            }
            
            // Configure sprite asset
            spriteAsset.spriteSheet = spriteSheet;
            spriteAsset.name = "GameIconsSpriteAsset";
            
            // Get sprites from the texture
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
            List<TMP_SpriteGlyph> glyphs = new List<TMP_SpriteGlyph>();
            List<TMP_SpriteCharacter> characters = new List<TMP_SpriteCharacter>();
            
            uint glyphIndex = 0;
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    // Create glyph
                    TMP_SpriteGlyph glyph = new TMP_SpriteGlyph();
                    glyph.index = glyphIndex;
                    glyph.sprite = sprite;
                    glyph.metrics = new UnityEngine.TextCore.GlyphMetrics(
                        sprite.rect.width,
                        sprite.rect.height,
                        0,
                        sprite.rect.height * 0.8f,
                        sprite.rect.width
                    );
                    glyph.glyphRect = new UnityEngine.TextCore.GlyphRect(
                        (int)sprite.rect.x,
                        (int)sprite.rect.y,
                        (int)sprite.rect.width,
                        (int)sprite.rect.height
                    );
                    glyphs.Add(glyph);
                    
                    // Create character
                    TMP_SpriteCharacter character = new TMP_SpriteCharacter();
                    character.name = sprite.name;
                    character.glyphIndex = glyphIndex;
                    character.scale = 1f;
                    characters.Add(character);
                    
                    glyphIndex++;
                }
            }
            
            // Add to sprite asset tables (clear existing and add new items)
            spriteAsset.spriteGlyphTable.Clear();
            foreach (var glyph in glyphs)
            {
                spriteAsset.spriteGlyphTable.Add(glyph);
            }
            
            spriteAsset.spriteCharacterTable.Clear();
            foreach (var character in characters)
            {
                spriteAsset.spriteCharacterTable.Add(character);
            }
            
            // Update hash table
            spriteAsset.UpdateLookupTables();
            
            EditorUtility.SetDirty(spriteAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[GameIcons] TMP Sprite Asset created at {assetPath} with {glyphIndex} sprites");
            EditorUtility.DisplayDialog("Success", 
                $"TMP Sprite Asset created with {glyphIndex} sprites!\n\n" +
                "The icons are now ready to use. GameIcons.cs will automatically load them.", "OK");
            
            // Ping the asset
            Selection.activeObject = spriteAsset;
            EditorGUIUtility.PingObject(spriteAsset);
        }
    }
}
#endif
