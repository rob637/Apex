using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ApexCitadels.Core;

namespace ApexCitadels.Tests.Runtime.Core
{
    /// <summary>
    /// PlayMode tests for ServiceLocator with actual MonoBehaviours.
    /// </summary>
    [TestFixture]
    public class ServiceLocatorPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Clear();
        }

        /// <summary>
        /// Test that GetOrCreate creates a MonoBehaviour if not registered
        /// </summary>
        [UnityTest]
        public IEnumerator GetOrCreate_CreatesMonoBehaviourIfNotRegistered()
        {
            // Act
            var service = ServiceLocator.GetOrCreate<TestMonoBehaviourService>();
            yield return null;

            // Assert
            Assert.IsNotNull(service, "Should create service");
            Assert.IsNotNull(service.gameObject, "Service should have a GameObject");
            Assert.That(service.gameObject.name, Does.Contain("TestMonoBehaviourService"));

            // Cleanup
            Object.Destroy(service.gameObject);
        }

        /// <summary>
        /// Test that destroyed MonoBehaviours are properly handled
        /// </summary>
        [UnityTest]
        public IEnumerator Get_DestroyedMonoBehaviour_ReturnsNull()
        {
            // Arrange
            var go = new GameObject("TestService");
            var service = go.AddComponent<TestMonoBehaviourService>();
            ServiceLocator.Register(service);

            // Verify it's registered
            Assert.IsNotNull(ServiceLocator.Get<TestMonoBehaviourService>());

            // Act - Destroy the GameObject
            Object.DestroyImmediate(go);
            yield return null;

            // Assert - Should return null since the object was destroyed
            var result = ServiceLocator.Get<TestMonoBehaviourService>();
            Assert.IsNull(result, "Should return null for destroyed MonoBehaviour");
        }

        /// <summary>
        /// Test service that's a MonoBehaviour
        /// </summary>
        private class TestMonoBehaviourService : MonoBehaviour
        {
            public string TestValue = "test";
        }
    }
}
