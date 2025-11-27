using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using PerSpec;

namespace PerSpec.Editor.Services
{
    /// <summary>
    /// Service for managing debug settings and logging.
    /// Now delegates to DebugLoggingService for consistency with DOTS approach.
    /// </summary>
    public static class DebugService
    {
        #region Properties

        public static bool IsDebugEnabled => DebugLoggingService.IsDebugEnabled;

        public static string DebugStatus => DebugLoggingService.DebugStatus;

        #endregion

        #region Public Methods

        /// <summary>
        /// Enable debug logging
        /// </summary>
        public static void EnableDebugLogging()
        {
            DebugLoggingService.EnableDebugLogging();
        }

        /// <summary>
        /// Disable debug logging
        /// </summary>
        public static void DisableDebugLogging()
        {
            DebugLoggingService.DisableDebugLogging();
        }

        /// <summary>
        /// Toggle debug logging
        /// </summary>
        public static void ToggleDebugLogging()
        {
            DebugLoggingService.ToggleDebugLogging();
        }
        
        /// <summary>
        /// Test all log levels
        /// </summary>
        public static void TestLogLevels()
        {
            Debug.Log("========================================");
            Debug.Log($"[TEST] Testing Log Levels (Mode: {BuildProfileHelper.ConfigurationMode})");
            Debug.Log($"[TEST] Debug Status: {(IsDebugEnabled ? "ENABLED" : "DISABLED")}");
            Debug.Log("========================================");

            // These always show - they're regular Unity Debug calls
            Debug.Log("[UNITY] Regular Unity Debug.Log - ALWAYS VISIBLE");
            Debug.LogWarning("[UNITY] Regular Unity Debug.LogWarning - ALWAYS VISIBLE");
            Debug.LogError("[UNITY] Regular Unity Debug.LogError - ALWAYS VISIBLE");

            Debug.Log("----------------------------------------");

#if PERSPEC_DEBUG
            Debug.Log("[PERSPEC] Debug is ENABLED - Testing PerSpecDebug calls:");

            // These are conditional - only compile and run when PERSPEC_DEBUG is defined
            PerSpecDebug.Log("[PERSPEC] PerSpecDebug.Log - Visible only when debug enabled");
            PerSpecDebug.LogWarning("[PERSPEC] PerSpecDebug.LogWarning - Visible only when debug enabled");
            PerSpecDebug.LogError("[PERSPEC] PerSpecDebug.LogError - Visible only when debug enabled");
            PerSpecDebug.LogTestSetup("Test setup phase");
            PerSpecDebug.LogTestAction("Test action phase");
            PerSpecDebug.LogTestAssert("Test assert phase");
            PerSpecDebug.LogTestComplete("Test complete phase");
            PerSpecDebug.LogFeatureStart("TestFeature", "Feature starting");
            PerSpecDebug.LogFeatureComplete("TestFeature", "Feature completed");

            Debug.Log("[PERSPEC] All PerSpecDebug calls executed successfully");
#else
            Debug.Log("[PERSPEC] Debug is DISABLED - All PerSpecDebug calls are STRIPPED from code");
            Debug.Log("[PERSPEC] To see PerSpecDebug messages, enable debug logging and recompile");
            Debug.Log("[PERSPEC] You should only see the Unity Debug.Log messages above");
#endif

            Debug.Log("========================================");
        }

        #endregion
    }
}