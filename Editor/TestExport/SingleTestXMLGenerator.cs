using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;

namespace PerSpec.Editor.TestExport
{
    /// <summary>
    /// Generates XML test results for individual test methods when Unity fails to generate proper results.
    /// This is a workaround for Unity's limitation with single test execution.
    /// </summary>
    public static class SingleTestXMLGenerator
    {
        /// <summary>
        /// Generates an NUnit-compatible XML file for a single test method that couldn't be properly executed.
        /// </summary>
        /// <param name="testName">Full name of the test method</param>
        /// <param name="platform">Test platform (EditMode or PlayMode)</param>
        /// <param name="outputPath">Optional output path. If null, uses default PerSpec/TestResults location</param>
        /// <returns>Path to the generated XML file</returns>
        public static string GenerateInconclusiveTestXML(string testName, string platform, string outputPath = null)
        {
            if (string.IsNullOrEmpty(testName))
            {
                throw new ArgumentException("Test name cannot be null or empty", nameof(testName));
            }
            
            // Generate output path if not provided
            if (string.IsNullOrEmpty(outputPath))
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var directory = Path.Combine(Application.dataPath, "..", "PerSpec", "TestResults");
                
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                outputPath = Path.Combine(directory, $"TestResults_{timestamp}.xml");
            }
            
            // Parse test name to extract class and method
            string className = "Unknown";
            string methodName = testName;
            
            var lastDot = testName.LastIndexOf('.');
            if (lastDot > 0)
            {
                className = testName.Substring(0, lastDot);
                methodName = testName.Substring(lastDot + 1);
            }
            
            // Create XML document
            var doc = new XDocument(new XDeclaration("1.0", "utf-8", "no"));
            
            var startTime = DateTime.Now;
            var endTime = startTime.AddMilliseconds(100); // Assume 100ms execution
            
