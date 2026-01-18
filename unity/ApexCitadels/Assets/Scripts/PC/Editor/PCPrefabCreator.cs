using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ApexCitadels.PC
{
    /// <summary>
    /// Utility class to create PC prefabs and scene setup
    /// Run from Unity Editor via menu: Apex/PC/Create PC Prefabs
    /// </summary>
    public static class PCPrefabCreator
    {
#if UNITY_EDITOR
        private const string PREFAB_PATH = "Assets/Prefabs/PC";
        private const string MATERIAL_PATH = "Assets/Materials/PC";
        
        [MenuItem("Apex/PC/Create All PC Prefabs")]
        public static void CreateAllPrefabs()
        {
            // Ensure directories exist
            EnsureDirectory(PREFAB_PATH);
            EnsureDirectory(MATERIAL_PATH);
            
            // Create materials first
            CreatePCMaterials();
            
            // Create prefabs
            CreateTerritoryMarkerPrefab();
            CreateBuildingPreviewPrefab();
            CreateUIButtonPrefab();
            CreateListItemPrefab();
            CreateTooltipPrefab();
            CreateMinimapMarkerPrefab();
            
            // Create UI Panels
            GameObject basePanel = CreatePanelPrefab();
            CreateSpecificPanel(basePanel, "MainMenuPanel", "Main Menu", new Color(0.1f, 0.1f, 0.15f, 0.95f));
            CreateSpecificPanel(basePanel, "TerritoryDetailPanel", "Territory Details", new Color(0.1f, 0.1f, 0.1f, 0.95f));
            CreateSpecificPanel(basePanel, "AlliancePanel", "Alliance", new Color(0.1f, 0.05f, 0.15f, 0.95f));
            CreateSpecificPanel(basePanel, "BuildMenuPanel", "Build Menu", new Color(0.15f, 0.1f, 0.05f, 0.95f));
            CreateSpecificPanel(basePanel, "InventoryPanel", "Inventory", new Color(0.1f, 0.1f, 0.05f, 0.95f));
            CreateSpecificPanel(basePanel, "StatisticsPanel", "Statistics", new Color(0.05f, 0.1f, 0.1f, 0.95f));
            CreateSpecificPanel(basePanel, "SettingsPanel", "Settings", new Color(0.1f, 0.1f, 0.1f, 0.95f));
            CreateSpecificPanel(basePanel, "ChatPanel", "Chat", new Color(0.05f, 0.05f, 0.1f, 0.9f));
            
            // Cleanup temp
            Object.DestroyImmediate(basePanel);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("[PCPrefabCreator] All PC prefabs created successfully!");
        }

        private static void CreateSpecificPanel(GameObject basePanelPrefab, string name, string title, Color color)
        {
            // Instantiate base (regular Instantiate because basePanelPrefab is a scene object, not a disk asset)
            GameObject instance = Object.Instantiate(basePanelPrefab);
            instance.name = name;
            
            // Customize
            var bg = instance.GetComponent<Image>();
            if (bg) bg.color = color;
            
            // Find title (Path: Header -> Title)
            var titleObj = instance.transform.Find("Header/Title");
            if (titleObj)
            {
                var tmp = titleObj.GetComponent<TextMeshProUGUI>();
                if (tmp) tmp.text = title;
            }
            
            // Add specific scripts based on name
            if (name == "TerritoryDetailPanel") instance.AddComponent<ApexCitadels.PC.UI.TerritoryDetailPanel>();
            if (name == "BuildMenuPanel") instance.AddComponent<ApexCitadels.PC.UI.BuildMenuPanel>();
            if (name == "AlliancePanel") instance.AddComponent<ApexCitadels.PC.UI.AlliancePanel>();
            
            // Add CanvasGroup if missing (useful for fading)
            if (instance.GetComponent<CanvasGroup>() == null)
                instance.AddComponent<CanvasGroup>();

            // Save as new prefab
            string path = $"{PREFAB_PATH}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
        }
        
        [MenuItem("Apex/PC/Create PC Materials")]
        public static void CreatePCMaterials()
        {
            EnsureDirectory(MATERIAL_PATH);
            
            // Territory marker materials
            CreateMaterial("TerritoryOwned", new Color(0.2f, 0.8f, 0.2f, 0.6f));
            CreateMaterial("TerritoryEnemy", new Color(0.8f, 0.2f, 0.2f, 0.6f));
            CreateMaterial("TerritoryNeutral", new Color(0.5f, 0.5f, 0.5f, 0.6f));
            CreateMaterial("TerritoryAllied", new Color(0.2f, 0.5f, 0.8f, 0.6f));
            CreateMaterial("TerritoryContested", new Color(0.8f, 0.8f, 0.2f, 0.6f));
            
            // Building materials
            CreateMaterial("BuildingPreviewValid", new Color(0f, 1f, 0f, 0.5f));
            CreateMaterial("BuildingPreviewInvalid", new Color(1f, 0f, 0f, 0.5f));
            CreateMaterial("BuildingGhost", new Color(0.5f, 0.5f, 1f, 0.3f));
            
            // Grid materials
            CreateMaterial("GridLine", new Color(1f, 1f, 1f, 0.2f));
            CreateMaterial("GridHighlight", new Color(0f, 0.8f, 1f, 0.4f));
            
            // Effect materials
            CreateMaterial("SelectionOutline", new Color(1f, 0.8f, 0f, 1f));
            CreateMaterial("AttackLine", new Color(1f, 0f, 0f, 0.8f));
            CreateMaterial("DefenseLine", new Color(0f, 0.5f, 1f, 0.8f));
            
            Debug.Log("[PCPrefabCreator] PC materials created");
        }
        
        private static void CreateMaterial(string name, Color color)
        {
            string path = $"{MATERIAL_PATH}/{name}.mat";
            
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            {
                return; // Already exists
            }
            
            var shader = Shader.Find("Standard");
            var material = new Material(shader);
            material.name = name;
            material.color = color;
            
            // For transparent materials
            if (color.a < 1f)
            {
                material.SetFloat("_Mode", 3); // Transparent
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }
            
            AssetDatabase.CreateAsset(material, path);
        }
        
        [MenuItem("Apex/PC/Create Territory Marker Prefab")]
        public static void CreateTerritoryMarkerPrefab()
        {
            EnsureDirectory(PREFAB_PATH);
            
            var marker = new GameObject("TerritoryMarker");
            
            // Base visual (hexagon approximated by cylinder)
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "Base";
            baseObj.transform.SetParent(marker.transform);
            baseObj.transform.localPosition = Vector3.zero;
            baseObj.transform.localScale = new Vector3(10f, 0.1f, 10f);
            
            // Remove collider from base
            Object.DestroyImmediate(baseObj.GetComponent<Collider>());
            
            // Flag pole
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "FlagPole";
            pole.transform.SetParent(marker.transform);
            pole.transform.localPosition = new Vector3(0, 3f, 0);
            pole.transform.localScale = new Vector3(0.2f, 3f, 0.2f);
            Object.DestroyImmediate(pole.GetComponent<Collider>());
            
            // Flag
            var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "Flag";
            flag.transform.SetParent(marker.transform);
            flag.transform.localPosition = new Vector3(1f, 5.5f, 0);
            flag.transform.localScale = new Vector3(2f, 1f, 0.1f);
            Object.DestroyImmediate(flag.GetComponent<Collider>());
            
            // Border indicator
            var border = new GameObject("Border");
            border.transform.SetParent(marker.transform);
            var lineRenderer = border.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 7; // Hexagon + close
            lineRenderer.startWidth = 0.2f;
            lineRenderer.endWidth = 0.2f;
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            
            // Set hexagon points
            float radius = 5f;
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                Vector3 point = new Vector3(Mathf.Cos(angle) * radius, 0.2f, Mathf.Sin(angle) * radius);
                lineRenderer.SetPosition(i, point);
            }
            lineRenderer.SetPosition(6, lineRenderer.GetPosition(0));
            
            // Territory label (using TextMeshPro if available)
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(marker.transform);
            labelObj.transform.localPosition = new Vector3(0, 1f, 0);
            
            // Add marker script placeholder
            // marker.AddComponent<TerritoryMarkerVisual>();
            
            SavePrefab(marker, "TerritoryMarker");
            Object.DestroyImmediate(marker);
        }
        
        [MenuItem("Apex/PC/Create Building Preview Prefab")]
        public static void CreateBuildingPreviewPrefab()
        {
            EnsureDirectory(PREFAB_PATH);
            
            var preview = new GameObject("BuildingPreview");
            
            // Visual placeholder
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(preview.transform);
            visual.transform.localPosition = Vector3.zero;
            
            // Remove collider
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            
            // Grid snap indicator
            var grid = new GameObject("GridIndicator");
            grid.transform.SetParent(preview.transform);
            var gridLine = grid.AddComponent<LineRenderer>();
            gridLine.positionCount = 5;
            gridLine.startWidth = 0.05f;
            gridLine.endWidth = 0.05f;
            gridLine.useWorldSpace = false;
            gridLine.loop = true;
            
            // Square indicator
            float size = 0.5f;
            gridLine.SetPosition(0, new Vector3(-size, 0, -size));
            gridLine.SetPosition(1, new Vector3(size, 0, -size));
            gridLine.SetPosition(2, new Vector3(size, 0, size));
            gridLine.SetPosition(3, new Vector3(-size, 0, size));
            gridLine.SetPosition(4, new Vector3(-size, 0, -size));
            
            SavePrefab(preview, "BuildingPreview");
            Object.DestroyImmediate(preview);
        }
        
        [MenuItem("Apex/PC/Create UI Prefabs")]
        public static void CreateUIPrefabs()
        {
            CreateUIButtonPrefab();
            CreatePanelPrefab();
            CreateListItemPrefab();
            CreateTooltipPrefab();
        }
        
        private static void CreateUIButtonPrefab()
        {
            var button = new GameObject("PCButton");
            button.AddComponent<RectTransform>().sizeDelta = new Vector2(200, 50);
            
            // Background
            var bg = button.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.3f, 0.4f, 0.9f);
            
            // Button component
            var btn = button.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.2f, 0.3f, 0.4f, 0.9f);
            colors.highlightedColor = new Color(0.3f, 0.4f, 0.5f, 1f);
            colors.pressedColor = new Color(0.15f, 0.25f, 0.35f, 1f);
            btn.colors = colors;
            
            // Text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(button.transform);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Button";
            text.fontSize = 18;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            
            SavePrefab(button, "UI/PCButton");
            Object.DestroyImmediate(button);
        }
        
        private static GameObject CreatePanelPrefab()
        {
            var panel = new GameObject("PCPanel");
            var rect = panel.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 600);
            
            // Background
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            
            // Header
            var header = new GameObject("Header");
            header.transform.SetParent(panel.transform);
            var headerRect = header.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.sizeDelta = new Vector2(0, 50);
            headerRect.anchoredPosition = Vector2.zero;
            
            var headerBg = header.AddComponent<Image>();
            headerBg.color = new Color(0.15f, 0.2f, 0.25f, 1f);
            
            // Title
            var title = new GameObject("Title");
            title.transform.SetParent(header.transform);
            var titleRect = title.AddComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(15, 0);
            titleRect.offsetMax = new Vector2(-50, 0);
            
            var titleText = title.AddComponent<TextMeshProUGUI>();
            titleText.text = "Panel Title";
            titleText.fontSize = 22;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Left;
            titleText.color = Color.white;
            
            // Close button
            var closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(header.transform);
            var closeRect = closeBtn.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 0.5f);
            closeRect.anchorMax = new Vector2(1, 0.5f);
            closeRect.pivot = new Vector2(1, 0.5f);
            closeRect.sizeDelta = new Vector2(40, 40);
            closeRect.anchoredPosition = new Vector2(-5, 0);
            
            var closeBg = closeBtn.AddComponent<Image>();
            closeBg.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);
            closeBtn.AddComponent<Button>();
            
            var closeText = new GameObject("X");
            closeText.transform.SetParent(closeBtn.transform);
            var closeTextRect = closeText.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;
            
            var closeTMP = closeText.AddComponent<TextMeshProUGUI>();
            closeTMP.text = "×";
            closeTMP.fontSize = 28;
            closeTMP.alignment = TextAlignmentOptions.Center;
            closeTMP.color = Color.white;
            
            // Content area
            var content = new GameObject("Content");
            content.transform.SetParent(panel.transform);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(10, 10);
            contentRect.offsetMax = new Vector2(-10, -60);
            
            return panel;
        }
        
        private static void CreateListItemPrefab()
        {
            var item = new GameObject("PCListItem");
            var rect = item.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(380, 60);
            
            // Background
            var bg = item.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.25f, 0.3f, 0.8f);
            
            // Button
            var btn = item.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.2f, 0.25f, 0.3f, 0.8f);
            colors.highlightedColor = new Color(0.25f, 0.3f, 0.35f, 0.9f);
            colors.selectedColor = new Color(0.3f, 0.4f, 0.5f, 1f);
            btn.colors = colors;
            
            // Icon placeholder
            var icon = new GameObject("Icon");
            icon.transform.SetParent(item.transform);
            var iconRect = icon.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.pivot = new Vector2(0, 0.5f);
            iconRect.sizeDelta = new Vector2(50, 50);
            iconRect.anchoredPosition = new Vector2(5, 0);
            icon.AddComponent<Image>().color = Color.gray;
            
            // Title
            var title = new GameObject("Title");
            title.transform.SetParent(item.transform);
            var titleRect = title.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.5f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(60, 5);
            titleRect.offsetMax = new Vector2(-10, -5);
            
            var titleText = title.AddComponent<TextMeshProUGUI>();
            titleText.text = "Item Title";
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Left;
            titleText.color = Color.white;
            
            // Subtitle
            var subtitle = new GameObject("Subtitle");
            subtitle.transform.SetParent(item.transform);
            var subRect = subtitle.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0, 0);
            subRect.anchorMax = new Vector2(1, 0.5f);
            subRect.offsetMin = new Vector2(60, 5);
            subRect.offsetMax = new Vector2(-10, -5);
            
            var subText = subtitle.AddComponent<TextMeshProUGUI>();
            subText.text = "Subtitle text";
            subText.fontSize = 12;
            subText.color = new Color(0.7f, 0.7f, 0.7f);
            subText.alignment = TextAlignmentOptions.Left;
            
            SavePrefab(item, "UI/PCListItem");
            Object.DestroyImmediate(item);
        }
        
        private static void CreateTooltipPrefab()
        {
            var tooltip = new GameObject("PCTooltip");
            var rect = tooltip.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(250, 100);
            
            // Background
            var bg = tooltip.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.1f, 0.95f);
            
            // Shadow/Outline effect using Shadow component
            var shadow = tooltip.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.4f, 0.5f, 0.6f, 0.8f);
            shadow.effectDistance = new Vector2(1, 1);
            
            // Content
            var content = new GameObject("Content");
            content.transform.SetParent(tooltip.transform);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(10, 10);
            contentRect.offsetMax = new Vector2(-10, -10);
            
            var text = content.AddComponent<TextMeshProUGUI>();
            text.text = "Tooltip text";
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.TopLeft;
            
            // Layout to auto-size
            var fitter = tooltip.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            SavePrefab(tooltip, "UI/PCTooltip");
            Object.DestroyImmediate(tooltip);
        }
        
        private static void CreateMinimapMarkerPrefab()
        {
            var marker = new GameObject("MinimapMarker");
            var rect = marker.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(10, 10);
            
            var image = marker.AddComponent<Image>();
            image.color = Color.white;
            
            SavePrefab(marker, "UI/MinimapMarker");
            Object.DestroyImmediate(marker);
        }
        
        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path);
                string folder = Path.GetFileName(path);
                
                if (!AssetDatabase.IsValidFolder(parent))
                {
                    EnsureDirectory(parent);
                }
                
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
        
        private static void SavePrefab(GameObject obj, string name)
        {
            string dir = Path.GetDirectoryName($"{PREFAB_PATH}/{name}");
            EnsureDirectory(dir);
            
            string path = $"{PREFAB_PATH}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(obj, path);
            Debug.Log($"[PCPrefabCreator] Created prefab: {path}");
        }
#endif
    }
}
