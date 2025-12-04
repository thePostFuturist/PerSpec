#!/usr/bin/env python3
"""
Scene Hierarchy Exporter - Export Unity scene hierarchy to JSON
"""

# Prevent Python from creating .pyc files
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
from typing import Optional, Dict, Any

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

def get_output_dir():
    """Get output directory for hierarchy exports"""
    project_root = get_project_root()
    return project_root / "PerSpec" / "SceneHierarchy"

class SceneHierarchyExporter:
    def __init__(self):
        self.db_path = Path(get_db_path())
        self.output_dir = get_output_dir()

        if not self.db_path.exists():
            raise FileNotFoundError(f"Database not found at {self.db_path}")

        # Ensure output directory exists
        self.output_dir.mkdir(parents=True, exist_ok=True)

    def _get_connection(self) -> sqlite3.Connection:
        """Get a database connection with proper settings"""
        conn = sqlite3.connect(str(self.db_path))
        conn.row_factory = sqlite3.Row
        conn.execute("PRAGMA journal_mode=WAL")
        return conn

    def ensure_table_exists(self) -> bool:
        """Check if scene_hierarchy_requests table exists"""
        conn = self._get_connection()
        try:
            cursor = conn.cursor()
            cursor.execute("""
                SELECT name FROM sqlite_master
                WHERE type='table' AND name='scene_hierarchy_requests'
            """)

            if not cursor.fetchone():
                print("Error: scene_hierarchy_requests table does not exist.")
                print("Please run: python PerSpec/Coordination/Scripts/add_scene_hierarchy_table.py")
                return False

            return True
        finally:
            conn.close()

    def submit_export_request(self, request_type: str = "full", target_path: str = None,
                            include_inactive: bool = True, include_components: bool = True,
                            priority: int = 0) -> int:
        """Submit a scene hierarchy export request"""
        if not self.ensure_table_exists():
            return -1

        conn = self._get_connection()
        try:
            cursor = conn.cursor()
            cursor.execute("""
                INSERT INTO scene_hierarchy_requests
                (request_type, target_path, include_inactive, include_components, priority)
                VALUES (?, ?, ?, ?, ?)
            """, (request_type, target_path, 1 if include_inactive else 0,
                  1 if include_components else 0, priority))

            request_id = cursor.lastrowid
            conn.commit()

            print(f"Hierarchy export request #{request_id} submitted")
            print(f"  Type: {request_type}")
            if target_path:
                print(f"  Target: {target_path}")
            print(f"  Include inactive: {include_inactive}")
            print(f"  Include components: {include_components}")

            return request_id

        finally:
            conn.close()

    def get_request_status(self, request_id: int) -> Optional[Dict[str, Any]]:
        """Get the status of a specific request"""
        conn = self._get_connection()
        try:
            cursor = conn.cursor()
            cursor.execute("""
                SELECT id, request_type, target_path, status, created_at,
                       started_at, completed_at, output_file, error_message
                FROM scene_hierarchy_requests
                WHERE id = ?
            """, (request_id,))

            row = cursor.fetchone()
            if row:
                return dict(row)
            return None

        finally:
            conn.close()

    def wait_for_completion(self, request_id: int, timeout: int = 60) -> str:
        """Wait for a request to complete"""
        start_time = time.time()

        print(f"Waiting for export request #{request_id} to complete...")

        while time.time() - start_time < timeout:
            status = self.get_request_status(request_id)

            if not status:
                print(f"Request {request_id} not found")
                return "not_found"

            if status['status'] in ['completed', 'failed', 'cancelled']:
                return status['status']

            # Show progress
            elapsed = int(time.time() - start_time)
            print(f"\rWaiting... {elapsed}s", end='', flush=True)

            time.sleep(0.5)

        print(f"\nRequest {request_id} timed out after {timeout} seconds")
        return "timeout"

    def get_latest_export(self) -> Optional[Path]:
        """Get the most recent export file"""
        if not self.output_dir.exists():
            return None

        json_files = list(self.output_dir.glob("hierarchy_*.json"))
        if not json_files:
            return None

        # Sort by modification time, most recent first
        json_files.sort(key=lambda x: x.stat().st_mtime, reverse=True)
        return json_files[0]

    def list_exports(self, limit: int = 10) -> list:
        """List recent export files"""
        if not self.output_dir.exists():
            return []

        json_files = list(self.output_dir.glob("hierarchy_*.json"))
        json_files.sort(key=lambda x: x.stat().st_mtime, reverse=True)

        exports = []
        for file_path in json_files[:limit]:
            stat = file_path.stat()
            exports.append({
                'name': file_path.name,
                'path': str(file_path),
                'size': stat.st_size,
                'modified': datetime.fromtimestamp(stat.st_mtime)
            })

        return exports

    def display_json(self, file_path: Path, pretty: bool = True):
        """Display JSON content from export file"""
        try:
            with open(file_path, 'r') as f:
                data = json.load(f)

            if pretty:
                print(json.dumps(data, indent=2))
            else:
                print(json.dumps(data))
        except Exception as e:
            print(f"Error reading JSON file: {e}")

    def print_summary(self, request_id: int):
        """Print a summary of the export request"""
        status = self.get_request_status(request_id)

        if not status:
            print(f"Request {request_id} not found")
            return

        print(f"\nExport Request #{request_id}")
        print("-" * 40)
        print(f"Type: {status['request_type']}")
        if status['target_path']:
            print(f"Target: {status['target_path']}")
        print(f"Status: {status['status']}")
        print(f"Created: {status['created_at']}")

        if status['started_at']:
            print(f"Started: {status['started_at']}")

        if status['completed_at']:
            print(f"Completed: {status['completed_at']}")

        if status['output_file']:
            file_path = Path(status['output_file'])
            if file_path.exists():
                size_mb = file_path.stat().st_size / (1024 * 1024)
                print(f"Output: {file_path.name} ({size_mb:.2f} MB)")
            else:
                print(f"Output: {status['output_file']} (file not found)")

        if status['error_message']:
            print(f"Error: {status['error_message']}")

