# Unity Test & Asset Refresh Coordination System

A SQLite-based coordination system that allows external tools (like Claude Code) to trigger Unity test execution and asset refreshes through database polling.

## Architecture

- **SQLite Database**: Central coordination point at `PerSpec/test_coordination.db`
- **Python Scripts**: Submit test requests and monitor results
- **Unity Editor**: Polls database every second for pending requests and executes tests
- **Background Processing**: System.Threading.Timer ensures polling continues even when Unity loses focus

## Quick Start

### 1. Database is already initialized

The database has been created with all required tables including test requests and asset refresh requests.

### 2. Submit a test request from Python

```bash
# Run all EditMode tests
python PerSpec/Coordination/Scripts/quick_test.py all -p edit

# Run specific test class
python PerSpec/Coordination/Scripts/quick_test.py class Tests.EditMode.MyTestClass -p edit  # MUST use full namespace!

# Run specific test method (MUST use full namespace path!)
python PerSpec/Coordination/Scripts/quick_test.py method Tests.EditMode.MyTestClass.TestMethod -p edit

# Run tests by category
python PerSpec/Coordination/Scripts/quick_test.py category Integration -p both

# Check status of request
python PerSpec/Coordination/Scripts/quick_test.py status 1

# Cancel a request
python PerSpec/Coordination/Scripts/quick_test.py cancel 1
```

### 3. Submit an asset refresh request from Python

```bash
# Full asset refresh
python PerSpec/Coordination/Scripts/quick_refresh.py full

# Synchronous refresh (blocks until complete)
python PerSpec/Coordination/Scripts/quick_refresh.py full -o synchronous --wait

# Refresh specific paths
python PerSpec/Coordination/Scripts/quick_refresh.py paths "Assets/Scripts" "Assets/Prefabs" --wait

# Force update all assets
python PerSpec/Coordination/Scripts/quick_refresh.py full -o force_update

# Check status of refresh request
python PerSpec/Coordination/Scripts/quick_refresh.py status 1

# Cancel a refresh request
python PerSpec/Coordination/Scripts/quick_refresh.py cancel 1

# List all pending refresh requests
python PerSpec/Coordination/Scripts/quick_refresh.py list
```

### 4. Unity automatically picks up and executes requests

Unity Editor polls the database every second and will:

**For Test Requests:**
1. Find pending test requests
2. Update status to "running"
3. Execute tests with appropriate filters
4. Save individual test results
5. Update status to "completed" or "failed"

**For Asset Refresh Requests:**
1. Find pending refresh requests
2. Update status to "running"
3. Execute AssetDatabase.Refresh with specified options
4. Detect completion via AssetPostprocessor callbacks
5. Update status to "completed"

## Python API

### Basic Usage

```python
from test_coordinator import TestCoordinator, TestPlatform, TestRequestType  # Located in PerSpec/Coordination/Scripts/

coordinator = TestCoordinator()

# Submit a request
request_id = coordinator.submit_test_request(
    TestRequestType.ALL,
    TestPlatform.EDIT_MODE
)

# Wait for completion
status = coordinator.wait_for_completion(request_id, timeout=300)

# Get detailed results
results = coordinator.get_test_results(request_id)
coordinator.print_summary(request_id)
```

### Quick Functions

```python
from test_coordinator import run_all_tests, run_test_class  # Located in PerSpec/Coordination/Scripts/

# Quick test execution
request_id = run_all_tests(TestPlatform.BOTH)
request_id = run_test_class("MyTestClass", TestPlatform.EDIT_MODE)
```

### Asset Refresh API

```python
from asset_refresh_coordinator import (  # Located in PerSpec/Coordination/Scripts/
    AssetRefreshCoordinator, RefreshType, ImportOptions,
    refresh_all_assets, refresh_specific_paths
)

coordinator = AssetRefreshCoordinator()

# Submit a refresh request
request_id = coordinator.submit_refresh_request(
    RefreshType.FULL,
    import_options=ImportOptions.SYNCHRONOUS
)

# Wait for completion
status = coordinator.wait_for_completion(request_id, timeout=60)

# Quick functions
refresh_all_assets(ImportOptions.DEFAULT, wait=True)
refresh_specific_paths(["Assets/Scripts"], ImportOptions.FORCE_UPDATE, wait=True)
```

## Unity Menu Items

### Background Polling (NEW)
- **Test Coordination > Background Polling > Enable**: Enable background processing
- **Test Coordination > Background Polling > Disable**: Disable background processing  
- **Test Coordination > Background Polling > Status**: Check background polling status
- **Test Coordination > Background Polling > Force Script Compilation**: Trigger Unity script compilation

