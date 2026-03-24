#!/usr/bin/env python3
"""
Unity Helper Coordinator - Submit scenario execution requests via PerSpec's database.
Follows the same pattern as quick_menu.py and test_coordinator.py.

Usage:
    python unityhelper_coordinator.py execute --file Assets/UnityHelper/scenarios/scenarios.json --wait
    python unityhelper_coordinator.py execute --file scenarios.json --target MyScenario --wait
    python unityhelper_coordinator.py execute --file scenarios.json --pending-only --wait
    python unityhelper_coordinator.py execute --file scenarios.json --failed-only --wait
    python unityhelper_coordinator.py status 123
    python unityhelper_coordinator.py status
    python unityhelper_coordinator.py list
    python unityhelper_coordinator.py cancel 123
"""

import sys
import os
sys.dont_write_bytecode = True
os.environ['PYTHONDONTWRITEBYTECODE'] = '1'

import argparse
import sqlite3
import time
import json
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


TABLE_NAME = "scenario_execution_requests"

CREATE_TABLE_SQL = f"""
CREATE TABLE IF NOT EXISTS {TABLE_NAME} (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    scenarios_file TEXT NOT NULL,
    target TEXT DEFAULT 'all',
    options TEXT,
    status TEXT NOT NULL DEFAULT 'pending'
        CHECK(status IN ('pending', 'running', 'completed', 'failed', 'cancelled')),
    priority INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    started_at TIMESTAMP,
    completed_at TIMESTAMP,
    result TEXT,
    error_message TEXT,
    tasks_total INTEGER DEFAULT 0,
    tasks_success INTEGER DEFAULT 0,
    tasks_failed INTEGER DEFAULT 0
)
"""


