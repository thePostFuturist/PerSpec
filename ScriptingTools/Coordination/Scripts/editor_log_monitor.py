#!/usr/bin/env python3
"""
Unity Editor.log Monitor - Captures compilation errors in real-time
Cross-platform support for Windows, macOS, and Linux
"""

import os
import sys
import platform
import time
import sqlite3
import re
from pathlib import Path
from datetime import datetime, timedelta
from typing import Optional, List, Dict, Tuple
import hashlib

# Prevent Python from creating .pyc files
sys.dont_write_bytecode = True
os.environ['PYTHONDONTWRITEBYTECODE'] = '1'

def get_editor_log_path() -> Path:
    """Get Unity Editor.log path based on operating system"""
    system = platform.system()
    
    if system == "Windows":
        # Windows: %LOCALAPPDATA%\Unity\Editor\Editor.log
        local_appdata = os.environ.get('LOCALAPPDATA')
        if local_appdata:
            return Path(local_appdata) / "Unity" / "Editor" / "Editor.log"
    elif system == "Darwin":  # macOS
        # macOS: ~/Library/Logs/Unity/Editor.log
        return Path.home() / "Library" / "Logs" / "Unity" / "Editor.log"
    elif system == "Linux":
        # Linux: ~/.config/unity3d/Editor.log
        return Path.home() / ".config" / "unity3d" / "Editor.log"
    
    raise RuntimeError(f"Unsupported operating system: {system}")

def get_project_root() -> Path:
    """Find Unity project root by looking for Assets folder"""
    current = Path.cwd()
    while current != current.parent:
        if (current / "Assets").exists():
            return current
        current = current.parent
    return Path.cwd()

def get_db_path() -> Path:
    """Get database path in PerSpec folder"""
    project_root = get_project_root()
    return project_root / "PerSpec" / "test_coordination.db"

class CompilationError:
    """Represents a parsed compilation error"""
    def __init__(self, full_text: str):
        self.full_text = full_text
        self.file_path = ""
        self.line_number = 0
        self.column_number = 0
        self.error_code = ""
        self.error_message = ""
        self.detected_at = datetime.now()
        self.batch_id = ""
        
        self._parse()
    
    def _parse(self):
        """Parse Unity compilation error format"""
        # Pattern: Assets/Scripts/Example.cs(10,5): error CS0117: message
        pattern = r'^(.+?)\((\d+),(\d+)\): error (CS\d+): (.+)$'
        match = re.match(pattern, self.full_text)
        
        if match:
            self.file_path = match.group(1)
            self.line_number = int(match.group(2))
            self.column_number = int(match.group(3))
            self.error_code = match.group(4)
            self.error_message = match.group(5)
        else:
            # Fallback for different error formats
            self.error_message = self.full_text
    
    def to_dict(self) -> Dict:
        """Convert to dictionary for database storage"""
        return {
            'error_code': self.error_code,
            'file_path': self.file_path,
            'line_number': self.line_number,
            'column_number': self.column_number,
            'error_message': self.error_message,
            'full_text': self.full_text,
            'detected_at': self.detected_at.isoformat(),
            'batch_id': self.batch_id,
            'is_stale': 0
        }
    
    def get_hash(self) -> str:
        """Get hash of error for deduplication"""
        content = f"{self.file_path}:{self.line_number}:{self.column_number}:{self.error_code}"
        return hashlib.md5(content.encode()).hexdigest()

