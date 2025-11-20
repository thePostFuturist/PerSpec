using UnityEngine;
using System.IO;

namespace PerSpec
{
    /// <summary>
    /// Runtime settings and utilities for PerSpec debug logging.
    /// This class provides runtime checks and utilities that work even when debug logging is disabled.
    ///
    /// Debug logging is controlled by the PERSPEC_DEBUG compiler directive.
    /// Enable by creating 'Assets/csc.rsp' with contents: -define:PERSPEC_DEBUG
    /// Disable by deleting the file.
    /// </summary>
    public static class PerSpecDebugSettings
    {
        #region csc.rsp Management

        private const string CSC_RSP_PATH = "Assets/csc.rsp";
        private const string PERSPEC_DEBUG_DEFINE = "-define:PERSPEC_DEBUG";

        /// <summary>
        /// Enables debug logging by creating Assets/csc.rsp with PERSPEC_DEBUG directive.
        /// Requires Unity recompilation to take effect.
        /// </summary>
        public static void EnableDebugLogging()
        {
#if UNITY_EDITOR
            try
            {
                File.WriteAllText(CSC_RSP_PATH, PERSPEC_DEBUG_DEFINE);
                Debug.Log($"[PerSpec] Created {CSC_RSP_PATH} - Debug logging will be enabled after recompilation.");

                // Force recompilation
                UnityEditor.AssetDatabase.Refresh();
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PerSpec] Failed to create {CSC_RSP_PATH}: {ex.Message}");
            }
#else
            Debug.LogWarning("[PerSpec] EnableDebugLogging() is only available in the Unity Editor.");
#endif
        }

