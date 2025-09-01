using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using PerSpec.Editor.Services;

namespace PerSpec.Editor.Initialization
{
    /// <summary>
    /// PerSpec initialization window that appears on first launch and creates working directories
    /// </summary>
    [InitializeOnLoad]
    public class PerSpecInitializer : EditorWindow
    {
        private static string ProjectPerSpecPath => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "PerSpec");
        private static string DatabasePath => Path.Combine(ProjectPerSpecPath, "test_coordination.db");
        private static bool hasShownThisSession = false;
        
        // Version tracking
        private const string VERSION_PREF_KEY = "PerSpec_LastKnownVersion";
        private const string PACKAGE_NAME = "com.digitraver.perspec";
        private const string CURRENT_VERSION = "1.0.0";  // Should match package.json
        
        // Update detection
        private bool isUpdate = false;
        private string previousVersion = "";
        private string currentVersion = CURRENT_VERSION;
        
        // State for inline message display
        private bool initializationAttempted = false;
        private bool initializationSuccess = false;
        private string initializationMessage = "";
        private bool showOpenControlCenter = false;
        
        static PerSpecInitializer()
        {
            // Check on Unity startup after a delay
            EditorApplication.delayCall += CheckInitializationOnStartup;
            
            // Also check for package updates
            EditorApplication.delayCall += CheckForPackageUpdate;
        }
        
        static void CheckInitializationOnStartup()
        {
            if (!Directory.Exists(ProjectPerSpecPath) && !hasShownThisSession)
            {
                hasShownThisSession = true;
                Debug.LogWarning("[PerSpec] Not initialized. Opening setup window...");
                ShowWindow();
            }
            else if (Directory.Exists(ProjectPerSpecPath))
            {
                Debug.Log($"[PerSpec] Initialized at: {ProjectPerSpecPath}");
            }
        }
        
        static void CheckForPackageUpdate()
        {
            try
            {
                // Get current package version
                var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForPackageName(PACKAGE_NAME);
                string detectedVersion = packageInfo != null ? packageInfo.version : CURRENT_VERSION;
                
                // Get last known version from EditorPrefs
                string lastKnownVersion = EditorPrefs.GetString(VERSION_PREF_KEY, "");
                
                if (string.IsNullOrEmpty(lastKnownVersion))
                {
                    // First time seeing this package
                    EditorPrefs.SetString(VERSION_PREF_KEY, detectedVersion);
                    Debug.Log($"[PerSpec] Package version {detectedVersion} registered");
                    
                    // If not initialized, show the setup window
                    if (!Directory.Exists(ProjectPerSpecPath) && !hasShownThisSession)
                    {
                        hasShownThisSession = true;
                        ShowWindow();
                    }
                }
                else if (lastKnownVersion != detectedVersion)
                {
                    // Package was updated!
                    Debug.Log($"[PerSpec] Package updated from {lastKnownVersion} to {detectedVersion}");
                    EditorPrefs.SetString(VERSION_PREF_KEY, detectedVersion);
                    
                    // Show update window
                    ShowUpdateWindow(lastKnownVersion, detectedVersion);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PerSpec] Could not check package version: {e.Message}");
            }
        }
        
        [MenuItem("Tools/PerSpec/Initialize PerSpec", priority = -100)]
        public static void ShowWindow()
        {
            var window = GetWindow<PerSpecInitializer>("PerSpec Setup");
            window.minSize = new Vector2(450, 350);
            window.maxSize = new Vector2(600, 500);
            window.isUpdate = false;
            window.Show();
        }
        
        public static void ShowUpdateWindow(string oldVersion, string newVersion)
        {
            var window = GetWindow<PerSpecInitializer>("PerSpec Updated!");
            window.minSize = new Vector2(450, 400);
            window.maxSize = new Vector2(600, 500);
            window.isUpdate = true;
            window.previousVersion = oldVersion;
            window.currentVersion = newVersion;
            window.Show();
            
            // Show notification
            window.ShowNotification(new GUIContent($"Updated: {oldVersion} → {newVersion}"), 3f);
        }
        
