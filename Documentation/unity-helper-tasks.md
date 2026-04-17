# Task Parameters Reference

Quick reference for scenarios.json task parameters. Unity Helper builds on PerSpec's coordination infrastructure.

## Scenario File Location

Place scenario JSON files in your project's `Assets/` folder, e.g. `Assets/Scenarios/scenarios.json`.

---

## Execution

### Via Unity UI (Scenario Runner)
Unity Menu → `Tools → PerSpec → Scenario Runner` — browse, run, skip, and retry tasks interactively.

### Via CLI
```bash
# Single inline action (no scenario file needed)
python PerSpec/Coordination/Scripts/unityhelper_coordinator.py execute \
  --action ExportHierarchyPrefab --param prefabPath=Assets/Prefabs/MyPrefab.prefab --focus --wait

# Full scenario file
python PerSpec/Coordination/Scripts/unityhelper_coordinator.py execute \
  --file Assets/Scenarios/scenarios.json --focus --wait
```

---

## Task Types

| Type | Value | Purpose | Executor |
|------|-------|---------|----------|
| **SCENE** | `0` | Scene/GameObject/Component operations | `SceneTaskExecutor.cs` |
| **LOCALIZATION** | `1` | Localization system operations | `LocalizationTaskExecutor.cs` |

## Available Actions & Parameters

### CreateScene
```json
{
    "action": "CreateScene",
    "parameters": [
        {"key": "name", "value": "SceneName"},
        {"key": "path", "value": "Assets/Scenes"}
    ]
}
```

### LoadScene
```json
{
    "action": "LoadScene",
    "parameters": [
        {"key": "path", "value": "Assets/Scenes/SceneName.unity"}
    ]
}
```

### AddGameObject
```json
{
    "action": "AddGameObject",
    "parameters": [
        {"key": "name", "value": "GameObjectName"},
        {"key": "parent", "value": "ParentPath"} // Optional
    ]
}
```

### AddComponent
```json
{
    "action": "AddComponent",
    "parameters": [
        {"key": "path", "value": "GameObject/Path"},
        {"key": "component", "value": "Namespace.TypeName, AssemblyName"}
    ]
}
```
**IMPORTANT:** `component` must be a **fully qualified assembly name** in .NET format:
- **Format:** `Namespace.TypeName, AssemblyName`
- **UI Components:** `"UnityEngine.UI.Image, UnityEngine.UI"`
- **Core Unity:** `"UnityEngine.Rigidbody, UnityEngine"`
- **TextMeshPro:** `"TMPro.TextMeshProUGUI, Unity.TextMeshPro"`
- **Project Scripts:** `"MyNamespace.WelcomeSceneManager, Assembly-CSharp"`

**Common Examples:**
```json
// Add Image component
{"key": "component", "value": "UnityEngine.UI.Image, UnityEngine.UI"}

// Add Button component
{"key": "component", "value": "UnityEngine.UI.Button, UnityEngine.UI"}

// Add TMP_Text component
{"key": "component", "value": "TMPro.TextMeshProUGUI, Unity.TextMeshPro"}

// Add custom script
{"key": "component", "value": "MyNamespace.LoginSceneManager, Assembly-CSharp"}
```

### InstantiatePrefab
```json
{
    "action": "InstantiatePrefab",
    "parameters": [
        {"key": "prefabPath", "value": "Assets/Prefabs/UI/Prefab.prefab"},
        {"key": "name", "value": "InstanceName"},
        {"key": "parent", "value": "ParentPath"} // Optional
    ]
}
```

### CreateCanvas
```json
{
    "action": "CreateCanvas",
    "parameters": [
        {"key": "name", "value": "CanvasName"}
    ]
}
```

### SetProperty
**Unified action for scene GameObjects, prefabs, and ScriptableObject assets**

**For current scene (backward compatible):**
```json
{
    "action": "SetProperty",
    "parameters": [
        {"key": "path", "value": "Canvas/Button"},
        {"key": "component", "value": "Image"},
        {"key": "field", "value": "sprite"},
        {"key": "value", "value": "Assets/Icons/Login.png"}
    ]
}
```