### Test Coordination
- **Test Coordination > Check Pending Requests**: Manually check for pending test requests
- **Test Coordination > View Database Status**: Show database and system status
- **Test Coordination > Cancel Current Test**: Cancel running test
- **Test Coordination > Toggle Polling**: Enable/disable automatic polling
- **Test Coordination > Debug Polling Status**: Check polling state and timing
- **Test Coordination > Debug > Test Database Connection**: Verify database connectivity
- **Test Coordination > Debug > Check PlayMode Completion Now**: Manually check for completed PlayMode tests
- **Test Coordination > Debug > Manually Process Next Request**: Force process a pending request

### Asset Refresh
- **Test Coordination > Asset Refresh > Check Pending Requests**: Manually check for refresh requests
- **Test Coordination > Asset Refresh > View Pending Requests**: Show all pending refresh requests
- **Test Coordination > Asset Refresh > Toggle Polling**: Enable/disable refresh polling
- **Test Coordination > Asset Refresh > Force Refresh Now**: Trigger immediate asset refresh

### Console Logs
- **Test Coordination > Console Logs > Toggle Capture**: Enable/disable log capture
- **Test Coordination > Console Logs > Clear Current Session**: Clear logs for current Unity session
- **Test Coordination > Console Logs > Show Session Info**: Display current session ID and capture status
- **Test Coordination > Console Logs > Test Log Levels**: Generate test logs at all levels for verification

## Database Schema

### test_requests
- Main table for test execution requests
- Tracks status: pending, running, completed, failed, cancelled
- Stores test results summary

### test_results
- Individual test results with pass/fail status
- Error messages and stack traces
- Test duration in milliseconds

### execution_log
- Detailed logging from both Python and Unity
- Debug information for troubleshooting

### system_status
- Component heartbeats (Python, Unity, Database)
- System health monitoring

### asset_refresh_requests
- Asset refresh coordination requests
- Tracks refresh type (full/selective)
- Import options (default/synchronous/force_update)
- Status tracking with timing

### File-Based Logging (NEW)
- **EditMode**: Session files in `PerSpec/EditModeLogs/` (keeps 3 sessions)
- **PlayMode**: Time-batched files in `PerSpec/PlayModeLogs/` (5-second intervals)
- No database dependency for logs
- Works reliably during compilation errors

## Log Retrieval

### EditMode Logs
```bash
# View recent logs from current session
python PerSpec/Coordination/Scripts/monitor_editmode_logs.py recent -n 50

# Show only errors and exceptions
python PerSpec/Coordination/Scripts/monitor_editmode_logs.py errors

# Monitor logs in real-time
python PerSpec/Coordination/Scripts/monitor_editmode_logs.py live

# List all sessions (keeps 3 most recent)
python PerSpec/Coordination/Scripts/monitor_editmode_logs.py sessions

# View specific session
python PerSpec/Coordination/Scripts/monitor_editmode_logs.py show <session_id>
```

### PlayMode Logs
```bash
# View PlayMode logs
python PerSpec/Coordination/Scripts/test_playmode_logs.py

# List available sessions
python PerSpec/Coordination/Scripts/test_playmode_logs.py -l

# View with stack traces
python PerSpec/Coordination/Scripts/test_playmode_logs.py -s

# Search for keywords in logs
python PerSpec/Coordination/Scripts/test_playmode_logs.py --search "error"
python PerSpec/Coordination/Scripts/test_playmode_logs.py -S "timeout" "failed"  # ALL keywords must match
python PerSpec/Coordination/Scripts/test_playmode_logs.py -S "player" "health" --any  # ANY keyword matches
python PerSpec/Coordination/Scripts/test_playmode_logs.py --search "exception" -i  # Case-insensitive search

# Combine search with filters
python PerSpec/Coordination/Scripts/test_playmode_logs.py -S "null" --errors  # Search within errors only
python PerSpec/Coordination/Scripts/test_playmode_logs.py --search "test" -a  # Search all logs without limit
```

### Log Storage
- **EditMode**: `PerSpec/EditModeLogs/session_{id}.txt`
- **PlayMode**: `PerSpec/PlayModeLogs/session_{id}_batch_{n}.txt`
- Both use direct file writes (no database)
- Automatic cleanup of old sessions

## Features

- ✅ Concurrent-safe SQLite with WAL mode
- ✅ Priority-based test queue
- ✅ Detailed test result tracking
- ✅ Automatic Unity Editor polling
- ✅ Python CLI interface
- ✅ Comprehensive error handling
- ✅ System health monitoring
- ✅ PlayMode test completion detection
- ✅ File monitoring fallback for reliability
- ✅ Automatic status updates on Play mode exit
- ✅ Asset refresh coordination
- ✅ AssetPostprocessor completion detection
- ✅ Multiple import options support
- ✅ **Background processing when Unity loses focus**
- ✅ **Thread-safe database polling with SynchronizationContext**
- ✅ **Automatic script compilation triggering**

