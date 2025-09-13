#!/usr/bin/env python3
"""
Add scene_hierarchy_requests table to the database for Unity scene export functionality.
"""

import sqlite3
import sys
from pathlib import Path
from datetime import datetime

def get_db_path():
    """Get database path in PerSpec folder"""
    script_dir = Path(__file__).parent
    perspec_dir = script_dir.parent.parent
    return perspec_dir / "test_coordination.db"

def add_scene_hierarchy_table():
    """Add the scene_hierarchy_requests table if it doesn't exist"""
    db_path = get_db_path()

    if not db_path.exists():
        print(f"Error: Database not found at {db_path}")
        return False

    try:
        conn = sqlite3.connect(str(db_path))
        cursor = conn.cursor()

        # Check if table already exists
        cursor.execute("""
            SELECT name FROM sqlite_master
            WHERE type='table' AND name='scene_hierarchy_requests'
        """)

        if cursor.fetchone():
            print("scene_hierarchy_requests table already exists")
            return True

        # Create the table
        cursor.execute("""
            CREATE TABLE scene_hierarchy_requests (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                request_type TEXT NOT NULL DEFAULT 'full',
                target_path TEXT,
                include_inactive INTEGER DEFAULT 1,
                include_components INTEGER DEFAULT 1,
                status TEXT NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'running', 'completed', 'failed', 'cancelled')),
                priority INTEGER DEFAULT 0,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                started_at TIMESTAMP,
                completed_at TIMESTAMP,
                output_file TEXT,
                error_message TEXT
            )
        """)

        # Create index for efficient polling
        cursor.execute("""
            CREATE INDEX idx_scene_hierarchy_requests_status_priority
            ON scene_hierarchy_requests (status, priority DESC, created_at ASC)
        """)

        conn.commit()
        print("Successfully created scene_hierarchy_requests table")

        # Show table info
        cursor.execute("PRAGMA table_info(scene_hierarchy_requests)")
        columns = cursor.fetchall()
        print("\nTable structure:")
        for col in columns:
            print(f"  {col[1]} ({col[2]})")

        return True

    except sqlite3.Error as e:
        print(f"Database error: {e}")
        return False
    except Exception as e:
        print(f"Unexpected error: {e}")
        return False
    finally:
        if conn:
            conn.close()

if __name__ == "__main__":
    success = add_scene_hierarchy_table()
    sys.exit(0 if success else 1)