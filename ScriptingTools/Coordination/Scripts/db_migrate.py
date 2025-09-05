#!/usr/bin/env python3
"""
Database Migration Tool - Ensures all required tables exist
Run this after updating the PerSpec package to add any new tables
"""

# Prevent Python from creating .pyc files
import sys
import os
sys.dont_write_bytecode = True
os.environ['PYTHONDONTWRITEBYTECODE'] = '1'
import sqlite3
from pathlib import Path
from datetime import datetime

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

class DatabaseMigrator:
    def __init__(self):
        self.db_path = get_db_path()
        self.migrations_applied = []
        self.migrations_skipped = []
        self.errors = []
    
    def check_table_exists(self, cursor, table_name):
        """Check if a table exists in the database"""
        cursor.execute("""
            SELECT name FROM sqlite_master 
            WHERE type='table' AND name=?
        """, (table_name,))
        return cursor.fetchone() is not None
    
    def migrate_menu_item_requests(self, conn, cursor):
        """Add menu_item_requests table if missing"""
        if self.check_table_exists(cursor, 'menu_item_requests'):
            self.migrations_skipped.append('menu_item_requests (already exists)')
            return True
        
        try:
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
            self.migrations_applied.append('menu_item_requests table')
            return True
            
        except sqlite3.Error as e:
            self.errors.append(f"menu_item_requests: {e}")
            conn.rollback()
            return False
    
    def ensure_all_tables(self, conn, cursor):
        """Ensure all required tables exist"""
        required_tables = {
            'test_requests': self.check_table_exists(cursor, 'test_requests'),
            'test_results': self.check_table_exists(cursor, 'test_results'),
            'system_status': self.check_table_exists(cursor, 'system_status'),
            'execution_log': self.check_table_exists(cursor, 'execution_log'),
            'asset_refresh_requests': self.check_table_exists(cursor, 'asset_refresh_requests'),
            'console_logs': self.check_table_exists(cursor, 'console_logs'),
            'menu_item_requests': self.check_table_exists(cursor, 'menu_item_requests')
        }
        
        missing = [table for table, exists in required_tables.items() if not exists]
        existing = [table for table, exists in required_tables.items() if exists]
        
        return missing, existing
    
    def run_migrations(self):
        """Run all database migrations"""
        if not self.db_path.exists():
            print(f"Database does not exist at: {self.db_path}")
            print("Run 'python db_initializer.py' to create the database first")
            return False
        
        conn = sqlite3.connect(str(self.db_path))
        conn.execute("PRAGMA journal_mode=WAL")
        cursor = conn.cursor()
        
        print("=" * 60)
        print("PerSpec Database Migration Tool")
        print("=" * 60)
        print(f"Database: {self.db_path}")
        print(f"Started: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        print()
        
        try:
            # Check current state
            missing_before, existing_before = self.ensure_all_tables(conn, cursor)
            
            if missing_before:
                print(f"Missing tables: {', '.join(missing_before)}")
            else:
                print("All tables already exist")
            
            print()
            
            # Run migrations
            print("Running migrations...")
            
            # Add menu_item_requests table
            self.migrate_menu_item_requests(conn, cursor)
            
            # Future migrations can be added here
            # self.migrate_new_feature_table(conn, cursor)
            
            # Check final state
            missing_after, existing_after = self.ensure_all_tables(conn, cursor)
            
            print()
            print("Migration Results:")
            print("-" * 40)
            
            if self.migrations_applied:
                print("Applied migrations:")
                for migration in self.migrations_applied:
                    print(f"  [OK] {migration}")
            
            if self.migrations_skipped:
                print("Skipped migrations:")
                for migration in self.migrations_skipped:
                    print(f"  [-] {migration}")
            
            if self.errors:
                print("Errors:")
                for error in self.errors:
                    print(f"  [ERROR] {error}")
            
            print()
            print("Final table status:")
            for table in ['test_requests', 'test_results', 'system_status', 
                         'execution_log', 'asset_refresh_requests', 
                         'console_logs', 'menu_item_requests']:
                exists = self.check_table_exists(cursor, table)
                status = "[OK]" if exists else "[MISSING]"
                print(f"  {status} {table}")
            
            print()
            
            if not missing_after:
                print("SUCCESS: All required tables are present")
                return True
            else:
                print(f"WARNING: Still missing tables: {', '.join(missing_after)}")
                print("You may need to run db_initializer.py reset")
                return False
            
        except Exception as e:
            print(f"Migration error: {e}")
            return False
        finally:
            conn.close()
            print("=" * 60)

def main():
    migrator = DatabaseMigrator()
    success = migrator.run_migrations()
    
    if not success:
        sys.exit(1)

if __name__ == "__main__":
    main()