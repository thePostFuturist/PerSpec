#!/usr/bin/env python3
"""
Mark all compilation errors as stale (for testing)
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

def mark_all_stale():
    """Mark all compilation errors as stale"""
    db_path = get_db_path()
    
    if not db_path.exists():
        print(f"Database does not exist at: {db_path}")
        return False
    
    conn = sqlite3.connect(str(db_path))
    cursor = conn.cursor()
    
    try:
        cursor.execute("""
            UPDATE compilation_errors 
            SET is_stale = 1 
            WHERE is_stale = 0
        """)
        
        updated = cursor.rowcount
        conn.commit()
        
        print(f"Marked {updated} errors as stale")
        return True
        
    except sqlite3.Error as e:
        print(f"Error: {e}")
        conn.rollback()
        return False
    finally:
        conn.close()

if __name__ == "__main__":
    mark_all_stale()