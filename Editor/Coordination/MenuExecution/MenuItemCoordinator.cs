using System;
using UnityEngine;
using UnityEditor;

namespace PerSpec.Editor.Coordination
{
    /// <summary>
    /// Polls database for menu item execution requests and executes them
    /// </summary>
    [InitializeOnLoad]
    public static class MenuItemCoordinator
    {
        private static double _lastCheckTime;
        private static double _checkInterval = 0.5; // Check every 0.5 seconds for faster response
        private static bool _isExecuting = false;
        private static SQLiteManager _dbManager;
        private static int _currentRequestId = -1;
        private static DateTime _executionStartTime;
        private static bool _pollingEnabled = true;
        
        static MenuItemCoordinator()
        {
            // Check if PerSpec is initialized
            if (!SQLiteManager.IsPerSpecInitialized())
            {
                // Silent - PerSpecInitializer will show the prompt
                return;
            }
            
            Debug.Log("[MenuItemCoordinator] Initializing menu item coordination");
            
            try
            {
                _dbManager = new SQLiteManager();
                
                // Only proceed if database is ready
                if (!_dbManager.IsInitialized)
                {
                    return;
                }
                
                EditorApplication.update += OnEditorUpdate;
                _lastCheckTime = EditorApplication.timeSinceStartup;
            }
            catch (Exception)
            {
                // Silent failure
                return;
            }
            
            Debug.Log("[MenuItemCoordinator] Menu item coordination initialized");
        }
        
        private static void OnEditorUpdate()
        {
            if (!_pollingEnabled || _dbManager == null || !_dbManager.IsInitialized)
                return;
            
            // Check if it's time to poll for requests
            if (EditorApplication.timeSinceStartup - _lastCheckTime < _checkInterval)
                return;
            
            _lastCheckTime = EditorApplication.timeSinceStartup;
            
            // Skip if already executing
            if (_isExecuting)
            {
                // Check for timeout (30 seconds)
                if ((DateTime.Now - _executionStartTime).TotalSeconds > 30)
                {
                    Debug.LogWarning($"[MenuItemCoordinator] Menu execution timeout for request {_currentRequestId}");
                    CompleteExecution(false, null, "Execution timed out");
                }
                return;
            }
            
            // Check for pending requests
            CheckForPendingRequests();
        }
        
        private static void CheckForPendingRequests()
        {
            try
            {
                var request = _dbManager.GetNextPendingMenuRequest();
                
                if (request != null)
                {
                    ExecuteMenuRequest(request);
                }
            }
            catch (Exception e)
            {
                // Check if error is due to missing table
                if (e.Message.Contains("no such table: menu_item_requests"))
                {
                    Debug.LogWarning("[MenuItemCoordinator] Menu execution table missing after package update.\n" +
                                   "To enable menu execution features, run:\n" +
                                   "  python PerSpec/Coordination/Scripts/db_migrate.py\n" +
                                   "This is a one-time migration after updating the PerSpec package.");
                    _pollingEnabled = false; // Disable polling to avoid spam
                    EditorApplication.update -= OnEditorUpdate; // Unsubscribe from updates
                }
                else
                {
                    Debug.LogError($"[MenuItemCoordinator] Error checking for requests: {e.Message}");
                }
            }
        }
        
        private static void ExecuteMenuRequest(MenuItemRequest request)
        {
            _isExecuting = true;
            _currentRequestId = request.Id;
            _executionStartTime = DateTime.Now;
            
            Debug.Log($"[MenuItemCoordinator] Starting execution of menu: {request.MenuPath}");
            
            // Update status to running
            _dbManager.UpdateMenuRequestStatus(request.Id, "running");
            
            // Delay execution to next frame to ensure database update completes
            EditorApplication.delayCall += () =>
            {
                try
                {
                    // Validate menu path
                    if (string.IsNullOrEmpty(request.MenuPath))
                    {
                        throw new ArgumentException("Menu path cannot be empty");
                    }
                    
                    // Special handling for some common menu items
                    bool success = false;
                    string result = null;
                    
                    // Try to execute the menu item
                    if (request.MenuPath.StartsWith("Assets/"))
                    {
                        // Asset menu items
                        success = EditorApplication.ExecuteMenuItem(request.MenuPath);
                        result = success ? "Menu executed successfully" : "Menu item not found or unavailable";
                    }
                    else if (request.MenuPath == "Edit/Play")
                    {
                        // Enter play mode
                        EditorApplication.EnterPlaymode();
                        success = true;
                        result = "Entered play mode";
                    }
                    else if (request.MenuPath == "Edit/Pause")
                    {
                        // Pause play mode
                        EditorApplication.isPaused = true;
                        success = true;
                        result = "Paused play mode";
                    }
                    else if (request.MenuPath == "Edit/Stop")
                    {
                        // Exit play mode
                        EditorApplication.ExitPlaymode();
                        success = true;
                        result = "Exited play mode";
                    }
                    else
                    {
                        // Generic menu execution
                        success = EditorApplication.ExecuteMenuItem(request.MenuPath);
                        
                        if (success)
                        {
                            result = $"Successfully executed: {request.MenuPath}";
                        }
                        else
                        {
                            // Menu might not exist or might be disabled
                            result = null;
                            throw new InvalidOperationException($"Menu item not found or disabled: {request.MenuPath}");
                        }
                    }
                    
                    CompleteExecution(success, result, success ? null : "Menu execution returned false");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MenuItemCoordinator] Failed to execute menu '{request.MenuPath}': {e.Message}");
                    CompleteExecution(false, null, e.Message);
                }
            };
        }
        
