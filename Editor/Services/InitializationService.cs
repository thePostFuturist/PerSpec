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
        #region Menu Items
        
        [MenuItem("Tools/PerSpec/Refresh Wrapper Scripts", false, 15)]
        private static void MenuRefreshScripts()
        {
            if (!IsInitialized)
            {
                Debug.LogError("[PerSpec] Cannot refresh scripts - PerSpec not initialized. Use Tools > PerSpec > Initialize first.");
                return;
            }
            
            if (RefreshWrapperScripts())
            {
                Debug.Log("[PerSpec] Wrapper scripts refreshed successfully");
            }
        }
        
        [MenuItem("Tools/PerSpec/Refresh Wrapper Scripts", true)]
        private static bool ValidateMenuRefreshScripts()
        {
            return IsInitialized;
        }
        
        #endregion
        
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
        
        /// <summary>
        /// Refresh wrapper scripts with current package location
        /// </summary>
        public static bool RefreshWrapperScripts()
        {
            try
            {
                if (!IsInitialized)
                {
                    Debug.LogError("[PerSpec] Cannot refresh scripts - PerSpec not initialized");
                    return false;
                }
                
                // Force package path refresh
                PackagePathResolver.RefreshPackagePath();
                
                // Recreate wrapper scripts
                CreateWrapperScripts();
                
                Debug.Log($"[PerSpec] Wrapper scripts refreshed. Package location: {PackagePathResolver.GetPackageLocationInfo()}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PerSpec] Failed to refresh wrapper scripts: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Check if wrapper scripts need refreshing
        /// </summary>
        public static bool ScriptsNeedRefresh()
        {
            if (!IsInitialized)
                return false;
                
            // Check if any wrapper script exists
            string testScript = Path.Combine(ScriptsPath, "test.py");
            if (!File.Exists(testScript))
                return true;
                
            // Check if it's using the old hardcoded path format
            try
            {
                string content = File.ReadAllText(testScript);
                // Old scripts have hardcoded paths, new ones have dynamic finding logic
                return !content.Contains("possible_paths");
            }
            catch
            {
                return true;
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private static void CreateWrapperScripts()
        {
            // Create dynamic Python wrapper scripts that find the package at runtime
            CreateDynamicWrapperScript("refresh", "quick_refresh.py");
            CreateDynamicWrapperScript("test", "quick_test.py");
            CreateDynamicWrapperScript("logs", "quick_logs.py");
            CreateDynamicWrapperScript("init_db", "db_initializer.py");
            CreateDynamicWrapperScript("monitor", "db_monitor.py");
            
            Debug.Log("[PerSpec] Created dynamic wrapper scripts");
        }
        
        private static void CreateDynamicWrapperScript(string name, string scriptFile)
        {
            // Create a Python script that dynamically finds the package
            string pythonFinder = $@"#!/usr/bin/env python3
import os
import sys
from pathlib import Path

# Prevent Python from creating .pyc files in the package directory
sys.dont_write_bytecode = True
os.environ['PYTHONDONTWRITEBYTECODE'] = '1'

# Set Python cache to PerSpec directory instead of package location
project_root = Path(__file__).parent.parent.parent
cache_dir = project_root / 'PerSpec' / 'cache'
cache_dir.mkdir(parents=True, exist_ok=True)
os.environ['PYTHONPYCACHEPREFIX'] = str(cache_dir)

# Possible locations for the PerSpec package
possible_paths = [
    # Development location
    project_root / 'Packages' / 'com.digitraver.perspec',
    # Check all PackageCache entries with hash
    *(project_root / 'Library' / 'PackageCache').glob('com.digitraver.perspec@*') if (project_root / 'Library' / 'PackageCache').exists() else []
]

# Find the script
script_found = False
for package_path in possible_paths:
    script_path = package_path / 'ScriptingTools' / 'Coordination' / 'Scripts' / '{scriptFile}'
    if script_path.exists():
        # Execute the script with all arguments, ensuring no bytecode in package dir
        import subprocess
        env = os.environ.copy()
        env['PYTHONDONTWRITEBYTECODE'] = '1'
        env['PYTHONPYCACHEPREFIX'] = str(cache_dir)
        result = subprocess.run([sys.executable, str(script_path)] + sys.argv[1:], 
                              stdout=sys.stdout, stderr=sys.stderr, env=env)
        sys.exit(result.returncode)
        script_found = True
        break

if not script_found:
    print(f""[PerSpec] Error: Could not find {scriptFile}"")
    print(""Please ensure the PerSpec package is properly installed."")
    print(""Try reinitializing from Unity: Tools > PerSpec > Initialize"")
    sys.exit(1)
";
            
            // Write Python wrapper
            string pyPath = Path.Combine(ScriptsPath, $"{name}.py");
            File.WriteAllText(pyPath, pythonFinder);
            
            // Create batch file for Windows (with bytecode prevention)
            string batScript = $@"@echo off
set PYTHONDONTWRITEBYTECODE=1
python ""%~dp0{name}.py"" %*";
            File.WriteAllText(Path.Combine(ScriptsPath, $"{name}.bat"), batScript);
            
            // Create shell script for Unix (with bytecode prevention)
            string shScript = $@"#!/bin/bash
export PYTHONDONTWRITEBYTECODE=1
python ""$(dirname ""$0"")/{name}.py"" ""$@""";
            File.WriteAllText(Path.Combine(ScriptsPath, $"{name}.sh"), shScript);
            
            // Make scripts executable on Unix
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.LinuxEditor)
            {
                try
                {
                    var chmod = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = $"+x \"{Path.Combine(ScriptsPath, name + ".sh")}\" \"{Path.Combine(ScriptsPath, name + ".py")}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    System.Diagnostics.Process.Start(chmod)?.WaitForExit(1000);
                }
                catch { /* Ignore chmod errors */ }
            }
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