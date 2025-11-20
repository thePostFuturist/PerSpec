using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace PerSpec.Editor.Services
{
    /// <summary>
    /// Service for managing DOTS/Entities support via csc.rsp compiler directives.
    /// Uses Assets/csc.rsp file for global compiler directives.
    /// </summary>
    public static class DOTSService
    {
        #region Constants

        private const string CSC_RSP_PATH = "Assets/csc.rsp";
        private const string DOTS_DEFINE = "-define:PERSPEC_DOTS_ENABLED";

        #endregion

        #region Properties

        /// <summary>
        /// Checks if DOTS support is enabled by looking for the directive in csc.rsp
        /// </summary>
        public static bool IsDOTSEnabled
        {
            get
            {
                if (!File.Exists(CSC_RSP_PATH))
                    return false;

                try
                {
                    string content = File.ReadAllText(CSC_RSP_PATH);
                    return content.Contains(DOTS_DEFINE);
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets a status string for DOTS support
        /// </summary>
        public static string DOTSStatus => IsDOTSEnabled
            ? "Enabled - DOTS code included (csc.rsp)"
            : "Disabled - DOTS code stripped";

        /// <summary>
        /// Checks if the csc.rsp file exists
        /// </summary>
        public static bool IsCscRspPresent => File.Exists(CSC_RSP_PATH);

        #endregion

        #region Public Methods

        /// <summary>
        /// Enable DOTS support by adding directive to csc.rsp
        /// </summary>
        public static void EnableDOTS()
        {
            try
            {
                string[] lines = File.Exists(CSC_RSP_PATH)
                    ? File.ReadAllLines(CSC_RSP_PATH)
                    : new string[0];

                // Check if already present
                if (lines.Any(line => line.Trim() == DOTS_DEFINE))
                {
                    Debug.Log("[PerSpec] DOTS support is already enabled.");
                    return;
                }

                // Add the directive
                var newLines = lines.Concat(new[] { DOTS_DEFINE }).ToArray();
                File.WriteAllLines(CSC_RSP_PATH, newLines);

                Debug.Log($"[PerSpec] DOTS support ENABLED - Added {DOTS_DEFINE} to {CSC_RSP_PATH}. Recompiling scripts...");

                // Force recompilation
                AssetDatabase.Refresh();
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PerSpec] Failed to enable DOTS support: {ex.Message}");
            }
        }

        /// <summary>
        /// Disable DOTS support by removing directive from csc.rsp
        /// </summary>
        public static void DisableDOTS()
        {
            try
            {
                if (!File.Exists(CSC_RSP_PATH))
                {
                    Debug.Log("[PerSpec] DOTS support is already disabled (csc.rsp does not exist).");
                    return;
                }

                string[] lines = File.ReadAllLines(CSC_RSP_PATH);
                var newLines = lines.Where(line => line.Trim() != DOTS_DEFINE).ToArray();

                // Check if anything changed
                if (lines.Length == newLines.Length)
                {
                    Debug.Log("[PerSpec] DOTS support is already disabled.");
                    return;
                }

                // Write back or delete if empty
                if (newLines.Length == 0 || newLines.All(line => string.IsNullOrWhiteSpace(line)))
                {
                    File.Delete(CSC_RSP_PATH);

                    // Also delete .meta file if it exists
                    string metaPath = CSC_RSP_PATH + ".meta";
                    if (File.Exists(metaPath))
                        File.Delete(metaPath);

                    Debug.Log($"[PerSpec] DOTS support DISABLED - Deleted {CSC_RSP_PATH} (no other directives). Recompiling scripts...");
                }
                else
                {
                    File.WriteAllLines(CSC_RSP_PATH, newLines);
                    Debug.Log($"[PerSpec] DOTS support DISABLED - Removed {DOTS_DEFINE} from {CSC_RSP_PATH}. Recompiling scripts...");
                }

                // Force recompilation
                AssetDatabase.Refresh();
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
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
    }
}
