# DOTS Test Execution Guide with UniTask

## 🚨 MANDATORY - DOTSTestBase Inheritance

> **CRITICAL REQUIREMENT**: ALL DOTS tests MUST inherit from `DOTSTestBase`. NEVER create DOTS tests from scratch!

### ✅ REQUIRED Pattern
```csharp
using PerSpec.Runtime.DOTS.Core;
using NUnit.Framework;

[TestFixture]
public class MyDOTSTest : DOTSTestBase  // MANDATORY - Use PerSpec base class
{
    // Your tests here
}
```

### ❌ FORBIDDEN Patterns
```csharp
// NEVER do this - Missing base class functionality
[TestFixture]
public class BadDOTSTest
{
    private World testWorld; // Don't create your own!
    private EntityManager entityManager; // This is provided!
}

// NEVER do this - Wrong base class
[TestFixture]
public class BadDOTSTest : TestFixture
{
    // Missing DOTS-specific setup and teardown
}

// NEVER do this - Manual world management
[TestFixture] 
public class BadDOTSTest
{
    [SetUp]
    public void Setup()
    {
        // Don't manually create worlds, systems, or managers
        var world = new World("Test");
    }
}
```

### Why DOTSTestBase is Mandatory
- **World Management**: Automatically creates isolated test worlds
- **DefaultGameObjectInjectionWorld**: Automatically sets test world as default (NEW)
- **Entity Manager**: Pre-configured with proper lifecycle
- **UniTask Integration**: Zero-allocation async testing support
- **Memory Leak Detection**: Automatic native collection tracking
- **Cancellation Tokens**: Built-in timeout and cleanup support
- **Performance Profiling**: DOTS-specific measurement helpers

### Required Assembly References
Your `.asmdef` must reference these assemblies:
```json
{
    "references": [
        "PerSpec.Runtime.DOTS",
        "PerSpec.Runtime.Unity", 
        "PerSpec.Runtime.Debug",      // Required for PerSpecDebug logging
        "UniTask",
        "Unity.Entities",
        "Unity.Transforms",
        "UnityEngine.TestRunner"
    ]
}
```

### 📝 Required Namespaces and Imports

Every DOTS test file MUST include these using statements:

```csharp
// MANDATORY for ALL DOTS tests
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using Cysharp.Threading.Tasks;
using PerSpec;                        // Core PerSpec utilities
using PerSpec.Runtime.DOTS;           // DOTS testing infrastructure
using PerSpec.Runtime.DOTS.Core;      // DOTSTestBase
using PerSpec.Runtime.DOTS.Helpers;   // DOTS test helpers
using Unity.Entities;                 // Entity, EntityManager, World
using Unity.Transforms;               // Translation, Rotation, Scale
using Unity.Mathematics;              // float3, quaternion, etc.

// Standard namespace pattern
namespace YourProject.Tests.PlayMode.DOTS  // or .EditMode.DOTS
{
    [TestFixture]
    public class YourDOTSTestClass : DOTSTestBase  // MANDATORY
    {
        // Tests here
    }
}
```

### 🎯 Test Facade Pattern for DOTS Production Code  

> **IMPORTANT**: Add PUBLIC test methods in DOTS production classes, wrapped in #if UNITY_EDITOR, to test private ECS functionality.

