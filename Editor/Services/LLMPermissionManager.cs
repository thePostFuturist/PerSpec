using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PerSpec.Editor.Services
{
    /// <summary>
    /// Manages LLM provider permissions for executing bash and python commands
    /// </summary>
    public static class LLMPermissionManager
    {
        #region Constants
        
        private const string PREF_KEY_PREFIX = "PerSpec_LLM_Permission_";
        private const string BASH_SUFFIX = "_Bash";
        private const string PYTHON_SUFFIX = "_Python";
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Permission state for an LLM provider
        /// </summary>
        public class PermissionState
        {
            public string Provider { get; set; }
            public bool BashEnabled { get; set; }
            public bool PythonEnabled { get; set; }
            public DateTime LastModified { get; set; }
            
            public bool HasAnyPermission => BashEnabled || PythonEnabled;
        }
        
        /// <summary>
        /// Get permission state for a specific provider
        /// </summary>
        public static PermissionState GetPermissions(string provider)
        {
            if (string.IsNullOrEmpty(provider))
                return new PermissionState { Provider = provider };
                
            string cleanProvider = CleanProviderName(provider);
            
            return new PermissionState
            {
                Provider = provider,
                BashEnabled = EditorPrefs.GetBool(GetBashKey(cleanProvider), false),
                PythonEnabled = EditorPrefs.GetBool(GetPythonKey(cleanProvider), false),
                LastModified = GetLastModifiedTime(cleanProvider)
            };
        }
        
        /// <summary>
        /// Set bash permission for a provider
        /// </summary>
        public static void SetBashPermission(string provider, bool enabled)
        {
            if (string.IsNullOrEmpty(provider))
                return;
                
            string cleanProvider = CleanProviderName(provider);
            EditorPrefs.SetBool(GetBashKey(cleanProvider), enabled);
            UpdateLastModifiedTime(cleanProvider);
            
            LogPermissionChange(provider, "Bash", enabled);
        }
        
        /// <summary>
        /// Set python permission for a provider
        /// </summary>
        public static void SetPythonPermission(string provider, bool enabled)
        {
            if (string.IsNullOrEmpty(provider))
                return;
                
            string cleanProvider = CleanProviderName(provider);
            EditorPrefs.SetBool(GetPythonKey(cleanProvider), enabled);
            UpdateLastModifiedTime(cleanProvider);
            
            LogPermissionChange(provider, "Python", enabled);
        }
        
        /// <summary>
        /// Set both permissions at once
        /// </summary>
        public static void SetPermissions(string provider, bool bashEnabled, bool pythonEnabled)
        {
            if (string.IsNullOrEmpty(provider))
                return;
                
            string cleanProvider = CleanProviderName(provider);
            EditorPrefs.SetBool(GetBashKey(cleanProvider), bashEnabled);
            EditorPrefs.SetBool(GetPythonKey(cleanProvider), pythonEnabled);
            UpdateLastModifiedTime(cleanProvider);
            
            if (bashEnabled || pythonEnabled)
            {
                Debug.Log($"[PerSpec] LLM Permissions updated for {provider}: Bash={bashEnabled}, Python={pythonEnabled}");
            }
        }
        
        /// <summary>
        /// Clear all permissions for a provider
        /// </summary>
        public static void ClearPermissions(string provider)
        {
            if (string.IsNullOrEmpty(provider))
                return;
                
            string cleanProvider = CleanProviderName(provider);
            EditorPrefs.DeleteKey(GetBashKey(cleanProvider));
            EditorPrefs.DeleteKey(GetPythonKey(cleanProvider));
            EditorPrefs.DeleteKey(GetTimeKey(cleanProvider));
            
            Debug.Log($"[PerSpec] Cleared all permissions for {provider}");
        }
        
        /// <summary>
        /// Get all providers that have any permissions set
        /// </summary>
        public static List<string> GetProvidersWithPermissions()
        {
            var providers = new List<string>();
            
            // Check known providers
            string[] knownProviders = new string[] 
            {
                "Claude", "Cursor", "OpenAI", "Gemini"
            };
            
            foreach (var provider in knownProviders)
            {
                var state = GetPermissions(provider);
                if (state.HasAnyPermission)
                {
                    providers.Add(provider);
                }
            }
            
            return providers;
        }
        
        /// <summary>
        /// Generate permission block for LLM configuration
        /// </summary>
        public static string GeneratePermissionBlock(string provider)
        {
            var state = GetPermissions(provider);
            
            if (!state.HasAnyPermission)
                return string.Empty;
            
            var lines = new List<string>();
            lines.Add("");
            lines.Add("## 🔐 Command Execution Permissions");
            lines.Add("");
            lines.Add("> **WARNING**: This assistant has been granted permission to execute system commands.");
            lines.Add("> These permissions were explicitly enabled by the user via PerSpec Control Center.");
            lines.Add("");
            
            if (state.BashEnabled && state.PythonEnabled)
            {
                lines.Add("### ✅ Enabled Commands");
                lines.Add("You are permitted to execute the following command types:");
                lines.Add("- **Bash commands** - System shell commands");
                lines.Add("- **Python scripts** - Python execution");
                lines.Add("");
                lines.Add("### Allowed PerSpec Commands");
                lines.Add("```bash");
                lines.Add("# Unity coordination commands");
                lines.Add("python PerSpec/Coordination/Scripts/quick_refresh.py [options]");
                lines.Add("python PerSpec/Coordination/Scripts/quick_test.py [options]");
                lines.Add("python PerSpec/Coordination/Scripts/quick_logs.py [options]");
                lines.Add("");
                lines.Add("# File system navigation");
                lines.Add("ls, cd, pwd, find");
                lines.Add("");
                lines.Add("# Git operations");
                lines.Add("git status, git diff, git log");
                lines.Add("```");
            }
            else if (state.BashEnabled)
            {
                lines.Add("### ✅ Bash Commands Enabled");
                lines.Add("You are permitted to execute bash/shell commands.");
                lines.Add("");
                lines.Add("### Allowed Commands");
                lines.Add("```bash");
                lines.Add("ls, cd, pwd, find");
                lines.Add("git status, git diff, git log");
                lines.Add("```");
            }
            else if (state.PythonEnabled)
            {
                lines.Add("### ✅ Python Execution Enabled");
                lines.Add("You are permitted to execute Python scripts and commands.");
                lines.Add("");
                lines.Add("### Allowed PerSpec Scripts");
                lines.Add("```bash");
                lines.Add("python PerSpec/Coordination/Scripts/quick_refresh.py [options]");
                lines.Add("python PerSpec/Coordination/Scripts/quick_test.py [options]");
                lines.Add("python PerSpec/Coordination/Scripts/quick_logs.py [options]");
                lines.Add("```");
            }
            
            lines.Add("");
            lines.Add("### ⚠️ Security Notice");
            lines.Add("- Only execute commands that are necessary for the task");
            lines.Add("- Always explain what commands will do before running them");
            lines.Add("- Never execute destructive commands without explicit confirmation");
            lines.Add("- Do not access sensitive files or credentials");
            lines.Add("");
            
            return string.Join("\n", lines);
        }
        
        /// <summary>
        /// Check if permission block exists in content
        /// </summary>
        public static bool HasPermissionBlock(string content)
        {
            return content.Contains("## 🔐 Command Execution Permissions") ||
                   content.Contains("Command Execution Permissions");
        }
        
        /// <summary>
        /// Update permission block in existing content
        /// </summary>
        public static string UpdatePermissionBlock(string content, string provider)
        {
            string newBlock = GeneratePermissionBlock(provider);
            
            // If no permissions, remove the block if it exists
            if (string.IsNullOrEmpty(newBlock))
            {
                return RemovePermissionBlock(content);
            }
            
            // If block exists, replace it
            if (HasPermissionBlock(content))
            {
                // Find and replace the permission block
                int startIndex = content.IndexOf("## 🔐 Command Execution Permissions");
                if (startIndex == -1)
                    startIndex = content.IndexOf("## Command Execution Permissions");
                    
                if (startIndex != -1)
                {
                    // Find the end of the permission block (next ## or end of content)
                    int endIndex = content.IndexOf("\n## ", startIndex + 1);
                    if (endIndex == -1)
                        endIndex = content.Length;
                    
                    string before = content.Substring(0, startIndex);
                    string after = content.Substring(endIndex);
                    
                    return before + newBlock + after;
                }
            }
            
            // If no block exists, add it after the Natural Language Commands section
            int insertIndex = content.IndexOf("## 🚀 TDD Development Workflow");
            if (insertIndex == -1)
                insertIndex = content.IndexOf("## TDD Development Workflow");
                
            if (insertIndex != -1)
            {
                string before = content.Substring(0, insertIndex);
                string after = content.Substring(insertIndex);
                return before + newBlock + "\n" + after;
            }
            
            // Fallback: append at the end
            return content + "\n" + newBlock;
        }
        
        #endregion
        
        #region Private Methods
        
        private static string CleanProviderName(string provider)
        {
            // Remove special characters and normalize
            return provider.Replace(" ", "")
                          .Replace("(", "")
                          .Replace(")", "")
                          .Replace(".", "")
                          .Replace("/", "_");
        }
        
        private static string GetBashKey(string provider)
        {
            return PREF_KEY_PREFIX + provider + BASH_SUFFIX;
        }
        
        private static string GetPythonKey(string provider)
        {
            return PREF_KEY_PREFIX + provider + PYTHON_SUFFIX;
        }
        
        private static string GetTimeKey(string provider)
        {
            return PREF_KEY_PREFIX + provider + "_LastModified";
        }
        
        private static void UpdateLastModifiedTime(string provider)
        {
            EditorPrefs.SetString(GetTimeKey(provider), DateTime.Now.ToString("O"));
        }
        
        private static DateTime GetLastModifiedTime(string provider)
        {
            string timeStr = EditorPrefs.GetString(GetTimeKey(provider), "");
            if (DateTime.TryParse(timeStr, out DateTime time))
                return time;
            return DateTime.MinValue;
        }
        
        private static void LogPermissionChange(string provider, string permissionType, bool enabled)
        {
            string action = enabled ? "ENABLED" : "DISABLED";
            Debug.Log($"[PerSpec] {permissionType} commands {action} for {provider}");
            
            if (enabled)
            {
                Debug.LogWarning($"[PerSpec] ⚠️ {provider} can now execute {permissionType.ToLower()} commands. " +
                               "Ensure you trust the commands being executed.");
            }
        }
        
        private static string RemovePermissionBlock(string content)
        {
            int startIndex = content.IndexOf("## 🔐 Command Execution Permissions");
            if (startIndex == -1)
                startIndex = content.IndexOf("## Command Execution Permissions");
                
            if (startIndex != -1)
            {
                // Find the end of the permission block
                int endIndex = content.IndexOf("\n## ", startIndex + 1);
                if (endIndex == -1)
                    endIndex = content.Length;
                
                string before = content.Substring(0, startIndex);
                string after = content.Substring(endIndex);
                
                // Clean up extra newlines
                while (before.EndsWith("\n\n\n"))
                    before = before.Substring(0, before.Length - 1);
                    
                return before + after;
            }
            
            return content;
        }
        
        #endregion
    }
}