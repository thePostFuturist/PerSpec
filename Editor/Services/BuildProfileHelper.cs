using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;

namespace PerSpec.Editor.Services
{
    /// <summary>
    /// Helper class for managing compiler directives with BuildProfile support.
    /// Provides a unified API that works with both Unity 6's BuildProfiles and 
    /// the classic PlayerSettings approach.
    /// </summary>
    public static class BuildProfileHelper
    {
        #region Constants
        
        private const string BUILD_PROFILE_NAMESPACE = "UnityEditor.Build.Profile";
        private const string BUILD_PROFILE_CLASS = "BuildProfile";
        private const string GET_ACTIVE_METHOD = "GetActiveBuildProfile";
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Checks if BuildProfiles are supported in the current Unity version
        /// </summary>
        public static bool AreBuildProfilesSupported
        {
            get
            {
                // Check if Unity 6 or newer (2023.3+)
#if UNITY_6000_0_OR_NEWER || UNITY_2023_3_OR_NEWER
                // Try to find the BuildProfile type
                var buildProfileType = GetBuildProfileType();
                return buildProfileType != null;
#else
                return false;
#endif
            }
        }
        
        /// <summary>
        /// Checks if a BuildProfile is currently active
        /// </summary>
        public static bool HasActiveBuildProfile
        {
            get
            {
                if (!AreBuildProfilesSupported)
                    return false;
                
                var activeBuildProfile = GetActiveBuildProfile();
                return activeBuildProfile != null;
            }
        }
        
        /// <summary>
        /// Gets a status string describing the current configuration mode
        /// </summary>
        public static string ConfigurationMode
        {
            get
            {
                if (!AreBuildProfilesSupported)
                    return "Classic PlayerSettings";
                
                if (HasActiveBuildProfile)
                    return "BuildProfile Active";
                
                return "Platform Profile (using PlayerSettings)";
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Adds a compiler directive to the current configuration
        /// </summary>
        public static void AddCompilerDirective(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
                return;
            
            // Try BuildProfile first if available
            if (HasActiveBuildProfile)
            {
                if (AddSymbolToBuildProfile(symbol))
                {
                    Debug.Log($"[PerSpec] Added '{symbol}' to active BuildProfile");
                    return;
                }
            }
            
            // Fall back to PlayerSettings
            AddSymbolToPlayerSettings(symbol);
        }
        
        /// <summary>
        /// Removes a compiler directive from the current configuration
        /// </summary>
        public static void RemoveCompilerDirective(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
                return;
            
            // Try BuildProfile first if available
            if (HasActiveBuildProfile)
            {
                if (RemoveSymbolFromBuildProfile(symbol))
                {
                    Debug.Log($"[PerSpec] Removed '{symbol}' from active BuildProfile");
                    return;
                }
            }
            
            // Fall back to PlayerSettings
            RemoveSymbolFromPlayerSettings(symbol);
        }
        
        /// <summary>
        /// Checks if a compiler directive exists in the current configuration
        /// </summary>
        public static bool HasCompilerDirective(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
                return false;
            
            // Check BuildProfile first if available
            if (HasActiveBuildProfile)
            {
                var hasInProfile = HasSymbolInBuildProfile(symbol);
                if (hasInProfile.HasValue)
                    return hasInProfile.Value;
            }
            
            // Fall back to PlayerSettings
            return HasSymbolInPlayerSettings(symbol);
        }
        
        /// <summary>
        /// Toggles a compiler directive in the current configuration
        /// </summary>
        public static void ToggleCompilerDirective(string symbol)
        {
            if (HasCompilerDirective(symbol))
                RemoveCompilerDirective(symbol);
            else
                AddCompilerDirective(symbol);
        }
        
        /// <summary>
        /// Scans the project for all BuildProfile assets
        /// </summary>
        /// <returns>Array of BuildProfileInfo containing profile details</returns>
        public static BuildProfileInfo[] ScanForBuildProfiles()
        {
            if (!AreBuildProfilesSupported)
                return new BuildProfileInfo[0];
            
            try
            {
                var buildProfileType = GetBuildProfileType();
                if (buildProfileType == null)
                    return new BuildProfileInfo[0];
                
                // Find all BuildProfile assets
                string[] guids = AssetDatabase.FindAssets($"t:{BUILD_PROFILE_CLASS}");
                var profiles = new System.Collections.Generic.List<BuildProfileInfo>();
                
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var profileAsset = AssetDatabase.LoadAssetAtPath(path, buildProfileType);
                    
                    if (profileAsset != null)
                    {
                        profiles.Add(new BuildProfileInfo
                        {
                            Name = profileAsset.name,
                            Path = path,
                            IsActive = IsProfileActive(profileAsset)
                        });
                    }
                }
                
                Debug.Log($"[PerSpec] Found {profiles.Count} BuildProfile(s) in project");
                return profiles.ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PerSpec] Error scanning for BuildProfiles: {ex.Message}");
                return new BuildProfileInfo[0];
            }
        }
        
