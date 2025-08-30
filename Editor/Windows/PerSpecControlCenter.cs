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
        }
        
        #endregion
        
        #region GUI Drawing - LLM Setup Tab
        
        private void DrawLLMSetupTab()
        {
            DrawSection("LLM Configuration Management", () =>
            {
                EditorGUILayout.HelpBox("Configure your AI coding assistant with PerSpec's TDD instructions.", MessageType.Info);
                EditorGUILayout.Space(10);
                
                // Detect existing LLM configurations
                var detectedConfigs = DetectLLMConfigurations();
                
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
        
        private void UpdateLLMConfiguration(string configPath)
        {
            try
            {
                string llmContent = GetLLMContent();
                string existingContent = File.ReadAllText(configPath);
                
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
                    
                    File.WriteAllText(configPath, updatedContent);
                    
                    EditorUtility.DisplayDialog("Success",
                        $"Updated {Path.GetFileName(configPath)} with latest PerSpec instructions.",
                        "OK");
                }
                else if (existingContent.Contains("PerSpec Testing Framework") || 
                         existingContent.Contains("TDD Development Workflow"))
                {
                    // Legacy content exists without markers
                    EditorUtility.DisplayDialog("Manual Update Required",
                        "This configuration contains PerSpec instructions without block markers.\n" +
                        "Please remove the old content manually and update again.",
                        "OK");
                    return;
                }
                else
                {
                    // Append new content with markers
                    string separator = "\n\n";
                    string contentWithMarkers = $"{separator}{startMarker}\n{llmContent}\n{endMarker}";
                    File.AppendAllText(configPath, contentWithMarkers);
                    
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
        
        #endregion
    }
}