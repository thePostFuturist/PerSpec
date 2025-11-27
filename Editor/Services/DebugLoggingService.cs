using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
#if UNITY_6000_0_OR_NEWER
using UnityEditor.Build.Profile;
#endif

namespace PerSpec.Editor.Services
{
    /// <summary>
    /// Service for managing debug logging via PlayerSettings scripting define symbols.
    /// Uses NamedBuildTarget API for compatibility with all Unity versions 2021.2+.
    /// Properly handles BuildProfiles in Unity 6+.
    /// </summary>
    public static class DebugLoggingService
    {
        #region Constants

        private const string DEBUG_DEFINE = "PERSPEC_DEBUG";

        // All supported NamedBuildTargets
        private static readonly NamedBuildTarget[] AllNamedBuildTargets = new[]
        {
            NamedBuildTarget.Standalone,
            NamedBuildTarget.iOS,
            NamedBuildTarget.Android,
            NamedBuildTarget.WebGL,
            NamedBuildTarget.Server
        };

        #endregion

        #region Properties

        /// <summary>
        /// Checks if debug logging is enabled by looking at the current platform's define symbols
        /// </summary>
        public static bool IsDebugEnabled
        {
            get
            {
                // Check active BuildProfile first (Unity 6+)
#if UNITY_6000_0_OR_NEWER
                if (TryGetBuildProfileSymbols(out string[] profileSymbols))
                {
                    return profileSymbols?.Contains(DEBUG_DEFINE) ?? false;
                }
#endif

                // Fall back to PlayerSettings for current platform
                return HasSymbolInPlayerSettings(DEBUG_DEFINE);
            }
        }

        /// <summary>
        /// Gets a status string for debug logging
        /// </summary>
        public static string DebugStatus => IsDebugEnabled
            ? "Enabled - Debug logs included (PlayerSettings)"
            : "Disabled - Debug logs stripped";

        #endregion

        #region Public Methods

        /// <summary>
        /// Enable debug logging by adding directive to all build targets and active BuildProfile
        /// </summary>
        public static void EnableDebugLogging()
        {
            try
            {
                bool addedToProfile = false;
                bool addedToPlayerSettings = false;

                // 1. Add to active BuildProfile (Unity 6+) if present
#if UNITY_6000_0_OR_NEWER
                addedToProfile = AddSymbolToBuildProfile(DEBUG_DEFINE);
#endif

                // 2. Add to PlayerSettings for all platforms (ensures packages see it)
                addedToPlayerSettings = AddSymbolToAllPlatforms(DEBUG_DEFINE);

                // Log appropriate message
                if (addedToProfile && addedToPlayerSettings)
                {
                    Debug.Log($"[PerSpec] Debug logging ENABLED - Added {DEBUG_DEFINE} to BuildProfile and PlayerSettings. Recompiling scripts...");
                }
                else if (addedToProfile)
                {
                    Debug.Log($"[PerSpec] Debug logging ENABLED - Added {DEBUG_DEFINE} to active BuildProfile. Recompiling scripts...");
                }
                else if (addedToPlayerSettings)
                {
                    Debug.Log($"[PerSpec] Debug logging ENABLED - Added {DEBUG_DEFINE} to PlayerSettings. Recompiling scripts...");
                }
                else
                {
                    Debug.Log("[PerSpec] Debug logging is already enabled.");
                    return;
                }

                // Force recompilation
                RequestRecompilation();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PerSpec] Failed to enable debug logging: {ex.Message}");
            }
        }

        /// <summary>
        /// Disable debug logging by removing directive from all build targets and active BuildProfile
        /// </summary>
        public static void DisableDebugLogging()
        {
            try
            {
                bool removedFromProfile = false;
                bool removedFromPlayerSettings = false;

                // 1. Remove from active BuildProfile (Unity 6+) if present
#if UNITY_6000_0_OR_NEWER
                removedFromProfile = RemoveSymbolFromBuildProfile(DEBUG_DEFINE);
#endif

                // 2. Remove from PlayerSettings for all platforms
                removedFromPlayerSettings = RemoveSymbolFromAllPlatforms(DEBUG_DEFINE);

                // Log appropriate message
                if (removedFromProfile && removedFromPlayerSettings)
                {
                    Debug.Log($"[PerSpec] Debug logging DISABLED - Removed {DEBUG_DEFINE} from BuildProfile and PlayerSettings. Recompiling scripts...");
                }
                else if (removedFromProfile)
                {
                    Debug.Log($"[PerSpec] Debug logging DISABLED - Removed {DEBUG_DEFINE} from active BuildProfile. Recompiling scripts...");
                }
                else if (removedFromPlayerSettings)
                {
                    Debug.Log($"[PerSpec] Debug logging DISABLED - Removed {DEBUG_DEFINE} from PlayerSettings. Recompiling scripts...");
                }
                else
                {
                    Debug.Log("[PerSpec] Debug logging is already disabled.");
                    return;
                }

                // Force recompilation
                RequestRecompilation();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PerSpec] Failed to disable debug logging: {ex.Message}");
            }
        }

        /// <summary>
        /// Toggle debug logging
        /// </summary>
        public static void ToggleDebugLogging()
        {
            if (IsDebugEnabled)
                DisableDebugLogging();
            else
                EnableDebugLogging();
        }

        #endregion

        #region BuildProfile Methods (Unity 6+)

