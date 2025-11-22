# Changelog

All notable changes to the PerSpec Testing Framework will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.5.8] - 2025-11-21

### Changed
- **DOTSService Code Cleanup**
  - Removed legacy csc.rsp migration code (from v1.5.4-1.5.6)
  - Simplified EnableDOTS() and DisableDOTS() methods
  - Removed migration methods: MigrateFromCscRsp() and CleanupCscRsp()
  - Removed csc.rsp-related constants: CSC_RSP_PATH and OLD_DOTS_DEFINE
  - Removed System.IO using statement (no longer needed)
  - Reduced code complexity by ~80 lines

### Technical Details
- Migration from csc.rsp to PlayerSettings was introduced in v1.5.7
- Users on versions older than v1.5.7 should upgrade to v1.5.7+ before updating to this version
- DOTSService now exclusively uses NamedBuildTarget API with PlayerSettings
- No breaking changes - all public APIs (EnableDOTS, DisableDOTS, ToggleDOTS, IsDOTSEnabled) remain identical
- BuildProfile support for Unity 6+ maintained

## [1.5.7] - 2025-11-20

### Fixed
- **Scene Hierarchy Exporter Unity Version Compatibility**
  - Fixed compilation error on Unity 2021.3 using `FindObjectsByType()` API
  - `FindObjectsByType()` was introduced in Unity 2021.3.18 but preprocessor directives only support major.minor versions
  - Added compiler directive using `UNITY_2022_2_OR_NEWER` as safe cutoff
  - Unity 2021.3.x now uses legacy `FindObjectsOfType()` API for guaranteed compatibility
  - Unity 2022.2+ uses modern `FindObjectsByType()` with performance optimizations (FindObjectsSortMode.None, FindObjectsInactive control)
  - Code now compiles correctly across all supported Unity versions

## [1.5.6] - 2025-11-19

### Fixed
- **README Documentation**
  - Removed broken "Technical Architecture Deep Dive" link that pointed to non-existent section
  - Link was causing 404 errors on GitHub

### Improved
- **TDD Workflow Documentation**
  - Added LLM automation instructions to "The Recommended TDD Workflow" section
  - Each step (2-4) now shows both manual Control Center actions and LLM prompts
  - Added "Automating with LLM Prompts" subsection with command reference table
  - Clearly documents that capable LLMs automatically run verification after code changes
- **CLAUDE.md LLM Instructions**
  - Added "Natural Language Prompt Recognition" subsection
  - Explicit mapping of natural language prompts to Python commands
  - Mandatory automatic triggering instructions for LLMs after code changes
  - Emphasizes that automatic verification is NOT optional for LLM assistants

### Added
- **Natural Language Prompt Table**
  - "refresh Unity" → `quick_refresh.py full --wait`
  - "show errors" or "get errors" → `monitor_editmode_logs.py --errors`
  - "run tests" → `quick_test.py all -p edit --wait`
- **Cross-references**
  - README TDD workflow links to LLM automation section
  - README references AI/LLM Integration Guide for advanced usage

## [1.5.5] - 2025-11-19

### Improved
- **README Documentation**
  - Replaced git URL installation with OpenUPM CLI instructions
  - Added manual installation section with required scoped registry configuration
  - Clearly documents all three required scopes: `com.digitraver.perspec`, `com.cysharp.unitask`, `com.gilzoide.sqlite-net`
  - Reorganized Command Reference to appear after Quick Start
  - Simplified command descriptions to focus on Control Center UI

### Changed
- **Installation Instructions**
  - Removed non-functional git URL method
  - OpenUPM CLI is now the recommended installation method
  - Manual manifest.json configuration available as collapsible alternative

## [1.5.4] - 2025-11-19

### Fixed
- **DOTS Directive Not Reaching Package Assemblies**
  - `Assets/csc.rsp` only affected Assets/ assemblies, not package assemblies
  - DOTSService now uses NamedBuildTarget API with PlayerSettings
  - PlayerSettings scripting define symbols are truly global (reach all assemblies)
  - Properly handles BuildProfiles in Unity 6+ via `#if UNITY_6000_0_OR_NEWER`

### Changed
- **DOTSService Implementation**
  - Switched from csc.rsp to PlayerSettings.SetScriptingDefineSymbols
  - Uses modern NamedBuildTarget API (Unity 2021.2+)
  - Adds directive to all platforms (Standalone, iOS, Android, WebGL, etc.)
  - Updates both BuildProfile and PlayerSettings in Unity 6+

