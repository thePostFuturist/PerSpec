using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace PerSpec.Editor.Coordination
{
    /// <summary>
    /// Initializes PlayMode log capture.
    /// Simplified version - logs are now written directly to files, no PlayerPrefs needed.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeLogInitializer
    {
        static PlayModeLogInitializer()
        {
            Initialize();
        }
        
        private static void Initialize()
        {
            // Subscribe to play mode state changes
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    InitializePlayModeCapture();
                    break;
                    
                case PlayModeStateChange.ExitingPlayMode:
                    // Logs are automatically flushed by PlayModeLogCapture
                    Debug.Log("[PlayModeLogInitializer] Exiting PlayMode - logs will be saved to PerSpec/PlayModeLogs/");
                    break;
                    
                case PlayModeStateChange.EnteredEditMode:
                    // No cleanup needed - logs are in files
                    NotifyLogsAvailable();
                    break;
            }
        }
        
        private static void InitializePlayModeCapture()
        {
            try
            {
                // Get database path (kept for compatibility, but not used for logs anymore)
                var dbPath = Path.Combine(Application.dataPath, "..", "PerSpec", "test_coordination.db");
                dbPath = Path.GetFullPath(dbPath);
                
                // Get current session ID
                var sessionId = DateTime.Now.Ticks.ToString();
                
                // Use reflection to initialize PlayModeLogCapture in runtime
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    var type = assembly.GetType("PerSpec.Runtime.Logging.PlayModeLogCapture");
                    if (type != null)
                    {
                        var initMethod = type.GetMethod("Initialize", 
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (initMethod != null)
                        {
                            // Initialize with database path (for compatibility) and session ID
                            initMethod.Invoke(null, new object[] { dbPath, sessionId });
                            Debug.Log("[PlayModeLogInitializer] PlayMode log capture initialized - logs will be written to PerSpec/PlayModeLogs/");
                            return;
                        }
                    }
                }
                
                Debug.LogWarning("[PlayModeLogInitializer] Could not find PlayModeLogCapture type");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayModeLogInitializer] Failed to initialize PlayMode capture: {ex.Message}");
            }
        }
        
        private static void NotifyLogsAvailable()
        {
            var logDir = Path.Combine(Application.dataPath, "..", "PerSpec", "PlayModeLogs");
            if (Directory.Exists(logDir))
            {
                var files = Directory.GetFiles(logDir, "*.txt");
                if (files.Length > 0)
                {
                    Debug.Log($"[PlayModeLogInitializer] PlayMode logs available in: {logDir}");
                    Debug.Log($"[PlayModeLogInitializer] Found {files.Length} log file(s). Run 'python PerSpec/Coordination/Scripts/test_playmode_logs.py' to view.");
                }
            }
        }
    }
}