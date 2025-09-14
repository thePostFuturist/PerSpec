#!/usr/bin/env python3
"""
Sync Python scripts from package to PerSpec working directory.
This script copies all .py files from the package to the PerSpec/Coordination/Scripts directory.
"""

import os
import shutil
from pathlib import Path
import sys
import io

# Set stdout to handle Unicode properly on Windows
if sys.platform == "win32":
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

def get_project_root():
    """Find Unity project root by looking for Assets folder"""
    current = Path.cwd()
    while current != current.parent:
        if (current / "Assets").exists():
            return current
        current = current.parent
    return Path.cwd()

def sync_python_scripts():
    """Copy all Python scripts from package to PerSpec directory"""
    project_root = get_project_root()

    # Define source directories in package
    package_base = project_root / "Packages" / "com.digitraver.perspec"
    source_dirs = [
        package_base / "Editor" / "Coordination" / "Scripts",
        package_base / "ScriptingTools" / "Coordination" / "Scripts"
    ]

    # Define destination directory
    dest_dir = project_root / "PerSpec" / "Coordination" / "Scripts"

    # Ensure destination exists
    dest_dir.mkdir(parents=True, exist_ok=True)

    copied_files = []
    skipped_files = []

    print(f"Syncing Python scripts to: {dest_dir}")
    print("-" * 60)

    for source_dir in source_dirs:
        if not source_dir.exists():
            print(f"Source directory not found: {source_dir}")
            continue

        print(f"\nProcessing: {source_dir.relative_to(project_root)}")

        # Find all .py files in source directory
        py_files = list(source_dir.glob("*.py"))

        for py_file in py_files:
            dest_file = dest_dir / py_file.name

            # Skip this sync script itself
            if py_file.name == "sync_python_scripts.py":
                continue

            try:
                # Copy and overwrite
                shutil.copy2(py_file, dest_file)
                copied_files.append(py_file.name)
                print(f"  + Copied: {py_file.name}")
            except Exception as e:
                skipped_files.append((py_file.name, str(e)))
                print(f"  - Failed: {py_file.name} - {e}")

    # Summary
    print("\n" + "=" * 60)
    print(f"Sync Complete!")
    print(f"  Files copied: {len(copied_files)}")
    if skipped_files:
        print(f"  Files failed: {len(skipped_files)}")
        for name, error in skipped_files:
            print(f"    - {name}: {error}")

    print(f"\nDestination: {dest_dir}")

    # List all Python files now in destination
    dest_files = sorted(dest_dir.glob("*.py"))
    if dest_files:
        print(f"\nPython scripts in PerSpec ({len(dest_files)} total):")
        for f in dest_files:
            size_kb = f.stat().st_size / 1024
            print(f"  - {f.name:40} ({size_kb:6.1f} KB)")

    return len(copied_files)

if __name__ == "__main__":
    try:
        count = sync_python_scripts()
        sys.exit(0 if count > 0 else 1)
    except Exception as e:
        print(f"Error: {e}")
        sys.exit(1)