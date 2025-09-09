using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using SQLite;

namespace PerSpec.Editor.Coordination
{
    /// <summary>
    /// Robust logging handler that intercepts Unity's Debug.Log at the lowest level
    /// Maintains functionality even during compilation errors
    /// </summary>
    [InitializeOnLoad]
    public class RobustLogHandler : ILogHandler
    {
        private static RobustLogHandler _instance;
        private static readonly object _instanceLock = new object();
        
        private readonly ILogHandler _defaultLogHandler;
        private readonly LogBuffer _persistentBuffer;
        private readonly CompilationStateTracker _compilationTracker;
        private SQLiteManager _dbManager;
        
        private bool _isCompiling = false;
        private bool _hasCompilationErrors = false;
        private readonly Queue<BufferedLog> _compilationBuffer = new Queue<BufferedLog>();
        private readonly object _bufferLock = new object();
        
        private const int MAX_BUFFER_SIZE = 10000;
        private const string PERSISTENT_LOG_KEY = "PerSpec_PersistentLogs";
        private const string COMPILATION_STATE_KEY = "PerSpec_CompilationState";
        
        static RobustLogHandler()
        {
            InitializeHandler();
        }
        
        private static void InitializeHandler()
        {
            lock (_instanceLock)
            {
                if (_instance == null)
                {
                    _instance = new RobustLogHandler();
                    _instance.Initialize();
                }
            }
        }
        
        private RobustLogHandler()
        {
            _defaultLogHandler = Debug.unityLogger.logHandler;
            _persistentBuffer = new LogBuffer();
            _compilationTracker = new CompilationStateTracker();
        }
        
        private void Initialize()
        {
            try
            {
                // Check if PerSpec is initialized
                if (!SQLiteManager.IsPerSpecInitialized())
                {
                    // Store logs in buffer until database is ready
                    RestorePersistentLogs();
                    return;
                }
                
                _dbManager = new SQLiteManager();
                if (!_dbManager.IsInitialized)
                {
                    RestorePersistentLogs();
                    return;
                }
                
                // Hook into Unity's logger at the lowest level
                Debug.unityLogger.logHandler = this;
                
                // Subscribe to compilation events
                CompilationPipeline.compilationStarted += OnCompilationStarted;
                CompilationPipeline.compilationFinished += OnCompilationFinished;
                CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
                
                // Subscribe to domain reload events
                AssemblyReloadEvents.beforeAssemblyReload += OnBeforeDomainReload;
                AssemblyReloadEvents.afterAssemblyReload += OnAfterDomainReload;
                
                // Subscribe to play mode state changes
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                
                // Process any buffered logs from previous session
                RestorePersistentLogs();
                ProcessBufferedLogs();
                
                // Regular processing
                EditorApplication.update += ProcessLogBuffer;
                
                Debug.Log("[RobustLogHandler] Initialized with ILogHandler interception - EditMode compilation logs will be captured");
            }
            catch (Exception ex)
            {
                // Fall back to default handler
                Debug.unityLogger.logHandler = _defaultLogHandler;
                _defaultLogHandler.LogException(ex, null);
            }
        }
        
        #region ILogHandler Implementation
        
        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            string message = args != null && args.Length > 0 ? string.Format(format, args) : format;
            
            // Always pass through to default handler to maintain console parity
            _defaultLogHandler?.LogFormat(logType, context, format, args);
            
            // Capture the log
            CaptureLog(message, null, logType, context);
        }
        
        public void LogException(Exception exception, UnityEngine.Object context)
        {
            // Always pass through to default handler
            _defaultLogHandler?.LogException(exception, context);
            
            // Capture the exception
            CaptureLog(exception.Message, exception.StackTrace, LogType.Exception, context);
        }
        
        #endregion
        
        #region Log Capture
        