#### ✅ REQUIRED Pattern for DOTS Systems:
```csharp
// Production DOTS System
public partial class MovementSystem : SystemBase
{
    // Private production implementation
    private EntityQuery movableQuery;
    private float deltaTime;
    
    protected override void OnCreate()
    {
        movableQuery = GetEntityQuery(typeof(Translation), typeof(MovementData));
    }
    
    protected override void OnUpdate()
    {
        deltaTime = Time.DeltaTime;
        ProcessMovement();
    }
    
    private void ProcessMovement()
    {
        Entities
            .WithEntityQueryOptions(EntityQueryOptions.FilterWriteGroup)
            .ForEach((ref Translation translation, in MovementData movement) =>
            {
                translation.Value += movement.Direction * movement.Speed * deltaTime;
            }).ScheduleParallel();
    }
    
    private void ValidateEntities()
    {
        // Internal validation logic
        var entityCount = movableQuery.CalculateEntityCount();
        if (entityCount > 1000)
            UnityEngine.Debug.LogWarning($"High entity count: {entityCount}");
    }
    
    #if UNITY_EDITOR
    // Test facade methods - Only exist in Editor builds
    public void Test_ForceUpdate(float testDeltaTime)
    {
        // Orchestrates private system update for testing
        deltaTime = testDeltaTime;
        ProcessMovement();
        ValidateEntities();
    }
    
    public void Test_ProcessSingleFrame()
    {
        // Simulates one complete frame update
        deltaTime = Time.DeltaTime;
        ProcessMovement();
        CompleteAllJobs();
    }
    
    public int Test_GetMovableEntityCount() => movableQuery.CalculateEntityCount();
    public EntityQuery Test_GetMovableQuery() => movableQuery;
    #endif
}

// Test using DOTS facades
[UnityTest]
public IEnumerator Should_UpdateEntityPositions_WhenSystemRuns() => RunAsyncTest(async () => 
{
    // Create test entities
    var entities = await CreateTestEntitiesAsync(10, typeof(Translation), typeof(MovementData));
    
    // Get system instance
    var movementSystem = testWorld.GetExistingSystem<MovementSystem>();
    
    // Use test facade to trigger private system behavior
    movementSystem.Test_ForceUpdate(0.016f); // 16ms frame
    
    await WaitForFramesAsync(1);
    
    // Verify system processed entities
    Assert.AreEqual(10, movementSystem.Test_GetMovableEntityCount());
    
    entities.Dispose();
});
```

#### ❌ FORBIDDEN DOTS Patterns:
- Don't use reflection: `GetField("movableQuery", BindingFlags.NonPublic)`
- Don't expose private system internals as public
- Don't put #if UNITY_EDITOR in test code
- Don't modify OnUpdate() signature for testing

## Overview

This guide provides comprehensive documentation for testing Unity DOTS (Data-Oriented Technology Stack) applications using modern async/await patterns with UniTask. The DOTS test infrastructure extends the Unity Test Framework with specialized support for Entity Component System (ECS) testing, Burst compilation validation, and Job System testing - all with zero-allocation async operations.

### 🔄 Important: DefaultGameObjectInjectionWorld Handling (v2.0+)

As of PerSpec v2.0, `DOTSTestBase` automatically manages `World.DefaultGameObjectInjectionWorld`:

- **Automatic Setup**: Test world is set as `DefaultGameObjectInjectionWorld` by default
- **Compatibility**: Code expecting the default world now works in tests
- **Cleanup**: Properly cleared between tests to prevent contamination
- **Override**: Set `protected override bool SetAsDefaultWorld => false;` to disable

This solves the common issue where `World.DefaultGameObjectInjectionWorld` was null in tests, causing failures in production code that relied on it.

## Prefab-Based DOTS Testing (Standard Approach)

> **IMPORTANT**: Like all Unity testing, DOTS tests should use the Prefab Pattern for any non-trivial scenarios.

### When to Use DOTS Prefab Pattern
- **Entity Archetypes** - Define complex entity configurations
- **System Testing** - Test systems with pre-configured worlds
- **Hybrid Components** - Mix GameObjects with entities
- **Conversion Testing** - Test GameObject-to-Entity conversion
- **Scene Testing** - Test subscenes and entity scenes

### DOTS Prefab Factory Example

