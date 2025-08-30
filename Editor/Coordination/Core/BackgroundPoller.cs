using System;
using System.Threading;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.TestTools.TestRunner.Api;

namespace PerSpec.Editor.Coordination
{
    /// <summary>
    /// Background polling system that continues to run even when Unity loses focus
    /// Uses System.Threading.Timer for true background operation
    /// </summary>
    [InitializeOnLoad]
    public static class BackgroundPoller
    {
        private static System.Threading.Timer _backgroundTimer;
        private static SynchronizationContext _unitySyncContext;
        private static SQLiteManager _dbManager;
        private static bool _isEnabled = false;
        private static readonly object _lockObject = new object();
        private static DateTime _lastPollTime;
        private static int _pollInterval = 1000; // 1 second in milliseconds
        
        // Track if we're currently processing to avoid overlapping operations
        private static bool _isProcessing = false;
        
        static BackgroundPoller()
        {
            // Check if PerSpec is initialized
            if (!SQLiteManager.IsPerSpecInitialized())
            {
                // Silent - PerSpecInitializer will show the prompt
                return;
            }
            
            Debug.Log("[BackgroundPoller] Initializing background polling system");
            
            // Capture Unity's synchronization context for thread marshalling
            _unitySyncContext = SynchronizationContext.Current;
            
            // Initialize database manager
            try
            {
                _dbManager = new SQLiteManager();
                
                // Only proceed if database is ready
                if (!_dbManager.IsInitialized)
                {
                    return;
                }
            }
            catch (Exception)
            {
                // Silent failure
                return;
            }
            
            // Auto-enable background polling
            EnableBackgroundPolling();
            
            // Subscribe to domain reload to clean up
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }
        
        private static void OnBeforeAssemblyReload()
        {
            Debug.Log("[BackgroundPoller] Assembly reloading, stopping background timer");
            DisableBackgroundPolling();
        }
        
        public static void EnableBackgroundPolling()
        {
            lock (_lockObject)
            {
                if (_isEnabled)
                {
                    Debug.Log("[BackgroundPoller] Background polling already enabled");
                    return;
                }
                
                _isEnabled = true;
                _lastPollTime = DateTime.Now;
                
                // Create and start the background timer
                _backgroundTimer = new System.Threading.Timer(
                    BackgroundPollCallback,
                    null,
                    0, // Start immediately
                    _pollInterval // Repeat every second
                );
                
                Debug.Log("[BackgroundPoller] Background polling ENABLED");
            }
        }
        
        public static void DisableBackgroundPolling()
        {
            lock (_lockObject)
            {
                if (!_isEnabled)
                {
                    Debug.Log("[BackgroundPoller] Background polling already disabled");
                    return;
                }
                
                _isEnabled = false;
                
                // Dispose of the timer
                _backgroundTimer?.Dispose();
                _backgroundTimer = null;
                
                Debug.Log("[BackgroundPoller] Background polling DISABLED");
            }
        }
        
