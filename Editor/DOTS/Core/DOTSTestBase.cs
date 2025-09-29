using System;
using System.Threading;
using NUnit.Framework;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;

namespace PerSpec.Editor.DOTS
{
    /// <summary>
    /// Base class for DOTS tests with UniTask support
    /// Provides helpers for ECS, Jobs, and async operations
    /// </summary>
    public abstract class DOTSTestBase
    {
        #region Fields
        protected World testWorld;
        protected EntityManager entityManager;
        protected string testName;
        protected CancellationTokenSource testCancellationTokenSource;
        
        // Operation tracking for graceful shutdown
        private readonly System.Collections.Generic.HashSet<string> runningOperations = new System.Collections.Generic.HashSet<string>();
        private readonly object operationLock = new object();
        
        // Configuration
        protected virtual int GracefulShutdownTimeoutMs => 5000;
        protected virtual bool EnableOperationTracking => true;
        protected virtual bool LogOperationLifecycle => false;
        
        /// <summary>
        /// When true, sets the test world as World.DefaultGameObjectInjectionWorld
        /// This allows code that expects the default world to work in tests
        /// </summary>
        protected virtual bool SetAsDefaultWorld => true;
        
        #endregion
        
        #region Setup and Teardown
        
        [SetUp]
        public virtual void Setup()
        {
            // Create a test world for ECS
            testWorld = new World("DOTS_Test_World");
            entityManager = testWorld.EntityManager;
            
            // Optionally set as default world for compatibility with code that expects it
            if (SetAsDefaultWorld)
            {
                // Store the existing default world if any (Unity may have created one)
                var existingDefault = World.DefaultGameObjectInjectionWorld;
                if (existingDefault != null && existingDefault != testWorld)
                {
                    Debug.Log($"[DOTS-TEST] Replacing existing DefaultGameObjectInjectionWorld '{existingDefault.Name}' with test world");
                }
                
                World.DefaultGameObjectInjectionWorld = testWorld;
                Debug.Log($"[DOTS-TEST] Set test world as DefaultGameObjectInjectionWorld");
            }
            
            // Initialize cancellation token
            testCancellationTokenSource = new CancellationTokenSource();
            
            // Initialize test name
            testName = TestContext.CurrentContext.Test.Name;
            Debug.Log($"[DOTS-TEST] {testName} starting");
        }
        
        [TearDown]
        public virtual void Teardown()
        {
            Debug.Log($"[DOTS-TEST] {testName} completed");
            
            // Graceful shutdown: wait for operations to complete
            if (EnableOperationTracking)
            {
                WaitForOperationsToComplete();
            }
            
            // Now cancel any remaining operations
            testCancellationTokenSource?.Cancel();
            testCancellationTokenSource?.Dispose();
            
            // Clear default world reference if we set it
            if (World.DefaultGameObjectInjectionWorld == testWorld)
            {
                World.DefaultGameObjectInjectionWorld = null;
                Debug.Log($"[DOTS-TEST] Cleared DefaultGameObjectInjectionWorld reference");
            }
            
            if (testWorld != null && testWorld.IsCreated)
            {
                testWorld.Dispose();
            }
        }
        
        #endregion
        
        #region Operation Tracking
        
        /// <summary>
        /// Waits for all running operations to complete or timeout
        /// </summary>
        private void WaitForOperationsToComplete()
        {
            var startTime = DateTime.Now;
            var timeout = TimeSpan.FromMilliseconds(GracefulShutdownTimeoutMs);
            
            while (true)
            {
                lock (operationLock)
                {
                    if (runningOperations.Count == 0)
                    {
                        Debug.Log($"[DOTS-TEST] All operations completed gracefully");
                        return;
                    }
                    
                    if (DateTime.Now - startTime > timeout)
                    {
                        Debug.LogWarning($"[DOTS-TEST] Timeout waiting for {runningOperations.Count} operations to complete: {string.Join(", ", runningOperations)}");
                        return;
                    }
                }
                
                // Small delay to avoid busy waiting
                System.Threading.Thread.Sleep(10);
            }
        }
        
        /// <summary>
        /// Registers an operation as running
        /// </summary>
        protected string BeginOperation(string operationName)
        {
            if (!EnableOperationTracking) return operationName;
            
            var operationId = $"{operationName}_{Guid.NewGuid():N}";
            lock (operationLock)
            {
                runningOperations.Add(operationId);
                if (LogOperationLifecycle)
                {
                    Debug.Log($"[DOTS-OPERATION] Started: {operationId}");
                }
            }
            return operationId;
        }
        