```csharp
using UnityEngine;
using UnityEditor;
using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using PerSpec;

namespace YourProject.Tests.Editor.DOTSFactories  // Example namespace
{
    public static class DOTSCharacterPrefabFactory
    {
        private const string PREFAB_PATH = "Assets/Resources/TestPrefabs/DOTSCharacter.prefab";
        
        [MenuItem("TestFramework/DOTS/Create Character Prefab")]
        public static void CreateCharacterPrefab()
        {
            var prefabRoot = new GameObject("DOTSCharacter");
            
            try
            {
                // Add conversion components
                var convertToEntity = prefabRoot.AddComponent<ConvertToEntity>();
                convertToEntity.ConversionMode = ConvertToEntity.Mode.ConvertAndInjectGameObject;
                
                // Add hybrid components for testing
                SetupRenderMesh(prefabRoot);
                SetupPhysics(prefabRoot);
                SetupCustomComponents(prefabRoot);
                
                // Save prefab
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PREFAB_PATH);
                PerSpecDebug.Log($"[DOTS] Created test prefab at: {PREFAB_PATH}");
            }
            finally
            {
                Object.DestroyImmediate(prefabRoot);
            }
        }
        
        private static void SetupRenderMesh(GameObject root)
        {
            var meshFilter = root.AddComponent<MeshFilter>();
            meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            
            var meshRenderer = root.AddComponent<MeshRenderer>();
            meshRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }
        
        private static void SetupCustomComponents(GameObject root)
        {
            // Add authoring components that convert to entities
            root.AddComponent<HealthAuthoring>();
            root.AddComponent<MovementAuthoring>();
            root.AddComponent<WeaponAuthoring>();
        }
    }
}
```

### Testing with DOTS Prefabs

```csharp
[TestFixture]
public class DOTSCharacterTests : DOTSTestBase
{
    private GameObject characterPrefab;
    private Entity characterEntity;
    
    [SetUp]
    public override void Setup()
    {
        base.Setup();
        
        // Load DOTS prefab
        characterPrefab = Resources.Load<GameObject>("TestPrefabs/DOTSCharacter");
        Assert.IsNotNull(characterPrefab, "Run 'TestFramework/DOTS/Create Character Prefab' first");
        
        // Convert to entity
        var instance = Object.Instantiate(characterPrefab);
        GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(testWorld, null);
        characterEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(instance, settings);
        Object.DestroyImmediate(instance);
    }
    
    [UnityTest]
    public IEnumerator Should_Have_Expected_Components() => RunAsyncTest(async () =>
    {
        // Verify entity has expected components from prefab
        Assert.IsTrue(entityManager.HasComponent<Translation>(characterEntity));
        Assert.IsTrue(entityManager.HasComponent<Health>(characterEntity));
        Assert.IsTrue(entityManager.HasComponent<Movement>(characterEntity));
        
        await UniTask.Yield();
    });
}
```

## DOTS Test Infrastructure Architecture

### Example Directory Structure (Prefab-First)

> **Note**: This is a suggested structure for organizing your DOTS tests. Your actual paths may vary.

```
Assets/Tests/DOTS/                  # Example test location
├── Editor/
│   └── PrefabFactories/            # DOTS prefab factories
│       ├── EntityPrefabFactory.cs
│       └── SystemPrefabFactory.cs
├── PlayMode/                       # DOTS PlayMode tests
│   ├── EntityTests.cs
│   └── SystemTests.cs
└── EditMode/                       # Pure ECS logic tests (rare)
    └── ComponentDataTests.cs
    
Resources/
└── TestPrefabs/
    └── DOTS/                       # DOTS test prefabs
        ├── TestEntity.prefab
        └── TestSystem.prefab
```

### Assembly Definitions

The DOTS test infrastructure uses the following assembly structure:

1. **PerSpec.Runtime.DOTS** - DOTS test infrastructure with UniTask
2. **PerSpec.Runtime.Unity** - Base Unity test framework with UniTask
3. **UniTask** - Zero-allocation async/await library

## World Management Features (NEW)

`DOTSTestBase` provides several helpers for world management:

```csharp
[TestFixture]
public class MyDOTSTest : DOTSTestBase
{
    // Control whether test world becomes DefaultGameObjectInjectionWorld
    protected override bool SetAsDefaultWorld => true; // Default is true
    
    [UnityTest]
    public IEnumerator TestWithDefaultWorld() => RunAsyncTest(async () =>
    {
        // Method 1: Automatic (if SetAsDefaultWorld is true)
        Assert.IsNotNull(World.DefaultGameObjectInjectionWorld);
        Assert.AreEqual(testWorld, World.DefaultGameObjectInjectionWorld);
        
        // Method 2: Manual control
        EnsureDefaultWorldIsSet(); // Manually set if needed
        
        // Method 3: Get appropriate world
        var world = GetWorldForTesting(); // Returns test or default world
        
        // Method 4: Get or create systems
        var mySystem = GetOrCreateSystem<MyTestSystem>();
        
        await UniTask.Yield();
    });
}
```

### Helper Methods

- **`GetWorldForTesting()`**: Returns the test world or default world
- **`EnsureDefaultWorldIsSet()`**: Manually sets test world as default
- **`GetOrCreateSystem<T>()`**: Gets or creates a system in the test world

## Using DOTSTestBase with UniTask

All DOTS tests should inherit from `DOTSTestBase` and use UniTask patterns:

```csharp
using PerSpec.Runtime.DOTS.Core;
using PerSpec.Runtime.DOTS.Helpers;
using Unity.Entities;
using NUnit.Framework;
using Cysharp.Threading.Tasks;
using System.Collections;
using PerSpec;

[TestFixture]
public class MyDOTSTest : DOTSTestBase
{
    #region Setup and Teardown
    
    [SetUp]
    public override void Setup()
    {
        base.Setup(); // Creates test world, entity manager, and cancellation token
    }
    
    [TearDown]
    public override void Teardown()
    {
        base.Teardown(); // Disposes world and cancellation token
    }
    
    #endregion
    
    #region Tests
    
    [UnityTest]
    public IEnumerator TestEntityCreation() => RunAsyncTest(async () =>
    {
        // Create test entity asynchronously
        var entity = await CreateTestEntityAsync(
            typeof(Translation),
            typeof(Rotation)
        );
        
        // Validate entity
        var isValid = await ValidateEntityAsync(entity, e => 
            entityManager.HasComponent<Translation>(e) &&
            entityManager.HasComponent<Rotation>(e)
        );
        
        Assert.IsTrue(isValid, "Entity should have required components");
    });
    
    #endregion
}
```

## UniTask Async Patterns for DOTS

### Async Entity Operations

```csharp
[UnityTest]
public IEnumerator TestAsyncEntityOperations() => RunAsyncTest(async () =>
{
    // Create single entity
    var entity = await CreateTestEntityAsync(typeof(Translation));
    
    // Create multiple entities
    var entities = await CreateTestEntitiesAsync(100, 
        typeof(Translation), 
        typeof(Rotation)
    );
    
    try
    {
        // Process entities asynchronously
        await ProcessEntitiesAsync(entities);
        
        // Wait for frames
        await WaitForFramesAsync(5, testCancellationTokenSource.Token);
        
        // Validate all entities
        foreach (var e in entities)
        {
            var valid = await ValidateEntityAsync(e, ValidateEntity);
            Assert.IsTrue(valid);
        }
    }
    finally
    {
        // Cleanup
        entities.Dispose();
    }
});

private async UniTask ProcessEntitiesAsync(NativeArray<Entity> entities)
{
    await UniTask.SwitchToMainThread();
    
    foreach (var entity in entities)
    {
        entityManager.SetComponentData(entity, new Translation 
        { 
            Value = UnityEngine.Random.insideUnitSphere 
        });
    }
}
```

### Async Job Testing