            var testRun = new XElement("test-run",
                new XAttribute("id", "1"),
                new XAttribute("testcasecount", "1"),
                new XAttribute("result", "Inconclusive"),
                new XAttribute("total", "1"),
                new XAttribute("passed", "0"),
                new XAttribute("failed", "0"),
                new XAttribute("inconclusive", "1"),
                new XAttribute("skipped", "0"),
                new XAttribute("asserts", "0"),
                new XAttribute("engine-version", Application.unityVersion),
                new XAttribute("clr-version", Environment.Version.ToString()),
                new XAttribute("start-time", startTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new XAttribute("end-time", endTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new XAttribute("duration", "0.100")
            );
            
            // Add environment information
            var environment = new XElement("environment",
                new XAttribute("framework-version", Application.unityVersion),
                new XAttribute("os-version", SystemInfo.operatingSystem),
                new XAttribute("platform", Application.platform.ToString()),
                new XAttribute("cwd", Application.dataPath),
                new XAttribute("machine-name", SystemInfo.deviceName),
                new XAttribute("user", Environment.UserName),
                new XAttribute("user-domain", Environment.UserDomainName)
            );
            testRun.Add(environment);
            
            // Add test suite
            var testSuite = new XElement("test-suite",
                new XAttribute("type", "TestFixture"),
                new XAttribute("id", "1000"),
                new XAttribute("name", className),
                new XAttribute("fullname", className),
                new XAttribute("testcasecount", "1"),
                new XAttribute("result", "Inconclusive"),
                new XAttribute("start-time", startTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new XAttribute("end-time", endTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new XAttribute("duration", "0.100"),
                new XAttribute("total", "1"),
                new XAttribute("passed", "0"),
                new XAttribute("failed", "0"),
                new XAttribute("inconclusive", "1"),
                new XAttribute("skipped", "0"),
                new XAttribute("asserts", "0")
            );
            
            // Add properties
            var properties = new XElement("properties",
                new XElement("property",
                    new XAttribute("name", "platform"),
                    new XAttribute("value", platform)
                ),
                new XElement("property",
                    new XAttribute("name", "generated"),
                    new XAttribute("value", "true")
                ),
                new XElement("property",
                    new XAttribute("name", "reason"),
                    new XAttribute("value", "Unity Test Framework limitation - individual test results not available")
                )
            );
            testSuite.Add(properties);
            
            // Add test case
            var testCase = new XElement("test-case",
                new XAttribute("id", "1001"),
                new XAttribute("name", methodName),
                new XAttribute("fullname", testName),
                new XAttribute("methodname", methodName),
                new XAttribute("classname", className),
                new XAttribute("result", "Inconclusive"),
                new XAttribute("start-time", startTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new XAttribute("end-time", endTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new XAttribute("duration", "0.100"),
                new XAttribute("asserts", "0")
            );
            
            // Add reason for inconclusive result
            var reason = new XElement("reason",
                new XElement("message", 
                    new XCData($"Individual test execution completed but Unity Test Framework did not generate results. " +
                              $"This is a known limitation when running single test methods. " +
                              $"The test may have passed or failed - check Unity console for actual results."))
            );
            testCase.Add(reason);
            
            // Add output element with informational message
            var output = new XElement("output",
                new XCData($"[INFO] Test '{testName}' was executed individually on {platform} platform.\n" +
                          $"[INFO] Unity Test Framework does not provide detailed results for individual test methods.\n" +
                          $"[INFO] Please check Unity console logs for actual test outcome.\n" +
                          $"[INFO] Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
            );
            testCase.Add(output);
            
            testSuite.Add(testCase);
            testRun.Add(testSuite);
            doc.Add(testRun);
            
            // Save XML with proper formatting
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
                Encoding = Encoding.UTF8
            };
            
            using (var writer = XmlWriter.Create(outputPath, settings))
            {
                doc.Save(writer);
            }
            
            Debug.Log($"[SingleTestXMLGenerator] Generated inconclusive test result XML for '{testName}' at: {outputPath}");
            
            // Also create a summary file
            var summaryPath = Path.ChangeExtension(outputPath, ".summary.txt");
            CreateSummaryFile(summaryPath, testName, platform);
            
            return outputPath;
        }
        
        /// <summary>
        /// Generates an XML file for a single test with known result.
        /// </summary>
        public static string GenerateTestXML(string testName, bool passed, string platform, 
            string errorMessage = null, string stackTrace = null, float duration = 0.1f, string outputPath = null)
        {
            if (string.IsNullOrEmpty(testName))
            {
                throw new ArgumentException("Test name cannot be null or empty", nameof(testName));
            }
            
            // Generate output path if not provided
            if (string.IsNullOrEmpty(outputPath))
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var directory = Path.Combine(Application.dataPath, "..", "PerSpec", "TestResults");
                
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                outputPath = Path.Combine(directory, $"TestResults_{timestamp}.xml");
            }
            
            // Parse test name
            string className = "Unknown";
            string methodName = testName;
            
            var lastDot = testName.LastIndexOf('.');
            if (lastDot > 0)
            {
                className = testName.Substring(0, lastDot);
                methodName = testName.Substring(lastDot + 1);
            }
            
            // Create XML document
            var doc = new XDocument(new XDeclaration("1.0", "utf-8", "no"));
            
            var startTime = DateTime.Now.AddSeconds(-duration);
            var endTime = DateTime.Now;
            var result = passed ? "Passed" : "Failed";
            
            var testRun = new XElement("test-run",
                new XAttribute("id", "1"),
                new XAttribute("testcasecount", "1"),
                new XAttribute("result", result),
                new XAttribute("total", "1"),
                new XAttribute("passed", passed ? "1" : "0"),
                new XAttribute("failed", passed ? "0" : "1"),
                new XAttribute("inconclusive", "0"),
                new XAttribute("skipped", "0"),
                new XAttribute("asserts", "0"),
                new XAttribute("engine-version", Application.unityVersion),
                new XAttribute("clr-version", Environment.Version.ToString()),
                new XAttribute("start-time", startTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new XAttribute("end-time", endTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new XAttribute("duration", duration.ToString("F3"))
            );
            
            // Add test suite
            var testSuite = new XElement("test-suite",
                new XAttribute("type", "TestFixture"),
                new XAttribute("id", "1000"),
                new XAttribute("name", className),
                new XAttribute("fullname", className),
                new XAttribute("testcasecount", "1"),
                new XAttribute("result", result),
                new XAttribute("start-time", startTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new XAttribute("end-time", endTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new XAttribute("duration", duration.ToString("F3")),
                new XAttribute("total", "1"),
                new XAttribute("passed", passed ? "1" : "0"),
                new XAttribute("failed", passed ? "0" : "1"),
                new XAttribute("inconclusive", "0"),
                new XAttribute("skipped", "0"),
                new XAttribute("asserts", "0")
            );
            
            // Add test case
            var testCase = new XElement("test-case",
                new XAttribute("id", "1001"),
                new XAttribute("name", methodName),
                new XAttribute("fullname", testName),
                new XAttribute("methodname", methodName),
                new XAttribute("classname", className),
                new XAttribute("result", result),
                new XAttribute("start-time", startTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new XAttribute("end-time", endTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new XAttribute("duration", duration.ToString("F3")),
                new XAttribute("asserts", "0")
            );
            
            // Add failure information if test failed
            if (!passed && !string.IsNullOrEmpty(errorMessage))
            {
                var failure = new XElement("failure",
                    new XElement("message", new XCData(errorMessage ?? "Test failed")),
                    new XElement("stack-trace", new XCData(stackTrace ?? ""))
                );
                testCase.Add(failure);
            }
            
            testSuite.Add(testCase);
            testRun.Add(testSuite);
            doc.Add(testRun);
            
            // Save XML
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
                Encoding = Encoding.UTF8
            };
            
            using (var writer = XmlWriter.Create(outputPath, settings))
            {
                doc.Save(writer);
            }
            
            Debug.Log($"[SingleTestXMLGenerator] Generated test result XML for '{testName}' ({result}) at: {outputPath}");
            
            return outputPath;
        }
        
        private static void CreateSummaryFile(string summaryPath, string testName, string platform)
        {
            var summary = new StringBuilder();
            summary.AppendLine("========================================");
            summary.AppendLine("Individual Test Execution Summary");
            summary.AppendLine("========================================");
            summary.AppendLine($"Test: {testName}");
            summary.AppendLine($"Platform: {platform}");
            summary.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            summary.AppendLine();
            summary.AppendLine("Result: INCONCLUSIVE");
            summary.AppendLine();
            summary.AppendLine("Note: Unity Test Framework does not provide detailed results");
            summary.AppendLine("for individual test method execution. Please check Unity");
            summary.AppendLine("console logs for the actual test outcome.");
            summary.AppendLine();
            summary.AppendLine("This file was automatically generated as a workaround.");
            
            File.WriteAllText(summaryPath, summary.ToString());
            Debug.Log($"[SingleTestXMLGenerator] Summary saved to: {summaryPath}");
        }
    }
}