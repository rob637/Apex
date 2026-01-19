using System;
using System.Collections.Generic;
using UnityEngine;

namespace ApexCitadels.Core
{
    /// <summary>
    /// Generic object pooling system for Apex Citadels.
    /// Reduces garbage collection by reusing objects.
    /// 
    /// Usage:
    ///   // Get from pool
    ///   var bullet = ObjectPoolManager.Get<BulletBehaviour>(bulletPrefab);
    ///   
    ///   // Return to pool
    ///   ObjectPoolManager.Return(bullet);
    ///   
    ///   // Pre-warm pool
    ///   ObjectPoolManager.Prewarm<DamageNumber>(damageNumberPrefab, 50);
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        public static ObjectPoolManager Instance { get; private set; }

        #region Serialized Fields

        [Header("Settings")]
        [SerializeField] private int defaultPoolSize = 20;
        [SerializeField] private int maxPoolSize = 100;
        [SerializeField] private bool autoExpand = true;
        [SerializeField] private bool logPoolActivity = false;

        [Header("Pre-configured Pools")]
        [SerializeField] private PoolConfig[] preConfiguredPools;

        #endregion

        #region Private Fields

        private Dictionary<int, Pool> _pools = new Dictionary<int, Pool>();
        private Dictionary<int, int> _prefabToPoolId = new Dictionary<int, int>();
        private Transform _poolContainer;
        private int _nextPoolId = 0;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _poolContainer = new GameObject("ObjectPools").transform;
            _poolContainer.SetParent(transform);

            // Initialize pre-configured pools
            InitializePreConfiguredPools();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Get an object from pool
        /// </summary>
        public static T Get<T>(GameObject prefab) where T : Component
        {
            EnsureInstance();
            return Instance.GetFromPool<T>(prefab);
        }

        /// <summary>
        /// Get a GameObject from pool
        /// </summary>
        public static GameObject Get(GameObject prefab)
        {
            EnsureInstance();
            return Instance.GetFromPool(prefab);
        }

        /// <summary>
        /// Get an object from pool at specific position/rotation
        /// </summary>
        public static T Get<T>(GameObject prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            var obj = Get<T>(prefab);
            if (obj != null)
            {
                obj.transform.SetPositionAndRotation(position, rotation);
            }
            return obj;
        }