**For ScriptableObject asset:**
```json
{
    "action": "SetProperty",
    "parameters": [
        {"key": "owner", "value": "Assets/DataService/DefaultSprites.asset"},
        {"key": "field", "value": "gameAudio"},
        {"key": "value", "value": "Assets/ScreenTextures/GameAudio.png"}
    ]
}
```
Note: For assets, `path` and `component` are NOT needed - only `owner`, `field`, and `value`.

**For wiring component references (scene GameObjects):**
```json
{
    "action": "SetProperty",
    "parameters": [
        {"key": "path", "value": "Canvas/GameStatus"},
        {"key": "component", "value": "MyNamespace.GameStatusUI"},
        {"key": "field", "value": "gameIcon"},
        {"key": "value", "value": "Canvas/GameStatus/GameIcon"}
    ]
}
```
- `value` is a GameObject path in the scene (not starting with "Assets/")
- Automatically finds the GameObject and gets the required component type
- Works for Image, TMP_Text, Slider, etc.

**For prefab:**
```json
{
    "action": "SetProperty",
    "parameters": [
        {"key": "owner", "value": "Assets/Prefabs/UI/Button.prefab"},
        {"key": "path", "value": "Icon"},
        {"key": "component", "value": "Image"},
        {"key": "field", "value": "color"},
        {"key": "value", "value": "1,1,1,1"}
    ]
}
```

**For specific scene file:**
```json
{
    "action": "SetProperty",
    "parameters": [
        {"key": "owner", "value": "Assets/Scenes/Login.unity"},
        {"key": "path", "value": "Canvas/LoginButton"},
        {"key": "component", "value": "Button"},
        {"key": "field", "value": "interactable"},
        {"key": "value", "value": "true"}
    ]
}
```

### SetTransform
```json
{
    "action": "SetTransform",
    "parameters": [
        {"key": "path", "value": "GameObject/Path"},
        {"key": "position", "value": "0,0,0"}, // Optional
        {"key": "rotation", "value": "0,0,0"}, // Optional
        {"key": "scale", "value": "1,1,1"},    // Optional
        {"key": "siblingIndex", "value": "0"}  // Optional (-1 for last)
    ]
}
```

### SetRectTransform
```json
{
    "action": "SetRectTransform",
    "parameters": [
        {"key": "path", "value": "GameObject/Path"},
        {"key": "anchorMin", "value": "0,0"},           // Optional
        {"key": "anchorMax", "value": "1,1"},           // Optional
        {"key": "pivot", "value": "0.5,0.5"},           // Optional
        {"key": "anchoredPosition", "value": "0,0"},    // Optional
        {"key": "sizeDelta", "value": "100,100"},       // Optional
        {"key": "offsetMin", "value": "0,0"},           // Optional
        {"key": "offsetMax", "value": "0,0"}            // Optional
    ]
}
```

### DeleteGameObject
```json
{
    "action": "DeleteGameObject",
    "parameters": [
        {"key": "path", "value": "GameObject/Path"}
    ]
}
```

### SaveScene
```json
{
    "action": "SaveScene",
    "parameters": [
        {"key": "path", "value": "Assets/Scenes/SceneName.unity"}
    ]
}
```

### AddToBuildSettings
```json
{
    "action": "AddToBuildSettings",
    "parameters": [
        {"key": "scenePath", "value": "Assets/Scenes/SceneName.unity"}
    ]
}
```

### CreateScriptableObject
```json
{
    "action": "CreateScriptableObject",
    "parameters": [
        {"key": "type", "value": "MyNamespace.SpriteManager, Assembly-CSharp"},
        {"key": "path", "value": "Assets/DefaultSprites.asset"}
    ]
}
```
**IMPORTANT:** `type` must be a **fully qualified assembly name** in .NET format:
- **Format:** `Namespace.TypeName, AssemblyName`
- **Project ScriptableObjects:** `"MyNamespace.ThemeManager, Assembly-CSharp"`
- **Unity ScriptableObjects:** Use the assembly where the type is defined

