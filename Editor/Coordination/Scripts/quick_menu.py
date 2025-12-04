#!/usr/bin/env python3
"""
Quick Menu Runner - Execute Unity menu items from Python
"""

# Prevent Python from creating .pyc files
import sys
import os
sys.dont_write_bytecode = True
os.environ['PYTHONDONTWRITEBYTECODE'] = '1'
import argparse
import sqlite3
import time
from pathlib import Path
from datetime import datetime
from typing import Optional

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

class MenuCoordinator:
    def __init__(self):
        self.db_path = Path(get_db_path())
        if not self.db_path.exists():
            raise FileNotFoundError(f"Database not found at {self.db_path}")
    
    def _get_connection(self) -> sqlite3.Connection:
        """Get a database connection with proper settings"""
        conn = sqlite3.connect(str(self.db_path))
        conn.row_factory = sqlite3.Row
        conn.execute("PRAGMA journal_mode=WAL")
        return conn
    
    def submit_menu_request(self, menu_path: str, priority: int = 0) -> int:
        """Submit a menu item execution request"""
        conn = self._get_connection()
        try:
            cursor = conn.cursor()
            cursor.execute("""
                INSERT INTO menu_item_requests (menu_path, priority)
                VALUES (?, ?)
            """, (menu_path, priority))
            
            request_id = cursor.lastrowid
            conn.commit()
            
            print(f"Menu request #{request_id} submitted: {menu_path}")
            return request_id
            
        finally:
            conn.close()
    
    def get_request_status(self, request_id: int) -> Optional[dict]:
        """Get the status of a specific request"""
        conn = self._get_connection()
        try:
            cursor = conn.cursor()
            cursor.execute("""
                SELECT id, menu_path, status, created_at, started_at, 
                       completed_at, duration_seconds, result, error_message
                FROM menu_item_requests
                WHERE id = ?
            """, (request_id,))
            
            row = cursor.fetchone()
            if row:
                return dict(row)
            return None
            
        finally:
            conn.close()
    
    def cancel_request(self, request_id: int) -> bool:
        """Cancel a pending request"""
        conn = self._get_connection()
        try:
            cursor = conn.cursor()
            cursor.execute("""
                UPDATE menu_item_requests
                SET status = 'cancelled'
                WHERE id = ? AND status = 'pending'
            """, (request_id,))
            
            conn.commit()
            
            if cursor.rowcount > 0:
                print(f"Request {request_id} cancelled")
                return True
            else:
                print(f"Request {request_id} could not be cancelled (not found or not pending)")
                return False
                
        finally:
            conn.close()
    
    def wait_for_completion(self, request_id: int, timeout: int = 60) -> str:
        """Wait for a request to complete"""
        start_time = time.time()
        
        while time.time() - start_time < timeout:
            status = self.get_request_status(request_id)
            
            if not status:
                print(f"Request {request_id} not found")
                return "not_found"
            
            if status['status'] in ['completed', 'failed', 'cancelled']:
                return status['status']
            
            # Show progress
            elapsed = int(time.time() - start_time)
            print(f"\rWaiting for completion... {elapsed}s", end='', flush=True)
            
            time.sleep(0.5)
        
        print(f"\nRequest {request_id} timed out after {timeout} seconds")
        return "timeout"
    
    def get_pending_requests(self) -> list:
        """Get all pending requests"""
        conn = self._get_connection()
        try:
            cursor = conn.cursor()
            cursor.execute("""
                SELECT id, menu_path, priority, created_at
                FROM menu_item_requests
                WHERE status = 'pending'
                ORDER BY priority DESC, created_at ASC
            """)
            
            return [dict(row) for row in cursor.fetchall()]
            
        finally:
            conn.close()
    
    def print_summary(self, request_id: int):
        """Print a summary of the request"""
        status = self.get_request_status(request_id)
        
        if not status:
            print(f"Request {request_id} not found")
            return
        
        print(f"\n{'='*60}")
        print(f"Menu Request #{status['id']}")
        print(f"{'='*60}")
        print(f"Menu Path: {status['menu_path']}")
        print(f"Status: {status['status']}")
        print(f"Created: {status['created_at']}")
        
        if status['started_at']:
            print(f"Started: {status['started_at']}")
        
        if status['completed_at']:
            print(f"Completed: {status['completed_at']}")
            if status['duration_seconds']:
                print(f"Duration: {status['duration_seconds']:.2f} seconds")
        
        if status['result']:
            print(f"Result: {status['result']}")
        
        if status['error_message']:
            print(f"Error: {status['error_message']}")
        
        print(f"{'='*60}")

