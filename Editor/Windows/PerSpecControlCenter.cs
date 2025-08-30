using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PerSpec.Editor.Services;
using PerSpec.Editor.Coordination;

namespace PerSpec.Editor.Windows
{
    /// <summary>
    /// Central control panel for all PerSpec features
    /// </summary>
    public class PerSpecControlCenter : EditorWindow
    {
        #region Constants
        
        private const string WINDOW_TITLE = "PerSpec Control Center";
        private static readonly Vector2 MIN_SIZE = new Vector2(600, 500);
        
        #endregion
        
        #region Fields
        
        private int selectedTab = 0;
        private string[] tabNames = new string[]
        {
            "Dashboard",
            "Test Coordinator",
            "Debug Settings",
            "Console Logs",
            "Initialization",
            "LLM Setup",
            "About"
        };
        
        private Vector2 scrollPosition;
        private GUIStyle headerStyle;
        private GUIStyle sectionStyle;
        private GUIStyle statusStyle;
        
        // BuildProfile management
        private BuildProfileHelper.BuildProfileInfo[] scannedProfiles;
        private bool showProfilesList = false;
        
        #endregion
        
        #region Unity Menu
        
        [MenuItem("Tools/PerSpec/Control Center", priority = -200)]
        public static void ShowWindow()
        {
            var window = GetWindow<PerSpecControlCenter>(false, WINDOW_TITLE);
            window.minSize = MIN_SIZE;
            window.Show();
        }
        
        [MenuItem("Tools/PerSpec/Quick Actions/Run Pending Tests", priority = 1)]
        private static void QuickRunTests()
        {
            if (TestCoordinationService.CheckPendingTests())
                Debug.Log("[PerSpec] Started pending tests");
            else
                Debug.Log("[PerSpec] No pending tests found");
        }
        
        [MenuItem("Tools/PerSpec/Quick Actions/Toggle Debug Logging", priority = 2)]
        private static void QuickToggleDebug()
        {
            DebugService.ToggleDebugLogging();
        }
        
        [MenuItem("Tools/PerSpec/Quick Actions/Open Working Directory", priority = 3)]
        private static void QuickOpenDirectory()
        {
            InitializationService.OpenWorkingDirectory();
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void OnEnable()
        {
            titleContent = new GUIContent(WINDOW_TITLE, EditorGUIUtility.IconContent("d_Settings").image);
        }
        
        private void OnGUI()
        {
            InitStyles();
            
            // Header
            DrawHeader();
            
            // Tab bar
            EditorGUILayout.Space(5);
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(30));
            EditorGUILayout.Space(5);
            
            // Tab content
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            switch (selectedTab)
            {
                case 0: DrawDashboardTab(); break;
                case 1: DrawTestCoordinatorTab(); break;
                case 2: DrawDebugSettingsTab(); break;
                case 3: DrawConsoleLogsTab(); break;
                case 4: DrawInitializationTab(); break;
                case 5: DrawLLMSetupTab(); break;
                case 6: DrawAboutTab(); break;
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        #endregion
        
        #region GUI Drawing - Header
        
        private void InitStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.largeLabel)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
            }
            
            if (sectionStyle == null)
            {
                sectionStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }
            
            if (statusStyle == null)
            {
                statusStyle = new GUIStyle(EditorStyles.label)
                {
                    richText = true,
                    alignment = TextAnchor.MiddleCenter
                };
            }
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("PerSpec Control Center", headerStyle);
            EditorGUILayout.LabelField("Unity Test Framework with TDD & SQLite Coordination", 
                new GUIStyle(EditorStyles.centeredGreyMiniLabel));
        }
        
        #endregion
        
        #region GUI Drawing - Dashboard Tab
        
        private void DrawDashboardTab()
        {
            // Quick Status
            DrawSection("System Status", () =>
            {
                DrawStatusRow("Initialization", InitializationService.IsInitialized ? "✓ Ready" : "✗ Not initialized", 
                    InitializationService.IsInitialized ? Color.green : Color.red);
                
                DrawStatusRow("Test Coordinator", TestCoordinationService.GetStatusSummary(), 
                    TestCoordinationService.IsRunningTests ? Color.yellow : Color.green);
                
                DrawStatusRow("Debug Logging", DebugService.DebugStatus,
                    DebugService.IsDebugEnabled ? Color.green : Color.gray);
                
                DrawStatusRow("Console Capture", ConsoleService.CaptureStatus,
                    ConsoleService.IsCaptureEnabled ? Color.green : Color.gray);
                
                DrawStatusRow("Compiler Directives", BuildProfileHelper.ConfigurationMode,
                    BuildProfileHelper.HasActiveBuildProfile ? Color.cyan : Color.white);
            });
            
            EditorGUILayout.Space(10);
            
            // Quick Actions
            DrawSection("Quick Actions", () =>
            {
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Run Tests", GUILayout.Height(40)))
                {
                    TestCoordinationService.CheckPendingTests();
                }
                
                if (GUILayout.Button("Toggle Debug", GUILayout.Height(40)))
                {
                    DebugService.ToggleDebugLogging();
                }
                
                if (GUILayout.Button("Open Directory", GUILayout.Height(40)))
                {
                    InitializationService.OpenWorkingDirectory();
                }
                
                EditorGUILayout.EndHorizontal();
            });
            
            EditorGUILayout.Space(10);
            
            // Statistics
            DrawSection("Statistics", () =>
            {
                EditorGUILayout.LabelField("Unity Version:", Application.unityVersion);
                EditorGUILayout.LabelField("Database Size:", 
                    $"{InitializationService.DatabaseSize / 1024f:F1} KB");
                EditorGUILayout.LabelField("Console Logs:", 
                    $"{ConsoleService.CapturedLogCount} captured");
                EditorGUILayout.LabelField("Errors:", 
                    $"{ConsoleService.ErrorCount}", 
                    ConsoleService.ErrorCount > 0 ? EditorStyles.boldLabel : EditorStyles.label);
            });
        }
        