        /// <summary>
        /// Marks an operation as completed
        /// </summary>
        protected void EndOperation(string operationId)
        {
            if (!EnableOperationTracking) return;
            
            lock (operationLock)
            {
                if (runningOperations.Remove(operationId))
                {
                    if (LogOperationLifecycle)
                    {
                        Debug.Log($"[DOTS-OPERATION] Completed: {operationId}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Runs an operation with automatic tracking
        /// </summary>
        protected async UniTask<T> RunTrackedOperationAsync<T>(string operationName, Func<CancellationToken, UniTask<T>> operation, CancellationToken cancellationToken = default)
        {
            var operationId = BeginOperation(operationName);
            try
            {
                var token = cancellationToken == default ? testCancellationTokenSource.Token : cancellationToken;
                return await operation(token);
            }
            finally
            {
                EndOperation(operationId);
            }
        }
        
        /// <summary>
        /// Runs an operation with automatic tracking (void version)
        /// </summary>
        protected async UniTask RunTrackedOperationAsync(string operationName, Func<CancellationToken, UniTask> operation, CancellationToken cancellationToken = default)
        {
            var operationId = BeginOperation(operationName);
            try
            {
                var token = cancellationToken == default ? testCancellationTokenSource.Token : cancellationToken;
                await operation(token);
            }
            finally
            {
                EndOperation(operationId);
            }
        }
        
        #endregion
        
        #region Entity Creation
        
        protected Entity CreateTestEntity(params ComponentType[] components)
        {
            var entity = entityManager.CreateEntity(components);
            Debug.Log($"[DOTS-ENTITY] Created entity ID={entity.Index} with {components.Length} components");
            return entity;
        }
        
        /// <summary>
        /// Creates an entity asynchronously on the main thread
        /// </summary>
        protected async UniTask<Entity> CreateTestEntityAsync(params ComponentType[] components)
        {
            await UniTask.SwitchToMainThread();
            return CreateTestEntity(components);
        }
        
        /// <summary>
        /// Creates multiple entities asynchronously
        /// </summary>
        protected async UniTask<NativeArray<Entity>> CreateTestEntitiesAsync(int count, params ComponentType[] components)
        {
            await UniTask.SwitchToMainThread();
            
            var entities = new NativeArray<Entity>(count, Allocator.Temp);
            entityManager.CreateEntity(entityManager.CreateArchetype(components), entities);
            
            Debug.Log($"[DOTS-ENTITY] Created {count} entities with {components.Length} components");
            return entities;
        }
        
        #endregion
        
        #region World Management Helpers
        
        /// <summary>
        /// Gets the world to use for testing. Returns the test world or the default world if available.
        /// Useful for tests that need to work with systems that expect DefaultGameObjectInjectionWorld.
        /// </summary>
        protected World GetWorldForTesting()
        {
            if (testWorld != null && testWorld.IsCreated)
                return testWorld;
            
            if (World.DefaultGameObjectInjectionWorld != null && World.DefaultGameObjectInjectionWorld.IsCreated)
                return World.DefaultGameObjectInjectionWorld;
            
            throw new InvalidOperationException("No valid world available for testing");
        }
        
        /// <summary>
        /// Gets or creates a system in the test world
        /// </summary>
        protected T GetOrCreateSystem<T>() where T : SystemBase, new()
        {
            var world = GetWorldForTesting();
            var systemHandle = world.GetExistingSystem<T>();
            
            if (systemHandle == SystemHandle.Null)
            {
                // Create the system if it doesn't exist
                systemHandle = world.CreateSystem<T>();
                Debug.Log($"[DOTS-TEST] Created system {typeof(T).Name} in world {world.Name}");
            }
            
            return world.GetExistingSystemManaged<T>();
        }
        
        /// <summary>
        /// Ensures the test world is set as the default world
        /// Call this if your test needs DefaultGameObjectInjectionWorld to be non-null
        /// </summary>
        protected void EnsureDefaultWorldIsSet()
        {
            if (World.DefaultGameObjectInjectionWorld == null && testWorld != null && testWorld.IsCreated)
            {
                World.DefaultGameObjectInjectionWorld = testWorld;
                Debug.Log($"[DOTS-TEST] Manually set test world as DefaultGameObjectInjectionWorld");
            }
        }
        
        #endregion
        
        #region Logging Methods
        
        protected void LogJobExecution(string jobName, float executionTimeMs)
        {
            Debug.Log($"[DOTS-JOB] {jobName} completed in {executionTimeMs:F2}ms");
        }
        
        protected void LogSystemUpdate(string systemName, float deltaTime)
        {
            Debug.Log($"[DOTS-SYSTEM] {systemName} triggered at {deltaTime:F2}ms");
        }
        
        protected void LogBufferOperation(string operation, int size)
        {
            Debug.Log($"[DOTS-BUFFER] {operation}: {size} bytes");
        }
        
        protected void LogBurstCompilation(string jobName, bool success)
        {
            Debug.Log($"[DOTS-BURST] {jobName} compiled with Burst: {(success ? "SUCCESS" : "FAILED")}");
        }
        
        protected void LogResult(string testName, bool passed, string details = null)
        {
            string status = passed ? "PASSED" : "FAILED";
            string message = $"[RESULT] {testName}: {status}";
            if (!string.IsNullOrEmpty(details))
            {
                message += $" - {details}";
            }
            Debug.Log(message);
        }
        
        protected void AssertNoMemoryLeaks()
        {
            var allocatedMemory = NativeLeakDetection.Mode;
            // Accept both Enabled and EnabledWithStackTrace as valid modes
            Assert.IsTrue(allocatedMemory == NativeLeakDetectionMode.Enabled || 
                         allocatedMemory == NativeLeakDetectionMode.EnabledWithStackTrace, 
                $"Memory leak detection should be enabled, but was {allocatedMemory}");
        }
        
        /// <summary>
        /// Profiles memory allocation during DOTS operation with tracking
        /// </summary>
        protected async UniTask<long> ProfileDOTSMemoryAsync(Func<UniTask> operation)
        {
            return await RunTrackedOperationAsync("ProfileMemory", async (token) =>
            {
                await UniTask.SwitchToMainThread(token);
                
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.Collect();
                
                var startMemory = System.GC.GetTotalMemory(false);
                await operation();
                var endMemory = System.GC.GetTotalMemory(false);
                
                var allocated = endMemory - startMemory;
                Debug.Log($"[DOTS-MEMORY] Allocated: {allocated:N0} bytes");
                
                return allocated;
            });
        }
        
        #endregion
        
        #region Test Lifecycle
        
        #endregion
        
        #region Async Wait Methods
        
        /// <summary>
        /// Waits for specified frames using UniTask with operation tracking
        /// </summary>
        protected async UniTask WaitForFramesAsync(int frameCount, CancellationToken cancellationToken = default)
        {
            await RunTrackedOperationAsync($"WaitForFrames_{frameCount}", async (token) =>
            {
                for (int i = 0; i < frameCount; i++)
                {
                    token.ThrowIfCancellationRequested();
                    await UniTask.Yield(token);
                }
            }, cancellationToken);
        }
        
        /// <summary>
        /// Waits for a job to complete asynchronously with operation tracking
        /// </summary>
        protected async UniTask WaitForJobAsync(JobHandle jobHandle, CancellationToken cancellationToken = default)
        {
            await RunTrackedOperationAsync("WaitForJob", async (token) =>
            {
                while (!jobHandle.IsCompleted)
                {
                    token.ThrowIfCancellationRequested();
                    await UniTask.Yield(token);
                }
                
                jobHandle.Complete();
            }, cancellationToken);
        }
        
        /// <summary>
        /// Waits for system update asynchronously with operation tracking
        /// </summary>
        protected async UniTask WaitForSystemUpdateAsync<T>(CancellationToken cancellationToken = default) where T : SystemBase
        {
            await RunTrackedOperationAsync($"WaitForSystem_{typeof(T).Name}", async (token) =>
            {
                var systemHandle = testWorld.GetExistingSystem<T>();
                
                if (systemHandle == SystemHandle.Null)
                {
                    throw new InvalidOperationException($"System {typeof(T).Name} not found in test world");
                }
                
                token.ThrowIfCancellationRequested();
                await UniTask.Yield(token);
                
                systemHandle.Update(testWorld.Unmanaged);
                
                token.ThrowIfCancellationRequested();
                await UniTask.Yield(token);
            }, cancellationToken);
        }
        
        #endregion
        
        #region UniTask Test Helpers
        
        /// <summary>
        /// Converts an async UniTask test for Unity Test Framework
        /// This is the bridge between [UnityTest] and async/await
        /// </summary>
        protected System.Collections.IEnumerator RunAsyncTest(Func<UniTask> asyncTest)
        {
            return asyncTest().ToCoroutine();
        }
        
        /// <summary>
        /// Runs a DOTS operation with performance timing and tracking
        /// </summary>
        protected async UniTask<float> MeasureDOTSOperationAsync(Func<UniTask> operation, string operationName)
        {
            return await RunTrackedOperationAsync($"DOTSMeasure_{operationName}", async (token) =>
            {
                var startTime = Time.realtimeSinceStartup;
                await operation();
                var elapsed = Time.realtimeSinceStartup - startTime;
                
                Debug.Log($"[DOTS-TIMING] {operationName}: {elapsed * 1000:F2}ms");
                return elapsed;
            });
        }
        
        /// <summary>
        /// Validates entity state asynchronously
        /// </summary>
        protected async UniTask<bool> ValidateEntityAsync(Entity entity, Func<Entity, bool> validation)
        {
            await UniTask.SwitchToMainThread();
            
            if (!entityManager.Exists(entity))
            {
                Debug.LogError($"[DOTS-ENTITY] Entity {entity.Index} does not exist");
                return false;
            }
            
            return validation(entity);
        }
        
        #endregion
        
        #region Memory Management
        
        /// <summary>
        /// Called at the start of each test
        /// </summary>
        protected void OnTestStart(string testName)
        {
            Debug.Log($"[TEST-START] {testName}");
        }
        
        /// <summary>
        /// Called at the end of each test
        /// </summary>
        protected void OnTestEnd(string testName, bool passed = true, string details = null)
        {
            LogResult(testName, passed, details);
        }
        
        /// <summary>
        /// Called when test throws an exception
        /// </summary>
        protected void OnTestException(string testName, System.Exception ex)
        {
            Debug.LogError($"[EXCEPTION] {testName}: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
            OnTestEnd(testName, false, $"Exception: {ex.Message}");
        }
        
        #endregion
    }
}