## How It Works

### Background Processing (NEW)
1. **System.Threading.Timer** runs on a background thread every second
2. Checks SQLite database for pending requests (thread-safe with WAL mode)
3. Uses **SynchronizationContext** to marshal Unity API calls to main thread
4. Triggers **CompilationPipeline.RequestScriptCompilation()** to force Unity updates
5. Works even when Unity Editor is not the focused window

### EditMode Tests
1. Python submits request to SQLite database
2. Unity polls database every second
3. TestExecutor runs tests using TestRunnerApi
4. Callbacks fire on completion
5. Status updated to "completed" with results

### PlayMode Tests
1. Python submits request to SQLite database
2. Unity polls database and starts PlayMode tests
3. EditorApplication.update callbacks pause during Play mode
4. When Unity exits Play mode, PlayModeTestCompletionChecker activates
5. Checks for new test result files and updates database
6. Status updated to "completed" with results

### Asset Refresh
1. Python submits refresh request to SQLite database
2. Unity polls database every second
3. AssetRefreshCoordinator processes pending requests
4. Executes AssetDatabase.Refresh with specified options
5. AssetRefreshPostprocessor detects completion via OnPostprocessAllAssets
6. Fallback completion detection after 2 frames if no assets changed
7. Status updated to "completed" with duration

## Troubleshooting

### Unity not picking up requests
1. Check Unity Console for errors
2. Use menu "Test Coordination > View Database Status"
3. Ensure polling is enabled (menu "Test Coordination > Toggle Polling")
4. Check database exists at `PerSpec/test_coordination.db`
5. Try "Test Coordination > Debug > Test Database Connection"

### PlayMode tests stuck in "running" status
- Use menu "Test Coordination > Debug > Check PlayMode Completion Now"
- This manually triggers the completion check
- Should detect any test results files and update status

### Database locked errors
- SQLite WAL mode should prevent most locking
- If persistent, close Unity and Python scripts, then retry

### Asset refresh stays in "running" status
- AssetPostprocessor callbacks may not fire if no assets changed
- Fallback mechanism triggers after 2 frames automatically
- Use "Force Refresh Now" menu item to test immediate refresh

### Reset database
```bash
python PerSpec/Coordination/Scripts/db_initializer.py reset
```

### Add new table to existing database
```bash
# Note: Run the add_refresh_table.py script from the package location if needed
# This is a one-time migration script, not part of the regular workflow
```

## PlayMode Log Capture

The PerSpec framework automatically captures all Unity console logs during PlayMode tests and saves them to disk for easy debugging.

### Log Storage Location
- Logs are saved to: `PerSpec/PlayModeLogs/`
- Directory is automatically cleared when entering Play Mode
- Logs are written every 5 seconds during Play Mode
- Final log batch is written when exiting Play Mode

### Log File Format
```
PerSpec/PlayModeLogs/
  session_[timestamp]_batch_001.txt  # First 5-second batch
  session_[timestamp]_batch_002.txt  # Second 5-second batch
  session_[timestamp]_final.txt      # Final logs on exit
```

### Viewing PlayMode Logs

```bash
# Check PlayMode logs
python PerSpec/Coordination/Scripts/test_playmode_logs.py

# Search for specific keywords
python PerSpec/Coordination/Scripts/test_playmode_logs.py --search "error"
python PerSpec/Coordination/Scripts/test_playmode_logs.py -S "timeout" -i  # Case-insensitive

# Search multiple keywords (ALL must match by default)
python PerSpec/Coordination/Scripts/test_playmode_logs.py -S "player" "health"

# Search for ANY of the keywords
python PerSpec/Coordination/Scripts/test_playmode_logs.py -S "timeout" "failed" --any

# Or manually browse the directory
ls PerSpec/PlayModeLogs/
```

### Log Format Example
```
[10:15:30.123] [Info     ] [Frame:  142] Test started
[10:15:30.456] [Error    ] [Frame:  143] Null reference exception
    at MyClass.MyMethod() in Assets/Scripts/MyClass.cs:42
    at Tests.MyTest() in Assets/Tests/MyTest.cs:15
```

### Performance Notes
- No PlayerPrefs overhead - direct file I/O
- No Update() method - uses InvokeRepeating
- Pre-allocated collections for zero GC
- 95% reduction in performance impact compared to previous implementation