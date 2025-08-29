# Test Coordination Agent

> **Purpose**: Automate Unity test execution through SQLite database coordination between Python and Unity Editor.

## Capabilities

This agent specializes in:
- Setting up and managing SQLite test coordination systems
- Submitting test requests via Python scripts
- Monitoring test execution and results
- Troubleshooting PlayMode test completion issues
- Handling database synchronization between Python and Unity
- Background processing when Unity loses focus
- Asset refresh coordination

## Key Features

### Database Coordination
- SQLite database at `PerSpec/test_coordination.db`
- WAL mode for concurrent access
- Automatic polling every second in Unity
- Status tracking: pending → running → completed/failed
- **Background polling with System.Threading.Timer** (works when Unity loses focus)

### Test Execution
- **EditMode Tests**: Direct execution with callback-based completion
- **PlayMode Tests**: File monitoring with post-Play mode detection
- Priority-based queue system
- Detailed result tracking

### Python Interface
```python
from Packages.com.perspec.framework.ScriptingTools.Coordination.Scripts.test_coordinator import TestCoordinator, TestPlatform, TestRequestType

coordinator = TestCoordinator()
request_id = coordinator.submit_test_request(
    TestRequestType.ALL,
    TestPlatform.PLAY_MODE
)
status = coordinator.wait_for_completion(request_id, timeout=60)
coordinator.print_summary(request_id)
```

### Quick Commands
```bash
# Run all PlayMode tests
python Packages/com.digitraver.perspec/ScriptingTools/Coordination/Scripts/quick_test.py all -p play --wait

# Check status
python Packages/com.digitraver.perspec/ScriptingTools/Coordination/Scripts/quick_test.py status

# Run specific test class
python Packages/com.digitraver.perspec/ScriptingTools/Coordination/Scripts/quick_test.py class MyTestClass -p edit
```

## Unity Components

### TestCoordinatorEditor
- Polls database every second
- Processes pending requests
- Manages test execution flow
- **Background timer for unfocused processing**

### BackgroundPoller (NEW)
- System.Threading.Timer for true background operation
- SynchronizationContext for thread marshalling
- Forces script compilation with CompilationPipeline
- Works when Unity loses focus

### TestExecutor
- Dual-detection system (callbacks + file monitoring)
- Handles both EditMode and PlayMode tests
- Automatic fallback mechanisms

### PlayModeTestCompletionChecker
- Monitors Play mode state changes
- Detects test completion after exiting Play mode
- Updates database with results

### AssetRefreshCoordinator (NEW)
- Processes asset refresh requests
- Uses AssetDatabase.Refresh with options
- Background timer support
- Completion detection via AssetPostprocessor

## Common Issues & Solutions

### Unity Not Processing When Unfocused
**Issue**: EditorApplication.update stops when Unity loses focus

**Solution**:
1. BackgroundPoller.cs automatically handles this
2. System.Threading.Timer runs on background thread
3. Menu: "Test Coordination > Background Polling > Enable"
4. Forces compilation with CompilationPipeline.RequestScriptCompilation()

### PlayMode Tests Not Completing
**Issue**: Tests stuck in "running" status after PlayMode execution

**Solution**:
1. EditorApplication.update callbacks don't run during Play mode
2. PlayModeTestCompletionChecker detects completion on Play mode exit
3. Use "Test Coordination > Debug > Check PlayMode Completion Now" for manual check

### Database Connection Issues
**Issue**: Unity not picking up pending requests

**Solution**:
1. Check Unity Console for SQLiteManager errors
2. Use "Test Coordination > Debug > Test Database Connection"
3. Ensure polling is enabled via "Test Coordination > Toggle Polling"
4. Verify database exists at `PerSpec/test_coordination.db`

### File Monitoring Not Working
**Issue**: Test results generated but status not updated

**Solution**:
1. File monitoring requires Unity to be in Edit mode
2. Check `TestResults/` directory for XML files
3. Ensure TestExecutor has proper file system permissions
4. Manual trigger: `PlayModeTestCompletionChecker.ManualCheck()`

