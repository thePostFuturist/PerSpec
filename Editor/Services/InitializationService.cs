using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace PerSpec.Editor.Services
{
    /// <summary>
    /// Service for managing PerSpec initialization and setup
    /// </summary>
    public static class InitializationService
    {
        #region Properties
        
        public static string ProjectPerSpecPath => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "PerSpec");
        public static string DatabasePath => Path.Combine(ProjectPerSpecPath, "test_coordination.db");
        public static string ScriptsPath => Path.Combine(ProjectPerSpecPath, "Scripts");
        public static string TestResultsPath => Path.Combine(ProjectPerSpecPath, "TestResults");
        
        public static bool IsInitialized => Directory.Exists(ProjectPerSpecPath);
        
        public static DateTime? InitializedDate
        {
            get
            {
                if (IsInitialized)
                    return Directory.GetCreationTime(ProjectPerSpecPath);
                return null;
            }
        }
        
        public static long DatabaseSize
        {
            get
            {
                if (File.Exists(DatabasePath))
                    return new FileInfo(DatabasePath).Length;
                return 0;
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Initialize PerSpec working directories
        /// </summary>
        public static bool Initialize()
        {
            try
            {
                // Create main directory
                if (!Directory.Exists(ProjectPerSpecPath))
                {
                    Directory.CreateDirectory(ProjectPerSpecPath);
                    Debug.Log($"[PerSpec] Created working directory: {ProjectPerSpecPath}");
                }
                
                // Create Scripts subdirectory
                if (!Directory.Exists(ScriptsPath))
                {
                    Directory.CreateDirectory(ScriptsPath);
                    CreateWrapperScripts();
                }
                
                // Create .gitignore
                CreateGitIgnore();
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PerSpec] Failed to initialize: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Reset PerSpec installation
        /// </summary>
        public static bool Reset()
        {
            try
            {
                if (Directory.Exists(ProjectPerSpecPath))
                {
                    Directory.Delete(ProjectPerSpecPath, true);
                    Debug.Log("[PerSpec] Reset complete. Directories removed.");
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PerSpec] Failed to reset: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Open working directory in file explorer
        /// </summary>
        public static void OpenWorkingDirectory()
        {
            if (IsInitialized)
            {
                EditorUtility.RevealInFinder(ProjectPerSpecPath);
            }
        }
        
        /// <summary>
        /// Get initialization status summary
        /// </summary>
        public static string GetStatusSummary()
        {
            if (!IsInitialized)
                return "Not initialized";
                
            var dbSize = DatabaseSize / 1024f; // Convert to KB
            return $"Initialized • DB: {dbSize:F1} KB • Created: {InitializedDate:yyyy-MM-dd}";
        }
        
        #endregion
        
        #region Private Methods
        
        private static void CreateWrapperScripts()
        {
            // Create convenience wrapper scripts with dynamic paths
            string refreshPath = PackagePathResolver.GetPythonScriptPath("quick_refresh.py");
            string testPath = PackagePathResolver.GetPythonScriptPath("quick_test.py");
            string logsPath = PackagePathResolver.GetPythonScriptPath("quick_logs.py");
            
            string refreshScript = $@"@echo off
python ""{refreshPath}"" %*";
            
            string testScript = $@"@echo off
python ""{testPath}"" %*";
            
            string logsScript = $@"@echo off
python ""{logsPath}"" %*";
            
            File.WriteAllText(Path.Combine(ScriptsPath, "refresh.bat"), refreshScript);
            File.WriteAllText(Path.Combine(ScriptsPath, "test.bat"), testScript);
            File.WriteAllText(Path.Combine(ScriptsPath, "logs.bat"), logsScript);
            
            // Create Unix shell scripts too
            string refreshShScript = $@"#!/bin/bash
python ""{refreshPath}"" ""$@""";
            string testShScript = $@"#!/bin/bash
python ""{testPath}"" ""$@""";
            string logsShScript = $@"#!/bin/bash
python ""{logsPath}"" ""$@""";
            
            File.WriteAllText(Path.Combine(ScriptsPath, "refresh.sh"), refreshShScript);
            File.WriteAllText(Path.Combine(ScriptsPath, "test.sh"), testShScript);
            File.WriteAllText(Path.Combine(ScriptsPath, "logs.sh"), logsShScript);
            
            // Make shell scripts executable on Unix
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.LinuxEditor)
            {
                try
                {
                    var chmod = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = $"+x \"{Path.Combine(ScriptsPath, "*.sh")}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    System.Diagnostics.Process.Start(chmod)?.WaitForExit(1000);
                }
                catch { /* Ignore chmod errors */ }
            }
            
            Debug.Log("[PerSpec] Created convenience wrapper scripts");
        }
        
        private static void CreateGitIgnore()
        {
            string gitignorePath = Path.Combine(ProjectPerSpecPath, ".gitignore");
            if (!File.Exists(gitignorePath))
            {
                string content = @"# PerSpec working files
*.db
*.db-shm
*.db-wal
*.log
*.tmp";
                File.WriteAllText(gitignorePath, content);
            }
        }
        
        #endregion
    }
}