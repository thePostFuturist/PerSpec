using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace PerSpec.Editor.Coordination
{
    /// <summary>
    /// Polls database for scene hierarchy export requests and executes them
    /// </summary>
    [InitializeOnLoad]
    public static class SceneHierarchyCoordinator
    {
        private static double _lastCheckTime;
        private static double _checkInterval = 0.5; // Check every 0.5 seconds
        private static bool _isExecuting = false;
        private static SQLiteManager _dbManager;
        private static int _currentRequestId = -1;
        private static DateTime _executionStartTime;
        private static bool _pollingEnabled = true;

        static SceneHierarchyCoordinator()
        {
            // Check if PerSpec is initialized
            if (!SQLiteManager.IsPerSpecInitialized())
            {
                // Silent - PerSpecInitializer will show the prompt
                return;
            }

            Debug.Log("[SceneHierarchyCoordinator] Initializing scene hierarchy coordination");

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

            Debug.Log("[SceneHierarchyCoordinator] Scene hierarchy coordination initialized");
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
                    Debug.LogWarning($"[SceneHierarchyCoordinator] Export timeout for request {_currentRequestId}");
                    CompleteExecution(false, null, "Export timed out");
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
                var request = _dbManager.GetNextPendingHierarchyRequest();

                if (request != null)
                {
                    ExecuteHierarchyRequest(request);
                }
            }
            catch (Exception e)
            {
                // Check if error is due to missing table
                if (e.Message.Contains("no such table: scene_hierarchy_requests"))
                {
                    Debug.LogWarning("[SceneHierarchyCoordinator] Scene hierarchy table missing after package update.\n" +
                                   "To enable scene hierarchy export features, run:\n" +
                                   "  python PerSpec/Coordination/Scripts/add_scene_hierarchy_table.py\n" +
                                   "This is a one-time migration after updating the PerSpec package.");
                    _pollingEnabled = false; // Disable polling to avoid spam
                    EditorApplication.update -= OnEditorUpdate; // Unsubscribe from updates
                }
                else
                {
                    Debug.LogError($"[SceneHierarchyCoordinator] Error checking for requests: {e.Message}");
                }
            }
        }

        private static void ExecuteHierarchyRequest(SceneHierarchyRequest request)
        {
            _isExecuting = true;
            _currentRequestId = request.Id;
            _executionStartTime = DateTime.Now;

            Debug.Log($"[SceneHierarchyCoordinator] Starting export: Type={request.RequestType}, Target={request.TargetPath ?? "full"}");

            // Update status to running
            _dbManager.UpdateHierarchyRequestStatus(request.Id, "running");

            // Delay execution to next frame to ensure database update completes
            EditorApplication.delayCall += () =>
            {
                string outputFile = null;
                string errorMessage = null;
                bool success = false;

                try
                {
                    // Ensure output directory exists
                    string projectPath = Directory.GetParent(Application.dataPath).FullName;
                    string outputDir = Path.Combine(projectPath, "PerSpec", "SceneHierarchy");

                    if (!Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }

                    // Clean directory before export
                    SceneHierarchyExporter.CleanOutputDirectory();

                    // Generate output filename
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string filename = $"hierarchy_{timestamp}.json";
                    outputFile = Path.Combine(outputDir, filename);

                    // Execute the export based on request type
                    string jsonContent;
                    if (request.RequestType == "single_object" && !string.IsNullOrEmpty(request.TargetPath))
                    {
                        Debug.Log($"[SceneHierarchyCoordinator] Exporting single GameObject: {request.TargetPath}");
                        jsonContent = SceneHierarchyExporter.ExportSingleGameObject(
                            request.TargetPath,
                            request.IncludeInactive,
                            request.IncludeComponents
                        );
                    }
                    else
                    {
                        Debug.Log("[SceneHierarchyCoordinator] Exporting full hierarchy");
                        jsonContent = SceneHierarchyExporter.ExportFullHierarchy(
                            request.IncludeInactive,
                            request.IncludeComponents
                        );
                    }

                    // Write to file
                    File.WriteAllText(outputFile, jsonContent);

                    // Get file size for logging
                    var fileInfo = new FileInfo(outputFile);
                    float fileSizeMB = fileInfo.Length / (1024f * 1024f);

                    Debug.Log($"[SceneHierarchyCoordinator] Export completed successfully: {filename} ({fileSizeMB:F2} MB)");
                    success = true;
                }
                catch (Exception e)
                {
                    errorMessage = e.Message;
                    Debug.LogError($"[SceneHierarchyCoordinator] Export failed: {e.Message}\n{e.StackTrace}");
                }
                finally
                {
                    CompleteExecution(success, outputFile, errorMessage);
                }
            };
        }

        private static void CompleteExecution(bool success, string outputFile, string errorMessage)
        {
            if (_currentRequestId > 0)
            {
                // Update request status in database
                _dbManager.UpdateHierarchyRequestStatus(
                    _currentRequestId,
                    success ? "completed" : "failed",
                    outputFile,
                    errorMessage
                );

                if (success && !string.IsNullOrEmpty(outputFile))
                {
                    Debug.Log($"[SceneHierarchyCoordinator] Request {_currentRequestId} completed. Output: {Path.GetFileName(outputFile)}");
                }
                else
                {
                    Debug.LogError($"[SceneHierarchyCoordinator] Request {_currentRequestId} failed: {errorMessage}");
                }
            }

            // Reset execution state
            _isExecuting = false;
            _currentRequestId = -1;
        }

        #region Reset Support

        /// <summary>
        /// Stop all polling for reset operations
        /// </summary>
        public static void StopPolling()
        {
            try
            {
                Debug.Log("[SceneHierarchyCoordinator] Stopping polling for reset...");

                // Disable polling flag
                _pollingEnabled = false;

                // Unsubscribe from EditorApplication.update
                EditorApplication.update -= OnEditorUpdate;

                // Clear database manager reference (will be GC'd)
                _dbManager = null;

                Debug.Log("[SceneHierarchyCoordinator] Polling stopped for reset");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SceneHierarchyCoordinator] Error stopping polling: {ex.Message}");
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
                    Debug.LogWarning("[SceneHierarchyCoordinator] Cannot start polling - PerSpec not initialized");
                    return;
                }

                Debug.Log("[SceneHierarchyCoordinator] Restarting polling after reset...");

                // Recreate database manager
                _dbManager = new SQLiteManager();

                if (!_dbManager.IsInitialized)
                {
                    Debug.LogWarning("[SceneHierarchyCoordinator] Database not initialized, cannot start polling");
                    return;
                }

                // Re-enable polling
                _pollingEnabled = true;

                // Re-subscribe to EditorApplication.update
                EditorApplication.update += OnEditorUpdate;

                // Reset last check time
                _lastCheckTime = EditorApplication.timeSinceStartup;

                Debug.Log("[SceneHierarchyCoordinator] Polling restarted after reset");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SceneHierarchyCoordinator] Error restarting polling: {ex.Message}");
            }
        }

        #endregion
    }
}