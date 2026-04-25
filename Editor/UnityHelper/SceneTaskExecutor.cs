using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

namespace PerSpec.UnityHelper.Editor
{
    public class SceneTaskExecutor : BaseTaskExecutor
    {
        public override ExecutorType Type => ExecutorType.SCENE;
        
        private UnityEngine.SceneManagement.Scene currentScene;
        private bool _lastExecuteWasAsync;

        public override bool IsAsyncTask(Task task) => _lastExecuteWasAsync;

        public override bool Execute(Task task)
        {
            try
            {
                _lastExecuteWasAsync = false;
                switch (task.action)
                {
                    case "CreateScene":
                        return CreateScene(task);
                    case "LoadScene":
                        return LoadScene(task);
                    case "AddGameObject":
                        return AddGameObject(task);
                    case "AddComponent":
                        return AddComponent(task);
                    case "InstantiatePrefab":
                        return InstantiatePrefab(task);
                    case "SetProperty":
                        return SetPropertyUnified(task);
                    case "SetTransform":
                        return SetTransform(task);
                    case "SetRectTransform":
                        return SetRectTransform(task);
                    case "SaveScene":
                        return SaveScene(task);
                    case "CreateCanvas":
                        return CreateCanvas(task);
                    case "DeleteGameObject":
                        return DeleteGameObject(task);
                    case "AddToBuildSettings":
                        return AddToBuildSettings(task);
                    case "RenameGameObject":
                        return RenameGameObject(task);
                    case "CreateScriptableObject":
                        return CreateScriptableObject(task);
                    case "SaveAsPrefab":
                        return SaveAsPrefab(task);
                    case "ExportHierarchy":
                        return ExportHierarchy(task);
                    case "ExportHierarchyPrefab":
                        return ExportHierarchyPrefab(task);
                    case "ExportHierarchyScene":
                        return ExportHierarchyScene(task);
                    case "InspectGameObject":
                        return InspectGameObject(task);
                    case "SetListProperty":
                        return SetListProperty(task);
                    case "TakeScreenshot":
                        return TakeScreenshot(task);
                    case "WaitForGameObject":
                        return WaitForGameObject(task);
                    case "CallMethod":
                        return CallMethod(task);
                    case "SetParentByTransform":
                        return SetParentByTransform(task);
                    case "OpenPrefab":
                        return OpenPrefab(task);
                    case "SavePrefab":
                        return SavePrefab(task);
                    default:
                        task.error = $"Unknown action: {task.action}";
                        return false;
                }
            }
            catch (Exception ex)
            {
                task.error = ex.Message;
                return false;
            }
        }

        private bool CreateScene(Task task)
        {
            string name = GetParam(task, "name");
            string scenePath = $"Assets/Scenes/{name}.unity";
            
            if (System.IO.File.Exists(scenePath))
            {
                currentScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                Debug.Log($"[SceneTaskExecutor]Scene already exists, opened: {name}");
                return true;
            }
            
            currentScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Debug.Log($"[SceneTaskExecutor]Created scene: {name} (empty - no camera)");
            return true;
        }

        private bool AddGameObject(Task task)
        {
            string name = GetParam(task, "name");
            string parent = GetOptionalParam(task, "parent");

            var fullPath = string.IsNullOrEmpty(parent) ? name : $"{parent}/{name}";

            if (GameObject.Find(fullPath) != null)
            {
                Debug.Log($"[SceneTaskExecutor]GameObject already exists: {fullPath}");
                return true;
            }

            var go = new GameObject(name);

            if (!string.IsNullOrEmpty(parent))
            {
                var parentGO = GameObject.Find(parent);
                if (parentGO == null)
                {
                    task.error = $"Parent GameObject not found: {parent}";
                    GameObject.DestroyImmediate(go);
                    return false;
                }

                // If parent has Canvas or RectTransform, add RectTransform to child
                if (parentGO.GetComponent<Canvas>() != null || parentGO.GetComponent<RectTransform>() != null)
                {
                    go.AddComponent<RectTransform>();
                }

                go.transform.SetParent(parentGO.transform, false);
            }

            // VERIFY: Confirm GameObject was created and is findable
            if (GameObject.Find(fullPath) == null)
            {
                task.error = $"GameObject creation verification failed: {fullPath}";
                return false;
            }

            Debug.Log($"[SceneTaskExecutor]Created GameObject: {name}" + (parent != null ? $" under {parent}" : "") + " ✓");
            return true;
        }

        // Other methods would be implemented similarly...
        // For brevity, I'm showing only a couple of methods
        // Each method should follow the same pattern:
        // 1. Get parameters using GetParam/GetOptionalParam
        // 2. Perform the operation
        // 3. Return true on success, false on failure with task.error set
        
        // Placeholder for other methods - they would be implemented similarly
        private bool LoadScene(Task task) 
        { 
            string path = GetParam(task, "path");
            
            if (!System.IO.File.Exists(path))
            {
                task.error = $"Scene file not found: {path}";
                return false;
            }
            
            currentScene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            
            // TODO: VERIFY scene is actually active and loaded
            // if (EditorSceneManager.GetActiveScene().path != path) return false;
            
            Debug.Log($"[SceneTaskExecutor]Loaded scene: {path}");
            return true;
        }
        private bool AddComponent(Task task)
        {
            string path = GetParam(task, "path");
            string component = GetParam(task, "component");

            var go = GameObject.Find(path);
            if (go == null)
            {
                task.error = $"GameObject not found: {path}";
                return false;
            }

            // Resolve the component type - search all loaded assemblies
            System.Type componentType = ResolveType(component);
            if (componentType == null)
            {
                task.error = $"Component type not found: {component}. Use fully qualified name: 'Namespace.Type, AssemblyName'";
                return false;
            }

            // Check if component already exists (idempotent)
            if (go.GetComponent(componentType) != null)
            {
                Debug.Log($"[SceneTaskExecutor]Component already exists: {component} on {path}");
                return true;
            }

            go.AddComponent(componentType);
            
            // VERIFY: Confirm component was added
            if (go.GetComponent(componentType) == null)
            {
                task.error = $"Component addition verification failed: {component} on {path}";
                return false;
            }

            Debug.Log($"[SceneTaskExecutor]Added component: {component} to {path} ✓");
            return true;
        }

        private bool SetParent(Task task)
        {
            string path = GetParam(task, "path");
            string parent = GetParam(task, "parent");
            string worldPositionStaysStr = GetOptionalParam(task, "worldPositionStays", "false");
            bool worldPositionStays = bool.Parse(worldPositionStaysStr);

            var go = GameObject.Find(path);
            if (go == null)
            {
                task.error = $"GameObject not found: {path}";
                return false;
            }

            if (string.IsNullOrEmpty(parent))
            {
                go.transform.SetParent(null, worldPositionStays);
            }
            else
            {
                var parentGo = GameObject.Find(parent);
                if (parentGo == null)
                {
                    task.error = $"Parent GameObject not found: {parent}";
                    return false;
                }
                go.transform.SetParent(parentGo.transform, worldPositionStays);
            }

            Debug.Log($"[SceneTaskExecutor] SetParent: {path} → {parent ?? "root"} ✓");
            return true;
        }

        private bool InstantiatePrefab(Task task)
        { 
            string prefabPath = GetParam(task, "prefabPath");
            string name = GetParam(task, "name");
            string parent = GetOptionalParam(task, "parent");

            // Check if already exists (idempotent)
            if (GameObject.Find(name) != null)
            {
                Debug.Log($"[SceneTaskExecutor]GameObject already exists: {name}");
                return true;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                task.error = $"Prefab not found at: {prefabPath}";
                return false;
            }

            // Use PrefabUtility to maintain prefab connection
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                task.error = $"Failed to instantiate prefab: {prefabPath}";
                return false;
            }
            instance.name = name;

            if (!string.IsNullOrEmpty(parent))
            {
                var parentGO = GameObject.Find(parent);
                if (parentGO == null)
                {
                    task.error = $"Parent GameObject not found: {parent}";
                    return false;
                }
                instance.transform.SetParent(parentGO.transform, false);
            }

            // VERIFY: Confirm instantiation succeeded
            var verifyPath = string.IsNullOrEmpty(parent) ? name : $"{parent}/{name}";
            if (GameObject.Find(verifyPath) == null)
            {
                task.error = $"Prefab instantiation verification failed: {verifyPath}";
                return false;
            }

