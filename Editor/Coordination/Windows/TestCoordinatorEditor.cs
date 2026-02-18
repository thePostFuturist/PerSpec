using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine.TestTools;
using UnityEditor.TestTools.TestRunner.Api;
using System.Collections;
using TestMode = UnityEditor.TestTools.TestRunner.Api.TestMode;

namespace PerSpec.Editor.Coordination
{
    [InitializeOnLoad]
    public static class TestCoordinatorEditor
    {
        private static double _lastCheckTime;
        private static double _checkInterval = 1.0; // Check every 1 second
        private static bool _isRunningTests = false;
        private static SQLiteManager _dbManager;
        private static TestExecutor _testExecutor;
        private static int _currentRequestId = -1;
        
        // Background processing support
        private static SynchronizationContext _unitySyncContext;
        private static System.Threading.Timer _backgroundTimer;
        private static bool _useBackgroundPolling = true;
        private static DateTime _lastBackgroundPoll;
        
        static TestCoordinatorEditor()
        {
            // Check if PerSpec is initialized
            if (!SQLiteManager.IsPerSpecInitialized())
            {
                // Silent - PerSpecInitializer will show the prompt
                return;
            }
            
            Debug.Log("[TestCoordinator] Initializing test coordination system");
            
            // Capture Unity's sync context for thread marshalling
            _unitySyncContext = SynchronizationContext.Current;
            
            try
            {
                _dbManager = new SQLiteManager();
                
                // Only proceed if database is ready
                if (!_dbManager.IsInitialized)
                {
                    return;
                }
                
                _testExecutor = new TestExecutor(_dbManager);
                
                EditorApplication.update += OnEditorUpdate;
                
                // Initialize last check time
                _lastCheckTime = EditorApplication.timeSinceStartup;
                
                // Set up background polling if enabled
                if (_useBackgroundPolling)
                {
                    SetupBackgroundPolling();
                }
                
                // Force Unity to run in background
                Application.runInBackground = true;
            }
            catch (Exception)
            {
                // Silent failure
                return;
            }
            
            // Update system heartbeat
            _dbManager.UpdateSystemHeartbeat("Unity");

            // Recover any requests orphaned by domain reload
            RecoverOrphanedRequests();

            Debug.Log("[TestCoordinator] Test coordination system initialized");
        }

        /// <summary>
        /// Recovers requests that were stuck in processing/executing status after a domain reload.
        /// When Unity recompiles scripts or reloads the domain, any in-progress test monitoring
        /// is lost, leaving requests in a stuck state. This method detects and handles them.
        /// </summary>
        private static void RecoverOrphanedRequests()
        {
            try
            {
                // Find requests stuck for more than 3 minutes
                var stuckRequests = _dbManager.GetStuckRequests(TimeSpan.FromMinutes(3));

                if (stuckRequests.Count == 0)
                    return;

                Debug.Log($"[TestCoordinator] Found {stuckRequests.Count} orphaned request(s) after domain reload");

                foreach (var request in stuckRequests)
                {
                    Debug.Log($"[TestCoordinator] Recovering stuck request #{request.Id} " +
                             $"(type: {request.RequestType}, platform: {request.TestPlatform}, status: {request.Status})");

                    // Check if test results exist in AppData (Unity's default output location)
                    string appDataResult = FindAppDataTestResult();

                    if (!string.IsNullOrEmpty(appDataResult) && File.Exists(appDataResult))
                    {
                        // Check if the result file was written after the request was created
                        var resultFileTime = File.GetLastWriteTime(appDataResult);
                        if (resultFileTime > request.CreatedAt)
                        {
                            Debug.Log($"[TestCoordinator] Found potential results at {appDataResult} " +
                                     $"(written: {resultFileTime:HH:mm:ss}, request created: {request.CreatedAt:HH:mm:ss})");

                            // Try to recover using the result file
                            if (TryRecoverFromResultFile(request, appDataResult))
                            {
                                Debug.Log($"[TestCoordinator] Successfully recovered request #{request.Id} from AppData results");
                                continue;
                            }
                        }
                    }

                    // No valid results found - mark as failed due to domain reload
                    _dbManager.UpdateRequestStatus(request.Id, "failed",
                        "Request interrupted by domain reload - no results recovered");
                    _dbManager.LogExecution(request.Id, "WARN", "Unity",
                        "Request orphaned by domain reload, marked as failed");

                    Debug.LogWarning($"[TestCoordinator] Marked stuck request #{request.Id} as failed (no results found)");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TestCoordinator] Error recovering orphaned requests: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds Unity's default TestResults.xml location in AppData
        /// </summary>
        private static string FindAppDataTestResult()
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string unityPath = Path.Combine(appDataPath, "Unity", "Editor", "TestResults.xml");

                if (File.Exists(unityPath))
                    return unityPath;
            }
            catch (Exception)
            {
                // Ignore errors finding AppData path
            }

            return null;
        }

