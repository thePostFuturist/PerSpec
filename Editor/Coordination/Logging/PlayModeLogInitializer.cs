using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using PerSpec.Runtime.Logging;
using SQLite;

namespace PerSpec.Editor.Coordination
{
    /// <summary>
    /// Initializes PlayMode log capture and handles log data transfer from runtime to database.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeLogInitializer
    {
        private static SQLiteManager _dbManager;
        private static bool _isProcessingLogs = false;
        private static float _lastProcessTime = 0f;
        private const float PROCESS_INTERVAL = 0.5f; // Process logs every 0.5 seconds
        
        static PlayModeLogInitializer()
        {
            Initialize();
        }
        
        private static void Initialize()
        {
            // Subscribe to play mode state changes
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            // Subscribe to update for log processing
            EditorApplication.update += ProcessPlayModeLogs;
            
            // Initialize database manager
            if (SQLiteManager.IsPerSpecInitialized())
            {
                _dbManager = new SQLiteManager();
            }
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    InitializePlayModeCapture();
                    break;
                    
                case PlayModeStateChange.ExitingPlayMode:
                    // Process any remaining logs before exiting
                    ProcessAllRemainingLogs();
                    break;
                    
                case PlayModeStateChange.EnteredEditMode:
                    // Clean up after returning to edit mode
                    CleanupPlayModeLogs();
                    break;
            }
        }
        