def main():
    # Ensure UTF-8 encoding for emoji/Unicode characters
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')

    parser = argparse.ArgumentParser(description='Export Unity scene hierarchy to JSON')
    subparsers = parser.add_subparsers(dest='command', help='Commands')

    # Export command
    export_parser = subparsers.add_parser('export', help='Export scene hierarchy')
    export_parser.add_argument('type', choices=['full', 'object'], help='Export type')
    export_parser.add_argument('target', nargs='?', help='GameObject path for object export')
    export_parser.add_argument('--no-inactive', action='store_true', help='Exclude inactive GameObjects')
    export_parser.add_argument('--no-components', action='store_true', help='Exclude component details')
    export_parser.add_argument('--priority', type=int, default=0, help='Request priority')
    export_parser.add_argument('--wait', action='store_true', help='Wait for completion')
    export_parser.add_argument('--timeout', type=int, default=60, help='Wait timeout in seconds')
    export_parser.add_argument('--show', action='store_true', help='Show JSON output after export')

    # Status command
    status_parser = subparsers.add_parser('status', help='Check request status')
    status_parser.add_argument('request_id', type=int, help='Request ID')

    # Latest command
    latest_parser = subparsers.add_parser('latest', help='Get latest export')
    latest_parser.add_argument('--show', action='store_true', help='Display JSON content')
    latest_parser.add_argument('--compact', action='store_true', help='Compact JSON output')

    # List command
    list_parser = subparsers.add_parser('list', help='List export files')
    list_parser.add_argument('-n', type=int, default=10, help='Number of files to list')

    # Clean command
    clean_parser = subparsers.add_parser('clean', help='Clean old export files')
    clean_parser.add_argument('--keep', type=int, default=5, help='Number of files to keep')
    clean_parser.add_argument('--confirm', action='store_true', help='Confirm deletion')

    args = parser.parse_args()

    if not args.command:
        parser.print_help()
        return

    try:
        exporter = SceneHierarchyExporter()

        if args.command == 'export':
            # Determine request type
            request_type = 'full' if args.type == 'full' else 'single_object'
            target_path = args.target if args.type == 'object' else None

            if args.type == 'object' and not target_path:
                print("Error: GameObject path required for object export")
                return

            # Submit export request
            request_id = exporter.submit_export_request(
                request_type=request_type,
                target_path=target_path,
                include_inactive=not args.no_inactive,
                include_components=not args.no_components,
                priority=args.priority
            )

            if request_id < 0:
                return

            # Wait for completion if requested
            if args.wait:
                status = exporter.wait_for_completion(request_id, args.timeout)
                print(f"\nExport {status}")

                if status == 'completed':
                    exporter.print_summary(request_id)

                    # Show JSON if requested
                    if args.show:
                        latest = exporter.get_latest_export()
                        if latest:
                            print("\nExported JSON:")
                            print("-" * 40)
                            exporter.display_json(latest, pretty=True)

        elif args.command == 'status':
            exporter.print_summary(args.request_id)

        elif args.command == 'latest':
            latest = exporter.get_latest_export()
            if latest:
                print(f"Latest export: {latest.name}")
                size_mb = latest.stat().st_size / (1024 * 1024)
                print(f"Size: {size_mb:.2f} MB")
                print(f"Path: {latest}")

                if args.show:
                    print("\nContent:")
                    print("-" * 40)
                    exporter.display_json(latest, pretty=not args.compact)
            else:
                print("No export files found")

        elif args.command == 'list':
            exports = exporter.list_exports(args.n)
            if exports:
                print(f"Recent exports (showing {len(exports)}):")
                print("-" * 60)
                for exp in exports:
                    size_mb = exp['size'] / (1024 * 1024)
                    print(f"{exp['name']:30} {size_mb:8.2f} MB  {exp['modified']:%Y-%m-%d %H:%M:%S}")
            else:
                print("No export files found")

        elif args.command == 'clean':
            output_dir = exporter.output_dir
            if output_dir.exists():
                json_files = list(output_dir.glob("hierarchy_*.json"))
                json_files.sort(key=lambda x: x.stat().st_mtime, reverse=True)

                if len(json_files) > args.keep:
                    files_to_delete = json_files[args.keep:]

                    print(f"Found {len(json_files)} export files")
                    print(f"Will delete {len(files_to_delete)} old files, keeping {args.keep} most recent")

                    if not args.confirm:
                        print("Use --confirm to proceed with deletion")
                        return

                    for file_path in files_to_delete:
                        try:
                            file_path.unlink()
                            print(f"Deleted: {file_path.name}")
                        except Exception as e:
                            print(f"Error deleting {file_path.name}: {e}")

                    print(f"Cleanup complete. Deleted {len(files_to_delete)} files")
                else:
                    print(f"Only {len(json_files)} files found, no cleanup needed")

    except Exception as e:
        print(f"Error: {e}")
        return 1

if __name__ == "__main__":
    sys.exit(main() or 0)