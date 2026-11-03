using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PerSpec.Editor.Coordination
{
    /// <summary>
    /// Drops unsaved changes on the open scenes so automated scene switches and test runs
    /// never block on Unity's modal "Scene(s) Have Been Modified" save prompt.
    /// </summary>
    public static class SceneDirtyUtility
    {
        // Internal Unity API; resolved by reflection so a missing/renamed method falls back to a reload.
        private static readonly MethodInfo ClearSceneDirtinessMethod = typeof(EditorSceneManager).GetMethod(
            "ClearSceneDirtiness", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
            null, new[] { typeof(Scene) }, null);

        public static bool AnySceneDirty()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) return true;
            return false;
        }

        /// <summary>
        /// Clears the dirty flag on every loaded scene. If Unity's internal API is unavailable,
        /// reloads the current scene setup from disk instead (untitled scenes are dropped).
        /// </summary>
        public static void DiscardUnsavedChanges(string context = null)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !AnySceneDirty()) return;

            var dirtyNames = string.Join(", ", Enumerable.Range(0, SceneManager.sceneCount)
                .Select(SceneManager.GetSceneAt).Where(s => s.isDirty)
                .Select(s => string.IsNullOrEmpty(s.path) ? "Untitled" : s.path));
            Debug.Log($"[SceneDirtyUtility] Discarding unsaved changes{(context != null ? $" before {context}" : "")}: {dirtyNames}");

            if (ClearSceneDirtinessMethod != null)
            {
                try
                {
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                    {
                        var scene = SceneManager.GetSceneAt(i);
                        if (scene.isDirty) ClearSceneDirtinessMethod.Invoke(null, new object[] { scene });
                    }
                    if (!AnySceneDirty()) return;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SceneDirtyUtility] ClearSceneDirtiness failed, reloading scenes from disk: {e.Message}");
                }
            }

            ReloadSceneSetupFromDisk();
        }

        private static void ReloadSceneSetupFromDisk()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup().Where(s => !string.IsNullOrEmpty(s.path)).ToArray();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (setup.Length == 0) return;

            if (!setup.Any(s => s.isActive && s.isLoaded))
            {
                foreach (var s in setup) s.isActive = false;
                var firstLoaded = setup.FirstOrDefault(s => s.isLoaded) ?? setup[0];
                firstLoaded.isLoaded = true;
                firstLoaded.isActive = true;
            }

            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }
}