```csharp
[BurstCompile]
struct TestJob : IJob
{
    public NativeArray<float> data;
    
    public void Execute()
    {
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = data[i] * 2.0f;
        }
    }
}

[UnityTest]
public IEnumerator TestJobWithUniTask() => RunAsyncTest(async () =>
{
    var data = new NativeArray<float>(1024, Allocator.TempJob);
    
    try
    {
        // Initialize data
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = i;
        }
        
        // Schedule and wait for job
        var job = new TestJob { data = data };
        var handle = job.Schedule();
        
        // Wait for job completion asynchronously
        await WaitForJobAsync(handle, testCancellationTokenSource.Token);
        
        // Validate results
        for (int i = 0; i < data.Length; i++)
        {
            Assert.AreEqual(i * 2.0f, data[i]);
        }
    }
    finally
    {
        if (data.IsCreated)
            data.Dispose();
    }
});
```

### Async System Testing

```csharp
[UnityTest]
public IEnumerator TestSystemUpdateAsync() => RunAsyncTest(async () =>
{
    // Create test entities
    var entities = await CreateTestEntitiesAsync(100, typeof(Translation));
    
    try
    {
        // Measure system update performance
        var elapsed = await MeasureDOTSOperationAsync(async () =>
        {
            await WaitForSystemUpdateAsync<MyTestSystem>(testCancellationTokenSource.Token);
        }, "MyTestSystem Update");
        
        // Validate performance
        Assert.Less(elapsed, 0.01667f, "System should update within frame time");
        
        // Wait for multiple frames
        await WaitForFramesAsync(10);
        
        // Validate system effects
        foreach (var entity in entities)
        {
            var translation = entityManager.GetComponentData<Translation>(entity);
            Assert.AreNotEqual(float3.zero, translation.Value);
        }
    }
    finally
    {
        entities.Dispose();
    }
});
```

## Test Configuration

Use `DOTSTestConfiguration` for consistent test setups:

```csharp
// Basic test configuration
var config = DOTSTestConfiguration.CreateDefault();
config.TestEntityCount = 1000;
config.EnableDiagnostics = true;

// Performance test configuration
var perfConfig = DOTSTestConfiguration.CreatePerformanceTest();
perfConfig.TestEntityCount = 10000;
perfConfig.UseJobSystem = true;
perfConfig.EnableBurstCompilation = true;

// Stress test configuration
var stressConfig = DOTSTestConfiguration.CreateStressTest();
stressConfig.TestUpdateInterval = 0.008f; // 120 FPS target
```

## Test Factory with Async Support

```csharp
[UnityTest]
public IEnumerator TestWithFactory() => RunAsyncTest(async () =>
{
    using (var factory = new DOTSTestFactory())
    {
        // Create test setups asynchronously
        await UniTask.SwitchToMainThread();
        
        var config = DOTSTestConfiguration.CreateDefault();
        var testSetup = DOTSTestFactory.CreateBasicTestSetup(config);
        
        // Create test entities
        var entity = DOTSTestFactory.CreateTestEntity(entityManager, config);
        
        // Wait for initialization
        await UniTask.Delay(100);
        
        // Validate setup
        Assert.IsNotNull(testSetup);
        Assert.IsTrue(entityManager.Exists(entity));
        
        // Factory automatically cleans up on disposal
    }
});
```

## Performance Testing with UniTask

