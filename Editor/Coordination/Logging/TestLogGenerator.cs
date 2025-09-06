using UnityEngine;
using UnityEditor;

namespace PerSpec.Editor.Coordination
{
    /// <summary>
    /// Test utility to generate various log types for testing the robust logging system
    /// </summary>
    public static class TestLogGenerator
    {
        [MenuItem("Tools/PerSpec/Debug/Generate Test Logs", false, 500)]
        public static void GenerateTestLogs()
        {
            Debug.Log("[TestLogGenerator] Starting test log generation...");
            
            // Generate info logs
            Debug.Log("This is a standard info message");
            Debug.Log("Another info message with some data: " + System.DateTime.Now);
            
            // Generate warnings
            Debug.LogWarning("This is a warning message");
            Debug.LogWarning("Warning: Something might be wrong!");
            
            // Generate errors
            Debug.LogError("This is an error message");
            Debug.LogError("Critical error: Something went wrong!");
            
            // Generate exception
            try
            {
                throw new System.InvalidOperationException("This is a test exception");
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
            
            // Generate assertion
            Debug.Assert(false, "This is a test assertion failure");
            
            // Test compilation context logs
            Debug.Log("[Compilation Test] This log simulates compilation context");
            
            Debug.Log($"[TestLogGenerator] Generated test logs at {System.DateTime.Now:HH:mm:ss.fff}");
            Debug.Log($"[TestLogGenerator] RobustLogHandler Status:\n{RobustLogHandler.GetStatus()}");
        }
        
        [MenuItem("Tools/PerSpec/Debug/Test Compilation Error Simulation", false, 501)]
        public static void SimulateCompilationError()
        {
            Debug.Log("[TestLogGenerator] Simulating compilation error scenario...");
            
            // Generate logs that would appear during compilation
            Debug.LogError("Assets/TestScript.cs(42,10): error CS0103: The name 'nonExistentVariable' does not exist in the current context");
            Debug.LogError("Assets/TestScript.cs(55,5): error CS0246: The type or namespace name 'MissingType' could not be found");
            Debug.LogWarning("Assets/OldScript.cs(12,8): warning CS0618: 'ObsoleteMethod' is obsolete");
            
            Debug.Log("[TestLogGenerator] Compilation error simulation complete");
        }
        
        [MenuItem("Tools/PerSpec/Debug/Force Log Buffer Flush", false, 502)]
        public static void ForceLogFlush()
        {
            RobustLogHandler.ForceFlush();
            Debug.Log("[TestLogGenerator] Forced log buffer flush");
        }
    }
}