        private static void CompleteExecution(bool success, string result, string errorMessage)
        {
            if (_currentRequestId > 0)
            {
                var status = success ? "completed" : "failed";
                _dbManager.UpdateMenuRequestStatus(_currentRequestId, status, result, errorMessage);
                
                var duration = (DateTime.Now - _executionStartTime).TotalSeconds;
                Debug.Log($"[MenuItemCoordinator] Request {_currentRequestId} {status} in {duration:F2}s");
            }
            
            _isExecuting = false;
            _currentRequestId = -1;
        }
        
        public static void EnablePolling(bool enable)
        {
            _pollingEnabled = enable;
            Debug.Log($"[MenuItemCoordinator] Polling {(enable ? "enabled" : "disabled")}");
        }
        
        public static string GetStatus()
        {
            return $"Menu Coordinator Status:\n" +
                   $"  Polling: {(_pollingEnabled ? "Enabled" : "Disabled")}\n" +
                   $"  Database: {(_dbManager?.IsInitialized == true ? "Connected" : "Not connected")}\n" +
                   $"  Executing: {(_isExecuting ? $"Yes (Request #{_currentRequestId})" : "No")}\n" +
                   $"  Check Interval: {_checkInterval:F1}s";
        }
        
        // Menu commands for testing
        [MenuItem("Tools/PerSpec/Debug/Coordination/Test Menu Execution")]
        public static void TestMenuExecution()
        {
            Debug.Log("[MenuItemCoordinator] Testing menu execution - submitting test request via database");
            Debug.Log("Use: python PerSpec/Coordination/Scripts/quick_menu.py execute \"Help/About Unity\" --wait");
        }

        #region Reset Support

        /// <summary>
        /// Stop all polling for reset operations
        /// </summary>
        public static void StopPolling()
        {
            try
            {
                Debug.Log("[MenuItemCoordinator] Stopping polling for reset...");

                // Disable polling flag
                _pollingEnabled = false;

                // Unsubscribe from EditorApplication.update
                EditorApplication.update -= OnEditorUpdate;

                // Clear database manager reference (will be GC'd)
                _dbManager = null;

                Debug.Log("[MenuItemCoordinator] Polling stopped for reset");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MenuItemCoordinator] Error stopping polling: {ex.Message}");
            }
        }

        /// <summary>
        /// Restart polling after reset operations
        /// </summary>
        public static void StartPolling()
        {
            try
            {
                if (!SQLiteManager.IsPerSpecInitialized())
                {
                    Debug.LogWarning("[MenuItemCoordinator] Cannot start polling - PerSpec not initialized");
                    return;
                }

                Debug.Log("[MenuItemCoordinator] Restarting polling after reset...");

                // Recreate database manager
                _dbManager = new SQLiteManager();

                if (!_dbManager.IsInitialized)
                {
                    Debug.LogWarning("[MenuItemCoordinator] Database not initialized, cannot start polling");
                    return;
                }

                // Re-enable polling
                _pollingEnabled = true;

                // Re-subscribe to EditorApplication.update
                EditorApplication.update += OnEditorUpdate;

                // Reset last check time
                _lastCheckTime = EditorApplication.timeSinceStartup;

                Debug.Log("[MenuItemCoordinator] Polling restarted after reset");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MenuItemCoordinator] Error restarting polling: {ex.Message}");
            }
        }

        #endregion
    }
}