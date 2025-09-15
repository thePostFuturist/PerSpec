using System;
using UnityEngine;
using UnityEditor;

namespace PerSpec.Editor.Services
{
    /// <summary>
    /// Central settings service for PerSpec framework
    /// Controls global enable/disable state and coordinates all services
    /// </summary>
    public static class PerSpecSettings
    {
        #region Constants

        private const string ENABLED_PREF_KEY = "PerSpec_Enabled";
        private const string FIRST_RUN_PREF_KEY = "PerSpec_FirstRun";

        #endregion

        #region Events

        public static event Action<bool> OnEnabledStateChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether PerSpec is enabled globally
        /// Default is TRUE - PerSpec is enabled by default
        /// </summary>
        public static bool IsEnabled
        {
            get
            {
                // First check if this is the first run
                if (!EditorPrefs.HasKey(FIRST_RUN_PREF_KEY))
                {
                    // First run - set defaults
                    EditorPrefs.SetBool(FIRST_RUN_PREF_KEY, true);
                    EditorPrefs.SetBool(ENABLED_PREF_KEY, true); // ENABLED BY DEFAULT
                    return true;
                }

                // Return stored preference, default to true if not set
                return EditorPrefs.GetBool(ENABLED_PREF_KEY, true);
            }
            set
            {
                bool currentValue = IsEnabled;
                if (currentValue != value)
                {
                    EditorPrefs.SetBool(ENABLED_PREF_KEY, value);

                    // Log the state change
                    if (value)
                    {
                        Debug.Log("[PerSpec] PerSpec ENABLED - All services activated");
                    }
                    else
                    {
                        Debug.LogWarning("[PerSpec] PerSpec DISABLED - All services deactivated");
                    }

                    // Notify all subscribers
                    OnEnabledStateChanged?.Invoke(value);

                    // Update dependent services
                    UpdateDependentServices(value);
                }
            }
        }

        /// <summary>
        /// Check if PerSpec should be running (enabled AND initialized)
        /// </summary>
        public static bool ShouldRun
        {
            get
            {
                // Check if initialized by looking for the PerSpec directory
                string projectPath = System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName;
                string perspecPath = System.IO.Path.Combine(projectPath, "PerSpec");
                bool isInitialized = System.IO.Directory.Exists(perspecPath);

                return IsEnabled && isInitialized;
            }
        }

        /// <summary>
        /// Get a human-readable status string
        /// </summary>
        public static string Status
        {
            get
            {
                // Check if initialized by looking for the PerSpec directory
                string projectPath = System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName;
                string perspecPath = System.IO.Path.Combine(projectPath, "PerSpec");
                bool isInitialized = System.IO.Directory.Exists(perspecPath);

                if (!isInitialized)
                    return "Not Initialized";

                return IsEnabled ? "● Enabled" : "● Disabled";
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Enable PerSpec globally
        /// </summary>
        public static void Enable()
        {
            IsEnabled = true;
        }

        /// <summary>
        /// Disable PerSpec globally
        /// </summary>
        public static void Disable()
        {
            IsEnabled = false;
        }

        /// <summary>
        /// Toggle PerSpec enabled state
        /// </summary>
        public static void Toggle()
        {
            IsEnabled = !IsEnabled;
        }

        /// <summary>
        /// Remove PerSpec completely from the project
        /// </summary>
        public static bool RemovePerSpec()
        {
            try
            {
                // First disable to stop all services
                if (IsEnabled)
                {
                    Disable();
                }

                // Remove the PerSpec directory directly
                string projectPath = System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName;
                string perspecPath = System.IO.Path.Combine(projectPath, "PerSpec");

                bool result = false;
                if (System.IO.Directory.Exists(perspecPath))
                {
                    System.IO.Directory.Delete(perspecPath, true);
                    result = true;
                }

                if (result)
                {
                    // Clear all preferences
                    EditorPrefs.DeleteKey(ENABLED_PREF_KEY);
                    EditorPrefs.DeleteKey(FIRST_RUN_PREF_KEY);
                    EditorPrefs.DeleteKey("PerSpec_LastKnownVersion");

                    Debug.Log("[PerSpec] PerSpec has been completely removed from the project");

                    // Show confirmation
                    EditorUtility.DisplayDialog(
                        "PerSpec Removed",
                        "PerSpec has been completely removed from your project.\n\n" +
                        "The PerSpec directory and all associated data have been deleted.\n\n" +
                        "You can re-initialize PerSpec at any time from Tools > PerSpec > Initialize.",
                        "OK"
                    );

                    return true;
                }
                else
                {
                    Debug.LogError("[PerSpec] Failed to remove PerSpec directory");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PerSpec] Error removing PerSpec: {e.Message}");
                return false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Update all dependent services when enabled state changes
        /// </summary>
        private static void UpdateDependentServices(bool enabled)
        {
            // Services will check the ShouldRun property on their own
            // This avoids circular dependencies between assemblies
        }

        #endregion
    }
}