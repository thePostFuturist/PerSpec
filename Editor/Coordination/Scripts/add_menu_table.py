#!/usr/bin/env python3
"""
Add menu_item_requests table to existing database
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

def add_menu_table():
    """Add menu_item_requests table to existing database"""
    db_path = get_db_path()
    
    if not db_path.exists():
        print(f"Database does not exist at: {db_path}")
        return False
    
    conn = sqlite3.connect(str(db_path))
    conn.execute("PRAGMA journal_mode=WAL")
    cursor = conn.cursor()
    
    try:
        # Check if table already exists
        cursor.execute("""
            SELECT name FROM sqlite_master 
            WHERE type='table' AND name='menu_item_requests'
        """)
        
        if cursor.fetchone():
            print("Table menu_item_requests already exists")
            return True
        
        # Create menu_item_requests table
        cursor.execute("""
            CREATE TABLE menu_item_requests (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                menu_path TEXT NOT NULL,
                status TEXT NOT NULL DEFAULT 'pending' CHECK(status IN ('pending', 'running', 'completed', 'failed', 'cancelled')),
                priority INTEGER DEFAULT 0,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                started_at TIMESTAMP,
                completed_at TIMESTAMP,
                duration_seconds REAL DEFAULT 0.0,
                result TEXT,
                error_message TEXT
            )
        """)
        
        # Create indexes
        cursor.execute("CREATE INDEX idx_menu_status ON menu_item_requests(status)")
        cursor.execute("CREATE INDEX idx_menu_created ON menu_item_requests(created_at DESC)")
        
        conn.commit()
        print("Successfully added menu_item_requests table")
        
        # Verify
        cursor.execute("SELECT * FROM menu_item_requests LIMIT 1")
        print("Table verified and ready for use")
        
        return True
        
    except sqlite3.Error as e:
        print(f"Error adding table: {e}")
        conn.rollback()
        return False
    finally:
        conn.close()

if __name__ == "__main__":
    add_menu_table()