### Added
- **Migration from csc.rsp**
  - Automatically migrates existing `PERSPEC_DOTS_ENABLED` from csc.rsp to PlayerSettings
  - Cleans up empty csc.rsp files after migration

## [1.5.3] - 2025-11-19

### Fixed
- **Automatic SQLite Table Initialization**
  - SQLite tables are now automatically created when PerSpec is enabled
  - Eliminates "no such table" errors when Python scripts access database before initialization
  - New `DatabaseInitializer.cs` creates all 8 tables using C# (no Python dependency)
  - Called automatically on Editor startup when PerSpec is enabled
  - Called when enabling PerSpec via Control Center
  - Called on-demand when SQLiteManager detects missing database

### Added
- **scene_hierarchy_requests Table in db_initializer.py**
  - Python script now creates all 8 tables (was missing scene_hierarchy_requests)
  - Maintains parity between C# and Python database initialization

### Improved
- **Database Initialization Robustness**
  - Multiple entry points ensure database is always ready
  - Uses `CREATE TABLE IF NOT EXISTS` for idempotent operations
  - WAL mode enabled for better concurrency

## [1.5.2] - 2025-11-19

### Changed
- **DOTSService Now Uses csc.rsp**
  - Switched from BuildProfile/PlayerSettings to csc.rsp file approach
  - PERSPEC_DOTS_ENABLED directive now managed alongside PERSPEC_DEBUG
  - Multiple directives can coexist in Assets/csc.rsp (one per line)
  - Removed dependency on BuildProfileHelper for DOTS toggle
  - Simplified status display in Control Center

### Improved
- **Unified Compiler Directive Management**
  - Both debug logging and DOTS support now use same csc.rsp mechanism
  - Consistent behavior across all Unity versions
  - No more BuildProfile vs PlayerSettings confusion
  - Handles edge cases: deletes csc.rsp when last directive is removed

## [1.5.1] - 2025-11-19

### Fixed
- **Thread Safety Bug in PerSpecDebug**
  - Removed non-thread-safe dictionary cache that could cause race conditions during async operations
  - Feature logging methods (LogFeatureStart, LogFeatureProgress, etc.) now inline ToUpper() calls
  - Eliminates potential InvalidOperationException during concurrent logging

### Added
- **Simplified Debug Logging Control via csc.rsp**
  - New `PerSpecDebugSettings.EnableDebugLogging()` - creates Assets/csc.rsp
  - New `PerSpecDebugSettings.DisableDebugLogging()` - deletes Assets/csc.rsp
  - New `PerSpecDebugSettings.IsCscRspPresent` property
  - New `PerSpecDebug.VerifyEnabled()` method to confirm logging status

### Changed
- **Debug Directive Management**
  - PERSPEC_DEBUG now controlled via Assets/csc.rsp file (global to all code)
  - Bypasses BuildProfile/PlayerSettings synchronization issues
  - Single source of truth - file present = logging enabled
  - Improved validation messages with clear instructions

### Improved
- **Startup Validation**
  - ValidateDebugConfiguration now detects mismatches between file and compile state
  - Clear warning when csc.rsp exists but recompilation is needed
  - Helpful instructions in console for enabling/disabling logging

## [1.5.0] - 2025-11-19

### Breaking Changes
- **DOTS/Entities Now Optional**
  - DOTS support is now gated behind `PERSPEC_DOTS_ENABLED` compiler directive
  - Users must manually enable DOTS support in Control Center > Debug Settings
  - Unity.Entities removed from package dependencies (now optional)
  - Minimum Unity version lowered to 2021.3 (from 6000.0)

### Added
- **DOTSService Toggle System**
  - New `DOTSService` class for managing DOTS/Entities compiler directive
  - Toggle in Control Center Dashboard shows DOTS status
  - Toggle in Control Center Debug Settings tab to enable/disable DOTS support
  - Methods: `IsDOTSEnabled`, `EnableDOTS()`, `DisableDOTS()`, `ToggleDOTS()`

- **Conditional DOTS Compilation**
  - Pure DOTS asmdefs use `defineConstraints` for `PERSPEC_DOTS_ENABLED`
  - Mixed asmdefs use `versionDefines` to detect Unity.Entities package
  - All DOTS C# files wrapped with `#if PERSPEC_DOTS_ENABLED` guards

