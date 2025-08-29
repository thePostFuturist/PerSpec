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
        /// </summary>
        public static string PackagePath
        {
            get
            {
                if (string.IsNullOrEmpty(_cachedPackagePath))
                {
                    var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForPackageName(PACKAGE_NAME);
                    if (packageInfo != null)
                    {
                        _cachedPackagePath = packageInfo.resolvedPath;
                    }
                    else
                    {
                        // Fallback for development scenario
                        _cachedPackagePath = Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName, 
                            "Packages", PACKAGE_NAME);
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
    }
}