using System;
using System.IO;
using UnityEngine;
using UnityEditor;
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
        
        static PerSpecInitializer()
        {
            // Check on Unity startup after a delay
            EditorApplication.delayCall += CheckInitializationOnStartup;
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
        
        [MenuItem("Tools/PerSpec/Initialize PerSpec", priority = -100)]
        public static void ShowWindow()
        {
            var window = GetWindow<PerSpecInitializer>("PerSpec Setup");
            window.minSize = new Vector2(450, 350);
            window.maxSize = new Vector2(600, 500);
            window.Show();
        }
        
        [MenuItem("Tools/PerSpec/Documentation", priority = 600)]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://github.com/thePostFuturist/PerSpec");
            Debug.Log("[PerSpec] Opening documentation in browser...");
        }
        
        private void OnGUI()
        {
            // Header with logo
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
            EditorGUILayout.HelpBox("PerSpec is initialized and ready to use!", MessageType.Info);
            
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
                    InitializePerSpec();
                }
            }
        }
        
        private void ShowNotInitializedUI()
        {
            EditorGUILayout.HelpBox("PerSpec needs to be initialized for this project.", MessageType.Warning);
            
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
                // Use the centralized InitializationService
                bool success = InitializationService.Initialize();
                
                if (success)
                {
                    // Success message
                    EditorUtility.DisplayDialog("Success", 
                        "PerSpec has been initialized successfully!\n\n" +
                        "Working directory created at:\n" + ProjectPerSpecPath + "\n\n" +
                        "You can now:\n" +
                        "• Use Tools > PerSpec menu items\n" +
                        "• Run tests from PerSpec/Coordination/Scripts/\n" +
                        "• View test results in PerSpec/TestResults/\n" +
                        "• Export logs to PerSpec/Logs/", 
                        "OK");
                    
                    // Refresh Unity
                    AssetDatabase.Refresh();
                    
                    // Update window
                    Repaint();
                    
                    Debug.Log("[PerSpec] Initialization complete!");
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", 
                        "Failed to initialize PerSpec. Check the console for details.", 
                        "OK");
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", 
                    $"Failed to initialize PerSpec:\n{e.Message}", 
                    "OK");
                Debug.LogError($"[PerSpec] Initialization failed: {e}");
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