**Common Examples:**
```json
// Custom ScriptableObject
{"key": "type", "value": "MyNamespace.ThemeManager, Assembly-CSharp"}
{"key": "type", "value": "MyNamespace.DataService, Assembly-CSharp"}
```

### SaveAsPrefab
Saves a GameObject from the current scene as a prefab asset.
```json
{
    "action": "SaveAsPrefab",
    "parameters": [
        {"key": "path", "value": "Canvas/StatCard"},
        {"key": "prefabPath", "value": "Assets/Prefabs/UI/StatCard.prefab"}
    ]
}
```
- `path` - GameObject path in scene hierarchy
- `prefabPath` - Where to save the prefab (creates directory if needed)
- Idempotent: Replaces existing prefab if it already exists

### SetListProperty
Sets a list property on a ScriptableObject with multiple asset references.

**Simple lists (pipe-separated values):**
```json
{
    "action": "SetListProperty",
    "parameters": [
        {"key": "owner", "value": "Assets/DataService/SpriteManager.asset"},
        {"key": "field", "value": "gameIcons"},
        {"key": "values", "value": "Assets/Icons/Game1.png|Assets/Icons/Game2.png|Assets/Icons/Game3.png"},
        {"key": "elementType", "value": "Sprite"},
        {"key": "append", "value": "false"}
    ]
}
```

**Complex/nested lists (JSON format):**
```json
{
    "action": "SetListProperty",
    "parameters": [
        {"key": "owner", "value": "Assets/DataService/SceneNavigationData.asset"},
        {"key": "field", "value": "scenes"},
        {"key": "json", "value": "[{\"scene\":\"Assets/Scenes/WelcomeScene.unity\",\"nextScenes\":[\"Assets/Scenes/SignInScene.unity\",\"Assets/Scenes/CreateAccountScene.unity\"],\"description\":\"[0] SignIn, [1] CreateAccount\"}]"}
    ]
}
```

**Parameters:**
- `owner` - Path to the ScriptableObject asset
- `field` - Name of the List<T> field to set
- `values` - Pipe-separated (`|`) asset paths (for simple lists)
- `json` - JSON array string (for complex/nested types)
- `elementType` (optional) - Type of list elements. Auto-detected from field if not provided
- `append` (optional) - "true" to append to existing list, default "false" replaces

**Supported Element Types:**
- `SceneAsset` - Unity scene files (.unity)
- `Sprite` - Sprite/texture assets
- `GameObject` / `Prefab` - Prefab assets
- `Texture2D` / `Texture` - Texture assets
- `AudioClip` / `Audio` - Audio files
- `Material` - Material assets
- `ScriptableObject` - Custom ScriptableObjects
- Any fully qualified type name

**JSON Format for Nested Types:**
When using `json` parameter, the format matches your class structure:
- String values starting with "Assets/" are loaded as assets
- Nested classes with `sceneAsset` field (like `SceneReference`) are auto-populated
- Arrays of strings become `List<SceneReference>` when the field type matches

**Example - Set simple scene list (via scenario JSON):**
```json
{
    "action": "SetListProperty",
    "parameters": [
        {"key": "owner", "value": "Assets/Data/NavData.asset"},
        {"key": "field", "value": "scenes"},
        {"key": "values", "value": "Assets/Scenes/Home.unity|Assets/Scenes/Game.unity"},
        {"key": "elementType", "value": "SceneAsset"}
    ]
}
```

**Example - Set complex navigation list (SceneNavigationData):**
```json
{
    "action": "SetListProperty",
    "parameters": [
        {"key": "owner", "value": "Assets/DataService/SceneNavigationData.asset"},
        {"key": "field", "value": "scenes"},
        {"key": "json", "value": "[{\"scene\":\"Assets/Scenes/WelcomeScene.unity\",\"nextScenes\":[\"Assets/Scenes/SignInScene.unity\",\"Assets/Scenes/CreateAccountScene.unity\"],\"description\":\"[0] SignIn, [1] CreateAccount\"}]"}
    ]
}
```

