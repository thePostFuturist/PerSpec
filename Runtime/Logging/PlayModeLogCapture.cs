using System;
using System.Collections.Generic;
using UnityEngine;

namespace PerSpec.Runtime.Logging
{
    /// <summary>
    /// Runtime log capture for PlayMode tests.
    /// Captures all Debug.Log calls during PlayMode and stores them for the monitoring system.
    /// </summary>
    public class PlayModeLogCapture : MonoBehaviour
    {
        private static PlayModeLogCapture _instance;
        private readonly Queue<LogEntry> _logQueue = new Queue<LogEntry>();
        private readonly object _queueLock = new object();
        private bool _isCapturing = false;
        
        // Thread-safe frame count caching
        private int _cachedFrameCount = 0;
        private readonly object _frameCountLock = new object();
        
        // SQLite connection info passed from Editor
        private string _databasePath;
        private string _sessionId;
        
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
            instance._databasePath = databasePath;
            instance._sessionId = sessionId;
            instance.StartCapture();
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
            
            // Initialize cached frame count
            try
            {
                lock (_frameCountLock)
                {
                    _cachedFrameCount = Time.frameCount;
                }
            }
            catch
            {
                // If Time.frameCount fails during initialization, start at 0
                lock (_frameCountLock)
                {
                    _cachedFrameCount = 0;
                }
            }
            
            Application.logMessageReceived += OnLogMessageReceived;
            Application.logMessageReceivedThreaded += OnLogMessageReceivedThreaded;
            _isCapturing = true;
            
            Debug.Log($"[PlayModeLogCapture] Started capturing logs for session: {_sessionId}");
        }
        
        private void StopCapture()
        {
            if (!_isCapturing) return;
            
            Application.logMessageReceived -= OnLogMessageReceived;
            Application.logMessageReceivedThreaded -= OnLogMessageReceivedThreaded;
            _isCapturing = false;
            
            // Process remaining logs
            ProcessLogQueue();
            
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
            // Handle frame count safely for threaded vs non-threaded contexts
            int frameCount;
            if (isThreaded)
            {
                // From background thread - use cached value
                lock (_frameCountLock)
                {
                    frameCount = _cachedFrameCount;
                }
            }
            else
            {
                // From main thread - get current value and update cache
                try
                {
                    frameCount = Time.frameCount;
                    lock (_frameCountLock)
                    {
                        _cachedFrameCount = frameCount;
                    }
                }
                catch
                {
                    // Fallback if Time.frameCount fails (e.g., during initialization)
                    lock (_frameCountLock)
                    {
                        frameCount = _cachedFrameCount;
                    }
                }
            }
            
            var entry = new LogEntry
            {
                Message = logString,
                StackTrace = stackTrace,
                LogType = type,
                Timestamp = DateTime.Now,
                IsThreaded = isThreaded,
                FrameCount = frameCount
            };
            
            lock (_queueLock)
            {
                _logQueue.Enqueue(entry);
                
                // Prevent memory overflow - increased limit for better capture
                const int MAX_QUEUE_SIZE = 10000;
                if (_logQueue.Count > MAX_QUEUE_SIZE)
                {
                    // Log warning about dropped messages
                    var droppedCount = _logQueue.Count - MAX_QUEUE_SIZE + 1;
                    Debug.LogWarning($"[PlayModeLogCapture] Queue overflow - dropping {droppedCount} oldest log(s). Consider reducing log volume.");
                    
                    while (_logQueue.Count > MAX_QUEUE_SIZE)
                    {
                        _logQueue.Dequeue();
                    }
                }
            }
        }
        
        private void Update()
        {
            // Update cached frame count for thread-safe access
            lock (_frameCountLock)
            {
                _cachedFrameCount = Time.frameCount;
            }
            
            // Process log queue more frequently for better real-time capture
            if (Time.frameCount % 5 == 0) // Every 5 frames (roughly 0.083 seconds at 60fps)
            {
                ProcessLogQueue();
            }
        }
        
        private void ProcessLogQueue()
        {
            if (string.IsNullOrEmpty(_databasePath) || string.IsNullOrEmpty(_sessionId))
                return;
            
            List<LogEntry> logsToProcess = null;
            
            lock (_queueLock)
            {
                if (_logQueue.Count > 0)
                {
                    logsToProcess = new List<LogEntry>(_logQueue);
                    _logQueue.Clear();
                }
            }
            
            if (logsToProcess != null && logsToProcess.Count > 0)
            {
                // Send logs to Editor for database storage
                SendLogsToEditor(logsToProcess);
            }
        }
        
        private void SendLogsToEditor(List<LogEntry> logs)
        {
            // Use Unity's Editor connection to send logs back
            // This will be picked up by the PlayModeLogReceiver in Editor
            foreach (var log in logs)
            {
                var data = new PlayModeLogData
                {
                    SessionId = _sessionId,
                    LogLevel = ConvertLogType(log.LogType),
                    Message = log.Message,
                    StackTrace = log.StackTrace,
                    Timestamp = log.Timestamp.Ticks,
                    FrameCount = log.FrameCount,
                    Context = $"PlayMode|Thread:{(log.IsThreaded ? "Background" : "Main")}|Frame:{log.FrameCount}"
                };
                
                // Store in PlayerPrefs temporarily (will be read by Editor)
                StoreLogForEditor(data);
            }
        }
        
        private void StoreLogForEditor(PlayModeLogData data)
        {
            // Use a circular buffer in PlayerPrefs for communication
            // This is a temporary storage that the Editor will read and clear
            var key = $"PlayModeLog_{DateTime.Now.Ticks}_{UnityEngine.Random.Range(0, 10000)}";
            var json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(key, json);
            
            // Store the key in a list for the Editor to find
            var keyList = PlayerPrefs.GetString("PlayModeLogKeys", "");
            if (!string.IsNullOrEmpty(keyList))
                keyList += "|";
            keyList += key;
            
            // Keep only last 500 keys to prevent overflow while allowing more logs
            var keys = keyList.Split('|');
            if (keys.Length > 500)
            {
                var recentKeys = new string[500];
                Array.Copy(keys, keys.Length - 500, recentKeys, 0, 500);
                keyList = string.Join("|", recentKeys);
            }
            
            PlayerPrefs.SetString("PlayModeLogKeys", keyList);
            PlayerPrefs.Save();
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
                default: return "Info";
            }
        }
        
        private void OnDestroy()
        {
            // Force immediate processing of all remaining logs
            ProcessLogQueue();
            StopCapture();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            // Process logs immediately on both pause and resume
            ProcessLogQueue();
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            // Process logs immediately on both focus and unfocus
            ProcessLogQueue();
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
        
        [Serializable]
        public class PlayModeLogData
        {
            public string SessionId;
            public string LogLevel;
            public string Message;
            public string StackTrace;
            public long Timestamp;
            public int FrameCount;
            public string Context;
        }
    }
}