using System;
using System.IO;
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
    /// Service for managing DOTS/Entities support via PlayerSettings scripting define symbols.
    /// Uses NamedBuildTarget API for compatibility with all Unity versions 2021.2+.
    /// Properly handles BuildProfiles in Unity 6+.
    /// </summary>
    public static class DOTSService
    {
        #region Constants

        private const string DOTS_DEFINE = "PERSPEC_DOTS_ENABLED";
        private const string CSC_RSP_PATH = "Assets/csc.rsp";
        private const string OLD_DOTS_DEFINE = "-define:PERSPEC_DOTS_ENABLED";

        // All supported NamedBuildTargets
        private static readonly NamedBuildTarget[] AllNamedBuildTargets = new[]
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

        #endregion

        #region Properties

        /// <summary>
        /// Checks if DOTS support is enabled by looking at the current platform's define symbols
        /// </summary>
        public static bool IsDOTSEnabled
        {
            get
            {
                // Check active BuildProfile first (Unity 6+)
#if UNITY_6000_0_OR_NEWER
                if (TryGetBuildProfileSymbols(out string[] profileSymbols))
                {
                    return profileSymbols?.Contains(DOTS_DEFINE) ?? false;
                }
#endif

                // Fall back to PlayerSettings for current platform
                return HasSymbolInPlayerSettings(DOTS_DEFINE);
            }
        }

        /// <summary>
        /// Gets a status string for DOTS support
        /// </summary>
        public static string DOTSStatus => IsDOTSEnabled
            ? "Enabled - DOTS code included (PlayerSettings)"
            : "Disabled - DOTS code stripped";

        #endregion

        #region Public Methods

        /// <summary>
        /// Enable DOTS support by adding directive to all build targets and active BuildProfile
        /// </summary>
        public static void EnableDOTS()
        {
            try
            {
                // First, migrate from csc.rsp if needed
                MigrateFromCscRsp();

                bool addedToProfile = false;
                bool addedToPlayerSettings = false;

                // 1. Add to active BuildProfile (Unity 6+) if present
#if UNITY_6000_0_OR_NEWER
                addedToProfile = AddSymbolToBuildProfile(DOTS_DEFINE);
#endif

                // 2. Add to PlayerSettings for all platforms (ensures packages see it)
                addedToPlayerSettings = AddSymbolToAllPlatforms(DOTS_DEFINE);

                // Log appropriate message
                if (addedToProfile && addedToPlayerSettings)
                {
                    Debug.Log($"[PerSpec] DOTS support ENABLED - Added {DOTS_DEFINE} to BuildProfile and PlayerSettings. Recompiling scripts...");
                }
                else if (addedToProfile)
                {
                    Debug.Log($"[PerSpec] DOTS support ENABLED - Added {DOTS_DEFINE} to active BuildProfile. Recompiling scripts...");
                }
                else if (addedToPlayerSettings)
                {
                    Debug.Log($"[PerSpec] DOTS support ENABLED - Added {DOTS_DEFINE} to PlayerSettings. Recompiling scripts...");
                }
                else
                {
                    Debug.Log("[PerSpec] DOTS support is already enabled.");
                    return;
                }

                // Force recompilation
                RequestRecompilation();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PerSpec] Failed to enable DOTS support: {ex.Message}");
            }
        }

        /// <summary>
        /// Disable DOTS support by removing directive from all build targets and active BuildProfile
        /// </summary>
        public static void DisableDOTS()
        {
            try
            {
                bool removedFromProfile = false;
                bool removedFromPlayerSettings = false;

                // 1. Remove from active BuildProfile (Unity 6+) if present
#if UNITY_6000_0_OR_NEWER
                removedFromProfile = RemoveSymbolFromBuildProfile(DOTS_DEFINE);
#endif

                // 2. Remove from PlayerSettings for all platforms
                removedFromPlayerSettings = RemoveSymbolFromAllPlatforms(DOTS_DEFINE);

                // 3. Also clean up any remaining csc.rsp entries
                CleanupCscRsp();

                // Log appropriate message
                if (removedFromProfile && removedFromPlayerSettings)
                {
                    Debug.Log($"[PerSpec] DOTS support DISABLED - Removed {DOTS_DEFINE} from BuildProfile and PlayerSettings. Recompiling scripts...");
                }
                else if (removedFromProfile)
                {
                    Debug.Log($"[PerSpec] DOTS support DISABLED - Removed {DOTS_DEFINE} from active BuildProfile. Recompiling scripts...");
                }
                else if (removedFromPlayerSettings)
                {
                    Debug.Log($"[PerSpec] DOTS support DISABLED - Removed {DOTS_DEFINE} from PlayerSettings. Recompiling scripts...");
                }
                else
                {
                    Debug.Log("[PerSpec] DOTS support is already disabled.");
                    return;
                }

                // Force recompilation
                RequestRecompilation();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PerSpec] Failed to disable DOTS support: {ex.Message}");
            }
        }

        /// <summary>
        /// Toggle DOTS support
        /// </summary>
        public static void ToggleDOTS()
        {
            if (IsDOTSEnabled)
                DisableDOTS();
            else
                EnableDOTS();
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

        #region Migration Methods

        /// <summary>
        /// Migrate from old csc.rsp approach to PlayerSettings
        /// </summary>
        private static void MigrateFromCscRsp()
        {
            if (!File.Exists(CSC_RSP_PATH))
                return;

            try
            {
                string content = File.ReadAllText(CSC_RSP_PATH);
                if (!content.Contains(OLD_DOTS_DEFINE))
                    return;

                Debug.Log("[PerSpec] Migrating DOTS directive from csc.rsp to PlayerSettings...");

                // Remove from csc.rsp
                var lines = File.ReadAllLines(CSC_RSP_PATH)
                    .Where(l => l.Trim() != OLD_DOTS_DEFINE)
                    .ToArray();

                if (lines.Length == 0 || lines.All(string.IsNullOrWhiteSpace))
                {
                    File.Delete(CSC_RSP_PATH);
                    var meta = CSC_RSP_PATH + ".meta";
                    if (File.Exists(meta)) File.Delete(meta);
                }
                else
                {
                    File.WriteAllLines(CSC_RSP_PATH, lines);
                }

                Debug.Log("[PerSpec] Migration complete - DOTS directive moved to PlayerSettings");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PerSpec] Error during csc.rsp migration: {ex.Message}");
            }
        }

        /// <summary>
        /// Clean up any remaining DOTS directive in csc.rsp
        /// </summary>
        private static void CleanupCscRsp()
        {
            if (!File.Exists(CSC_RSP_PATH))
                return;

            try
            {
                var lines = File.ReadAllLines(CSC_RSP_PATH);
                var newLines = lines.Where(l => l.Trim() != OLD_DOTS_DEFINE).ToArray();

                if (lines.Length == newLines.Length)
                    return; // Nothing to clean up

                if (newLines.Length == 0 || newLines.All(string.IsNullOrWhiteSpace))
                {
                    File.Delete(CSC_RSP_PATH);
                    var meta = CSC_RSP_PATH + ".meta";
                    if (File.Exists(meta)) File.Delete(meta);
                }
                else
                {
                    File.WriteAllLines(CSC_RSP_PATH, newLines);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PerSpec] Error cleaning up csc.rsp: {ex.Message}");
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