        private static void BackgroundPollCallback(object state)
        {
            // Skip if already processing or disabled
            if (!_isEnabled || _isProcessing)
            {
                return;
            }
            
            try
            {
                _isProcessing = true;
                
                // Database operations are thread-safe with SQLite WAL mode
                bool hasTestRequests = CheckForPendingTestRequests();
                bool hasRefreshRequests = CheckForPendingRefreshRequests();
                
                if (hasTestRequests || hasRefreshRequests)
                {
                    Debug.Log($"[BackgroundPoller-Thread] Found pending requests - Test: {hasTestRequests}, Refresh: {hasRefreshRequests}");
                    
                    // Marshal the processing back to Unity's main thread
                    _unitySyncContext?.Post(_ =>
                    {
                        try
                        {
                            Debug.Log("[BackgroundPoller-MainThread] Processing pending requests on main thread");
                            
                            if (hasTestRequests)
                            {
                                // Trigger test processing
                                ProcessPendingTestRequest();
                            }
                            
                            if (hasRefreshRequests)
                            {
                                // Trigger refresh processing
                                ProcessPendingRefreshRequest();
                            }
                            
                            // Force script compilation to ensure Unity processes everything
                            if (hasTestRequests || hasRefreshRequests)
                            {
                                Debug.Log("[BackgroundPoller-MainThread] Requesting script compilation");
                                CompilationPipeline.RequestScriptCompilation();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[BackgroundPoller-MainThread] Error processing requests: {ex.Message}");
                        }
                    }, null);
                }
            }
            catch (Exception ex)
            {
                // Log errors but don't crash the background thread
                // Note: Debug.Log might not work from background thread
                UnityEngine.Debug.LogError($"[BackgroundPoller-Thread] Error in background poll: {ex.Message}");
            }
            finally
            {
                _isProcessing = false;
            }
        }
        
        private static bool CheckForPendingTestRequests()
        {
            try
            {
                // Direct database check - thread safe
                var request = _dbManager.GetNextPendingRequest();
                return request != null;
            }
            catch
            {
                return false;
            }
        }
        
        private static bool CheckForPendingRefreshRequests()
        {
            try
            {
                // Direct database check - thread safe
                var request = _dbManager.GetNextPendingRefreshRequest();
                return request != null;
            }
            catch
            {
                return false;
            }
        }
        
        private static void ProcessPendingTestRequest()
        {
            try
            {
                // Get the request again on main thread
                var request = _dbManager.GetNextPendingRequest();
                if (request != null)
                {
                    Debug.Log($"[BackgroundPoller] Processing test request #{request.Id}");
                    
                    // Delegate to TestCoordinatorEditor which has proper database update logic
                    TestCoordinatorEditor.ProcessTestRequest(request);
                    
                    _dbManager.LogExecution(request.Id, "INFO", "BackgroundPoller", 
                        "Test request triggered via background polling and delegated to TestCoordinator");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BackgroundPoller] Error processing test request: {ex.Message}");
            }
        }
        
        
        private static void ProcessPendingRefreshRequest()
        {
            try
            {
                // Get the request again on main thread
                var request = _dbManager.GetNextPendingRefreshRequest();
                if (request != null)
                {
                    Debug.Log($"[BackgroundPoller] Processing refresh request #{request.Id}");
                    
                    // Update status to running
                    _dbManager.UpdateRefreshRequestStatus(request.Id, "running");
                    
                    // Execute the refresh
                    AssetDatabase.Refresh();
                    
                    // Mark as completed
                    _dbManager.UpdateRefreshRequestStatus(request.Id, "completed", 
                        "Refresh triggered via background polling");
                    
                    _dbManager.LogExecution(request.Id, "INFO", "BackgroundPoller", 
                        "Refresh request triggered via background polling");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BackgroundPoller] Error processing refresh request: {ex.Message}");
            }
        }
        
        // Menu items for manual control
        // Methods now accessed via Control Center
        public static void MenuEnablePolling()
        {
            EnableBackgroundPolling();
        }
        
        // Method now accessed via Control Center
        public static void MenuDisablePolling()
        {
            DisableBackgroundPolling();
        }
        
        // Method now accessed via Control Center
        public static void ShowPollingStatus()
        {
            Debug.Log($"[BackgroundPoller] Status: {(_isEnabled ? "ENABLED" : "DISABLED")}");
            if (_isEnabled)
            {
                Debug.Log($"  Last poll: {_lastPollTime:HH:mm:ss}");
                Debug.Log($"  Poll interval: {_pollInterval}ms");
                Debug.Log($"  Is processing: {_isProcessing}");
            }
        }
        
        // Method now accessed via Control Center
        public static void ForceScriptCompilation()
        {
            Debug.Log("[BackgroundPoller] Forcing script compilation");
            CompilationPipeline.RequestScriptCompilation();
        }
    }
}