        /// <summary>
        /// Disables debug logging by deleting Assets/csc.rsp.
        /// Requires Unity recompilation to take effect.
        /// </summary>
        public static void DisableDebugLogging()
        {
#if UNITY_EDITOR
            try
            {
                if (File.Exists(CSC_RSP_PATH))
                {
                    File.Delete(CSC_RSP_PATH);

                    // Also delete the .meta file if it exists
                    string metaPath = CSC_RSP_PATH + ".meta";
                    if (File.Exists(metaPath))
                        File.Delete(metaPath);

                    Debug.Log($"[PerSpec] Deleted {CSC_RSP_PATH} - Debug logging will be disabled after recompilation.");

                    // Force recompilation
                    UnityEditor.AssetDatabase.Refresh();
                    UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
                }
                else
                {
                    Debug.Log($"[PerSpec] {CSC_RSP_PATH} does not exist - Debug logging is already disabled.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PerSpec] Failed to delete {CSC_RSP_PATH}: {ex.Message}");
            }
#else
            Debug.LogWarning("[PerSpec] DisableDebugLogging() is only available in the Unity Editor.");
#endif
        }

        /// <summary>
        /// Checks if the csc.rsp file exists (runtime check, may differ from compile-time state)
        /// </summary>
        public static bool IsCscRspPresent => File.Exists(CSC_RSP_PATH);

        #endregion

        #region Constants
        
        // Log category colors (for rich text console)
        private const string COLOR_TEST = "#00FF00";        // Green
        private const string COLOR_ERROR = "#FF0000";       // Red
        private const string COLOR_WARNING = "#FFA500";     // Orange
        private const string COLOR_INFO = "#00BFFF";        // Blue
        private const string COLOR_COMPLETE = "#90EE90";    // Light Green
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Check if debug logging is enabled at compile time
        /// </summary>
        public static bool IsDebugEnabled
        {
            get
            {
#if PERSPEC_DEBUG
                return true;
#else
                return false;
#endif
            }
        }
        
        /// <summary>
        /// Check if running in Unity Editor
        /// </summary>
        public static bool IsEditor
        {
            get
            {
#if UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }
        
        /// <summary>
        /// Check if running in a development build
        /// </summary>
        public static bool IsDevelopmentBuild
        {
            get
            {
                return Debug.isDebugBuild;
            }
        }
        
        #endregion
        
        #region Formatting Helpers
        
        /// <summary>
        /// Format a test message with appropriate prefix and color
        /// </summary>
        public static string FormatTestMessage(string category, string message)
        {
            string prefix = $"[TEST-{category.ToUpper()}]";
            
#if UNITY_EDITOR
            // Use rich text in editor
            string color = GetCategoryColor(category);
            return $"<color={color}>{prefix}</color> {message}";
#else
            return $"{prefix} {message}";
#endif
        }
        
        /// <summary>
        /// Format a feature message with appropriate prefix
        /// </summary>
        public static string FormatFeatureMessage(string feature, string stage, string message)
        {
            string prefix = $"[{feature.ToUpper()}-{stage.ToUpper()}]";
            
#if UNITY_EDITOR
            string color = GetStageColor(stage);
            return $"<color={color}>{prefix}</color> {message}";
#else
            return $"{prefix} {message}";
#endif
        }
        
        /// <summary>
        /// Get color for test category
        /// </summary>
        private static string GetCategoryColor(string category)
        {
            switch (category.ToUpper())
            {
                case "ERROR":
                    return COLOR_ERROR;
                case "COMPLETE":
                    return COLOR_COMPLETE;
                case "SETUP":
                    return COLOR_INFO;
                case "ACT":
                case "ACTION":
                    return COLOR_WARNING;
                case "ASSERT":
                    return COLOR_TEST;
                default:
                    return COLOR_INFO;
            }
        }
        
        /// <summary>
        /// Get color for feature stage
        /// </summary>
        private static string GetStageColor(string stage)
        {
            switch (stage.ToUpper())
            {
                case "ERROR":
                    return COLOR_ERROR;
                case "COMPLETE":
                    return COLOR_COMPLETE;
                case "START":
                    return COLOR_INFO;
                case "PROGRESS":
                    return COLOR_WARNING;
                default:
                    return COLOR_INFO;
            }
        }
        
        #endregion
        
        #region Conditional Execution
        
        /// <summary>
        /// Execute an action only if debug is enabled (for non-critical debug operations)
        /// </summary>
        public static void ExecuteIfDebug(System.Action action)
        {
#if PERSPEC_DEBUG
            action?.Invoke();
#endif
        }
        
        /// <summary>
        /// Return a value based on whether debug is enabled
        /// </summary>
        public static T ConditionalValue<T>(T debugValue, T releaseValue)
        {
#if PERSPEC_DEBUG
            return debugValue;
#else
            return releaseValue;
#endif
        }
        
        #endregion
        
        #region Performance Tracking (Debug Only)
        
#if PERSPEC_DEBUG
        private static System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
#endif
        
        /// <summary>
        /// Start a performance timer (only in debug)
        /// </summary>
        public static void StartTimer()
        {
#if PERSPEC_DEBUG
            stopwatch.Restart();
#endif
        }
        
        /// <summary>
        /// Stop timer and log elapsed time (only in debug)
        /// </summary>
        public static void StopTimer(string operationName)
        {
#if PERSPEC_DEBUG
            stopwatch.Stop();
            PerSpecDebug.Log($"[TIMING] {operationName}: {stopwatch.ElapsedMilliseconds}ms");
#endif
        }
        
        /// <summary>
        /// Get elapsed time without stopping (only in debug)
        /// </summary>
        public static long GetElapsedMilliseconds()
        {
#if PERSPEC_DEBUG
            return stopwatch.ElapsedMilliseconds;
#else
            return 0;
#endif
        }
        
        #endregion
        
        #region Memory Tracking (Debug Only)
        
        /// <summary>
        /// Log current memory usage (only in debug)
        /// </summary>
        public static void LogMemoryUsage(string context = null)
        {
#if PERSPEC_DEBUG
            long totalMemory = System.GC.GetTotalMemory(false);
            string memoryMB = (totalMemory / (1024f * 1024f)).ToString("F2");
            string message = string.IsNullOrEmpty(context) 
                ? $"[MEMORY] Current usage: {memoryMB} MB"
                : $"[MEMORY] {context}: {memoryMB} MB";
            PerSpecDebug.Log(message);
#endif
        }
        
        /// <summary>
        /// Force garbage collection and log memory (only in debug)
        /// </summary>
        public static void ForceGCAndLogMemory(string context = null)
        {
#if PERSPEC_DEBUG
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            LogMemoryUsage(context);
#endif
        }
        
        #endregion
        
        #region Validation Helpers

        /// <summary>
        /// Validate that debug symbols are properly configured
        /// </summary>
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void ValidateDebugConfiguration()
        {
            bool symbolDefined = false;
#if PERSPEC_DEBUG
            symbolDefined = true;
#endif

            bool fileExists = IsCscRspPresent;

            if (symbolDefined)
            {
                Debug.Log("[PerSpec] Debug logging is ENABLED (Assets/csc.rsp present).");
            }
            else if (fileExists)
            {
                Debug.LogWarning("[PerSpec] Assets/csc.rsp exists but PERSPEC_DEBUG not defined. " +
                    "Unity may need to recompile. Try: Assets > Refresh or Ctrl+R.");
            }
            else
            {
                Debug.Log("[PerSpec] Debug logging is DISABLED. All PerSpecDebug calls are stripped from code.\n" +
                    "To enable: PerSpecDebugSettings.EnableDebugLogging() or create Assets/csc.rsp with -define:PERSPEC_DEBUG");
            }
        }
#endif

        #endregion
    }
}