        /// <summary>
        /// Get a GameObject from pool at specific position/rotation
        /// </summary>
        public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var obj = Get(prefab);
            if (obj != null)
            {
                obj.transform.SetPositionAndRotation(position, rotation);
            }
            return obj;
        }

        /// <summary>
        /// Return an object to its pool
        /// </summary>
        public static void Return(Component component)
        {
            if (Instance != null)
            {
                Instance.ReturnToPool(component.gameObject);
            }
            else
            {
                // Fallback: destroy if pool manager doesn't exist
                Destroy(component.gameObject);
            }
        }

        /// <summary>
        /// Return a GameObject to its pool
        /// </summary>
        public static void Return(GameObject obj)
        {
            if (Instance != null)
            {
                Instance.ReturnToPool(obj);
            }
            else
            {
                Destroy(obj);
            }
        }

        /// <summary>
        /// Return object to pool after delay
        /// </summary>
        public static void ReturnDelayed(GameObject obj, float delay)
        {
            if (Instance != null)
            {
                Instance.StartCoroutine(Instance.ReturnDelayedCoroutine(obj, delay));
            }
        }

        /// <summary>
        /// Pre-warm a pool with instances
        /// </summary>
        public static void Prewarm(GameObject prefab, int count)
        {
            EnsureInstance();
            Instance.PrewarmPool(prefab, count);
        }

        /// <summary>
        /// Pre-warm a pool with instances
        /// </summary>
        public static void Prewarm<T>(GameObject prefab, int count) where T : Component
        {
            Prewarm(prefab, count);
        }

        /// <summary>
        /// Clear a specific pool
        /// </summary>
        public static void ClearPool(GameObject prefab)
        {
            Instance?.ClearPoolInternal(prefab);
        }

        /// <summary>
        /// Clear all pools
        /// </summary>
        public static void ClearAllPools()
        {
            Instance?.ClearAllPoolsInternal();
        }

        /// <summary>
        /// Get pool statistics
        /// </summary>
        public static PoolStats GetStats(GameObject prefab)
        {
            return Instance?.GetPoolStats(prefab) ?? default;
        }

        /// <summary>
        /// Get total pool statistics
        /// </summary>
        public static string GetTotalStats()
        {
            return Instance?.GetTotalStatsInternal() ?? "Pool manager not initialized";
        }

        #endregion

        #region Instance Methods

        private T GetFromPool<T>(GameObject prefab) where T : Component
        {
            var go = GetFromPool(prefab);
            return go?.GetComponent<T>();
        }

        private GameObject GetFromPool(GameObject prefab)
        {
            if (prefab == null)
            {
                ApexLogger.LogWarning("Attempted to get null prefab from pool");
                return null;
            }

            int prefabId = prefab.GetInstanceID();
            
            // Get or create pool
            if (!_prefabToPoolId.TryGetValue(prefabId, out int poolId))
            {
                poolId = CreatePool(prefab);
            }

            var pool = _pools[poolId];
            GameObject obj = null;

            // Try to get from pool
            while (pool.Available.Count > 0)
            {
                obj = pool.Available.Pop();
                if (obj != null)
                {
                    break;
                }
                // Object was destroyed externally, continue looking
                pool.TotalCreated--;
            }

            // Create new if needed
            if (obj == null)
            {
                if (pool.TotalCreated >= maxPoolSize && !autoExpand)
                {
                    ApexLogger.LogWarning($"Pool for {prefab.name} at max capacity ({maxPoolSize})");
                    return null;
                }

                obj = CreateNewInstance(prefab, pool);
            }

            // Activate and configure
            obj.SetActive(true);
            obj.transform.SetParent(null);
            pool.ActiveCount++;
            pool.TotalGets++;

            if (logPoolActivity)
            {
                ApexLogger.LogVerbose($"Pool GET: {prefab.name} (Active: {pool.ActiveCount}, Available: {pool.Available.Count})");
            }

            // Notify poolable
            var poolable = obj.GetComponent<IPoolable>();
            poolable?.OnGetFromPool();

            return obj;
        }

        private void ReturnToPool(GameObject obj)
        {
            if (obj == null) return;

            var pooled = obj.GetComponent<PooledObject>();
            if (pooled == null || !_pools.TryGetValue(pooled.PoolId, out var pool))
            {
                // Not from a pool, just destroy
                Destroy(obj);
                return;
            }

            // Notify poolable
            var poolable = obj.GetComponent<IPoolable>();
            poolable?.OnReturnToPool();

            // Deactivate and return
            obj.SetActive(false);
            obj.transform.SetParent(pool.Container);
            pool.Available.Push(obj);
            pool.ActiveCount--;
            pool.TotalReturns++;

            if (logPoolActivity)
            {
                ApexLogger.LogVerbose($"Pool RETURN: {pool.Prefab.name} (Active: {pool.ActiveCount}, Available: {pool.Available.Count})");
            }
        }

        private System.Collections.IEnumerator ReturnDelayedCoroutine(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnToPool(obj);
        }

        private int CreatePool(GameObject prefab)
        {
            int poolId = _nextPoolId++;
            int prefabId = prefab.GetInstanceID();

            // Create container
            var container = new GameObject($"Pool_{prefab.name}").transform;
            container.SetParent(_poolContainer);

            var pool = new Pool
            {
                Id = poolId,
                Prefab = prefab,
                Container = container,
                Available = new Stack<GameObject>(),
                ActiveCount = 0,
                TotalCreated = 0,
                TotalGets = 0,
                TotalReturns = 0
            };

            _pools[poolId] = pool;
            _prefabToPoolId[prefabId] = poolId;

            ApexLogger.Log($"Created pool for: {prefab.name}", ApexLogger.LogCategory.Performance);

            return poolId;
        }

        private GameObject CreateNewInstance(GameObject prefab, Pool pool)
        {
            var obj = Instantiate(prefab, pool.Container);
            obj.name = $"{prefab.name}_{pool.TotalCreated}";
            obj.SetActive(false);

            // Add pool tracking component
            var pooled = obj.AddComponent<PooledObject>();
            pooled.PoolId = pool.Id;

            pool.TotalCreated++;

            return obj;
        }

        private void PrewarmPool(GameObject prefab, int count)
        {
            int prefabId = prefab.GetInstanceID();
            
            if (!_prefabToPoolId.TryGetValue(prefabId, out int poolId))
            {
                poolId = CreatePool(prefab);
            }

            var pool = _pools[poolId];
            int toCreate = Mathf.Min(count - pool.Available.Count, maxPoolSize - pool.TotalCreated);

            for (int i = 0; i < toCreate; i++)
            {
                var obj = CreateNewInstance(prefab, pool);
                pool.Available.Push(obj);
            }

            ApexLogger.Log($"Prewarmed pool: {prefab.name} with {toCreate} instances (Total: {pool.TotalCreated})", ApexLogger.LogCategory.Performance);
        }

        private void ClearPoolInternal(GameObject prefab)
        {
            int prefabId = prefab.GetInstanceID();
            
            if (_prefabToPoolId.TryGetValue(prefabId, out int poolId))
            {
                var pool = _pools[poolId];
                
                while (pool.Available.Count > 0)
                {
                    var obj = pool.Available.Pop();
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                }
                
                pool.TotalCreated = pool.ActiveCount;
            }
        }

        private void ClearAllPoolsInternal()
        {
            foreach (var pool in _pools.Values)
            {
                while (pool.Available.Count > 0)
                {
                    var obj = pool.Available.Pop();
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                }
                pool.TotalCreated = pool.ActiveCount;
            }
        }

        private PoolStats GetPoolStats(GameObject prefab)
        {
            int prefabId = prefab.GetInstanceID();
            
            if (_prefabToPoolId.TryGetValue(prefabId, out int poolId))
            {
                var pool = _pools[poolId];
                return new PoolStats
                {
                    PrefabName = prefab.name,
                    TotalCreated = pool.TotalCreated,
                    ActiveCount = pool.ActiveCount,
                    AvailableCount = pool.Available.Count,
                    TotalGets = pool.TotalGets,
                    TotalReturns = pool.TotalReturns
                };
            }

            return default;
        }

        private string GetTotalStatsInternal()
        {
            int totalCreated = 0;
            int totalActive = 0;
            int totalAvailable = 0;

            foreach (var pool in _pools.Values)
            {
                totalCreated += pool.TotalCreated;
                totalActive += pool.ActiveCount;
                totalAvailable += pool.Available.Count;
            }

            return $"Pools: {_pools.Count} | Created: {totalCreated} | Active: {totalActive} | Available: {totalAvailable}";
        }

        private void InitializePreConfiguredPools()
        {
            if (preConfiguredPools == null) return;

            foreach (var config in preConfiguredPools)
            {
                if (config.Prefab != null && config.PrewarmCount > 0)
                {
                    PrewarmPool(config.Prefab, config.PrewarmCount);
                }
            }
        }

        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                var go = new GameObject("ObjectPoolManager");
                Instance = go.AddComponent<ObjectPoolManager>();
            }
        }

        #endregion

        #region Data Types

        private class Pool
        {
            public int Id;
            public GameObject Prefab;
            public Transform Container;
            public Stack<GameObject> Available;
            public int ActiveCount;
            public int TotalCreated;
            public int TotalGets;
            public int TotalReturns;
        }

        [Serializable]
        public class PoolConfig
        {
            public GameObject Prefab;
            public int PrewarmCount = 10;
        }

        public struct PoolStats
        {
            public string PrefabName;
            public int TotalCreated;
            public int ActiveCount;
            public int AvailableCount;
            public int TotalGets;
            public int TotalReturns;

            public float ReuseRate => TotalGets > 0 ? (float)TotalReturns / TotalGets : 0f;
        }

        #endregion

        #region Debug

        [ContextMenu("Log Pool Stats")]
        private void LogPoolStats()
        {
            ApexLogger.Log($"=== Object Pool Stats ===\n{GetTotalStatsInternal()}");
            foreach (var pool in _pools.Values)
            {
                ApexLogger.Log($"  {pool.Prefab.name}: Created={pool.TotalCreated}, Active={pool.ActiveCount}, Available={pool.Available.Count}");
            }
        }

        #endregion
    }

    #region Pool Interfaces

    /// <summary>
    /// Interface for objects that need initialization when retrieved from pool
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Called when object is retrieved from pool
        /// </summary>
        void OnGetFromPool();

        /// <summary>
        /// Called when object is returned to pool
        /// </summary>
        void OnReturnToPool();
    }

    /// <summary>
    /// Component added to track pooled objects
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        [HideInInspector]
        public int PoolId;

        /// <summary>
        /// Return this object to its pool
        /// </summary>
        public void ReturnToPool()
        {
            ObjectPoolManager.Return(gameObject);
        }

        /// <summary>
        /// Return to pool after delay
        /// </summary>
        public void ReturnToPool(float delay)
        {
            ObjectPoolManager.ReturnDelayed(gameObject, delay);
        }
    }

    #endregion

    #region Extension Methods

    /// <summary>
    /// Extension methods for easy pooling
    /// </summary>
    public static class ObjectPoolExtensions
    {
        /// <summary>
        /// Return this GameObject to its pool
        /// </summary>
        public static void ReturnToPool(this GameObject obj)
        {
            ObjectPoolManager.Return(obj);
        }

        /// <summary>
        /// Return this component's GameObject to its pool
        /// </summary>
        public static void ReturnToPool(this Component component)
        {
            ObjectPoolManager.Return(component);
        }

        /// <summary>
        /// Return to pool after delay
        /// </summary>
        public static void ReturnToPool(this GameObject obj, float delay)
        {
            ObjectPoolManager.ReturnDelayed(obj, delay);
        }

        /// <summary>
        /// Spawn from pool (alias for Get)
        /// </summary>
        public static GameObject Spawn(this GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return ObjectPoolManager.Get(prefab, position, rotation);
        }

        /// <summary>
        /// Spawn from pool with component
        /// </summary>
        public static T Spawn<T>(this GameObject prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            return ObjectPoolManager.Get<T>(prefab, position, rotation);
        }
    }

    #endregion
}
