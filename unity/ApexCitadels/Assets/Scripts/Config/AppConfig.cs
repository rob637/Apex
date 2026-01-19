using UnityEngine;
using System;
using ApexCitadels.Core;

namespace ApexCitadels.Config
{
    /// <summary>
    /// Loads and provides access to app configuration.
    /// Configuration is stored in Resources/AppConfig.json
    /// </summary>
    public class AppConfig : MonoBehaviour
    {
        private static AppConfig _instance;
        private static ConfigData _config;
        
        public static AppConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("AppConfig");
                    _instance = go.AddComponent<AppConfig>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public static ConfigData Config
        {
            get
            {
                if (_config == null)
                {
                    LoadConfig();
                }
                return _config;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadConfig();
        }

        private static void LoadConfig()
        {
            var configJson = UnityEngine.Resources.Load<TextAsset>("AppConfig");
            if (configJson != null)
            {
                _config = JsonUtility.FromJson<ConfigData>(configJson.text);
                ApexLogger.Log($"Loaded config for project: {_config.firebase.projectId}", ApexLogger.LogCategory.General);
            }
            else
            {
                ApexLogger.LogError("AppConfig.json not found in Resources folder!", ApexLogger.LogCategory.General);
                _config = new ConfigData();
            }
        }

        // Helper properties for common access patterns
        public static bool IsEmulatorMode => Config.settings?.emulatorMode ?? false;
        public static string FirebaseProjectId => Config.firebase?.projectId ?? "";
        public static string GeospatialApiKey => Config.arcore?.geospatialApiKey ?? "";
        
        public static string GetEmulatorUrl(string service)
        {
            if (!IsEmulatorMode) return null;
            
            var host = Config.settings.emulatorHost;
            var port = service switch
            {
                "auth" => Config.settings.emulatorPorts.auth,
                "firestore" => Config.settings.emulatorPorts.firestore,
                "functions" => Config.settings.emulatorPorts.functions,
                _ => 0
            };
            
            return port > 0 ? $"http://{host}:{port}" : null;
        }
    }

    #region Config Data Classes
    [Serializable]
    public class ConfigData
    {
        public ProjectConfig project;
        public FirebaseConfig firebase;
        public ARCoreConfig arcore;
        public SettingsConfig settings;
    }

    [Serializable]
    public class ProjectConfig
    {
        public string name;
        public BundleIdentifiers bundleIdentifier;
    }

    [Serializable]
    public class BundleIdentifiers
    {
        public string android;
        public string ios;
    }

    [Serializable]
    public class FirebaseConfig
    {
        public string projectId;
        public string storageBucket;
        public string messagingSenderId;
        public AppIds appId;
    }

    [Serializable]
    public class AppIds
    {
        public string android;
        public string ios;
    }

    [Serializable]
    public class ARCoreConfig
    {
        public string geospatialApiKey;
        public bool geospatialEnabled;
        public bool cloudAnchorsEnabled;
    }

    [Serializable]
    public class SettingsConfig
    {
        public bool emulatorMode;
        public string emulatorHost;
        public EmulatorPorts emulatorPorts;
    }

    [Serializable]
    public class EmulatorPorts
    {
        public int auth;
        public int firestore;
        public int functions;
    }
    #endregion
}