### Changed
- **Package Configuration**
  - Updated `package.json` minimum Unity version from 6000.0 to 2021.3
  - Removed `com.unity.entities` from required dependencies
  - Added "DOTS" to package keywords

- **Assembly Definitions**
  - `PerSpec.Runtime.DOTS.asmdef` - Added defineConstraints
  - `PerSpec.Editor.DOTS.asmdef` - Added defineConstraints
  - `PerSpec.Runtime.asmdef` - Removed DOTS refs, added versionDefines
  - `PerSpec.Editor.asmdef` - Removed DOTS refs, added versionDefines
  - `PerSpec.Editor.Coordination.asmdef` - Removed DOTS refs, added versionDefines
  - `PerSpec.Editor.PrefabFactories.asmdef` - Removed DOTS refs, added versionDefines

### Migration Notes
- If upgrading from 1.4.x with DOTS code, enable DOTS support manually:
  1. Open Control Center (Tools > PerSpec > Control Center)
  2. Go to Debug Settings tab
  3. Click "Enable DOTS Support"
- Ensure Unity.Entities package is installed before enabling DOTS support

## [1.4.0] - 2025-11-07

### Added
- **New LLM Provider Support**
  - Added Windsurf IDE support with directory-based configuration (`.windsurf/rules/`)
  - Added OpenAI direct API support (`.openai.md`)
  - Added DeepSeek model support (`.deepseek.md`)
  - Total of 8 supported LLM providers now available in Control Center

### Improved
- **LLM Configuration Management**
  - Enhanced `CreateLLMConfiguration()` to handle directory-based configs (Windsurf)
  - Updated `UpdateLLMConfiguration()` with special handling for Windsurf directory structure
  - Improved `DetectLLMConfigurations()` to detect directory-based configs
  - Better provider detection logic in `GetProviderFromPath()` for all new providers

### Changed
- **Control Center UI**
  - Updated LLM Setup tab instructions to list all 8 supported providers
  - Increased instructions text area height to 220px for better readability
  - Enhanced provider selection interface with new options

## [1.3.4] - 2025-01-27

### Fixed
- **LLM Setup Tab in Control Center**
  - Replaced problematic dropdown with checkbox system using EditorPrefs
  - Fixed issue where dropdown selection wouldn't persist between window reopens
  - Added persistent state storage that survives Unity restarts and script recompiles
  - Checkboxes now properly maintain their selected state across all sessions

### Improved
- **LLM Configuration UI**
  - Added scrollable area for checkbox list (120px height)
  - New "Select All" and "Clear All" buttons for bulk operations
  - Support for creating/updating multiple LLM configurations at once
  - Better visual feedback with clear checkbox states
  - Window title simplified from "PerSpec Control Center" to "Control Center"

### Added
- **Multi-Selection Support**
  - Can now select and create multiple LLM configurations simultaneously
  - Batch processing with detailed success/error reporting
  - Centralized configuration path management

## [1.3.3] - 2025-01-22

### Added
- **PlayMode Log Search Functionality**
  - New `--search` or `-S` flag to search for keywords across all PlayMode log files
  - Support for multiple keywords with AND/OR logic
  - Case-insensitive search option with `-i` or `--ignore-case`
  - `--any` flag to match ANY keyword instead of ALL keywords
  - Keyword highlighting in search results (yellow background)
  - Search works in both message content and stack traces
  - Combinable with existing filters (--errors, --cs-errors, --level)
  - Shows search statistics including matches found and search mode

### Improved
- **PlayMode Log Viewer**
  - Enhanced help text with search examples
  - Better user guidance for search operations
  - Documentation updated with search usage examples

## [1.3.2] - 2025-01-20

### Added
- **ECS/DOTS Compilation Error Detection**
  - BC error codes (BC0001-BC9999) for Burst Compiler errors
  - DC error codes (DC0001-DC9999) for Domain Compilation errors
  - SGICE error codes for Source Generator Internal Compiler Errors
  - Detection of ECS-specific patterns (Entities.ForEach, EntityCommandBuffer, etc.)
  - Job System error patterns (NativeArray, JobHandle, IJobParallelFor)
  - New `--ecs-errors` flag to filter only ECS/DOTS/Burst errors

