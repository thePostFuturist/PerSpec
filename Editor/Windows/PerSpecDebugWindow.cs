using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;
using PerSpec.Editor.Services;

namespace PerSpec.Editor.Windows
{
    /// <summary>
    /// Editor window for controlling PerSpec debug logging.
    /// Manages the PERSPEC_DEBUG scripting define symbol.
    /// </summary>
    public class PerSpecDebugWindow : EditorWindow
    {
        #region Constants
        
        private const string PERSPEC_DEBUG_SYMBOL = "PERSPEC_DEBUG";
        private const string WINDOW_TITLE = "PerSpec Debug Settings";
        
        #endregion
        
        #region Fields
        
        private bool isDebugEnabled;
        private bool isRefreshing;
        private GUIStyle headerStyle;
        private GUIStyle statusStyle;
        
        #endregion
        
        #region Unity Menu
        
        // Window now accessed via Control Center - Tools > PerSpec > Control Center
        public static void ShowWindow()
        {
            var window = GetWindow<PerSpecDebugWindow>(false, WINDOW_TITLE);
            window.minSize = new Vector2(350, 200);
            window.Show();
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void OnEnable()
        {
            CheckDebugSymbol();
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }
        
        private void OnDisable()
        {
            CompilationPipeline.compilationStarted -= OnCompilationStarted;
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
        }
        
        private void OnGUI()
        {
            InitStyles();
            
            EditorGUILayout.Space(10);
            
            // Header
            EditorGUILayout.LabelField("PerSpec Debug Logger", headerStyle);
            EditorGUILayout.Space(5);
            
            // Description
            EditorGUILayout.HelpBox(
                "Control whether PerSpec debug logging is compiled into builds.\n\n" +
                "When disabled, all PerSpec.Log() and PerSpec.LogError() calls are " +
                "completely stripped from the compiled code with zero runtime overhead.",
                MessageType.Info
            );
            
            EditorGUILayout.Space(10);
            
            // Current Status
            DrawStatus();
            
            EditorGUILayout.Space(5);
            
            // Configuration Mode
            DrawConfigurationMode();
            
            EditorGUILayout.Space(10);
            
            // Toggle Button
            DrawToggleButton();
            
            EditorGUILayout.Space(10);
            
            // Additional Info
            DrawAdditionalInfo();
            
            // Refresh if compilation is happening
            if (isRefreshing)
            {
                Repaint();
            }
        }
        
        #endregion
        
        #region GUI Drawing
        
        private void InitStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.largeLabel)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
            }
            
            if (statusStyle == null)
            {
                statusStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter,
                    richText = true
                };
            }
        }
        
        private void DrawStatus()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Current Status", EditorStyles.boldLabel);
            
            string statusText = isDebugEnabled 
                ? "<color=#90EE90>● ENABLED</color> - Debug logs will be included" 
                : "<color=#FFB6C1>● DISABLED</color> - Debug logs are stripped";
            
            EditorGUILayout.LabelField(statusText, statusStyle);
            
            if (isRefreshing)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("⟳ Recompiling scripts...", statusStyle);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawConfigurationMode()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Configuration Mode", EditorStyles.boldLabel);
            
            string modeText = $"Using: <b>{BuildProfileHelper.ConfigurationMode}</b>";
            EditorGUILayout.LabelField(modeText, statusStyle);
            
           
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawToggleButton()
        {
            EditorGUI.BeginDisabledGroup(isRefreshing);
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            string buttonText = isDebugEnabled ? "Disable Debug Logging" : "Enable Debug Logging";
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = isDebugEnabled ? new Color(1f, 0.6f, 0.6f) : new Color(0.6f, 1f, 0.6f);
            
            if (GUILayout.Button(buttonText, GUILayout.Height(40), GUILayout.Width(200)))
            {
                ToggleDebugSymbol();
            }
            
            GUI.backgroundColor = originalColor;
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            EditorGUI.EndDisabledGroup();
        }
        
        private void DrawAdditionalInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Usage Example", EditorStyles.boldLabel);
            
            string exampleCode = @"using PerSpec;

// These calls are stripped when disabled:
PerSpecDebug.Log(""[TEST] Starting test"");
PerSpecDebug.LogError(""[ERROR] Test failed"");
PerSpecDebug.LogTestSetup(""Creating prefab"");";
            
            EditorGUILayout.TextArea(exampleCode, EditorStyles.textArea, GUILayout.Height(80));
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("View PerSpecDebug.cs", EditorStyles.miniButton))
            {
                var debugScript = AssetDatabase.FindAssets("t:Script PerSpecDebug")
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .FirstOrDefault();
                    
                if (!string.IsNullOrEmpty(debugScript))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(debugScript);
                    AssetDatabase.OpenAsset(asset);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        #endregion
        
        #region Symbol Management
        
        private void CheckDebugSymbol()
        {
            isDebugEnabled = BuildProfileHelper.HasCompilerDirective(PERSPEC_DEBUG_SYMBOL);
        }
        
        private void ToggleDebugSymbol()
        {
            isDebugEnabled = !isDebugEnabled;
            
            if (isDebugEnabled)
                BuildProfileHelper.AddCompilerDirective(PERSPEC_DEBUG_SYMBOL);
            else
                BuildProfileHelper.RemoveCompilerDirective(PERSPEC_DEBUG_SYMBOL);
            
            Debug.Log($"[PerSpec] Debug logging {(isDebugEnabled ? "ENABLED" : "DISABLED")} via {BuildProfileHelper.ConfigurationMode}. Recompiling scripts...");
        }
        
        // Symbol manipulation methods removed - now handled by BuildProfileHelper
        
        #endregion
        
        #region Compilation Callbacks
        
        private void OnCompilationStarted(object obj)
        {
            isRefreshing = true;
            Repaint();
        }
        
        private void OnCompilationFinished(object obj)
        {
            isRefreshing = false;
            CheckDebugSymbol();
            Repaint();
        }
        
        #endregion
        
        #region Quick Toggle Methods (accessed via Control Center)
        
        // These methods are now called from PerSpecControlCenter
        private static void EnableDebugLogging()
        {
            SetDebugEnabled(true);
        }
        
        private static bool ValidateEnableDebugLogging()
        {
            return !IsDebugEnabled();
        }
        
        private static void DisableDebugLogging()
        {
            SetDebugEnabled(false);
        }
        
        private static bool ValidateDisableDebugLogging()
        {
            return IsDebugEnabled();
        }
        
        private static bool IsDebugEnabled()
        {
            return BuildProfileHelper.HasCompilerDirective(PERSPEC_DEBUG_SYMBOL);
        }
        
        private static void SetDebugEnabled(bool enabled)
        {
            var window = CreateInstance<PerSpecDebugWindow>();
            window.isDebugEnabled = !enabled; // Toggle will flip it
            window.ToggleDebugSymbol();
            DestroyImmediate(window);
        }
        
        #endregion
    }
}