using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ApexCitadels.Tests.Editor
{
    /// <summary>
    /// Base class for all Apex Citadels Editor tests.
    /// Provides common utilities, mocking helpers, and setup/teardown.
    /// </summary>
    public abstract class TestBase
    {
        protected List<GameObject> _createdGameObjects = new List<GameObject>();
        protected List<IDisposable> _disposables = new List<IDisposable>();

        [SetUp]
        public virtual void SetUp()
        {
            _createdGameObjects.Clear();
            _disposables.Clear();
            
            // Suppress expected log messages during tests
            LogAssert.ignoreFailingMessages = false;
        }

        [TearDown]
        public virtual void TearDown()
        {
            // Clean up created GameObjects
            foreach (var go in _createdGameObjects)
            {
                if (go != null)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
            _createdGameObjects.Clear();

            // Clean up disposables
            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }
            _disposables.Clear();
        }

        #region Helper Methods

        /// <summary>
        /// Create a GameObject that will be automatically cleaned up after the test
        /// </summary>
        protected GameObject CreateTestGameObject(string name = "TestObject")
        {
            var go = new GameObject(name);
            _createdGameObjects.Add(go);
            return go;
        }

        /// <summary>
        /// Create a GameObject with a specific component attached
        /// </summary>
        protected T CreateTestComponent<T>(string name = null) where T : Component
        {
            var go = CreateTestGameObject(name ?? typeof(T).Name);
            return go.AddComponent<T>();
        }

        /// <summary>
        /// Assert that a value is within a tolerance of an expected value
        /// </summary>
        protected void AssertApproximately(float expected, float actual, float tolerance = 0.001f, string message = null)
        {
            Assert.That(actual, Is.EqualTo(expected).Within(tolerance), message);
        }

        /// <summary>
        /// Assert that a value is within a tolerance of an expected value (double precision)
        /// </summary>
        protected void AssertApproximately(double expected, double actual, double tolerance = 0.0001, string message = null)
        {
            Assert.That(actual, Is.EqualTo(expected).Within(tolerance), message);
        }

        /// <summary>
        /// Assert that two vectors are approximately equal
        /// </summary>
        protected void AssertVectorsApproximatelyEqual(Vector3 expected, Vector3 actual, float tolerance = 0.001f)
        {
            AssertApproximately(expected.x, actual.x, tolerance, "X component mismatch");
            AssertApproximately(expected.y, actual.y, tolerance, "Y component mismatch");
            AssertApproximately(expected.z, actual.z, tolerance, "Z component mismatch");
        }

        /// <summary>
        /// Register a disposable to be cleaned up after the test
        /// </summary>
        protected void RegisterDisposable(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        /// <summary>
        /// Generate a random string for test data
        /// </summary>
        protected string RandomString(int length = 8)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new System.Random();
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }
            return new string(result);
        }

        /// <summary>
        /// Generate a random GUID string
        /// </summary>
        protected string RandomId() => Guid.NewGuid().ToString();

        #endregion

        #region GPS/Location Helpers

        /// <summary>
        /// Sample GPS coordinates for testing (Vienna, VA area)
        /// </summary>
        protected static class TestLocations
        {
            public static readonly (double Lat, double Lon) ViennaCenter = (38.9012, -77.2654);
            public static readonly (double Lat, double Lon) ViennaPark = (38.9045, -77.2612);
            public static readonly (double Lat, double Lon) ViennaStation = (38.8997, -77.2711);
            
            // San Francisco test locations
            public static readonly (double Lat, double Lon) SFDowntown = (37.7749, -122.4194);
            public static readonly (double Lat, double Lon) GoldenGate = (37.8199, -122.4783);
            
            // Points that are ~100m apart
            public static readonly (double Lat, double Lon) Point100mA = (38.9012, -77.2654);
            public static readonly (double Lat, double Lon) Point100mB = (38.9021, -77.2654); // ~100m north
            
            // Points that are ~10m apart
            public static readonly (double Lat, double Lon) Point10mA = (38.9012, -77.2654);
            public static readonly (double Lat, double Lon) Point10mB = (38.90129, -77.2654); // ~10m north
        }

        #endregion

        #region Mock Data Generators

        /// <summary>
        /// Create mock player data for testing
        /// </summary>
        protected MockPlayer CreateMockPlayer(string name = null, int level = 1)
        {
            return new MockPlayer
            {
                Id = RandomId(),
                Name = name ?? $"TestPlayer_{RandomString(4)}",
                Level = level,
                Gems = 1000,
                Gold = 5000
            };
        }

        /// <summary>
        /// Simple mock player for testing
        /// </summary>
        protected class MockPlayer
        {
            public string Id;
            public string Name;
            public int Level;
            public int Gems;
            public int Gold;
        }

        #endregion
    }
}
