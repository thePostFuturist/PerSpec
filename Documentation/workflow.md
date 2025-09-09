# The 4-Step PerSpec Workflow

## Overview

The PerSpec workflow ensures code quality through a consistent 4-step process:

1. **Write** - Create code and tests
2. **Refresh** - Update Unity assets
3. **Check** - Verify compilation
4. **Test** - Execute tests

## Step 1: Write Code/Tests

Write your implementation and tests using PerSpec base classes:

```csharp
using PerSpec.Runtime.Unity;

public class FeatureTests : UniTaskTestBase
{
    [UnityTest]
    public IEnumerator TestFeature() => UniTask.ToCoroutine(async () =>
    {
        // Your test implementation
    });
}
```

## Step 2: Refresh Unity

Force Unity to recompile and refresh assets:

```bash
python PerSpec/Coordination/Scripts/quick_refresh.py full --wait
```

**Options:**
- `full` - Complete reimport
- `scripts` - Scripts only
- `--wait` - Wait for completion

## Step 3: Check Compilation

Verify no compilation errors exist:

```bash
python PerSpec/Coordination/Scripts/quick_logs.py errors
```

**Must see:** "No error logs found" before proceeding.

## Step 4: Run Tests

Execute your tests:

```bash
python PerSpec/Coordination/Scripts/quick_test.py all -p edit --wait
```

**Options:**
- `all` - Run all tests
- `class Namespace.ClassName` - Specific class (**MUST use full namespace!**)
- `method Namespace.ClassName.MethodName` - Specific method (**MUST use full path!**)
- `-p edit` - EditMode tests
- `-p play` - PlayMode tests
- `-p both` - Both modes

> **⚠️ IMPORTANT**: Class and method filters require FULL namespace-qualified names!
> - ❌ WRONG: `class SimplePerSpecTest` (finds 0 tests)
> - ✅ CORRECT: `class Tests.PlayMode.SimplePerSpecTest`

## Complete Example

```bash
# 1. Write your feature code
# 2. Refresh Unity
python PerSpec/Coordination/Scripts/quick_refresh.py full --wait

# 3. Check for errors (MUST be clean)
python PerSpec/Coordination/Scripts/quick_logs.py errors

# 4. Run tests
python PerSpec/Coordination/Scripts/quick_test.py all -p edit --wait
```

## Error Resolution

### Compilation Errors

If Step 3 shows errors:

1. Read error details:
   ```bash
   python PerSpec/Coordination/Scripts/quick_logs.py errors -v
   ```

2. Fix the errors in your code

3. Repeat from Step 2 (Refresh)

## Accessing PlayMode Logs

After running PlayMode tests, you can access the captured console logs:

```bash
# View PlayMode log summary
python PerSpec/Coordination/Scripts/test_playmode_logs.py

# Logs are stored in
PerSpec/PlayModeLogs/session_*.txt
```

Each log file contains:
- Timestamp, log level, and frame number
- Full message text
- Stack traces for errors and exceptions
- Thread information (main vs background)

### PlayMode Log Features
- Logs are written to `PerSpec/PlayModeLogs/` directory
- Directory is cleared when entering Play Mode
- Logs are saved every 5 seconds during Play Mode
- Final batch written when exiting Play Mode
- No performance overhead (no PlayerPrefs, no Update method)

### Common Errors

| Error | Solution |
|-------|----------|
| CS1626 (yield in try) | Use `UniTask.ToCoroutine()` |
| UniTask not found | Add to asmdef references |
| PerSpecDebug not found | Add `PerSpec.Runtime.Debug` to asmdef references |
| Thread exception | Add `UniTask.SwitchToMainThread()` |

## Background Processing

PerSpec continues working when Unity loses focus:
- System.Threading.Timer polls database
- Tests execute in background
- Results update automatically

## Monitoring

### Live Monitoring

```bash
# Watch for new logs
python PerSpec/Coordination/Scripts/quick_logs.py monitor -l error

# Watch test execution
python PerSpec/Coordination/Scripts/quick_test.py all -p edit --monitor
```

### Export Logs

```bash
# Export all logs (auto-saves to PerSpec/Logs/)
python PerSpec/Coordination/Scripts/quick_logs.py export

# Export as JSON
python PerSpec/Coordination/Scripts/quick_logs.py export --json

# Export only errors
python PerSpec/Coordination/Scripts/quick_logs.py export -l error
```

> **Note**: Exports automatically clear `PerSpec/Logs/` directory and save with timestamp

### Status Checks

From Unity:
- `Tools > PerSpec > Debug > Database Status`
- `Tools > PerSpec > Debug > Polling Status`

## Best Practices

1. **Always follow all 4 steps** - Don't skip compilation check
2. **Fix errors immediately** - Don't accumulate technical debt
3. **Use --wait flags** - Ensure operations complete
4. **Monitor background tasks** - Check polling status regularly

## Automation

Create a batch script for the workflow:

```bash
#!/bin/bash
# perspec-test.sh

echo "Step 2: Refreshing Unity..."
python PerSpec/Coordination/Scripts/quick_refresh.py full --wait

echo "Step 3: Checking errors..."
python PerSpec/Coordination/Scripts/quick_logs.py errors
if [ $? -ne 0 ]; then
    echo "Errors found! Fix them first."
    exit 1
fi

echo "Step 4: Running tests..."
python PerSpec/Coordination/Scripts/quick_test.py all -p both --wait
```

## Troubleshooting

### Tests Not Running

```bash
# Check database
python PerSpec/Coordination/Scripts/quick_logs.py sessions

# Reset coordination
Tools > PerSpec > Debug > Force Reinitialize
```

### Slow Performance

```bash
# Use targeted refresh
python PerSpec/Coordination/Scripts/quick_refresh.py scripts --wait

# Run specific tests
python PerSpec/Coordination/Scripts/quick_test.py class MyTests -p edit
```

## Summary

The 4-step workflow ensures:
- ✅ Code compiles correctly
- ✅ Tests execute reliably
- ✅ Errors caught early
- ✅ Consistent quality

Always: **Write → Refresh → Check → Test**