**Example - Append sprites to list:**
```json
{
    "action": "SetListProperty",
    "parameters": [
        {"key": "owner", "value": "Assets/Data/Icons.asset"},
        {"key": "field", "value": "iconList"},
        {"key": "values", "value": "Assets/Icons/New1.png|Assets/Icons/New2.png"},
        {"key": "append", "value": "true"}
    ]
}
```

## Localization Actions

### CreateLocalizationSettings
Checks if Unity Localization system is initialized. **Must be run FIRST before any other localization actions.**
```json
{
    "type": "localization",
    "action": "CreateLocalizationSettings",
    "parameters": []
}
```
- No parameters required
- Checks if LocalizationSettings exists
- If not exists, provides instructions to create it manually via Unity Editor
- Idempotent: Returns success if already exists
- **PREREQUISITE:** Must run before AddLanguage, CreateStringTable, SetString, or UpdateAll

### DebugAllLocalizationInfo
Logs comprehensive information about the localization system for debugging purposes.
```json
{
    "type": "localization",
    "action": "DebugAllLocalizationInfo",
    "parameters": []
}
```
- No parameters required
- Logs all available localization data to Unity Console:
  - Settings info (name, path, type)
  - Selected locale
  - Project locales (Editor)
  - Available locales (Runtime)
  - String and Asset databases
  - String table collections with sample entries
  - Asset table collections with sample entries
- Always succeeds if LocalizationSettings exists
- Useful for troubleshooting localization setup

### AddLanguage
Adds a language to the localization system.
```json
{
    "type": "localization",
    "action": "AddLanguage",
    "parameters": [
        {"key": "languageCode", "value": "es"},
        {"key": "displayName", "value": "Spanish"}
    ]
}
```
- `languageCode` - ISO language code (e.g., "es", "fr", "zh-Hant")
- `displayName` - Human-readable name for the language

### CreateStringTable
Creates a string table collection for all available locales.
```json
{
    "type": "localization",
    "action": "CreateStringTable",
    "parameters": [
        {"key": "tableName", "value": "General"}
    ]
}
```
- `tableName` - Name of the string table collection (required)
- Automatically includes all available locales
- Idempotent: Returns success if table already exists

### SetString
Sets a localized string value.
```json
{
    "type": "localization",
    "action": "SetString",
    "parameters": [
        {"key": "key", "value": "welcome_message"},
        {"key": "value", "value": "Welcome!"},
        {"key": "language", "value": "en"},
        {"key": "table", "value": "General"}
    ]
}
```
- `key` - Localization key identifier (required)
- `value` - The localized text (required)
- `language` - Language code (optional, defaults to "en")
- `table` - String table name (optional, defaults to "General")

### UpdateAll
Updates all localizations from a source language.
```json
{
    "type": "localization",
    "action": "UpdateAll",
    "parameters": [
        {"key": "sourceLanguage", "value": "en"},
        {"key": "force", "value": "true"},
        {"key": "table", "value": "General"}
    ]
}
```
- `sourceLanguage` - Source language to update from (optional, defaults to "en")
- `force` - Whether to force update existing translations (optional, defaults to "false")
- `table` - String table name to update (optional, defaults to "General")

### BulkSetStrings ⭐
**GAME CHANGER:** Import ALL strings from a file at once! Replaces dozens of SetString tasks.
```json
{
    "type": "localization",
    "action": "BulkSetStrings",
    "parameters": [
        {"key": "filePath", "value": "Assets/Localization/localization_strings.txt"},
        {"key": "language", "value": "en"},
        {"key": "table", "value": "General"}
    ]
}
```
- `filePath` - Path to text file with key=value or key:value format (required)
- `language` - Language code (optional, defaults to "en")
- `table` - Table name (optional, defaults to "General")
- File format: `key=value` or `key:value`, one per line
- Supports comments: lines starting with # or //
- Logs count of added/updated entries