        /// <summary>
        /// Attempts to recover a stuck request by parsing an existing result file
        /// </summary>
        private static bool TryRecoverFromResultFile(TestRequest request, string resultFilePath)
        {
            try
            {
                // Read and parse the XML file
                string xmlContent = File.ReadAllText(resultFilePath);

                // Basic XML validation
                if (!xmlContent.Contains("<test-run") || !xmlContent.Contains("</test-run>"))
                {
                    Debug.LogWarning($"[TestCoordinator] Result file appears incomplete or invalid");
                    return false;
                }

                // Parse test counts from XML attributes
                int total = ParseXmlAttribute(xmlContent, "total");
                int passed = ParseXmlAttribute(xmlContent, "passed");
                int failed = ParseXmlAttribute(xmlContent, "failed");
                int skipped = ParseXmlAttribute(xmlContent, "skipped");

                // Calculate duration
                float duration = 0;
                var durationMatch = System.Text.RegularExpressions.Regex.Match(xmlContent, @"duration=""([^""]+)""");
                if (durationMatch.Success && float.TryParse(durationMatch.Groups[1].Value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float parsedDuration))
                {
                    duration = parsedDuration;
                }

                // Update the request with recovered results
                _dbManager.UpdateRequestResults(
                    request.Id,
                    "completed",
                    total,
                    passed,
                    failed,
                    skipped,
                    duration
                );

                _dbManager.LogExecution(request.Id, "INFO", "Unity",
                    $"Recovered from domain reload: {passed}/{total} passed, {failed} failed");

                // Copy the result file to PerSpec/TestResults for consistency
                CopyResultToPerSpecDirectory(resultFilePath, request.Id);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TestCoordinator] Failed to recover from result file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Parses an integer attribute from NUnit XML format
        /// </summary>
        private static int ParseXmlAttribute(string xml, string attributeName)
        {
            var match = System.Text.RegularExpressions.Regex.Match(xml, $@"{attributeName}=""(\d+)""");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int value))
                return value;
            return 0;
        }

        /// <summary>
        /// Copies a recovered result file to the PerSpec/TestResults directory
        /// </summary>
        private static void CopyResultToPerSpecDirectory(string sourcePath, int requestId)
        {
            try
            {
                string projectPath = Directory.GetParent(Application.dataPath).FullName;
                string testResultsPath = Path.Combine(projectPath, "PerSpec", "TestResults");

                if (!Directory.Exists(testResultsPath))
                    Directory.CreateDirectory(testResultsPath);

                string destFileName = $"TestResults_Recovered_{requestId}_{DateTime.Now:yyyyMMdd_HHmmss}.xml";
                string destPath = Path.Combine(testResultsPath, destFileName);

                File.Copy(sourcePath, destPath, true);
                Debug.Log($"[TestCoordinator] Copied recovered results to {destFileName}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TestCoordinator] Could not copy result file: {ex.Message}");
            }
        }
        
