using System;
using System.Collections.Generic;
using UnityEngine;

namespace ApexCitadels.Core
{
    /// <summary>
    /// Service Locator pattern for safe access to game services.
    /// Provides null-safe access to singletons with fallback creation.
    /// 
    /// Usage:
    ///   var audioManager = ServiceLocator.Get<AudioManager>();
    ///   ServiceLocator.Register<IAnalyticsService>(new FirebaseAnalytics());
    ///   
    /// With safe access:
    ///   ServiceLocator.TryGet<GameManager>(out var manager);
    ///   
    /// This prevents NullReferenceException when accessing singletons
    /// that may not be initialized yet or have been destroyed.
    /// </summary>
    public static class ServiceLocator
    {
        #region Private Fields

        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private static readonly Dictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>();
        private static readonly object _lock = new object();
        private static bool _isQuitting = false;

        #endregion

        #region Initialization

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            // Reset on domain reload (editor play mode)
            _services.Clear();
            _factories.Clear();
            _isQuitting = false;

            // Register for application quit
            Application.quitting += OnApplicationQuit;
        }

        private static void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        #endregion

        #region Registration

        /// <summary>
        /// Register a service instance
        /// </summary>
        public static void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                ApexLogger.LogWarning($"Attempted to register null service for type {typeof(T).Name}");
                return;
            }

            lock (_lock)
            {
                var type = typeof(T);
                if (_services.ContainsKey(type))
                {
                    ApexLogger.LogWarning($"Service {type.Name} is already registered. Overwriting.");
                }
                _services[type] = service;
                ApexLogger.LogVerbose($"Registered service: {type.Name}");
            }
        }

        /// <summary>
        /// Register a factory for lazy instantiation
        /// </summary>
        public static void RegisterFactory<T>(Func<T> factory) where T : class
        {
            if (factory == null)
            {
                ApexLogger.LogWarning($"Attempted to register null factory for type {typeof(T).Name}");
                return;
            }

            lock (_lock)
            {
                _factories[typeof(T)] = () => factory();
            }
        }

        /// <summary>
        /// Register a MonoBehaviour singleton that auto-registers itself
        /// </summary>
        public static void RegisterSingleton<T>(T singleton) where T : MonoBehaviour
        {
            Register<T>(singleton);
        }

        /// <summary>
        /// Unregister a service
        /// </summary>
        public static void Unregister<T>() where T : class
        {
            lock (_lock)
            {
                var type = typeof(T);
                _services.Remove(type);
                _factories.Remove(type);
            }
        }

        /// <summary>
        /// Unregister a specific instance (useful for destroyed MonoBehaviours)
        /// </summary>
        public static void Unregister<T>(T instance) where T : class
        {
            lock (_lock)
            {
                var type = typeof(T);
                if (_services.TryGetValue(type, out var registered) && ReferenceEquals(registered, instance))
                {
                    _services.Remove(type);
                }
            }
        }

        #endregion

        #region Retrieval

        /// <summary>
        /// Get a registered service. Returns null if not found.
        /// </summary>
        public static T Get<T>() where T : class
        {
            if (_isQuitting)
                return null;

            lock (_lock)
            {
                var type = typeof(T);

                // Check registered services
                if (_services.TryGetValue(type, out var service))
                {
                    // Validate MonoBehaviour hasn't been destroyed
                    if (service is MonoBehaviour mb && mb == null)
                    {
                        _services.Remove(type);
                        return TryCreateFromFactory<T>();
                    }
                    return service as T;
                }

                // Try factory
                return TryCreateFromFactory<T>();
            }
        }

        /// <summary>
        /// Try to get a service, returns false if not available
        /// </summary>
        public static bool TryGet<T>(out T service) where T : class
        {
            service = Get<T>();
            return service != null;
        }

        /// <summary>
        /// Get a service or create it if it doesn't exist (for MonoBehaviours)
        /// </summary>
        public static T GetOrCreate<T>() where T : MonoBehaviour
        {
            if (_isQuitting)
                return null;

            var service = Get<T>();
            if (service != null)
                return service;

            // Find in scene
            service = UnityEngine.Object.FindFirstObjectByType<T>();
            if (service != null)
            {
                Register(service);
                return service;
            }

            // Create new GameObject with component
            var go = new GameObject($"[{typeof(T).Name}]");
            service = go.AddComponent<T>();
            UnityEngine.Object.DontDestroyOnLoad(go);
            Register(service);

            ApexLogger.Log($"Created service on demand: {typeof(T).Name}");
            return service;
        }

        /// <summary>
        /// Check if a service is registered
        /// </summary>
        public static bool IsRegistered<T>() where T : class
        {
            lock (_lock)
            {
                return _services.ContainsKey(typeof(T)) || _factories.ContainsKey(typeof(T));
            }
        }

        private static T TryCreateFromFactory<T>() where T : class
        {
            var type = typeof(T);
            if (_factories.TryGetValue(type, out var factory))
            {
                var service = factory() as T;
                if (service != null)
                {
                    _services[type] = service;
                    return service;
                }
            }
            return null;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Clear all registered services (useful for tests)
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _services.Clear();
                _factories.Clear();
            }
        }

        /// <summary>
        /// Get count of registered services
        /// </summary>
        public static int ServiceCount
        {
            get
            {
                lock (_lock)
                {
                    return _services.Count;
                }
            }
        }

        /// <summary>
        /// Log all registered services (debug)
        /// </summary>
        public static void LogRegisteredServices()
        {
            lock (_lock)
            {
                ApexLogger.Log($"=== Registered Services ({_services.Count}) ===");
                foreach (var kvp in _services)
                {
                    ApexLogger.Log($"  - {kvp.Key.Name}: {kvp.Value?.GetType().Name ?? "null"}");
                }
            }
        }

        #endregion
    }

    #region Service Registration Helper

    /// <summary>
    /// Base class for MonoBehaviour singletons that auto-register with ServiceLocator
    /// </summary>
    public abstract class RegisteredSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance => ServiceLocator.Get<T>();

        protected virtual void Awake()
        {
            if (ServiceLocator.IsRegistered<T>())
            {
                var existing = ServiceLocator.Get<T>();
                if (existing != null && existing != this)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            ServiceLocator.Register(this as T);
            
            if (transform.parent != null)
                transform.SetParent(null);
            
            DontDestroyOnLoad(gameObject);
            
            OnSingletonAwake();
        }

        protected virtual void OnDestroy()
        {
            ServiceLocator.Unregister(this as T);
            OnSingletonDestroy();
        }

        /// <summary>
        /// Called after singleton registration in Awake
        /// </summary>
        protected virtual void OnSingletonAwake() { }

        /// <summary>
        /// Called before singleton unregistration in OnDestroy
        /// </summary>
        protected virtual void OnSingletonDestroy() { }
    }

    #endregion

    #region Safe Access Extensions

    /// <summary>
    /// Extension methods for safe service access
    /// </summary>
    public static class ServiceLocatorExtensions
    {
        /// <summary>
        /// Safely execute an action on a service if it exists
        /// </summary>
        public static void WithService<T>(this MonoBehaviour _, Action<T> action) where T : class
        {
            if (ServiceLocator.TryGet<T>(out var service))
            {
                action(service);
            }
        }

        /// <summary>
        /// Safely get a value from a service, with default fallback
        /// </summary>
        public static TResult FromService<T, TResult>(this MonoBehaviour _, Func<T, TResult> getter, TResult defaultValue = default) where T : class
        {
            if (ServiceLocator.TryGet<T>(out var service))
            {
                return getter(service);
            }
            return defaultValue;
        }
    }

    #endregion
}