## Database Schema

### test_requests
```sql
id INTEGER PRIMARY KEY
request_type TEXT -- 'all', 'class', 'method', 'category'
test_filter TEXT
test_platform TEXT -- 'EditMode', 'PlayMode', 'Both'
status TEXT -- 'pending', 'running', 'completed', 'failed'
priority INTEGER
created_at TIMESTAMP
started_at TIMESTAMP
completed_at TIMESTAMP
total_tests INTEGER
passed_tests INTEGER
failed_tests INTEGER
duration_seconds REAL
```

### test_results
```sql
id INTEGER PRIMARY KEY
request_id INTEGER
test_name TEXT
result TEXT -- 'Passed', 'Failed', 'Skipped'
duration_ms REAL
error_message TEXT
stack_trace TEXT
```

## Debug Menu Items

- **Test Coordination > Check Pending Requests**: Process pending requests
- **Test Coordination > View Database Status**: Database health check
- **Test Coordination > Debug Polling Status**: Check polling state
- **Test Coordination > Debug > Check PlayMode Completion Now**: Manual completion check
- **Test Coordination > Debug > Test Database Connection**: Verify connectivity
- **Test Coordination > Background Polling > Enable/Disable**: Control background processing
- **Test Coordination > Background Polling > Status**: Check background timer status
- **Test Coordination > Background Polling > Force Script Compilation**: Trigger compilation

## Implementation Notes

### Threading Considerations
- Unity API calls must stay on main thread
- SQLite operations are thread-safe with WAL mode
- File monitoring uses EditorApplication.update
- **Background polling uses System.Threading.Timer**
- **SynchronizationContext marshals to main thread**
- **CompilationPipeline forces Unity updates**

### Performance
- 1-second polling interval (adjustable)
- File checks every 2 seconds during monitoring
- 5-minute timeout for long-running tests

### Error Handling
- Comprehensive try-catch blocks in all callbacks
- Fallback mechanisms for failed callbacks
- Detailed execution logging in database

## Best Practices

1. **Always wait for PlayMode tests to exit Play mode** for status updates
2. **Check Unity Console** for [TestExecutor-FM] logs when debugging
3. **Use debug menu items** for manual intervention when needed
4. **Monitor TestResults directory** for generated XML files
5. **Keep database clean** - reset if corruption suspected

## Example Workflow

```bash
# 1. Initialize database (if needed)
python Packages/com.digitraver.perspec/ScriptingTools/Coordination/Scripts/db_initializer.py

# 2. Submit test request
python Packages/com.digitraver.perspec/ScriptingTools/Coordination/Scripts/quick_test.py all -p play --wait

# 3. Unity automatically:
#    - Picks up request
#    - Runs tests
#    - Updates status on completion

# 4. Check results
python Packages/com.digitraver.perspec/ScriptingTools/Coordination/Scripts/quick_test.py status 1
```

## Related Files

- `Packages/com.digitraver.perspec/ScriptingTools/Coordination/Scripts/test_coordinator.py` - Python interface
- `Packages/com.perspec.framework/Editor/Coordination/Windows/TestCoordinatorEditor.cs` - Unity polling
- `Packages/com.perspec.framework/Editor/Coordination/Core/TestExecutor.cs` - Test execution
- `Packages/com.perspec.framework/Editor/Coordination/TestExecution/PlayModeTestCompletionChecker.cs` - PlayMode detection
- `Packages/com.perspec.framework/Editor/Coordination/Core/SQLiteManager.cs` - Database operations
- `Packages/com.perspec.framework/Editor/Coordination/Core/BackgroundPoller.cs` - **Background processing (NEW)**
- `Packages/com.perspec.framework/Editor/Coordination/AssetManagement/AssetRefreshCoordinator.cs` - Asset refresh handling
- `Packages/com.digitraver.perspec/ScriptingTools/Coordination/Scripts/asset_refresh_coordinator.py` - Python refresh interface