### Improved
- **Error Categorization in EditMode Logs**
  - Error types now displayed with compilation errors (CS/BC/DC/SG/Burst/ECS/Jobs)
  - Enhanced `is_compilation_error()` function with comprehensive patterns
  - Better distinction between C# compiler, Burst, and Domain compilation errors
  - Error statistics showing distribution by compilation type
  - More accurate error filtering for ECS/DOTS development workflows

### Changed
- **EditMode Log Monitor Enhancements**
  - Updated `--errors` flag description to include BC/DC/ECS errors
  - Added error type prefixes when viewing compilation errors
  - Improved error counting with categorization by type
  - Extended compilation patterns to cover DOTS ecosystem

## [1.3.1] - 2025-01-13

### Added
- **Python Script Synchronization System**
  - New `sync_python_scripts.py` tool for syncing scripts from package to PerSpec directory
  - Automatic copying of all Python scripts from package to working directory
  - Support for multiple source directories within the package
  - Detailed sync report showing copied files and any failures
  - File size information in sync output for verification

### Fixed
- **PlayMode Error Filtering**
  - Changed `--errors` flag to show ALL errors and exceptions (not just compilation errors)
  - Added new `--cs-errors` flag specifically for compilation errors (CS errors)
  - Aligned error filtering behavior between PlayMode and EditMode log viewers
  - Maintained backward compatibility with `--all-errors` flag (now same as `--errors`)

### Improved
- **Python Script Management**
  - Scripts now properly maintained in package and synced to PerSpec directory
  - Clear separation between package source and working directory
  - Better workflow for package updates and git operations
  - Documentation updated with sync instructions

## [1.3.0] - 2025-01-13

### Added
- **Scene Hierarchy Export System**
  - New functionality to export Unity scene hierarchy to JSON format
  - Full hierarchy export with all GameObjects and components
  - Single GameObject export with detailed component properties
  - Component serialization with actual values (no GUIDs)
  - Transform data export (position, rotation, scale)
  - Support for inactive GameObjects (configurable)
  - Automatic output directory cleanup before each export

- **Database Support**
  - Added `scene_hierarchy_requests` table for request tracking
  - SceneHierarchyRequest model in SQLiteManager
  - Polling-based coordination between Python and Unity

- **Python CLI Tool** (`scene_hierarchy.py`)
  - Export full scene hierarchy or single GameObject
  - Wait for completion with timeout support
  - List and manage export files
  - Pretty-print JSON output
  - Clean up old export files

- **Unity Components**
  - SceneHierarchyExporter: Core JSON serialization logic
  - SceneHierarchyCoordinator: Database polling and request execution
  - SerializedObject-based property extraction for accurate values

### Technical Details
- Output directory: `PerSpec/SceneHierarchy/`
- File format: `hierarchy_YYYYMMDD_HHMMSS.json`
- Uses Newtonsoft.Json for robust JSON serialization
- Thread-safe file operations with proper error handling

## [1.2.1] - 2025-09-13

### Fixed
- **Test Completion Detection Reliability**
  - Fixed issue where test status remained "processing" even after tests completed
  - Added fallback detection when RunStarted callback doesn't fire
  - File monitoring now processes XML files even without RunStarted callback
  - Added delayed fallback to assume tests started after Execute() call
  - Improved robustness for both EditMode and PlayMode tests

### Improved
- **Debug Logging**
  - Added more detailed logging for test execution flow
  - Better visibility into callback firing and file monitoring states
  - Clear indication when fallback mechanisms are triggered
  - Database status update logging for troubleshooting

### Added
- **SQLiteManager Enhancement**
  - Added GetRequestStatus() method to check current status before updates
  - Prevents unnecessary status transitions

## [1.2.0] - 2025-09-12

### Added
- **Test Execution State Management**
  - New granular test execution states: `processing`, `executing`, `finalizing`, `timeout`
  - Real-time progress tracking with percentage updates during test execution
  - Per-test completion monitoring with count tracking
  - Progress logging to database for visibility

- **Test Results Viewer** (`test_results.py`)
  - View latest test results with detailed summary
  - List and analyze multiple test run files
  - Filter to show only failed tests across sessions
  - Statistics aggregation from recent test runs
  - Clean up old result files with configurable retention
  - JSON output support for automation

