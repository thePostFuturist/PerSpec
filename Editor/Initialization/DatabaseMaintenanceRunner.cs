using System;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using static UnityEditor.PackageManager.Events;

namespace PerSpec.Editor.Initialization
{
    /// <summary>
    /// Automatically runs database maintenance when the package is imported or updated
    /// </summary>
    [InitializeOnLoad]
    public static class DatabaseMaintenanceRunner
    {
        private const string LAST_MAINTENANCE_KEY = "PerSpec_LastDatabaseMaintenance";
        private const string PACKAGE_VERSION_KEY = "PerSpec_LastPackageVersion";
        private const double MAINTENANCE_INTERVAL_DAYS = 7; // Run at most once per week
        
        static DatabaseMaintenanceRunner()
        {
            // Check if we should run maintenance
            EditorApplication.delayCall += CheckAndRunMaintenance;
            
            // Also listen for package events
            Events.registeredPackages += OnPackagesRegistered;
            Events.registeringPackages += OnPackagesRegistering;
        }
        
        private static void OnPackagesRegistering(PackageRegistrationEventArgs args)
        {
            // Package is being registered
            foreach (var packageInfo in args.added)
            {
                if (packageInfo.name == "com.digitraver.perspec")
                {
                    UnityEngine.Debug.Log($"[PerSpec] Package registering: {packageInfo.version}");
                    EditorApplication.delayCall += () => RunMaintenanceIfNeeded(true);
                }
            }
        }
        
        private static void OnPackagesRegistered(PackageRegistrationEventArgs args)
        {
            // Package registration complete
            foreach (var packageInfo in args.added)
            {
                if (packageInfo.name == "com.digitraver.perspec")
                {
                    UnityEngine.Debug.Log($"[PerSpec] Package registered: {packageInfo.version}");
                }
            }
            
            foreach (var packageInfo in args.changedTo)
            {
                if (packageInfo.name == "com.digitraver.perspec")
                {
                    UnityEngine.Debug.Log($"[PerSpec] Package updated to: {packageInfo.version}");
                    EditorApplication.delayCall += () => RunMaintenanceIfNeeded(true);
                }
            }
        }
        
        private static void CheckAndRunMaintenance()
        {
            // Don't run during play mode
            if (Application.isPlaying)
            {
                return;
            }
            
            // Check if enough time has passed since last maintenance
            string lastMaintenanceStr = EditorPrefs.GetString(LAST_MAINTENANCE_KEY, "");
            
            bool shouldRun = false;
            string reason = "";
            
            // Check if this is first run
            if (string.IsNullOrEmpty(lastMaintenanceStr))
            {
                shouldRun = true;
                reason = "First run after package installation";
            }
            else
            {
                // Check if enough time has passed
                if (DateTime.TryParse(lastMaintenanceStr, out DateTime lastMaintenance))
                {
                    var daysSinceLastRun = (DateTime.Now - lastMaintenance).TotalDays;
                    if (daysSinceLastRun >= MAINTENANCE_INTERVAL_DAYS)
                    {
                        shouldRun = true;
                        reason = $"Last run was {daysSinceLastRun:F1} days ago";
                    }
                }
                
                // Check if package version changed
                var currentVersion = GetPackageVersion();
                var lastVersion = EditorPrefs.GetString(PACKAGE_VERSION_KEY, "");
                
                if (!string.IsNullOrEmpty(currentVersion) && currentVersion != lastVersion)
                {
                    shouldRun = true;
                    reason = $"Package updated from {lastVersion} to {currentVersion}";
                }
            }
            
            if (shouldRun)
            {
                RunMaintenanceIfNeeded(false, reason);
            }
        }
        
