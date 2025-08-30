using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

namespace PerSpec.Editor.Coordination
{
    /// <summary>
    /// Monitors for PlayMode test completion after Unity exits Play mode
    /// Since EditorApplication.update doesn't run during Play mode, we need to check after
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeTestCompletionChecker
    {
        private static string _testResultsPath;
        
        static PlayModeTestCompletionChecker()
        {
            string projectPath = Directory.GetParent(Application.dataPath).FullName;
            _testResultsPath = Path.Combine(projectPath, "PerSpec", "TestResults");
            
            // Subscribe to play mode state changes
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            Debug.Log("[PlayModeTestCompletionChecker] Initialized");
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            Debug.Log($"[PlayModeTestCompletionChecker] Play mode state changed to: {state}");
            
            // When exiting play mode, check for test results
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                Debug.Log("[PlayModeTestCompletionChecker] Exited Play mode, checking for test results...");
                CheckForCompletedTests();
            }
        }
        
        private static void CheckForCompletedTests()
        {
            try
            {
                var dbManager = new SQLiteManager();
                
                // Get all running PlayMode test requests
                var runningRequests = dbManager.GetRunningRequests()
                    .Where(r => r.TestPlatform == "PlayMode")
                    .ToList();
                
                if (runningRequests.Count == 0)
                {
                    Debug.Log("[PlayModeTestCompletionChecker] No running PlayMode tests to check");
                    return;
                }
                
                Debug.Log($"[PlayModeTestCompletionChecker] Found {runningRequests.Count} running PlayMode test(s)");
                
                // Look for the latest test result file
                var latestResultFile = GetLatestResultFile();
                
                if (!string.IsNullOrEmpty(latestResultFile))
                {
                    Debug.Log($"[PlayModeTestCompletionChecker] Found result file: {latestResultFile}");
                    
                    // Parse the summary file if it exists
                    string summaryPath = latestResultFile.Replace(".xml", ".summary.txt");
                    if (File.Exists(summaryPath))
                    {
                        var summary = ParseSummaryFile(summaryPath);
                        
                        // Update the most recent running request
                        var requestToUpdate = runningRequests.OrderByDescending(r => r.Id).First();
                        
                        Debug.Log($"[PlayModeTestCompletionChecker] Updating request {requestToUpdate.Id} with results");
                        
                        dbManager.UpdateRequestResults(
                            requestToUpdate.Id,
                            "completed",
                            summary.TotalTests,
                            summary.PassedTests,
                            summary.FailedTests,
                            summary.SkippedTests,
                            summary.Duration
                        );
                        
                        dbManager.LogExecution(requestToUpdate.Id, "INFO", "PlayModeTestCompletionChecker", 
                            $"Test completed (detected after Play mode exit): {summary.PassedTests}/{summary.TotalTests} passed");
                        
                        Debug.Log($"[PlayModeTestCompletionChecker] Request {requestToUpdate.Id} marked as completed");
                    }
                }
                else
                {
                    Debug.Log("[PlayModeTestCompletionChecker] No test result files found in PerSpec/TestResults, checking Unity default location...");
                    
                    // Check Unity's default location and copy if found
                    var copiedFile = CopyFromUnityDefaultLocation();
                    if (!string.IsNullOrEmpty(copiedFile))
                    {
                        Debug.Log($"[PlayModeTestCompletionChecker] Copied test results from Unity default location to: {copiedFile}");
                        
                        // Parse the copied file
                        string summaryPath = copiedFile.Replace(".xml", ".summary.txt");
                        if (File.Exists(summaryPath))
                        {
                            var summary = ParseSummaryFile(summaryPath);
                            
                            // Update the most recent running request
                            var requestToUpdate = runningRequests.OrderByDescending(r => r.Id).First();
                            
                            Debug.Log($"[PlayModeTestCompletionChecker] Updating request {requestToUpdate.Id} with results from copied file");
                            
                            dbManager.UpdateRequestResults(
                                requestToUpdate.Id,
                                "completed",
                                summary.TotalTests,
                                summary.PassedTests,
                                summary.FailedTests,
                                summary.SkippedTests,
                                summary.Duration
                            );
                            
                            dbManager.LogExecution(requestToUpdate.Id, "INFO", "PlayModeTestCompletionChecker", 
                                $"Test completed (copied from Unity default): {summary.PassedTests}/{summary.TotalTests} passed");
                            
                            Debug.Log($"[PlayModeTestCompletionChecker] Request {requestToUpdate.Id} marked as completed");
                        }
                        else
                        {
                            // Parse XML file directly if no summary
                            ParseXmlAndUpdateRequest(copiedFile, runningRequests.OrderByDescending(r => r.Id).First(), dbManager);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[PlayModeTestCompletionChecker] No test results found in any location");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayModeTestCompletionChecker] Error checking for completed tests: {e.Message}");
            }
        }
        
        private static string GetLatestResultFile()
        {
            if (!Directory.Exists(_testResultsPath)) return null;
            
            var xmlFiles = Directory.GetFiles(_testResultsPath, "*.xml")
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .FirstOrDefault();
            
            return xmlFiles;
        }
        
        private static TestResultSummary ParseSummaryFile(string summaryPath)
        {
            var summary = new TestResultSummary();
            var lines = File.ReadAllLines(summaryPath);
            
            foreach (var line in lines)
            {
                if (line.Contains("Total Tests:"))
                {
                    if (int.TryParse(Regex.Match(line, @"\d+").Value, out int totalTests))
                        summary.TotalTests = totalTests;
                }
                else if (line.Contains("Passed:"))
                {
                    if (int.TryParse(Regex.Match(line, @"\d+").Value, out int passedTests))
                        summary.PassedTests = passedTests;
                }
                else if (line.Contains("Failed:"))
                {
                    if (int.TryParse(Regex.Match(line, @"\d+").Value, out int failedTests))
                        summary.FailedTests = failedTests;
                }
                else if (line.Contains("Skipped:"))
                {
                    if (int.TryParse(Regex.Match(line, @"\d+").Value, out int skippedTests))
                        summary.SkippedTests = skippedTests;
                }
                else if (line.Contains("Duration:"))
                {
                    var match = Regex.Match(line, @"[\d.]+");
                    if (match.Success)
                    {
                        if (float.TryParse(match.Value, out float duration))
                            summary.Duration = duration;
                    }
                }
            }
            
            return summary;
        }
        
        private static string CopyFromUnityDefaultLocation()
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low";
                
                // Try multiple possible locations in order of likelihood
                string[] possiblePaths = new string[]
                {
                    // Primary: Use actual company and product names from Unity settings
                    Path.Combine(appDataPath, Application.companyName, Application.productName),
                    
                    // Fallback 1: DefaultCompany with actual product name
                    Path.Combine(appDataPath, "DefaultCompany", Application.productName),
                    
                    // Fallback 2: Hardcoded for TestFramework project (backward compatibility)
                    Path.Combine(appDataPath, "DefaultCompany", "TestFramework"),
                    
                    // Fallback 3: DefaultCompany with project folder name
                    Path.Combine(appDataPath, "DefaultCompany", Path.GetFileName(Directory.GetParent(Application.dataPath).FullName))
                };
                
                string sourceFile = null;
                string foundPath = null;
                
                // Try each possible path
                foreach (var testPath in possiblePaths)
                {
                    string candidateFile = Path.Combine(testPath, "TestResults.xml");
                    if (File.Exists(candidateFile))
                    {
                        sourceFile = candidateFile;
                        foundPath = testPath;
                        Debug.Log($"[PlayModeTestCompletionChecker] Found test results at: {candidateFile}");
                        break;
                    }
                }
                
                if (sourceFile == null)
                {
                    Debug.Log($"[PlayModeTestCompletionChecker] No test results found. Searched locations:");
                    foreach (var path in possiblePaths)
                    {
                        Debug.Log($"  - {Path.Combine(path, "TestResults.xml")}");
                    }
                    return null;
                }
                
                // Ensure TestResults directory exists
                if (!Directory.Exists(_testResultsPath))
                    Directory.CreateDirectory(_testResultsPath);
                
                // Copy with timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string destFile = Path.Combine(_testResultsPath, $"TestResults_{timestamp}.xml");
                File.Copy(sourceFile, destFile, true);
                
                Debug.Log($"[PlayModeTestCompletionChecker] Copied from {sourceFile} to {destFile}");
                Debug.Log($"[PlayModeTestCompletionChecker] Company: {Application.companyName}, Product: {Application.productName}");
                return destFile;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayModeTestCompletionChecker] Error copying from Unity default location: {e.Message}");
                return null;
            }
        }
        
        private static void ParseXmlAndUpdateRequest(string xmlPath, TestRequest request, SQLiteManager dbManager)
        {
            try
            {
                var doc = System.Xml.Linq.XDocument.Load(xmlPath);
                var testRun = doc.Root;
                
                if (testRun != null)
                {
                    int totalTests = int.Parse(testRun.Attribute("total")?.Value ?? "0");
                    int passedTests = int.Parse(testRun.Attribute("passed")?.Value ?? "0");
                    int failedTests = int.Parse(testRun.Attribute("failed")?.Value ?? "0");
                    int skippedTests = int.Parse(testRun.Attribute("skipped")?.Value ?? "0");
                    float duration = float.Parse(testRun.Attribute("duration")?.Value ?? "0");
                    
                    Debug.Log($"[PlayModeTestCompletionChecker] Parsed XML - Total: {totalTests}, Passed: {passedTests}, Failed: {failedTests}");
                    
                    dbManager.UpdateRequestResults(
                        request.Id,
                        "completed",
                        totalTests,
                        passedTests,
                        failedTests,
                        skippedTests,
                        duration
                    );
                    
                    dbManager.LogExecution(request.Id, "INFO", "PlayModeTestCompletionChecker", 
                        $"Test completed (parsed from XML): {passedTests}/{totalTests} passed");
                    
                    Debug.Log($"[PlayModeTestCompletionChecker] Request {request.Id} marked as completed from XML");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayModeTestCompletionChecker] Error parsing XML file: {e.Message}");
            }
        }
        
        // Integrated into main coordinator - no longer needed as separate menu item
        // [MenuItem("Test Coordination/Debug/Check PlayMode Completion Now")]
        public static void ManualCheck()
        {
            CheckForCompletedTests();
        }
    }
}