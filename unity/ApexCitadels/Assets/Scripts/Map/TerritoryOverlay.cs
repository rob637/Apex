using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ApexCitadels.Territory;

namespace ApexCitadels.Map
{
    /// <summary>
    /// Renders territory boundaries and control zones on the map
    /// </summary>
    public class TerritoryOverlay : MonoBehaviour
    {
        public static TerritoryOverlay Instance { get; private set; }

        [Header("Overlay Settings")]
        [SerializeField] private float overlayOpacity = 0.4f;
        [SerializeField] private float borderWidth = 3f;
        [SerializeField] private bool showTerritoryLabels = true;
        [SerializeField] private bool showContestIndicator = true;

        [Header("Colors")]
        [SerializeField] private Color ownedColor = new Color(0.2f, 0.8f, 0.2f, 0.4f);
        [SerializeField] private Color allianceColor = new Color(0.2f, 0.6f, 0.9f, 0.4f);
        [SerializeField] private Color enemyColor = new Color(0.8f, 0.2f, 0.2f, 0.4f);
        [SerializeField] private Color neutralColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);
        [SerializeField] private Color contestedColor = new Color(1f, 0.6f, 0f, 0.5f);

        [Header("Prefabs")]
        [SerializeField] private GameObject territoryZonePrefab;
        [SerializeField] private GameObject territoryLabelPrefab;
        [SerializeField] private GameObject contestIndicatorPrefab;

        [Header("References")]
        [SerializeField] private RectTransform overlayContainer;

        // Territory overlays
        private Dictionary<string, TerritoryZoneUI> _territoryZones = new Dictionary<string, TerritoryZoneUI>();

        // Cache
        private string _currentPlayerId;
        private string _currentAllianceId;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (MapRenderer.Instance != null)
            {
                MapRenderer.Instance.OnMapCenterChanged += OnMapCenterChanged;
                MapRenderer.Instance.OnZoomChanged += OnZoomChanged;
            }