        private static void SetupBackgroundPolling()
        {
            _backgroundTimer = new System.Threading.Timer(
                BackgroundPollCallback,
                null,
                TimeSpan.FromSeconds(2), // Initial delay
                TimeSpan.FromSeconds(1)  // Repeat every second
            );
            
            Debug.Log("[TestCoordinator] Background polling enabled");
        }
        
        private static void BackgroundPollCallback(object state)
        {
            // Skip if already running tests
            if (_isRunningTests)
                return;
            
            try
            {
                // Check database from background thread (thread-safe)
                var request = _dbManager.GetNextPendingRequest();
                
                if (request != null)
                {
                    _lastBackgroundPoll = DateTime.Now;
                    Debug.Log($"[TestCoordinator-BG] Found pending test request #{request.Id}");
                    
                    // Marshal back to Unity main thread
                    _unitySyncContext?.Post(_ =>
                    {
                        if (!_isRunningTests && request != null)
                        {
                            CheckForPendingRequests();
                            // Force compilation to ensure Unity processes
                            CompilationPipeline.RequestScriptCompilation();
                        }
                    }, null);
                }
            }
            catch (Exception ex)
            {
                // Log but don't crash the background thread
                UnityEngine.Debug.LogError($"[TestCoordinator-BG] Error: {ex.Message}");
            }
        }
        
        private static void OnEditorUpdate()
        {
            // Check for new requests periodically using Editor time
            double currentTime = EditorApplication.timeSinceStartup;
            
            if (currentTime - _lastCheckTime >= _checkInterval)
            {
                _lastCheckTime = currentTime;
                
                if (!_isRunningTests)
                {
                    CheckForPendingRequests();
                }
                
                // Update heartbeat every check
                _dbManager.UpdateSystemHeartbeat("Unity");
            }
        }
        
        private static void CheckForPendingRequests()
        {
            try
            {
                var pendingRequest = _dbManager.GetNextPendingRequest();
                
                if (pendingRequest != null)
                {
                    Debug.Log($"[TestCoordinator] Found pending request: {pendingRequest.Id}");
                    ProcessTestRequest(pendingRequest);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TestCoordinator] Error checking for pending requests: {ex.Message}");
            }
        }
        
        internal static void ProcessTestRequest(TestRequest request)
        {
            _isRunningTests = true;
            _currentRequestId = request.Id;
            
            try
            {
                // Clean TestResults directory before running new tests
                CleanTestResultsDirectory();
                
                // Update status to processing
                _dbManager.UpdateRequestStatus(request.Id, "processing");
                _dbManager.LogExecution(request.Id, "INFO", "Unity", $"Processing test request {request.Id}");
                
                // Create test filter based on request
                Filter filter = CreateTestFilter(request);
                
                // Execute tests
                _testExecutor.ExecuteTests(request, filter, OnTestComplete);
                
                Debug.Log($"[TestCoordinator] Executing tests for request {request.Id}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TestCoordinator] Error processing request {request.Id}: {ex.Message}");
                
                _dbManager.UpdateRequestStatus(request.Id, "failed", ex.Message);
                _dbManager.LogExecution(request.Id, "ERROR", "Unity", $"Failed to execute tests: {ex.Message}");
                
                _isRunningTests = false;
                _currentRequestId = -1;
            }
        }
        
