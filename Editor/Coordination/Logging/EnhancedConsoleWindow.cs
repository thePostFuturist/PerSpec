using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace PerSpec.Editor.Coordination
{
    /// <summary>
    /// Enhanced console window that provides robust logging visualization
    /// Works even during compilation errors
    /// </summary>
    public class EnhancedConsoleWindow : EditorWindow
    {
        private SQLiteManager _dbManager;
        private ListView _logListView;
        private TextField _searchField;
        private ToolbarToggle _infoToggle;
        private ToolbarToggle _warningToggle;
        private ToolbarToggle _errorToggle;
        private ToolbarButton _clearButton;
        private ToolbarButton _refreshButton;
        private ToolbarButton _exportButton;
        private Label _statusLabel;
        private ScrollView _detailScrollView;
        private Label _detailLabel;
        
        private List<ConsoleLogEntry> _allLogs = new List<ConsoleLogEntry>();
        private List<ConsoleLogEntry> _filteredLogs = new List<ConsoleLogEntry>();
        private ConsoleLogEntry _selectedLog;
        
        private bool _autoRefresh = true;
        private float _lastRefreshTime;
        private const float REFRESH_INTERVAL = 0.5f;
        private bool _hasFocus = false;
        private int _lastKnownLogCount = 0;
        
        private bool _showInfo = true;
        private bool _showWarnings = true;
        private bool _showErrors = true;
        private string _searchText = "";
        private bool _showAllSessions = false;
        
        private const int MAX_DISPLAY_LOGS = 1000;
        
        [MenuItem("Tools/PerSpec/Windows/Enhanced Console", false, 150)]
        public static void ShowWindow()
        {
            var window = GetWindow<EnhancedConsoleWindow>();
            window.titleContent = new GUIContent("Enhanced Console", EditorGUIUtility.IconContent("console.infoicon").image);
            window.minSize = new Vector2(600, 400);
        }
        
        private void OnEnable()
        {
            InitializeDatabase();
            CreateGUI();
            RefreshLogs();
            
            // Subscribe to compilation events for real-time updates
            UnityEditor.Compilation.CompilationPipeline.compilationStarted += OnCompilationStarted;
            UnityEditor.Compilation.CompilationPipeline.compilationFinished += OnCompilationFinished;
            
            // Initialize refresh timer
            _lastRefreshTime = (float)EditorApplication.timeSinceStartup;
        }
        
        private void OnFocus()
        {
            _hasFocus = true;
            // Refresh immediately when window gains focus
            RefreshLogs();
            _lastRefreshTime = (float)EditorApplication.timeSinceStartup;
        }
        
        private void OnLostFocus()
        {
            _hasFocus = false;
        }
        
        private void OnDisable()
        {
            UnityEditor.Compilation.CompilationPipeline.compilationStarted -= OnCompilationStarted;
            UnityEditor.Compilation.CompilationPipeline.compilationFinished -= OnCompilationFinished;
        }
        
        private void InitializeDatabase()
        {
            try
            {
                if (SQLiteManager.IsPerSpecInitialized())
                {
                    _dbManager = new SQLiteManager();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EnhancedConsole] Failed to initialize database: {ex.Message}");
            }
        }
        
        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.Clear();
            
            // Create toolbar
            var toolbar = new Toolbar();
            toolbar.style.height = 25;
            
            // Log level toggles
            _infoToggle = new ToolbarToggle { text = "Info", value = _showInfo };
            _infoToggle.RegisterValueChangedCallback(evt => { 
                _showInfo = evt.newValue; 
                ApplyFilters(); 
                _lastRefreshTime = (float)EditorApplication.timeSinceStartup; // Reset timer for immediate update
            });
            
            _warningToggle = new ToolbarToggle { text = "Warnings", value = _showWarnings };
            _warningToggle.RegisterValueChangedCallback(evt => { 
                _showWarnings = evt.newValue; 
                ApplyFilters(); 
                _lastRefreshTime = (float)EditorApplication.timeSinceStartup;
            });
            
            _errorToggle = new ToolbarToggle { text = "Errors", value = _showErrors };
            _errorToggle.RegisterValueChangedCallback(evt => { 
                _showErrors = evt.newValue; 
                ApplyFilters(); 
                _lastRefreshTime = (float)EditorApplication.timeSinceStartup;
            });
            
            // Control buttons
            _clearButton = new ToolbarButton(() => ClearLogs()) { text = "Clear" };
            _refreshButton = new ToolbarButton(() => RefreshLogs()) { text = "Refresh" };
            _exportButton = new ToolbarButton(() => ExportLogs()) { text = "Export" };
            
            var autoRefreshToggle = new ToolbarToggle { text = "Auto-Refresh", value = _autoRefresh };
            autoRefreshToggle.RegisterValueChangedCallback(evt => _autoRefresh = evt.newValue);
            
            toolbar.Add(_infoToggle);
            toolbar.Add(_warningToggle);
            toolbar.Add(_errorToggle);
            toolbar.Add(new ToolbarSpacer());
            toolbar.Add(_clearButton);
            toolbar.Add(_refreshButton);
            toolbar.Add(_exportButton);
            toolbar.Add(new ToolbarSpacer() { style = { flexGrow = 1 } });
            
            var allSessionsToggle = new ToolbarToggle { text = "All Sessions", value = _showAllSessions };
            allSessionsToggle.RegisterValueChangedCallback(evt => 
            { 
                _showAllSessions = evt.newValue; 
                RefreshLogs(); 
            });
            toolbar.Add(allSessionsToggle);
            toolbar.Add(autoRefreshToggle);
            
            root.Add(toolbar);
            
            // Search bar
            var searchBar = new VisualElement();
            searchBar.style.flexDirection = FlexDirection.Row;
            searchBar.style.height = 25;
            searchBar.style.paddingLeft = 5;
            searchBar.style.paddingRight = 5;
            
            _searchField = new TextField();
            _searchField.style.flexGrow = 1;
            _searchField.RegisterValueChangedCallback(evt => { _searchText = evt.newValue; ApplyFilters(); });
            
            var searchLabel = new Label("Search:");
            searchLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            searchLabel.style.width = 50;
            
            searchBar.Add(searchLabel);
            searchBar.Add(_searchField);
            
            root.Add(searchBar);
            
            // Split view for logs and details
            var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Vertical);
            
            // Log list
            _logListView = new ListView();
            _logListView.makeItem = MakeLogItem;
            _logListView.bindItem = BindLogItem;
            _logListView.selectionType = SelectionType.Single;
            _logListView.selectionChanged += OnLogSelectionChanged;
            _logListView.style.flexGrow = 1;
            
            splitView.Add(_logListView);
            
            // Detail view
            _detailScrollView = new ScrollView();
            _detailScrollView.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            
            _detailLabel = new Label();
            _detailLabel.style.whiteSpace = WhiteSpace.Normal;
            _detailLabel.style.paddingLeft = 5;
            _detailLabel.style.paddingRight = 5;
            _detailLabel.style.paddingTop = 5;
            _detailLabel.style.paddingBottom = 5;
            _detailLabel.style.fontSize = 11;
            _detailLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
            
            _detailScrollView.Add(_detailLabel);
            splitView.Add(_detailScrollView);
            
            root.Add(splitView);
            
            // Status bar
            _statusLabel = new Label("Ready");
            _statusLabel.style.height = 20;
            _statusLabel.style.paddingLeft = 5;
            _statusLabel.style.borderTopWidth = 1;
            _statusLabel.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            _statusLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            _statusLabel.style.fontSize = 10;
            
            root.Add(_statusLabel);
            
            // Register update callback
            EditorApplication.update += OnEditorUpdate;
        }
        
        private VisualElement MakeLogItem()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.height = 20;
            container.style.paddingLeft = 5;
            container.style.paddingRight = 5;
            
            var icon = new Image();
            icon.style.width = 16;
            icon.style.height = 16;
            icon.style.marginRight = 5;
            icon.userData = "icon";
            
            var label = new Label();
            label.style.flexGrow = 1;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.overflow = Overflow.Hidden;
            label.userData = "label";
            
            var timeLabel = new Label();
            timeLabel.style.width = 100;
            timeLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            timeLabel.style.fontSize = 10;
            timeLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            timeLabel.userData = "time";
            
            container.Add(icon);
            container.Add(label);
            container.Add(timeLabel);
            
            return container;
        }
        
        private void BindLogItem(VisualElement element, int index)
        {
            if (element == null || index >= _filteredLogs.Count) return;
            
            var log = _filteredLogs[index];
            if (log == null) return;
            
            // Find elements by userData since Q<> might not work reliably
            Image icon = null;
            Label label = null;
            Label timeLabel = null;
            
            foreach (var child in element.Children())
            {
                if (child.userData as string == "icon")
                    icon = child as Image;
                else if (child.userData as string == "label")
                    label = child as Label;
                else if (child.userData as string == "time")
                    timeLabel = child as Label;
            }
            
            // Set icon based on log level if found
            if (icon != null)
            {
                switch (log.LogLevel)
                {
                    case "Info":
                        icon.image = EditorGUIUtility.IconContent("console.infoicon.sml").image;
                        element.style.backgroundColor = new Color(0, 0, 0, 0);
                        break;
                    case "Warning":
                        icon.image = EditorGUIUtility.IconContent("console.warnicon.sml").image;
                        element.style.backgroundColor = new Color(0.5f, 0.5f, 0, 0.1f);
                        break;
                    case "Error":
                    case "Exception":
                        icon.image = EditorGUIUtility.IconContent("console.erroricon.sml").image;
                        element.style.backgroundColor = new Color(0.5f, 0, 0, 0.1f);
                        break;
                    default:
                        element.style.backgroundColor = new Color(0, 0, 0, 0);
                        break;
                }
            }
            
            // Set text if labels found
            if (label != null)
            {
                label.text = log.Message?.Replace("\n", " ") ?? "";
                
                // Highlight compilation logs
                if (log.Context != null && log.Context.Contains("Compilation:True"))
                {
                    label.style.unityFontStyleAndWeight = FontStyle.Bold;
                }
                else
                {
                    label.style.unityFontStyleAndWeight = FontStyle.Normal;
                }
            }
            
            if (timeLabel != null)
            {
                timeLabel.text = log.Timestamp.ToString("HH:mm:ss.fff");
            }
        }
        
        private void OnLogSelectionChanged(IEnumerable<object> selection)
        {
            var selectedIndex = _logListView.selectedIndex;
            if (selectedIndex >= 0 && selectedIndex < _filteredLogs.Count)
            {
                _selectedLog = _filteredLogs[selectedIndex];
                UpdateDetailView();
            }
        }
        
        private void UpdateDetailView()
        {
            if (_selectedLog == null)
            {
                _detailLabel.text = "";
                return;
            }
            
            var details = $"<b>Time:</b> {_selectedLog.Timestamp:yyyy-MM-dd HH:mm:ss.fff}\n";
            details += $"<b>Level:</b> {_selectedLog.LogLevel}\n";
            details += $"<b>Session:</b> {_selectedLog.SessionId}\n";
            
            if (!string.IsNullOrEmpty(_selectedLog.Context))
            {
                details += $"<b>Context:</b> {_selectedLog.Context}\n";
            }
            
            if (!string.IsNullOrEmpty(_selectedLog.SourceFile))
            {
                details += $"<b>Source:</b> {_selectedLog.SourceFile}";
                if (_selectedLog.SourceLine.HasValue)
                {
                    details += $":{_selectedLog.SourceLine}";
                }
                details += "\n";
            }
            
            details += $"\n<b>Message:</b>\n{_selectedLog.Message}\n";
            
            if (!string.IsNullOrEmpty(_selectedLog.TruncatedStack))
            {
                details += $"\n<b>Stack Trace:</b>\n{_selectedLog.TruncatedStack}\n";
            }
            else if (!string.IsNullOrEmpty(_selectedLog.StackTrace))
            {
                details += $"\n<b>Stack Trace:</b>\n{_selectedLog.StackTrace}\n";
            }
            
            _detailLabel.text = details;
        }
        
        private void RefreshLogs()
        {
            // Ensure UI elements are initialized
            if (_statusLabel == null || _logListView == null)
                return;
                
            if (_dbManager == null || !_dbManager.IsInitialized)
            {
                _statusLabel.text = "Database not initialized";
                return;
            }
            
            try
            {
                List<ConsoleLogEntry> logs;
                
                if (_showAllSessions)
                {
                    // Get recent logs from all sessions
                    var cutoffTime = DateTime.Now.AddHours(-1); // Last hour
                    logs = _dbManager.GetRecentConsoleLogs(cutoffTime, MAX_DISPLAY_LOGS);
                }
                else
                {
                    // Get only current session logs (mirrors Unity console)
                    var currentSessionId = UnityConsoleSessionManager.CurrentSessionId;
                    logs = _dbManager.GetCurrentSessionLogs(currentSessionId, MAX_DISPLAY_LOGS);
                }
                
                if (logs != null)
                {
                    _allLogs = logs;
                    // Sort by timestamp descending (newest first)
                    _allLogs.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
                }
                else
                {
                    _allLogs = new List<ConsoleLogEntry>();
                }
                
                ApplyFilters();
                
                var status = RobustLogHandler.GetStatus();
                var firstLine = status != null && status.Contains('\n') 
                    ? status.Split('\n')[0] 
                    : status ?? "Unknown";
                // Get session info
                var sessionCounts = _dbManager.GetSessionLogCounts();
                var totalSessions = sessionCounts.Count;
                var currentSession = UnityConsoleSessionManager.CurrentSessionId;
                var currentCount = sessionCounts.ContainsKey(currentSession) ? sessionCounts[currentSession] : 0;
                
                var sessionInfo = _showAllSessions 
                    ? $"All {totalSessions} Sessions" 
                    : $"Current Session ({currentCount} logs)";
                _statusLabel.text = $"Showing {_filteredLogs.Count} of {_allLogs.Count} logs | {sessionInfo} | Sessions: {totalSessions}/4";
            }
            catch (Exception ex)
            {
                if (_statusLabel != null)
                    _statusLabel.text = $"Error: {ex.Message}";
                Debug.LogError($"[EnhancedConsole] Failed to refresh logs: {ex.Message}");
            }
        }
        
        private void ApplyFilters()
        {
            _filteredLogs = _allLogs.Where(log =>
            {
                // Level filter
                bool levelMatch = false;
                switch (log.LogLevel)
                {
                    case "Info":
                        levelMatch = _showInfo;
                        break;
                    case "Warning":
                        levelMatch = _showWarnings;
                        break;
                    case "Error":
                    case "Exception":
                    case "Assert":
                        levelMatch = _showErrors;
                        break;
                    default:
                        levelMatch = true;
                        break;
                }
                
                if (!levelMatch) return false;
                
                // Search filter
                if (!string.IsNullOrEmpty(_searchText))
                {
                    var searchLower = _searchText.ToLower();
                    return (log.Message?.ToLower().Contains(searchLower) ?? false) ||
                           (log.StackTrace?.ToLower().Contains(searchLower) ?? false) ||
                           (log.SourceFile?.ToLower().Contains(searchLower) ?? false);
                }
                
                return true;
            }).ToList();
            
            _logListView.itemsSource = _filteredLogs;
            _logListView.Rebuild();
        }
        
        private void ClearLogs()
        {
            if (_dbManager == null) return;
            
            // Clear Unity's console and start new session
            UnityConsoleSessionManager.ClearConsole();
            
            // Refresh to show empty console
            RefreshLogs();
            _statusLabel.text = "Console cleared - new session started";
        }
        
        private void ExportLogs()
        {
            var path = EditorUtility.SaveFilePanel("Export Logs", "", "console_logs.txt", "txt");
            if (string.IsNullOrEmpty(path)) return;
            
            try
            {
                var content = new System.Text.StringBuilder();
                content.AppendLine($"Enhanced Console Log Export - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                content.AppendLine(new string('=', 80));
                
                foreach (var log in _filteredLogs)
                {
                    content.AppendLine($"[{log.Timestamp:HH:mm:ss.fff}] [{log.LogLevel}] {log.Message}");
                    
                    if (!string.IsNullOrEmpty(log.SourceFile))
                    {
                        content.AppendLine($"  Source: {log.SourceFile}:{log.SourceLine}");
                    }
                    
                    if (!string.IsNullOrEmpty(log.TruncatedStack))
                    {
                        content.AppendLine("  Stack:");
                        foreach (var line in log.TruncatedStack.Split('\n'))
                        {
                            content.AppendLine($"    {line}");
                        }
                    }
                    
                    content.AppendLine();
                }
                
                System.IO.File.WriteAllText(path, content.ToString());
                _statusLabel.text = $"Exported {_filteredLogs.Count} logs to {path}";
                
                if (EditorUtility.DisplayDialog("Export Complete", 
                    $"Exported {_filteredLogs.Count} logs to:\n{path}", 
                    "Open File", "OK"))
                {
                    EditorUtility.RevealInFinder(path);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EnhancedConsole] Failed to export logs: {ex.Message}");
            }
        }
        
        private void OnEditorUpdate()
        {
            // Use EditorApplication.timeSinceStartup instead of Time.realtimeSinceStartup
            // Time.realtimeSinceStartup may not update properly in Editor context
            float currentTime = (float)EditorApplication.timeSinceStartup;
            
            // More aggressive refresh when window has focus
            float effectiveInterval = _hasFocus ? REFRESH_INTERVAL * 0.5f : REFRESH_INTERVAL;
            
            if (_autoRefresh && currentTime - _lastRefreshTime > effectiveInterval)
            {
                _lastRefreshTime = currentTime;
                
                // Only refresh if there might be new logs
                if (ShouldRefresh())
                {
                    RefreshLogs();
                }
            }
        }
        
        private bool ShouldRefresh()
        {
            // Always refresh if database is not initialized
            if (_dbManager == null || !_dbManager.IsInitialized)
                return true;
            
            // Check if log count has changed (quick check)
            try
            {
                var currentCount = _dbManager.GetTotalLogCount();
                if (currentCount != _lastKnownLogCount)
                {
                    _lastKnownLogCount = currentCount;
                    return true;
                }
            }
            catch
            {
                // If we can't check, refresh anyway
                return true;
            }
            
            // Also refresh if filters or search changed (handled elsewhere)
            return false;
        }
        
        private void OnCompilationStarted(object obj)
        {
            _statusLabel.text = "Compilation in progress...";
        }
        
        private void OnCompilationFinished(object obj)
        {
            RefreshLogs();
            _statusLabel.text = "Compilation finished - logs refreshed";
        }
        
        private void OnDestroy()
        {
            EditorApplication.update -= OnEditorUpdate;
            UnityEditor.Compilation.CompilationPipeline.compilationStarted -= OnCompilationStarted;
            UnityEditor.Compilation.CompilationPipeline.compilationFinished -= OnCompilationFinished;
        }
    }
}