# Apex Citadels - Test Framework

This directory contains the unit and integration tests for Apex Citadels.

## Structure

```
Tests/
├── Editor/                      # EditMode tests (run in editor without play mode)
│   ├── ApexCitadels.Tests.Editor.asmdef
│   ├── TestBase.cs              # Base class with common utilities
│   ├── Alliance/
│   │   └── AllianceTests.cs     # Alliance data model tests
│   ├── Core/
│   │   └── ServiceLocatorTests.cs
│   └── Territory/
│       └── TerritoryTests.cs    # Territory data model & GPS tests
│
└── Runtime/                     # PlayMode tests (run during play mode)
    ├── ApexCitadels.Tests.Runtime.asmdef
    ├── SmokeTests.cs            # Basic Unity integration tests
    └── Core/
        └── ServiceLocatorPlayModeTests.cs
```

## Running Tests

### In Unity Editor

1. Open **Window > General > Test Runner**
2. Select **EditMode** or **PlayMode** tab
3. Click **Run All** or select specific tests

### Command Line (CI/CD)

```bash
# Run EditMode tests
unity -runTests -testPlatform editmode -projectPath . -testResults results.xml

# Run PlayMode tests
unity -runTests -testPlatform playmode -projectPath . -testResults results.xml
```

## Test Categories

### EditMode Tests (Fast, No Play Mode Required)
- **Data Model Tests**: Territory, Alliance, Player, etc.
- **Utility Tests**: ServiceLocator, GPS calculations, etc.
- **Pure Logic Tests**: Any code that doesn't require MonoBehaviour lifecycle

### PlayMode Tests (Slower, Requires Play Mode)
- **Integration Tests**: Scene loading, component interactions
- **MonoBehaviour Tests**: Lifecycle events, coroutines
- **System Tests**: Full gameplay loops

## Writing New Tests

### EditMode Test Template

```csharp
using NUnit.Framework;
using ApexCitadels.Tests.Editor;

namespace ApexCitadels.Tests.Editor.MyFeature
{
    [TestFixture]
    public class MyFeatureTests : TestBase
    {
        [Test]
        public void MyMethod_WhenCondition_ShouldResult()
        {
            // Arrange
            var sut = new MyClass();
            
            // Act
            var result = sut.MyMethod();
            
            // Assert
            Assert.AreEqual(expected, result);
        }
    }
}
```

### PlayMode Test Template

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace ApexCitadels.Tests.Runtime.MyFeature
{
    [TestFixture]
    public class MyFeaturePlayModeTests
    {
        [UnityTest]
        public IEnumerator MyMethod_WhenCondition_ShouldResult()
        {
            // Arrange
            var go = new GameObject();
            var component = go.AddComponent<MyComponent>();
            
            yield return null; // Wait one frame
            
            // Assert
            Assert.IsNotNull(component);
            
            // Cleanup
            Object.Destroy(go);
        }
    }
}
```

## TestBase Utilities

The `TestBase` class provides:

- `CreateTestGameObject(name)` - Auto-cleanup GameObjects
- `CreateTestComponent<T>()` - Create component with auto-cleanup
- `AssertApproximately(expected, actual)` - Float/double comparison
- `AssertVectorsApproximatelyEqual(expected, actual)` - Vector3 comparison
- `RandomString(length)` / `RandomId()` - Test data generation
- `TestLocations` - Pre-defined GPS coordinates for testing
- `CreateMockPlayer()` - Mock player data

## Test Coverage Goals

| System | Target Coverage | Priority |
|--------|----------------|----------|
| Territory (GPS/Distance) | 90%+ | HIGH |
| Alliance (Permissions) | 85%+ | HIGH |
| ServiceLocator | 95%+ | HIGH |
| Combat calculations | 80%+ | MEDIUM |
| Economy/Resources | 80%+ | MEDIUM |
| UI Panels | 50%+ | LOW |

## Best Practices

1. **Name tests clearly**: `MethodName_Scenario_ExpectedResult`
2. **One assertion per test** (when practical)
3. **Use TestBase** for common setup/teardown
4. **Mock external dependencies** (Firebase, etc.)
5. **Keep tests fast** - EditMode tests should run in milliseconds
6. **Test edge cases** - null inputs, empty lists, boundary values
