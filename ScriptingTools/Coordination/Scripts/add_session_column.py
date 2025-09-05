#!/usr/bin/env python3
"""
Add session_id column to compilation_errors table if it doesn't exist
"""

# Prevent Python from creating .pyc files
import sys
import os
sys.dont_write_bytecode = True
os.environ['PYTHONDONTWRITEBYTECODE'] = '1'
import sqlite3
from pathlib import Path

def get_project_root():
    """Find Unity project root by looking for Assets folder"""
    current = Path.cwd()
    while current != current.parent:
        if (current / "Assets").exists():
            return current
        current = current.parent
    return Path.cwd()

def get_db_path():
    """Get database path in PerSpec folder"""
    project_root = get_project_root()
    perspec_dir = project_root / "PerSpec"
    return perspec_dir / "test_coordination.db"

def add_session_column():
    """Add session_id column to compilation_errors table"""
    db_path = get_db_path()
    
    if not db_path.exists():
        print(f"Database does not exist at: {db_path}")
        return False
    
    conn = sqlite3.connect(str(db_path))
    cursor = conn.cursor()
    
    try:
        # Check if compilation_errors table exists
        cursor.execute("""
            SELECT name FROM sqlite_master 
            WHERE type='table' AND name='compilation_errors'
        """)
        
        if not cursor.fetchone():
            print("Table compilation_errors does not exist")
            return False
        
        # Check if session_id column already exists
        cursor.execute("PRAGMA table_info(compilation_errors)")
        columns = cursor.fetchall()
        column_names = [col[1] for col in columns]
        
        if 'session_id' in column_names:
            print("Column session_id already exists")
            return True
        
        # Add session_id column
        cursor.execute("""
            ALTER TABLE compilation_errors 
            ADD COLUMN session_id TEXT
        """)
        
        conn.commit()
        print("Successfully added session_id column to compilation_errors table")
        return True
        
    except sqlite3.Error as e:
        print(f"Error adding column: {e}")
        conn.rollback()
        return False
    finally:
        conn.close()

if __name__ == "__main__":
    add_session_column()