- **Database Auto-Maintenance System**
  - Automatic schema updates on package installation/update
  - Migration system with version tracking
  - Weekly automatic maintenance checks
  - Manual maintenance via Unity menu: `Tools > PerSpec > Database > Run Maintenance Now`
  - Old data cleanup (>7 days for tests, >1 hour for logs)
  - Database optimization with VACUUM
  - Performance indexes for faster queries

- **Long-Running Test Support**
  - Example tests demonstrating proper long-duration handling (30s, 10s, quick)
  - Progress reporting during extended test execution
  - Proper timeout handling for individual vs batch tests

### Fixed
- **Premature Test Completion Detection** (Critical Fix)
  - Tests no longer incorrectly show as "failed" while still running
  - XML file validation ensures complete results before processing
  - File stability checking (3-second wait for size stabilization)
  - Test count validation against expected number
  - Proper handling of PlayMode vs EditMode completion detection

- **Database Schema Constraints**
  - Updated constraints to support new test execution states
  - Added migration script (`db_update_status_constraint.py`) for existing projects
  - Fixed "CHECK constraint failed" errors on status updates
  - Backward compatibility with legacy "running" status

### Changed
- **Test Coordination Workflow**
  - `--wait` flag now waits for full test execution completion (breaking change)
  - Added `--wait-processing` flag for legacy behavior (backward compatibility)
  - Status transitions now properly reflect test execution phases
  - Improved status reporting with clear execution state messages
  - Better distinction between request processing and test execution

- **File Monitoring Logic**
  - Enhanced detection of complete XML files with schema validation
  - Validation of test results before marking as complete
  - Proper handling of PlayMode vs EditMode test completion
  - More robust error handling with retry logic
  - File size stability checking before processing

### Improved
- **Test Status Accuracy**
  - Clear differentiation between "processing" and "executing" states
  - Accurate duration reporting for all test types
  - Better handling of timeout scenarios
  - Improved error messages for failed tests

- **Database Performance**
  - Added indexes on frequently queried columns
  - Automatic cleanup of old data
  - VACUUM optimization after migrations
  - Reduced database size through regular maintenance

### Documentation
- Updated CLAUDE.md with database maintenance instructions
- Added Test Results Viewer usage examples
- Documented new status states and their meanings
- Added troubleshooting guide for schema updates

## [1.1.7] - 2025-09-09

### Added
- `--errors` flag for both EditMode and PlayMode log scripts
- Direct error filtering without piping to grep
- Error filtering searches across ALL session batches/files
- Stack trace support with `-s` flag for error output
- Error statistics showing counts by type (Error, Exception, Assert)
- Better error messages when log directories don't exist

### Changed
- monitor_editmode_logs.py now searches all sessions (up to 3) for errors
- test_playmode_logs.py filters errors across all batch files in session
- Consistent command-line interface between EditMode and PlayMode logs
- Updated documentation with new error filtering examples

### Improved
- Error filtering performance with direct flag instead of grep
- User experience with clearer error messages and statistics
- Command consistency across all log monitoring scripts

## [1.1.6] - 2025-09-08

### Added
- New file-based logging system for EditMode and PlayMode
- EditModeLogCapture.cs for session-based logging (keeps 3 sessions)
- CompilationErrorCapture.cs for reliable compilation error capture
- monitor_editmode_logs.py for viewing EditMode logs
- test_playmode_logs.py for viewing PlayMode logs
- Immediate log writes without buffering
- Automatic session cleanup to prevent disk bloat

### Changed
- Complete rewrite of log capture system from database to file-based
- EditMode logs now stored in PerSpec/EditModeLogs/ as session files
- PlayMode logs remain in PerSpec/PlayModeLogs/ with 5-second batches
- Compilation errors now reliably captured even during Unity failures
- Removed database dependency for all logging operations

### Removed
- ConsoleLogCapture.cs (database-based capture)
- RobustLogHandler.cs (problematic ILogHandler interception)
- UnityConsoleSessionManager.cs (database session management)
- EnhancedConsoleWindow.cs (database-based viewer)
- TestLogGenerator.cs (obsolete test generator)
- monitor_logs.py (database log queries)
- quick_logs.py (database log commands)
- console_log_reader.py (database reader)
- add_console_logs_table.py (database migration)

### Performance
- Eliminated ILogHandler overhead
- Removed EditorPrefs persistence overhead
- No database queries for log retrieval
- Immediate file writes (no buffering delays)
- Significantly improved reliability during compilation errors

