using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ApexCitadels.Tests.Runtime
{
    /// <summary>
    /// PlayMode integration tests that run in the Unity runtime.
    /// These tests can test actual MonoBehaviour lifecycle, coroutines, and scene loading.
    /// </summary>
    [TestFixture]
    public class SmokeTests
    {
        /// <summary>
        /// Verify that Unity can instantiate a basic GameObject
        /// </summary>
        [UnityTest]
        public IEnumerator Unity_CanInstantiateGameObject()
        {
            // Arrange & Act
            var go = new GameObject("SmokeTest");
            
            yield return null; // Wait one frame
            
            // Assert
            Assert.IsNotNull(go, "Should be able to create GameObject");
            Assert.AreEqual("SmokeTest", go.name);
            
            // Cleanup
            Object.Destroy(go);
        }

        /// <summary>
        /// Verify that a component can be added and accessed
        /// </summary>
        [UnityTest]
        public IEnumerator Unity_CanAddAndAccessComponent()
        {
            // Arrange
            var go = new GameObject("ComponentTest");
            
            // Act
            var rigidbody = go.AddComponent<Rigidbody>();
            yield return null;
            
            // Assert
            Assert.IsNotNull(rigidbody, "Should be able to add Rigidbody");
            Assert.IsNotNull(go.GetComponent<Rigidbody>(), "Should be able to get Rigidbody");
            
            // Cleanup
            Object.Destroy(go);
        }

        /// <summary>
        /// Verify that async operations work correctly
        /// </summary>
        [UnityTest]
        public IEnumerator Unity_AsyncOperationsWork()
        {
            // Arrange
            float startTime = Time.time;
            float waitTime = 0.1f;
            
            // Act
            yield return new WaitForSeconds(waitTime);
            
            // Assert
            float elapsed = Time.time - startTime;
            Assert.That(elapsed, Is.GreaterThanOrEqualTo(waitTime * 0.9f), 
                "WaitForSeconds should wait approximately the specified time");
        }

        /// <summary>
        /// Verify that MonoBehaviour lifecycle events fire
        /// </summary>
        [UnityTest]
        public IEnumerator MonoBehaviour_LifecycleEventsWork()
        {
            // Arrange
            var go = new GameObject("LifecycleTest");
            var tracker = go.AddComponent<LifecycleTracker>();
            
            yield return null; // Wait for Start
            
            // Assert
            Assert.IsTrue(tracker.AwakeCalled, "Awake should be called");
            Assert.IsTrue(tracker.StartCalled, "Start should be called");
            
            // Cleanup
            Object.Destroy(go);
            yield return null;
            
            Assert.IsTrue(tracker.OnDestroyCalled, "OnDestroy should be called");
        }

        /// <summary>
        /// Helper component to track lifecycle events
        /// </summary>
        private class LifecycleTracker : MonoBehaviour
        {
            public bool AwakeCalled { get; private set; }
            public bool StartCalled { get; private set; }
            public bool OnDestroyCalled { get; private set; }

            private void Awake() => AwakeCalled = true;
            private void Start() => StartCalled = true;
            private void OnDestroy() => OnDestroyCalled = true;
        }
    }
}