        /// <summary>
        /// Sets a BuildProfile as active by its asset path
        /// </summary>
        public static bool SetActiveBuildProfile(string assetPath)
        {
            if (!AreBuildProfilesSupported || string.IsNullOrEmpty(assetPath))
                return false;
            
            try
            {
                var buildProfileType = GetBuildProfileType();
                if (buildProfileType == null)
                    return false;
                
                // Load the profile
                var profile = AssetDatabase.LoadAssetAtPath(assetPath, buildProfileType);
                if (profile == null)
                {
                    Debug.LogError($"[PerSpec] Failed to load BuildProfile at path: {assetPath}");
                    return false;
                }
                
                // Get SetActiveBuildProfile method
                var setActiveMethod = buildProfileType.GetMethod("SetActiveBuildProfile",
                    BindingFlags.Public | BindingFlags.Static);
                
                if (setActiveMethod == null)
                {
                    Debug.LogError("[PerSpec] SetActiveBuildProfile method not found");
                    return false;
                }
                
                // Set as active
                setActiveMethod.Invoke(null, new object[] { profile });
                
                Debug.Log($"[PerSpec] Activated BuildProfile: {profile.name}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PerSpec] Error setting active BuildProfile: {ex.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region Helper Classes
        
        /// <summary>
        /// Information about a BuildProfile asset
        /// </summary>
        public struct BuildProfileInfo
        {
            public string Name;
            public string Path;
            public bool IsActive;
        }
        
        #endregion
        
        #region BuildProfile Methods (Unity 6+)
        
        private static Type GetBuildProfileType()
        {
            try
            {
                // Try to find the BuildProfile type using reflection
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    var type = assembly.GetType($"{BUILD_PROFILE_NAMESPACE}.{BUILD_PROFILE_CLASS}");
                    if (type != null)
                        return type;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PerSpec] Error checking for BuildProfile type: {ex.Message}");
            }
            
            return null;
        }
        
        private static object GetActiveBuildProfile()
        {
            try
            {
                var buildProfileType = GetBuildProfileType();
                if (buildProfileType == null)
                    return null;
                
                var getActiveMethod = buildProfileType.GetMethod(GET_ACTIVE_METHOD, 
                    BindingFlags.Public | BindingFlags.Static);
                
                if (getActiveMethod == null)
                    return null;
                
                // This may return null if a platform profile is active
                return getActiveMethod.Invoke(null, null);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PerSpec] Error getting active BuildProfile: {ex.Message}");
                return null;
            }
        }
        
        private static bool IsProfileActive(object profile)
        {
            if (profile == null)
                return false;
            
            var activeProfile = GetActiveBuildProfile();
            if (activeProfile == null)
                return false;
            
            // Compare by reference - they should be the same object if active
            return ReferenceEquals(profile, activeProfile);
        }
        
        private static bool AddSymbolToBuildProfile(string symbol)
        {
            try
            {
                var buildProfile = GetActiveBuildProfile();
                if (buildProfile == null)
                    return false;
                
                var scriptingDefinesProperty = buildProfile.GetType().GetProperty("scriptingDefines");
                if (scriptingDefinesProperty == null)
                    return false;
                
                var currentDefines = scriptingDefinesProperty.GetValue(buildProfile) as string[];
                if (currentDefines == null)
                    currentDefines = new string[0];
                
                // Check if already exists
                if (currentDefines.Contains(symbol))
                    return true;
                
                // Add the symbol
                var newDefines = currentDefines.Concat(new[] { symbol }).ToArray();
                scriptingDefinesProperty.SetValue(buildProfile, newDefines);
                
                // Mark as dirty and save
                EditorUtility.SetDirty(buildProfile as UnityEngine.Object);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PerSpec] Error adding symbol to BuildProfile: {ex.Message}");
                return false;
            }
        }
        
