using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace PerSpec.Runtime.Logging
{
    /// <summary>
    /// Runtime log capture for PlayMode tests.
    /// Captures all Debug.Log calls during PlayMode and stores them to disk.
    /// Performance-optimized version with file-based logging.
    /// </summary>
    public class PlayModeLogCapture : MonoBehaviour
    {
        private static PlayModeLogCapture _instance;
        private readonly Queue<LogEntry> _logQueue = new Queue<LogEntry>();
        private readonly object _queueLock = new object();
        private bool _isCapturing = false;
        
        // File writing
        private string _logDirectory;
        private string _sessionId;
        private int _batchNumber = 0;
        private StreamWriter _currentWriter;
        private readonly StringBuilder _stringBuilder = new StringBuilder(4096);
        
        // Performance optimization - pre-allocated and reused
        private readonly List<LogEntry> _processingList = new List<LogEntry>(1000);
        
        public static PlayModeLogCapture Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[PlayModeLogCapture]");
                    _instance = go.AddComponent<PlayModeLogCapture>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        public static void Initialize(string databasePath, string sessionId)
        {
            var instance = Instance;
            instance._sessionId = sessionId ?? DateTime.Now.Ticks.ToString();
            instance.InitializeLogging();
            instance.StartCapture();
        }
        
        private void InitializeLogging()
        {
            // Setup log directory
            var projectRoot = Directory.GetCurrentDirectory();
            _logDirectory = Path.Combine(projectRoot, "PerSpec", "PlayModeLogs");
            
            // Clear directory on Play Mode enter
            if (Directory.Exists(_logDirectory))
            {
                try
                {
                    foreach (var file in Directory.GetFiles(_logDirectory))
                    {
                        File.Delete(file);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[PlayModeLogCapture] Could not clear log directory: {e.Message}");
                }
            }
            else
            {
                Directory.CreateDirectory(_logDirectory);
            }
            
            Debug.Log($"[PlayModeLogCapture] Initialized logging to: {_logDirectory}");
        }
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private void StartCapture()
        {
            if (_isCapturing) return;
            
            Application.logMessageReceived += OnLogMessageReceived;
            Application.logMessageReceivedThreaded += OnLogMessageReceivedThreaded;
            _isCapturing = true;
            
            // Start writing every 5 seconds
            InvokeRepeating(nameof(WriteLogBatch), 5f, 5f);
            
            Debug.Log($"[PlayModeLogCapture] Started capturing logs for session: {_sessionId}");
        }
        
        private void StopCapture()
        {
            if (!_isCapturing) return;
            
            Application.logMessageReceived -= OnLogMessageReceived;
            Application.logMessageReceivedThreaded -= OnLogMessageReceivedThreaded;
            _isCapturing = false;
            
            // Cancel repeated invocation
            CancelInvoke(nameof(WriteLogBatch));
            
            // Write final batch
            WriteFinalLogs();
            
            Debug.Log("[PlayModeLogCapture] Stopped capturing logs");
        }
        
        private void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            CaptureLog(logString, stackTrace, type, false);
        }
        
        private void OnLogMessageReceivedThreaded(string logString, string stackTrace, LogType type)
        {
            CaptureLog(logString, stackTrace, type, true);
        }
        
        private void CaptureLog(string logString, string stackTrace, LogType type, bool isThreaded)
        {
            var entry = new LogEntry
            {
                Message = logString,
                StackTrace = stackTrace,
                LogType = type,
                Timestamp = DateTime.Now,
                IsThreaded = isThreaded,
                FrameCount = isThreaded ? -1 : Time.frameCount
            };
            
            lock (_queueLock)
            {
                _logQueue.Enqueue(entry);
                
                // Prevent memory overflow
                const int MAX_QUEUE_SIZE = 10000;
                if (_logQueue.Count > MAX_QUEUE_SIZE)
                {
                    // Drop oldest logs
                    var toRemove = _logQueue.Count - MAX_QUEUE_SIZE;
                    for (int i = 0; i < toRemove; i++)
                    {
                        _logQueue.Dequeue();
                    }
                }
            }
        }
        
        private void WriteLogBatch()
        {
            // Early exit if no logs
            lock (_queueLock)
            {
                if (_logQueue.Count == 0) return;
                
                // Move logs to processing list
                _processingList.Clear();
                while (_logQueue.Count > 0)
                {
                    _processingList.Add(_logQueue.Dequeue());
                }
            }
            
            // Write to file (outside of lock)
            WriteLogsToFile(_processingList, false);
        }
        
        private void WriteFinalLogs()
        {
            lock (_queueLock)
            {
                if (_logQueue.Count == 0) return;
                
                _processingList.Clear();
                while (_logQueue.Count > 0)
                {
                    _processingList.Add(_logQueue.Dequeue());
                }
            }
            
            // Write final batch
            WriteLogsToFile(_processingList, true);
        }
        
        private void WriteLogsToFile(List<LogEntry> logs, bool isFinal)
        {
            if (logs.Count == 0 || string.IsNullOrEmpty(_logDirectory)) return;
            
            try
            {
                _batchNumber++;
                var fileName = isFinal 
                    ? $"session_{_sessionId}_final.txt"
                    : $"session_{_sessionId}_batch_{_batchNumber:D3}.txt";
                
                var filePath = Path.Combine(_logDirectory, fileName);
                
                // Use StringBuilder for efficient string concatenation
                _stringBuilder.Clear();
                _stringBuilder.AppendLine($"=== PlayMode Log Batch - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                _stringBuilder.AppendLine($"Session: {_sessionId}");
                _stringBuilder.AppendLine($"Batch: {_batchNumber}");
                _stringBuilder.AppendLine($"Count: {logs.Count}");
                _stringBuilder.AppendLine("=" + new string('=', 60));
                _stringBuilder.AppendLine();
                
                foreach (var log in logs)
                {
                    // Format: [timestamp] [level] [frame] message
                    _stringBuilder.AppendFormat("[{0:HH:mm:ss.fff}] [{1,-9}] ",
                        log.Timestamp,
                        ConvertLogType(log.LogType));
                    
                    if (log.FrameCount >= 0)
                    {
                        _stringBuilder.AppendFormat("[Frame:{0,5}] ", log.FrameCount);
                    }
                    else
                    {
                        _stringBuilder.Append("[Thread    ] ");
                    }
                    
                    _stringBuilder.AppendLine(log.Message);
                    
                    // Add stack trace for errors and exceptions
                    if (!string.IsNullOrEmpty(log.StackTrace) && 
                        (log.LogType == LogType.Error || log.LogType == LogType.Exception || log.LogType == LogType.Assert))
                    {
                        // Indent stack trace
                        var stackLines = log.StackTrace.Split('\n');
                        foreach (var line in stackLines)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                _stringBuilder.AppendLine($"    {line.Trim()}");
                            }
                        }
                    }
                    
                    _stringBuilder.AppendLine();
                }
                
                // Write to file
                File.WriteAllText(filePath, _stringBuilder.ToString());
                
                // Log success (but don't capture this log!)
                if (isFinal)
                {
                    Debug.Log($"[PlayModeLogCapture] Wrote final logs to: {fileName}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayModeLogCapture] Failed to write logs: {e.Message}");
            }
        }
        
        private string ConvertLogType(LogType logType)
        {
            switch (logType)
            {
                case LogType.Log: return "Info";
                case LogType.Warning: return "Warning";
                case LogType.Error: return "Error";
                case LogType.Exception: return "Exception";
                case LogType.Assert: return "Assert";
                default: return "Unknown";
            }
        }
        
        private void OnDestroy()
        {
            StopCapture();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // Write logs when pausing
                WriteLogBatch();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                // Write logs when losing focus
                WriteLogBatch();
            }
        }
        
        [Serializable]
        private class LogEntry
        {
            public string Message;
            public string StackTrace;
            public LogType LogType;
            public DateTime Timestamp;
            public bool IsThreaded;
            public int FrameCount;
        }
    }
}