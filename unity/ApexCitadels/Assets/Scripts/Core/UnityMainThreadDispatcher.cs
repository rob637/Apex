using System;
using System.Collections.Generic;
using UnityEngine;
using ApexCitadels.Core;

namespace ApexCitadels.Core
{
    /// <summary>
    /// Dispatches actions to the Unity main thread.
    /// Useful for Firebase callbacks that may occur on background threads.
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher _instance;
        private readonly Queue<Action> _executionQueue = new Queue<Action>();
        private readonly object _lock = new object();

        public static UnityMainThreadDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("UnityMainThreadDispatcher");
                    _instance = go.AddComponent<UnityMainThreadDispatcher>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
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
        }

        private void Update()
        {
            lock (_lock)
            {
                while (_executionQueue.Count > 0)
                {
                    try
                    {
                        _executionQueue.Dequeue()?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        ApexLogger.LogError($"[MainThreadDispatcher] Error executing action: {ex.Message}", ApexLogger.LogCategory.General);
                    }
                }
            }
        }

        /// <summary>
        /// Enqueue an action to be executed on the main thread
        /// </summary>
        public void Enqueue(Action action)
        {
            if (action == null) return;

            lock (_lock)
            {
                _executionQueue.Enqueue(action);
            }
        }

        /// <summary>
        /// Check if we're currently on the main thread
        /// </summary>
        public static bool IsMainThread()
        {
            return System.Threading.Thread.CurrentThread.ManagedThreadId == 1;
        }

        /// <summary>
        /// Execute on main thread, or immediately if already on main thread
        /// </summary>
        public void ExecuteOnMainThread(Action action)
        {
            if (IsMainThread())
            {
                action?.Invoke();
            }
            else
            {
                Enqueue(action);
            }
        }
    }
}