class EditorLogMonitor:
    def __init__(self):
        self.log_path = get_editor_log_path()
        self.db_path = get_db_path()
        self.last_position = 0
        self.last_error_time = None
        self.current_batch_id = None
        self.seen_errors = set()  # Track error hashes to avoid duplicates
        
        if not self.log_path.exists():
            raise FileNotFoundError(f"Unity Editor.log not found at: {self.log_path}")
        
        self._init_database()
        self._load_state()
    
    def _init_database(self):
        """Initialize database with compilation_errors table"""
        conn = sqlite3.connect(str(self.db_path))
        cursor = conn.cursor()
        
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS compilation_errors (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                error_code TEXT,
                file_path TEXT,
                line_number INTEGER,
                column_number INTEGER,
                error_message TEXT,
                full_text TEXT,
                detected_at TIMESTAMP,
                batch_id TEXT,
                is_stale INTEGER DEFAULT 0
            )
        """)
        
        # Create index for faster queries
        cursor.execute("""
            CREATE INDEX IF NOT EXISTS idx_compilation_errors_batch 
            ON compilation_errors(batch_id, is_stale)
        """)
        
        # Create state tracking table
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS monitor_state (
                key TEXT PRIMARY KEY,
                value TEXT
            )
        """)
        
        conn.commit()
        conn.close()
    
    def _load_state(self):
        """Load last read position from database"""
        conn = sqlite3.connect(str(self.db_path))
        cursor = conn.cursor()
        
        cursor.execute("SELECT value FROM monitor_state WHERE key = 'last_position'")
        row = cursor.fetchone()
        
        if row:
            self.last_position = int(row[0])
        else:
            # Start from end of file to avoid processing old errors
            self.last_position = self.log_path.stat().st_size
            self._save_state()
        
        conn.close()
    
    def _save_state(self):
        """Save current read position to database"""
        conn = sqlite3.connect(str(self.db_path))
        cursor = conn.cursor()
        
        cursor.execute("""
            INSERT OR REPLACE INTO monitor_state (key, value) 
            VALUES ('last_position', ?)
        """, (str(self.last_position),))
        
        conn.commit()
        conn.close()
    
    def _mark_old_errors_stale(self):
        """Mark errors older than 1 second as stale"""
        conn = sqlite3.connect(str(self.db_path))
        cursor = conn.cursor()
        
        cutoff_time = (datetime.now() - timedelta(seconds=1)).isoformat()
        cursor.execute("""
            UPDATE compilation_errors 
            SET is_stale = 1 
            WHERE detected_at < ? AND is_stale = 0
        """, (cutoff_time,))
        
        conn.commit()
        conn.close()
    
    def _store_error(self, error: CompilationError):
        """Store compilation error in database"""
        # Check for duplicate
        error_hash = error.get_hash()
        if error_hash in self.seen_errors:
            return
        
        self.seen_errors.add(error_hash)
        
        conn = sqlite3.connect(str(self.db_path))
        cursor = conn.cursor()
        
        data = error.to_dict()
        cursor.execute("""
            INSERT INTO compilation_errors 
            (error_code, file_path, line_number, column_number, error_message, 
             full_text, detected_at, batch_id, is_stale)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, (
            data['error_code'], data['file_path'], data['line_number'],
            data['column_number'], data['error_message'], data['full_text'],
            data['detected_at'], data['batch_id'], data['is_stale']
        ))
        
        conn.commit()
        conn.close()
    
    def _is_compilation_error(self, line: str) -> bool:
        """Check if line is a compilation error"""
        # Unity compilation error patterns
        patterns = [
            r'.*\(\d+,\d+\): error CS\d+:',  # C# errors
            r'.*\(\d+,\d+\): error.*',        # General errors
            r'Compilation failed:',            # Compilation summary
        ]
        
        for pattern in patterns:
            if re.match(pattern, line):
                return True
        return False
    
    def _is_refresh_marker(self, line: str) -> bool:
        """Check if line indicates a new compilation attempt"""
        markers = [
            "Refresh: detecting if any assets need to be imported",
            "Reloading assemblies",
            "Starting recompilation",
            "Compiling editor scripts",
            "- Starting script compilation",
        ]
        
        return any(marker in line for marker in markers)
    
    def process_new_content(self):
        """Process new content added to Editor.log"""
        current_size = self.log_path.stat().st_size
        
        # If file was truncated or rotated, reset position
        if current_size < self.last_position:
            print(f"[INFO] Editor.log was rotated/truncated. Resetting position.")
            self.last_position = 0
            self.seen_errors.clear()
        
        # No new content
        if current_size == self.last_position:
            return
        
        with open(self.log_path, 'r', encoding='utf-8', errors='ignore') as f:
            f.seek(self.last_position)
            new_lines = f.readlines()
            self.last_position = f.tell()
        
        errors_found = []
        current_time = datetime.now()
        
        for line in new_lines:
            line = line.strip()
            if not line:
                continue
            
            # Check for refresh marker (new compilation)
            if self._is_refresh_marker(line):
                # Mark all previous errors as stale
                self._mark_old_errors_stale()
                self.seen_errors.clear()
                self.current_batch_id = None
                print(f"[INFO] New compilation detected: {line[:50]}...")
                continue
            
            # Check for compilation error
            if self._is_compilation_error(line):
                error = CompilationError(line)
                
                # Determine batch ID (group errors within 5 seconds)
                if self.last_error_time and (current_time - self.last_error_time).seconds <= 5:
                    # Use existing batch
                    error.batch_id = self.current_batch_id
                else:
                    # New batch
                    self.current_batch_id = current_time.strftime("%Y%m%d_%H%M%S")
                    error.batch_id = self.current_batch_id
                    # Clear seen errors for new batch
                    self.seen_errors.clear()
                
                self.last_error_time = current_time
                errors_found.append(error)
        
        # Store errors
        for error in errors_found:
            self._store_error(error)
            print(f"[ERROR] {error.error_code}: {error.file_path}:{error.line_number} - {error.error_message[:50]}...")
        
        # Save state
        self._save_state()
        
        if errors_found:
            print(f"[INFO] Stored {len(errors_found)} compilation errors in batch {self.current_batch_id}")
    
    def monitor(self, interval: float = 1.0):
        """Monitor Editor.log continuously"""
        print(f"[START] Monitoring Unity Editor.log at: {self.log_path}")
        print(f"[INFO] Database: {self.db_path}")
        print(f"[INFO] Starting from position: {self.last_position}")
        print("[INFO] Press Ctrl+C to stop monitoring")
        
        try:
            while True:
                self.process_new_content()
                time.sleep(interval)
        except KeyboardInterrupt:
            print("\n[STOP] Monitoring stopped by user")
        except Exception as e:
            print(f"[ERROR] Monitoring failed: {e}")
            raise

def main():
    """Main entry point"""
    import argparse
    
    parser = argparse.ArgumentParser(description='Monitor Unity Editor.log for compilation errors')
    parser.add_argument('--interval', type=float, default=1.0,
                       help='Check interval in seconds (default: 1.0)')
    parser.add_argument('--reset', action='store_true',
                       help='Reset monitoring position to end of file')
    parser.add_argument('--clear', action='store_true',
                       help='Clear all compilation errors from database')
    
    args = parser.parse_args()
    
    try:
        monitor = EditorLogMonitor()
        
        if args.clear:
            conn = sqlite3.connect(str(monitor.db_path))
            cursor = conn.cursor()
            cursor.execute("DELETE FROM compilation_errors")
            conn.commit()
            conn.close()
            print("[INFO] Cleared all compilation errors from database")
            return 0
        
        if args.reset:
            monitor.last_position = monitor.log_path.stat().st_size
            monitor._save_state()
            print(f"[INFO] Reset position to end of file: {monitor.last_position}")
        
        monitor.monitor(interval=args.interval)
        
    except FileNotFoundError as e:
        print(f"[ERROR] {e}")
        print("\nMake sure Unity Editor is running and has created the Editor.log file.")
        return 1
    except Exception as e:
        print(f"[ERROR] {e}")
        return 1
    
    return 0

if __name__ == "__main__":
    sys.exit(main())