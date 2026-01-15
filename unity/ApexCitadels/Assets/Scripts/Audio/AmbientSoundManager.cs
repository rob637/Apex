using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace ApexCitadels.Audio
{
    /// <summary>
    /// Ambient sound manager for environmental audio, spatial 3D sounds,
    /// and location-based audio landscapes.
    /// </summary>
    public class AmbientSoundManager : MonoBehaviour
    {
        public static AmbientSoundManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource globalAmbientSource;
        [SerializeField] private AudioSource secondaryAmbientSource;

        [Header("Ambient Library")]
        [SerializeField] private AmbientLibrary ambientLibrary;

        [Header("Settings")]
        [SerializeField] private float crossfadeDuration = 3f;
        [SerializeField] private float spatialUpdateInterval = 1f;
        [SerializeField] private int maxSpatialSources = 10;

        [Header("Time of Day")]
        [SerializeField] private bool useTimeOfDay = true;
        [SerializeField] private float dayStartHour = 6f;
        [SerializeField] private float nightStartHour = 20f;

        // State
        private AmbientPreset currentPreset;
        private AmbientPreset targetPreset;
        private bool isCrossfading = false;
        private Coroutine crossfadeCoroutine;
        private TimeOfDay currentTimeOfDay = TimeOfDay.Day;

        // Spatial audio
        private List<SpatialAudioSource> spatialSources = new List<SpatialAudioSource>();
        private Dictionary<string, SpatialAudioSource> namedSpatialSources = new Dictionary<string, SpatialAudioSource>();
        private Transform listenerTransform;

        // Random one-shots
        private Dictionary<string, float> lastOneShotTime = new Dictionary<string, float>();

        // Events
        public event Action<AmbientPreset> OnPresetChanged;
        public event Action<TimeOfDay> OnTimeOfDayChanged;

        // Properties
        public AmbientPreset CurrentPreset => currentPreset;
        public TimeOfDay CurrentTimeOfDay => currentTimeOfDay;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            InitializeAudioSources();
        }

        private void Start()
        {
            // Find listener (usually main camera)
            if (Camera.main != null)
            {
                listenerTransform = Camera.main.transform;
            }

            // Start with default ambient
            if (ambientLibrary != null)
            {
                UpdateTimeOfDay();
            }

            // Start spatial update loop
            StartCoroutine(SpatialUpdateLoop());

            // Start random one-shot loop
            StartCoroutine(RandomOneShotLoop());
        }

        private void Update()
        {
            // Update listener reference
            if (listenerTransform == null && Camera.main != null)
            {
                listenerTransform = Camera.main.transform;
            }

            // Check time of day changes
            if (useTimeOfDay)
            {
                UpdateTimeOfDay();
            }
        }

        #region Initialization

        private void InitializeAudioSources()
        {
            if (globalAmbientSource == null)
            {
                GameObject obj = new GameObject("GlobalAmbient");
                obj.transform.SetParent(transform);
                globalAmbientSource = obj.AddComponent<AudioSource>();
            }

            if (secondaryAmbientSource == null)
            {
                GameObject obj = new GameObject("SecondaryAmbient");
                obj.transform.SetParent(transform);
                secondaryAmbientSource = obj.AddComponent<AudioSource>();
            }

            ConfigureAmbientSource(globalAmbientSource);
            ConfigureAmbientSource(secondaryAmbientSource);
        }

        private void ConfigureAmbientSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f; // 2D global sound
            source.priority = 200; // Lower priority than music
            source.outputAudioMixerGroup = AudioManager.Instance?.AmbientGroup;
        }

        #endregion

        #region Ambient Presets

        /// <summary>
        /// Set ambient preset by ID
        /// </summary>
        public void SetPreset(string presetId, bool crossfade = true)
        {
            if (ambientLibrary == null) return;

            AmbientPreset preset = ambientLibrary.GetPreset(presetId);
            if (preset != null)
            {
                SetPreset(preset, crossfade);
            }
            else
            {
                Debug.LogWarning($"[AmbientSoundManager] Preset not found: {presetId}");
            }
        }

        /// <summary>
        /// Set ambient preset
        /// </summary>
        public void SetPreset(AmbientPreset preset, bool crossfade = true)
        {
            if (preset == null) return;

            // Don't restart if same preset
            if (currentPreset != null && currentPreset.id == preset.id)
            {
                return;
            }

            if (crossfade && globalAmbientSource.isPlaying)
            {
                CrossfadeToPreset(preset);
            }
            else
            {
                SetPresetImmediate(preset);
            }
        }

        /// <summary>
        /// Set preset immediately without crossfade
        /// </summary>
        public void SetPresetImmediate(AmbientPreset preset)
        {
            if (preset == null) return;

            if (crossfadeCoroutine != null)
            {
                StopCoroutine(crossfadeCoroutine);
                isCrossfading = false;
            }

            currentPreset = preset;

            // Set main ambient loop
            if (preset.mainLoop != null)
            {
                globalAmbientSource.clip = preset.mainLoop;
                globalAmbientSource.volume = preset.mainLoopVolume;
                globalAmbientSource.Play();
            }
            else
            {
                globalAmbientSource.Stop();
            }

            // Set secondary layer
            if (preset.secondaryLoop != null)
            {
                secondaryAmbientSource.clip = preset.secondaryLoop;
                secondaryAmbientSource.volume = preset.secondaryLoopVolume;
                secondaryAmbientSource.Play();
            }
            else
            {
                secondaryAmbientSource.Stop();
            }

            OnPresetChanged?.Invoke(preset);
        }

        /// <summary>
        /// Crossfade to a new preset
        /// </summary>
        public void CrossfadeToPreset(AmbientPreset preset)
        {
            if (preset == null) return;

            if (crossfadeCoroutine != null)
            {
                StopCoroutine(crossfadeCoroutine);
            }

            targetPreset = preset;
            crossfadeCoroutine = StartCoroutine(CrossfadeCoroutine(preset));
        }

        private IEnumerator CrossfadeCoroutine(AmbientPreset newPreset)
        {
            isCrossfading = true;

            float elapsed = 0f;
            float startVolumeMain = globalAmbientSource.volume;
            float startVolumeSecondary = secondaryAmbientSource.volume;

            // Fade out current
            while (elapsed < crossfadeDuration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (crossfadeDuration / 2f);

                globalAmbientSource.volume = Mathf.Lerp(startVolumeMain, 0f, t);
                secondaryAmbientSource.volume = Mathf.Lerp(startVolumeSecondary, 0f, t);

                yield return null;
            }

            // Switch clips
            currentPreset = newPreset;

            if (newPreset.mainLoop != null)
            {
                globalAmbientSource.clip = newPreset.mainLoop;
                globalAmbientSource.Play();
            }
            else
            {
                globalAmbientSource.Stop();
            }

            if (newPreset.secondaryLoop != null)
            {
                secondaryAmbientSource.clip = newPreset.secondaryLoop;
                secondaryAmbientSource.Play();
            }
            else
            {
                secondaryAmbientSource.Stop();
            }

            // Fade in new
            elapsed = 0f;
            while (elapsed < crossfadeDuration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (crossfadeDuration / 2f);

                if (newPreset.mainLoop != null)
                {
                    globalAmbientSource.volume = Mathf.Lerp(0f, newPreset.mainLoopVolume, t);
                }
                if (newPreset.secondaryLoop != null)
                {
                    secondaryAmbientSource.volume = Mathf.Lerp(0f, newPreset.secondaryLoopVolume, t);
                }

                yield return null;
            }

            isCrossfading = false;
            crossfadeCoroutine = null;
            targetPreset = null;

            OnPresetChanged?.Invoke(newPreset);
        }

        #endregion

        #region Time of Day

        private void UpdateTimeOfDay()
        {
            float hour = DateTime.Now.Hour + DateTime.Now.Minute / 60f;
            
            TimeOfDay newTimeOfDay;
            if (hour >= dayStartHour && hour < nightStartHour)
            {
                newTimeOfDay = TimeOfDay.Day;
            }
            else
            {
                newTimeOfDay = TimeOfDay.Night;
            }

            if (newTimeOfDay != currentTimeOfDay)
            {
                currentTimeOfDay = newTimeOfDay;
                ApplyTimeOfDayPreset();
                OnTimeOfDayChanged?.Invoke(currentTimeOfDay);
            }
        }

        private void ApplyTimeOfDayPreset()
        {
            if (ambientLibrary == null) return;

            string presetId = currentTimeOfDay == TimeOfDay.Day ? "outdoor_day" : "outdoor_night";
            AmbientPreset preset = ambientLibrary.GetPreset(presetId);
            
            if (preset != null)
            {
                SetPreset(preset);
            }
        }

        /// <summary>
        /// Force a specific time of day
        /// </summary>
        public void SetTimeOfDay(TimeOfDay timeOfDay)
        {
            useTimeOfDay = false;
            currentTimeOfDay = timeOfDay;
            ApplyTimeOfDayPreset();
            OnTimeOfDayChanged?.Invoke(currentTimeOfDay);
        }

        /// <summary>
        /// Enable automatic time of day
        /// </summary>
        public void EnableAutoTimeOfDay()
        {
            useTimeOfDay = true;
        }

        #endregion

        #region Spatial Audio

        /// <summary>
        /// Create a spatial audio source at a position
        /// </summary>
        public SpatialAudioSource CreateSpatialSource(string id, AudioClip clip, Vector3 position, float radius = 20f, float volume = 1f, bool loop = true)
        {
            if (spatialSources.Count >= maxSpatialSources)
            {
                // Remove oldest non-essential source
                RemoveOldestSpatialSource();
            }

            GameObject obj = new GameObject($"Spatial_{id}");
            obj.transform.position = position;
            obj.transform.SetParent(transform);

            AudioSource source = obj.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.loop = loop;
            source.spatialBlend = AudioManager.Instance.Is3DAudioEnabled ? 1f : 0f;
            source.minDistance = radius * 0.1f;
            source.maxDistance = radius;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.outputAudioMixerGroup = AudioManager.Instance?.AmbientGroup;
            source.Play();

            SpatialAudioSource spatialSource = new SpatialAudioSource
            {
                id = id,
                source = source,
                position = position,
                radius = radius,
                baseVolume = volume,
                isPersistent = false
            };

            spatialSources.Add(spatialSource);
            namedSpatialSources[id] = spatialSource;

            return spatialSource;
        }

        /// <summary>
        /// Create a persistent spatial source (won't be auto-removed)
        /// </summary>
        public SpatialAudioSource CreatePersistentSpatialSource(string id, AudioClip clip, Vector3 position, float radius = 20f, float volume = 1f)
        {
            SpatialAudioSource source = CreateSpatialSource(id, clip, position, radius, volume, true);
            source.isPersistent = true;
            return source;
        }

        /// <summary>
        /// Remove a spatial source by ID
        /// </summary>
        public void RemoveSpatialSource(string id)
        {
            if (namedSpatialSources.TryGetValue(id, out SpatialAudioSource source))
            {
                spatialSources.Remove(source);
                namedSpatialSources.Remove(id);

                if (source.source != null)
                {
                    Destroy(source.source.gameObject);
                }
            }
        }

        /// <summary>
        /// Update position of a spatial source
        /// </summary>
        public void UpdateSpatialSourcePosition(string id, Vector3 position)
        {
            if (namedSpatialSources.TryGetValue(id, out SpatialAudioSource source))
            {
                source.position = position;
                if (source.source != null)
                {
                    source.source.transform.position = position;
                }
            }
        }

        private void RemoveOldestSpatialSource()
        {
            for (int i = 0; i < spatialSources.Count; i++)
            {
                if (!spatialSources[i].isPersistent)
                {
                    SpatialAudioSource oldest = spatialSources[i];
                    spatialSources.RemoveAt(i);
                    namedSpatialSources.Remove(oldest.id);

                    if (oldest.source != null)
                    {
                        Destroy(oldest.source.gameObject);
                    }
                    return;
                }
            }
        }

        private IEnumerator SpatialUpdateLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(spatialUpdateInterval);

                if (listenerTransform == null) continue;

                // Update volume based on distance
                foreach (var spatial in spatialSources)
                {
                    if (spatial.source == null) continue;

                    float distance = Vector3.Distance(listenerTransform.position, spatial.position);
                    
                    if (distance > spatial.radius * 2f)
                    {
                        // Too far, mute
                        spatial.source.volume = 0f;
                    }
                    else if (distance < spatial.radius * 0.1f)
                    {
                        // Very close, full volume
                        spatial.source.volume = spatial.baseVolume;
                    }
                    else
                    {
                        // Distance-based volume
                        float t = 1f - (distance / spatial.radius);
                        spatial.source.volume = spatial.baseVolume * Mathf.Clamp01(t);
                    }
                }

                // Clean up destroyed sources
                spatialSources.RemoveAll(s => s.source == null);
            }
        }

        #endregion

        #region Random One-Shots

        /// <summary>
        /// Play a random one-shot ambient sound
        /// </summary>
        public void PlayRandomOneShot(string category, float volumeMultiplier = 1f)
        {
            if (currentPreset == null || currentPreset.oneShots == null) return;

            var oneShots = currentPreset.oneShots.FindAll(o => o.category == category);
            if (oneShots.Count == 0) return;

            var oneShot = oneShots[UnityEngine.Random.Range(0, oneShots.Count)];
            PlayOneShot(oneShot, volumeMultiplier);
        }

        /// <summary>
        /// Play a specific one-shot
        /// </summary>
        public void PlayOneShot(AmbientOneShot oneShot, float volumeMultiplier = 1f)
        {
            if (oneShot == null || oneShot.clips == null || oneShot.clips.Length == 0) return;

            // Check cooldown
            string key = oneShot.category;
            if (lastOneShotTime.ContainsKey(key))
            {
                if (Time.time - lastOneShotTime[key] < oneShot.minInterval)
                {
                    return;
                }
            }

            AudioClip clip = oneShot.clips[UnityEngine.Random.Range(0, oneShot.clips.Length)];
            
            if (oneShot.is3D && listenerTransform != null)
            {
                // Random position around listener
                Vector3 offset = UnityEngine.Random.insideUnitSphere * oneShot.maxDistance;
                offset.y = Mathf.Clamp(offset.y, -5f, 10f);
                Vector3 position = listenerTransform.position + offset;

                AudioManager.Instance?.PlayClipAtPoint(clip, position, oneShot.volume * volumeMultiplier, AudioManager.Instance?.AmbientGroup);
            }
            else
            {
                // 2D one-shot
                globalAmbientSource.PlayOneShot(clip, oneShot.volume * volumeMultiplier);
            }

            lastOneShotTime[key] = Time.time;
        }

        private IEnumerator RandomOneShotLoop()
        {
            while (true)
            {
                // Wait random interval
                float waitTime = UnityEngine.Random.Range(5f, 30f);
                yield return new WaitForSeconds(waitTime);

                // Play random one-shot from current preset
                if (currentPreset != null && currentPreset.oneShots != null && currentPreset.oneShots.Count > 0)
                {
                    var oneShot = currentPreset.oneShots[UnityEngine.Random.Range(0, currentPreset.oneShots.Count)];
                    
                    // Check probability
                    if (UnityEngine.Random.value < oneShot.probability)
                    {
                        PlayOneShot(oneShot);
                    }
                }
            }
        }

        #endregion

        #region Control Methods

        /// <summary>
        /// Stop all ambient sounds
        /// </summary>
        public void StopAll(bool fade = true)
        {
            if (fade)
            {
                StartCoroutine(FadeOutAll());
            }
            else
            {
                globalAmbientSource.Stop();
                secondaryAmbientSource.Stop();
                
                foreach (var spatial in spatialSources)
                {
                    if (spatial.source != null)
                    {
                        spatial.source.Stop();
                    }
                }
            }
        }

        private IEnumerator FadeOutAll()
        {
            float startVolumeMain = globalAmbientSource.volume;
            float startVolumeSecondary = secondaryAmbientSource.volume;
            float elapsed = 0f;
            float fadeDuration = 1f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;

                globalAmbientSource.volume = Mathf.Lerp(startVolumeMain, 0f, t);
                secondaryAmbientSource.volume = Mathf.Lerp(startVolumeSecondary, 0f, t);

                yield return null;
            }

            globalAmbientSource.Stop();
            secondaryAmbientSource.Stop();
        }

        /// <summary>
        /// Pause all ambient sounds
        /// </summary>
        public void PauseAll()
        {
            globalAmbientSource.Pause();
            secondaryAmbientSource.Pause();
            
            foreach (var spatial in spatialSources)
            {
                spatial.source?.Pause();
            }
        }

        /// <summary>
        /// Resume all ambient sounds
        /// </summary>
        public void ResumeAll()
        {
            globalAmbientSource.UnPause();
            secondaryAmbientSource.UnPause();
            
            foreach (var spatial in spatialSources)
            {
                spatial.source?.UnPause();
            }
        }

        #endregion

        #region Convenience Methods

        public void SetIndoorAmbient() => SetPreset("indoor");
        public void SetOutdoorDayAmbient() => SetPreset("outdoor_day");
        public void SetOutdoorNightAmbient() => SetPreset("outdoor_night");
        public void SetCityAmbient() => SetPreset("city");
        public void SetForestAmbient() => SetPreset("forest");
        public void SetBattlefieldAmbient() => SetPreset("battlefield");
        public void SetMenuAmbient() => SetPreset("menu");

        #endregion
    }

    /// <summary>
    /// Time of day enum
    /// </summary>
    public enum TimeOfDay
    {
        Day,
        Night,
        Dawn,
        Dusk
    }

    /// <summary>
    /// Spatial audio source container
    /// </summary>
    public class SpatialAudioSource
    {
        public string id;
        public AudioSource source;
        public Vector3 position;
        public float radius;
        public float baseVolume;
        public bool isPersistent;
    }

    /// <summary>
    /// Ambient one-shot sound configuration
    /// </summary>
    [Serializable]
    public class AmbientOneShot
    {
        public string category;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 0.5f;
        [Range(0f, 1f)] public float probability = 0.3f;
        public float minInterval = 10f;
        public bool is3D = true;
        public float maxDistance = 30f;
    }

    /// <summary>
    /// Ambient preset configuration
    /// </summary>
    [Serializable]
    public class AmbientPreset
    {
        public string id;
        public string displayName;
        public AudioClip mainLoop;
        [Range(0f, 1f)] public float mainLoopVolume = 0.5f;
        public AudioClip secondaryLoop;
        [Range(0f, 1f)] public float secondaryLoopVolume = 0.3f;
        public List<AmbientOneShot> oneShots = new List<AmbientOneShot>();
    }

    /// <summary>
    /// ScriptableObject containing all ambient presets
    /// </summary>
    [CreateAssetMenu(fileName = "AmbientLibrary", menuName = "Apex Citadels/Audio/Ambient Library")]
    public class AmbientLibrary : ScriptableObject
    {
        [SerializeField] private List<AmbientPreset> presets = new List<AmbientPreset>();
        
        private Dictionary<string, AmbientPreset> presetLookup;

        private void OnEnable()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            presetLookup = new Dictionary<string, AmbientPreset>();
            foreach (var preset in presets)
            {
                if (!string.IsNullOrEmpty(preset.id))
                {
                    presetLookup[preset.id] = preset;
                }
            }
        }

        public AmbientPreset GetPreset(string id)
        {
            if (presetLookup == null) BuildLookup();
            presetLookup.TryGetValue(id, out AmbientPreset preset);
            return preset;
        }

        public List<AmbientPreset> GetAllPresets() => presets;

        #if UNITY_EDITOR
        /// <summary>
        /// Generate default preset entries
        /// </summary>
        [ContextMenu("Generate Default Presets")]
        public void GenerateDefaultPresets()
        {
            presets.Clear();

            // Menu
            AddPreset("menu", "Menu Ambient", 0.3f, 0.2f);

            // Outdoor
            AddPreset("outdoor_day", "Outdoor Day", 0.5f, 0.3f);
            AddPreset("outdoor_night", "Outdoor Night", 0.4f, 0.2f);

            // Indoor
            AddPreset("indoor", "Indoor", 0.2f, 0.1f);

            // Environments
            AddPreset("city", "City", 0.5f, 0.4f);
            AddPreset("forest", "Forest", 0.4f, 0.3f);
            AddPreset("beach", "Beach", 0.5f, 0.4f);
            AddPreset("mountains", "Mountains", 0.3f, 0.2f);

            // Special
            AddPreset("battlefield", "Battlefield", 0.6f, 0.5f);
            AddPreset("victory", "Victory", 0.3f, 0f);
            AddPreset("defeat", "Defeat", 0.4f, 0f);

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[AmbientLibrary] Generated {presets.Count} presets");
        }

        private void AddPreset(string id, string name, float mainVolume, float secondaryVolume)
        {
            var preset = new AmbientPreset
            {
                id = id,
                displayName = name,
                mainLoopVolume = mainVolume,
                secondaryLoopVolume = secondaryVolume,
                oneShots = new List<AmbientOneShot>()
            };

            // Add default one-shots based on preset type
            if (id.Contains("outdoor") || id == "forest" || id == "beach" || id == "mountains")
            {
                preset.oneShots.Add(new AmbientOneShot
                {
                    category = "birds",
                    volume = 0.4f,
                    probability = 0.5f,
                    minInterval = 15f,
                    is3D = true,
                    maxDistance = 40f
                });
            }

            if (id.Contains("city"))
            {
                preset.oneShots.Add(new AmbientOneShot
                {
                    category = "traffic",
                    volume = 0.3f,
                    probability = 0.6f,
                    minInterval = 10f,
                    is3D = true,
                    maxDistance = 50f
                });
            }

            if (id == "battlefield")
            {
                preset.oneShots.Add(new AmbientOneShot
                {
                    category = "distant_explosions",
                    volume = 0.5f,
                    probability = 0.4f,
                    minInterval = 20f,
                    is3D = true,
                    maxDistance = 100f
                });
            }

            presets.Add(preset);
        }
        #endif
    }
}
