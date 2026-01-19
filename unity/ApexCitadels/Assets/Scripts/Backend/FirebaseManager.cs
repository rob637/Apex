using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ApexCitadels.Core;

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
                ApexLogger.LogError($"Function {functionName} failed: {e.Message}", ApexLogger.LogCategory.Firebase);
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
                ApexLogger.LogError($"Function {functionName} failed: {e.Message}", ApexLogger.LogCategory.Firebase);
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
            ApexLogger.LogWarning("Firebase not enabled. Running in stub mode.", ApexLogger.LogCategory.Firebase);
        }

        public Task<T> CallFunction<T>(string functionName, Dictionary<string, object> data = null) where T : class
        {
            ApexLogger.LogVerbose($"Stub: CallFunction<{typeof(T).Name}>({functionName})", ApexLogger.LogCategory.Firebase);
            return Task.FromResult<T>(null);
        }

        public Task<Dictionary<string, object>> CallFunction(string functionName, Dictionary<string, object> data = null)
        {
            ApexLogger.LogVerbose($"Stub: CallFunction({functionName})", ApexLogger.LogCategory.Firebase);
            return Task.FromResult(new Dictionary<string, object>());
        }
#endif
    }
}
