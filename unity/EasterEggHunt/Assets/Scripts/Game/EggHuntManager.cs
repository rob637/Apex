using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace EasterEggHunt.Game
{
    /// <summary>
    /// Main game manager for the Easter Egg Hunt game.
    /// Players hide and find virtual Easter eggs in AR.
    /// </summary>
    public class EggHuntManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button _hideEggButton;
        [SerializeField] private Button _findEggsButton;
        [SerializeField] private Button _clearEggsButton;
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _scoreText;

        [Header("Prefabs")]
        [SerializeField] private GameObject[] _eggPrefabs; // Different colored eggs

        [Header("Game Settings")]
        [SerializeField] private float _searchRadius = 100f; // meters
        [SerializeField] private int _pointsPerEgg = 10;

        private int _eggsFound = 0;
        private int _eggsHidden = 0;
        private bool _isHidingMode = false;

        private void Start()
        {
            if (_hideEggButton != null)
                _hideEggButton.onClick.AddListener(OnHideEggClicked);
            
            if (_findEggsButton != null)
                _findEggsButton.onClick.AddListener(OnFindEggsClicked);
            
            if (_clearEggsButton != null)
                _clearEggsButton.onClick.AddListener(OnClearEggsClicked);

            UpdateStatus("Ready! Hide eggs for others to find, or search for hidden eggs!");
            UpdateScore();
        }

        private void Update()
        {
            if (_isHidingMode && Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    TryHideEgg(touch.position);
                }
            }
        }

        private void OnHideEggClicked()
        {
            _isHidingMode = true;
            UpdateStatus("Tap on a surface to hide an egg...");
        }

        private void OnFindEggsClicked()
        {
            _isHidingMode = false;
            UpdateStatus("Searching for nearby eggs...");
            LoadNearbyEggs();
        }

        private void OnClearEggsClicked()
        {
            // Clear locally placed eggs
            UpdateStatus("Eggs cleared!");
        }

        private void TryHideEgg(Vector2 screenPosition)
        {
            _isHidingMode = false;
            // TODO: Implement AR raycast and egg placement
            UpdateStatus("Egg hidden! Others can now find it.");
            _eggsHidden++;
        }

        private async void LoadNearbyEggs()
        {
            // TODO: Load eggs from cloud within search radius
            UpdateStatus("Found eggs nearby! Tap them to collect!");
        }

        public void OnEggCollected(GameObject egg)
        {
            _eggsFound++;
            UpdateScore();
            UpdateStatus($"You found an egg! +{_pointsPerEgg} points!");
            Destroy(egg);
        }

        private void UpdateStatus(string message)
        {
            if (_statusText != null)
                _statusText.text = message;
            Debug.Log($"[EasterEggHunt] {message}");
        }

        private void UpdateScore()
        {
            if (_scoreText != null)
                _scoreText.text = $"Eggs Found: {_eggsFound} | Points: {_eggsFound * _pointsPerEgg}";
        }
    }
}
