using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using System.Reflection;

namespace PerSpec.Editor.Coordination
{
    /// <summary>
    /// Manages Unity console session lifecycle to mirror Unity's actual console state
    /// </summary>
    [InitializeOnLoad]
    public static class UnityConsoleSessionManager
    {
        private static string _currentSessionId;
        private static SQLiteManager _dbManager;
        private static bool _isInitialized = false;
        
        public static string CurrentSessionId 
        { 
            get 
            {
                if (string.IsNullOrEmpty(_currentSessionId))
                {
                    StartNewSession("Initialization");
                }
                return _currentSessionId;
            }
        }
        
        public static event Action<string, string> OnSessionChanged; // oldSessionId, newSessionId
        
        static UnityConsoleSessionManager()
        {
            Initialize();
        }
        
        private static void Initialize()
        {
            if (_isInitialized) return;
            
            try
            {
                if (!SQLiteManager.IsPerSpecInitialized())
                    return;
                
                _dbManager = new SQLiteManager();
                if (!_dbManager.IsInitialized)
                    return;
                
                // Start a new session on initialization
                StartNewSession("Domain Reload");
                
                // Subscribe to events that would clear Unity's console
                CompilationPipeline.compilationFinished += OnCompilationFinished;
                AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                
                // Hook into console clear if possible
                EditorApplication.update += CheckForConsoleClear;
                
                _isInitialized = true;
                
                Debug.Log($"[UnityConsoleSessionManager] Initialized with session: {_currentSessionId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityConsoleSessionManager] Failed to initialize: {ex.Message}");
            }
        }
        
        private static void StartNewSession(string reason)
        {
            var oldSessionId = _currentSessionId;
            _currentSessionId = Guid.NewGuid().ToString();
            
            if (_dbManager != null && _dbManager.IsInitialized)
            {
                // Mark the old session as ended
                if (!string.IsNullOrEmpty(oldSessionId))
                {
                    _dbManager.EndConsoleSession(oldSessionId);
                }
                
                // Start the new session
                _dbManager.StartConsoleSession(_currentSessionId, reason);
            }
            
            OnSessionChanged?.Invoke(oldSessionId, _currentSessionId);
            
            // Log to the new session
            if (_isInitialized)
            {
                Debug.Log($"[UnityConsoleSessionManager] New session started: {reason}");
            }
        }
        
        private static void OnCompilationFinished(object obj)
        {
            // Only start new session if compilation succeeded (console would be cleared)
            bool hasErrors = false;
            
            // Check if there are compilation errors
            if (UnityEditor.Compilation.CompilationPipeline.codeOptimization == 
                UnityEditor.Compilation.CodeOptimization.Debug)
            {
                // This is a simplified check - in reality we'd need to check for actual errors
                // For now, we'll check if the last compilation had errors via our tracking
                hasErrors = _dbManager?.HasCompilationErrors() ?? false;
            }
            
            if (!hasErrors)
            {
                // Unity clears console on successful compilation
                StartNewSession("Compilation Success");
            }
        }
        
        private static void OnAfterAssemblyReload()
        {
            // Domain reload typically clears the console
            StartNewSession("Assembly Reload");
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    // Check if "Clear on Play" is enabled (default Unity behavior)
                    if (GetClearOnPlaySetting())
                    {
                        StartNewSession("Entered Play Mode");
                    }
                    break;
                    
                case PlayModeStateChange.EnteredEditMode:
                    // Console is typically preserved when exiting play mode
                    // but we might want to track it separately
                    break;
            }
        }
        
        private static bool GetClearOnPlaySetting()
        {
            // Unity stores this in EditorPrefs
            // The actual key might vary by Unity version
            return EditorPrefs.GetBool("ClearConsoleOnPlay", true);
        }
        
        private static DateTime _lastConsoleCheckTime = DateTime.MinValue;
        private static int _lastConsoleEntryCount = -1;
        
        private static void CheckForConsoleClear()
        {
            // Check every second
            if ((DateTime.Now - _lastConsoleCheckTime).TotalSeconds < 1)
                return;
            
            _lastConsoleCheckTime = DateTime.Now;
            
            // Use reflection to check if console was manually cleared
            try
            {
                var logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor");
                if (logEntriesType != null)
                {
                    var getCountMethod = logEntriesType.GetMethod("GetCount", 
                        BindingFlags.Static | BindingFlags.Public);
                    
                    if (getCountMethod != null)
                    {
                        int currentCount = (int)getCountMethod.Invoke(null, null);
                        
                        // If count dropped to 0 from a higher number, console was cleared
                        if (_lastConsoleEntryCount > 0 && currentCount == 0)
                        {
                            StartNewSession("Console Cleared");
                        }
                        
                        _lastConsoleEntryCount = currentCount;
                    }
                }
            }
            catch
            {
                // Silently fail - reflection might not work in all Unity versions
            }
        }
        
        public static void ClearConsole()
        {
            // Programmatically clear Unity's console and start new session
            try
            {
                var logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor");
                if (logEntriesType != null)
                {
                    var clearMethod = logEntriesType.GetMethod("Clear", 
                        BindingFlags.Static | BindingFlags.Public);
                    clearMethod?.Invoke(null, null);
                }
                
                StartNewSession("Manual Clear");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityConsoleSessionManager] Failed to clear console: {ex.Message}");
            }
        }
        
        public static void Cleanup()
        {
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= CheckForConsoleClear;
        }
    }
}