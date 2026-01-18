using System;
using UnityEngine;

namespace ApexCitadels.PC
{
    /// <summary>
    /// Controls the day/night cycle, syncing with real-time or running at a scaled speed.
    /// </summary>
    public class DayNightCycle : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool syncWithRealTime = true;
        [SerializeField] private float timeScale = 100f; // If not syncing, degrees per second
        [SerializeField] private float updateInterval = 60f; // Update sun position every minute if real time

        private Light _sun;
        private float _lastUpdate;

        private void Start()
        {
            _sun = GetComponent<Light>();
            UpdateSunPosition();
        }

        private void Update()
        {
            if (syncWithRealTime)
            {
                if (Time.time - _lastUpdate > updateInterval)
                {
                    UpdateSunPosition();
                    _lastUpdate = Time.time;
                }
            }
            else
            {
                // Simple rotation for game-time
                float angle = Time.deltaTime * timeScale;
                transform.Rotate(Vector3.right, angle);
            }
        }

        private void UpdateSunPosition()
        {
            DateTime now = DateTime.Now;
            // Map 0..24 hours to sun rotation
            // Noon (12:00) should be highest point (~90 degrees)
            // Midnight (00:00) should be lowest point (~-90 or 270 degrees)
            
            float rawHour = (float)now.TimeOfDay.TotalHours;
            
            // Angle mapping:
            // 6:00 AM -> 0 degrees (Sunrise)
            // 12:00 PM -> 90 degrees (Noon)
            // 6:00 PM -> 180 degrees (Sunset)
            // 12:00 AM -> 270 degrees (Midnight)
            float angle = (rawHour - 6f) * 15f; 

            // Keep Y rotation fixed (-30 is a good aesthetic angle)
            transform.rotation = Quaternion.Euler(angle, -30f, 0f);
            
            // Adjust intensity based on angle (day vs night)
            if (_sun != null)
            {
                // Simple intensity curve
                // Day (0 to 180) -> Intensity > 0
                // Night (180 to 360) -> Intensity 0 or low
                // angle normalized to 0-360
                float normAngle = angle % 360f;
                if (normAngle < 0) normAngle += 360f;
                
                if (normAngle > 0 && normAngle < 180)
                {
                    _sun.intensity = Mathf.Sin(normAngle * Mathf.Deg2Rad) * 1.2f;
                }
                else
                {
                    _sun.intensity = 0f;
                }
            }
        }
    }
}