### RemoveLanguage
Removes a language from the localization system.
```json
{
    "type": "localization",
    "action": "RemoveLanguage",
    "parameters": [
        {"key": "languageCode", "value": "es"}
    ]
}
```
- `languageCode` - ISO language code to remove (required)
- Idempotent: Returns success if already removed

### DeleteString
Removes a string key from all languages in a table.
```json
{
    "type": "localization",
    "action": "DeleteString",
    "parameters": [
        {"key": "key", "value": "obsolete_key"},
        {"key": "table", "value": "General"}
    ]
}
```
- `key` - Localization key to delete (required)
- `table` - Table name (optional, defaults to "General")

### GetString
Logs the current value of a localization key (useful for verification).
```json
{
    "type": "localization",
    "action": "GetString",
    "parameters": [
        {"key": "key", "value": "welcome_message"},
        {"key": "language", "value": "en"},
        {"key": "table", "value": "General"}
    ]
}
```
- `key` - Localization key (required)
- `language` - Language code (optional, defaults to "en")
- `table` - Table name (optional, defaults to "General")

### CreateAssetTable
Creates an asset table collection for localizing assets (sprites, audio, etc.).
```json
{
    "type": "localization",
    "action": "CreateAssetTable",
    "parameters": [
        {"key": "tableName", "value": "UI_Assets"}
    ]
}
```
- `tableName` - Name of the asset table collection (required)
- Automatically includes all available locales
- Idempotent: Returns success if table already exists

### SetAsset
Assigns an asset to a localization key.
```json
{
    "type": "localization",
    "action": "SetAsset",
    "parameters": [
        {"key": "key", "value": "flag_icon"},
        {"key": "assetPath", "value": "Assets/Sprites/Flags/US.png"},
        {"key": "language", "value": "en"},
        {"key": "table", "value": "UI_Assets"}
    ]
}
```
- `key` - Localization key (required)
- `assetPath` - Path to asset file (required)
- `language` - Language code (optional, defaults to "en")
- `table` - Asset table name (optional, defaults to "General")

### ValidateKeys
Quality assurance tool - checks which translations are missing or empty.
```json
{
    "type": "localization",
    "action": "ValidateKeys",
    "parameters": [
        {"key": "sourceLanguage", "value": "en"},
        {"key": "table", "value": "General"}
    ]
}
```
- `sourceLanguage` - Source language to compare against (optional, defaults to "en")
- `table` - Table name (optional, defaults to "General")
- Logs comprehensive validation report to Unity Console

### RenameKey
Safely renames a key across all languages.
```json
{
    "type": "localization",
    "action": "RenameKey",
    "parameters": [
        {"key": "oldKey", "value": "old_name"},
        {"key": "newKey", "value": "new_name"},
        {"key": "table", "value": "General"}
    ]
}
```
- `oldKey` - Current key name (required)
- `newKey` - New key name (required)
- `table` - Table name (optional, defaults to "General")
- Renames in SharedTableData (affects all languages)
- Validates old key exists and new key doesn't exist

### CopyStringAcrossLanguages
Copies a specific string from one language to another.
```json
{
    "type": "localization",
    "action": "CopyStringAcrossLanguages",
    "parameters": [
        {"key": "key", "value": "welcome_message"},
        {"key": "sourceLanguage", "value": "en"},
        {"key": "targetLanguage", "value": "es"},
        {"key": "table", "value": "General"}
    ]
}
```
- `key` - Localization key to copy (required)
- `sourceLanguage` - Source language code (required)
- `targetLanguage` - Target language code (required)
- `table` - Table name (optional, defaults to "General")
- More precise than UpdateAll for single key copies