        #endregion
        
        #region GUI Drawing - Test Coordinator Tab
        
        private void DrawTestCoordinatorTab()
        {
            DrawInfoBox(
                "Test Coordinator manages automated test execution through SQLite database coordination. " +
                "Python scripts can submit test requests that Unity will automatically execute."
            );
            
            EditorGUILayout.Space(10);
            
            // Status
            DrawSection("Test Status", () =>
            {
                EditorGUILayout.LabelField("Status:", TestCoordinationService.GetStatusSummary());
                
                if (TestCoordinationService.IsRunningTests)
                {
                    EditorGUILayout.LabelField("Current Request:", 
                        $"#{TestCoordinationService.CurrentRequestId}");
                        
                    if (GUILayout.Button("Cancel Current Test"))
                    {
                        TestCoordinationService.CancelCurrentTest();
                    }
                }
            });
            
            EditorGUILayout.Space(10);
            
            // Controls
            DrawSection("Test Controls", () =>
            {
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Check Pending Tests", GUILayout.Height(30)))
                {
                    if (TestCoordinationService.CheckPendingTests())
                        ShowNotification(new GUIContent("Started pending tests"));
                    else
                        ShowNotification(new GUIContent("No pending tests"));
                }
                
                if (GUILayout.Button("Force Compilation", GUILayout.Height(30)))
                {
                    TestCoordinationService.ForceScriptCompilation();
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                bool polling = TestCoordinationService.PollingEnabled;
                bool newPolling = EditorGUILayout.Toggle("Auto-Polling Enabled", polling);
                if (newPolling != polling)
                {
                    TestCoordinationService.PollingEnabled = newPolling;
                }
            });
            
            EditorGUILayout.Space(10);
            
            // Database Info
            DrawSection("Database Status", () =>
            {
                EditorGUILayout.TextArea(TestCoordinationService.GetDatabaseStatus(), 
                    GUILayout.Height(100));
            });
        }
        
        #endregion
        
        #region GUI Drawing - Debug Settings Tab
        
        private void DrawDebugSettingsTab()
        {
            DrawInfoBox(
                "Debug Settings control whether PerSpec debug logging is compiled into builds. " +
                "When disabled, all PerSpecDebug.Log() calls are completely stripped with zero runtime overhead."
            );
            
            EditorGUILayout.Space(10);
            
            // Current Status
            DrawSection("Debug Status", () =>
            {
                string status = DebugService.IsDebugEnabled 
                    ? "● ENABLED - Debug logs will be included" 
                    : "● DISABLED - Debug logs are stripped";
                
                Color color = DebugService.IsDebugEnabled ? Color.green : Color.gray;
                GUI.color = color;
                EditorGUILayout.LabelField(status, statusStyle);
                GUI.color = Color.white;
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Configuration Mode:", BuildProfileHelper.ConfigurationMode);
                
                if (BuildProfileHelper.AreBuildProfilesSupported && !BuildProfileHelper.HasActiveBuildProfile)
                {
                    EditorGUILayout.HelpBox(
                        "No active BuildProfile detected. Using PlayerSettings fallback. " +
                        "Create and activate a BuildProfile in Unity 6 for better integration.",
                        MessageType.Info
                    );
                }
            });
            
            EditorGUILayout.Space(10);
            
            // Controls
            DrawSection("Debug Controls", () =>
            {
                EditorGUILayout.BeginHorizontal();
                
                GUI.backgroundColor = DebugService.IsDebugEnabled ? Color.red : Color.green;
                string buttonText = DebugService.IsDebugEnabled 
                    ? "Disable Debug Logging" 
                    : "Enable Debug Logging";
                    
                if (GUILayout.Button(buttonText, GUILayout.Height(40)))
                {
                    DebugService.ToggleDebugLogging();
                }
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(10);
                
                if (GUILayout.Button("Test Log Levels"))
                {
                    DebugService.TestLogLevels();
                }
            });
            
            EditorGUILayout.Space(10);
            
            // BuildProfile Management (Unity 6+)
            if (BuildProfileHelper.AreBuildProfilesSupported)
            {
                DrawSection("BuildProfile Management", () =>
                {
                    EditorGUILayout.HelpBox(
                        "Scan for BuildProfiles in your project and manage which one is active. " +
                        "BuildProfiles control platform-specific settings and compiler directives.",
                        MessageType.Info
                    );
                    
                    EditorGUILayout.Space(5);
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    if (GUILayout.Button("Scan for Build Profiles", GUILayout.Height(30)))
                    {
                        scannedProfiles = BuildProfileHelper.ScanForBuildProfiles();
                        showProfilesList = scannedProfiles != null && scannedProfiles.Length > 0;
                        
                        if (scannedProfiles == null || scannedProfiles.Length == 0)
                        {
                            ShowNotification(new GUIContent("No BuildProfiles found in project"));
                        }
                        else
                        {
                            ShowNotification(new GUIContent($"Found {scannedProfiles.Length} BuildProfile(s)"));
                        }
                    }
                    
                    if (showProfilesList && scannedProfiles != null && scannedProfiles.Length > 0)
                    {
                        if (GUILayout.Button("Hide Profiles", GUILayout.Height(30)))
                        {
                            showProfilesList = false;
                        }
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    
                    // Display found profiles
                    if (showProfilesList && scannedProfiles != null && scannedProfiles.Length > 0)
                    {
                        EditorGUILayout.Space(10);
                        EditorGUILayout.LabelField("Found BuildProfiles:", EditorStyles.boldLabel);
                        
                        foreach (var profile in scannedProfiles)
                        {
                            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                            
                            // Profile name
                            string displayName = profile.IsActive ? $"● {profile.Name} (Active)" : profile.Name;
                            EditorGUILayout.LabelField(displayName, profile.IsActive ? EditorStyles.boldLabel : EditorStyles.label);
                            
                            // Activate button
                            if (!profile.IsActive)
                            {
                                if (GUILayout.Button("Activate", GUILayout.Width(80)))
                                {
                                    if (BuildProfileHelper.SetActiveBuildProfile(profile.Path))
                                    {
                                        ShowNotification(new GUIContent($"Activated: {profile.Name}"));
                                        // Rescan to update active status
                                        scannedProfiles = BuildProfileHelper.ScanForBuildProfiles();
                                    }
                                    else
                                    {
                                        ShowNotification(new GUIContent($"Failed to activate: {profile.Name}"));
                                    }
                                }
                            }
                            
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                });
                
                EditorGUILayout.Space(10);
            }
            
            // Usage Example
            DrawSection("Usage Example", () =>
            {
                string code = @"using PerSpec;

// These calls are stripped when disabled:
PerSpecDebug.Log(""[TEST] Starting test"");
PerSpecDebug.LogError(""[ERROR] Test failed"");
PerSpecDebug.LogTestSetup(""Creating prefab"");
PerSpecDebug.LogTestComplete(""Test passed"");";
                
                EditorGUILayout.TextArea(code, GUILayout.Height(100));
            });
        }
        
        #endregion
        
        #region GUI Drawing - Console Logs Tab
        
        private void DrawConsoleLogsTab()
        {
            DrawInfoBox(
                "Console Log Capture saves Unity console output to SQLite database for analysis. " +
                "Captured logs can be retrieved via Python scripts for debugging and reporting."
            );
            
            EditorGUILayout.Space(10);
            
            // Status
            DrawSection("Capture Status", () =>
            {
                EditorGUILayout.LabelField("Status:", ConsoleService.CaptureStatus);
                EditorGUILayout.LabelField("Session:", ConsoleService.SessionId);
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Total: {ConsoleService.CapturedLogCount}");
                EditorGUILayout.LabelField($"Errors: {ConsoleService.ErrorCount}");
                EditorGUILayout.LabelField($"Warnings: {ConsoleService.WarningCount}");
                EditorGUILayout.EndHorizontal();
            });
            
            EditorGUILayout.Space(10);
            
            // Controls
            DrawSection("Console Controls", () =>
            {
                EditorGUILayout.BeginHorizontal();
                
                GUI.backgroundColor = ConsoleService.IsCaptureEnabled ? Color.red : Color.green;
                string buttonText = ConsoleService.IsCaptureEnabled 
                    ? "Stop Capture" 
                    : "Start Capture";
                    
                if (GUILayout.Button(buttonText, GUILayout.Height(40)))
                {
                    ConsoleService.ToggleCapture();
                }
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Clear Session"))
                {
                    ConsoleService.ClearSession();
                }
                
                if (GUILayout.Button("Export Logs"))
                {
                    ConsoleService.ExportLogs();
                }
                
                if (GUILayout.Button("Test Logs"))
                {
                    ConsoleService.TestLogLevels();
                }
                
                EditorGUILayout.EndHorizontal();
            });
            
            EditorGUILayout.Space(10);
            
            // Session Info
            DrawSection("Session Information", () =>
            {
                EditorGUILayout.TextArea(ConsoleService.GetSessionInfo(), GUILayout.Height(80));
            });
        }
        
        #endregion
        
        #region GUI Drawing - Initialization Tab
        
        private void DrawInitializationTab()
        {
            DrawInfoBox(
                "PerSpec requires a working directory in your project root for SQLite database and scripts. " +
                "This directory is created at: ProjectRoot/PerSpec/"
            );
            
            EditorGUILayout.Space(10);
            
            // Status
            DrawSection("Initialization Status", () =>
            {
                bool initialized = InitializationService.IsInitialized;
                
                string status = initialized 
                    ? "✓ PerSpec is initialized and ready" 
                    : "✗ PerSpec is not initialized";
                    
                Color color = initialized ? Color.green : Color.red;
                GUI.color = color;
                EditorGUILayout.LabelField(status, statusStyle);
                GUI.color = Color.white;
                
                if (initialized)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Working Directory:", 
                        InitializationService.ProjectPerSpecPath);
                    EditorGUILayout.LabelField("Database:", 
                        InitializationService.DatabasePath);
                    EditorGUILayout.LabelField("Status:", 
                        InitializationService.GetStatusSummary());
                }
            });
            
            EditorGUILayout.Space(10);
            
            // Controls
            DrawSection("Initialization Controls", () =>
            {
                if (!InitializationService.IsInitialized)
                {
                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button("Initialize PerSpec", GUILayout.Height(40)))
                    {
                        if (InitializationService.Initialize())
                        {
                            ShowNotification(new GUIContent("PerSpec initialized successfully"));
                        }
                    }
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    if (GUILayout.Button("Open Directory", GUILayout.Height(30)))
                    {
                        InitializationService.OpenWorkingDirectory();
                    }
                    
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("Reset", GUILayout.Height(30)))
                    {
                        if (EditorUtility.DisplayDialog("Reset PerSpec",
                            "This will delete the PerSpec working directory and all data. Continue?",
                            "Reset", "Cancel"))
                        {
                            InitializationService.Reset();
                        }
                    }
                    GUI.backgroundColor = Color.white;
                    
                    EditorGUILayout.EndHorizontal();
                }
            });
            
            // Script Management (only show if initialized)
            if (InitializationService.IsInitialized)
            {
                EditorGUILayout.Space(10);
                
                DrawSection("Script Management", () =>
                {
                    // Package location info
                    EditorGUILayout.LabelField("Package Location:", 
                        PackagePathResolver.GetPackageLocationInfo());
                    
                    // Check if scripts need refresh
                    bool needsRefresh = InitializationService.ScriptsNeedRefresh();
                    if (needsRefresh)
                    {
                        EditorGUILayout.HelpBox(
                            "Wrapper scripts appear to be outdated or using old package paths. " +
                            "Click 'Refresh Scripts' to update them.",
                            MessageType.Warning);
                    }
                    
                    EditorGUILayout.Space(5);
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    // Refresh Scripts button
                    GUI.backgroundColor = needsRefresh ? Color.yellow : Color.white;
                    if (GUILayout.Button(needsRefresh ? "Refresh Scripts (Recommended)" : "Refresh Scripts", 
                        GUILayout.Height(30)))
                    {
                        if (InitializationService.RefreshWrapperScripts())
                        {
                            ShowNotification(new GUIContent("Scripts refreshed successfully"));
                            Debug.Log($"[PerSpec] Scripts refreshed. Package at: {PackagePathResolver.PackagePath}");
                        }
                        else
                        {
                            ShowNotification(new GUIContent("Failed to refresh scripts"));
                        }
                    }
                    GUI.backgroundColor = Color.white;
                    
                    // Force Package Refresh button
                    if (GUILayout.Button("Force Package Refresh", GUILayout.Height(30)))
                    {
                        string oldPath = PackagePathResolver.PackagePath;
                        string newPath = PackagePathResolver.RefreshPackagePath();
                        
                        if (oldPath != newPath)
                        {
                            ShowNotification(new GUIContent("Package path updated"));
                            Debug.Log($"[PerSpec] Package path changed from: {oldPath} to: {newPath}");
                            
                            // Also refresh scripts after package path change
                            InitializationService.RefreshWrapperScripts();
                        }
                        else
                        {
                            ShowNotification(new GUIContent("Package path unchanged"));
                        }
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.Space(5);
                    
                    // Info about wrapper scripts
                    EditorGUILayout.HelpBox(
                        "Wrapper scripts allow you to run PerSpec commands from the command line:\n" +
                        "• refresh - Refresh Unity assets\n" +
                        "• test - Run Unity tests\n" +
                        "• logs - View Unity console logs\n" +
                        "• init_db - Initialize database",
                        MessageType.Info);
                });
            }
        }
        
        #endregion
        
        #region GUI Drawing - LLM Setup Tab
        
        private void DrawLLMSetupTab()
        {
            // Detect existing LLM configurations (moved outside to be accessible by both sections)
            var detectedConfigs = DetectLLMConfigurations();
            
            DrawSection("LLM Configuration Management", () =>
            {
                EditorGUILayout.HelpBox("Configure your AI coding assistant with PerSpec's TDD instructions.", MessageType.Info);
                EditorGUILayout.Space(10);
                
                if (detectedConfigs.Count > 0)
                {
                    EditorGUILayout.LabelField("Detected Configurations:", EditorStyles.boldLabel);
                    foreach (var config in detectedConfigs)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"• {config.Key}", GUILayout.Width(200));
                        
                        if (GUILayout.Button("Update", GUILayout.Width(80)))
                        {
                            UpdateLLMConfiguration(config.Value);
                        }
                        
                        if (GUILayout.Button("View", GUILayout.Width(80)))
                        {
                            EditorUtility.RevealInFinder(config.Value);
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No LLM configuration files detected in your project.", MessageType.Warning);
                }
                
                EditorGUILayout.Space(20);
                EditorGUILayout.LabelField("Create New Configuration:", EditorStyles.boldLabel);
                
                // Dropdown for LLM selection
                string[] llmOptions = new string[] 
                {
                    "Claude (CLAUDE.md)",
                    "Cursor (.cursorrules)",
                    "GitHub Copilot (.github/copilot-instructions.md)",
                    "Aider (.aider.conf.yml)",
                    "Codeium (.codeium/instructions.md)",
                    "Continue (.continue/context.md)"
                };
                
                EditorGUILayout.BeginHorizontal();
                int selectedLLM = EditorGUILayout.Popup("Select LLM:", 0, llmOptions);
                
                if (GUILayout.Button("Create", GUILayout.Width(100)))
                {
                    CreateLLMConfiguration(selectedLLM);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(10);
                
                // Copy agents folder option
                if (GUILayout.Button("Copy Agent Definitions to Project", GUILayout.Height(30)))
                {
                    CopyAgentDefinitions();
                }
            });
            
            EditorGUILayout.Space(10);
            
            // Command Permissions Section
            DrawSection("Command Execution Permissions", () =>
            {
                // Warning message
                EditorGUILayout.HelpBox(
                    "⚠️ WARNING: Enabling command execution allows AI assistants to run system commands.\n" +
                    "• Only enable for trusted AI providers\n" +
                    "• Commands can modify files and execute scripts\n" +
                    "• You can revoke permissions at any time",
                    MessageType.Warning);
                
                EditorGUILayout.Space(10);
                
                if (detectedConfigs.Count > 0)
                {
                    EditorGUILayout.LabelField("Configure Permissions per Provider:", EditorStyles.boldLabel);
                    EditorGUILayout.Space(5);
                    
                    foreach (var config in detectedConfigs)
                    {
                        DrawProviderPermissions(config.Key);
                        EditorGUILayout.Space(5);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No LLM providers detected. Create a configuration first.", EditorStyles.centeredGreyMiniLabel);
                }
                
                EditorGUILayout.Space(10);
                
                // Show providers with active permissions
                var providersWithPermissions = LLMPermissionManager.GetProvidersWithPermissions();
                if (providersWithPermissions.Count > 0)
                {
                    EditorGUILayout.LabelField("Active Permissions:", EditorStyles.boldLabel);
                    foreach (var provider in providersWithPermissions)
                    {
                        var state = LLMPermissionManager.GetPermissions(provider);
                        string perms = "";
                        if (state.BashEnabled) perms += "Bash ";
                        if (state.PythonEnabled) perms += "Python";
                        
                        EditorGUILayout.BeginHorizontal();
                        GUI.color = Color.yellow;
                        EditorGUILayout.LabelField($"⚠️ {provider}: {perms}", EditorStyles.boldLabel);
                        GUI.color = Color.white;
                        
                        if (GUILayout.Button("Revoke All", GUILayout.Width(80)))
                        {
                            if (EditorUtility.DisplayDialog("Revoke Permissions",
                                $"Revoke all command execution permissions for {provider}?",
                                "Revoke", "Cancel"))
                            {
                                LLMPermissionManager.ClearPermissions(provider);
                                ShowNotification(new GUIContent($"Revoked permissions for {provider}"));
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            });
            
            DrawSection("Instructions", () =>
            {
                string instructions = @"1. Detect: Scans your project for existing LLM configuration files
2. Update: Appends PerSpec's TDD instructions to existing configs
3. Create: Creates a new configuration file with PerSpec instructions
4. Agents: Copies specialized agent definitions to .claude/agents/

Supported LLMs:
• Claude Code - CLAUDE.md
• Cursor - .cursorrules
• GitHub Copilot - .github/copilot-instructions.md
• Aider - .aider.conf.yml
• Codeium - .codeium/instructions.md
• Continue - .continue/context.md";
                
                EditorGUILayout.TextArea(instructions, GUILayout.Height(180));
            });
        }
        
        private Dictionary<string, string> DetectLLMConfigurations()
        {
            var configs = new Dictionary<string, string>();
            string projectPath = Directory.GetParent(Application.dataPath).FullName;
            
            // Check for various LLM configuration files
            var llmFiles = new Dictionary<string, string>
            {
                { "Claude (CLAUDE.md)", "CLAUDE.md" },
                { "Cursor (.cursorrules)", ".cursorrules" },
                { "Copilot", Path.Combine(".github", "copilot-instructions.md") },
                { "Aider", ".aider.conf.yml" },
                { "Codeium", Path.Combine(".codeium", "instructions.md") },
                { "Continue", Path.Combine(".continue", "context.md") }
            };
            
            foreach (var file in llmFiles)
            {
                string fullPath = Path.Combine(projectPath, file.Value);
                if (File.Exists(fullPath))
                {
                    configs[file.Key] = fullPath;
                }
            }
            
            return configs;
        }
        
        private void DrawProviderPermissions(string providerDisplay)
        {
            // Extract clean provider name from display string
            string provider = providerDisplay.Split('(')[0].Trim();
            var permissions = LLMPermissionManager.GetPermissions(provider);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            
            // Provider name
            EditorGUILayout.LabelField(providerDisplay, GUILayout.Width(200));
            
            // Bash permission toggle
            bool newBash = permissions.BashEnabled;
            GUI.backgroundColor = newBash ? Color.red : Color.green;
            string bashLabel = newBash ? "Bash ✓" : "Bash ✗";
            
            if (GUILayout.Button(bashLabel, GUILayout.Width(70)))
            {
                newBash = !newBash;
                
                if (newBash && !permissions.BashEnabled)
                {
                    if (EditorUtility.DisplayDialog("Enable Bash Commands",
                        $"Allow {provider} to execute bash/shell commands?\n\n" +
                        "This includes:\n" +
                        "• File system operations (ls, cd, find)\n" +
                        "• Git commands\n" +
                        "• System utilities\n\n" +
                        "Only enable if you trust this AI provider.",
                        "Enable", "Cancel"))
                    {
                        LLMPermissionManager.SetBashPermission(provider, true);
                        UpdateLLMConfigurationPermissions(provider);
                        ShowNotification(new GUIContent($"Bash enabled for {provider}"));
                    }
                }
                else if (!newBash && permissions.BashEnabled)
                {
                    LLMPermissionManager.SetBashPermission(provider, false);
                    UpdateLLMConfigurationPermissions(provider);
                    ShowNotification(new GUIContent($"Bash disabled for {provider}"));
                }
            }
            
            // Python permission toggle
            bool newPython = permissions.PythonEnabled;
            GUI.backgroundColor = newPython ? Color.red : Color.green;
            string pythonLabel = newPython ? "Python ✓" : "Python ✗";
            
            if (GUILayout.Button(pythonLabel, GUILayout.Width(70)))
            {
                newPython = !newPython;
                
                if (newPython && !permissions.PythonEnabled)
                {
                    if (EditorUtility.DisplayDialog("Enable Python Execution",
                        $"Allow {provider} to execute Python scripts?\n\n" +
                        "This includes:\n" +
                        "• PerSpec coordination scripts\n" +
                        "• Python script execution\n" +
                        "• Package management (pip)\n\n" +
                        "Only enable if you trust this AI provider.",
                        "Enable", "Cancel"))
                    {
                        LLMPermissionManager.SetPythonPermission(provider, true);
                        UpdateLLMConfigurationPermissions(provider);
                        ShowNotification(new GUIContent($"Python enabled for {provider}"));
                    }
                }
                else if (!newPython && permissions.PythonEnabled)
                {
                    LLMPermissionManager.SetPythonPermission(provider, false);
                    UpdateLLMConfigurationPermissions(provider);
                    ShowNotification(new GUIContent($"Python disabled for {provider}"));
                }
            }
            
            GUI.backgroundColor = Color.white;
            
            // Status indicator
            if (permissions.HasAnyPermission)
            {
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField("⚠️ Active", GUILayout.Width(60));
                GUI.color = Color.white;
            }
            else
            {
                EditorGUILayout.LabelField("Disabled", GUILayout.Width(60));
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        
        private void UpdateLLMConfigurationPermissions(string provider)
        {
            // Find the config file for this provider
            var configs = DetectLLMConfigurations();
            string configPath = null;
            
            foreach (var config in configs)
            {
                if (config.Key.StartsWith(provider))
                {
                    configPath = config.Value;
                    break;
                }
            }
            
            if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
            {
                try
                {
                    string content = File.ReadAllText(configPath);
                    string updatedContent = LLMPermissionManager.UpdatePermissionBlock(content, provider);
                    
                    if (content != updatedContent)
                    {
                        File.WriteAllText(configPath, updatedContent);
                        Debug.Log($"[PerSpec] Updated permissions in {Path.GetFileName(configPath)} for {provider}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PerSpec] Failed to update permissions in config: {e.Message}");
                }
            }
        }
        
        private void UpdateLLMConfiguration(string configPath)
        {
            try
            {
                string llmContent = GetLLMContent();
                string existingContent = File.ReadAllText(configPath);
                
                // Determine provider from config path
                string provider = GetProviderFromPath(configPath);
                
                // Check for existing block markers
                string startMarker = GetStartMarker(configPath);
                string endMarker = GetEndMarker(configPath);
                
                if (existingContent.Contains(startMarker) && existingContent.Contains(endMarker))
                {
                    // Replace existing content between markers
                    string updatedContent = ReplaceContentBetweenMarkers(
                        existingContent, 
                        llmContent, 
                        startMarker, 
                        endMarker
                    );
                    
                    // Add permission block if enabled
                    updatedContent = LLMPermissionManager.UpdatePermissionBlock(updatedContent, provider);
                    
                    File.WriteAllText(configPath, updatedContent);
                    
                    EditorUtility.DisplayDialog("Success",
                        $"Updated {Path.GetFileName(configPath)} with latest PerSpec instructions.",
                        "OK");
                }
                else
                {
                    // No markers found - just append with markers at the end
                    string separator = "\n\n";
                    string contentWithMarkers = $"{separator}{startMarker}\n{llmContent}\n{endMarker}";
                    // Add permission block if enabled
                    string fullContent = existingContent + contentWithMarkers;
                    fullContent = LLMPermissionManager.UpdatePermissionBlock(fullContent, provider);
                    
                    File.WriteAllText(configPath, fullContent);
                    
                    EditorUtility.DisplayDialog("Success",
                        $"Added PerSpec instructions to {Path.GetFileName(configPath)}.",
                        "OK");
                }
                
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error",
                    $"Failed to update configuration: {e.Message}",
                    "OK");
            }
        }
        
        private void CreateLLMConfiguration(int llmIndex)
        {
            string projectPath = Directory.GetParent(Application.dataPath).FullName;
            string configPath = "";
            
            switch (llmIndex)
            {
                case 0: // Claude
                    configPath = Path.Combine(projectPath, "CLAUDE.md");
                    break;
                case 1: // Cursor
                    configPath = Path.Combine(projectPath, ".cursorrules");
                    break;
                case 2: // Copilot
                    string githubDir = Path.Combine(projectPath, ".github");
                    if (!Directory.Exists(githubDir))
                        Directory.CreateDirectory(githubDir);
                    configPath = Path.Combine(githubDir, "copilot-instructions.md");
                    break;
                case 3: // Aider
                    configPath = Path.Combine(projectPath, ".aider.conf.yml");
                    break;
                case 4: // Codeium
                    string codeiumDir = Path.Combine(projectPath, ".codeium");
                    if (!Directory.Exists(codeiumDir))
                        Directory.CreateDirectory(codeiumDir);
                    configPath = Path.Combine(codeiumDir, "instructions.md");
                    break;
                case 5: // Continue
                    string continueDir = Path.Combine(projectPath, ".continue");
                    if (!Directory.Exists(continueDir))
                        Directory.CreateDirectory(continueDir);
                    configPath = Path.Combine(continueDir, "context.md");
                    break;
            }
            
            if (File.Exists(configPath))
            {
                if (!EditorUtility.DisplayDialog("File Exists",
                    $"Configuration file already exists at:\n{configPath}\n\nOverwrite?",
                    "Overwrite", "Cancel"))
                {
                    return;
                }
            }
            
            try
            {
                string llmContent = GetLLMContent();
                
                // For Aider, we need to wrap in YAML format
                if (llmIndex == 3)
                {
                    llmContent = $"# Aider configuration with PerSpec instructions\n\n" +
                                $"instructions: |\n" +
                                string.Join("\n", llmContent.Split('\n').Select(line => "  " + line));
                }
                
                // Add permission block if enabled
                string providerName = GetProviderFromIndex(llmIndex);
                llmContent = LLMPermissionManager.UpdatePermissionBlock(llmContent, providerName);
                
                File.WriteAllText(configPath, llmContent);
                
                EditorUtility.DisplayDialog("Success",
                    $"Created {Path.GetFileName(configPath)} with PerSpec instructions.",
                    "OK");
                
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error",
                    $"Failed to create configuration: {e.Message}",
                    "OK");
            }
        }
        
        private void CopyAgentDefinitions()
        {
            try
            {
                string projectPath = Directory.GetParent(Application.dataPath).FullName;
                string targetDir = Path.Combine(projectPath, ".claude", "agents");
                string sourceDir = Path.Combine(PackagePathResolver.PackagePath, "Documentation", "agents");
                
                if (!Directory.Exists(sourceDir))
                {
                    EditorUtility.DisplayDialog("Error",
                        "Agent definitions not found in package.",
                        "OK");
                    return;
                }
                
                // Create target directory
                Directory.CreateDirectory(targetDir);
                
                // Track copied and updated files
                int copiedCount = 0;
                int updatedCount = 0;
                List<string> existingFiles = new List<string>();
                
                // Copy all agent files
                foreach (string file in Directory.GetFiles(sourceDir, "*.md"))
                {
                    string fileName = Path.GetFileName(file);
                    string targetPath = Path.Combine(targetDir, fileName);
                    
                    if (File.Exists(targetPath))
                    {
                        existingFiles.Add(fileName);
                        updatedCount++;
                    }
                    else
                    {
                        copiedCount++;
                    }
                    
                    File.Copy(file, targetPath, true);
                }
                
                // Build result message
                string message = "Agent definitions synchronized:\n";
                if (copiedCount > 0)
                    message += $"• {copiedCount} new agent(s) copied\n";
                if (updatedCount > 0)
                    message += $"• {updatedCount} existing agent(s) updated\n";
                message += $"\nLocation: {targetDir}";
                
                EditorUtility.DisplayDialog("Success", message, "OK");
                
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error",
                    $"Failed to copy agent definitions: {e.Message}",
                    "OK");
            }
        }
        
        private string GetLLMContent()
        {
            string llmPath = Path.Combine(PackagePathResolver.PackagePath, "Documentation", "LLM.md");
            
            if (!File.Exists(llmPath))
            {
                throw new FileNotFoundException($"LLM.md not found at: {llmPath}");
            }
            
            return File.ReadAllText(llmPath);
        }
        
        #endregion
        
        #region GUI Drawing - About Tab
        
        private void DrawAboutTab()
        {
            DrawSection("About PerSpec", () =>
            {
                EditorGUILayout.LabelField("Version:", "1.0.0");
                EditorGUILayout.LabelField("Unity:", Application.unityVersion);
                
                EditorGUILayout.Space(10);
                
                EditorGUILayout.LabelField("PerSpec is a Test-Driven Development framework for Unity", 
                    EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField("that uses SQLite coordination for automated testing.", 
                    EditorStyles.wordWrappedLabel);
                    
                EditorGUILayout.Space(10);
                
                if (GUILayout.Button("View Documentation"))
                {
                    Application.OpenURL("https://github.com/yourusername/perspec/wiki");
                }
                
                if (GUILayout.Button("Report Issue"))
                {
                    Application.OpenURL("https://github.com/yourusername/perspec/issues");
                }
            });
            
            EditorGUILayout.Space(10);
            
            DrawSection("4-Step TDD Workflow", () =>
            {
                // Use relative paths for display (these are informational, not executed)
                string workflow = @"1. Write code and tests with TDD
2. Refresh Unity: 
   python quick_refresh.py full --wait
3. Check for errors:
   python quick_logs.py errors
4. Run tests:
   python quick_test.py all -p edit --wait

Note: Use the convenience scripts in PerSpec/Scripts/ or run from package location";
   
                EditorGUILayout.TextArea(workflow, GUILayout.Height(140));
            });
        }
        
        #endregion
        
        #region Helper Methods
        
        private void DrawSection(string title, Action content)
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            content?.Invoke();
            EditorGUILayout.EndVertical();
        }
        
        private void DrawInfoBox(string message)
        {
            EditorGUILayout.HelpBox(message, MessageType.Info);
        }
        
        private void DrawStatusRow(string label, string status, Color color)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(120));
            
            var oldColor = GUI.color;
            GUI.color = color;
            EditorGUILayout.LabelField(status);
            GUI.color = oldColor;
            
            EditorGUILayout.EndHorizontal();
        }
        
        #endregion
        
        #region Block Marker Helpers
        
        private string GetStartMarker(string configPath)
        {
            string fileName = Path.GetFileName(configPath);
            
            // Use appropriate comment syntax based on file type
            if (fileName.EndsWith(".md"))
                return "<!-- PERSPEC_CONFIG_START -->";
            else if (fileName.EndsWith(".yml") || fileName.EndsWith(".yaml"))
                return "# PERSPEC_CONFIG_START";
            else
                return "# PERSPEC_CONFIG_START";
        }
        
        private string GetEndMarker(string configPath)
        {
            string fileName = Path.GetFileName(configPath);
            
            // Use appropriate comment syntax based on file type
            if (fileName.EndsWith(".md"))
                return "<!-- PERSPEC_CONFIG_END -->";
            else if (fileName.EndsWith(".yml") || fileName.EndsWith(".yaml"))
                return "# PERSPEC_CONFIG_END";
            else
                return "# PERSPEC_CONFIG_END";
        }
        
        private string ReplaceContentBetweenMarkers(string content, string newContent, string startMarker, string endMarker)
        {
            int startIndex = content.IndexOf(startMarker);
            int endIndex = content.IndexOf(endMarker);
            
            if (startIndex == -1 || endIndex == -1 || endIndex <= startIndex)
            {
                throw new InvalidOperationException("Invalid marker positions in content");
            }
            
            // Find the end of the end marker line
            int endMarkerEnd = content.IndexOf('\n', endIndex);
            if (endMarkerEnd == -1)
                endMarkerEnd = content.Length;
            
            // Build the new content
            string beforeMarkers = content.Substring(0, startIndex);
            string afterMarkers = content.Substring(endMarkerEnd);
            
            return $"{beforeMarkers}{startMarker}\n{newContent}\n{endMarker}{afterMarkers}";
        }
        
        private string GetProviderFromPath(string configPath)
        {
            string fileName = Path.GetFileName(configPath);
            
            if (fileName.Contains("CLAUDE") || fileName.Contains("claude"))
                return "Claude";
            else if (fileName.Contains("cursor"))
                return "Cursor";
            else if (configPath.Contains(".github") && fileName.Contains("copilot"))
                return "Copilot";
            else if (fileName.Contains("aider"))
                return "Aider";
            else if (configPath.Contains(".codeium"))
                return "Codeium";
            else if (configPath.Contains(".continue"))
                return "Continue";
            
            return "Unknown";
        }
        
        private string GetProviderFromIndex(int index)
        {
            switch (index)
            {
                case 0: return "Claude";
                case 1: return "Cursor";
                case 2: return "Copilot";
                case 3: return "Aider";
                case 4: return "Codeium";
                case 5: return "Continue";
                default: return "Unknown";
            }
        }
        
        #endregion
    }
}