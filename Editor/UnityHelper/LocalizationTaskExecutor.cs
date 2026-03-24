#if HAS_UNITY_LOCALIZATION
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEditor.Localization;
using System.IO;

namespace PerSpec.UnityHelper.Editor
{
    public class LocalizationTaskExecutor : BaseTaskExecutor
    {
        public override ExecutorType Type => ExecutorType.LOCALIZATION;

        public override bool Execute(Task task)
        {
            try
            {
                switch (task.action)
                {
                    case "CreateLocalizationSettings":
                        return CreateLocalizationSettings(task);
                    case "DebugAllLocalizationInfo":
                        return DebugAllLocalizationInfo(task);
                    case "AddLanguage":
                        return AddLanguage(task);
                    case "RemoveLanguage":
                        return RemoveLanguage(task);
                    case "CreateStringTable":
                        return CreateStringTable(task);
                    case "CreateAssetTable":
                        return CreateAssetTable(task);
                    case "SetString":
                        return SetString(task);
                    case "DeleteString":
                        return DeleteString(task);
                    case "GetString":
                        return GetString(task);
                    case "SetAsset":
                        return SetAsset(task);
                    case "BulkSetStrings":
                        return BulkSetStrings(task);
                    case "ValidateKeys":
                        return ValidateKeys(task);
                    case "RenameKey":
                        return RenameKey(task);
                    case "CopyStringAcrossLanguages":
                        return CopyStringAcrossLanguages(task);
                    case "ExportTableToCSV":
                        return ExportTableToCSV(task);
                    case "ImportTableFromCSV":
                        return ImportTableFromCSV(task);
                    case "UpdateAll":
                        return UpdateAllLocalizations(task);
                    default:
                        task.error = $"Unknown localization action: {task.action}";
                        return false;
                }
            }
            catch (Exception ex)
            {
                task.error = ex.Message;
                return false;
            }
        }

        private bool CreateLocalizationSettings(Task task)
        {
            try
            {
                // Check if LocalizationSettings already exists
                var existingSettings = LocalizationEditorSettings.ActiveLocalizationSettings;
                if (existingSettings != null)
                {
                    return true; // Already exists, idempotent
                }

                // Unity doesn't provide a programmatic way to create LocalizationSettings
                // User must create it manually once via Unity Editor UI
                task.error = "CreateLocalizationSettings requires manual setup: Open 'Window > Asset Management > Localization Tables' in Unity Editor. It will prompt you to create LocalizationSettings. Click 'Create' and then run this scenario again.";
                return false;
            }
            catch (Exception ex)
            {
                task.error = $"CreateLocalizationSettings failed: {ex.Message}";
                return false;
            }
        }