### ExportTableToCSV
Exports table to CSV for translators.
```json
{
    "type": "localization",
    "action": "ExportTableToCSV",
    "parameters": [
        {"key": "table", "value": "General"},
        {"key": "outputPath", "value": "Assets/Localization/Export/General.csv"}
    ]
}
```
- `table` - Table name to export (required)
- `outputPath` - Where to save CSV file (required)
- CSV format: First column is Key, subsequent columns are language codes
- Properly escapes quotes and commas
- Creates directory if it doesn't exist

### ImportTableFromCSV
Imports translations from CSV.
```json
{
    "type": "localization",
    "action": "ImportTableFromCSV",
    "parameters": [
        {"key": "csvPath", "value": "Assets/Localization/Import/General_translated.csv"},
        {"key": "table", "value": "General"}
    ]
}
```
- `csvPath` - Path to CSV file (required)
- `table` - Target table name (required)
- CSV format: First column must be "Key", subsequent columns are language codes
- Handles quoted values and escaped quotes
- Logs count of imported and updated entries

### ExportHierarchy
Exports the **active scene** hierarchy to `task.result` with GameObjects and components.
```json
{
    "action": "ExportHierarchy",
    "parameters": []
}
```
- No parameters required
- Exports all GameObjects in active scene with their components
- Shows hierarchy structure with indentation
- Result stored in `task.result` (two-way communication)
- Also logs output to Unity Console

### ExportHierarchyScene
Opens a **specific scene file**, exports its hierarchy, then restores the previously active scene.
```json
{
    "action": "ExportHierarchyScene",
    "parameters": [
        {"key": "scenePath", "value": "Assets/Scenes/MyScene.unity"}
    ]
}
```
- `scenePath` - Path to the `.unity` file (required)
- Exports all GameObjects and components with indentation
- Restores the previously active scene after export
- Result stored in `task.result`

### ExportHierarchyPrefab
Exports the hierarchy of a **prefab asset** to `task.result` without loading a scene.
```json
{
    "action": "ExportHierarchyPrefab",
    "parameters": [
        {"key": "prefabPath", "value": "Assets/Prefabs/MyPrefab.prefab"}
    ]
}
```
- `prefabPath` - Path to the `.prefab` asset (required)
- Loads the prefab via `AssetDatabase` — no scene required
- Shows full child hierarchy with components
- Result stored in `task.result`

### TakeScreenshot
Captures a screenshot of the current Game view with configurable output and resolution.
```json
{
    "action": "TakeScreenshot",
    "parameters": [
        {"key": "outputDir", "value": "Assets/Screenshots"},
        {"key": "filename", "value": "welcome-scene"},
        {"key": "supersampling", "value": "2"}
    ]
}
```
- `outputDir` - Directory to save screenshots (optional, default: `Assets/Screenshots`)
- `filename` - Base filename without extension (optional, default: `screenshot`)
- `supersampling` - Resolution multiplier, e.g. 2 for 2x (optional, default: `1`)
- Automatically appends timestamp: `{filename}_{yyyyMMdd_HHmmss}.png`
- Creates output directory if it doesn't exist
- Stores the full output path in `task.result`

**Scenario JSON Example:**
```json
{
    "action": "TakeScreenshot",
    "parameters": [
        {"key": "outputDir", "value": "Assets/Screenshots"},
        {"key": "filename", "value": "test"}
    ]
}
```

### WaitForGameObject
Waits for a GameObject to appear in the scene, with optional component type verification. Uses async polling via `EditorApplication.delayCall`.
```json
{
    "action": "WaitForGameObject",
    "parameters": [
        {"key": "path", "value": "WelcomeSceneController"},
        {"key": "component", "value": "MyNamespace.WelcomeSceneManager, Assembly-CSharp"},
        {"key": "timeout", "value": "10"}
    ]
}
```
- `path` - GameObject name or hierarchy path to find via `GameObject.Find()` (required)
- `component` - Fully qualified component type to verify on the found object (optional)
- `timeout` - Maximum wait time in seconds (optional, default: `10`)
- Returns immediately if the GameObject (and optional component) is already present
- If not found, polls asynchronously until found or timeout
- Stores `"Found: {path}"` in `task.result` on success
- **Note:** Async polling only works when run via the Scenario Runner (EditorWindow). The CLI coordinator executes synchronously (immediate check only).