        private static bool RemoveSymbolFromBuildProfile(string symbol)
        {
            try
            {
                var buildProfile = GetActiveBuildProfile();
                if (buildProfile == null)
                    return false;
                
                var scriptingDefinesProperty = buildProfile.GetType().GetProperty("scriptingDefines");
                if (scriptingDefinesProperty == null)
                    return false;
                
                var currentDefines = scriptingDefinesProperty.GetValue(buildProfile) as string[];
                if (currentDefines == null || !currentDefines.Contains(symbol))
                    return true; // Already removed
                
                // Remove the symbol
                var newDefines = currentDefines.Where(d => d != symbol).ToArray();
                scriptingDefinesProperty.SetValue(buildProfile, newDefines);
                
                // Mark as dirty and save
                EditorUtility.SetDirty(buildProfile as UnityEngine.Object);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PerSpec] Error removing symbol from BuildProfile: {ex.Message}");
                return false;
            }
        }
        
        private static bool? HasSymbolInBuildProfile(string symbol)
        {
            try
            {
                var buildProfile = GetActiveBuildProfile();
                if (buildProfile == null)
                    return null;
                
                var scriptingDefinesProperty = buildProfile.GetType().GetProperty("scriptingDefines");
                if (scriptingDefinesProperty == null)
                    return null;
                
                var currentDefines = scriptingDefinesProperty.GetValue(buildProfile) as string[];
                if (currentDefines == null)
                    return false;
                
                return currentDefines.Contains(symbol);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PerSpec] Error checking symbol in BuildProfile: {ex.Message}");
                return null;
            }
        }
        
        #endregion
        
        #region PlayerSettings Methods (Classic)
        
        private static void AddSymbolToPlayerSettings(string symbol)
        {
            // Apply to all named build targets
            var namedTargets = new[]
            {
                NamedBuildTarget.Standalone,
                NamedBuildTarget.iOS,
                NamedBuildTarget.Android,
                NamedBuildTarget.WebGL,
                NamedBuildTarget.WindowsStoreApps,
                NamedBuildTarget.tvOS,
                NamedBuildTarget.LinuxHeadlessSimulation,
                NamedBuildTarget.Server
            };
            
            foreach (var target in namedTargets)
            {
                try
                {
                    string currentSymbols = PlayerSettings.GetScriptingDefineSymbols(target);
                    string newSymbols = AddSymbolToString(currentSymbols, symbol);
                    
                    if (currentSymbols != newSymbols)
                    {
                        PlayerSettings.SetScriptingDefineSymbols(target, newSymbols);
                    }
                }
                catch
                {
                    // Some build targets might not be available
                    continue;
                }
            }
            
            // Force recompilation
            AssetDatabase.Refresh();
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            
            Debug.Log($"[PerSpec] Added '{symbol}' via PlayerSettings");
        }
        
        private static void RemoveSymbolFromPlayerSettings(string symbol)
        {
            // Apply to all named build targets
            var namedTargets = new[]
            {
                NamedBuildTarget.Standalone,
                NamedBuildTarget.iOS,
                NamedBuildTarget.Android,
                NamedBuildTarget.WebGL,
                NamedBuildTarget.WindowsStoreApps,
                NamedBuildTarget.tvOS,
                NamedBuildTarget.LinuxHeadlessSimulation,
                NamedBuildTarget.Server
            };
            
            foreach (var target in namedTargets)
            {
                try
                {
                    string currentSymbols = PlayerSettings.GetScriptingDefineSymbols(target);
                    string newSymbols = RemoveSymbolFromString(currentSymbols, symbol);
                    
                    if (currentSymbols != newSymbols)
                    {
                        PlayerSettings.SetScriptingDefineSymbols(target, newSymbols);
                    }
                }
                catch
                {
                    // Some build targets might not be available
                    continue;
                }
            }
            
            // Force recompilation
            AssetDatabase.Refresh();
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            
            Debug.Log($"[PerSpec] Removed '{symbol}' via PlayerSettings");
        }
        
        private static bool HasSymbolInPlayerSettings(string symbol)
        {
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(buildTarget));
            string symbols = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
            return HasSymbolInString(symbols, symbol);
        }
        
        #endregion
        
        #region String Helpers
        
        private static bool HasSymbolInString(string symbols, string symbol)
        {
            if (string.IsNullOrEmpty(symbols))
                return false;
            
            var symbolList = symbols.Split(';');
            return symbolList.Contains(symbol);
        }
        
        private static string AddSymbolToString(string symbols, string symbol)
        {
            if (string.IsNullOrEmpty(symbols))
                return symbol;
            
            if (HasSymbolInString(symbols, symbol))
                return symbols;
            
            return symbols + ";" + symbol;
        }
        
        private static string RemoveSymbolFromString(string symbols, string symbol)
        {
            if (string.IsNullOrEmpty(symbols))
                return string.Empty;
            
            var symbolList = symbols.Split(';').ToList();
            symbolList.Remove(symbol);
            return string.Join(";", symbolList);
        }
        
        #endregion
    }
}