```csharp
[TestFixture]
public class PerformanceTests : DOTSTestBase
{
    [UnityTest]
    public IEnumerator TestEntityCreationPerformance() => RunAsyncTest(async () =>
    {
        var config = DOTSTestConfiguration.CreatePerformanceTest();
        
        // Measure entity creation performance
        var elapsed = await MeasureDOTSOperationAsync(async () =>
        {
            for (int i = 0; i < config.TestEntityCount; i++)
            {
                await CreateTestEntityAsync(typeof(Translation), typeof(Rotation));
                
                // Yield periodically to avoid blocking
                if (i % 100 == 0)
                    await UniTask.Yield();
            }
        }, "Entity Creation");
        
        var entitiesPerMs = config.TestEntityCount / (elapsed * 1000);
        PerSpecDebug.Log($"[PERFORMANCE] Created {entitiesPerMs:F2} entities/ms");
        
        Assert.Greater(entitiesPerMs, 100, "Should create >100 entities per ms");
    });
    
    [UnityTest]
    public IEnumerator TestJobPerformance() => RunAsyncTest(async () =>
    {
        var data = new NativeArray<float>(1_000_000, Allocator.TempJob);
        
        try
        {
            // Benchmark job execution
            var avgTime = await UniTaskTestHelpers.BenchmarkAsync(async () =>
            {
                var job = new TestJob { data = data };
                var handle = job.Schedule();
                await WaitForJobAsync(handle);
            }, iterations: 10, warmupIterations: 2);
            
            PerSpecDebug.Log($"[PERFORMANCE] Job execution: {avgTime * 1000:F2}ms average");
            Assert.Less(avgTime, 0.01f, "Job should complete within 10ms");
        }
        finally
        {
            data.Dispose();
        }
    });
}
```

## Memory Management with UniTask

```csharp
[UnityTest]
public IEnumerator TestMemoryManagement() => RunAsyncTest(async () =>
{
    // Ensure no memory leaks
    AssertNoMemoryLeaks();
    
    // Profile memory allocation
    var allocated = await ProfileDOTSMemoryAsync(async () =>
    {
        var entities = await CreateTestEntitiesAsync(1000, typeof(Translation));
        
        // Process entities
        await UniTask.Delay(100);
        
        // Clean up
        entityManager.DestroyEntity(entities);
        entities.Dispose();
    });
    
    PerSpecDebug.Log($"[MEMORY] Total allocated: {allocated:N0} bytes");
    Assert.Less(allocated, 1_000_000, "Should allocate less than 1MB");
});
```

## Dynamic Buffer Testing

```csharp
[UnityTest]
public IEnumerator TestDynamicBuffer() => RunAsyncTest(async () =>
{
    var entity = await CreateTestEntityAsync();
    var buffer = entityManager.AddBuffer<MyBufferElement>(entity);
    
    // Add elements asynchronously
    await UniTask.RunOnThreadPool(() =>
    {
        // Prepare data on thread pool
        for (int i = 0; i < 100; i++)
        {
            // Heavy computation here
        }
    });
    
    await UniTask.SwitchToMainThread();
    
    // Add to buffer on main thread
    for (int i = 0; i < 100; i++)
    {
        buffer.Add(new MyBufferElement { Value = i });
    }
    
    // Validate buffer
    Assert.AreEqual(100, buffer.Length);
    
    // Measure buffer operations
    var elapsed = await MeasureDOTSOperationAsync(async () =>
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            var value = buffer[i].Value;
        }
        await UniTask.Yield();
    }, "Buffer Read");
    
    Assert.Less(elapsed, 0.001f, "Buffer read should be fast");
});
```

## Best Practices

### 1. Always Use UniTask for Async Operations
```csharp
// Good - Uses UniTask
[UnityTest]
public IEnumerator MyTest() => RunAsyncTest(async () =>
{
    await UniTask.Delay(100);
    // Test logic
});

// Bad - Uses coroutines
[UnityTest]
public IEnumerator MyTest()
{
    yield return new WaitForSeconds(0.1f);
    // Test logic
}
```

### 2. Proper Resource Cleanup
```csharp
[UnityTest]
public IEnumerator TestWithCleanup() => RunAsyncTest(async () =>
{
    NativeArray<float> data = default;
    try
    {
        data = new NativeArray<float>(1024, Allocator.TempJob);
        // Use data
        await ProcessDataAsync(data);
    }
    finally
    {
        if (data.IsCreated)
            data.Dispose();
    }
});
```

