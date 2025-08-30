using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;

namespace PerSpec.Editor.Services
{
    /// <summary>
    /// Service for managing debug settings and logging
    /// </summary>
    public static class DebugService
    {
        #region Constants
        
        private const string PERSPEC_DEBUG_SYMBOL = "PERSPEC_DEBUG";
        
        #endregion
        
        #region Properties
        
        public static bool IsDebugEnabled
        {
            get
            {
                return BuildProfileHelper.HasCompilerDirective(PERSPEC_DEBUG_SYMBOL);
            }
        }
        
        public static string DebugStatus => IsDebugEnabled 
            ? $"Enabled - Debug logs included ({BuildProfileHelper.ConfigurationMode})" 
            : $"Disabled - Debug logs stripped ({BuildProfileHelper.ConfigurationMode})";
            
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Enable debug logging
        /// </summary>
        public static void EnableDebugLogging()
        {
            SetDebugEnabled(true);
        }
        
        /// <summary>
        /// Disable debug logging
        /// </summary>
        public static void DisableDebugLogging()
        {
            SetDebugEnabled(false);
        }
        
        /// <summary>
        /// Toggle debug logging
        /// </summary>
        public static void ToggleDebugLogging()
        {
            SetDebugEnabled(!IsDebugEnabled);
        }
        
        /// <summary>
        /// Test all log levels
        /// </summary>
        public static void TestLogLevels()
        {
            Debug.Log("========================================");
            Debug.Log($"[TEST] Testing Log Levels (Mode: {BuildProfileHelper.ConfigurationMode})");
            Debug.Log("========================================");
            
            // These always show - they're regular Unity Debug calls
            Debug.Log("[UNITY] This is a regular Unity Debug.Log - ALWAYS VISIBLE");
            Debug.LogWarning("[UNITY] This is a Unity Debug.LogWarning - ALWAYS VISIBLE");
            Debug.LogError("[UNITY] This is a Unity Debug.LogError - ALWAYS VISIBLE");
            
            Debug.Log("----------------------------------------");
            
            // These are conditional - only show when PERSPEC_DEBUG is defined
#if PERSPEC_DEBUG
            Debug.Log("[PERSPEC] Debug is ENABLED - The following PerSpecDebug calls ARE compiled:");
            // Note: These will only compile and run when PERSPEC_DEBUG is defined
            Debug.Log("[PERSPEC] PerSpecDebug.Log would appear here - ONLY when debug enabled");
            Debug.Log("[PERSPEC] PerSpecDebug.LogTestSetup would appear here - ONLY when debug enabled");
            Debug.Log("[PERSPEC] PerSpecDebug.LogTestComplete would appear here - ONLY when debug enabled");
            Debug.Log("[PERSPEC] PerSpecDebug.LogError would appear here - ONLY when debug enabled");
#else
            Debug.Log("[PERSPEC] Debug is DISABLED - All PerSpecDebug calls are STRIPPED from code");
            Debug.Log("[PERSPEC] To see PerSpecDebug messages, enable debug logging and recompile");
#endif
            
            Debug.Log("========================================");
            Debug.Log($"[TEST] Current Status: Debug is {(IsDebugEnabled ? "ENABLED" : "DISABLED")}");
            Debug.Log("========================================");
        }
        
        #endregion
        
        #region Private Methods
        
        private static void SetDebugEnabled(bool enabled)
        {
            if (enabled)
                BuildProfileHelper.AddCompilerDirective(PERSPEC_DEBUG_SYMBOL);
            else
                BuildProfileHelper.RemoveCompilerDirective(PERSPEC_DEBUG_SYMBOL);
            
            Debug.Log($"[PerSpec] Debug logging {(enabled ? "ENABLED" : "DISABLED")} via {BuildProfileHelper.ConfigurationMode}. Recompiling scripts...");
        }
        
        // Symbol manipulation methods removed - now handled by BuildProfileHelper
        
        #endregion
    }
}