        private void CaptureLog(string message, string stackTrace, LogType logType, UnityEngine.Object context)
        {
            try
            {
                // Also forward to Application.logMessageReceived for any dependent systems
                // This ensures compatibility with systems that rely on Application.logMessageReceived
                // Note: We use a direct delegate call to avoid infinite recursion
                try
                {
                    var logMessageReceivedField = typeof(Application).GetField("logMessageReceived", 
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    if (logMessageReceivedField != null)
                    {
                        var handler = logMessageReceivedField.GetValue(null) as Application.LogCallback;
                        handler?.Invoke(message, stackTrace ?? "", logType);
                    }
                }
                catch
                {
                    // Silently ignore if we can't forward
                }
                
                var log = new BufferedLog
                {
                    Message = message,
                    StackTrace = stackTrace ?? GetStackTrace(),
                    LogType = logType,
                    Timestamp = DateTime.Now,
                    ThreadId = Thread.CurrentThread.ManagedThreadId,
                    Context = context?.name,
                    IsCompilationLog = _isCompiling,
                    HasCompilationErrors = _hasCompilationErrors
                };
                
                lock (_bufferLock)
                {
                    if (_isCompiling || _dbManager == null || !_dbManager.IsInitialized)
                    {
                        // Buffer during compilation or when database isn't ready
                        _compilationBuffer.Enqueue(log);
                        
                        // Prevent memory overflow
                        while (_compilationBuffer.Count > MAX_BUFFER_SIZE)
                        {
                            _compilationBuffer.Dequeue();
                        }
                        
                        // Also persist to EditorPrefs for crash recovery
                        PersistCriticalLogs(log);
                    }
                    else
                    {
                        // Direct to database when stable
                        SaveLogToDatabase(log);
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently fail to avoid recursion
                System.Diagnostics.Debug.WriteLine($"[RobustLogHandler] Capture failed: {ex.Message}");
            }
        }
        
        private string GetStackTrace()
        {
            // Get current stack trace, filtering out logging infrastructure
            var stackTrace = new System.Diagnostics.StackTrace(3, true);
            var sb = new StringBuilder();
            
            foreach (var frame in stackTrace.GetFrames())
            {
                var method = frame.GetMethod();
                if (method == null) continue;
                
                var declaringType = method.DeclaringType?.FullName ?? "Unknown";
                
                // Skip logging infrastructure
                if (declaringType.Contains("RobustLogHandler") ||
                    declaringType.Contains("UnityEngine.Debug") ||
                    declaringType.Contains("UnityEngine.Logger"))
                    continue;
                
                var fileName = frame.GetFileName();
                var lineNumber = frame.GetFileLineNumber();
                
                if (!string.IsNullOrEmpty(fileName))
                {
                    sb.AppendLine($"  at {declaringType}.{method.Name} (at {fileName}:{lineNumber})");
                }
                else
                {
                    sb.AppendLine($"  at {declaringType}.{method.Name}");
                }
            }
            
            return sb.ToString();
        }
        
        #endregion
        
        #region Compilation Events
        
        private void OnCompilationStarted(object obj)
        {
            _isCompiling = true;
            _hasCompilationErrors = false;
            
            Debug.Log("[RobustLogHandler] Compilation started - buffering logs");
            
            // Save current state
            EditorPrefs.SetBool(COMPILATION_STATE_KEY, true);
        }
        
        private void OnCompilationFinished(object obj)
        {
            _isCompiling = false;
            
            Debug.Log(_hasCompilationErrors 
                    ? "[RobustLogHandler] Compilation finished with errors - processing buffered logs" 
                    : "[RobustLogHandler] Compilation finished successfully - processing buffered logs");
            
            // Clear compilation state
            EditorPrefs.SetBool(COMPILATION_STATE_KEY, false);
            
            // Process all buffered logs
            ProcessBufferedLogs();
        }
        
        private void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            foreach (var message in messages)
            {
                if (message.type == CompilerMessageType.Error)
                {
                    _hasCompilationErrors = true;
                    
                    // Format compilation error to match Unity's standard format with CS error codes
                    // This ensures SQLiteManager.HasCompilationErrors() can detect them with '%error CS%' pattern
                    var errorLog = new BufferedLog
                    {
                        Message = $"{message.file}({message.line},{message.column}): error {message.message}",
                        StackTrace = null,
                        LogType = LogType.Error,
                        Timestamp = DateTime.Now,
                        ThreadId = Thread.CurrentThread.ManagedThreadId,
                        Context = "Compilation",
                        IsCompilationLog = true,
                        HasCompilationErrors = true
                    };
                    
                    lock (_bufferLock)
                    {
                        _compilationBuffer.Enqueue(errorLog);
                    }
                    
                    // Immediately save compilation errors to database for real-time detection
                    SaveLogToDatabase(errorLog);
                }
            }
        }
        
        #endregion
        
        #region Domain Reload Events
        
        private void OnBeforeDomainReload()
        {
            // Persist all buffered logs before domain reload
            PersistAllBufferedLogs();
            
            // Restore default handler temporarily
            if (Debug.unityLogger.logHandler == this)
            {
                Debug.unityLogger.logHandler = _defaultLogHandler;
            }
        }
        
        private void OnAfterDomainReload()
        {
            // Re-initialize after domain reload
            InitializeHandler();
        }
        
        #endregion
        
        #region Play Mode Events
        
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    LogFormat(LogType.Log, null, "[RobustLogHandler] Entering Play Mode - Editor logging will continue");
                    PersistAllBufferedLogs();
                    // Keep the handler active during PlayMode for Editor-side logs
                    break;
                    
                case PlayModeStateChange.EnteredPlayMode:
                    // PlayMode runtime logging is handled by PlayModeLogCapture
                    LogFormat(LogType.Log, null, "[RobustLogHandler] PlayMode active - Runtime logs handled by PlayModeLogCapture");
                    break;
                    
                case PlayModeStateChange.ExitingPlayMode:
                    LogFormat(LogType.Log, null, "[RobustLogHandler] Exiting Play Mode");
                    // Ensure any buffered logs are processed
                    ProcessBufferedLogs();
                    break;
                    
                case PlayModeStateChange.EnteredEditMode:
                    LogFormat(LogType.Log, null, "[RobustLogHandler] Returned to Edit Mode");
                    RestorePersistentLogs();
                    // Re-establish as primary log handler
                    if (Debug.unityLogger.logHandler != this)
                    {
                        Debug.unityLogger.logHandler = this;
                    }
                    break;
            }
        }
        