class ScenarioCoordinator:
    def __init__(self):
        self.db_path = Path(get_db_path())
        if not self.db_path.exists():
            raise FileNotFoundError(
                f"Database not found at {self.db_path}\n"
                "Run PerSpec db_initializer.py first."
            )
        self._ensure_table()

    def _get_connection(self) -> sqlite3.Connection:
        """Get a database connection with proper settings"""
        conn = sqlite3.connect(str(self.db_path))
        conn.row_factory = sqlite3.Row
        conn.execute("PRAGMA journal_mode=WAL")
        return conn

    def _ensure_table(self):
        """Create scenario_execution_requests table if it doesn't exist"""
        conn = self._get_connection()
        try:
            conn.execute(CREATE_TABLE_SQL)
            conn.commit()
        finally:
            conn.close()

    def submit_request(self, scenarios_file: str, target: str = "all",
                       pending_only: bool = False, failed_only: bool = False,
                       priority: int = 0) -> int:
        """Submit a scenario execution request"""
        conn = self._get_connection()
        try:
            cursor = conn.cursor()

            options = json.dumps({
                "scenarios_file": scenarios_file,
                "target": target,
                "pending_only": pending_only,
                "failed_only": failed_only
            })

            cursor.execute(f"""
                INSERT INTO {TABLE_NAME} (scenarios_file, target, options, priority)
                VALUES (?, ?, ?, ?)
            """, (scenarios_file, target, options, priority))

            request_id = cursor.lastrowid
            conn.commit()

            print(f"Scenario request #{request_id} submitted")
            print(f"  File: {scenarios_file}")
            print(f"  Target: {target}")
            if pending_only:
                print(f"  Mode: pending only")
            if failed_only:
                print(f"  Mode: failed only")
            return request_id

        finally:
            conn.close()

    def get_request_status(self, request_id: int) -> Optional[dict]:
        """Get the status of a specific request"""
        conn = self._get_connection()
        try:
            cursor = conn.cursor()
            cursor.execute(f"""
                SELECT id, scenarios_file, target, status, priority,
                       created_at, started_at, completed_at,
                       result, error_message,
                       tasks_total, tasks_success, tasks_failed
                FROM {TABLE_NAME}
                WHERE id = ?
            """, (request_id,))

            row = cursor.fetchone()
            if row:
                return dict(row)
            return None

        finally:
            conn.close()

    def wait_for_completion(self, request_id: int, timeout: int = 300) -> str:
        """Wait for a request to complete"""
        start_time = time.time()

        while time.time() - start_time < timeout:
            status = self.get_request_status(request_id)

            if not status:
                print(f"Request {request_id} not found")
                return "not_found"

            if status['status'] in ['completed', 'failed', 'cancelled']:
                return status['status']

            elapsed = int(time.time() - start_time)
            print(f"\rWaiting for completion... {elapsed}s (status: {status['status']})", end='', flush=True)

            time.sleep(0.5)

        print(f"\nRequest {request_id} timed out after {timeout} seconds")
        return "timeout"

    def cancel_request(self, request_id: int) -> bool:
        """Cancel a pending request"""
        conn = self._get_connection()
        try:
            cursor = conn.cursor()
            cursor.execute(f"""
                UPDATE {TABLE_NAME}
                SET status = 'cancelled', completed_at = CURRENT_TIMESTAMP
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

    def get_pending_requests(self) -> list:
        """Get all pending requests"""
        conn = self._get_connection()
        try:
            cursor = conn.cursor()
            cursor.execute(f"""
                SELECT id, scenarios_file, target, priority, created_at
                FROM {TABLE_NAME}
                WHERE status = 'pending'
                ORDER BY priority DESC, created_at ASC
            """)

            return [dict(row) for row in cursor.fetchall()]

        finally:
            conn.close()

    def get_recent_requests(self, limit: int = 10) -> list:
        """Get recent requests"""
        conn = self._get_connection()
        try:
            cursor = conn.cursor()
            cursor.execute(f"""
                SELECT id, scenarios_file, target, status, created_at,
                       tasks_total, tasks_success, tasks_failed
                FROM {TABLE_NAME}
                ORDER BY created_at DESC
                LIMIT ?
            """, (limit,))

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
        print(f"Scenario Request #{status['id']}")
        print(f"{'='*60}")
        print(f"File: {status['scenarios_file']}")
        print(f"Target: {status['target']}")
        print(f"Status: {status['status']}")
        print(f"Created: {status['created_at']}")

        if status['started_at']:
            print(f"Started: {status['started_at']}")

        if status['completed_at']:
            print(f"Completed: {status['completed_at']}")

        if status['tasks_total']:
            print(f"Tasks: {status['tasks_success']}/{status['tasks_total']} success, {status['tasks_failed']} failed")

        if status['result']:
            print(f"Result: {status['result']}")

        if status['error_message']:
            print(f"Error: {status['error_message']}")

        print(f"{'='*60}")


def main():
    parser = argparse.ArgumentParser(description='Unity Helper - Scenario Execution via PerSpec')

    subparsers = parser.add_subparsers(dest='command', help='Commands')

    # Execute command
    execute_parser = subparsers.add_parser('execute', help='Execute a scenario file')
    execute_parser.add_argument('--file', '-f', required=True, help='Path to scenarios.json')
    execute_parser.add_argument('--target', '-t', default='all', help='Scenario name or "all"')
    execute_parser.add_argument('--pending-only', action='store_true', help='Run only pending tasks')
    execute_parser.add_argument('--failed-only', action='store_true', help='Run only failed tasks (retry)')
    execute_parser.add_argument('--priority', type=int, default=0, help='Priority (higher runs first)')
    execute_parser.add_argument('--wait', '-w', action='store_true', help='Wait for completion')
    execute_parser.add_argument('--timeout', type=int, default=300, help='Wait timeout in seconds')

    # Status command
    status_parser = subparsers.add_parser('status', help='Check request status')
    status_parser.add_argument('request_id', type=int, nargs='?', help='Request ID (or show all pending)')

    # Cancel command
    cancel_parser = subparsers.add_parser('cancel', help='Cancel a pending request')
    cancel_parser.add_argument('request_id', type=int, help='Request ID to cancel')

    # List command
    list_parser = subparsers.add_parser('list', help='List recent requests')
    list_parser.add_argument('-n', type=int, default=10, help='Number of requests to show')
    list_parser.add_argument('--pending', action='store_true', help='Show only pending requests')

    args = parser.parse_args()

    if not args.command:
        parser.print_help()
        return

    try:
        coordinator = ScenarioCoordinator()
    except FileNotFoundError as e:
        print(f"Error: {e}")
        sys.exit(1)

    try:
        if args.command == 'execute':
            request_id = coordinator.submit_request(
                scenarios_file=args.file,
                target=args.target,
                pending_only=args.pending_only,
                failed_only=args.failed_only,
                priority=args.priority
            )

            if args.wait:
                print(f"Waiting for completion (timeout: {args.timeout}s)...")
                final_status = coordinator.wait_for_completion(request_id, args.timeout)
                print()
                coordinator.print_summary(request_id)

                if final_status == 'failed':
                    sys.exit(1)
            else:
                print(f"Use 'python unityhelper_coordinator.py status {request_id}' to check progress")

        elif args.command == 'status':
            if args.request_id:
                coordinator.print_summary(args.request_id)
            else:
                requests = coordinator.get_pending_requests()
                if requests:
                    print("\nPending scenario requests:")
                    for req in requests:
                        print(f"  #{req['id']}: {req['scenarios_file']} target={req['target']} (priority: {req['priority']})")
                else:
                    print("No pending scenario requests")

        elif args.command == 'cancel':
            coordinator.cancel_request(args.request_id)

        elif args.command == 'list':
            if args.pending:
                requests = coordinator.get_pending_requests()
                print(f"\nPending requests ({len(requests)}):")
            else:
                requests = coordinator.get_recent_requests(args.n)
                print(f"\nRecent requests (showing {len(requests)}):")

            print("-" * 80)
            for req in requests:
                status = req.get('status', 'pending')
                file = Path(req.get('scenarios_file', '')).name
                target = req.get('target', 'all')[:15]
                created = req.get('created_at', '')
                tasks = f"{req.get('tasks_success', 0)}/{req.get('tasks_total', 0)}" if req.get('tasks_total') else ""
                print(f"#{req['id']:4}  {status:10}  {file:30}  {target:15}  {tasks:8}  {created}")

            if not requests:
                print("  (none)")

    except KeyboardInterrupt:
        print("\nOperation cancelled")
        sys.exit(1)
    except Exception as e:
        print(f"Error: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()