            Debug.Log($"[SceneTaskExecutor]Instantiated prefab: {name}" + (parent != null ? $" under {parent}" : "") + " ✓");
            return true;
        }
        private bool SetPropertyUnified(Task task)
        {
            string ownerPath = GetOptionalParam(task, "owner");
            string goPath = GetOptionalParam(task, "path");
            string componentName = GetOptionalParam(task, "component");
            string fieldName = GetParam(task, "field");
            string valuePath = GetParam(task, "value");

            if (string.IsNullOrEmpty(fieldName))
            {
                task.error = "SetProperty requires 'field' parameter";
                return false;
            }

            if (valuePath == null)
            {
                task.error = "SetProperty requires 'value' parameter";
                return false;
            }

            // Case 1: Prefab asset (owner is .prefab + path + component provided)
            if (!string.IsNullOrEmpty(ownerPath) && ownerPath.EndsWith(".prefab") &&
                !string.IsNullOrEmpty(goPath) && !string.IsNullOrEmpty(componentName))
            {
                return SetPropertyOnPrefab(task, ownerPath, goPath, componentName, fieldName, valuePath);
            }

            // Case 2: Scene file (owner is .unity + path + component provided)
            if (!string.IsNullOrEmpty(ownerPath) && ownerPath.EndsWith(".unity") &&
                !string.IsNullOrEmpty(goPath) && !string.IsNullOrEmpty(componentName))
            {
                return SetPropertyOnSceneFile(task, ownerPath, goPath, componentName, fieldName, valuePath);
            }

            // Case 3: Scene GameObject (path + component provided, no owner)
            if (string.IsNullOrEmpty(ownerPath) && !string.IsNullOrEmpty(goPath) && !string.IsNullOrEmpty(componentName))
            {
                return SetPropertyOnGameObject(task, goPath, componentName, fieldName, valuePath);
            }

            // Case 4: ScriptableObject asset (owner provided, no path/component)
            if (!string.IsNullOrEmpty(ownerPath))
            {
                return SetPropertyOnScriptableObject(task, ownerPath, fieldName, valuePath);
            }

            task.error = "SetProperty requires either 'path' + 'component' (for scene GameObjects) or 'owner' (for ScriptableObject/Prefab/Scene assets)";
            return false;
        }

        private bool SetPropertyOnPrefab(Task task, string prefabPath, string goPath, string componentName, string fieldName, string valuePath)
        {
            var prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabRoot == null)
            {
                task.error = $"Prefab not found: {prefabPath}";
                return false;
            }

            // Find child by name (supports root name or child name)
            GameObject target = null;
            if (prefabRoot.name == goPath)
            {
                target = prefabRoot;
            }
            else
            {
                var allChildren = prefabRoot.GetComponentsInChildren<Transform>(true);
                foreach (var t in allChildren)
                {
                    if (t.name == goPath)
                    {
                        target = t.gameObject;
                        break;
                    }
                }
            }

            if (target == null)
            {
                task.error = $"GameObject '{goPath}' not found in prefab: {prefabPath}";
                return false;
            }

            System.Type componentType = ResolveType(componentName);
            if (componentType == null)
            {
                task.error = $"Component type not found: {componentName}";
                return false;
            }

            var component = target.GetComponent(componentType);
            if (component == null)
            {
                task.error = $"Component {componentName} not found on '{goPath}' in prefab {prefabPath}";
                return false;
            }

            var field = componentType.GetField(fieldName,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field == null)
            {
                task.error = $"Field '{fieldName}' not found on {componentName}";
                return false;
            }

            // Resolve value — for UnityObject references, search within the prefab first
            object value;
            if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
            {
                // Try to find as child GO in the same prefab
                GameObject refGo = null;
                var allChildren = prefabRoot.GetComponentsInChildren<Transform>(true);
                foreach (var t in allChildren)
                {
                    if (t.name == valuePath)
                    {
                        refGo = t.gameObject;
                        break;
                    }
                }

                if (refGo != null)
                {
                    if (field.FieldType == typeof(GameObject))
                        value = refGo;
                    else if (typeof(Component).IsAssignableFrom(field.FieldType))
                        value = refGo.GetComponent(field.FieldType);
                    else
                        value = refGo;
                }
                else
                {
                    value = AssetDatabase.LoadAssetAtPath(valuePath, field.FieldType);
                }
            }
            else
            {
                value = ConvertValue(field.FieldType, valuePath);
            }