        #endregion
        
        #region Buffer Processing
        
        private void ProcessLogBuffer()
        {
            if (_isCompiling || _compilationBuffer.Count == 0)
                return;
            
            ProcessBufferedLogs();
        }
        
        private void ProcessBufferedLogs()
        {
            List<BufferedLog> logsToProcess = null;
            
            lock (_bufferLock)
            {
                if (_compilationBuffer.Count > 0)
                {
                    logsToProcess = new List<BufferedLog>(_compilationBuffer);
                    _compilationBuffer.Clear();
                }
            }
            
            if (logsToProcess != null && logsToProcess.Count > 0)
            {
                foreach (var log in logsToProcess)
                {
                    SaveLogToDatabase(log);
                }
                
                // Clear persistent storage after successful processing
                EditorPrefs.DeleteKey(PERSISTENT_LOG_KEY);
            }
        }
        
        private void SaveLogToDatabase(BufferedLog log)
        {
            if (_dbManager == null || !_dbManager.IsInitialized)
            {
                // Re-queue if database isn't ready
                lock (_bufferLock)
                {
                    _compilationBuffer.Enqueue(log);
                }
                return;
            }
            
            try
            {
                var entry = new ConsoleLogEntry
                {
                    SessionId = SessionTracker.CurrentSessionId,
                    LogLevel = ConvertLogType(log.LogType),
                    Message = log.Message,
                    StackTrace = log.StackTrace,
                    Timestamp = log.Timestamp,
                    Context = $"{log.Context ?? ""}|Thread:{log.ThreadId}|Compilation:{log.IsCompilationLog}|Errors:{log.HasCompilationErrors}"
                };
                
                _dbManager.SaveConsoleLog(entry);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RobustLogHandler] Database save failed: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Persistence
        
        private void PersistCriticalLogs(BufferedLog log)
        {
            if (log.LogType == LogType.Error || log.LogType == LogType.Exception || log.IsCompilationLog)
            {
                _persistentBuffer.AddLog(log);
                
                // Only persist most recent critical logs to avoid EditorPrefs bloat
                var recentLogs = _persistentBuffer.GetRecentLogs(100);
                var json = JsonUtility.ToJson(new SerializableLogList { Logs = recentLogs });
                EditorPrefs.SetString(PERSISTENT_LOG_KEY, json);
            }
        }
        
        private void PersistAllBufferedLogs()
        {
            lock (_bufferLock)
            {
                if (_compilationBuffer.Count > 0)
                {
                    var allLogs = new List<BufferedLog>(_compilationBuffer);
                    var json = JsonUtility.ToJson(new SerializableLogList { Logs = allLogs });
                    EditorPrefs.SetString(PERSISTENT_LOG_KEY, json);
                }
            }
        }
        
        private void RestorePersistentLogs()
        {
            var json = EditorPrefs.GetString(PERSISTENT_LOG_KEY, null);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var logList = JsonUtility.FromJson<SerializableLogList>(json);
                    if (logList?.Logs != null)
                    {
                        lock (_bufferLock)
                        {
                            foreach (var log in logList.Logs)
                            {
                                _compilationBuffer.Enqueue(log);
                            }
                        }
                        
                        LogFormat(LogType.Log, null, $"[RobustLogHandler] Restored {logList.Logs.Count} persistent logs");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[RobustLogHandler] Failed to restore logs: {ex.Message}");
                }
            }
        }
        
