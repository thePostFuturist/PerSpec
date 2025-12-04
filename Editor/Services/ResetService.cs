using System;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEditor;
using PerSpec.Editor.Coordination;

namespace PerSpec.Editor.Services
{
    /// <summary>
    /// Service for performing comprehensive reset of PerSpec system
    /// Stops all polling, cleans database and logs, reinitializes, and restarts services
    /// </summary>
    public static class ResetService
    {
        #region Public Methods

        /// <summary>
        /// Perform a complete reset of PerSpec system
        /// </summary>
        /// <param name="progressCallback">Optional callback for progress updates (message, progress 0-1)</param>
        /// <returns>True if reset completed successfully, false if critical errors occurred</returns>
        public static bool Reset(Action<string, float> progressCallback = null)
        {
            Debug.Log("[ResetService] Starting comprehensive reset...");
            bool hasWarnings = false;

            try
            {
                // STEP 1: Stop all polling services (10-25%)
                progressCallback?.Invoke("Stopping coordination services...", 0.1f);
                if (!StopAllServices())
                {
                    Debug.LogWarning("[ResetService] Some services failed to stop cleanly - continuing anyway");
                    hasWarnings = true;
                }

                // STEP 2: Wait for services to stop (25-30%)
                progressCallback?.Invoke("Waiting for services to stop...", 0.25f);
                WaitForServicesStop();

                // STEP 3: Clean database data (30-40%)
                progressCallback?.Invoke("Cleaning database data...", 0.3f);
                if (!ResetDatabase())
                {
                    Debug.LogError("[ResetService] Failed to reset database");
                    hasWarnings = true;
                    // Continue anyway to clean logs and restart services
                }

                // STEP 4: Clean log directories (40-60%)
                progressCallback?.Invoke("Cleaning log directories...", 0.5f);
                if (!CleanLogDirectories())
                {
                    Debug.LogWarning("[ResetService] Some log files could not be deleted");
                    hasWarnings = true;
                }

                // STEP 5: Restart polling services (60-80%)
                progressCallback?.Invoke("Restarting coordination services...", 0.7f);
                if (!RestartAllServices())
                {
                    Debug.LogWarning("[ResetService] Some services failed to restart");
                    hasWarnings = true;
                }

                // STEP 6: Complete (100%)
                progressCallback?.Invoke("Reset complete!", 1.0f);

                if (hasWarnings)
                {
                    Debug.LogWarning("[ResetService] Reset completed with warnings - check console for details");
                    return false;
                }
                else
                {
                    Debug.Log("[ResetService] Reset completed successfully!");
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ResetService] Critical error during reset: {e.Message}\n{e.StackTrace}");
                progressCallback?.Invoke("Reset failed!", 1.0f);
                return false;
            }
        }

        #endregion

        #region Private Methods - Service Control

        /// <summary>
        /// Stop all polling services
        /// </summary>
        private static bool StopAllServices()
        {
            bool allStopped = true;

            try
            {
                Debug.Log("[ResetService] Stopping BackgroundPoller...");
                BackgroundPoller.DisableBackgroundPolling();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ResetService] Failed to stop BackgroundPoller: {e.Message}");
                allStopped = false;
            }

            try
            {
                Debug.Log("[ResetService] Stopping TestCoordinatorEditor...");
                TestCoordinatorEditor.StopPolling();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ResetService] Failed to stop TestCoordinatorEditor: {e.Message}");
                allStopped = false;
            }

