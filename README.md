# PerSpec: Command-Line Superpowers for Unity

<div align="center">

```
     ____            _____                
    / __ \___  _____/ ___/____  ___  _____
   / /_/ / _ \/ ___/\__ \/ __ \/ _ \/ ___/
  / ____/  __/ /   ___/ / /_/ /  __/ /__  
 /_/    \___/_/   /____/ .___/\___/\___/  
                      /_/
```

**Tired of copy-pasting Unity errors into your LLM? Frustrated with the endless cycle of switching back to the editor just to click "Run"?**

PerSpec bridges the gap between your terminal and the Unity Editor, giving you powerful command-line control over your development workflow. Stop the context switching. Start building faster.

</div>

## What is PerSpec?

PerSpec is a lightweight, rock-solid toolkit that connects your terminal to the Unity Editor using a simple but powerful combination of Python scripts and a local SQLite database. It allows you to **refresh assets, check for errors, run tests, and view logs** without ever leaving your command line.

It's designed for modern, test-driven development and is perfect for solo developers, teams, and even for coordinating with AI coding assistants.

## 🚀 Quick Start

1.  **Install via Unity Package Manager:**
    *   Go to `Window > Package Manager`.
    *   Click `+` > `Add package from git URL...`.
    *   Enter: `https://github.com/thePostFuturist/PerSpec.git`

2.  **Initialize PerSpec in Unity:**
    *   Once the package is installed, go to `Tools > PerSpec > Initialize PerSpec`.
    *   This creates the `PerSpec/` directory in your project root, which contains the coordination database and Python scripts.

3.  **Sync Python Scripts:**
    *   Open your terminal and run the sync script. This is the **single source of truth** that copies the necessary tools into your working directory. You should run this after every package update.
    ```bash
    python Packages/com.digitraver.perspec/ScriptingTools/sync_python_scripts.py
    ```

You're all set! You can now control Unity from your terminal.

## 🎮 The Recommended TDD Workflow

PerSpec is built for a fast, iterative Test-Driven Development (TDD) workflow. Follow this cycle for maximum efficiency and confidence.

#### **Step 1: Write Your Code & Tests**
Create your feature and a corresponding test file in your IDE.

#### **Step 2: Refresh Unity**
Tell Unity to import the new files and get ready for compilation.
```bash
python PerSpec/Coordination/Scripts/quick_refresh.py full --wait
```

#### **Step 3: Check for Compilation Errors**
Before you run any tests, make sure your code actually compiles! This command will show you any C# errors.
```bash
python PerSpec/Coordination/Scripts/monitor_editmode_logs.py --errors
```
> **Pro Tip:** If you see errors, fix them and go back to Step 2. Don't run tests if your code doesn't compile!

#### **Step 4: Run Your Tests**
Execute your test suite and see the results.
```bash
python PerSpec/Coordination/Scripts/quick_test.py all -p edit --wait
```

Rinse and repeat! This simple loop ensures you're always working with a stable, tested codebase.

## 🛠️ Command Reference

Here are the most common commands to supercharge your workflow

### Interacting with Unity

| Command | Prompt |
|---|---|
| `quick_refresh.py full --wait` | **Refresh Assets**: tells Unity to re-import and compile your code |
| `quick_menu.py execute "File/Save Project" --wait` | **Save Project**: saves the current scene and any project changes |
| `quick_menu.py execute "Window/General/Console" --wait` | **Open Console**: opens the Unity Console window |

### Viewing Logs (EditMode)

| Command | Prompt |
|---|---|
| `monitor_editmode_logs.py recent -n 50` | show the last 50 log entries |
| `monitor_editmode_logs.py --errors` | **Show Compilation Errors**: display C# compilation errors only |
| `monitor_editmode_logs.py --all-errors` | show all errors and exceptions from the editor |
| `monitor_editmode_logs.py live` | monitor logs in real-time |

### Viewing Logs (PlayMode)

| Command | Prompt |
|---|---|
| `test_playmode_logs.py` | show the last 50 logs from the most recent PlayMode session |
| `test_playmode_logs.py --errors` | **Show All Errors**: displays all runtime errors, exceptions, and assertions |
| `test_playmode_logs.py --cs-errors` | show only C# compilation errors found during PlayMode |
| `test_playmode_logs.py -S "keyword" -i` | search for a keyword in the logs (case-insensitive) |

### Running Tests & Viewing Results

| Command | Prompt |
|---|---|
| `quick_test.py all -p edit --wait` | run all **EditMode** tests |
| `quick_test.py all -p play --wait` | run all **PlayMode** tests |
| `test_results.py latest` | show a summary of the latest test run |
| `test_results.py latest -v` | show a verbose, detailed view of the latest test run |
| `test_results.py failed -v` | show detailed results for only the tests that failed |

### Scene & Hierarchy

| Command | Prompt |
|---|---|
| `scene_hierarchy.py export full --wait --show` | export the entire scene hierarchy to JSON and print it |
| `scene_hierarchy.py latest --show` | show the most recent scene export |

### Maintenance

| Command | Prompt |
|---|---|
| `quick_clean.py quick` | performs a quick cleanup of logs and compacts the database |
| `db_auto_maintenance.py` | run the automatic database maintenance process |

## 🤖 For Power Users & AI Integration

PerSpec was originally built to help guide LLM assistants like Claude and Gemini. The robust command-line interface is perfect for programmatic use. If you want to dive deeper into the architecture or integrate PerSpec with an AI, check out the detailed documentation:

*   **[AI/LLM Integration Guide](Documentation/LLM.md)**
*   **[Technical Architecture Deep Dive](README.md#%EF%B8%8F-technical-background-why-database--background-threading--network-servers)**

## 🤝 Contributing

PerSpec is open source! Contributions are welcome. Fork the repository and help us make Unity development faster and more reliable for everyone.

*   **Repo:** [https://github.com/thePostFuturist/PerSpec](https://github.com/thePostFuturist/PerSpec)

## 📄 License

MIT License - Use it, modify it, ship it!