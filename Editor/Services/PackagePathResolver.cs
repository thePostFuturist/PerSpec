using System.IO;
using UnityEditor;

namespace PerSpec.Editor.Services
{
    /// <summary>
    /// Resolves package paths dynamically to support both development (Packages/) and installed (Library/PackageCache/) scenarios
    /// </summary>
    public static class PackagePathResolver
    {
        private const string PACKAGE_NAME = "com.digitraver.perspec";
        private static string _cachedPackagePath;
        
        /// <summary>
        /// Gets the actual filesystem path to the PerSpec package
        /// Auto-refreshes if cached path is invalid
        /// </summary>
        public static string PackagePath
        {
            get
            {
                // Validate cached path and refresh if needed
                if (!string.IsNullOrEmpty(_cachedPackagePath) && !Directory.Exists(_cachedPackagePath))
                {
                    UnityEngine.Debug.LogWarning($"[PerSpec] Cached package path no longer valid: {_cachedPackagePath}. Refreshing...");
                    _cachedPackagePath = null;
                }
                
                if (string.IsNullOrEmpty(_cachedPackagePath))
                {
                    var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForPackageName(PACKAGE_NAME);
                    if (packageInfo != null)
                    {
                        _cachedPackagePath = packageInfo.resolvedPath;
                        UnityEngine.Debug.Log($"[PerSpec] Package path resolved: {_cachedPackagePath}");
                        
                        // Update location file whenever path is resolved
                        PackageLocationTracker.UpdateLocationFile();
                    }
                    else
                    {
                        // Fallback for development scenario
                        _cachedPackagePath = Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName, 
                            "Packages", PACKAGE_NAME);
                        
                        // If fallback also doesn't exist, try to find in PackageCache
                        if (!Directory.Exists(_cachedPackagePath))
                        {
                            var packageCachePath = Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName, 
                                "Library", "PackageCache");
                            if (Directory.Exists(packageCachePath))
                            {
                                var dirs = Directory.GetDirectories(packageCachePath, PACKAGE_NAME + "@*");
                                if (dirs.Length > 0)
                                {
                                    _cachedPackagePath = dirs[0];
                                    UnityEngine.Debug.Log($"[PerSpec] Found package in cache: {_cachedPackagePath}");
                                    
                                    // Update location file when found in cache
                                    PackageLocationTracker.UpdateLocationFile();
                                }
                            }
                        }
                    }
                }
                return _cachedPackagePath;
            }
        }
        
        /// <summary>
        /// Gets the path to ScriptingTools/Coordination/Scripts directory
        /// </summary>
        public static string ScriptingToolsPath => Path.Combine(PackagePath, "ScriptingTools", "Coordination", "Scripts");
        
        /// <summary>
        /// Gets the path to a specific Python script
        /// </summary>
        public static string GetPythonScriptPath(string scriptName)
        {
            return Path.Combine(ScriptingToolsPath, scriptName);
        }
        
        /// <summary>
        /// Gets a relative path from project root to the Python scripts
        /// For display purposes and for creating wrapper scripts
        /// </summary>
        public static string GetRelativeScriptPath(string scriptName)
        {
            string projectRoot = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
            string fullPath = GetPythonScriptPath(scriptName);
            
            // Try to make path relative to project root
            if (fullPath.StartsWith(projectRoot))
            {
                return fullPath.Substring(projectRoot.Length + 1).Replace('\\', '/');
            }
            
            return fullPath.Replace('\\', '/');
        }
        
        /// <summary>
        /// Clears the cached package path (useful if package location changes)
        /// </summary>
        public static void ClearCache()
        {
            _cachedPackagePath = null;
        }
        
        /// <summary>
        /// Forces a refresh of the package path and returns the new path
        /// </summary>
        public static string RefreshPackagePath()
        {
            ClearCache();
            return PackagePath;
        }
        
        /// <summary>
        /// Checks if the current package path is valid
        /// </summary>
        public static bool IsPackagePathValid()
        {
            return !string.IsNullOrEmpty(_cachedPackagePath) && Directory.Exists(_cachedPackagePath);
        }
        
        /// <summary>
        /// Gets information about the package location for debugging
        /// </summary>
        public static string GetPackageLocationInfo()
        {
            if (string.IsNullOrEmpty(PackagePath))
                return "Package not found";
                
            if (PackagePath.Contains("PackageCache"))
            {
                // Extract hash from path
                var match = System.Text.RegularExpressions.Regex.Match(PackagePath, @"@([a-f0-9]+)");
                if (match.Success)
                    return $"PackageCache (hash: {match.Groups[1].Value})";
                return "PackageCache";
            }
            else if (PackagePath.Contains("Packages"))
            {
                return "Local Development";
            }
            
            return "Custom Location";
        }
    }
}