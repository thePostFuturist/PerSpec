# PerSpec - Unity Alternative to MCP via SQLite and Code Generation Framework

> **Transform your LLM from a code chaos generator into a precision TDD machine**
> 
> 🚫 **NO MCP SERVERS REQUIRED** - Uses rock-solid SQLite + Python instead!

## 🎯 The Problem with LLMs

What's the typical result of vibe coding in Unity?

- **Monolithic nightmares** - 500+ line scripts that do everything and nothing
- **Indigestible spaghetti** - Code so tangled you can't tell where features start or end  
- **Untestable mysteries** - "It works on my machine" becomes "It worked when the AI wrote it"
- **Copy-paste disasters** - The same boilerplate repeated with slight variations
- **Context confusion** - Mixing MonoBehaviour patterns, async patterns, and Unity APIs incorrectly

You end up spending more time untangling AI-generated code than if you'd written it yourself.

## 🚀 Why We Don't Use MCP (Model Context Protocol)

**PerSpec deliberately avoids MCP servers** in favor of a bulletproof SQLite + Python approach:

### The MCP Problem
MCP servers promise LLM-Unity communication but deliver headaches:
- **Constant disconnections** breaking your workflow
- **Port conflicts** with other development tools
- **Security vulnerabilities** from exposed network services
- **Complex setup** requiring server maintenance
- **State loss** when servers crash or restart

### The PerSpec Solution: SQLite + Python

We use **SQLite database + Python scripts** for rock-solid Unity coordination:

| Feature | MCP Server | PerSpec SQLite + Python |
|---------|------------|------------------------|
| **Reliability** | ❌ Server crashes, disconnects | ✅ Always works, no services to fail |
| **Setup** | ❌ Install, configure, maintain | ✅ Single file, zero configuration |
| **Security** | ❌ Open ports, network exposure | ✅ Local file, no attack surface |
| **Performance** | ❌ Network latency, timeouts | ✅ Direct file I/O, instant access |
| **Persistence** | ❌ Memory-based, volatile | ✅ Database survives everything |
| **Background Work** | ❌ Requires active connection | ✅ Works even when Unity loses focus |
| **Debugging** | ❌ Black box server logs | ✅ SQL queries you can inspect |
| **Cross-platform** | ❌ Platform-specific servers | ✅ SQLite works everywhere |

### How It Works
```python
# Simple, reliable Python-Unity communication
db.execute("INSERT INTO commands (type, data) VALUES ('refresh', 'full')")
# Unity polls database in background (even when not in focus!)
# No servers, no ports, no problems
```

The Unity editor uses a background timer to poll the SQLite database, ensuring:
- **Commands execute even when Unity isn't the active window**
- **Test results are captured reliably**
- **No "connection lost" errors ever**
- **Your workflow never breaks**

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

### "Vibe Code" Your Features
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
https://github.com/yourusername/perspec-framework.git#main
```

**Or add to `manifest.json`:**
```json
{
  "dependencies": {
    "com.perspec.framework": "https://github.com/yourusername/perspec-framework.git#main",
    "com.cysharp.unitask": "2.3.3"
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

Copy the LLM instructions to your project:

**For Claude (create `CLAUDE.md` in project root):**
```bash
cp Packages/com.perspec.framework/Documentation~/LLM.md ./CLAUDE.md
```

**For Cursor (create `.cursorrules` in project root):**
```bash
cp Packages/com.perspec.framework/Documentation~/LLM.md ./.cursorrules
```

**For any LLM:** Just copy the contents of `LLM.md` to wherever your LLM reads instructions.

### 4. Copy Agent Definitions

```bash
cp -r Packages/com.perspec.framework/Documentation~/agents ./.claude/agents
```

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
python ScriptingTools/Coordination/Scripts/quick_refresh.py full --wait

# Step 3: Check for errors (must be clean!)
python ScriptingTools/Coordination/Scripts/quick_logs.py errors

# Step 4: Run tests
python ScriptingTools/Coordination/Scripts/quick_test.py all -p edit --wait
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

## 🛠️ Requirements

- Unity 2021.3 or higher
- UniTask package
- Python 3.7+ (for coordination scripts)
- Git (for package installation)

## 📚 Documentation

- **LLM Instructions**: `Packages/com.perspec.framework/Documentation~/LLM.md`
- **Agent Definitions**: `Packages/com.perspec.framework/Documentation~/agents/`
- **API Documentation**: `Packages/com.perspec.framework/Documentation~/API.md`

## 🤝 Contributing

PerSpec is open source! We welcome contributions that help make LLM-generated Unity code better.

## 📄 License

MIT License - Use it, modify it, ship it!

---

**Stop fighting with AI-generated spaghetti code. Start writing Unity code that makes sense.**

🚀 **Get PerSpec Now** → Transform your LLM into a TDD machine