        private static void InitializePlayModeCapture()
        {
            if (_dbManager == null || !_dbManager.IsInitialized)
            {
                Debug.LogWarning("[PlayModeLogInitializer] Database not initialized, PlayMode logging disabled");
                return;
            }
            
            try
            {
                // Get database path
                var dbPath = Path.Combine(Application.dataPath, "..", "PerSpec", "test_coordination.db");
                dbPath = Path.GetFullPath(dbPath);
                
                // Get current session ID
                var sessionId = UnityConsoleSessionManager.CurrentSessionId;
                
                // Create initialization script that will run in PlayMode
                var initCode = $@"
                    var dbPath = @""{dbPath.Replace("\\", "\\\\")}"";
                    var sessionId = ""{sessionId}"";
                    PerSpec.Runtime.Logging.PlayModeLogCapture.Initialize(dbPath, sessionId);
                ";
                
                // Execute initialization in PlayMode context
                EditorApplication.delayCall += () =>
                {
                    if (EditorApplication.isPlaying)
                    {
                        // Use reflection to execute in runtime context
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
                                    initMethod.Invoke(null, new object[] { dbPath, sessionId });
                                    Debug.Log("[PlayModeLogInitializer] PlayMode log capture initialized");
                                    _isProcessingLogs = true;
                                    return;
                                }
                            }
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayModeLogInitializer] Failed to initialize PlayMode capture: {ex.Message}");
            }
        }
        
        private static void ProcessPlayModeLogs()
        {
            if (!_isProcessingLogs || !EditorApplication.isPlaying)
                return;
            
            // Throttle processing to avoid performance impact
            if (Time.realtimeSinceStartup - _lastProcessTime < PROCESS_INTERVAL)
                return;
            
            _lastProcessTime = Time.realtimeSinceStartup;
            
            ProcessStoredLogs();
        }
        
        private static void ProcessStoredLogs()
        {
            if (_dbManager == null || !_dbManager.IsInitialized)
                return;
            
            try
            {
                // Read log keys from PlayerPrefs
                var keyList = PlayerPrefs.GetString("PlayModeLogKeys", "");
                if (string.IsNullOrEmpty(keyList))
                    return;
                
                var keys = keyList.Split('|');
                var processedKeys = new List<string>();
                var remainingKeys = new List<string>();
                
                foreach (var key in keys)
                {
                    if (string.IsNullOrEmpty(key))
                        continue;
                    
                    var json = PlayerPrefs.GetString(key, "");
                    if (!string.IsNullOrEmpty(json))
                    {
                        try
                        {
                            var logData = JsonUtility.FromJson<PlayModeLogCapture.PlayModeLogData>(json);
                            if (logData != null)
                            {
                                SaveLogToDatabase(logData);
                                processedKeys.Add(key);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[PlayModeLogInitializer] Failed to process log: {ex.Message}");
                            processedKeys.Add(key); // Remove corrupted entries
                        }
                    }
                    else
                    {
                        processedKeys.Add(key); // Remove empty entries
                    }
                }
                
                // Clean up processed keys
                foreach (var key in processedKeys)
                {
                    PlayerPrefs.DeleteKey(key);
                }
                
                // Update key list with remaining keys
                foreach (var key in keys)
                {
                    if (!processedKeys.Contains(key))
                    {
                        remainingKeys.Add(key);
                    }
                }
                
                if (remainingKeys.Count > 0)
                {
                    PlayerPrefs.SetString("PlayModeLogKeys", string.Join("|", remainingKeys));
                }
                else
                {
                    PlayerPrefs.DeleteKey("PlayModeLogKeys");
                }
                
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayModeLogInitializer] Error processing stored logs: {ex.Message}");
            }
        }
        
        private static void SaveLogToDatabase(PlayModeLogCapture.PlayModeLogData logData)
        {
            try
            {
                var entry = new ConsoleLogEntry
                {
                    SessionId = logData.SessionId,
                    LogLevel = logData.LogLevel,
                    Message = logData.Message,
                    StackTrace = logData.StackTrace,
                    Timestamp = new DateTime(logData.Timestamp),
                    FrameCount = logData.FrameCount,
                    Context = logData.Context,
                    // Extract source info from stack trace if available
                    SourceFile = ExtractSourceFile(logData.StackTrace),
                    SourceLine = ExtractSourceLine(logData.StackTrace)
                };
                
                // Truncate stack trace for storage
                if (!string.IsNullOrEmpty(entry.StackTrace) && entry.StackTrace.Length > 500)
                {
                    entry.TruncatedStack = entry.StackTrace.Substring(0, 500);
                    entry.IsTruncated = true;
                }
                
                _dbManager.SaveConsoleLog(entry);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayModeLogInitializer] Failed to save log to database: {ex.Message}");
            }
        }
        
        private static string ExtractSourceFile(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
                return null;
            
            // Look for pattern like "at ClassName.Method (at FilePath:LineNumber)"
            var match = System.Text.RegularExpressions.Regex.Match(stackTrace, 
                @"\(at ([^:]+):(\d+)\)");
            
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            
            return null;
        }
        
        private static int? ExtractSourceLine(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
                return null;
            
            var match = System.Text.RegularExpressions.Regex.Match(stackTrace, 
                @"\(at [^:]+:(\d+)\)");
            
            if (match.Success && int.TryParse(match.Groups[1].Value, out int line))
            {
                return line;
            }
            
            return null;
        }
        
        private static void ProcessAllRemainingLogs()
        {
            _isProcessingLogs = false;
            
            // Process any remaining logs multiple times to ensure we get everything
            for (int i = 0; i < 5; i++)
            {
                ProcessStoredLogs();
            }
        }
        
        private static void CleanupPlayModeLogs()
        {
            // Clean up any remaining PlayerPrefs keys
            PlayerPrefs.DeleteKey("PlayModeLogKeys");
            
            var keyList = PlayerPrefs.GetString("PlayModeLogKeys", "");
            if (!string.IsNullOrEmpty(keyList))
            {
                var keys = keyList.Split('|');
                foreach (var key in keys)
                {
                    if (!string.IsNullOrEmpty(key))
                    {
                        PlayerPrefs.DeleteKey(key);
                    }
                }
                PlayerPrefs.DeleteKey("PlayModeLogKeys");
            }
            
            PlayerPrefs.Save();
            
            Debug.Log("[PlayModeLogInitializer] PlayMode log cleanup complete");
        }
    }
}