        private static Filter CreateTestFilter(TestRequest request)
        {
            var filter = new Filter();
            
            // Set test mode
            if (request.TestPlatform == "EditMode")
            {
                filter.testMode = TestMode.EditMode;
            }
            else if (request.TestPlatform == "PlayMode")
            {
                filter.testMode = TestMode.PlayMode;
            }
            else // Both
            {
                // For "Both", we'll need to run tests twice
                filter.testMode = TestMode.EditMode | TestMode.PlayMode;
            }
            
            // Apply filters based on request type
            switch (request.RequestType)
            {
                case "all":
                    // No additional filters needed
                    break;
                    
                case "class":
                    if (!string.IsNullOrEmpty(request.TestFilter))
                    {
                        filter.testNames = new[] { request.TestFilter };
                    }
                    break;
                    
                case "method":
                    if (!string.IsNullOrEmpty(request.TestFilter))
                    {
                        filter.testNames = new[] { request.TestFilter };
                    }
                    break;
                    
                case "category":
                    if (!string.IsNullOrEmpty(request.TestFilter))
                    {
                        filter.categoryNames = new[] { request.TestFilter };
                    }
                    break;
            }
            
            return filter;
        }
        
        private static void OnTestComplete(TestRequest request, bool success, string errorMessage, TestResultSummary summary)
        {
            try
            {
                if (success)
                {
                    // Update request with results
                    _dbManager.UpdateRequestResults(
                        request.Id,
                        "completed",
                        summary.TotalTests,
                        summary.PassedTests,
                        summary.FailedTests,
                        summary.SkippedTests,
                        summary.Duration
                    );
                    
                    _dbManager.LogExecution(request.Id, "INFO", "Unity", 
                        $"Tests completed: {summary.PassedTests}/{summary.TotalTests} passed");
                    
                    Debug.Log($"[TestCoordinator] Tests completed for request {request.Id}: " +
                             $"{summary.PassedTests}/{summary.TotalTests} passed");
                }
                else
                {
                    _dbManager.UpdateRequestStatus(request.Id, "failed", errorMessage);
                    _dbManager.LogExecution(request.Id, "ERROR", "Unity", $"Test execution failed: {errorMessage}");
                    
                    Debug.LogError($"[TestCoordinator] Tests failed for request {request.Id}: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TestCoordinator] Error updating test results: {ex.Message}");
            }
            finally
            {
                _isRunningTests = false;
                _currentRequestId = -1;
            }
        }
        
        // Window functionality now accessed via Control Center - Tools > PerSpec > Control Center
        public static void ShowTestCoordinatorWindow()
        {
            Debug.Log("[PerSpec] Test Coordinator is running in background mode.");
            Debug.Log("[PerSpec] Use the Commands and Debug menu items to interact with the coordinator.");
            Debug.Log($"[PerSpec] Current status: {(_isRunningTests ? $"Running test {_currentRequestId}" : "Idle")}");
            
            if (_dbManager != null)
            {
                var status = _dbManager.GetSystemStatus();
                Debug.Log($"[PerSpec] Database Status:\n{status}");
            }
        }
        
        // Method now accessed via Control Center
        public static void ManualCheckPendingRequests()
        {
            if (!_isRunningTests)
            {
                CheckForPendingRequests();
            }
            else
            {
                Debug.Log($"[TestCoordinator] Currently running test request {_currentRequestId}");
            }
        }
        
        // Method now accessed via Control Center
        public static void ViewDatabaseStatus()
        {
            if (_dbManager != null)
            {
                var status = _dbManager.GetSystemStatus();
                Debug.Log($"[TestCoordinator] Database Status:\n{status}");
            }
        }
        
        // Method now accessed via Control Center
        public static void CancelCurrentTest()
        {
            if (_isRunningTests && _currentRequestId > 0)
            {
                _dbManager.UpdateRequestStatus(_currentRequestId, "cancelled", "Cancelled by user");
                _isRunningTests = false;
                _currentRequestId = -1;
                Debug.Log($"[TestCoordinator] Cancelled test request {_currentRequestId}");
            }
            else
            {
                Debug.Log("[TestCoordinator] No test currently running");
            }
        }
        
        // Method now accessed via Control Center
        public static void TogglePolling()
        {
            if (_checkInterval > 0)
            {
                _checkInterval = 0;
                Debug.Log("[TestCoordinator] Polling disabled");
            }
            else
            {
                _checkInterval = 1.0;
                Debug.Log("[TestCoordinator] Polling enabled (1 second interval)");
            }
        }
        
        // Method now accessed via Control Center
        public static void DebugPollingStatus()
        {
            double currentTime = EditorApplication.timeSinceStartup;
            double timeSinceLastCheck = currentTime - _lastCheckTime;
            
            Debug.Log($"[TestCoordinator] Polling Debug Info:");
            Debug.Log($"  - Polling Enabled: {_checkInterval > 0}");
            Debug.Log($"  - Check Interval: {_checkInterval} seconds");
            Debug.Log($"  - Current Time: {currentTime:F2}");
            Debug.Log($"  - Last Check Time: {_lastCheckTime:F2}");
            Debug.Log($"  - Time Since Last Check: {timeSinceLastCheck:F2} seconds");
            Debug.Log($"  - Is Running Tests: {_isRunningTests}");
            Debug.Log($"  - Current Request ID: {_currentRequestId}");
        }
        
        private static void CleanTestResultsDirectory()
        {
            try
            {
                string projectPath = Directory.GetParent(Application.dataPath).FullName;
                string testResultsPath = Path.Combine(projectPath, "PerSpec", "TestResults");
                
                if (Directory.Exists(testResultsPath))
                {
                    // Get all files in the TestResults directory
                    string[] files = Directory.GetFiles(testResultsPath, "*", SearchOption.AllDirectories);
                    
                    foreach (string file in files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[TestCoordinator] Failed to delete file {file}: {ex.Message}");
                        }
                    }
                    
                    // Get and delete all subdirectories
                    string[] directories = Directory.GetDirectories(testResultsPath, "*", SearchOption.AllDirectories);
                    
                    // Delete directories in reverse order (deepest first)
                    for (int i = directories.Length - 1; i >= 0; i--)
                    {
                        try
                        {
                            Directory.Delete(directories[i], true);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[TestCoordinator] Failed to delete directory {directories[i]}: {ex.Message}");
                        }
                    }
                    
                    Debug.Log($"[TestCoordinator] Cleaned TestResults directory");
                }
                else
                {
                    // Create the directory if it doesn't exist
                    Directory.CreateDirectory(testResultsPath);
                    Debug.Log($"[TestCoordinator] Created TestResults directory");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TestCoordinator] Error cleaning TestResults directory: {ex.Message}");
            }
        }
        
