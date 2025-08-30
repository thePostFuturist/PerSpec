using System.IO;
using UnityEngine;
using UnityEditor;

namespace PerSpec.Editor.Services
{
    /// <summary>
    /// Tracks and maintains the current package location in a file for external tools and LLMs
    /// </summary>
    [InitializeOnLoad]
    public static class PackageLocationTracker
    {
        private const string LOCATION_FILE = "package_location.txt";
        private static string LocationFilePath => Path.Combine(GetPerSpecRoot(), LOCATION_FILE);
        private static string GetPerSpecRoot() => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "PerSpec");
        
        static PackageLocationTracker()
        {
            // Update on Unity startup
            EditorApplication.delayCall += UpdateLocationFile;
            
            // Update on domain reload
            AssemblyReloadEvents.afterAssemblyReload += UpdateLocationFile;
            
            // Update periodically during editor updates
            EditorApplication.update += OnEditorUpdate;
        }
        
        private static float lastUpdateTime = 0;
        private static void OnEditorUpdate()
        {
            // Update every 30 seconds while editor is running
            if (Time.realtimeSinceStartup - lastUpdateTime > 30f)
            {
                lastUpdateTime = Time.realtimeSinceStartup;
                UpdateLocationFile();
            }
        }
        
        [MenuItem("Tools/PerSpec/Update Package Location", priority = 50)]
        public static void UpdateLocationFileMenuItem()
        {
            UpdateLocationFile();
            Debug.Log($"[PerSpec] Package location updated: {GetCurrentLocation()}");
        }
        
        public static void UpdateLocationFile()
        {
            try
            {
                // Ensure PerSpec directory exists
                var perspecRoot = GetPerSpecRoot();
                if (!Directory.Exists(perspecRoot))
                {
                    // Don't create if it doesn't exist - let initialization handle that
                    return;
                }
                
                // Get current package path
                var packagePath = PackagePathResolver.PackagePath;
                if (string.IsNullOrEmpty(packagePath))
                {
                    Debug.LogWarning("[PerSpec] Could not determine package path");
                    return;
                }
                
                // Convert to relative path if possible for portability
                var projectRoot = Directory.GetParent(Application.dataPath).FullName;
                var relativePath = packagePath;
                
                if (packagePath.StartsWith(projectRoot))
                {
                    relativePath = packagePath.Substring(projectRoot.Length + 1).Replace('\\', '/');
                }
                
                // Write both absolute and relative paths
                var content = $"{relativePath}\n# Absolute: {packagePath}\n# Updated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                File.WriteAllText(LocationFilePath, content);
                
                // Also write a JSON version for better parsing
                var jsonPath = Path.Combine(perspecRoot, "package_info.json");
                var jsonContent = $@"{{
  ""package_name"": ""com.digitraver.perspec"",
  ""relative_path"": ""{relativePath.Replace("\\", "\\\\")}"",
  ""absolute_path"": ""{packagePath.Replace("\\", "\\\\")}"",
  ""updated"": ""{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}"",
  ""unity_version"": ""{Application.unityVersion}"",
  ""package_location_type"": ""{PackagePathResolver.GetPackageLocationInfo()}""
}}";
                File.WriteAllText(jsonPath, jsonContent);
                
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PerSpec] Failed to update location file: {e.Message}");
            }
        }
        
        public static string GetCurrentLocation()
        {
            if (File.Exists(LocationFilePath))
            {
                var lines = File.ReadAllLines(LocationFilePath);
                if (lines.Length > 0)
                {
                    return lines[0]; // First line is the relative path
                }
            }
            return null;
        }
        
        public static void EnsureLocationFileExists()
        {
            if (!File.Exists(LocationFilePath))
            {
                UpdateLocationFile();
            }
        }
    }
}