        [MenuItem("Tools/PerSpec/Documentation", priority = 600)]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://github.com/thePostFuturist/PerSpec");
            Debug.Log("[PerSpec] Opening documentation in browser...");
        }
        
        private void OnGUI()
        {
            if (isUpdate)
            {
                DrawUpdateUI();
            }
            else
            {
                DrawSetupUI();
            }
        }
        
        private void DrawUpdateUI()
        {
            // Update header
            EditorGUILayout.Space(10);
            
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("🎉 PerSpec Updated!", headerStyle, GUILayout.Height(30));
            
            EditorGUILayout.Space(10);
            
            // Version info
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Previous Version:", GUILayout.Width(100));
            EditorGUILayout.LabelField(previousVersion);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Current Version:", GUILayout.Width(100));
            var versionStyle = new GUIStyle(EditorStyles.boldLabel);
            versionStyle.normal.textColor = Color.green;
            EditorGUILayout.LabelField(currentVersion, versionStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(20);
            
            // Update actions
            EditorGUILayout.HelpBox(
                "PerSpec has been updated! It's recommended to refresh your coordination scripts to get the latest features.",
                MessageType.Info
            );
            
            EditorGUILayout.Space(10);
            
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("Refresh Coordination Scripts", GUILayout.Height(35)))
            {
                if (InitializationService.IsInitialized)
                {
                    if (InitializationService.RefreshCoordinationScripts())
                    {
                        ShowNotification(new GUIContent("✓ Scripts refreshed successfully!"));
                        Debug.Log("[PerSpec] Coordination scripts refreshed with latest version");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Failed to refresh scripts. Check console for details.", "OK");
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Not Initialized", "Please initialize PerSpec first.", "OK");
                }
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("Open Control Center", GUILayout.Height(30)))
            {
                EditorApplication.ExecuteMenuItem("Tools/PerSpec/Control Center");
                Close();
            }
            
            if (GUILayout.Button("View Changelog", GUILayout.Height(25)))
            {
                Application.OpenURL("https://github.com/thePostFuturist/PerSpec/blob/main/CHANGELOG.md");
            }
            
            EditorGUILayout.Space(10);
            
            // What's new section (could be populated from changelog)
            EditorGUILayout.LabelField("What's New:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "• Unity focus management for Windows & macOS\n" +
                "• Improved test coordination\n" +
                "• Enhanced debug logging with PerSpecDebug\n" +
                "• Better documentation and agent support",
                MessageType.None
            );
        }
        
        private void DrawSetupUI()
        {
            // Original setup UI
            EditorGUILayout.Space(10);
            
            // Try to load and display logo
            var logo = Resources.Load<Texture2D>("Icons/digitraver");
            if (logo != null)
            {
                var rect = GUILayoutUtility.GetRect(48, 48, GUILayout.ExpandWidth(false));
                rect.x = (position.width - 48) / 2;
                GUI.DrawTexture(rect, logo, ScaleMode.ScaleToFit);
                EditorGUILayout.Space(5);
            }
            
            var centeredStyle = new GUIStyle(EditorStyles.largeLabel);
            centeredStyle.alignment = TextAnchor.MiddleCenter;
            var miniCenteredStyle = new GUIStyle(EditorStyles.miniLabel);
            miniCenteredStyle.alignment = TextAnchor.MiddleCenter;
            
            GUILayout.Label("PerSpec Testing Framework", centeredStyle);
            GUILayout.Label("Professional Unity TDD with UniTask and SQLite coordination", miniCenteredStyle);
            
            EditorGUILayout.Space(5);
            GUILayout.Label("Made in San Francisco by Valentin Burov", miniCenteredStyle);
            EditorGUILayout.Space(10);
            
            // Draw separator
            DrawUILine(Color.gray);
            
            // Status check
            bool isInitialized = Directory.Exists(ProjectPerSpecPath);
            
            if (isInitialized)
            {
                ShowInitializedUI();
            }
            else
            {
                ShowNotInitializedUI();
            }
            
            // Footer
            GUILayout.FlexibleSpace();
            DrawUILine(Color.gray);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Documentation"))
            {
                Application.OpenURL("https://github.com/thePostFuturist/PerSpec");
            }
            if (GUILayout.Button("Fork on GitHub"))
            {
                Application.OpenURL("https://github.com/thePostFuturist/PerSpec");
            }
            if (GUILayout.Button("digitRaver.com"))
            {
                Application.OpenURL("https://digitRaver.com");
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void ShowInitializedUI()
        {
            // Show inline success message if just initialized
            if (initializationAttempted && initializationSuccess)
            {
                EditorGUILayout.HelpBox(initializationMessage, MessageType.Info);
                
                if (showOpenControlCenter)
                {
                    EditorGUILayout.Space(5);
                    
                    var oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
                    if (GUILayout.Button("Open Control Center", GUILayout.Height(35)))
                    {
                        EditorApplication.ExecuteMenuItem("Tools/PerSpec/Control Center");
                        showOpenControlCenter = false;
                        initializationAttempted = false;
                    }
                    GUI.backgroundColor = oldColor;
                    
                    EditorGUILayout.Space(10);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("PerSpec is initialized and ready to use!", MessageType.Info);
            }
            
            EditorGUILayout.Space(10);
            
            // Show paths
            EditorGUILayout.LabelField("Working Directory:", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(ProjectPerSpecPath);
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.LabelField("Database Path:", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(DatabasePath);
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(10);
            
            // Actions
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Open PerSpec Folder"))
            {
                EditorUtility.RevealInFinder(ProjectPerSpecPath);
            }
            
            if (GUILayout.Button("Open Test Coordinator"))
            {
                EditorApplication.ExecuteMenuItem("Tools/PerSpec/Test Coordinator");
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Re-initialize option
            EditorGUILayout.HelpBox("Re-initializing will keep your database but recreate the folder structure and copy fresh scripts from the package.", MessageType.None);
            
            if (GUILayout.Button("Re-Initialize PerSpec"))
            {
                if (EditorUtility.DisplayDialog("Re-Initialize PerSpec", 
                    "This will recreate the folder structure and copy fresh scripts from the package.\n\nYour database and test results will be preserved.\n\nContinue?", 
                    "Yes", "Cancel"))
                {
                    // Reset message state for re-initialization
                    initializationAttempted = false;
                    InitializePerSpec();
                }
            }
        }
        
        private void ShowNotInitializedUI()
        {
            // Show inline error message if initialization failed
            if (initializationAttempted && !initializationSuccess)
            {
                EditorGUILayout.HelpBox(initializationMessage, MessageType.Error);
                EditorGUILayout.Space(10);
            }
            else
            {
                EditorGUILayout.HelpBox("PerSpec needs to be initialized for this project.", MessageType.Warning);
            }
            
            EditorGUILayout.Space(10);
            
            GUILayout.Label("This will create:", EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            GUILayout.Label("• PerSpec/ folder in your project root");
            GUILayout.Label("• SQLite database for test coordination");
            GUILayout.Label("• Python coordination scripts in PerSpec/Coordination/Scripts/");
            GUILayout.Label("• TestResults/ directory for test output");
            GUILayout.Label("• Logs/ directory for console exports");
            EditorGUI.indentLevel--;
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.HelpBox("The PerSpec folder will be created at:\n" + ProjectPerSpecPath, MessageType.None);
            
            EditorGUILayout.Space(20);
            
            // Big initialization button
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;
            
            if (GUILayout.Button("Initialize PerSpec", GUILayout.Height(40)))
            {
                InitializePerSpec();
            }
            
            GUI.backgroundColor = oldColor;
        }
        
        private void InitializePerSpec()
        {
            try
            {
                // Reset state
                initializationAttempted = true;
                initializationSuccess = false;
                initializationMessage = "";
                showOpenControlCenter = false;
                
                // Use the centralized InitializationService
                bool success = InitializationService.Initialize();
                
                if (success)
                {
                    // Set success state
                    initializationSuccess = true;
                    showOpenControlCenter = true;
                    initializationMessage = "PerSpec has been initialized successfully!\n\n" +
                        "Working directory created at: " + ProjectPerSpecPath + "\n\n" +
                        "You can now:\n" +
                        "• Use Tools > PerSpec menu items\n" +
                        "• Run tests from PerSpec/Coordination/Scripts/\n" +
                        "• View test results in PerSpec/TestResults/\n" +
                        "• Export logs to PerSpec/Logs/";
                    
                    // Refresh Unity
                    AssetDatabase.Refresh();
                    
                    // Update window
                    Repaint();
                    
                    Debug.Log("[PerSpec] Initialization complete! Working directory: " + ProjectPerSpecPath);
                }
                else
                {
                    // Set error state
                    initializationSuccess = false;
                    initializationMessage = "Failed to initialize PerSpec. Check the console for error details.";
                    
                    Debug.LogError("[PerSpec] Initialization failed. Please check the console for details.");
                    
                    // Update window
                    Repaint();
                }
            }
            catch (Exception e)
            {
                // Set error state
                initializationAttempted = true;
                initializationSuccess = false;
                initializationMessage = $"Failed to initialize PerSpec:\n{e.Message}";
                
                Debug.LogError($"[PerSpec] Initialization failed: {e}");
                
                // Update window
                Repaint();
            }
        }
        
        private static void DrawUILine(Color color, int thickness = 1, int padding = 10)
        {
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            rect.height = thickness;
            rect.y += padding / 2;
            rect.x -= 2;
            rect.width += 4;
            EditorGUI.DrawRect(rect, color);
        }
    }
}