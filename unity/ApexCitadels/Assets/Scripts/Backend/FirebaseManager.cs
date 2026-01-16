using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#if FIREBASE_ENABLED
using Firebase.Functions;
using Firebase.Extensions;
using Newtonsoft.Json;
#endif

namespace ApexCitadels.Backend
{
    /// <summary>
    /// Firebase manager singleton for cloud functions
    /// </summary>
    public class FirebaseManager : MonoBehaviour
    {
        private static FirebaseManager _instance;
        public static FirebaseManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("FirebaseManager");
                    _instance = go.AddComponent<FirebaseManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

#if FIREBASE_ENABLED
        private FirebaseFunctions _functions;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            _functions = FirebaseFunctions.DefaultInstance;
        }

        /// <summary>
        /// Call a Firebase Cloud Function and deserialize the result
        /// </summary>
        public async Task<T> CallFunction<T>(string functionName, Dictionary<string, object> data = null) where T : class
        {
            try
            {
                var callable = _functions.GetHttpsCallable(functionName);
                var result = data != null 
                    ? await callable.CallAsync(data) 
                    : await callable.CallAsync();
                
                if (result?.Data != null)
                {
                    return JsonConvert.DeserializeObject<T>(result.Data.ToString());
                }
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseManager] Function {functionName} failed: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Call a Firebase Cloud Function without expecting a typed result
        /// </summary>
        public async Task<Dictionary<string, object>> CallFunction(string functionName, Dictionary<string, object> data = null)
        {
            try
            {
                var callable = _functions.GetHttpsCallable(functionName);
                var result = data != null 
                    ? await callable.CallAsync(data) 
                    : await callable.CallAsync();
                
                if (result?.Data != null)
                {
                    return JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                }
                return new Dictionary<string, object>();
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseManager] Function {functionName} failed: {e.Message}");
                throw;
            }
        }
#else
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            Debug.LogWarning("[FirebaseManager] Firebase not enabled. Running in stub mode.");
        }

        public Task<T> CallFunction<T>(string functionName, Dictionary<string, object> data = null) where T : class
        {
            Debug.LogWarning($"[FirebaseManager] Stub: CallFunction<{typeof(T).Name}>({functionName})");
            return Task.FromResult<T>(null);
        }

        public Task<Dictionary<string, object>> CallFunction(string functionName, Dictionary<string, object> data = null)
        {
            Debug.LogWarning($"[FirebaseManager] Stub: CallFunction({functionName})");
            return Task.FromResult(new Dictionary<string, object>());
        }
#endif
    }
}