            try
            {
                Debug.Log("[ResetService] Stopping AssetRefreshCoordinator...");
                AssetRefreshCoordinator.StopPolling();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ResetService] Failed to stop AssetRefreshCoordinator: {e.Message}");
                allStopped = false;
            }

            try
            {
                Debug.Log("[ResetService] Stopping MenuItemCoordinator...");
                MenuItemCoordinator.StopPolling();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ResetService] Failed to stop MenuItemCoordinator: {e.Message}");
                allStopped = false;
            }

            try
            {
                Debug.Log("[ResetService] Stopping SceneHierarchyCoordinator...");
                SceneHierarchyCoordinator.StopPolling();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ResetService] Failed to stop SceneHierarchyCoordinator: {e.Message}");
                allStopped = false;
            }

            return allStopped;
        }

        /// <summary>
        /// Wait for services to stop and close database connections
        /// </summary>
        private static void WaitForServicesStop()
        {
            Debug.Log("[ResetService] Waiting for services to wind down...");

            // Give services time to stop
            Thread.Sleep(1000);

            // Force garbage collection to close database connections
            Debug.Log("[ResetService] Forcing garbage collection to close database connections...");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Give finalizers more time to complete
            Thread.Sleep(1000);

            // One more aggressive GC cycle to ensure everything is closed
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Final wait for system to release file handles
            Thread.Sleep(500);

            Debug.Log("[ResetService] Services stopped and connections closed");
        }

        /// <summary>
        /// Restart all polling services
        /// </summary>
        private static bool RestartAllServices()
        {
            bool allRestarted = true;

            // Wait a moment before restarting
            Thread.Sleep(200);

            try
            {
                Debug.Log("[ResetService] Restarting BackgroundPoller...");
                BackgroundPoller.EnableBackgroundPolling();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ResetService] Failed to restart BackgroundPoller: {e.Message}");
                allRestarted = false;
            }

            try
            {
                Debug.Log("[ResetService] Restarting TestCoordinatorEditor...");
                TestCoordinatorEditor.StartPolling();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ResetService] Failed to restart TestCoordinatorEditor: {e.Message}");
                allRestarted = false;
            }

            try
            {
                Debug.Log("[ResetService] Restarting AssetRefreshCoordinator...");
                AssetRefreshCoordinator.StartPolling();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ResetService] Failed to restart AssetRefreshCoordinator: {e.Message}");
                allRestarted = false;
            }

            try
            {
                Debug.Log("[ResetService] Restarting MenuItemCoordinator...");
                MenuItemCoordinator.StartPolling();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ResetService] Failed to restart MenuItemCoordinator: {e.Message}");
                allRestarted = false;
            }

            try
            {
                Debug.Log("[ResetService] Restarting SceneHierarchyCoordinator...");
                SceneHierarchyCoordinator.StartPolling();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ResetService] Failed to restart SceneHierarchyCoordinator: {e.Message}");
                allRestarted = false;
            }

            return allRestarted;
        }

        #endregion

        #region Private Methods - File Operations

        /// <summary>
        /// Reset database by dropping and recreating all tables (keeps database file)
        /// </summary>
        private static bool ResetDatabase()
        {
            try
            {
                Debug.Log("[ResetService] Resetting database via Python script...");

                string pythonScript = Path.Combine(
                    InitializationService.CoordinationScriptsPath,
                    "db_initializer.py"
                );

                if (!File.Exists(pythonScript))
                {
                    Debug.LogError($"[ResetService] Database initializer script not found: {pythonScript}");
                    return false;
                }

                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"\"{pythonScript}\" reset",  // 'reset' arg drops and recreates tables
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Directory.GetParent(Application.dataPath).FullName
                };

                Debug.Log($"[ResetService] Executing: python \"{pythonScript}\" reset");

                using (var process = System.Diagnostics.Process.Start(processInfo))
                {
                    if (process == null)
                    {
                        Debug.LogError("[ResetService] Failed to start Python process");
                        return false;
                    }

                    bool completed = process.WaitForExit(10000); // 10 second timeout

                    if (!completed)
                    {
                        Debug.LogError("[ResetService] Database reset timed out");
                        try { process.Kill(); } catch { }
                        return false;
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    if (process.ExitCode == 0)
                    {
                        Debug.Log($"[ResetService] Database reset successfully");
                        if (!string.IsNullOrEmpty(output))
                        {
                            Debug.Log($"[ResetService] Output: {output}");
                        }
                        return true;
                    }
                    else
                    {
                        Debug.LogError($"[ResetService] Database reset failed with exit code {process.ExitCode}");
                        if (!string.IsNullOrEmpty(error))
                        {
                            Debug.LogError($"[ResetService] Error: {error}");
                        }
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ResetService] Failed to reset database: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clean all log directories
        /// </summary>
        private static bool CleanLogDirectories()
        {
            bool allCleaned = true;
            string perspecPath = InitializationService.ProjectPerSpecPath;

            // Clean EditModeLogs
            string editModeLogsPath = Path.Combine(perspecPath, "EditModeLogs");
            if (!CleanDirectory(editModeLogsPath, "*"))
            {
                allCleaned = false;
            }

            // Clean PlayModeLogs
            string playModeLogsPath = Path.Combine(perspecPath, "PlayModeLogs");
            if (!CleanDirectory(playModeLogsPath, "*"))
            {
                allCleaned = false;
            }

            // Clean TestResults
            string testResultsPath = Path.Combine(perspecPath, "TestResults");
            if (!CleanDirectory(testResultsPath, "*"))
            {
                allCleaned = false;
            }

            // Clean Logs (legacy)
            string logsPath = Path.Combine(perspecPath, "Logs");
            if (!CleanDirectory(logsPath, "*"))
            {
                allCleaned = false;
            }

            // Clean SceneHierarchy
            string sceneHierarchyPath = Path.Combine(perspecPath, "SceneHierarchy");
            if (!CleanDirectory(sceneHierarchyPath, "*"))
            {
                allCleaned = false;
            }

            if (allCleaned)
            {
                Debug.Log("[ResetService] All log directories cleaned successfully");
            }

            return allCleaned;
        }

        /// <summary>
        /// Clean all files in a directory matching a pattern
        /// </summary>
        private static bool CleanDirectory(string path, string pattern)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    // Directory doesn't exist, nothing to clean
                    return true;
                }

                bool allDeleted = true;
                var files = Directory.GetFiles(path, pattern);

                if (files.Length == 0)
                {
                    Debug.Log($"[ResetService] No files to clean in: {path}");
                    return true;
                }

                Debug.Log($"[ResetService] Cleaning {files.Length} files from: {path}");

                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[ResetService] Could not delete {file}: {e.Message}");
                        allDeleted = false;
                    }
                }

                if (allDeleted)
                {
                    Debug.Log($"[ResetService] Cleaned all files from: {path}");
                }

                return allDeleted;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ResetService] Error cleaning directory {path}: {e.Message}");
                return false;
            }
        }

        #endregion
    }
}
