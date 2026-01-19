using NUnit.Framework;
using UnityEngine;
using ApexCitadels.Core;

namespace ApexCitadels.Tests.Editor.Core
{
    /// <summary>
    /// Unit tests for the ServiceLocator pattern implementation.
    /// Tests registration, retrieval, and lifecycle management.
    /// </summary>
    [TestFixture]
    public class ServiceLocatorTests : TestBase
    {
        // Test service interfaces and implementations
        private interface ITestService
        {
            string GetValue();
        }

        private class TestServiceImpl : ITestService
        {
            private string _value;
            public TestServiceImpl(string value = "default") => _value = value;
            public string GetValue() => _value;
        }

        private class AnotherTestService
        {
            public int Counter { get; set; }
        }

        public override void SetUp()
        {
            base.SetUp();
            // Clear ServiceLocator state between tests
            ServiceLocator.Clear();
        }

        public override void TearDown()
        {
            ServiceLocator.Clear();
            base.TearDown();
        }

        #region Registration Tests

        [Test]
        public void Register_ValidService_CanBeRetrieved()
        {
            // Arrange
            var service = new TestServiceImpl("test_value");

            // Act
            ServiceLocator.Register<ITestService>(service);
            var retrieved = ServiceLocator.Get<ITestService>();

            // Assert
            Assert.IsNotNull(retrieved, "Should retrieve registered service");
            Assert.AreEqual("test_value", retrieved.GetValue(), "Should retrieve correct service instance");
        }

        [Test]
        public void Register_Null_DoesNotThrow()
        {
            // Act & Assert - should not throw
            Assert.DoesNotThrow(() => ServiceLocator.Register<ITestService>(null),
                "Registering null should not throw");
        }

        [Test]
        public void Register_SameTypeTwice_OverwritesPrevious()
        {
            // Arrange
            var service1 = new TestServiceImpl("first");
            var service2 = new TestServiceImpl("second");

            // Act
            ServiceLocator.Register<ITestService>(service1);
            ServiceLocator.Register<ITestService>(service2);
            var retrieved = ServiceLocator.Get<ITestService>();

            // Assert
            Assert.AreEqual("second", retrieved.GetValue(), "Should use the most recently registered service");
        }

        [Test]
        public void Register_DifferentTypes_AllRetrievable()
        {
            // Arrange
            var service1 = new TestServiceImpl("test");
            var service2 = new AnotherTestService { Counter = 42 };

            // Act
            ServiceLocator.Register<ITestService>(service1);
            ServiceLocator.Register(service2);

            // Assert
            Assert.IsNotNull(ServiceLocator.Get<ITestService>(), "First service should be retrievable");
            Assert.IsNotNull(ServiceLocator.Get<AnotherTestService>(), "Second service should be retrievable");
            Assert.AreEqual(42, ServiceLocator.Get<AnotherTestService>().Counter);
        }

        #endregion

        #region Get Tests

        [Test]
        public void Get_UnregisteredService_ReturnsNull()
        {
            // Act
            var result = ServiceLocator.Get<ITestService>();

            // Assert
            Assert.IsNull(result, "Should return null for unregistered service");
        }

        [Test]
        public void Get_AfterClear_ReturnsNull()
        {
            // Arrange
            ServiceLocator.Register<ITestService>(new TestServiceImpl());

            // Act
            ServiceLocator.Clear();
            var result = ServiceLocator.Get<ITestService>();

            // Assert
            Assert.IsNull(result, "Should return null after Clear()");
        }

        #endregion

        #region TryGet Tests

        [Test]
        public void TryGet_RegisteredService_ReturnsTrue()
        {
            // Arrange
            ServiceLocator.Register<ITestService>(new TestServiceImpl("test"));

            // Act
            bool found = ServiceLocator.TryGet<ITestService>(out var service);

            // Assert
            Assert.IsTrue(found, "TryGet should return true for registered service");
            Assert.IsNotNull(service, "Output parameter should be set");
            Assert.AreEqual("test", service.GetValue());
        }

        [Test]
        public void TryGet_UnregisteredService_ReturnsFalse()
        {
            // Act
            bool found = ServiceLocator.TryGet<ITestService>(out var service);

            // Assert
            Assert.IsFalse(found, "TryGet should return false for unregistered service");
            Assert.IsNull(service, "Output parameter should be null");
        }

        #endregion

        #region Unregister Tests

        [Test]
        public void Unregister_RegisteredService_RemovesIt()
        {
            // Arrange
            ServiceLocator.Register<ITestService>(new TestServiceImpl());

            // Act
            ServiceLocator.Unregister<ITestService>();
            var result = ServiceLocator.Get<ITestService>();

            // Assert
            Assert.IsNull(result, "Service should be removed after Unregister");
        }

        [Test]
        public void Unregister_UnregisteredService_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => ServiceLocator.Unregister<ITestService>(),
                "Unregistering non-existent service should not throw");
        }

        #endregion

        #region IsRegistered Tests

        [Test]
        public void IsRegistered_RegisteredService_ReturnsTrue()
        {
            // Arrange
            ServiceLocator.Register<ITestService>(new TestServiceImpl());

            // Act & Assert
            Assert.IsTrue(ServiceLocator.IsRegistered<ITestService>(),
                "IsRegistered should return true for registered service");
        }

        [Test]
        public void IsRegistered_UnregisteredService_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(ServiceLocator.IsRegistered<ITestService>(),
                "IsRegistered should return false for unregistered service");
        }

        #endregion

        #region Factory Tests

        [Test]
        public void RegisterFactory_LazyInstantiation_CreatesOnFirstGet()
        {
            // Arrange
            int factoryCalls = 0;
            ServiceLocator.RegisterFactory<ITestService>(() =>
            {
                factoryCalls++;
                return new TestServiceImpl("factory_created");
            });

            // Assert - factory not called yet
            Assert.AreEqual(0, factoryCalls, "Factory should not be called before Get");

            // Act
            var service = ServiceLocator.Get<ITestService>();

            // Assert
            Assert.AreEqual(1, factoryCalls, "Factory should be called once on first Get");
            Assert.AreEqual("factory_created", service.GetValue());
        }

        [Test]
        public void RegisterFactory_MultipleGets_OnlyCreatesOnce()
        {
            // Arrange
            int factoryCalls = 0;
            ServiceLocator.RegisterFactory<ITestService>(() =>
            {
                factoryCalls++;
                return new TestServiceImpl();
            });

            // Act
            ServiceLocator.Get<ITestService>();
            ServiceLocator.Get<ITestService>();
            ServiceLocator.Get<ITestService>();

            // Assert
            Assert.AreEqual(1, factoryCalls, "Factory should only be called once");
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_RemovesAllServices()
        {
            // Arrange
            ServiceLocator.Register<ITestService>(new TestServiceImpl());
            ServiceLocator.Register(new AnotherTestService());

            // Act
            ServiceLocator.Clear();

            // Assert
            Assert.IsNull(ServiceLocator.Get<ITestService>(), "First service should be cleared");
            Assert.IsNull(ServiceLocator.Get<AnotherTestService>(), "Second service should be cleared");
        }

        #endregion
    }
}
