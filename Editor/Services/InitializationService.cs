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
        
        [MenuItem("Tools/PerSpec/Refresh Coordination Scripts", false, 15)]
        private static void MenuRefreshScripts()
        {
            if (!IsInitialized)
            {
                Debug.LogError("[PerSpec] Cannot refresh scripts - PerSpec not initialized. Use Tools > PerSpec > Initialize first.");
                return;
            }
            
            if (RefreshCoordinationScripts())
            {
                Debug.Log("[PerSpec] Coordination scripts refreshed successfully");
            }
        }
        
        [MenuItem("Tools/PerSpec/Refresh Coordination Scripts", true)]
        private static bool ValidateMenuRefreshScripts()
        {
            return IsInitialized;
        }
        
        #endregion
        
        #region Properties
        
        public static string ProjectPerSpecPath => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "PerSpec");
        public static string DatabasePath => Path.Combine(ProjectPerSpecPath, "test_coordination.db");
        public static string CoordinationScriptsPath => Path.Combine(ProjectPerSpecPath, "Coordination", "Scripts");
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
                
                // Create Coordination/Scripts subdirectory and copy scripts
                if (!Directory.Exists(CoordinationScriptsPath))
                {
                    Directory.CreateDirectory(CoordinationScriptsPath);
                    CopyCoordinationScripts();
                }
                
                // Create TestResults subdirectory
                if (!Directory.Exists(TestResultsPath))
                {
                    Directory.CreateDirectory(TestResultsPath);
                    Debug.Log($"[PerSpec] Created TestResults directory: {TestResultsPath}");
                }
                
                // Create .gitignore
                CreateGitIgnore();
                
                // Create package location file
                PackageLocationTracker.UpdateLocationFile();
                
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
        /// Refresh coordination scripts from package
        /// </summary>
        public static bool RefreshCoordinationScripts()
        {
            try
            {
                if (!IsInitialized)
                {
                    Debug.LogError("[PerSpec] Cannot refresh scripts - PerSpec not initialized");
                    return false;
                }
                
                // Copy coordination scripts from package
                CopyCoordinationScripts();
                
                // Update package location file (still useful for reference)
                PackageLocationTracker.UpdateLocationFile();
                
                Debug.Log("[PerSpec] Scripts refreshed with hardcoded paths");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PerSpec] Failed to refresh scripts: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Force update coordination scripts (delete existing and copy fresh)
        /// </summary>
        public static bool ForceUpdateCoordinationScripts()
        {
            try
            {
                if (!IsInitialized)
                {
                    Debug.LogError("[PerSpec] Cannot force update scripts - PerSpec not initialized");
                    return false;
                }
                
                // Delete existing scripts directory to ensure clean update
                if (Directory.Exists(CoordinationScriptsPath))
                {
                    Directory.Delete(CoordinationScriptsPath, true);
                    Debug.Log($"[PerSpec] Removed existing scripts from {CoordinationScriptsPath}");
                }
                
                // Recreate directory
                Directory.CreateDirectory(CoordinationScriptsPath);
                
                // Copy fresh scripts from package
                CopyCoordinationScripts();
                
                // Update package location file
                PackageLocationTracker.UpdateLocationFile();
                
                Debug.Log("[PerSpec] Scripts force updated from package");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PerSpec] Failed to force update scripts: {e.Message}");
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
                
            // Check if coordination scripts exist
            if (!Directory.Exists(CoordinationScriptsPath))
                return true;
                
            // Check if the test script exists
            string testScript = Path.Combine(CoordinationScriptsPath, "quick_test.py");
            return !File.Exists(testScript);
        }
        
        #endregion
        
        #region Private Methods
        
        private static void CopyCoordinationScripts()
        {
            try
            {
                // Find the package location
                string packagePath = PackagePathResolver.PackagePath;
                if (string.IsNullOrEmpty(packagePath))
                {
                    Debug.LogError("[PerSpec] Could not find package path to copy coordination scripts");
                    return;
                }
                
                string sourceScriptsPath = Path.Combine(packagePath, "ScriptingTools", "Coordination", "Scripts");
                if (!Directory.Exists(sourceScriptsPath))
                {
                    Debug.LogError($"[PerSpec] Source scripts directory not found: {sourceScriptsPath}");
                    return;
                }
                
                // Ensure destination directory exists
                if (!Directory.Exists(CoordinationScriptsPath))
                {
                    Directory.CreateDirectory(CoordinationScriptsPath);
                }
                
                // Copy all Python scripts
                foreach (string sourceFile in Directory.GetFiles(sourceScriptsPath, "*.py"))
                {
                    string fileName = Path.GetFileName(sourceFile);
                    string destFile = Path.Combine(CoordinationScriptsPath, fileName);
                    File.Copy(sourceFile, destFile, true);
                }
                
                Debug.Log($"[PerSpec] Copied coordination scripts to {CoordinationScriptsPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PerSpec] Failed to copy coordination scripts: {e.Message}");
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