        #region Debug Methods (formerly in TestCoordinationDebug)
        
        // Force reinitialization - accessed via Control Center
        public static void ForceReinitialize()
        {
            Debug.Log("[TestCoordinator] Forcing reinitialization...");
            
            // This will trigger the static constructor again after domain reload
            EditorUtility.RequestScriptReload();
        }
        
        // Test database connection - accessed via Control Center
        public static void TestDatabaseConnection()
        {
            try
            {
                var dbManager = new SQLiteManager();
                Debug.Log("[TestCoordinator] Database connection successful");
                
                var pendingRequests = dbManager.GetAllPendingRequests();
                Debug.Log($"[TestCoordinator] Found {pendingRequests.Count} pending requests");
                
                foreach (var request in pendingRequests)
                {
                    Debug.Log($"  - Request #{request.Id}: {request.RequestType} on {request.TestPlatform} (Status: {request.Status})");
                }
                
                dbManager.UpdateSystemHeartbeat("Unity");
                Debug.Log("[TestCoordinator] Heartbeat updated");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestCoordinator] Database error: {e.Message}");
                Debug.LogError(e.StackTrace);
            }
        }
        
        // Manually process next request - accessed via Control Center
        public static void ManuallyProcessNextRequest()
        {
            try
            {
                var dbManager = new SQLiteManager();
                var nextRequest = dbManager.GetNextPendingRequest();
                
                if (nextRequest != null)
                {
                    Debug.Log($"[TestCoordinator] Processing request #{nextRequest.Id}");
                    
                    // Update to running
                    dbManager.UpdateRequestStatus(nextRequest.Id, "running");
                    
                    // Try to execute
                    var testExecutor = new TestExecutor(dbManager);
                    var filter = new Filter();
                    
                    if (nextRequest.TestPlatform == "EditMode")
                    {
                        filter.testMode = TestMode.EditMode;
                    }
                    else if (nextRequest.TestPlatform == "PlayMode")
                    {
                        filter.testMode = TestMode.PlayMode;
                    }
                    
                    testExecutor.ExecuteTests(nextRequest, filter, (req, success, error, summary) =>
                    {
                        if (success && summary != null)
                        {
                            Debug.Log($"[TestCoordinator] Test completed: {summary.PassedTests}/{summary.TotalTests} passed");
                            dbManager.UpdateRequestResults(req.Id, "completed", 
                                summary.TotalTests, summary.PassedTests, 
                                summary.FailedTests, summary.SkippedTests, summary.Duration);
                        }
                        else
                        {
                            Debug.LogError($"[TestCoordinator] Test failed: {error}");
                            dbManager.UpdateRequestStatus(req.Id, "failed", error);
                        }
                    });
                }
                else
                {
                    Debug.Log("[TestCoordinator] No pending requests found");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestCoordinator] Error processing request: {e.Message}");
                Debug.LogError(e.StackTrace);
            }
        }
        
        // Clear all pending requests - accessed via Control Center
        public static void ClearAllPendingRequests()
        {
            try
            {
                var dbManager = new SQLiteManager();
                var pendingRequests = dbManager.GetAllPendingRequests();
                
                foreach (var request in pendingRequests)
                {
                    dbManager.UpdateRequestStatus(request.Id, "cancelled", "Cancelled by debug tool");
                    Debug.Log($"[TestCoordinator] Cancelled request #{request.Id}");
                }
                
                Debug.Log($"[TestCoordinator] Cleared {pendingRequests.Count} pending requests");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TestCoordinator] Error clearing requests: {e.Message}");
            }
        }
        
        #endregion

        #region Reset Support

        /// <summary>
        /// Stop all polling for reset operations
        /// </summary>
        public static void StopPolling()
        {
            try
            {
                Debug.Log("[TestCoordinatorEditor] Stopping polling for reset...");

                // Unsubscribe from EditorApplication.update
                EditorApplication.update -= OnEditorUpdate;

                // Dispose background timer if exists
                _backgroundTimer?.Dispose();
                _backgroundTimer = null;

                // Clear database manager reference (will be GC'd)
                _dbManager = null;
                _testExecutor = null;

                Debug.Log("[TestCoordinatorEditor] Polling stopped for reset");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TestCoordinatorEditor] Error stopping polling: {ex.Message}");
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
                    Debug.LogWarning("[TestCoordinatorEditor] Cannot start polling - PerSpec not initialized");
                    return;
                }

                Debug.Log("[TestCoordinatorEditor] Restarting polling after reset...");

                // Recreate database manager
                _dbManager = new SQLiteManager();

                if (!_dbManager.IsInitialized)
                {
                    Debug.LogWarning("[TestCoordinatorEditor] Database not initialized, cannot start polling");
                    return;
                }

                // Recreate test executor
                _testExecutor = new TestExecutor(_dbManager);

                // Re-subscribe to EditorApplication.update
                EditorApplication.update += OnEditorUpdate;

                // Reset last check time
                _lastCheckTime = EditorApplication.timeSinceStartup;

                // Set up background polling if enabled
                if (_useBackgroundPolling)
                {
                    SetupBackgroundPolling();
                }

                // Update heartbeat
                _dbManager.UpdateSystemHeartbeat("Unity");

                Debug.Log("[TestCoordinatorEditor] Polling restarted after reset");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TestCoordinatorEditor] Error restarting polling: {ex.Message}");
            }
        }

        #endregion
    }
}