        private bool DebugAllLocalizationInfo(Task task)
        {
            try
            {
                // Check if LocalizationSettings already exists
                var existingSettings = LocalizationEditorSettings.ActiveLocalizationSettings;
                
                // Log all available information about LocalizationEditorSettings and tables
                Debug.Log("[LocalizationTaskExecutor] === LocalizationEditorSettings Information ===");
                Debug.Log($"ActiveLocalizationSettings exists: {existingSettings != null}");
                
                if (existingSettings != null)
                {
                    Debug.Log($"Settings name: {existingSettings.name}");
                    Debug.Log($"Settings path: {AssetDatabase.GetAssetPath(existingSettings)}");
                    Debug.Log($"Settings type: {existingSettings.GetType().FullName}");
                    
                    // Log selected locale
                    var selectedLocale = existingSettings.GetSelectedLocale();
                    Debug.Log($"Selected Locale: {(selectedLocale != null ? $"{selectedLocale.name} ({selectedLocale.Identifier.Code})" : "None")}");
                    
                    // Log project locales using LocalizationEditorSettings
                    Debug.Log("=== Project Locales (Editor) ===");
                    var projectLocales = LocalizationEditorSettings.GetLocales();
                    if (projectLocales != null)
                    {
                        Debug.Log($"Project locales count: {projectLocales.Count}");
                        foreach (var locale in projectLocales)
                        {
                            Debug.Log($"  - Locale: {locale.name}");
                            Debug.Log($"    Code: {locale.Identifier.Code}");
                            Debug.Log($"    CultureInfo: {locale.Identifier.CultureInfo?.Name ?? "null"}");
                            Debug.Log($"    Sort Order: {locale.SortOrder}");
                            Debug.Log($"    Path: {AssetDatabase.GetAssetPath(locale)}");
                        }
                    }
                    else
                    {
                        Debug.Log("No project locales found");
                    }
                    
                    // Log available locales (Runtime)
                    Debug.Log("=== Available Locales (Runtime) ===");
                    var availableLocales = existingSettings.GetAvailableLocales();
                    if (availableLocales != null && availableLocales.Locales != null)
                    {
                        Debug.Log($"Available locales count: {availableLocales.Locales.Count}");
                        foreach (var locale in availableLocales.Locales)
                        {
                            Debug.Log($"  - Locale: {locale.name} ({locale.Identifier.Code})");
                        }
                    }
                    else
                    {
                        Debug.Log("No available locales found or locales list is null");
                    }
                    
                    // Log databases
                    Debug.Log("=== Databases ===");
                    var stringDatabase = existingSettings.GetStringDatabase();
                    if (stringDatabase != null)
                    {
                        Debug.Log($"String Database: {stringDatabase.GetType().Name}");
                        Debug.Log($"  TableProvider: {stringDatabase.TableProvider?.GetType().Name ?? "null"}");
                    }
                    else
                    {
                        Debug.Log("String Database: null");
                    }
                    
                    var assetDatabase = existingSettings.GetAssetDatabase();
                    if (assetDatabase != null)
                    {
                        Debug.Log($"Asset Database: {assetDatabase.GetType().Name}");
                        Debug.Log($"  TableProvider: {assetDatabase.TableProvider?.GetType().Name ?? "null"}");
                    }
                    else
                    {
                        Debug.Log("Asset Database: null");
                    }
                    
                    // Log string table collections
                    Debug.Log("=== String Table Collections ===");
                    var stringTableCollections = LocalizationEditorSettings.GetStringTableCollections();
                    if (stringTableCollections != null)
                    {
                        Debug.Log($"String table collections count: {stringTableCollections.Count}");
                        foreach (var collection in stringTableCollections)
                        {
                            Debug.Log($"  - Collection: {collection.TableCollectionName} (SharedTableData: {collection.SharedData?.name ?? "null"})");
                            Debug.Log($"    Path: {AssetDatabase.GetAssetPath(collection)}");
                            Debug.Log($"    Tables in collection: {collection.Tables?.Count ?? 0}");
                            
                            // Log SharedTableData keys
                            if (collection.SharedData != null)
                            {
                                Debug.Log($"    SharedTableData Keys (Total: {collection.SharedData.Entries.Count}):");
                                int keyCount = 0;
                                foreach (var entry in collection.SharedData.Entries)
                                {
                                    Debug.Log($"      [{entry.Id}] {entry.Key}");
                                    keyCount++;
                                    if (keyCount >= 20) // Limit to first 20 keys to avoid spam
                                    {
                                        Debug.Log($"      ... and {collection.SharedData.Entries.Count - 20} more keys");
                                        break;
                                    }
                                }
                            }
                            
                            if (collection.Tables != null && projectLocales != null)
                            {
                                // Iterate through all locales to get their tables
                                foreach (var locale in projectLocales)
                                {
                                    var stringTable = collection.GetTable(locale.Identifier) as UnityEngine.Localization.Tables.StringTable;
                                    if (stringTable != null)
                                    {
                                        Debug.Log($"      - Table: {stringTable.name}, Locale: {stringTable.LocaleIdentifier.Code}, Entries: {stringTable.Count}");
                                        Debug.Log($"        Path: {AssetDatabase.GetAssetPath(stringTable)}");
                                        
                                        // Log sample entries (first 10)
                                        if (stringTable.Count > 0)
                                        {
                                            Debug.Log($"        Sample Entries (showing up to 10):");
                                            int entryCount = 0;
                                            foreach (var entry in stringTable.Values)
                                            {
                                                var value = entry.Value;
                                                var truncated = value != null && value.Length > 50 ? value.Substring(0, 50) + "..." : value;
                                                Debug.Log($"          [{entry.Key}] = \"{truncated}\"");
                                                entryCount++;
                                                if (entryCount >= 10)
                                                {
                                                    if (stringTable.Count > 10)
                                                    {
                                                        Debug.Log($"          ... and {stringTable.Count - 10} more entries");
                                                    }
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("No string table collections found");
                    }
                    
                    // Log asset table collections (if any)
                    Debug.Log("=== Asset Table Collections ===");
                    var assetTableCollections = LocalizationEditorSettings.GetAssetTableCollections();
                    if (assetTableCollections != null)
                    {
                        Debug.Log($"Asset table collections count: {assetTableCollections.Count}");
                        foreach (var collection in assetTableCollections)
                        {
                            Debug.Log($"  - Collection: {collection.TableCollectionName} (SharedTableData: {collection.SharedData?.name ?? "null"})");
                            Debug.Log($"    Path: {AssetDatabase.GetAssetPath(collection)}");
                            Debug.Log($"    Tables in collection: {collection.Tables?.Count ?? 0}");
                            
                            // Log SharedTableData keys
                            if (collection.SharedData != null)
                            {
                                Debug.Log($"    SharedTableData Keys (Total: {collection.SharedData.Entries.Count}):");
                                int keyCount = 0;
                                foreach (var entry in collection.SharedData.Entries)
                                {
                                    Debug.Log($"      [{entry.Id}] {entry.Key}");
                                    keyCount++;
                                    if (keyCount >= 20)
                                    {
                                        Debug.Log($"      ... and {collection.SharedData.Entries.Count - 20} more keys");
                                        break;
                                    }
                                }
                            }
                            
                            if (collection.Tables != null && projectLocales != null)
                            {
                                foreach (var locale in projectLocales)
                                {
                                    var assetTable = collection.GetTable(locale.Identifier) as UnityEngine.Localization.Tables.AssetTable;
                                    if (assetTable != null)
                                    {
                                        Debug.Log($"      - Table: {assetTable.name}, Locale: {assetTable.LocaleIdentifier.Code}, Entries: {assetTable.Count}");
                                        Debug.Log($"        Path: {AssetDatabase.GetAssetPath(assetTable)}");
                                        
                                        // Log sample entries
                                        if (assetTable.Count > 0)
                                        {
                                            Debug.Log($"        Sample Asset Entries (showing up to 10):");
                                            int entryCount = 0;
                                            foreach (var entry in assetTable.Values)
                                            {
                                                var assetGuid = entry.Guid;
                                                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                                                Debug.Log($"          [{entry.Key}] = Asset: {assetPath} (GUID: {assetGuid})");
                                                entryCount++;
                                                if (entryCount >= 10)
                                                {
                                                    if (assetTable.Count > 10)
                                                    {
                                                        Debug.Log($"          ... and {assetTable.Count - 10} more entries");
                                                    }
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("No asset table collections found");
                    }
                }
                else
                {
                    Debug.Log("LocalizationSettings is null - not yet created");
                }
                Debug.Log("[LocalizationTaskExecutor] === End LocalizationEditorSettings Information ===");
                return true; // Debug task always succeeds if it runs
            }
            catch (Exception ex)
            {
                task.error = $"DebugAllLocalizationInfo failed: {ex.Message}";
                return false;
            }
        }

        private bool AddLanguage(Task task)
        {
            string languageCode = GetParam(task, "languageCode");
            string displayName = GetParam(task, "displayName");
            
            if (string.IsNullOrEmpty(languageCode))
            {
                task.error = "AddLanguage failed: 'languageCode' parameter is required. Example: {\"key\":\"languageCode\",\"value\":\"es\"}";
                return false;
            }
            
            if (string.IsNullOrEmpty(displayName))
            {
                task.error = "AddLanguage failed: 'displayName' parameter is required. Example: {\"key\":\"displayName\",\"value\":\"Spanish\"}";
                return false;
            }

            try
            {
                // 1. Get or create LocalizationSettings
                var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
                if (settings == null)
                {
                    task.error = "AddLanguage failed: LocalizationSettings not found. Please create localization settings first via Window > Asset Management > Localization Tables.";
                    return false;
                }

                // 2. Check if locale already exists
                var projectLocales = LocalizationEditorSettings.GetLocales();
                var existingLocale = projectLocales?.FirstOrDefault(l => l.Identifier.Code == languageCode);
                if (existingLocale != null)
                {
                    return true;
                }

                // 3. Create locale directory if it doesn't exist
                string localeDir = "Assets/Localization";
                if (!Directory.Exists(localeDir))
                {
                    Directory.CreateDirectory(localeDir);
                }

                // 4. Create new Locale asset
                string localePath = $"{localeDir}/{displayName} ({languageCode}).asset";
                
                // Check if file already exists
                if (File.Exists(localePath))
                {
                    var existingAsset = AssetDatabase.LoadAssetAtPath<Locale>(localePath);
                    if (existingAsset != null)
                    {
                        LocalizationEditorSettings.AddLocale(existingAsset, false);
                        EditorUtility.SetDirty(settings);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        return true;
                    }
                }

                // Create new locale
                var locale = Locale.CreateLocale(languageCode);
                locale.name = $"{displayName} ({languageCode})";
                
                // 5. Save locale asset FIRST
                AssetDatabase.CreateAsset(locale, localePath);
                AssetDatabase.SaveAssets();
                
                // 6. Add to LocalizationSettings
                LocalizationEditorSettings.AddLocale(locale, false);
                
                // 7. Mark settings dirty and save
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                return true;
            }
            catch (Exception ex)
            {
                task.error = $"AddLanguage failed for '{languageCode}' ({displayName}): {ex.Message}. Check that Unity Localization package is installed and LocalizationSettings exists.";
                return false;
            }
        }

        private bool CreateStringTable(Task task)
        {
            string tableName = GetParam(task, "tableName");
            
            if (string.IsNullOrEmpty(tableName))
            {
                task.error = "CreateStringTable failed: 'tableName' parameter is required. Example: {\"key\":\"tableName\",\"value\":\"General\"}";
                return false;
            }

            try
            {
                // Force refresh to pick up newly added locales
                AssetDatabase.Refresh();
                
                // Check if table already exists
                var existingCollection = LocalizationEditorSettings.GetStringTableCollection(tableName);
                if (existingCollection != null)
                {
                    return true; // Already exists, idempotent
                }

                // Get LocalizationSettings (force reload)
                var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
                if (settings == null)
                {
                    task.error = "CreateStringTable failed: LocalizationSettings not found. Create it via Window > Asset Management > Localization Tables.";
                    return false;
                }

                // Force reload available locales
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                
                // Get all project locales (editor-time)
                var projectLocales = LocalizationEditorSettings.GetLocales();
                if (projectLocales == null || projectLocales.Count == 0)
                {
                    task.error = $"CreateStringTable failed: No locales found in LocalizationSettings. Available locales count: {projectLocales?.Count ?? 0}. Add at least one language first using AddLanguage.";
                    return false;
                }

                // Create string table collection with all project locales
                var collection = LocalizationEditorSettings.CreateStringTableCollection(tableName, "Assets/Localization/Tables", projectLocales.ToList());
                
                if (collection == null)
                {
                    task.error = $"CreateStringTable failed: Unable to create string table collection '{tableName}'.";
                    return false;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                return true;
            }
            catch (Exception ex)
            {
                task.error = $"CreateStringTable failed for table '{tableName}': {ex.Message}";
                return false;
            }
        }

        private bool SetString(Task task)
        {
            string key = GetParam(task, "key");
            string value = GetParam(task, "value");
            string language = GetOptionalParam(task, "language", "en");
            string tableName = GetOptionalParam(task, "table", "General");

            if (string.IsNullOrEmpty(key))
            {
                task.error = "SetString failed: 'key' parameter is required. Example: {\"key\":\"key\",\"value\":\"welcome_message\"}";
                return false;
            }

            if (string.IsNullOrEmpty(value))
            {
                task.error = "SetString failed: 'value' parameter is required. Example: {\"key\":\"value\",\"value\":\"Welcome!\"}";
                return false;
            }

            try
            {
                // Get LocalizationSettings
                var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
                if (settings == null)
                {
                    task.error = "SetString failed: LocalizationSettings not found. Create it via Window > Asset Management > Localization Tables.";
                    return false;
                }

                // Find the locale from project locales
                var projectLocales = LocalizationEditorSettings.GetLocales();
                var locale = projectLocales?.FirstOrDefault(l => l.Identifier.Code == language);
                if (locale == null)
                {
                    task.error = $"SetString failed: Locale '{language}' not found. Add it first using AddLanguage action with languageCode='{language}'.";
                    return false;
                }

                // Find or create string table collection
                var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
                if (collection == null)
                {
                    task.error = $"SetString failed: String table '{tableName}' not found. Create it via Window > Asset Management > Localization Tables.";
                    return false;
                }

                // Get the table for the specific locale, create if missing
                var table = collection.GetTable(locale.Identifier) as UnityEngine.Localization.Tables.StringTable;
                if (table == null)
                {
                    // Table doesn't exist for this locale, add it to the collection
                    collection.AddNewTable(locale.Identifier);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    
                    // Try to get it again
                    table = collection.GetTable(locale.Identifier) as UnityEngine.Localization.Tables.StringTable;
                    if (table == null)
                    {
                        task.error = $"SetString failed: Unable to create string table for locale '{language}' in collection '{tableName}'.";
                        return false;
                    }
                }

                // Add or update the entry
                var entry = table.GetEntry(key);
                if (entry == null)
                {
                    entry = table.AddEntry(key, value);
                }
                else
                {
                    entry.Value = value;
                }

                // Save changes
                EditorUtility.SetDirty(table);
                EditorUtility.SetDirty(collection);
                AssetDatabase.SaveAssets();

                return true;
            }
            catch (Exception ex)
            {
                task.error = $"SetString failed for key '{key}' in table '{tableName}' ({language}): {ex.Message}";
                return false;
            }
        }

        private bool UpdateAllLocalizations(Task task)
        {
            string sourceLanguage = GetOptionalParam(task, "sourceLanguage", "en");
            bool forceUpdate = GetOptionalParam(task, "force", "false").ToLower() == "true";
            string tableName = GetOptionalParam(task, "table", "General");

            try
            {
                // Get LocalizationSettings
                var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
                if (settings == null)
                {
                    task.error = "UpdateAll failed: LocalizationSettings not found. Create it via Window > Asset Management > Localization Tables.";
                    return false;
                }

                // Find source locale from project locales
                var projectLocales = LocalizationEditorSettings.GetLocales();
                var sourceLocale = projectLocales?.FirstOrDefault(l => l.Identifier.Code == sourceLanguage);
                if (sourceLocale == null)
                {
                    task.error = $"UpdateAll failed: Source locale '{sourceLanguage}' not found. Add it first using AddLanguage action.";
                    return false;
                }

                // Get string table collection
                var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
                if (collection == null)
                {
                    task.error = $"UpdateAll failed: String table '{tableName}' not found. Create it via Window > Asset Management > Localization Tables.";
                    return false;
                }

                // Get source table
                var sourceTable = collection.GetTable(sourceLocale.Identifier) as UnityEngine.Localization.Tables.StringTable;
                if (sourceTable == null)
                {
                    task.error = $"UpdateAll failed: Source string table for '{sourceLanguage}' not found in collection '{tableName}'.";
                    return false;
                }

                int updatedCount = 0;
                int addedCount = 0;

                // Iterate through all other locales
                foreach (var targetLocale in projectLocales)
                {
                    // Skip source locale
                    if (targetLocale.Identifier.Code == sourceLanguage)
                        continue;

                    var targetTable = collection.GetTable(targetLocale.Identifier) as UnityEngine.Localization.Tables.StringTable;
                    if (targetTable == null)
                    {
                        continue;
                    }

                    // Copy entries from source to target
                    foreach (var sourceEntry in sourceTable.Values)
                    {
                        var targetEntry = targetTable.GetEntry(sourceEntry.Key);
                        
                        if (targetEntry == null)
                        {
                            // Add new entry
                            targetTable.AddEntry(sourceEntry.Key, sourceEntry.Value);
                            addedCount++;
                        }
                        else if (forceUpdate || string.IsNullOrEmpty(targetEntry.Value))
                        {
                            // Update existing entry if force is enabled or it's empty
                            targetEntry.Value = sourceEntry.Value;
                            updatedCount++;
                        }
                    }

                    EditorUtility.SetDirty(targetTable);
                }

                // Save changes
                EditorUtility.SetDirty(collection);
                AssetDatabase.SaveAssets();

                return true;
            }
            catch (Exception ex)
            {
                task.error = $"UpdateAll failed for table '{tableName}' from source '{sourceLanguage}': {ex.Message}";
                return false;
            }
        }

        private bool BulkSetStrings(Task task)
        {
            string filePath = GetParam(task, "filePath");
            string language = GetOptionalParam(task, "language", "en");
            string tableName = GetOptionalParam(task, "table", "General");

            if (string.IsNullOrEmpty(filePath))
            {
                task.error = "BulkSetStrings failed: 'filePath' parameter is required.";
                return false;
            }

            try
            {
                if (!File.Exists(filePath))
                {
                    task.error = $"BulkSetStrings failed: File not found at '{filePath}'.";
                    return false;
                }

                var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
                if (settings == null)
                {
                    task.error = "BulkSetStrings failed: LocalizationSettings not found.";
                    return false;
                }

                var projectLocales = LocalizationEditorSettings.GetLocales();
                var locale = projectLocales?.FirstOrDefault(l => l.Identifier.Code == language);
                if (locale == null)
                {
                    task.error = $"BulkSetStrings failed: Locale '{language}' not found.";
                    return false;
                }

                var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
                if (collection == null)
                {
                    task.error = $"BulkSetStrings failed: String table '{tableName}' not found.";
                    return false;
                }

                var table = collection.GetTable(locale.Identifier) as UnityEngine.Localization.Tables.StringTable;
                if (table == null)
                {
                    task.error = $"BulkSetStrings failed: String table for locale '{language}' not found.";
                    return false;
                }

                // Read and parse file
                int addedCount = 0;
                int updatedCount = 0;
                string[] lines = File.ReadAllLines(filePath);

                foreach (string line in lines)
                {
                    // Skip empty lines and comments
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#") || line.TrimStart().StartsWith("//"))
                        continue;

                    // Expected format: key=value or key:value
                    string[] parts = line.Split(new[] { '=', ':' }, 2);
                    if (parts.Length != 2)
                        continue;

                    string key = parts[0].Trim();
                    string value = parts[1].Trim();

                    if (string.IsNullOrEmpty(key))
                        continue;

                    // Add or update entry
                    var entry = table.GetEntry(key);
                    if (entry == null)
                    {
                        table.AddEntry(key, value);
                        addedCount++;
                    }
                    else
                    {
                        entry.Value = value;
                        updatedCount++;
                    }
                }

                EditorUtility.SetDirty(table);
                EditorUtility.SetDirty(collection);
                AssetDatabase.SaveAssets();

                Debug.Log($"[LocalizationTaskExecutor] BulkSetStrings: Added {addedCount} new, updated {updatedCount} from '{filePath}'");
                return true;
            }
            catch (Exception ex)
            {
                task.error = $"BulkSetStrings failed: {ex.Message}";
                return false;
            }
        }

        private bool RemoveLanguage(Task task)
        {
            string languageCode = GetParam(task, "languageCode");
            
            if (string.IsNullOrEmpty(languageCode))
            {
                task.error = "RemoveLanguage failed: 'languageCode' parameter is required.";
                return false;
            }

            try
            {
                var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
                if (settings == null)
                {
                    task.error = "RemoveLanguage failed: LocalizationSettings not found.";
                    return false;
                }

                var projectLocales = LocalizationEditorSettings.GetLocales();
                var locale = projectLocales?.FirstOrDefault(l => l.Identifier.Code == languageCode);
                if (locale == null)
                {
                    return true; // Already removed, idempotent
                }

                LocalizationEditorSettings.RemoveLocale(locale, false);
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                return true;
            }
            catch (Exception ex)
            {
                task.error = $"RemoveLanguage failed for '{languageCode}': {ex.Message}";
                return false;
            }
        }

        private bool DeleteString(Task task)
        {
            string key = GetParam(task, "key");
            string tableName = GetOptionalParam(task, "table", "General");

            if (string.IsNullOrEmpty(key))
            {
                task.error = "DeleteString failed: 'key' parameter is required.";
                return false;
            }

            try
            {
                var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
                if (settings == null)
                {
                    task.error = "DeleteString failed: LocalizationSettings not found.";
                    return false;
                }

                var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
                if (collection == null)
                {
                    task.error = $"DeleteString failed: String table '{tableName}' not found.";
                    return false;
                }

                // Get the key ID first
                long keyId = collection.SharedData.GetId(key);
                if (keyId == 0)
                {
                    return true; // Key doesn't exist, idempotent
                }

                // Remove from all locale tables first
                var projectLocales = LocalizationEditorSettings.GetLocales();
                if (projectLocales != null && projectLocales.Count > 0)
                {
                    foreach (var locale in projectLocales)
                    {
                        var table = collection.GetTable(locale.Identifier) as UnityEngine.Localization.Tables.StringTable;
                        if (table != null)
                        {
                            table.Remove(keyId);
                            EditorUtility.SetDirty(table);
                        }
                    }
                }

                // Then remove from SharedTableData
                collection.SharedData.RemoveKey(key);
                EditorUtility.SetDirty(collection.SharedData);

                EditorUtility.SetDirty(collection);
                AssetDatabase.SaveAssets();
                
                return true;
            }
            catch (Exception ex)
            {
                task.error = $"DeleteString failed for key '{key}': {ex.Message}";
                return false;
            }
        }

        private bool GetString(Task task)
        {
            string key = GetParam(task, "key");
            string language = GetOptionalParam(task, "language", "en");
            string tableName = GetOptionalParam(task, "table", "General");

            if (string.IsNullOrEmpty(key))
            {
                task.error = "GetString failed: 'key' parameter is required.";
                return false;
            }

            try
            {
                var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
                if (settings == null)
                {
                    task.error = "GetString failed: LocalizationSettings not found.";
                    return false;
                }

                var projectLocales = LocalizationEditorSettings.GetLocales();
                var locale = projectLocales?.FirstOrDefault(l => l.Identifier.Code == language);
                if (locale == null)
                {
                    task.error = $"GetString failed: Locale '{language}' not found.";
                    return false;
                }

                var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
                if (collection == null)
                {
                    task.error = $"GetString failed: String table '{tableName}' not found.";
                    return false;
                }

                var table = collection.GetTable(locale.Identifier) as UnityEngine.Localization.Tables.StringTable;
                if (table == null)
                {
                    task.error = $"GetString failed: String table for locale '{language}' not found.";
                    return false;
                }

                var entry = table.GetEntry(key);
                if (entry == null)
                {
                    task.result = "";
                    return true;
                }

                task.result = entry.Value;
                return true;
            }
            catch (Exception ex)
            {
                task.error = $"GetString failed for key '{key}': {ex.Message}";
                return false;
            }
        }

        private bool CreateAssetTable(Task task)
        {
            string tableName = GetParam(task, "tableName");
            
            if (string.IsNullOrEmpty(tableName))
            {
                task.error = "CreateAssetTable failed: 'tableName' parameter is required.";
                return false;
            }

            try
            {
                AssetDatabase.Refresh();
                
                var existingCollection = LocalizationEditorSettings.GetAssetTableCollection(tableName);
                if (existingCollection != null)
                {
                    return true; // Already exists, idempotent
                }

                var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
                if (settings == null)
                {
                    task.error = "CreateAssetTable failed: LocalizationSettings not found.";
                    return false;
                }

                // Get project locales (editor-time) instead of runtime locales
                var projectLocales = LocalizationEditorSettings.GetLocales();
                if (projectLocales == null || projectLocales.Count == 0)
                {
                    task.error = $"CreateAssetTable failed: No locales found. Add at least one language first.";
                    return false;
                }

                var collection = LocalizationEditorSettings.CreateAssetTableCollection(tableName, "Assets/Localization/Tables", projectLocales.ToList());
                
                if (collection == null)
                {
                    task.error = $"CreateAssetTable failed: Unable to create asset table collection '{tableName}'.";
                    return false;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                return true;
            }
            catch (Exception ex)
            {
                task.error = $"CreateAssetTable failed for table '{tableName}': {ex.Message}";
                return false;
            }
        }

        private bool SetAsset(Task task)
        {
            string key = GetParam(task, "key");
            string assetPath = GetParam(task, "assetPath");
            string language = GetOptionalParam(task, "language", "en");
            string tableName = GetOptionalParam(task, "table", "General");

            if (string.IsNullOrEmpty(key))
            {
                task.error = "SetAsset failed: 'key' parameter is required.";
                return false;
            }

            if (string.IsNullOrEmpty(assetPath))
            {
                task.error = "SetAsset failed: 'assetPath' parameter is required.";
                return false;
            }

            try
            {
                var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
                if (settings == null)
                {
                    task.error = "SetAsset failed: LocalizationSettings not found.";
                    return false;
                }

                // Get project locales (editor-time) instead of runtime locales
                var projectLocales = LocalizationEditorSettings.GetLocales();
                var locale = projectLocales?.FirstOrDefault(l => l.Identifier.Code == language);
                if (locale == null)
                {
                    task.error = $"SetAsset failed: Locale '{language}' not found.";
                    return false;
                }

                var collection = LocalizationEditorSettings.GetAssetTableCollection(tableName);
                if (collection == null)
                {
                    task.error = $"SetAsset failed: Asset table '{tableName}' not found.";
                    return false;
                }

                var table = collection.GetTable(locale.Identifier) as UnityEngine.Localization.Tables.AssetTable;
                if (table == null)
                {
                    task.error = $"SetAsset failed: Asset table for locale '{language}' not found.";
                    return false;
                }

                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid))
                {
                    task.error = $"SetAsset failed: Asset not found at path '{assetPath}'.";
                    return false;
                }

                var entry = table.GetEntry(key);
                if (entry == null)
                {
                    entry = table.AddEntry(key, guid);
                }
                else
                {
                    entry.Guid = guid;
                }

                EditorUtility.SetDirty(table);
                EditorUtility.SetDirty(collection);
                AssetDatabase.SaveAssets();

                return true;
            }
            catch (Exception ex)
            {
                task.error = $"SetAsset failed for key '{key}': {ex.Message}";
                return false;
            }
        }

        private bool ValidateKeys(Task task)
        {
            string sourceLanguage = GetOptionalParam(task, "sourceLanguage", "en");
            string tableName = GetOptionalParam(task, "table", "General");

            try
            {
                var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
                if (settings == null)
                {
                    task.error = "ValidateKeys failed: LocalizationSettings not found.";
                    return false;
                }

                var projectLocales = LocalizationEditorSettings.GetLocales();
                var sourceLocale = projectLocales?.FirstOrDefault(l => l.Identifier.Code == sourceLanguage);
                if (sourceLocale == null)
                {
                    task.error = $"ValidateKeys failed: Source locale '{sourceLanguage}' not found.";
                    return false;
                }

                var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
                if (collection == null)
                {
                    task.error = $"ValidateKeys failed: String table '{tableName}' not found.";
                    return false;
                }

                var sourceTable = collection.GetTable(sourceLocale.Identifier) as UnityEngine.Localization.Tables.StringTable;
                if (sourceTable == null)
                {
                    task.error = $"ValidateKeys failed: Source table for '{sourceLanguage}' not found.";
                    return false;
                }

                Debug.Log($"[LocalizationTaskExecutor] === Validation Report for table '{tableName}' ===");
                Debug.Log($"Source language: {sourceLanguage}, Total keys: {sourceTable.Count}");

                var projectLocalesForValidation = LocalizationEditorSettings.GetLocales();
                foreach (var targetLocale in projectLocalesForValidation)
                {
                    if (targetLocale.Identifier.Code == sourceLanguage)
                        continue;

                    var targetTable = collection.GetTable(targetLocale.Identifier) as UnityEngine.Localization.Tables.StringTable;
                    if (targetTable == null)
                        continue;

                    var missingKeys = new List<string>();
                    var emptyKeys = new List<string>();

                    foreach (var sourceEntry in sourceTable.Values)
                    {
                        var targetEntry = targetTable.GetEntry(sourceEntry.Key);
                        if (targetEntry == null)
                            missingKeys.Add(sourceEntry.Key);
                        else if (string.IsNullOrEmpty(targetEntry.Value))
                            emptyKeys.Add(sourceEntry.Key);
                    }

                    Debug.Log($"  {targetLocale.Identifier.Code}: {targetTable.Count} entries, {missingKeys.Count} missing, {emptyKeys.Count} empty");
                    if (missingKeys.Count > 0)
                        Debug.Log($"    Missing: {string.Join(", ", missingKeys.Take(10))}{(missingKeys.Count > 10 ? "..." : "")}");
                }

                Debug.Log("[LocalizationTaskExecutor] === End Validation Report ===");
                return true;
            }
            catch (Exception ex)
            {
                task.error = $"ValidateKeys failed: {ex.Message}";
                return false;
            }
        }

        private bool RenameKey(Task task)
        {
            string oldKey = GetParam(task, "oldKey");
            string newKey = GetParam(task, "newKey");
            string tableName = GetOptionalParam(task, "table", "General");

            if (string.IsNullOrEmpty(oldKey))
            {
                task.error = "RenameKey failed: 'oldKey' parameter is required.";
                return false;
            }

            if (string.IsNullOrEmpty(newKey))
            {
                task.error = "RenameKey failed: 'newKey' parameter is required.";
                return false;
            }

            try
            {
                var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
                if (settings == null)
                {
                    task.error = "RenameKey failed: LocalizationSettings not found.";
                    return false;
                }

                var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
                if (collection == null)
                {
                    task.error = $"RenameKey failed: String table '{tableName}' not found.";
                    return false;
                }

                // Check if old key exists
                long oldKeyId = collection.SharedData.GetId(oldKey);
                if (oldKeyId == 0)
                {
                    task.error = $"RenameKey failed: Key '{oldKey}' not found in table '{tableName}'.";
                    return false;
                }

                // Check if new key already exists
                if (collection.SharedData.Contains(newKey))
                {
                    task.error = $"RenameKey failed: Key '{newKey}' already exists in table '{tableName}'.";
                    return false;
                }

                // Rename in SharedTableData
                collection.SharedData.RenameKey(oldKey, newKey);
                EditorUtility.SetDirty(collection.SharedData);
                EditorUtility.SetDirty(collection);
                AssetDatabase.SaveAssets();

                Debug.Log($"[LocalizationTaskExecutor] RenameKey: '{oldKey}' → '{newKey}' in '{tableName}'");
                return true;
            }
            catch (Exception ex)
            {
                task.error = $"RenameKey failed: {ex.Message}";
                return false;
            }
        }

        private bool CopyStringAcrossLanguages(Task task)
        {
            string key = GetParam(task, "key");
            string sourceLanguage = GetParam(task, "sourceLanguage");
            string targetLanguage = GetParam(task, "targetLanguage");
            string tableName = GetOptionalParam(task, "table", "General");

            if (string.IsNullOrEmpty(key))
            {
                task.error = "CopyStringAcrossLanguages failed: 'key' parameter is required.";
                return false;
            }

            if (string.IsNullOrEmpty(sourceLanguage))
            {
                task.error = "CopyStringAcrossLanguages failed: 'sourceLanguage' parameter is required.";
                return false;
            }

            if (string.IsNullOrEmpty(targetLanguage))
            {
                task.error = "CopyStringAcrossLanguages failed: 'targetLanguage' parameter is required.";
                return false;
            }

            try
            {
                var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
                if (settings == null)
                {
                    task.error = "CopyStringAcrossLanguages failed: LocalizationSettings not found.";
                    return false;
                }

                var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
                if (collection == null)
                {
                    task.error = $"CopyStringAcrossLanguages failed: String table '{tableName}' not found.";
                    return false;
                }

                var projectLocales = LocalizationEditorSettings.GetLocales();
                var sourceLocale = projectLocales?.FirstOrDefault(l => l.Identifier.Code == sourceLanguage);
                if (sourceLocale == null)
                {
                    task.error = $"CopyStringAcrossLanguages failed: Source locale '{sourceLanguage}' not found.";
                    return false;
                }

                var targetLocale = projectLocales?.FirstOrDefault(l => l.Identifier.Code == targetLanguage);
                if (targetLocale == null)
                {
                    task.error = $"CopyStringAcrossLanguages failed: Target locale '{targetLanguage}' not found.";
                    return false;
                }

                var sourceTable = collection.GetTable(sourceLocale.Identifier) as UnityEngine.Localization.Tables.StringTable;
                if (sourceTable == null)
                {
                    task.error = $"CopyStringAcrossLanguages failed: Source table for '{sourceLanguage}' not found.";
                    return false;
                }

                var targetTable = collection.GetTable(targetLocale.Identifier) as UnityEngine.Localization.Tables.StringTable;
                if (targetTable == null)
                {
                    task.error = $"CopyStringAcrossLanguages failed: Target table for '{targetLanguage}' not found.";
                    return false;
                }

                var sourceEntry = sourceTable.GetEntry(key);
                if (sourceEntry == null)
                {
                    task.error = $"CopyStringAcrossLanguages failed: Key '{key}' not found in source language '{sourceLanguage}'.";
                    return false;
                }

                var targetEntry = targetTable.GetEntry(key);
                if (targetEntry == null)
                {
                    targetTable.AddEntry(key, sourceEntry.Value);
                }
                else
                {
                    targetEntry.Value = sourceEntry.Value;
                }

                EditorUtility.SetDirty(targetTable);
                EditorUtility.SetDirty(collection);
                AssetDatabase.SaveAssets();

                Debug.Log($"[LocalizationTaskExecutor] CopyString: '{key}' from '{sourceLanguage}' → '{targetLanguage}'");
                return true;
            }
            catch (Exception ex)
            {
                task.error = $"CopyStringAcrossLanguages failed: {ex.Message}";
                return false;
            }
        }

        private bool ExportTableToCSV(Task task)
        {
            string tableName = GetParam(task, "table");
            string outputPath = GetParam(task, "outputPath");

            if (string.IsNullOrEmpty(tableName))
            {
                task.error = "ExportTableToCSV failed: 'table' parameter is required.";
                return false;
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                task.error = "ExportTableToCSV failed: 'outputPath' parameter is required.";
                return false;
            }

            try
            {
                var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
                if (settings == null)
                {
                    task.error = "ExportTableToCSV failed: LocalizationSettings not found.";
                    return false;
                }

                var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
                if (collection == null)
                {
                    task.error = $"ExportTableToCSV failed: String table '{tableName}' not found.";
                    return false;
                }

                var projectLocales = LocalizationEditorSettings.GetLocales();
                if (projectLocales == null || projectLocales.Count == 0)
                {
                    task.error = "ExportTableToCSV failed: No locales found.";
                    return false;
                }

                // Create directory if needed
                string directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (StreamWriter writer = new StreamWriter(outputPath))
                {
                    // Write header
                    writer.Write("Key");
                    foreach (var locale in projectLocales)
                    {
                        writer.Write($",{locale.Identifier.Code}");
                    }
                    writer.WriteLine();

                    // Write entries
                    if (collection.SharedData != null)
                    {
                        foreach (var sharedEntry in collection.SharedData.Entries)
                        {
                            writer.Write($"\"{sharedEntry.Key}\"");

                            foreach (var locale in projectLocales)
                            {
                                var table = collection.GetTable(locale.Identifier) as UnityEngine.Localization.Tables.StringTable;
                                string value = "";
                                if (table != null)
                                {
                                    var entry = table.GetEntry(sharedEntry.Key);
                                    if (entry != null)
                                    {
                                        value = entry.Value?.Replace("\"", "\"\"") ?? ""; // Escape quotes
                                    }
                                }
                                writer.Write($",\"{value}\"");
                            }
                            writer.WriteLine();
                        }
                    }
                }

                Debug.Log($"[LocalizationTaskExecutor] ExportCSV: '{tableName}' → '{outputPath}'");
                AssetDatabase.Refresh();
                return true;
            }
            catch (Exception ex)
            {
                task.error = $"ExportTableToCSV failed: {ex.Message}";
                return false;
            }
        }

        private bool ImportTableFromCSV(Task task)
        {
            string csvPath = GetParam(task, "csvPath");
            string tableName = GetParam(task, "table");

            if (string.IsNullOrEmpty(csvPath))
            {
                task.error = "ImportTableFromCSV failed: 'csvPath' parameter is required.";
                return false;
            }

            if (string.IsNullOrEmpty(tableName))
            {
                task.error = "ImportTableFromCSV failed: 'table' parameter is required.";
                return false;
            }

            try
            {
                if (!File.Exists(csvPath))
                {
                    task.error = $"ImportTableFromCSV failed: File not found at '{csvPath}'.";
                    return false;
                }

                var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
                if (settings == null)
                {
                    task.error = "ImportTableFromCSV failed: LocalizationSettings not found.";
                    return false;
                }

                var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
                if (collection == null)
                {
                    task.error = $"ImportTableFromCSV failed: String table '{tableName}' not found.";
                    return false;
                }

                string[] lines = File.ReadAllLines(csvPath);
                if (lines.Length < 2)
                {
                    task.error = "ImportTableFromCSV failed: CSV file is empty or invalid.";
                    return false;
                }

                // Parse header
                string[] headers = ParseCSVLine(lines[0]);
                if (headers.Length < 2 || headers[0].ToLower() != "key")
                {
                    task.error = "ImportTableFromCSV failed: Invalid CSV header. Expected 'Key' as first column.";
                    return false;
                }

                // Map locale codes to their tables
                var localeTables = new Dictionary<string, UnityEngine.Localization.Tables.StringTable>();
                var projectLocales = LocalizationEditorSettings.GetLocales();
                
                for (int i = 1; i < headers.Length; i++)
                {
                    string localeCode = headers[i].Trim();
                    var locale = projectLocales?.FirstOrDefault(l => l.Identifier.Code == localeCode);
                    if (locale != null)
                    {
                        var table = collection.GetTable(locale.Identifier) as UnityEngine.Localization.Tables.StringTable;
                        if (table != null)
                        {
                            localeTables[localeCode] = table;
                        }
                    }
                }

                int importedCount = 0;
                int updatedCount = 0;

                // Import data
                for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
                {
                    string[] values = ParseCSVLine(lines[lineIndex]);
                    if (values.Length < 2)
                        continue;

                    string key = values[0];
                    if (string.IsNullOrEmpty(key))
                        continue;

                    for (int i = 1; i < values.Length && i < headers.Length; i++)
                    {
                        string localeCode = headers[i].Trim();
                        if (localeTables.TryGetValue(localeCode, out var table))
                        {
                            string value = values[i];
                            var entry = table.GetEntry(key);
                            if (entry == null)
                            {
                                table.AddEntry(key, value);
                                importedCount++;
                            }
                            else
                            {
                                entry.Value = value;
                                updatedCount++;
                            }
                            EditorUtility.SetDirty(table);
                        }
                    }
                }

                EditorUtility.SetDirty(collection);
                AssetDatabase.SaveAssets();

                Debug.Log($"[LocalizationTaskExecutor] ImportCSV: {importedCount} new, {updatedCount} updated from '{csvPath}'");
                return true;
            }
            catch (Exception ex)
            {
                task.error = $"ImportTableFromCSV failed: {ex.Message}";
                return false;
            }
        }

        private string[] ParseCSVLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var currentValue = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentValue.Append('"');
                        i++; // Skip next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            result.Add(currentValue.ToString());
            return result.ToArray();
        }
    }
}
#endif
