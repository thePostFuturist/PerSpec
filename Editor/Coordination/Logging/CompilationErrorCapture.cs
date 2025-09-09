using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;

namespace PerSpec.Editor.Coordination
{
    /// <summary>
    /// Captures compilation errors directly to the EditMode session log file.
    /// Works independently of the main logging system to ensure compilation errors are always captured.
    /// </summary>
    [InitializeOnLoad]
    public static class CompilationErrorCapture
    {
        private static readonly object FileLock = new object();
        private static readonly StringBuilder _stringBuilder = new StringBuilder(1024);
        private static bool _isCompiling = false;
        private static int _errorCount = 0;
        private static int _warningCount = 0;
        
        static CompilationErrorCapture()
        {
            try
            {
                // Subscribe to compilation events
                CompilationPipeline.compilationStarted += OnCompilationStarted;
                CompilationPipeline.compilationFinished += OnCompilationFinished;
                CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
                
                Debug.Log("[CompilationErrorCapture] Initialized - Compilation errors will be captured to session logs");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CompilationErrorCapture] Failed to initialize: {ex.Message}");
            }
        }
        
        private static void OnCompilationStarted(object obj)
        {
            _isCompiling = true;
            _errorCount = 0;
            _warningCount = 0;
            
            WriteCompilationLog("Info", "[CompilationErrorCapture] Compilation started");
        }
        
        private static void OnCompilationFinished(object obj)
        {
            _isCompiling = false;
            
            var message = _errorCount > 0 
                ? $"[CompilationErrorCapture] Compilation finished with {_errorCount} errors and {_warningCount} warnings"
                : _warningCount > 0
                    ? $"[CompilationErrorCapture] Compilation finished with {_warningCount} warnings"
                    : "[CompilationErrorCapture] Compilation finished successfully";
                    
            WriteCompilationLog(_errorCount > 0 ? "Error" : _warningCount > 0 ? "Warning" : "Info", message);
        }
        
        private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            if (messages == null || messages.Length == 0)
                return;
            
            foreach (var message in messages)
            {
                string level;
                switch (message.type)
                {
                    case CompilerMessageType.Error:
                        level = "Error";
                        _errorCount++;
                        break;
                    case CompilerMessageType.Warning:
                        level = "Warning";
                        _warningCount++;
                        break;
                    default:
                        level = "Info";
                        break;
                }
                
                // Format message to match Unity's standard compilation error format
                // This ensures compatibility with error detection patterns
                var formattedMessage = $"{message.file}({message.line},{message.column}): error {message.message}";
                
                // Write directly to the session log
                WriteCompilationLog(level, formattedMessage);
            }
        }
        
        private static void WriteCompilationLog(string level, string message)
        {
            try
            {
                // Get the current session file from EditModeLogCapture
                var sessionId = EditModeLogCapture.GetSessionId();
                if (string.IsNullOrEmpty(sessionId))
                {
                    // EditModeLogCapture not initialized yet
                    return;
                }
                
                var logDirectory = EditModeLogCapture.GetLogDirectory();
                if (string.IsNullOrEmpty(logDirectory) || !Directory.Exists(logDirectory))
                {
                    // Log directory not ready
                    return;
                }
                
                var sessionFilePath = Path.Combine(logDirectory, $"session_{sessionId}.txt");
                
                lock (FileLock)
                {
                    _stringBuilder.Clear();
                    
                    // Format: [HH:mm:ss.fff] [Level    ] Message
                    var timestamp = DateTime.Now;
                    _stringBuilder.Append('[');
                    _stringBuilder.Append(timestamp.ToString("HH:mm:ss.fff"));
                    _stringBuilder.Append("] [");
                    _stringBuilder.Append(level.PadRight(9));
                    _stringBuilder.Append("] ");
                    _stringBuilder.AppendLine(message);
                    
                    // Write to session file
                    File.AppendAllText(sessionFilePath, _stringBuilder.ToString());
                }
            }
            catch (Exception ex)
            {
                // Can't log errors about logging - would cause recursion
                System.Diagnostics.Debug.WriteLine($"[CompilationErrorCapture] Write failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Check if compilation is currently in progress
        /// </summary>
        public static bool IsCompiling()
        {
            return _isCompiling;
        }
        
        /// <summary>
        /// Get the current error count for the active compilation
        /// </summary>
        public static int GetErrorCount()
        {
            return _errorCount;
        }
        
        /// <summary>
        /// Get the current warning count for the active compilation
        /// </summary>
        public static int GetWarningCount()
        {
            return _warningCount;
        }
    }
}