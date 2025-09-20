# Changelog

All notable changes to the PerSpec Testing Framework will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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