### 3. Use Cancellation Tokens
```csharp
[UnityTest]
public IEnumerator TestWithCancellation() => RunAsyncTest(async () =>
{
    var cts = CancellationTokenSource.CreateLinkedTokenSource(
        testCancellationTokenSource.Token
    );
    cts.CancelAfter(5000); // 5 second timeout
    
    try
    {
        await LongRunningOperationAsync(cts.Token);
    }
    catch (OperationCanceledException)
    {
        // Handle cancellation gracefully
        PerSpecDebug.Log("Operation cancelled");
    }
});
```

### 4. Parallel Operations
```csharp
[UnityTest]
public IEnumerator TestParallelOperations() => RunAsyncTest(async () =>
{
    // Run operations in parallel
    var tasks = new[]
    {
        CreateTestEntityAsync(typeof(Translation)),
        CreateTestEntityAsync(typeof(Rotation)),
        CreateTestEntityAsync(typeof(Scale))
    };
    
    var entities = await UniTask.WhenAll(tasks);
    
    // All entities created in parallel
    foreach (var entity in entities)
    {
        Assert.IsTrue(entityManager.Exists(entity));
    }
});
```

## DOTS Test Development Workflow

Follow the **[4-Step Process](../../CLAUDE.md#test-development-workflow)** - REQUIRED for all DOTS development.

### DOTS-Specific Checks

**After Step 2 (Refresh), check for DOTS errors:**
```bash
# Burst compilation errors
python PerSpec/Coordination/Scripts/quick_logs.py errors | grep -i burst

# Native collection leaks
python PerSpec/Coordination/Scripts/quick_logs.py latest -n 50 | grep -i "disposed\|leak"

# Job System issues
python PerSpec/Coordination/Scripts/quick_logs.py latest -n 50 | grep -i "job\|scheduled"
```

**DOTS Test Commands:**
```bash
# Run DOTS category tests
python PerSpec/Coordination/Scripts/quick_test.py category DOTS -p edit --wait

# Run specific DOTS class
python PerSpec/Coordination/Scripts/quick_test.py class DOTSPerformanceTests -p edit --wait
```

### Common DOTS Issues

| Issue | Pattern | Fix |
|-------|---------|-----|
| Burst failed | "Burst compiler failed" | Check [BurstCompile] attributes |
| Native leak | "not been disposed" | Use try-finally with .Dispose() |
| Job safety | "previously scheduled job" | Complete handles before access |
| Entity query | "EntityQuery is invalid" | Match archetype structure |


## Troubleshooting

### DefaultGameObjectInjectionWorld is Null
1. Ensure test inherits from `DOTSTestBase`
2. Check `SetAsDefaultWorld` is not overridden to false
3. Call `EnsureDefaultWorldIsSet()` if needed
4. Use `GetWorldForTesting()` for flexible world access

### Tests Not Running
1. Ensure UniTask is installed via Package Manager
2. Check assembly references include UniTask
3. Verify `RunAsyncTest` or `UniTask.ToCoroutine` is used
4. Check for Burst compilation errors: `python PerSpec/Coordination/Scripts/quick_logs.py errors | grep -i burst`

### Memory Leaks
1. Always dispose NativeArrays/NativeLists
2. Use `finally` blocks for cleanup
3. Enable leak detection: `NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace`

### Async Deadlocks
1. Avoid blocking on main thread
2. Use `UniTask.SwitchToMainThread()` when needed
3. Always use cancellation tokens with timeouts

### Performance Issues
1. Use `UniTask.Yield()` in long loops
2. Profile with `MeasureDOTSOperationAsync`
3. Run CPU-intensive work on thread pool

## Summary

The DOTS Test Framework with UniTask provides:
1. **Zero-allocation async/await** testing
2. **Full cancellation support** with tokens
3. **Thread-safe operations** with thread switching
4. **Performance profiling** built-in
5. **Memory leak detection** for native collections
6. **Parallel test execution** support

For general Unity testing with UniTask, refer to the [Unity Test Guide](unity-test-guide.md).