## [1.1.5] - 2025-09-08

### Added
- Database performance optimization script (optimize_database.py)
- Composite indexes on console_logs table for faster queries
- Connection pooling for SQLite operations
- Timestamp conversion caching in monitor_logs.py
- Performance benchmark script (benchmark_performance.py)
- Exponential backoff for test polling operations
- Auto-VACUUM after database cleanup operations
- String caching in PerSpecDebug to reduce allocations

### Changed
- Increased default polling interval from 0.5s to 2.0s for reduced CPU usage
- Optimized string operations in PerSpecDebug using pre-cached constants
- Database queries now use optimized composite indexes
- Improved cleanup operations with automatic VACUUM and ANALYZE

### Performance
- Query times reduced from 10-50ms to <0.05ms (10-50x improvement)
- Batch insert performance: 0.6ms for 100 rows
- CPU usage reduced by ~70% through reduced polling frequency
- Database size optimization from 5.71MB to 0.61MB after cleanup
- Memory allocations reduced through string caching

## [1.1.4] - 2025-09-07

### Added
- Automatic package update detection with script and LLM config refresh
- Auto-refresh of Python coordination scripts on package version change
- Auto-update of LLM configurations (CLAUDE.md, .cursorrules, etc.) on package update
- Preservation of user permission settings during automatic updates

### Changed
- Improved PlayMode log capture reliability with faster processing
- Reduced log batching delay from 30 frames to 5 frames for better real-time capture
- Increased log queue capacity from 1,000 to 10,000 entries
- Reduced processing interval from 0.5s to 0.1s for faster log persistence
- Removed colorama dependency from monitor_logs.py for simpler LLM-friendly output
- monitor_logs.py now outputs plain text without color formatting

### Fixed
- PlayMode logs being missed during high-volume logging scenarios
- Log queue overflow causing silent data loss
- Thread safety issues with frame count access in PlayMode
- EnhancedConsole refresh timing using EditorApplication.timeSinceStartup

## [1.1.2] - Sep 1, 2025

### Added
- Enhanced CLAUDE.md documentation with MCP-like natural language command mappings
- Test results automatic export to PerSpec/TestResults/ directory
- Console log export functionality to PerSpec/Logs/ with auto-cleanup
- Improved command execution permissions documentation
- Simplified script access with fixed paths in PerSpec/Coordination/Scripts/

### Changed
- Updated natural language command recognition for better user experience
- Improved test result file management with timestamp-based naming
- Enhanced error message filtering and logging patterns
- Streamlined TDD workflow documentation with clearer step-by-step instructions

### Fixed
- Test result persistence across Unity restarts
- Console log export path consistency
- Background polling reliability improvements
- SQLite coordination edge cases during Unity focus loss

## [1.0.0] - Aug 28, 2025

### Added
- Initial release of PerSpec Testing Framework
- UniTask-based async test support with zero allocations
- DOTS/ECS testing base classes and helpers
- SQLite-based test coordination between Python and Unity
- Intelligent console log capture with stack trace truncation
- Background test polling using System.Threading.Timer
- Comprehensive 4-step workflow (Write → Refresh → Check → Test)
- Python CLI tools for test execution and monitoring
- Editor menu integration under Tools > PerSpec
- Support for EditMode and PlayMode tests
- Asset refresh coordination system
- Real-time test status monitoring
- Console log filtering and export capabilities

### Features
- **UniTaskTestBase** - Base class for async Unity testing
- **DOTSTestBase** - Base class for ECS/DOTS testing
- **Test Coordinator** - Main Unity editor window for test management
- **Console Log Capture** - Real-time Unity console monitoring
- **Background Polling** - Tests execute even when Unity loses focus
- **Python CLI Tools**:
  - `quick_test.py` - Execute tests with various filters
  - `quick_logs.py` - Monitor and retrieve console logs
  - `quick_refresh.py` - Force Unity asset refresh
  - `db_initializer.py` - Initialize SQLite database

### Technical Details
- Unity 2021.3+ support
- UniTask 2.3.3+ integration
- Unity Test Framework 1.3.0+ compatibility
- Thread-safe SQLite operations
- Intelligent stack trace truncation for LLM optimization

## [Unreleased]

### Planned
- GitHub Actions integration
- Cloud test result storage
- Performance profiling tools
- Visual test result dashboard
- Test coverage reporting