def main():
    # Ensure UTF-8 encoding for emoji/Unicode characters
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')

    parser = argparse.ArgumentParser(description='Execute Unity menu items from Python')
    
    subparsers = parser.add_subparsers(dest='command', help='Commands')
    
    # Execute command
    execute_parser = subparsers.add_parser('execute', help='Execute a menu item')
    execute_parser.add_argument('menu_path', help='Menu item path (e.g., "Assets/Create/C# Script")')
    execute_parser.add_argument('--priority', type=int, default=0, help='Priority (higher runs first)')
    execute_parser.add_argument('--wait', action='store_true', help='Wait for completion')
    execute_parser.add_argument('--timeout', type=int, default=60, help='Timeout in seconds')
    
    # Status command
    status_parser = subparsers.add_parser('status', help='Check request status')
    status_parser.add_argument('request_id', type=int, nargs='?', help='Request ID (or show all pending)')
    
    # Cancel command
    cancel_parser = subparsers.add_parser('cancel', help='Cancel a pending request')
    cancel_parser.add_argument('request_id', type=int, help='Request ID to cancel')
    
    # List command
    list_parser = subparsers.add_parser('list', help='List common Unity menu items')
    
    args = parser.parse_args()
    
    if args.command == 'list':
        print("\nCommon Unity Menu Items:")
        print("-" * 50)
        print("Assets/Create/C# Script")
        print("Assets/Create/Folder")
        print("Assets/Create/Material")
        print("Assets/Create/Prefab")
        print("Assets/Refresh")
        print("Assets/Reimport")
        print("Edit/Play")
        print("Edit/Pause")
        print("Edit/Stop")
        print("File/Save Project")
        print("File/Build Settings...")
        print("Window/General/Test Runner")
        print("Window/General/Console")
        print("Window/General/Project")
        print("Window/General/Hierarchy")
        print("Window/General/Inspector")
        print("Help/About Unity")
        print("\nNote: Use exact menu paths as shown in Unity")
        return
    
    if not args.command:
        parser.print_help()
        return
    
    coordinator = MenuCoordinator()
    
    try:
        if args.command == 'execute':
            request_id = coordinator.submit_menu_request(
                args.menu_path,
                args.priority
            )
            
            if args.wait:
                print(f"Waiting for completion (timeout: {args.timeout}s)...")
                final_status = coordinator.wait_for_completion(request_id, args.timeout)
                coordinator.print_summary(request_id)
                
                if final_status == 'failed':
                    sys.exit(1)
            else:
                print(f"Use 'python quick_menu.py status {request_id}' to check progress")
        
        elif args.command == 'status':
            if args.request_id:
                coordinator.print_summary(args.request_id)
            else:
                requests = coordinator.get_pending_requests()
                if requests:
                    print("\nPending menu requests:")
                    for req in requests:
                        print(f"  #{req['id']}: {req['menu_path']} (priority: {req['priority']})")
                else:
                    print("No pending menu requests")
        
        elif args.command == 'cancel':
            coordinator.cancel_request(args.request_id)
    
    except KeyboardInterrupt:
        print("\nOperation cancelled")
        sys.exit(1)
    except Exception as e:
        print(f"Error: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()