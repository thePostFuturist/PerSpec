# Changelog

All notable changes to the PerSpec Testing Framework will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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