        private static void RunMaintenanceIfNeeded(bool forceRun = false, string reason = "")
        {
            // Don't run if we're compiling or in play mode
            if (EditorApplication.isCompiling || Application.isPlaying)
            {
                // Reschedule for later
                EditorApplication.delayCall += () => RunMaintenanceIfNeeded(forceRun, reason);
                return;
            }
            
            try
            {
                string scriptPath = GetMaintenanceScriptPath();
                
                if (!File.Exists(scriptPath))
                {
                    UnityEngine.Debug.LogWarning($"[PerSpec] Database maintenance script not found at: {scriptPath}");
                    return;
                }
                
                if (!string.IsNullOrEmpty(reason))
                {
                    UnityEngine.Debug.Log($"[PerSpec] Running database maintenance: {reason}");
                }
                else
                {
                    UnityEngine.Debug.Log("[PerSpec] Running database maintenance...");
                }
                
                // Run the Python script
                var startInfo = new ProcessStartInfo
                {
                    FileName = GetPythonExecutable(),
                    Arguments = $"\"{scriptPath}\"",
                    WorkingDirectory = Path.GetDirectoryName(Application.dataPath),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(startInfo))
                {
                    process.WaitForExit(30000); // 30 second timeout
                    
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    
                    if (process.ExitCode == 0)
                    {
                        // Parse output for important messages
                        if (output.Contains("[SUCCESS]"))
                        {
                            UnityEngine.Debug.Log("[PerSpec] Database maintenance completed successfully");
                            
                            // Update last maintenance time
                            EditorPrefs.SetString(LAST_MAINTENANCE_KEY, DateTime.Now.ToString("O"));
                            
                            // Update package version
                            var currentVersion = GetPackageVersion();
                            if (!string.IsNullOrEmpty(currentVersion))
                            {
                                EditorPrefs.SetString(PACKAGE_VERSION_KEY, currentVersion);
                            }
                        }
                        
                        // Log any migrations that were applied
                        if (output.Contains("Applied") && output.Contains("migration"))
                        {
                            var lines = output.Split('\n');
                            foreach (var line in lines)
                            {
                                if (line.Contains("Migration") || line.Contains("Applied"))
                                {
                                    UnityEngine.Debug.Log($"[PerSpec] {line.Trim()}");
                                }
                            }
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"[PerSpec] Database maintenance failed with exit code {process.ExitCode}");
                        if (!string.IsNullOrEmpty(error))
                        {
                            UnityEngine.Debug.LogError($"[PerSpec] Error: {error}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[PerSpec] Failed to run database maintenance: {e.Message}");
            }
        }
        
        private static string GetMaintenanceScriptPath()
        {
            // First try the working directory (PerSpec/Coordination/Scripts)
            string projectPath = Directory.GetParent(Application.dataPath).FullName;
            string scriptPath = Path.Combine(projectPath, "PerSpec", "Coordination", "Scripts", "db_auto_maintenance.py");
            
            if (File.Exists(scriptPath))
            {
                return scriptPath;
            }
            
            // Fallback to package location
            string packagePath = Path.Combine(Application.dataPath, "..", "Packages", "com.digitraver.perspec");
            scriptPath = Path.Combine(packagePath, "Editor", "Coordination", "Scripts", "db_auto_maintenance.py");
            
            return scriptPath;
        }
        
        private static string GetPythonExecutable()
        {
            // Check for python3 first, then python
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                return "python";
            }
            else
            {
                // On Mac/Linux, prefer python3
                return "python3";
            }
        }
        
        private static string GetPackageVersion()
        {
            try
            {
                // Read package.json to get version
                string packagePath = Path.Combine(Application.dataPath, "..", "Packages", "com.digitraver.perspec", "package.json");
                if (File.Exists(packagePath))
                {
                    string json = File.ReadAllText(packagePath);
                    // Simple JSON parsing for version
                    var versionIndex = json.IndexOf("\"version\"");
                    if (versionIndex > 0)
                    {
                        var start = json.IndexOf("\"", versionIndex + 9) + 1;
                        var end = json.IndexOf("\"", start);
                        return json.Substring(start, end - start);
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
            
            return "";
        }
        
        [MenuItem("Tools/PerSpec/Database/Run Maintenance Now")]
        public static void RunMaintenanceManually()
        {
            RunMaintenanceIfNeeded(true, "Manual run from menu");
        }
        
        [MenuItem("Tools/PerSpec/Database/Reset Maintenance Timer")]
        public static void ResetMaintenanceTimer()
        {
            EditorPrefs.DeleteKey(LAST_MAINTENANCE_KEY);
            EditorPrefs.DeleteKey(PACKAGE_VERSION_KEY);
            UnityEngine.Debug.Log("[PerSpec] Maintenance timer reset. Maintenance will run on next editor restart.");
        }
    }
}