            if (TerritoryManager.Instance != null)
            {
                TerritoryManager.Instance.OnTerritoryUpdated += OnTerritoryUpdated;
            }
        }

        private void OnDestroy()
        {
            if (MapRenderer.Instance != null)
            {
                MapRenderer.Instance.OnMapCenterChanged -= OnMapCenterChanged;
                MapRenderer.Instance.OnZoomChanged -= OnZoomChanged;
            }

            if (TerritoryManager.Instance != null)
            {
                TerritoryManager.Instance.OnTerritoryUpdated -= OnTerritoryUpdated;
            }
        }

        #region Territory Management

        /// <summary>
        /// Set the current player/alliance for ownership coloring
        /// </summary>
        public void SetCurrentPlayer(string playerId, string allianceId)
        {
            _currentPlayerId = playerId;
            _currentAllianceId = allianceId;
            RefreshAllTerritories();
        }

        /// <summary>
        /// Add or update a territory on the overlay
        /// </summary>
        public void SetTerritory(TerritoryData territory)
        {
            if (!_territoryZones.TryGetValue(territory.Id, out TerritoryZoneUI zoneUI))
            {
                zoneUI = CreateTerritoryZone(territory);
                _territoryZones[territory.Id] = zoneUI;
            }

            UpdateTerritoryZone(zoneUI, territory);
        }

        /// <summary>
        /// Remove a territory from the overlay
        /// </summary>
        public void RemoveTerritory(string territoryId)
        {
            if (_territoryZones.TryGetValue(territoryId, out TerritoryZoneUI zoneUI))
            {
                Destroy(zoneUI.gameObject);
                _territoryZones.Remove(territoryId);
            }
        }

        /// <summary>
        /// Clear all territories from the overlay
        /// </summary>
        public void ClearTerritories()
        {
            foreach (var zone in _territoryZones.Values)
            {
                Destroy(zone.gameObject);
            }
            _territoryZones.Clear();
        }

        /// <summary>
        /// Load territories for the visible area
        /// </summary>
        public async void LoadTerritoriesForArea(double lat, double lon, double radiusMeters)
        {
            if (TerritoryManager.Instance == null) return;

            var territories = await TerritoryManager.Instance.GetNearbyTerritories(lat, lon, (float)radiusMeters);

            // Remove territories no longer in view
            List<string> toRemove = new List<string>();
            foreach (var id in _territoryZones.Keys)
            {
                if (!territories.Exists(t => t.Id == id))
                {
                    toRemove.Add(id);
                }
            }
            foreach (var id in toRemove)
            {
                RemoveTerritory(id);
            }

            // Add/update visible territories
            foreach (var territory in territories)
            {
                SetTerritory(new TerritoryData(territory));
            }
        }

        #endregion

        #region Zone Creation/Update

        private TerritoryZoneUI CreateTerritoryZone(TerritoryData territory)
        {
            GameObject zoneObj;

            if (territoryZonePrefab != null)
            {
                zoneObj = Instantiate(territoryZonePrefab, overlayContainer);
            }
            else
            {
                zoneObj = CreateDefaultZone();
            }

            zoneObj.name = $"Territory_{territory.Id}";

            TerritoryZoneUI zoneUI = zoneObj.GetComponent<TerritoryZoneUI>();
            if (zoneUI == null)
            {
                zoneUI = zoneObj.AddComponent<TerritoryZoneUI>();
            }

            return zoneUI;
        }

        private GameObject CreateDefaultZone()
        {
            GameObject zoneObj = new GameObject("TerritoryZone");
            zoneObj.transform.SetParent(overlayContainer, false);

            // Background fill
            Image fillImage = zoneObj.AddComponent<Image>();
            fillImage.raycastTarget = false;

            // Create border
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(zoneObj.transform, false);
            
            Outline outline = borderObj.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(borderWidth, borderWidth);

            // Create label
            if (showTerritoryLabels)
            {
                GameObject labelObj = new GameObject("Label");
                labelObj.transform.SetParent(zoneObj.transform, false);
                
                var tmp = labelObj.AddComponent<TMPro.TextMeshProUGUI>();
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
                tmp.fontSize = 12;
                tmp.raycastTarget = false;

                RectTransform labelRect = labelObj.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }

            return zoneObj;
        }

        private void UpdateTerritoryZone(TerritoryZoneUI zoneUI, TerritoryData territory)
        {
            // Position and size
            if (MapRenderer.Instance != null)
            {
                Vector2 screenPos = MapRenderer.Instance.GeoToScreen(territory.Latitude, territory.Longitude);
                float radiusPixels = (float)(territory.RadiusMeters / MapRenderer.Instance.GetMetersPerPixel());

                RectTransform rt = zoneUI.GetComponent<RectTransform>();
                rt.anchoredPosition = screenPos;
                rt.sizeDelta = new Vector2(radiusPixels * 2, radiusPixels * 2);
            }

            // Color based on ownership
            Color zoneColor = GetTerritoryColor(territory);
            zoneUI.SetColor(zoneColor);

            // Label
            if (showTerritoryLabels)
            {
                zoneUI.SetLabel(territory.Name);
            }

            // Contest indicator
            if (showContestIndicator && territory.IsContested)
            {
                zoneUI.ShowContestIndicator(true);
            }
            else
            {
                zoneUI.ShowContestIndicator(false);
            }

            // Store territory data
            zoneUI.TerritoryId = territory.Id;
            zoneUI.Territory = territory;
        }

        private Color GetTerritoryColor(TerritoryData territory)
        {
            if (territory.IsContested)
            {
                return contestedColor;
            }

            if (string.IsNullOrEmpty(territory.OwnerId))
            {
                return neutralColor;
            }

            if (territory.OwnerId == _currentPlayerId)
            {
                return ownedColor;
            }

            if (!string.IsNullOrEmpty(_currentAllianceId) && territory.AllianceId == _currentAllianceId)
            {
                return allianceColor;
            }

            return enemyColor;
        }

        #endregion

        #region Event Handlers

        private void OnMapCenterChanged(double lat, double lon)
        {
            RefreshAllTerritories();

            // Load more territories if needed
            if (MapRenderer.Instance != null)
            {
                double visibleRadius = MapRenderer.Instance.GetMetersPerPixel() * 1000; // Rough estimate
                LoadTerritoriesForArea(lat, lon, visibleRadius);
            }
        }

        private void OnZoomChanged(int zoom)
        {
            RefreshAllTerritories();
        }

        private void OnTerritoryUpdated(string territoryId)
        {
            // Refresh specific territory
            if (_territoryZones.TryGetValue(territoryId, out TerritoryZoneUI zoneUI))
            {
                if (TerritoryManager.Instance != null)
                {
                    var territory = TerritoryManager.Instance.GetTerritory(territoryId);
                    if (territory != null)
                    {
                        UpdateTerritoryZone(zoneUI, new TerritoryData(territory));
                    }
                }
            }
        }

        private void RefreshAllTerritories()
        {
            foreach (var kvp in _territoryZones)
            {
                if (kvp.Value.Territory != null)
                {
                    UpdateTerritoryZone(kvp.Value, kvp.Value.Territory);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// UI component for a single territory zone
    /// </summary>
    public class TerritoryZoneUI : MonoBehaviour
    {
        public string TerritoryId { get; set; }
        public TerritoryData Territory { get; set; }

        private Image _fillImage;
        private TMPro.TextMeshProUGUI _label;
        private GameObject _contestIndicator;

        private void Awake()
        {
            _fillImage = GetComponent<Image>();
            _label = GetComponentInChildren<TMPro.TextMeshProUGUI>();
        }

        public void SetColor(Color color)
        {
            if (_fillImage != null)
            {
                _fillImage.color = color;
            }
        }

        public void SetLabel(string text)
        {
            if (_label != null)
            {
                _label.text = text;
            }
        }

        public void ShowContestIndicator(bool show)
        {
            if (_contestIndicator != null)
            {
                _contestIndicator.SetActive(show);
            }
        }

        public void OnClick()
        {
            // Trigger territory selection
            if (MapViewController.Instance != null)
            {
                var marker = new MapMarker(TerritoryId, Territory?.Name ?? "", MapMarkerType.NeutralTerritory, 
                    Territory?.Latitude ?? 0, Territory?.Longitude ?? 0);
                // MapViewController.Instance.SelectMarker(TerritoryId);
            }
        }
    }

    /// <summary>
    /// Extended territory data for map display
    /// </summary>
    [Serializable]
    public class TerritoryData
    {
        public string Id;
        public string Name;
        public string OwnerId;
        public string OwnerName;
        public string AllianceId;
        public double Latitude;
        public double Longitude;
        public float RadiusMeters;
        public int Level;
        public bool IsContested;
        public float ContestProgress;
        public DateTime LastUpdated;

        public TerritoryData() { }

        public TerritoryData(Territory.Territory territory)
        {
            Id = territory.Id;
            Name = territory.Name;
            OwnerId = territory.OwnerId;
            Latitude = territory.Latitude;
            Longitude = territory.Longitude;
            RadiusMeters = territory.Radius;
        }
    }
}
