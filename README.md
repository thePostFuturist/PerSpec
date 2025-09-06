
```
     ____            _____                
    / __ \___  _____/ ___/____  ___  _____
   / /_/ / _ \/ ___/\__ \/ __ \/ _ \/ ___/
  / ____/  __/ /   ___/ / /_/ /  __/ /__  
 /_/    \___/_/   /____/ .___/\___/\___/  
                      /_/
```

## Unity Alternative to MCP via SQLite and Code Generation Framework

> **Organize your LLM's code chaos generator into testable, maintainable chunks**
> 
> 🚫 **NO MCP SERVERS REQUIRED** - Rock-solid SQLite + Python instead
>
> ⚔️ **BATTLE-TESTED** - Powering production Unity development at [DigitRaver](https://digitraver.com)



## 🎯 The Problem with LLMs

Recognize any of these characters during your vibe code?

- **Monolithic nightmares** - 500+ line scripts that do everything and nothing
- **Indigestible spaghetti** - Code so tangled you can't tell where features start or end  
- **Untestable mysteries** - "It works on my machine" becomes "It worked when the AI wrote it"
- **Copy-paste disasters** - The same boilerplate repeated with slight variations
- **Context confusion** - Mixing MonoBehaviour patterns, async patterns, and Unity APIs incorrectly

You end up spending more time untangling AI-generated code than if you'd written it yourself.

## 🚀 Why We Don't Use MCP (Model Context Protocol)

**PerSpec avoids MCP servers** in favor of a more reliable SQLite + Python approach:

### The MCP Problem
MCP servers promise LLM-Unity communication but deliver headaches:
- **Constant disconnections** breaking your workflow
- **Port conflicts** with other development tools
- **Security vulnerabilities** from exposed network services as well as restrictive corporate firewalls
- **Complex setup** requiring server maintenance
- **State loss** when servers crash or restart

### The PerSpec Solution: SQLite + Python

We use **SQLite database + Python scripts** for more reliable Unity coordination than MCP, though with some Unity-specific limitations:

| Feature | MCP Server | PerSpec SQLite + Python |
|---------|------------|------------------------|
| **AI Quality** | ❌ Still limited by frontier models | ❌ Still limited by frontier models |
| **Target Audience** | ⚠️ Intermediate+ developers required | ⚠️ Intermediate+ developers required |
| **Supervision Required** | ✅ Always - AI still makes mistakes | ✅ Always - AI still makes mistakes |
| **Code Organization** | ❌ No structure enforcement | ✅ TDD patterns, testable chunks |
| **Reliability** | ❌ Server crashes, disconnects | ✅ No network failures, no services |
| **Setup** | ❌ Install, configure, maintain | ✅ Single file, zero configuration |
| **Security** | ❌ Open ports, network exposure | ✅ Local file, no attack surface |
| **Performance** | ❌ Network latency, timeouts | ⚠️ Fast I/O but 1-second polling delay |
| **Persistence** | ❌ Memory-based, volatile | ✅ Database survives everything |
| **Background Work** | ❌ Requires active connection | ⚠️ Polls but Unity APIs throttled without focus |
| **Debugging** | ❌ Black box server logs | ✅ SQL queries you can inspect |
| **Cross-platform** | ❌ Platform-specific servers | ✅ SQLite works everywhere |
| **Unity Integration** | ❌ External process coordination | ⚠️ Requires editor focus for full speed |
| **Vendor Lock-in** | ⚠️ Depends on specific MCP server implementation | ✅ Works with Claude, Cursor, OpenAI, Gemini, OpenRouter |

✨ **Auto-Focus**: PerSpec automatically brings the Unity Editor to the foreground on Windows and macOS when operations need to execute. No manual clicking required - commands run at full speed automatically!

### How It Works

The Unity editor uses a background timer to poll the SQLite database:
- **Commands are queued** when Unity isn't the active window
- **Automatic focus management** brings Unity to foreground when needed (Windows/macOS)
- **Test results are captured** once Unity processes them
- **Database persists through everything** - no state loss ever
- **More reliable than MCP** but still subject to Unity's limitations

### Automatic Unity Focus Management

PerSpec includes smart focus management that ensures your operations execute at full speed:

**Windows & macOS Support:**
- Automatically brings Unity Editor to the foreground when commands are ready
- No manual intervention needed - just run your commands
- Operations execute immediately at full speed
- Works seamlessly with multi-monitor setups

**macOS First-Time Setup:**
- **First Run**: macOS may ask to grant Terminal/Python accessibility permissions
  - Go to: System Preferences → Security & Privacy → Privacy → Accessibility
  - Add Terminal or your Python executable
- **Security Note**: This is a standard macOS security feature for automation, not a limitation of PerSpec

**How it works:**
```csharp
// When database has pending commands
if (HasPendingCommands()) {
    // Automatically focus Unity Editor window
    BringUnityToFront();  // Platform-specific implementation
    // Commands execute at full speed
    ExecuteCommands();
}
```

This eliminates the focus throttling issue entirely - your tests and operations run at maximum speed without you having to click on Unity!

## ✨ The PerSpec Solution

PerSpec transforms your LLM into a **Test-Driven Development powerhouse** for Unity:

### Atomic, Testable Bits
Instead of monoliths, get small, focused components that do ONE thing well:
```csharp
// ❌ What LLMs usually generate
public class PlayerController : MonoBehaviour {
    // 500 lines handling input, physics, animation, 
    // inventory, saving, networking... 😱
}

// ✅ What PerSpec guides them to create
public class PlayerMovement : MonoBehaviour { /* 50 lines */ }
public class PlayerInput : MonoBehaviour { /* 30 lines */ }
public class PlayerAnimator : MonoBehaviour { /* 40 lines */ }
```

### Vibe Code Your Features
Watch your feature evolve through rapid TDD cycles:
1. **Write test** → See it fail (red)
2. **Write code** → Make it pass (green)  
3. **Refactor** → Keep it clean (refactor)
4. **Repeat** → Build confidence with each cycle

### Confidence Through Testing
Every feature comes with tests that prove it works:
```csharp
[UnityTest]
public IEnumerator PlayerShouldMoveForward() => UniTask.ToCoroutine(async () => {
    // Your LLM writes the test FIRST
    // Then implements the feature
    // You KNOW it works
});
```

## 📦 Quick Start

### 1. Install PerSpec

**Via Unity Package Manager:**
```
Window > Package Manager > + > Add package from git URL...
https://github.com/thePostFuturist/PerSpec.git
```

**Or add to `manifest.json`:**
```json
{
  "dependencies": {
    "com.digitraver.perspec": "https://github.com/thePostFuturist/PerSpec.git",
    "com.cysharp.unitask": "2.5.10",
    "com.gilzoide.sqlite-net": "1.3.1",
    "com.unity.nuget.newtonsoft-json": "3.2.1"
  }
}
```

### 2. Initialize PerSpec

In Unity Editor:
```
Tools > PerSpec > Initialize PerSpec
```

This creates your working directory with SQLite database and Python tools.

### 3. Configure Your LLM

Copy the LLM instructions to your project using `Tools > PerSpec > Control Panel ` then choose `LLM Setup`

### 5. Start the TDD Workflow

Tell your LLM: "Follow the 4-step TDD workflow in CLAUDE.md"

Watch as it:
1. Writes tests first
2. Refreshes Unity automatically
3. Checks for compilation errors
4. Runs tests until they pass

## 🎮 The 4-Step TDD Workflow

This is the heart of PerSpec - your LLM will follow this automatically:

```bash
# Step 1: Write feature with tests (TDD style)
# LLM creates test first, then implementation

# Step 2: Refresh Unity
python PerSpec/Coordination/Scripts/quick_refresh.py full --wait

# Step 3: Check for errors (must be clean!)
python PerSpec/Coordination/Scripts/monitor_logs.py errors

# Step 4: Run tests
python PerSpec/Coordination/Scripts/quick_test.py all -p edit --wait
# If tests fail, loop back to Step 1
```

## 🏗️ What You Get

### Clean Architecture
- **SOLID principles** enforced automatically
- **No singleton MonoBehaviours** (ever!)
- **Proper async patterns** with UniTask
- **Component-based** design that actually makes sense

### Readable Code
- **Focused classes** under 100 lines
- **Clear responsibilities** - one class, one job
- **Meaningful tests** that document behavior
- **Consistent patterns** across your codebase

### Confidence
- **Every feature has tests** - not optional
- **Continuous validation** - know immediately when something breaks
- **Refactor fearlessly** - tests have your back
- **Document through tests** - tests show how to use your code

### No Vendor Lock-in
- **Works with ANY AI assistant** - Claude, Cursor, OpenAI, Gemini, OpenRouter
- **Universal instruction format** - Same TDD workflow for all providers
- **Easy provider switching** - Change AI providers without changing your workflow
- **Multi-provider support** - Use different AIs for different tasks simultaneously

#### Supported AI Providers
Configure PerSpec to work with your preferred AI assistant:

| Provider | Config File | Purpose |
|----------|------------|---------|
| **Claude Code** | `CLAUDE.md` | Anthropic's coding assistant |
| **Cursor IDE** | `.cursorrules` | AI-powered IDE |
| **OpenAI Codex** | `.openai-codex.md` | GPT-based coding |
| **Gemini CLI** | `.gemini/config.md` | Google's AI assistant |
| **OpenRouter/Buddy** | `.buddyrules` | Access 100+ models via unified API |

All providers receive the same PerSpec TDD instructions - no rewriting needed when switching!

## 🛠️ Requirements

- Unity 6.000.47f1 or higher
- UniTask package
- SQLite package
- Entities package
- Python 3.7+ (for coordination scripts)
- Git (for package installation)

## 📚 Documentation

See the full documentation at [https://github.com/thePostFuturist/PerSpec](https://github.com/thePostFuturist/PerSpec)

## 🏢 About

<div style="text-align: center;">
  <img src="Editor/Resources/Icons/digitraver.png" width="256" height="256" alt="DigitRaver"/>
  </div>
  **Made in San Francisco**  
  By Valentin Burov
  
  Fork & contribute at [https://github.com/thePostFuturist/PerSpec](https://github.com/thePostFuturist/PerSpec)  
  Join us at [https://digitRaver.com](https://digitRaver.com)


## 🤝 Contributing

PerSpec is open source! We welcome contributions that help make LLM-generated Unity code better.

## 📄 License

MIT License - Use it, modify it, ship it!

---

**Stop fighting with AI-generated spaghetti code. Start writing Unity code that makes sense.**

🚀 **Get PerSpec Now** → Transform your LLM into a TDD machine

## 🔬 Technical Background: Why Database + Background Threading > Network Servers

### The Architecture That Actually Works

While other Unity automation tools rely on fragile network servers, PerSpec uses a rock-solid **SQLite database + System.Threading.Timer** approach that never fails.

### How Our Background Threading Works

The magic happens in `BackgroundPoller.cs`:

```csharp
// System.Threading.Timer runs on a thread pool thread
// This continues even when Unity loses focus!
_backgroundTimer = new System.Threading.Timer(
    BackgroundPollCallback,
    null,
    0,              // Start immediately
    _pollInterval   // Poll every second
);

// Database operations happen on background thread
private static void BackgroundPollCallback(object state) {
    // SQLite with WAL mode = thread-safe reads/writes
    bool hasRequests = CheckDatabase();  // Works from any thread!
    
    if (hasRequests) {
        // Marshal back to Unity's main thread for API calls
        _unitySyncContext.Post(_ => {
            AssetDatabase.Refresh();  // Unity APIs require main thread
            RunTests();
        }, null);
    }
}
```


### Why This Architecture Is Better (But Not Perfect)

#### 1. **Zero Network Overhead**
```
Server Approach:           Database Approach:
Python → TCP Socket       Python → SQLite File
       ↓ ~10ms                  ↓ ~0.01ms
Unity Server Listen       Unity → SQLite File
       ↓ Parse JSON              ↓ Direct Read
Execute Command           Execute Command
```

#### 2. **Background Operation Reality**

| Scenario | Server-Based | PerSpec Database |
|----------|--------------|------------------|
| Unity in background | ❌ Stops processing | ⚠️ Polls but operations throttled |
| Unity compiling | ❌ Server unreachable | ✅ Database accessible |
| Domain reload | ❌ Connection lost | ✅ Auto-reconnects |
| Unity crash | ❌ Server orphaned | ✅ Database persists |
| Real-time execution | ❌ Network delays | ⚠️ 1-second polling interval |

#### 3. **Thread-Safe by Design**

SQLite's WAL (Write-Ahead Logging) mode enables:
- **Concurrent readers and writers** - Python writes while Unity reads
- **ACID transactions** - Operations are atomic, never partial
- **Automatic rollback** - Failed operations don't corrupt state
- **Lock-free reads** - No blocking between processes

```sql
-- Python writes a command
BEGIN TRANSACTION;
INSERT INTO commands (type, data) VALUES ('test', 'all');
COMMIT;  -- Atomic - either fully written or not at all

-- Unity reads simultaneously (no lock needed with WAL)
SELECT * FROM commands WHERE status = 'pending';
```

#### 4. **Performance Characteristics**

**Latency Comparison:**
- **SQLite file I/O**: ~10-100 microseconds
- **Local TCP socket**: ~1-10 milliseconds  
- **HTTP REST call**: ~10-100 milliseconds
- **Network RPC**: ~5-50 milliseconds

**Throughput:**
- **SQLite**: 50,000+ operations/second
- **TCP Server**: 1,000-5,000 requests/second
- **HTTP Server**: 100-1,000 requests/second

#### 5. **Reliability Features Built-In**

```python
# Commands survive everything
conn.execute("INSERT INTO commands ...")  # Written to disk
# Unity crashes? Command still there when it restarts
# Python crashes? Database intact
# Power loss? SQLite's journal recovers on next start
```

**Automatic Recovery:**
- **Journal mode** protects against corruption
- **No connection state** to manage or restore
- **No ports** to check or release
- **No processes** to monitor or restart

### Real-World Benefits

#### During Development
- **Compile while testing** - Background thread keeps working during compilation
- **Multi-window workflow** - Switch between IDE and Unity without interruption  
- **Batch operations** - Queue 100 tests, walk away, come back to results

#### In CI/CD
- **No service management** - No servers to start/stop/health-check
- **Portable state** - Database file can be archived with test results
- **Debugging** - SQL queries show exact command history

#### For Reliability
- **Zero maintenance** - No server processes to monitor
- **No timeouts** - Commands wait patiently until Unity is ready
- **Crash-proof** - Database survives Unity crashes, restarts, updates

### The Implementation Details

**Unity Side (C#):**
```csharp
[InitializeOnLoad]
public static class BackgroundPoller {
    static BackgroundPoller() {
        // Captures Unity's main thread context
        _unitySyncContext = SynchronizationContext.Current;
        
        // Starts background timer (thread pool)
        _backgroundTimer = new System.Threading.Timer(...);
    }
}
```

**Python Side:**
```python
# Simple, direct, reliable
conn = sqlite3.connect("test_coordination.db")
conn.execute("PRAGMA journal_mode=WAL")  # Enable concurrent access
conn.execute("INSERT INTO commands ...")
conn.commit()
# That's it! Unity will pick it up within 1 second
```

### Why Not Just Use EditorCoroutines?

EditorCoroutines and similar Unity-based polling:
- **Stop when Unity loses focus** (our #1 problem to solve)
- **Pause during compilation** (when we need them most)
- **Can't recover from domain reloads** cleanly
- **Subject to Unity's update loop** throttling

System.Threading.Timer:
- **Runs on thread pool** - independent of Unity's state
- **Continues polling during compilation** - though execution waits for Unity
- **Survives domain reloads** - [InitializeOnLoad] restarts it
- **Background polling** - checks database every second

### The Bottom Line

By choosing **SQLite + Background Threading** over network servers, PerSpec delivers:
- **No network failures** - Database-based, not network-dependent
- **10-100x faster I/O** than network calls (though with 1-second polling)
- **Zero configuration** - No ports, URLs, or certificates
- **Queued operations** - Commands execute when Unity regains focus
- **Crash recovery** - Database persists through Unity restarts

This is a pragmatic compromise that works within Unity's constraints while avoiding the fragility of network-based solutions.

## 🎯 Zero-Cost Debug Logging with PerSpecDebug

### The Hidden Cost of Traditional Debug Logging

Most Unity projects use one of these approaches for debug logging:

```csharp
// Approach 1: Runtime flag checking
if (debugEnabled) {
    Debug.Log($"Processing {items.Count} items at {Time.time}");  // Still allocates!
}

// Approach 2: Preprocessor directives everywhere
#if DEBUG
    Debug.Log($"Processing {items.Count} items at {Time.time}");
#endif

// Approach 3: Custom logger with levels
Logger.LogDebug($"Processing {items.Count} items");  // Method still called!
```

**The Problems:**
- **Runtime overhead** - Flag checks happen millions of times per frame
- **Memory allocations** - Strings are created even when not logged
- **Code bloat** - Debug code remains in production builds
- **Ugly code** - #if directives everywhere make code unreadable

### PerSpecDebug: Compiler-Level Stripping

PerSpecDebug uses C#'s `[Conditional]` attribute for **complete compile-time removal**:

```csharp
// In your code - clean and readable
PerSpecDebug.Log($"Processing {items.Count} items");
PerSpecDebug.LogFeatureStart("PHYSICS", "Calculating collisions");
PerSpecDebug.LogTestAssert($"Expected {expected}, got {actual}");

// In PerSpecDebug.cs
[Conditional("PERSPEC_DEBUG")]  // Controlled by PERSPEC_DEBUG symbol only
public static void Log(object message) {
    UnityEngine.Debug.Log(message);
}
```

### What Actually Happens

**When PERSPEC_DEBUG symbol is defined (editor or builds):**
```csharp
// Your code compiles to:
PerSpecDebug.Log($"Processing {items.Count} items");  // ✅ Executes and shows in console
```

**When PERSPEC_DEBUG symbol is NOT defined:**
```csharp
// Your code compiles to:
// [NOTHING - The entire line is removed by the C# compiler]
```

That's right - the method call **doesn't exist** in production. Not disabled, not skipped - **completely absent**.

### Performance Comparison

```csharp
// Traditional approach - Production build
for (int i = 0; i < 1000000; i++) {
    if (debugEnabled) {  // ❌ 1 million checks
        Debug.Log($"Item {i}");  // ❌ String still formatted
    }
}
// Cost: ~5ms for flag checks + memory pressure

// PerSpecDebug approach - Production build
for (int i = 0; i < 1000000; i++) {
    PerSpecDebug.Log($"Item {i}");  // Compiler removes this line
}
// Cost: 0ms, 0 bytes - loop body is empty!
```

### Real-World Benefits

#### Memory Usage
```csharp
// Traditional: Allocates even when disabled
void Update() {
    string debugInfo = $"Player at {transform.position}";  // 64 bytes/frame
    if (showDebug) Debug.Log(debugInfo);  // 60 FPS = 3.8KB/second wasted
}

// PerSpecDebug: Zero allocations in production
void Update() {
    PerSpecDebug.Log($"Player at {transform.position}");  // 0 bytes in production
}
```

#### Build Size
- **Traditional Debug.Log**: Keeps all format strings in binary
- **PerSpecDebug**: Strips everything - smaller builds

Example from a real project:
- With Debug.Log (disabled): 142 MB build
- With PerSpecDebug: 138 MB build
- **4 MB saved** just from removing debug strings!

### Rich Development Experience

Despite zero production cost, developers get rich debugging:

```csharp
// Categorized logging for better organization
PerSpecDebug.LogTestSetup("Arranging test environment");
PerSpecDebug.LogTestAction("Executing player movement");
PerSpecDebug.LogTestAssert("Verifying position changed");
PerSpecDebug.LogTestComplete("Movement test passed");

// Feature-specific logging with prefixes
PerSpecDebug.LogFeatureStart("SAVE", "Beginning save operation");
PerSpecDebug.LogFeatureProgress("SAVE", "Written 50% of data");
PerSpecDebug.LogFeatureComplete("SAVE", "Save successful");
PerSpecDebug.LogFeatureError("SAVE", "Failed to write file");

// Assertions that vanish in production
PerSpecDebug.Assert(player != null, "Player must exist");
PerSpecDebug.AssertFormat(health > 0, "Health {0} must be positive", health);
```

Output in Unity Console:
```
[TEST-SETUP] Arranging test environment
[TEST-ACT] Executing player movement
[SAVE-START] Beginning save operation
[SAVE-PROGRESS] Written 50% of data
[SAVE-COMPLETE] Save successful
```

### The Compiler Does the Work

The magic is in letting the C# compiler optimize:

```csharp
// Your source code
public void ProcessData(byte[] data) {
    PerSpecDebug.LogFeatureStart("PROCESS", $"Processing {data.Length} bytes");
    
    for (int i = 0; i < data.Length; i++) {
        data[i] = ProcessByte(data[i]);
        PerSpecDebug.LogFeatureProgress("PROCESS", $"Processed {i}/{data.Length}");
    }
    
    PerSpecDebug.LogFeatureComplete("PROCESS", "All data processed");
}

// Production IL code (simplified)
public void ProcessData(byte[] data) {
    for (int i = 0; i < data.Length; i++) {
        data[i] = ProcessByte(data[i]);
    }
}
```

The compiler completely eliminates:
- Method calls
- String formatting
- Parameter evaluation
- Stack frame allocation

### Why This Matters

**Traditional Logging Tax:**
- 2-5% CPU overhead from disabled logging checks
- 100KB-1MB of wasted memory from debug strings
- 5-10MB larger builds from debug code

**PerSpecDebug Guarantee:**
- **0% CPU overhead** - Code doesn't exist
- **0 bytes allocated** - Strings never created
- **Smaller builds** - Debug code stripped

### Best Practices

```csharp
public class PlayerController : MonoBehaviour {
    [SerializeField] private bool verboseLogging = true;  // Editor-only flag
    
    void Update() {
        // Always use PerSpecDebug for development logging
        PerSpecDebug.Log($"Frame {Time.frameCount}: Processing input");
        
        // Use verboseLogging for optional detail in Editor
        if (verboseLogging) {
            PerSpecDebug.LogFormat("Input vector: {0}", inputVector);
        }
        
        // Critical errors should use regular Debug.LogError
        if (health < 0) {
            Debug.LogError("Player health negative!");  // Keep in production
        }
    }
}
```

### The Bottom Line

PerSpecDebug gives you:
- **Rich debugging** during development
- **Zero cost** in production
- **Clean code** without preprocessor directives
- **Automatic optimization** by the compiler

This is debug logging done right - all the benefits during development, none of the costs in production.