        #endregion
        
        #region Utilities
        
        private string ConvertLogType(LogType logType)
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
        
        public static void ForceFlush()
        {
            _instance?.ProcessBufferedLogs();
        }
        
        public static string GetStatus()
        {
            if (_instance == null)
                return "RobustLogHandler not initialized";
            
            lock (_instance._bufferLock)
            {
                return $"Active: {Debug.unityLogger.logHandler == _instance}\n" +
                       $"Compiling: {_instance._isCompiling}\n" +
                       $"Has Errors: {_instance._hasCompilationErrors}\n" +
                       $"Buffer Size: {_instance._compilationBuffer.Count}\n" +
                       $"Database Ready: {_instance._dbManager?.IsInitialized ?? false}";
            }
        }
        
        #endregion
        
        #region Helper Classes
        
        [Serializable]
        private class BufferedLog
        {
            public string Message;
            public string StackTrace;
            public LogType LogType;
            public DateTime Timestamp;
            public int ThreadId;
            public string Context;
            public bool IsCompilationLog;
            public bool HasCompilationErrors;
        }
        
        [Serializable]
        private class SerializableLogList
        {
            public List<BufferedLog> Logs;
        }
        
        private class LogBuffer
        {
            private readonly List<BufferedLog> _logs = new List<BufferedLog>();
            private readonly object _lock = new object();
            
            public void AddLog(BufferedLog log)
            {
                lock (_lock)
                {
                    _logs.Add(log);
                    
                    // Keep only recent logs
                    while (_logs.Count > 1000)
                    {
                        _logs.RemoveAt(0);
                    }
                }
            }
            
            public List<BufferedLog> GetRecentLogs(int count)
            {
                lock (_lock)
                {
                    var startIndex = Math.Max(0, _logs.Count - count);
                    return _logs.GetRange(startIndex, _logs.Count - startIndex);
                }
            }
        }
        
        private class CompilationStateTracker
        {
            public bool IsCompiling => EditorPrefs.GetBool(COMPILATION_STATE_KEY, false);
            
            public void Reset()
            {
                EditorPrefs.DeleteKey(COMPILATION_STATE_KEY);
            }
        }
        
        private static class SessionTracker
        {
            public static string CurrentSessionId
            {
                get
                {
                    // Use the centralized session manager
                    return UnityConsoleSessionManager.CurrentSessionId;
                }
            }
        }
        
        #endregion
    }
}