#if UNITY_6000_0_OR_NEWER
        private static bool TryGetBuildProfileSymbols(out string[] symbols)
        {
            symbols = null;
            try
            {
                var activeProfile = BuildProfile.GetActiveBuildProfile();
                if (activeProfile != null)
                {
                    symbols = activeProfile.scriptingDefines;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PerSpec] Error getting BuildProfile symbols: {ex.Message}");
            }
            return false;
        }

        private static bool AddSymbolToBuildProfile(string symbol)
        {
            try
            {
                var activeProfile = BuildProfile.GetActiveBuildProfile();
                if (activeProfile == null)
                    return false;

                var currentDefines = activeProfile.scriptingDefines ?? new string[0];

                // Check if already exists
                if (currentDefines.Contains(symbol))
                    return false;

                // Add the symbol
                var newDefines = currentDefines.Concat(new[] { symbol }).ToArray();
                activeProfile.scriptingDefines = newDefines;

                // Mark as dirty and save
                EditorUtility.SetDirty(activeProfile);
                AssetDatabase.SaveAssets();

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
                var activeProfile = BuildProfile.GetActiveBuildProfile();
                if (activeProfile == null)
                    return false;

                var currentDefines = activeProfile.scriptingDefines;
                if (currentDefines == null || !currentDefines.Contains(symbol))
                    return false;

                // Remove the symbol
                var newDefines = currentDefines.Where(d => d != symbol).ToArray();
                activeProfile.scriptingDefines = newDefines;

                // Mark as dirty and save
                EditorUtility.SetDirty(activeProfile);
                AssetDatabase.SaveAssets();

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PerSpec] Error removing symbol from BuildProfile: {ex.Message}");
                return false;
            }
        }
#endif

        #endregion

        #region PlayerSettings Methods

        private static bool AddSymbolToAllPlatforms(string symbol)
        {
            bool anyChanged = false;

            foreach (var target in AllNamedBuildTargets)
            {
                try
                {
                    string currentSymbols = PlayerSettings.GetScriptingDefineSymbols(target);

                    // Check if already present
                    var symbolList = string.IsNullOrEmpty(currentSymbols)
                        ? new string[0]
                        : currentSymbols.Split(';');

                    if (symbolList.Contains(symbol))
                        continue;

                    // Add symbol
                    string newSymbols = string.IsNullOrEmpty(currentSymbols)
                        ? symbol
                        : currentSymbols + ";" + symbol;

                    PlayerSettings.SetScriptingDefineSymbols(target, newSymbols);
                    anyChanged = true;
                }
                catch
                {
                    // Platform might not be installed, skip silently
                    continue;
                }
            }

            return anyChanged;
        }

        private static bool RemoveSymbolFromAllPlatforms(string symbol)
        {
            bool anyChanged = false;

            foreach (var target in AllNamedBuildTargets)
            {
                try
                {
                    string currentSymbols = PlayerSettings.GetScriptingDefineSymbols(target);

                    if (string.IsNullOrEmpty(currentSymbols))
                        continue;

                    var symbolList = currentSymbols.Split(';').ToList();

                    if (!symbolList.Contains(symbol))
                        continue;

                    // Remove symbol
                    symbolList.Remove(symbol);
                    string newSymbols = string.Join(";", symbolList);

                    PlayerSettings.SetScriptingDefineSymbols(target, newSymbols);
                    anyChanged = true;
                }
                catch
                {
                    // Platform might not be installed, skip silently
                    continue;
                }
            }

            return anyChanged;
        }

        private static bool HasSymbolInPlayerSettings(string symbol)
        {
            try
            {
                var buildTarget = EditorUserBuildSettings.activeBuildTarget;
                var namedTarget = NamedBuildTarget.FromBuildTargetGroup(
                    BuildPipeline.GetBuildTargetGroup(buildTarget));

                string symbols = PlayerSettings.GetScriptingDefineSymbols(namedTarget);

                if (string.IsNullOrEmpty(symbols))
                    return false;

                return symbols.Split(';').Contains(symbol);
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Utility Methods

        private static void RequestRecompilation()
        {
            AssetDatabase.Refresh();
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        }

        #endregion
    }
}