            field.SetValue(component, value);
            EditorUtility.SetDirty(prefabRoot);
            PrefabUtility.SavePrefabAsset(prefabRoot);
            Debug.Log($"[SceneTaskExecutor] SetPropertyOnPrefab: {prefabPath}/{goPath}/{componentName}.{fieldName} = {valuePath} ✓");
            return true;
        }

        private bool SetPropertyOnSceneFile(Task task, string scenePath, string goPath, string componentName, string fieldName, string valuePath)
        {
            var previousScene = EditorSceneManager.GetActiveScene();
            string previousScenePath = previousScene.path;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var go = GameObject.Find(goPath);
            if (go == null)
            {
                if (!string.IsNullOrEmpty(previousScenePath))
                    EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
                task.error = $"GameObject not found in scene '{scenePath}': {goPath}";
                return false;
            }

            bool result = SetPropertyOnGameObject(task, goPath, componentName, fieldName, valuePath);

            if (result)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[SceneTaskExecutor] SetPropertyOnSceneFile: {scenePath}/{goPath}/{componentName}.{fieldName} = {valuePath} ✓");
            }

            if (!string.IsNullOrEmpty(previousScenePath))
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);

            return result;
        }

        private bool SetPropertyOnGameObject(Task task, string goPath, string componentName, string fieldName, string valuePath)
        {
            var go = GameObject.Find(goPath);
            if (go == null)
            {
                task.error = $"GameObject not found: {goPath}";
                return false;
            }

            // Get component type - use ResolveType to search all assemblies including Assembly-CSharp
            System.Type componentType = ResolveType(componentName);
            if (componentType == null)
            {
                task.error = $"Component type not found: {componentName}. Use fully qualified name: 'Namespace.Type, AssemblyName'";
                return false;
            }

            var component = go.GetComponent(componentType);
            if (component == null)
            {
                task.error = $"Component {componentName} not found on {goPath}";
                return false;
            }

            // Handle sprite field specially for Image component
            if (fieldName == "sprite" && component is Image image)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(valuePath);
                if (sprite == null)
                {
                    task.error = $"Sprite not found: {valuePath}";
                    return false;
                }
                image.sprite = sprite;
                EditorUtility.SetDirty(go);
                Debug.Log($"[SceneTaskExecutor] SetProperty: {goPath}/{componentName}.{fieldName} = {valuePath} ✓");
                return true;
            }

            // Handle color field
            if (fieldName == "color" && component is Graphic graphic)
            {
                var parts = valuePath.Split(',');
                if (parts.Length == 4)
                {
                    graphic.color = new Color(
                        float.Parse(parts[0]),
                        float.Parse(parts[1]),
                        float.Parse(parts[2]),
                        float.Parse(parts[3])
                    );
                    EditorUtility.SetDirty(go);
                    Debug.Log($"[SceneTaskExecutor] SetProperty: {goPath}/{componentName}.{fieldName} = {valuePath} ✓");
                    return true;
                }
            }

            // Use reflection for other fields
            var field = componentType.GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var property = componentType.GetProperty(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                object value = ConvertValue(field.FieldType, valuePath);
                field.SetValue(component, value);
                EditorUtility.SetDirty(go);
                Debug.Log($"[SceneTaskExecutor] SetProperty: {goPath}/{componentName}.{fieldName} = {valuePath} ✓");
                return true;
            }
            else if (property != null && property.CanWrite)
            {
                object value = ConvertValue(property.PropertyType, valuePath);
                property.SetValue(component, value);
                EditorUtility.SetDirty(go);
                Debug.Log($"[SceneTaskExecutor] SetProperty: {goPath}/{componentName}.{fieldName} = {valuePath} ✓");
                return true;
            }

            task.error = $"Field or property '{fieldName}' not found on {componentName}";
            return false;
        }

        private object ConvertValue(System.Type targetType, string valuePath)
        {
            if (targetType == typeof(Sprite))
                return AssetDatabase.LoadAssetAtPath<Sprite>(valuePath);
            if (targetType == typeof(bool))
                return bool.Parse(valuePath);
            if (targetType == typeof(int))
                return int.Parse(valuePath);
            if (targetType == typeof(float))
                return float.Parse(valuePath);
            if (targetType == typeof(string))
                return valuePath;
            if (targetType == typeof(Color))
            {
                var parts = valuePath.Split(',');
                return new Color(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), parts.Length > 3 ? float.Parse(parts[3]) : 1f);
            }

            if (targetType == typeof(Vector2))
            {
                var parts = valuePath.Split(',');
                return new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
            }
            if (targetType == typeof(Vector3))
            {
                var parts = valuePath.Split(',');
                return new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), parts.Length > 2 ? float.Parse(parts[2]) : 0f);
            }
            if (targetType == typeof(Vector4))
            {
                var parts = valuePath.Split(',');
                return new Vector4(float.Parse(parts[0]), float.Parse(parts[1]), parts.Length > 2 ? float.Parse(parts[2]) : 0f, parts.Length > 3 ? float.Parse(parts[3]) : 0f);
            }
            if (targetType == typeof(Vector2Int))
            {
                var parts = valuePath.Split(',');
                return new Vector2Int(int.Parse(parts[0]), int.Parse(parts[1]));
            }

            // Handle enums (e.g., TextAlignmentOptions, FontStyles)
            if (targetType.IsEnum)
                return Enum.Parse(targetType, valuePath);

            // Try finding as scene GameObject first, then as asset
            if (typeof(UnityEngine.Object).IsAssignableFrom(targetType))
            {
                // For Component types (RectTransform, TMP_Text, etc.), try scene lookup
                if (typeof(Component).IsAssignableFrom(targetType))
                {
                    var sceneGo = GameObject.Find(valuePath);
                    if (sceneGo != null)
                        return sceneGo.GetComponent(targetType);
                }

                // For GameObject type
                if (targetType == typeof(GameObject))
                {
                    var sceneGo = GameObject.Find(valuePath);
                    if (sceneGo != null)
                        return sceneGo;
                }

                // Fall back to asset loading
                return AssetDatabase.LoadAssetAtPath(valuePath, targetType);
            }

            return valuePath;
        }

        private bool SetPropertyOnScriptableObject(Task task, string ownerPath, string fieldName, string valuePath)
        {
            // Load the ScriptableObject asset
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(ownerPath);
            if (asset == null)
            {
                task.error = $"ScriptableObject not found: {ownerPath}";
                return false;
            }

            // Use reflection to get the field (check both public and private)
            var assetType = asset.GetType();
            var field = assetType.GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field == null)
            {
                task.error = $"Field '{fieldName}' not found in {assetType.Name}";
                return false;
            }

            // Handle different field types
            object valueToSet = null;

            if (field.FieldType == typeof(Sprite))
            {
                valueToSet = AssetDatabase.LoadAssetAtPath<Sprite>(valuePath);
                if (valueToSet == null)
                {
                    task.error = $"Sprite not found: {valuePath}";
                    return false;
                }
            }
            else if (field.FieldType == typeof(SceneAsset))
            {
                valueToSet = AssetDatabase.LoadAssetAtPath<SceneAsset>(valuePath);
                if (valueToSet == null)
                {
                    task.error = $"Scene not found: {valuePath}";
                    return false;
                }
            }
            else if (typeof(ScriptableObject).IsAssignableFrom(field.FieldType))
            {
                valueToSet = AssetDatabase.LoadAssetAtPath<ScriptableObject>(valuePath);
                if (valueToSet == null)
                {
                    task.error = $"ScriptableObject not found: {valuePath}";
                    return false;
                }
            }
            // Handle nested class with sceneAsset field (like SceneReference)
            else if (field.FieldType.IsClass && valuePath.EndsWith(".unity"))
            {
                var sceneAssetField = field.FieldType.GetField("sceneAsset",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (sceneAssetField != null && sceneAssetField.FieldType == typeof(SceneAsset))
                {
                    var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(valuePath);
                    if (sceneAsset == null)
                    {
                        task.error = $"Scene not found: {valuePath}";
                        return false;
                    }

                    // Create instance of the nested type and set sceneAsset
                    valueToSet = Activator.CreateInstance(field.FieldType);
                    sceneAssetField.SetValue(valueToSet, sceneAsset);

                    // Also set cached sceneName if exists
                    var sceneNameField = field.FieldType.GetField("sceneName",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (sceneNameField != null)
                    {
                        sceneNameField.SetValue(valueToSet, sceneAsset.name);
                    }
                }
                else
                {
                    task.error = $"Unsupported field type: {field.FieldType.Name}";
                    return false;
                }
            }
            else
            {
                task.error = $"Unsupported field type: {field.FieldType.Name}";
                return false;
            }

            // Set the field value
            field.SetValue(asset, valueToSet);

            // Mark as dirty and save
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            Debug.Log($"[SceneTaskExecutor]SetProperty: {fieldName} = {valuePath} ✓");
            return true;
        }
        
        private bool SetTransform(Task task) 
        { 
            string path = GetParam(task, "path");
            var go = GameObject.Find(path);
            if (go == null)
            {
                task.error = $"GameObject not found: {path}";
                return false;
            }

            string position = GetOptionalParam(task, "position");
            if (!string.IsNullOrEmpty(position))
                go.transform.localPosition = ParseVector3(position);
                
            string rotation = GetOptionalParam(task, "rotation");
            if (!string.IsNullOrEmpty(rotation))
                go.transform.localEulerAngles = ParseVector3(rotation);
                
            string scale = GetOptionalParam(task, "scale");
            if (!string.IsNullOrEmpty(scale))
                go.transform.localScale = ParseVector3(scale);
                
            string siblingIndex = GetOptionalParam(task, "siblingIndex");
            if (!string.IsNullOrEmpty(siblingIndex))
            {
                int index = int.Parse(siblingIndex);
                if (index == -1)
                    go.transform.SetAsLastSibling();
                else if (index == 0)
                    go.transform.SetAsFirstSibling();
                else
                    go.transform.SetSiblingIndex(index);
            }

            // TODO: VERIFY transform values were actually set
            // Read back and compare to ensure values match what was requested
            
            Debug.Log($"[SceneTaskExecutor]Set transform on {path}");
            return true;
        }
        
        private Vector3 ParseVector3(string value)
        {
            var parts = value.Split(',');
            if (parts.Length != 3)
                throw new Exception($"Invalid Vector3 format: {value}");
            return new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
        }
        private bool SetRectTransform(Task task) 
        { 
            string path = GetParam(task, "path");
            var go = GameObject.Find(path);
            if (go == null)
            {
                task.error = $"GameObject not found: {path}";
                return false;
            }

            var rectTransform = go.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                task.error = $"RectTransform component not found on: {path}";
                return false;
            }

            string anchorMin = GetOptionalParam(task, "anchorMin");
            string anchorMax = GetOptionalParam(task, "anchorMax");
            string offsetMin = GetOptionalParam(task, "offsetMin");
            string offsetMax = GetOptionalParam(task, "offsetMax");
            string anchoredPosition = GetOptionalParam(task, "anchoredPosition");
            string sizeDelta = GetOptionalParam(task, "sizeDelta");

            if (!string.IsNullOrEmpty(anchorMin)) rectTransform.anchorMin = ParseVector2(anchorMin);
            if (!string.IsNullOrEmpty(anchorMax)) rectTransform.anchorMax = ParseVector2(anchorMax);
            if (!string.IsNullOrEmpty(offsetMin)) rectTransform.offsetMin = ParseVector2(offsetMin);
            if (!string.IsNullOrEmpty(offsetMax)) rectTransform.offsetMax = ParseVector2(offsetMax);
            if (!string.IsNullOrEmpty(anchoredPosition)) rectTransform.anchoredPosition = ParseVector2(anchoredPosition);
            if (!string.IsNullOrEmpty(sizeDelta)) rectTransform.sizeDelta = ParseVector2(sizeDelta);

            // TODO: VERIFY RectTransform values were actually set
            // Read back and compare to ensure values match what was requested
            
            Debug.Log($"[SceneTaskExecutor]Set RectTransform on {path}");
            return true;
        }
        
        private Vector2 ParseVector2(string value)
        {
            var parts = value.Split(',');
            if (parts.Length != 2)
                throw new Exception($"Invalid Vector2 format: {value}");
            return new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
        }
        private bool SaveScene(Task task)
        {
            string path = GetParam(task, "path");
            var activeScene = EditorSceneManager.GetActiveScene();

            // Ensure directory exists
            var directory = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // Save scene to specified path (no dialog)
            bool saved = EditorSceneManager.SaveScene(activeScene, path);

            // VERIFY: Confirm save succeeded
            if (!saved)
            {
                task.error = $"Scene save failed: {path}";
                return false;
            }

            Debug.Log($"[SceneTaskExecutor]Saved scene: {path} ✓");
            return true;
        }
        private bool CreateCanvas(Task task) 
        { 
            string name = GetParam(task, "name");
            
            // Check if Canvas already exists (idempotent)
            var existing = GameObject.Find(name);
            if (existing != null)
            {
                Debug.Log($"[SceneTaskExecutor]Canvas already exists: {name}");
                return true;
            }
            
            var canvasGO = new GameObject(name);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // TODO: VERIFY Canvas was created and has all required components
            // if (GameObject.Find(name)?.GetComponent<Canvas>() == null) return false;
            
            Debug.Log($"[SceneTaskExecutor]Created Canvas: {name}");
            return true;
        }
        private bool DeleteGameObject(Task task) 
        { 
            string path = GetParam(task, "path");
            var go = GameObject.Find(path);
            if (go == null)
            {
                Debug.Log($"[SceneTaskExecutor]GameObject already deleted or not found: {path}");
                return true;
            }
            
            GameObject.DestroyImmediate(go);
            
            // VERIFY: Confirm deletion succeeded
            if (GameObject.Find(path) != null)
            {
                task.error = $"GameObject deletion verification failed: {path} still exists";
                return false;
            }
            
            Debug.Log($"[SceneTaskExecutor]Deleted GameObject: {path} ✓");
            return true;
        }
        private bool AddToBuildSettings(Task task) 
        { 
            string scenePath = GetParam(task, "scenePath");
            
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogWarning($"[SceneTaskExecutor]Scene file not found: {scenePath}. Skipping AddToBuildSettings.");
                return true;
            }
            
            // Get current build settings scenes
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            
            // Check if scene already exists (idempotent)
            bool sceneExists = scenes.Exists(s => s.path == scenePath);
            if (sceneExists)
            {
                Debug.Log($"[SceneTaskExecutor]Scene already in Build Settings: {scenePath}");
                return true;
            }
            
            // Add new scene
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            
            // TODO: VERIFY scene is actually in build settings after adding
            // Re-check EditorBuildSettings.scenes to confirm it's there
            
            Debug.Log($"[SceneTaskExecutor]Added to Build Settings: {scenePath}");
            return true;
        }
        
        private bool RenameGameObject(Task task) 
        { 
            string path = GetParam(task, "path");
            string newName = GetParam(task, "newName");
            
            var go = GameObject.Find(path);
            if (go == null)
            {
                // Check if it's already renamed (idempotent)
                var alreadyRenamed = GameObject.Find(newName);
                if (alreadyRenamed != null)
                {
                    Debug.Log($"[SceneTaskExecutor]GameObject already renamed to: {newName}");
                    return true;
                }
                task.error = $"GameObject not found: {path}";
                return false;
            }
            
            go.name = newName;
            
            // TODO: VERIFY GameObject is findable by new name and not by old name
            // if (GameObject.Find(newName) == null || GameObject.Find(path) != null) return false;
            
            Debug.Log($"[SceneTaskExecutor]Renamed GameObject: {path} → {newName}");
            return true;
        }
        
        private bool CreateScriptableObject(Task task) 
        { 
            string typeName = GetParam(task, "type");
            string path = GetParam(task, "path");
            
            // Check if asset already exists (idempotent)
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) != null)
            {
                Debug.Log($"[SceneTaskExecutor]ScriptableObject already exists: {path}");
                return true;
            }
            
            // Find the type - search all loaded assemblies
            System.Type type = null;
            
            // First try direct GetType
            type = System.Type.GetType(typeName);
            
            // If not found, search all loaded assemblies
            if (type == null)
            {
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(typeName.Split(',')[0].Trim()); // Get type name without assembly part
                    if (type != null)
                        break;
                }
            }
            
            // Validate it's a ScriptableObject
            if (type == null || !typeof(ScriptableObject).IsAssignableFrom(type))
            {
                task.error = $"ScriptableObject type not found or invalid: {typeName}";
                return false;
            }
            
            // Create the asset
            ScriptableObject asset = ScriptableObject.CreateInstance(type);
            
            // Ensure directory exists
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            // Create and save the asset
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // TODO: VERIFY ScriptableObject exists at path and is correct type
            // if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) == null) return false;
            
            Debug.Log($"[SceneTaskExecutor]Created ScriptableObject: {path} (type: {typeName})");
            return true;
        }
        
        private bool SaveAsPrefab(Task task) 
        { 
            string path = GetParam(task, "path");
            string prefabPath = GetParam(task, "prefabPath");
            
            // Find GameObject in scene
            GameObject go = GameObject.Find(path);
            if (go == null)
            {
                task.error = $"GameObject not found: {path}";
                return false;
            }
            
            // Ensure directory exists
            string directory = System.IO.Path.GetDirectoryName(prefabPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }
            
            // Check if prefab already exists
            if (System.IO.File.Exists(prefabPath))
            {
                Debug.Log($"[SceneTaskExecutor]Prefab already exists, replacing: {prefabPath}");
                AssetDatabase.DeleteAsset(prefabPath);
            }
            
            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            
            // TODO: VERIFY prefab file exists and is loadable
            // if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null) return false;
            
            Debug.Log($"[SceneTaskExecutor]Saved prefab: {prefabPath}");
            return true;
        }
        
        /// <summary>
        /// Options controlling hierarchy export/search behavior.
        /// Shared by ExportHierarchy, ExportHierarchyScene, ExportHierarchyPrefab.
        /// </summary>
        private class HierarchyExportOptions
        {
            /// <summary>Max traversal depth. -1 = unlimited. 0 = only roots.</summary>
            public int maxDepth = -1;

            /// <summary>Case-insensitive substring match on GameObject name. Null/empty = disabled.</summary>
            public string nameFilter;

            /// <summary>Component type name (short or fully-qualified). Null/empty = disabled.</summary>
            public string componentFilter;

            /// <summary>Whether to emit component list per GameObject.</summary>
            public bool includeComponents = true;

            /// <summary>True if any find-style filter is active - switches to flat output mode.</summary>
            public bool HasFilter => !string.IsNullOrEmpty(nameFilter) || !string.IsNullOrEmpty(componentFilter);
        }

        /// <summary>
        /// Parses hierarchy options from task parameters.
        /// Optional params: maxDepth, nameFilter, componentFilter, includeComponents
        /// </summary>
        private HierarchyExportOptions ParseHierarchyOptions(Task task)
        {
            var opts = new HierarchyExportOptions();

            string maxDepthStr = GetOptionalParam(task, "maxDepth");
            if (!string.IsNullOrEmpty(maxDepthStr) && int.TryParse(maxDepthStr, out int d))
                opts.maxDepth = d;

            opts.nameFilter = GetOptionalParam(task, "nameFilter");
            opts.componentFilter = GetOptionalParam(task, "componentFilter");

            string incComp = GetOptionalParam(task, "includeComponents");
            if (!string.IsNullOrEmpty(incComp) && bool.TryParse(incComp, out bool ic))
                opts.includeComponents = ic;

            return opts;
        }

        private bool ExportHierarchy(Task task)
        {
            var opts = ParseHierarchyOptions(task);
            var activeScene = EditorSceneManager.GetActiveScene();
            var rootObjects = activeScene.GetRootGameObjects();

            var output = new System.Text.StringBuilder();
            output.AppendLine($"=== Scene Hierarchy: {activeScene.name} ===");
            output.AppendLine($"Path: {activeScene.path}");
            output.AppendLine($"Root GameObjects: {rootObjects.Length}");
            AppendOptionsHeader(output, opts);
            output.AppendLine();

            int matchCount = 0;
            foreach (var root in rootObjects)
                ExportGameObjectRecursive(root, output, 0, "", opts, ref matchCount);

            if (opts.HasFilter)
                output.AppendLine($"\n=== Matches: {matchCount} ===");

            task.result = output.ToString();
            return true;
        }

        private bool ExportHierarchyScene(Task task)
        {
            string scenePath = GetParam(task, "scenePath");
            var opts = ParseHierarchyOptions(task);

            if (!System.IO.File.Exists(scenePath))
            {
                task.error = $"Scene file not found: {scenePath}";
                return false;
            }

            var previousScene = EditorSceneManager.GetActiveScene();
            string previousScenePath = previousScene.path;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var output = new System.Text.StringBuilder();
            output.AppendLine($"=== Scene Hierarchy: {scene.name} ===");
            output.AppendLine($"Path: {scenePath}");
            output.AppendLine($"Root GameObjects: {scene.GetRootGameObjects().Length}");
            AppendOptionsHeader(output, opts);
            output.AppendLine();

            int matchCount = 0;
            foreach (var root in scene.GetRootGameObjects())
                ExportGameObjectRecursive(root, output, 0, "", opts, ref matchCount);

            if (opts.HasFilter)
                output.AppendLine($"\n=== Matches: {matchCount} ===");

            task.result = output.ToString();
            Debug.Log($"[SceneTaskExecutor] ExportHierarchyScene: {scenePath} ✓");

            if (!string.IsNullOrEmpty(previousScenePath))
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);

            return true;
        }

        private bool ExportHierarchyPrefab(Task task)
        {
            string prefabPath = GetParam(task, "prefabPath");
            string outputPath = GetOptionalParam(task, "outputPath");
            var opts = ParseHierarchyOptions(task);

            GameObject prefab = null;

            // Check if prefab is currently open in Prefab Mode
            var currentStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (currentStage != null && currentStage.prefabAssetPath == prefabPath)
            {
                prefab = currentStage.prefabContentsRoot;
                Debug.Log($"[SceneTaskExecutor] Using currently open prefab stage: {prefabPath}");
            }
            else
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    task.error = $"Prefab not found at: {prefabPath}";
                    return false;
                }
            }

            var output = new System.Text.StringBuilder();
            output.AppendLine($"=== Prefab Hierarchy: {prefab.name} ===");
            output.AppendLine($"Path: {prefabPath}");
            AppendOptionsHeader(output, opts);
            output.AppendLine();

            int matchCount = 0;
            ExportGameObjectRecursive(prefab, output, 0, "", opts, ref matchCount);

            if (opts.HasFilter)
                output.AppendLine($"\n=== Matches: {matchCount} ===");

            string result = output.ToString();
            task.result = result;
            Debug.Log($"[SceneTaskExecutor] ExportHierarchyPrefab: {prefabPath} ✓ (matches: {matchCount})");

            if (!string.IsNullOrEmpty(outputPath))
            {
                System.IO.File.WriteAllText(outputPath, result);
                Debug.Log($"[SceneTaskExecutor] Saved hierarchy to: {outputPath}");
            }

            return true;
        }

        private void AppendOptionsHeader(System.Text.StringBuilder output, HierarchyExportOptions opts)
        {
            if (opts.maxDepth >= 0)
                output.AppendLine($"MaxDepth: {opts.maxDepth}");
            if (!string.IsNullOrEmpty(opts.nameFilter))
                output.AppendLine($"NameFilter: \"{opts.nameFilter}\"");
            if (!string.IsNullOrEmpty(opts.componentFilter))
                output.AppendLine($"ComponentFilter: \"{opts.componentFilter}\"");
            if (opts.HasFilter)
                output.AppendLine("Mode: FLAT (find mode - showing only matches)");
        }

        /// <summary>
        /// Core recursive walker. In tree mode emits indented hierarchy with explicit parent/path.
        /// In find mode (any filter active), emits a flat list of only matching GameObjects with full path.
        /// Respects maxDepth (-1 = unlimited, 0 = only roots).
        /// </summary>
        private void ExportGameObjectRecursive(GameObject go, System.Text.StringBuilder output, int depth, string parentPath, HierarchyExportOptions opts, ref int matchCount)
        {
            string fullPath = string.IsNullOrEmpty(parentPath) ? go.name : $"{parentPath}/{go.name}";
            string parentDisplay = string.IsNullOrEmpty(parentPath) ? "<root>" : parentPath;

            if (opts.HasFilter)
            {
                // Flat find mode - only emit matching GameObjects (but still traverse all children)
                if (MatchesFilter(go, opts))
                {
                    matchCount++;
                    output.AppendLine($"[{go.name}]  (path: {fullPath})  (parent: {parentDisplay})");
                    if (opts.includeComponents)
                        AppendComponents(output, go, "  ");
                }
            }
            else
            {
                // Tree mode
                string indent = new string(' ', depth * 2);
                output.AppendLine($"{indent}[{go.name}]  (parent: {parentDisplay})  (path: {fullPath})");
                if (opts.includeComponents)
                    AppendComponents(output, go, new string(' ', (depth + 1) * 2));
            }

            // Respect max depth
            if (opts.maxDepth >= 0 && depth >= opts.maxDepth)
                return;

            for (int i = 0; i < go.transform.childCount; i++)
                ExportGameObjectRecursive(go.transform.GetChild(i).gameObject, output, depth + 1, fullPath, opts, ref matchCount);
        }

        private void AppendComponents(System.Text.StringBuilder output, GameObject go, string indent)
        {
            var components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                output.AppendLine($"{indent}• {comp.GetType().Name}");
            }
        }

        /// <summary>
        /// True if GameObject matches all active filters (name substring AND component presence).
        /// Name match is case-insensitive substring.
        /// Component match tries ResolveType first, then falls back to short-name comparison.
        /// </summary>
        private bool MatchesFilter(GameObject go, HierarchyExportOptions opts)
        {
            if (!string.IsNullOrEmpty(opts.nameFilter))
            {
                if (go.name.IndexOf(opts.nameFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }

            if (!string.IsNullOrEmpty(opts.componentFilter))
            {
                var type = ResolveType(opts.componentFilter);
                if (type != null)
                {
                    if (go.GetComponent(type) == null)
                        return false;
                }
                else
                {
                    // Fallback: match by short type name
                    bool found = false;
                    foreach (var c in go.GetComponents<Component>())
                    {
                        if (c == null) continue;
                        if (c.GetType().Name.Equals(opts.componentFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found) return false;
                }
            }

            return true;
        }
        
        private bool InspectGameObject(Task task)
        {
            string path = GetParam(task, "path");
            string componentType = GetOptionalParam(task, "component");
            string fieldsParam = GetOptionalParam(task, "fields") ?? "all";
            
            // Find GameObject
            GameObject go = GameObject.Find(path);
            if (go == null)
            {
                task.error = $"GameObject not found: {path}";
                return false;
            }
            
            var output = new System.Text.StringBuilder();
            output.AppendLine($"=== GameObject Inspector: {go.name} ===");
            output.AppendLine($"Path: {path}");
            output.AppendLine($"Active: {go.activeInHierarchy}");
            output.AppendLine();
            
            // If component type specified, inspect that component
            if (!string.IsNullOrEmpty(componentType))
            {
                Component comp = go.GetComponent(componentType);
                if (comp == null)
                {
                    task.error = $"Component '{componentType}' not found on GameObject '{path}'";
                    return false;
                }
                
                InspectComponent(comp, output, fieldsParam);
            }
            else
            {
                // Inspect all components
                var components = go.GetComponents<Component>();
                foreach (var comp in components)
                {
                    if (comp == null) continue;
                    InspectComponent(comp, output, fieldsParam);
                    output.AppendLine();
                }
            }
            
            string result = output.ToString();
            task.result = result;
            return true;
        }
        
        private void InspectComponent(Component comp, System.Text.StringBuilder output, string fieldsFilter)
        {
            output.AppendLine($"[{comp.GetType().Name}]");

            var type = comp.GetType();
            var fields = type.GetFields(System.Reflection.BindingFlags.Public |
                                       System.Reflection.BindingFlags.NonPublic |
                                       System.Reflection.BindingFlags.Instance);

            // Parse fields filter
            string[] requestedFields = fieldsFilter.ToLower() == "all"
                ? null
                : fieldsFilter.Split(',').Select(f => f.Trim()).ToArray();

            foreach (var field in fields)
            {
                // Skip if specific fields requested and this isn't one of them
                if (requestedFields != null && !requestedFields.Contains(field.Name.ToLower()))
                    continue;

                try
                {
                    object value = field.GetValue(comp);
                    string valueStr = value == null ? "null" : value.ToString();

                    // Special formatting for Unity objects
                    if (value is UnityEngine.Object unityObj)
                    {
                        valueStr = unityObj == null ? "null" : $"{unityObj.name} ({unityObj.GetType().Name})";
                    }

                    output.AppendLine($"  {field.Name}: {valueStr}");
                }
                catch (Exception ex)
                {
                    output.AppendLine($"  {field.Name}: <error: {ex.Message}>");
                }
            }
        }

        /// <summary>
        /// Resolves a type from a fully qualified type name by searching all loaded assemblies.
        /// Type.GetType() only searches the calling assembly and mscorlib by default,
        /// so we need this helper to find types in Assembly-CSharp and other Unity assemblies.
        /// </summary>
        private System.Type ResolveType(string typeName)
        {
            // First try the standard Type.GetType
            var type = System.Type.GetType(typeName);
            if (type != null)
                return type;

            // Parse the type name to extract just the type part (before comma)
            string typeNameOnly = typeName.Split(',')[0].Trim();

            // Search all loaded assemblies
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeNameOnly);
                if (type != null)
                    return type;
            }

            // Not found in any assembly
            return null;
        }

        /// <summary>
        /// Sets a list property on a ScriptableObject.
        /// Parameters:
        ///   - owner: Path to ScriptableObject asset
        ///   - field: Field name (e.g., "sprites", "scenes")
        ///   - values: Pipe-separated asset paths for simple lists (e.g., "Assets/Scenes/A.unity|Assets/Scenes/B.unity")
        ///   - json: JSON array for complex/nested types (e.g., '[{"scene":"Assets/Scenes/A.unity","nextScenes":["Assets/Scenes/B.unity"]}]')
        ///   - elementType: (optional) Type of list elements - auto-detected if not provided
        ///                  Supported: SceneAsset, Sprite, GameObject, Texture2D, AudioClip, Material, ScriptableObject
        ///   - append: (optional) "true" to append to existing list, default replaces
        /// </summary>
        private bool SetListProperty(Task task)
        {
            string ownerPath = GetParam(task, "owner");
            string fieldName = GetParam(task, "field");
            string valuesStr = GetOptionalParam(task, "values");
            string jsonStr = GetOptionalParam(task, "json");
            string elementTypeName = GetOptionalParam(task, "elementType");
            string appendStr = GetOptionalParam(task, "append");
            bool append = appendStr?.ToLower() == "true";

            if (string.IsNullOrEmpty(valuesStr) && string.IsNullOrEmpty(jsonStr))
            {
                task.error = "SetListProperty requires either 'values' (pipe-separated) or 'json' (JSON array) parameter";
                return false;
            }

            // Load the ScriptableObject asset
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(ownerPath);
            if (asset == null)
            {
                task.error = $"ScriptableObject not found: {ownerPath}";
                return false;
            }

            // Find the field
            var assetType = asset.GetType();
            var field = assetType.GetField(fieldName,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field == null)
            {
                task.error = $"Field '{fieldName}' not found in {assetType.Name}";
                return false;
            }

            // Verify field is a list type
            if (!field.FieldType.IsGenericType || field.FieldType.GetGenericTypeDefinition() != typeof(List<>))
            {
                task.error = $"Field '{fieldName}' is not a List<T> type (found {field.FieldType.Name})";
                return false;
            }

            // Get element type from field
            System.Type elementType = field.FieldType.GetGenericArguments()[0];

            // Check if this is a complex/nested type (has fields other than simple types)
            bool isComplexType = !IsSimpleAssetType(elementType);

            // If JSON provided, use JSON parsing for complex types
            if (!string.IsNullOrEmpty(jsonStr))
            {
                return SetListPropertyFromJson(task, asset, field, elementType, jsonStr, append);
            }

            // Override element type if specified (for simple types)
            if (!string.IsNullOrEmpty(elementTypeName))
            {
                elementType = ResolveAssetType(elementTypeName);
                if (elementType == null)
                {
                    task.error = $"Unknown element type: {elementTypeName}. Supported: SceneAsset, Sprite, GameObject, Texture2D, AudioClip, Material, ScriptableObject";
                    return false;
                }
            }

            // For complex types without JSON, provide helpful error
            if (isComplexType)
            {
                task.error = $"Field '{fieldName}' has complex element type '{elementType.Name}'. Use 'json' parameter instead of 'values'.";
                return false;
            }

            // Parse values (pipe-separated) for simple types
            string[] valuePaths = valuesStr.Split('|').Select(v => v.Trim()).Where(v => !string.IsNullOrEmpty(v)).ToArray();

            // Get or create the list
            var listType = typeof(List<>).MakeGenericType(elementType);
            object list;

            if (append)
            {
                list = field.GetValue(asset);
                if (list == null)
                {
                    list = Activator.CreateInstance(listType);
                }
            }
            else
            {
                list = Activator.CreateInstance(listType);
            }

            var addMethod = listType.GetMethod("Add");

            // Load and add each asset
            int successCount = 0;
            foreach (string valuePath in valuePaths)
            {
                var loadedAsset = LoadAssetByType(valuePath, elementType);
                if (loadedAsset != null)
                {
                    addMethod.Invoke(list, new object[] { loadedAsset });
                    successCount++;
                }
                else
                {
                    Debug.LogWarning($"[SceneTaskExecutor]Asset not found: {valuePath}");
                }
            }

            // Set the field value
            field.SetValue(asset, list);

            // Mark dirty and save
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            Debug.Log($"[SceneTaskExecutor]SetListProperty: {fieldName} = {successCount}/{valuePaths.Length} assets ✓");
            task.result = $"Set {successCount} items in {fieldName}";
            return true;
        }

        /// <summary>
        /// Check if type is a simple Unity asset type (not a complex nested class)
        /// </summary>
        private bool IsSimpleAssetType(System.Type type)
        {
            return typeof(UnityEngine.Object).IsAssignableFrom(type) ||
                   type == typeof(string) ||
                   type.IsPrimitive;
        }

        /// <summary>
        /// Sets a list property from JSON for complex/nested types
        /// </summary>
        private bool SetListPropertyFromJson(Task task, ScriptableObject asset, System.Reflection.FieldInfo field,
            System.Type elementType, string jsonStr, bool append)
        {
            try
            {
                // Parse JSON array
                var jsonEntries = ParseJsonArray(jsonStr);
                if (jsonEntries == null)
                {
                    task.error = "Failed to parse JSON array. Expected format: [{...}, {...}]";
                    return false;
                }

                // Get or create the list
                var listType = typeof(List<>).MakeGenericType(elementType);
                object list;

                if (append)
                {
                    list = field.GetValue(asset);
                    if (list == null)
                    {
                        list = Activator.CreateInstance(listType);
                    }
                }
                else
                {
                    list = Activator.CreateInstance(listType);
                }

                var addMethod = listType.GetMethod("Add");
                int successCount = 0;

                // Process each JSON entry
                foreach (var jsonEntry in jsonEntries)
                {
                    var element = CreateElementFromJson(elementType, jsonEntry);
                    if (element != null)
                    {
                        addMethod.Invoke(list, new object[] { element });
                        successCount++;
                    }
                }

                // Set the field value
                field.SetValue(asset, list);

                // Mark dirty and save
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();

                Debug.Log($"[SceneTaskExecutor]SetListProperty (JSON): {field.Name} = {successCount}/{jsonEntries.Count} entries ✓");
                task.result = $"Set {successCount} items in {field.Name}";
                return true;
            }
            catch (Exception ex)
            {
                task.error = $"Error parsing JSON: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Parse a JSON array string into a list of dictionaries
        /// </summary>
        private List<Dictionary<string, object>> ParseJsonArray(string jsonStr)
        {
            // Always use manual parsing for complex nested JSON structures
            // JsonUtility cannot handle arrays of objects directly
            return ParseJsonArrayManual(jsonStr);
        }

        /// <summary>
        /// Manual JSON array parsing for complex nested structures
        /// </summary>
        private List<Dictionary<string, object>> ParseJsonArrayManual(string jsonStr)
        {
            var result = new List<Dictionary<string, object>>();
            jsonStr = jsonStr.Trim();

            if (!jsonStr.StartsWith("[") || !jsonStr.EndsWith("]"))
                return null;

            // Remove outer brackets
            jsonStr = jsonStr.Substring(1, jsonStr.Length - 2).Trim();

            if (string.IsNullOrEmpty(jsonStr))
                return result;

            // Split by objects (respecting nested braces)
            var objects = SplitJsonObjects(jsonStr);

            foreach (var obj in objects)
            {
                var dict = ParseJsonObject(obj);
                if (dict != null)
                    result.Add(dict);
            }

            return result;
        }

        /// <summary>
        /// Split JSON string into individual object strings
        /// </summary>
        private List<string> SplitJsonObjects(string jsonStr)
        {
            var objects = new List<string>();
            int braceCount = 0;
            int bracketCount = 0;
            int start = 0;
            bool inString = false;

            for (int i = 0; i < jsonStr.Length; i++)
            {
                char c = jsonStr[i];

                if (c == '"' && (i == 0 || jsonStr[i - 1] != '\\'))
                    inString = !inString;

                if (!inString)
                {
                    if (c == '{') braceCount++;
                    else if (c == '}') braceCount--;
                    else if (c == '[') bracketCount++;
                    else if (c == ']') bracketCount--;
                    else if (c == ',' && braceCount == 0 && bracketCount == 0)
                    {
                        objects.Add(jsonStr.Substring(start, i - start).Trim());
                        start = i + 1;
                    }
                }
            }

            // Add last object
            if (start < jsonStr.Length)
                objects.Add(jsonStr.Substring(start).Trim());

            return objects;
        }

        /// <summary>
        /// Parse a JSON object string into a dictionary
        /// </summary>
        private Dictionary<string, object> ParseJsonObject(string jsonStr)
        {
            var dict = new Dictionary<string, object>();
            jsonStr = jsonStr.Trim();

            if (!jsonStr.StartsWith("{") || !jsonStr.EndsWith("}"))
                return dict;

            // Remove outer braces
            jsonStr = jsonStr.Substring(1, jsonStr.Length - 2).Trim();
            // Parse key-value pairs
            int i = 0;
            while (i < jsonStr.Length)
            {
                // Skip whitespace
                while (i < jsonStr.Length && char.IsWhiteSpace(jsonStr[i])) i++;
                if (i >= jsonStr.Length) break;

                // Find key (quoted string)
                if (jsonStr[i] != '"') break;
                int keyStart = i + 1;
                i++;
                while (i < jsonStr.Length && jsonStr[i] != '"') i++;
                string key = jsonStr.Substring(keyStart, i - keyStart);
                i++; // Skip closing quote

                // Skip to colon
                while (i < jsonStr.Length && jsonStr[i] != ':') i++;
                i++; // Skip colon

                // Skip whitespace
                while (i < jsonStr.Length && char.IsWhiteSpace(jsonStr[i])) i++;

                // Parse value
                object value = null;
                if (jsonStr[i] == '"')
                {
                    // String value
                    int valueStart = i + 1;
                    i++;
                    while (i < jsonStr.Length && !(jsonStr[i] == '"' && jsonStr[i - 1] != '\\')) i++;
                    value = jsonStr.Substring(valueStart, i - valueStart);
                    i++; // Skip closing quote
                }
                else if (jsonStr[i] == '[')
                {
                    // Array value
                    int bracketCount = 1;
                    int valueStart = i;
                    i++;
                    while (i < jsonStr.Length && bracketCount > 0)
                    {
                        if (jsonStr[i] == '[') bracketCount++;
                        else if (jsonStr[i] == ']') bracketCount--;
                        i++;
                    }
                    string arrayStr = jsonStr.Substring(valueStart, i - valueStart);
                    value = ParseJsonStringArray(arrayStr);
                }
                else if (jsonStr[i] == '{')
                {
                    // Nested object
                    int braceCount = 1;
                    int valueStart = i;
                    i++;
                    while (i < jsonStr.Length && braceCount > 0)
                    {
                        if (jsonStr[i] == '{') braceCount++;
                        else if (jsonStr[i] == '}') braceCount--;
                        i++;
                    }
                    value = jsonStr.Substring(valueStart, i - valueStart);
                }
                else
                {
                    // Number or boolean
                    int valueStart = i;
                    while (i < jsonStr.Length && jsonStr[i] != ',' && jsonStr[i] != '}') i++;
                    value = jsonStr.Substring(valueStart, i - valueStart).Trim();
                }

                dict[key] = value;

                // Skip to next key or end
                while (i < jsonStr.Length && (jsonStr[i] == ',' || char.IsWhiteSpace(jsonStr[i]))) i++;
            }

            return dict;
        }

        /// <summary>
        /// Parse a JSON array of strings
        /// </summary>
        private List<string> ParseJsonStringArray(string jsonStr)
        {
            var result = new List<string>();
            jsonStr = jsonStr.Trim();

            if (!jsonStr.StartsWith("[") || !jsonStr.EndsWith("]"))
                return result;

            jsonStr = jsonStr.Substring(1, jsonStr.Length - 2).Trim();

            if (string.IsNullOrEmpty(jsonStr))
                return result;

            // Split by commas (respecting quotes)
            bool inString = false;
            int start = 0;

            for (int i = 0; i < jsonStr.Length; i++)
            {
                if (jsonStr[i] == '"' && (i == 0 || jsonStr[i - 1] != '\\'))
                    inString = !inString;
                else if (jsonStr[i] == ',' && !inString)
                {
                    string item = jsonStr.Substring(start, i - start).Trim().Trim('"');
                    if (!string.IsNullOrEmpty(item))
                        result.Add(item);
                    start = i + 1;
                }
            }

            // Add last item
            string lastItem = jsonStr.Substring(start).Trim().Trim('"');
            if (!string.IsNullOrEmpty(lastItem))
                result.Add(lastItem);

            return result;
        }

        /// <summary>
        /// Create an element instance from JSON dictionary using reflection
        /// </summary>
        private object CreateElementFromJson(System.Type elementType, Dictionary<string, object> jsonDict)
        {
            var element = Activator.CreateInstance(elementType);

            foreach (var kvp in jsonDict)
            {
                var field = elementType.GetField(kvp.Key,
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (field == null)
                    continue;

                object value = ConvertJsonValue(field.FieldType, kvp.Value);
                if (value != null)
                    field.SetValue(element, value);
            }

            return element;
        }

        /// <summary>
        /// Convert a JSON value to the target type
        /// </summary>
        private object ConvertJsonValue(System.Type targetType, object jsonValue)
        {
            if (jsonValue == null)
                return null;

            // String value - could be asset path or simple string
            if (jsonValue is string strValue)
            {
                // First check for nested type with scene asset field (like SceneReference)
                // This must come BEFORE direct asset loading check
                if (targetType.IsClass && !typeof(UnityEngine.Object).IsAssignableFrom(targetType) && targetType != typeof(string))
                {
                    // Look for sceneAsset field
                    var sceneAssetField = targetType.GetField("sceneAsset",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                    if (sceneAssetField != null && sceneAssetField.FieldType == typeof(SceneAsset))
                    {
                        var instance = Activator.CreateInstance(targetType);
                        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(strValue);
                        if (sceneAsset != null)
                        {
                            sceneAssetField.SetValue(instance, sceneAsset);

                            // Also set cached name if exists
                            var sceneNameField = targetType.GetField("sceneName",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (sceneNameField != null)
                            {
                                sceneNameField.SetValue(instance, sceneAsset.name);
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[SceneTaskExecutor] Failed to load SceneAsset: {strValue}");
                        }
                        return instance;
                    }
                }

                // Check if it's an asset path for direct Unity Object types
                if (strValue.StartsWith("Assets/") && targetType != typeof(string))
                {
                    // Try to load as asset
                    if (targetType == typeof(SceneAsset))
                        return AssetDatabase.LoadAssetAtPath<SceneAsset>(strValue);
                    if (typeof(UnityEngine.Object).IsAssignableFrom(targetType))
                        return AssetDatabase.LoadAssetAtPath(strValue, targetType);
                }

                return strValue;
            }

            // List of strings - convert to list of assets or nested objects
            if (jsonValue is List<string> strList)
            {
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var listElementType = targetType.GetGenericArguments()[0];
                    var list = Activator.CreateInstance(targetType);
                    var addMethod = targetType.GetMethod("Add");

                    foreach (var item in strList)
                    {
                        var convertedItem = ConvertJsonValue(listElementType, item);
                        if (convertedItem != null)
                        {
                            addMethod.Invoke(list, new object[] { convertedItem });
                        }
                    }

                    return list;
                }
            }

            // Nested JSON object string
            if (jsonValue is string objStr && objStr.StartsWith("{"))
            {
                var nestedDict = ParseJsonObject(objStr);
                return CreateElementFromJson(targetType, nestedDict);
            }

            return jsonValue;
        }

        [Serializable]
        private class JsonArrayWrapper
        {
            public string[] items;
        }

        /// <summary>
        /// Resolves common Unity asset types by name
        /// </summary>
        private System.Type ResolveAssetType(string typeName)
        {
            return typeName.ToLower() switch
            {
                "sceneasset" => typeof(SceneAsset),
                "sprite" => typeof(Sprite),
                "gameobject" => typeof(GameObject),
                "texture2d" => typeof(Texture2D),
                "texture" => typeof(Texture2D),
                "audioclip" => typeof(AudioClip),
                "audio" => typeof(AudioClip),
                "material" => typeof(Material),
                "scriptableobject" => typeof(ScriptableObject),
                "prefab" => typeof(GameObject),
                _ => ResolveType(typeName) // Try full type name resolution
            };
        }

        /// <summary>
        /// Loads an asset from path based on target type
        /// </summary>
        private UnityEngine.Object LoadAssetByType(string path, System.Type targetType)
        {
            // Handle SceneAsset specially - it's editor-only
            if (targetType == typeof(SceneAsset))
            {
                return AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            }

            // For other types, use generic load
            return AssetDatabase.LoadAssetAtPath(path, targetType);
        }

        /// <summary>
        /// Takes a screenshot of the current Game view.
        /// Parameters:
        ///   - outputDir: Directory to save screenshot (default: Assets/Screenshots)
        ///   - filename: Base filename without extension (default: screenshot)
        ///   - supersampling: Resolution multiplier (default: 1)
        /// Automatically appends timestamp to filename.
        /// Stores the full output path in task.result.
        /// </summary>
        private bool TakeScreenshot(Task task)
        {
            string outputDir = GetOptionalParam(task, "outputDir", "Assets/Screenshots");
            string filename = GetOptionalParam(task, "filename", "screenshot");
            string supersamplingStr = GetOptionalParam(task, "supersampling", "1");
            int supersampling = int.Parse(supersamplingStr);

            // Create output directory
            if (!System.IO.Directory.Exists(outputDir))
                System.IO.Directory.CreateDirectory(outputDir);

            // Generate filename with timestamp
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fullPath = System.IO.Path.Combine(outputDir, $"{filename}_{timestamp}.png");

            // Capture screenshot
            ScreenCapture.CaptureScreenshot(fullPath, supersampling);

            task.result = fullPath;
            Debug.Log($"[SceneTaskExecutor] Captured: {fullPath} (supersampling: {supersampling}x)");
            return true;
        }

        /// <summary>
        /// Waits for a GameObject to appear in the scene, with optional component check.
        /// Parameters:
        ///   - path: GameObject name/path to find via GameObject.Find()
        ///   - component: (optional) Fully qualified component type name to verify (e.g., "MyNamespace.MyComponent, Assembly-CSharp")
        ///   - timeout: (optional) Maximum wait time in seconds (default: 10)
        /// Returns immediately if found, otherwise polls asynchronously via EditorApplication.delayCall.
        /// Stores "Found: {path}" in task.result on success.
        /// </summary>
        private bool WaitForGameObject(Task task)
        {
            string path = GetParam(task, "path");
            string component = GetOptionalParam(task, "component");
            string timeoutStr = GetOptionalParam(task, "timeout", "10");
            float timeout = float.Parse(timeoutStr);

            // Immediate check
            var go = GameObject.Find(path);
            if (go != null && CheckComponentOnGameObject(go, component))
            {
                task.result = $"Found: {path}";
                Debug.Log($"[SceneTaskExecutor] Found immediately: {path}");
                return true;
            }

            // Start async polling
            _lastExecuteWasAsync = true;
            double startTime = EditorApplication.timeSinceStartup;

            void Poll()
            {
                if (EditorApplication.timeSinceStartup - startTime > timeout)
                {
                    CompleteAsyncTask(task, false, $"Timeout waiting for GameObject '{path}' after {timeout}s");
                    return;
                }

                var found = GameObject.Find(path);
                if (found != null && CheckComponentOnGameObject(found, component))
                {
                    task.result = $"Found: {path}";
                    Debug.Log($"[SceneTaskExecutor] Found: {path} (after {EditorApplication.timeSinceStartup - startTime:F1}s)");
                    CompleteAsyncTask(task, true);
                    return;
                }

                EditorApplication.delayCall += Poll;
            }

            EditorApplication.delayCall += Poll;
            Debug.Log($"[SceneTaskExecutor] Waiting for: {path} (timeout: {timeout}s)");
            return true;
        }

        /// <summary>
        /// Checks if a GameObject has a specific component type.
        /// Returns true if componentName is empty (no check needed) or if the component is found.
        /// </summary>
        private bool CheckComponentOnGameObject(GameObject go, string componentName)
        {
            if (string.IsNullOrEmpty(componentName))
                return true;

            var type = ResolveType(componentName);
            if (type == null)
                return false;

            return go.GetComponent(type) != null;
        }

        /// <summary>
        /// Calls a method on a component via reflection.
        /// Parameters:
        ///   - path: GameObject name/path to find via GameObject.Find()
        ///   - component: Fully qualified component type name (e.g., "MyNamespace.MyComponent, Assembly-CSharp")
        ///   - method: Method name to invoke (supports public and non-public instance methods)
        /// Stores the method return value (or "void") in task.result.
        /// </summary>
        private bool CallMethod(Task task)
        {
            string path = GetParam(task, "path");
            string componentName = GetParam(task, "component");
            string methodName = GetParam(task, "method");

            var go = GameObject.Find(path);
            if (go == null)
            {
                task.error = $"GameObject not found: {path}";
                return false;
            }

            var type = ResolveType(componentName);
            if (type == null)
            {
                task.error = $"Component type not found: {componentName}. Use fully qualified name: 'Namespace.Type, AssemblyName'";
                return false;
            }

            var component = go.GetComponent(type);
            if (component == null)
            {
                task.error = $"Component {componentName} not found on {path}";
                return false;
            }

            var method = type.GetMethod(methodName,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (method == null)
            {
                task.error = $"Method '{methodName}' not found on {componentName}";
                return false;
            }

            var result = method.Invoke(component, null);
            task.result = result?.ToString() ?? "void";
            Debug.Log($"[SceneTaskExecutor] {path}/{componentName}.{methodName}() => {task.result}");
            return true;
        }

        /// <summary>
        /// Opens a prefab in Prefab Mode for editing.
        /// Parameters:
        ///   - prefabPath: Path to the prefab asset (e.g., "Assets/Prefabs/MyPrefab.prefab")
        /// </summary>
        private bool OpenPrefab(Task task)
        {
            string prefabPath = GetParam(task, "prefabPath");

            if (!System.IO.File.Exists(prefabPath))
            {
                task.error = $"Prefab not found: {prefabPath}";
                return false;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                task.error = $"Failed to load prefab: {prefabPath}";
                return false;
            }

            var stage = UnityEditor.SceneManagement.PrefabStageUtility.OpenPrefab(prefabPath);
            if (stage == null)
            {
                task.error = $"Failed to open prefab stage: {prefabPath}";
                return false;
            }

            task.result = $"Opened prefab: {prefabPath}";
            Debug.Log($"[SceneTaskExecutor] Opened prefab: {prefabPath}");
            return true;
        }

        /// <summary>
        /// Saves the currently open prefab and closes Prefab Mode.
        /// No parameters required.
        /// </summary>
        private bool SavePrefab(Task task)
        {
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null)
            {
                task.error = "No prefab stage is currently open";
                return false;
            }

            var prefabPath = stage.prefabAssetPath;
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(stage.scene);

            task.result = $"Saved prefab: {prefabPath}";
            Debug.Log($"[SceneTaskExecutor] Saved prefab: {prefabPath}");
            return true;
        }

        /// <summary>
        /// Sets the parent of a GameObject using Transform.Find() for Prefab Mode.
        /// Parameters:
        ///   - path: GameObject name to find via GameObject.Find()
        ///   - parentPath: Relative path from prefab root (e.g., "O2_PSC_Driver/O2_PSC/CART_BASE_FRAME/Console/sus_op_ctrl/Visuals/sus_op_mesh/XiPSC_Boom_Rotation")
        /// </summary>
        private bool SetParentByTransform(Task task)
        {
            string path = GetParam(task, "path");
            string parentPath = GetParam(task, "parentPath");

            var go = GameObject.Find(path);
            if (go == null)
            {
                task.error = $"GameObject not found: {path}";
                return false;
            }

            // Check if we're in Prefab Mode
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null)
            {
                task.error = "Not in Prefab Mode - use SetParent instead";
                return false;
            }

            // Use Transform.Find() to locate parent from prefab root
            var prefabRoot = stage.prefabContentsRoot;
            Transform parentTransform = prefabRoot.transform;

            // Split the path and traverse using Transform.Find()
            string[] pathParts = parentPath.Split('/');
            foreach (string part in pathParts)
            {
                parentTransform = parentTransform.Find(part);
                if (parentTransform == null)
                {
                    task.error = $"Parent GameObject not found via Transform.Find: {parentPath} (failed at: {part})";
                    return false;
                }
            }

            go.transform.SetParent(parentTransform);
            task.result = $"Set parent via Transform.Find: {path} -> {parentPath}";
            Debug.Log($"[SceneTaskExecutor] Set parent via Transform.Find: {path} -> {parentPath}");
            return true;
        }
    }
}
