using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace PerSpec.Editor.Coordination
{
    /// <summary>
    /// Session-based file logging for EditMode.
    /// Captures all logs directly to files, bypassing database and EditorPrefs.
    /// Keeps only the 3 most recent sessions to prevent disk bloat.
    /// Works reliably even during compilation errors.
    /// </summary>
    [InitializeOnLoad]
    public static class EditModeLogCapture
    {
        private static readonly string SessionId;
        private static readonly string LogDirectory;
        private static readonly string SessionFilePath;
        private static readonly object FileLock = new object();
        private static bool _isCapturing = false;
        private static StreamWriter _writer;
        private static readonly StringBuilder _stringBuilder = new StringBuilder(2048);
        
        private const int MAX_SESSIONS = 3;
        private const int FLUSH_INTERVAL = 100; // Flush every 100 logs
        private static int _logCount = 0;
        
        static EditModeLogCapture()
        {
            try
            {
                // Check if PerSpec is enabled and initialized
                bool isEnabled = EditorPrefs.GetBool("PerSpec_Enabled", true);
                var projectRoot = Directory.GetCurrentDirectory();
                var perspecPath = Path.Combine(projectRoot, "PerSpec");
                if (!isEnabled || !Directory.Exists(perspecPath))
                {
                    return; // Don't initialize if PerSpec is disabled or not initialized
                }

                // Generate unique session ID
                SessionId = DateTime.Now.Ticks.ToString();

                // Setup log directory (reuse projectRoot variable)
                LogDirectory = Path.Combine(projectRoot, "PerSpec", "EditModeLogs");

                // Ensure directory exists
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }

                // Clean old sessions (keep only 3 most recent)
                CleanOldSessions();
                
                // Create session file path
                SessionFilePath = Path.Combine(LogDirectory, $"session_{SessionId}.txt");
                
                // Write session header
                WriteSessionHeader();
                
                // Subscribe to log events
                Application.logMessageReceived += OnLogMessageReceived;
                Application.logMessageReceivedThreaded += OnLogMessageReceivedThreaded;
                
                // Subscribe to editor events for cleanup
                EditorApplication.quitting += OnEditorQuitting;
                AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                
                _isCapturing = true;
                
                // Log initialization
                WriteLog("Info", "[EditModeLogCapture] Session started", null, DateTime.Now);
                Debug.Log($"[EditModeLogCapture] Session {SessionId} started - Logging to: {SessionFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EditModeLogCapture] Failed to initialize: {ex.Message}");
            }
        }
        
        private static void CleanOldSessions()
        {
            try
            {
                var sessionFiles = Directory.GetFiles(LogDirectory, "session_*.txt")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();
                
                // Keep only MAX_SESSIONS most recent files
                if (sessionFiles.Count >= MAX_SESSIONS)
                {
                    var filesToDelete = sessionFiles.Skip(MAX_SESSIONS - 1); // -1 because we're about to create a new one
                    foreach (var file in filesToDelete)
                    {
                        try
                        {
                            file.Delete();
                            Debug.Log($"[EditModeLogCapture] Deleted old session: {file.Name}");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[EditModeLogCapture] Could not delete old session {file.Name}: {ex.Message}");
                        }
                    }
                }
                
                Debug.Log($"[EditModeLogCapture] Session cleanup complete. {sessionFiles.Count} existing sessions found.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EditModeLogCapture] Session cleanup failed: {ex.Message}");
            }
        }
        
        private static void WriteSessionHeader()
        {
            try
            {
                var header = new StringBuilder();
                header.AppendLine("================================================================================");
                header.AppendLine($"EditMode Log Session: {SessionId}");
                header.AppendLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                header.AppendLine($"Unity Version: {Application.unityVersion}");
                header.AppendLine($"Platform: {Application.platform}");
                header.AppendLine("================================================================================");
                header.AppendLine();
                
                File.WriteAllText(SessionFilePath, header.ToString());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EditModeLogCapture] Failed to write session header: {ex.Message}");
            }
        }
        
        private static void OnLogMessageReceived(string message, string stackTrace, LogType logType)
        {
            if (!_isCapturing) return;
            
            // Filter out our own logs to prevent recursion
            if (message.StartsWith("[EditModeLogCapture]")) return;
            
            // Main thread log - write immediately
            WriteLog(ConvertLogType(logType), message, stackTrace, DateTime.Now);
        }
        
        private static void OnLogMessageReceivedThreaded(string message, string stackTrace, LogType logType)
        {
            bool isEnabled = EditorPrefs.GetBool("PerSpec_Enabled", true);
            if (!_isCapturing || !isEnabled) return;
            
            // Filter out our own logs to prevent recursion
            if (message.StartsWith("[EditModeLogCapture]")) return;
            
            // Thread-safe log - write immediately
            WriteLog(ConvertLogType(logType), message, stackTrace, DateTime.Now);
        }
        
        private static void WriteLog(string level, string message, string stackTrace, DateTime timestamp)
        {
            lock (FileLock)
            {
                try
                {
                    _stringBuilder.Clear();
                    
                    // Format: [HH:mm:ss.fff] [Level    ] Message
                    _stringBuilder.Append('[');
                    _stringBuilder.Append(timestamp.ToString("HH:mm:ss.fff"));
                    _stringBuilder.Append("] [");
                    _stringBuilder.Append(level.PadRight(9));
                    _stringBuilder.Append("] ");
                    _stringBuilder.AppendLine(message);
                    
                    // Add stack trace if present
                    if (!string.IsNullOrEmpty(stackTrace))
                    {
                        // Indent stack trace for readability
                        var lines = stackTrace.Split('\n');
                        foreach (var line in lines)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                _stringBuilder.Append("    ");
                                _stringBuilder.AppendLine(line.Trim());
                            }
                        }
                    }
                    
                    // Write to file immediately
                    File.AppendAllText(SessionFilePath, _stringBuilder.ToString());
                    
                    // Increment log count for periodic flushing
                    _logCount++;
                    
                    // Force flush to disk periodically
                    if (_logCount >= FLUSH_INTERVAL)
                    {
                        _logCount = 0;
                        // File.AppendAllText already flushes, but this ensures the file system catches up
                    }
                }
                catch (Exception ex)
                {
                    // Can't log errors about logging - would cause recursion
                    // Silent fail is acceptable here
                    System.Diagnostics.Debug.WriteLine($"[EditModeLogCapture] Write failed: {ex.Message}");
                }
            }
        }
        
        private static string ConvertLogType(LogType logType)
        {
            switch (logType)
            {
                case LogType.Log: return "Info";
                case LogType.Warning: return "Warning";
                case LogType.Error: return "Error";
                case LogType.Exception: return "Exception";
                case LogType.Assert: return "Assert";
                default: return "Info";
            }
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    WriteLog("Info", "[EditModeLogCapture] Entering PlayMode - EditMode logging suspended", null, DateTime.Now);
                    FlushAndClose();
                    _isCapturing = false;
                    break;
                    
                case PlayModeStateChange.EnteredEditMode:
                    _isCapturing = true;
                    WriteLog("Info", "[EditModeLogCapture] Returned to EditMode - EditMode logging resumed", null, DateTime.Now);
                    break;
            }
        }
        
        private static void OnBeforeAssemblyReload()
        {
            WriteLog("Info", "[EditModeLogCapture] Assembly reload starting", null, DateTime.Now);
            FlushAndClose();
        }
        
        private static void OnEditorQuitting()
        {
            WriteLog("Info", "[EditModeLogCapture] Editor quitting", null, DateTime.Now);
            FlushAndClose();
            _isCapturing = false;
        }
        
        private static void FlushAndClose()
        {
            lock (FileLock)
            {
                try
                {
                    if (_writer != null)
                    {
                        _writer.Flush();
                        _writer.Close();
                        _writer = null;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[EditModeLogCapture] Flush failed: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Get the current session ID
        /// </summary>
        public static string GetSessionId()
        {
            return SessionId;
        }
        
        /// <summary>
        /// Get the log directory path
        /// </summary>
        public static string GetLogDirectory()
        {
            return LogDirectory;
        }
        
        /// <summary>
        /// Check if logging is active
        /// </summary>
        public static bool IsCapturing()
        {
            return _isCapturing;
        }
    }
}