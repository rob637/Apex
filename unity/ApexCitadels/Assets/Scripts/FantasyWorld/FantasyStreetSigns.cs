// ============================================================================
// FANTASY STREET SIGNS - Generate medieval-style street name signs
// ============================================================================
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace ApexCitadels.FantasyWorld
{
    /// <summary>
    /// Generates fantasy-themed street signs at road intersections
    /// </summary>
    public class FantasyStreetSigns : MonoBehaviour
    {
        [Header("Sign Settings")]
        public float signHeight = 3f;
        public float signWidth = 2.5f;
        public float poleRadius = 0.08f;
        public float fontSize = 0.4f;
        
        [Header("Colors")]
        public Color woodColor = new Color(0.4f, 0.25f, 0.1f);
        public Color metalColor = new Color(0.3f, 0.3f, 0.35f);
        public Color textColor = new Color(0.9f, 0.85f, 0.7f);
        
        [Header("Fantasy Name Generator")]
        public bool useFantasyNames = true;
        
        private static readonly string[] _prefixes = {
            "Dragon's", "King's", "Queen's", "Wizard's", "Knight's",
            "Mystic", "Golden", "Silver", "Crystal", "Shadow",
            "Moonlit", "Starfall", "Thunder", "Iron", "Cobblestone",
            "Merchant's", "Noble's", "Enchanted", "Ancient", "Sacred"
        };
        
        private static readonly string[] _suffixes = {
            "Way", "Lane", "Road", "Path", "Street",
            "Alley", "Court", "Circle", "Walk", "Row",
            "Gate", "Bridge", "Crossing", "Square", "Plaza"
        };
        
        private static readonly string[] _middleNames = {
            "Oak", "Willow", "Rose", "Thorn", "Raven",
            "Wolf", "Bear", "Fox", "Hawk", "Serpent",
            "Stone", "Fire", "Ice", "Wind", "Storm",
            "Crown", "Sword", "Shield", "Scroll", "Gem"
        };
        
        private Dictionary<string, string> _streetNameCache = new Dictionary<string, string>();
        private int _signCount = 0;
        
        /// <summary>
        /// Generate signs for all roads
        /// </summary>
        public void GenerateSigns(List<OSMRoad> roads, Transform parent)
        {
            foreach (var road in roads)
            {
                if (road.Points == null || road.Points.Count < 2) continue;
                
                // Get or generate fantasy name for this road
                string realName = !string.IsNullOrEmpty(road.Name) ? road.Name : $"Road_{road.Id}";
                string fantasyName = GetFantasyName(realName);
                
                // Place signs at start, middle, and end of road
                PlaceSign(road.Points[0], fantasyName, parent);
                
                if (road.Points.Count > 4)
                {
                    int midIndex = road.Points.Count / 2;
                    PlaceSign(road.Points[midIndex], fantasyName, parent);
                }
            }
        }
        
        private string GetFantasyName(string realName)
        {
            if (_streetNameCache.TryGetValue(realName, out string cached))
                return cached;
            
            string fantasyName;
            
            if (useFantasyNames)
            {
                // Generate a consistent fantasy name based on hash
                int hash = realName.GetHashCode();
                System.Random rng = new System.Random(hash);
                
                string prefix = _prefixes[rng.Next(_prefixes.Length)];
                string middle = _middleNames[rng.Next(_middleNames.Length)];
                string suffix = _suffixes[rng.Next(_suffixes.Length)];
                
                // Sometimes skip prefix
                if (rng.NextDouble() > 0.7f)
                    fantasyName = $"{middle} {suffix}";
                else
                    fantasyName = $"{prefix} {middle} {suffix}";
            }
            else
            {
                fantasyName = realName;
            }
            
            _streetNameCache[realName] = fantasyName;
            return fantasyName;
        }
        
        private void PlaceSign(Vector3 position, string streetName, Transform parent)
        {
            // Create sign post
            GameObject signPost = new GameObject($"StreetSign_{_signCount++}");
            signPost.transform.SetParent(parent);
            signPost.transform.position = position + Vector3.up * 0.1f;
            
            // Random rotation to face road-ish direction
            signPost.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            
            // Create pole
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(signPost.transform);
            pole.transform.localPosition = new Vector3(0, signHeight / 2, 0);
            pole.transform.localScale = new Vector3(poleRadius * 2, signHeight / 2, poleRadius * 2);
            
            // Remove collider from pole
            var poleCollider = pole.GetComponent<Collider>();
            if (poleCollider != null) Object.Destroy(poleCollider);
            
            // Pole material (wood)
            var poleRenderer = pole.GetComponent<Renderer>();
            ApplyWoodMaterial(poleRenderer);
            
            // Create sign board
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Board";
            board.transform.SetParent(signPost.transform);
            board.transform.localPosition = new Vector3(signWidth / 2 + 0.1f, signHeight - 0.3f, 0);
            board.transform.localScale = new Vector3(signWidth, 0.5f, 0.1f);
            
            // Remove collider from board
            var boardCollider = board.GetComponent<Collider>();
            if (boardCollider != null) Object.Destroy(boardCollider);
            
            // Board material (wood)
            var boardRenderer = board.GetComponent<Renderer>();
            ApplyWoodMaterial(boardRenderer);
            
            // Create text
            CreateSignText(board.transform, streetName);
            
            // Add decorative bracket
            CreateBracket(signPost.transform, signHeight - 0.3f);
        }
        
        private void ApplyWoodMaterial(Renderer renderer)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                renderer.material = new Material(shader);
                renderer.material.color = woodColor;
                renderer.material.SetFloat("_Smoothness", 0.1f);
            }
        }
        
        private void CreateSignText(Transform boardTransform, string text)
        {
            // Create TextMeshPro object
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(boardTransform);
            textObj.transform.localPosition = new Vector3(0, 0, -0.06f);
            textObj.transform.localRotation = Quaternion.identity;
            textObj.transform.localScale = Vector3.one;
            
            // Add TextMeshPro component
            var tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            
            // Size the rect transform
            var rect = tmp.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(signWidth * 0.9f, 0.4f);
            
            // Also create text on the back side
            GameObject textObjBack = new GameObject("TextBack");
            textObjBack.transform.SetParent(boardTransform);
            textObjBack.transform.localPosition = new Vector3(0, 0, 0.06f);
            textObjBack.transform.localRotation = Quaternion.Euler(0, 180, 0);
            textObjBack.transform.localScale = Vector3.one;
            
            var tmpBack = textObjBack.AddComponent<TextMeshPro>();
            tmpBack.text = text;
            tmpBack.fontSize = fontSize;
            tmpBack.color = textColor;
            tmpBack.alignment = TextAlignmentOptions.Center;
            tmpBack.fontStyle = FontStyles.Bold;
            
            var rectBack = tmpBack.GetComponent<RectTransform>();
            rectBack.sizeDelta = new Vector2(signWidth * 0.9f, 0.4f);
        }
        
        private void CreateBracket(Transform parent, float height)
        {
            // Simple decorative bracket
            GameObject bracket = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bracket.name = "Bracket";
            bracket.transform.SetParent(parent);
            bracket.transform.localPosition = new Vector3(0.15f, height, 0);
            bracket.transform.localScale = new Vector3(0.3f, 0.08f, 0.08f);
            bracket.transform.localRotation = Quaternion.Euler(0, 0, 45);
            
            var collider = bracket.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
            
            var renderer = bracket.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                renderer.material = new Material(shader);
                renderer.material.color = metalColor;
                renderer.material.SetFloat("_Metallic", 0.8f);
                renderer.material.SetFloat("_Smoothness", 0.4f);
            }
        }
    }
}
