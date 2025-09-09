using System.Diagnostics;
using UnityEngine;

namespace PerSpec
{
    /// <summary>
    /// Debug logging wrapper for PerSpec framework.
    /// All methods are stripped when PERSPEC_DEBUG symbol is not defined.
    /// Controlled by PERSPEC_DEBUG symbol only - works in editor and builds.
    /// Enable/disable via Tools > PerSpec > Debug Settings in Unity Editor.
    /// </summary>
    public static class PerSpecDebug
    {
        // Cache for uppercase feature names to avoid repeated string allocations
        private static readonly System.Collections.Generic.Dictionary<string, string> _upperCaseCache = 
            new System.Collections.Generic.Dictionary<string, string>(16);
        
        // Pre-formatted strings to reduce allocations
        private const string TestSetupPrefix = "[TEST-SETUP] ";
        private const string TestActionPrefix = "[TEST-ACT] ";
        private const string TestAssertPrefix = "[TEST-ASSERT] ";
        private const string TestCompletePrefix = "[TEST-COMPLETE] ";
        private const string TestErrorPrefix = "[TEST-ERROR] ";
        private const string AssertFailedPrefix = "[ASSERT-FAILED] ";
        #region Log Methods
        
        /// <summary>
        /// Logs a message to the Unity Console (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG")]
        public static void Log(object message)
        {
            UnityEngine.Debug.Log(message);
        }
        
        /// <summary>
        /// Logs a message with context to the Unity Console (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG")]
        public static void Log(object message, Object context)
        {
            UnityEngine.Debug.Log(message, context);
        }
        
        /// <summary>
        /// Logs a formatted message to the Unity Console (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG")]
        public static void LogFormat(string format, params object[] args)
        {
            UnityEngine.Debug.LogFormat(format, args);
        }
        
        /// <summary>
        /// Logs a formatted message with context to the Unity Console (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG")]
        public static void LogFormat(Object context, string format, params object[] args)
        {
            UnityEngine.Debug.LogFormat(context, format, args);
        }
        
        #endregion
        
        #region LogError Methods
        
        /// <summary>
        /// Logs an error message to the Unity Console (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG")]
        public static void LogError(object message)
        {
            UnityEngine.Debug.LogError(message);
        }
        
        /// <summary>
        /// Logs an error message with context to the Unity Console (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG")]
        public static void LogError(object message, Object context)
        {
            UnityEngine.Debug.LogError(message, context);
        }
        
        /// <summary>
        /// Logs a formatted error message to the Unity Console (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG")]
        public static void LogErrorFormat(string format, params object[] args)
        {
            UnityEngine.Debug.LogErrorFormat(format, args);
        }
        
        /// <summary>
        /// Logs a formatted error message with context to the Unity Console (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG")]
        public static void LogErrorFormat(Object context, string format, params object[] args)
        {
            UnityEngine.Debug.LogErrorFormat(context, format, args);
        }
        
        #endregion
        
        #region LogWarning Methods
        
        /// <summary>
        /// Logs a warning message to the Unity Console (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG")]
        public static void LogWarning(object message)
        {
            UnityEngine.Debug.LogWarning(message);
        }
        
        /// <summary>
        /// Logs a warning message with context to the Unity Console (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG")]
        public static void LogWarning(object message, Object context)
        {
            UnityEngine.Debug.LogWarning(message, context);
        }
        
        #endregion
        
        #region Test-Specific Logging
        
        /// <summary>
        /// Logs a test setup message (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG")]
        public static void LogTestSetup(string message)
        {
            UnityEngine.Debug.Log(TestSetupPrefix + message);
        }
        
        /// <summary>
        /// Logs a test action message (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG")]
        public static void LogTestAction(string message)
        {
            UnityEngine.Debug.Log(TestActionPrefix + message);
        }
        
        /// <summary>
        /// Logs a test assertion message (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG")]
        public static void LogTestAssert(string message)
        {
            UnityEngine.Debug.Log(TestAssertPrefix + message);
        }
        
        /// <summary>
        /// Logs a test completion message (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG")]
        public static void LogTestComplete(string message)
        {
            UnityEngine.Debug.Log(TestCompletePrefix + message);
        }
        
        /// <summary>
        /// Logs a test error message (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG")]
        public static void LogTestError(string message)
        {
            UnityEngine.Debug.LogError(TestErrorPrefix + message);
        }
        
        #endregion
        
        #region Feature-Specific Logging
        
        /// <summary>
        /// Logs a feature start message (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG")]
        public static void LogFeatureStart(string feature, string message)
        {
            string upperFeature = GetCachedUpperCase(feature);
            UnityEngine.Debug.Log($"[{upperFeature}-START] {message}");
        }
        
        /// <summary>
        /// Logs a feature progress message (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG")]
        public static void LogFeatureProgress(string feature, string message)
        {
            string upperFeature = GetCachedUpperCase(feature);
            UnityEngine.Debug.Log($"[{upperFeature}-PROGRESS] {message}");
        }
        
        /// <summary>
        /// Logs a feature completion message (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG")]
        public static void LogFeatureComplete(string feature, string message)
        {
            string upperFeature = GetCachedUpperCase(feature);
            UnityEngine.Debug.Log($"[{upperFeature}-COMPLETE] {message}");
        }
        
        /// <summary>
        /// Logs a feature error message (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG")]
        public static void LogFeatureError(string feature, string message)
        {
            string upperFeature = GetCachedUpperCase(feature);
            UnityEngine.Debug.LogError($"[{upperFeature}-ERROR] {message}");
        }
        
        #endregion
        
        #region Assertion Methods
        
        /// <summary>
        /// Assert a condition in debug builds (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG")]
        public static void Assert(bool condition, string message = null)
        {
            if (!condition)
            {
                UnityEngine.Debug.LogError(AssertFailedPrefix + (message ?? "Assertion failed"));
                UnityEngine.Debug.Break();
            }
        }
        
        /// <summary>
        /// Assert a condition with formatted message (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG")]
        public static void AssertFormat(bool condition, string format, params object[] args)
        {
            if (!condition)
            {
                UnityEngine.Debug.LogErrorFormat(AssertFailedPrefix + format, args);
                UnityEngine.Debug.Break();
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Get cached uppercase version of a string to avoid repeated allocations
        /// </summary>
        private static string GetCachedUpperCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;
                
            if (!_upperCaseCache.TryGetValue(input, out string upper))
            {
                upper = input.ToUpper();
                
                // Limit cache size to prevent unbounded growth
                if (_upperCaseCache.Count < 100)
                {
                    _upperCaseCache[input] = upper;
                }
            }
            
            return upper;
        }
        
        #endregion
    }
}