### CallMethod
Calls a method on a component via reflection. Replaces hardcoded test helper calls with a generic, parameterized action.
```json
{
    "action": "CallMethod",
    "parameters": [
        {"key": "path", "value": "WelcomeSceneController"},
        {"key": "component", "value": "MyNamespace.WelcomeSceneManager, Assembly-CSharp"},
        {"key": "method", "value": "TestHelper_ClickLoginButton"}
    ]
}
```
- `path` - GameObject name or hierarchy path (required)
- `component` - Fully qualified component type name (required)
- `method` - Method name to invoke (required). Supports public and non-public instance methods.
- Invokes parameterless methods only (no arguments)
- Stores the method return value (or `"void"`) in `task.result`

**Example: Full UI Flow as a Scenario**

The old hardcoded `TestRunnerUIFlow` can now be expressed as a declarative JSON scenario:
```json
{
    "name": "UI Flow Screenshots",
    "taskGroups": [{
        "name": "Welcome to Home Flow",
        "tasks": [
            {"action": "LoadScene", "parameters": [{"key": "path", "value": "Assets/Scenes/MainScene.unity"}]},
            {"action": "WaitForGameObject", "parameters": [{"key": "path", "value": "WelcomeSceneController"}, {"key": "timeout", "value": "10"}]},
            {"action": "TakeScreenshot", "parameters": [{"key": "outputDir", "value": "Assets/Screenshots/UIFlow"}, {"key": "filename", "value": "01_welcome"}]},
            {"action": "CallMethod", "parameters": [{"key": "path", "value": "WelcomeSceneController"}, {"key": "component", "value": "Undugu.WelcomeSceneManager, Assembly-CSharp"}, {"key": "method", "value": "TestHelper_ClickLoginButton"}]},
            {"action": "WaitForGameObject", "parameters": [{"key": "path", "value": "SignInSceneManager"}, {"key": "timeout", "value": "10"}]},
            {"action": "TakeScreenshot", "parameters": [{"key": "outputDir", "value": "Assets/Screenshots/UIFlow"}, {"key": "filename", "value": "02_signin"}]}
        ]
    }]
}
```

### InspectGameObject
Reads component field values from a GameObject at runtime. **Works in Play mode!**
```json
{
    "action": "InspectGameObject",
    "parameters": [
        {"key": "path", "value": "LoginSceneManager"},
        {"key": "component", "value": "LoginSceneManager"},
        {"key": "fields", "value": "loginButton,usernameInput,passwordInput"}
    ]
}
```
- `path` - GameObject path in hierarchy (required)
- `component` - Component type name (optional, inspects all if omitted)
- `fields` - Comma-separated field names or "all" (optional, defaults to "all")
- Returns field names and values in task.result
- Shows null/assigned state for Unity Object references
- Can inspect private and public fields
- **Use Case:** Debug component state, verify LoadVars() found references correctly

## Common Mistakes to Avoid

❌ **WRONG:**
- `"gameObjectPath"` → Use `"path"`
- `"componentType"` → Use `"component"`
- `"parentPath"` → Use `"parent"`
- `"component": "Image"` → Use fully qualified: `"UnityEngine.UI.Image, UnityEngine.UI"`
- `"component": "UnityEngine.UI.Image"` → Missing assembly: `"UnityEngine.UI.Image, UnityEngine.UI"`
- `"type": "MyNamespace.ThemeManager"` → Missing assembly: `"MyNamespace.ThemeManager, Assembly-CSharp"`

✅ **RIGHT:**
- Always use exact parameter names from this reference
- AddComponent requires **full assembly-qualified names**: `"Namespace.Type, AssemblyName"`
- CreateScriptableObject requires **full assembly-qualified names**: `"Namespace.Type, AssemblyName"`
- Check TaskExecutor.cs if unsure
- Parameters are case-sensitive
