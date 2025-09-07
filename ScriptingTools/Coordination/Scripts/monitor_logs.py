#!/usr/bin/env python3
"""
Enhanced log monitoring system for Unity console logs.
Provides real-time monitoring even during compilation errors.
"""

import sqlite3
import argparse
import json
import time
import sys
import os
from datetime import datetime, timedelta
from pathlib import Path
from typing import List, Dict, Optional, Tuple
# Plain text output for LLM consumption - no color dependencies

class LogMonitor:
    """Monitor and analyze Unity console logs from SQLite database."""
    
    def __init__(self, db_path: str = None):
        """Initialize the log monitor with database connection."""
        if db_path is None:
            db_path = os.path.join(os.path.dirname(__file__), '..', '..', 'test_coordination.db')
        
        self.db_path = db_path
        self.last_log_id = 0
        self.session_filter = None
        self.level_filters = ['Info', 'Warning', 'Error', 'Exception', 'Assert']
        self.current_session_only = True  # Default to current session only
        
    def connect(self) -> sqlite3.Connection:
        """Create a new database connection."""
        conn = sqlite3.connect(self.db_path)
        conn.row_factory = sqlite3.Row
        return conn
    
    def get_current_session_id(self) -> Optional[str]:
        """Get the most recent session ID."""
        conn = self.connect()
        try:
            cursor = conn.cursor()
            # Get the most recent session
            cursor.execute("""
                SELECT session_id 
                FROM console_logs 
                ORDER BY id DESC 
                LIMIT 1
            """)
            row = cursor.fetchone()
            return row['session_id'] if row else None
        finally:
            conn.close()
    
    def get_recent_logs(self, minutes: int = 60, limit: int = 100, 
                       level_filter: List[str] = None, all_sessions: bool = False) -> List[Dict]:
        """Get recent logs from the database."""
        conn = self.connect()
        try:
            # Unity uses DateTime.UtcNow.Ticks (UTC-based ticks)
            # But the stored value appears to be 7 hours off from what we'd expect
            # This suggests Unity is actually using DateTime.Now.Ticks (local time)
            cutoff_time = datetime.now() - timedelta(minutes=minutes)
            # Use local timestamp directly, as Unity seems to use local time
            unix_timestamp = cutoff_time.timestamp()
            # Adjust for timezone to match Unity's local time ticks
            # Unity appears to be in UTC-7 timezone based on the 7-hour difference
            unity_ticks = int((unix_timestamp + 62135596800 - 25200) * 10000000)
            
            query = """
                SELECT id, session_id, log_level, message, 
                       truncated_stack, source_file, source_line, 
                       timestamp, context, frame_count, is_truncated
                FROM console_logs
                WHERE timestamp > ?
            """
            params = [unity_ticks]
            
            if level_filter:
                placeholders = ','.join(['?' for _ in level_filter])
                query += f" AND log_level IN ({placeholders})"
                params.extend(level_filter)
            
            # Filter by session unless showing all
            if not all_sessions and self.current_session_only:
                current_session = self.get_current_session_id()
                if current_session:
                    query += " AND session_id = ?"
                    params.append(current_session)
            elif self.session_filter:
                query += " AND session_id = ?"
                params.append(self.session_filter)
            
            query += " ORDER BY timestamp DESC LIMIT ?"
            params.append(limit)
            
            cursor = conn.cursor()
            cursor.execute(query, params)
            
            logs = []
            for row in cursor.fetchall():
                logs.append(dict(row))
            
            return logs
            
        finally:
            conn.close()
    
    def monitor_live(self, refresh_interval: float = 0.5, 
                    level_filter: List[str] = None, all_sessions: bool = False):
        """Monitor logs in real-time."""
        print("Starting live log monitoring...")
        print(f"Refresh interval: {refresh_interval}s")
        if level_filter:
            print(f"Filtering levels: {', '.join(level_filter)}")
        
        if not all_sessions:
            current_session = self.get_current_session_id()
            if current_session:
                print(f"Session: {current_session[:8]}... (current only)")
        else:
            print("Showing logs from all sessions")
            
        print("Press Ctrl+C to stop\n")
        
        try:
            while True:
                conn = self.connect()
                try:
                    query = """
                        SELECT id, session_id, log_level, message, 
                               truncated_stack, source_file, source_line, 
                               timestamp, context
                        FROM console_logs
                        WHERE id > ?
                    """
                    params = [self.last_log_id]
                    
                    if level_filter:
                        placeholders = ','.join(['?' for _ in level_filter])
                        query += f" AND log_level IN ({placeholders})"
                        params.extend(level_filter)
                    
                    # Filter by current session unless showing all
                    if not all_sessions and self.current_session_only:
                        current_session = self.get_current_session_id()
                        if current_session:
                            query += " AND session_id = ?"
                            params.append(current_session)
                    
                    query += " ORDER BY id ASC"
                    
                    cursor = conn.cursor()
                    cursor.execute(query, params)
                    
                    for row in cursor.fetchall():
                        self.last_log_id = row['id']
                        self.display_log(dict(row))
                    
                finally:
                    conn.close()
                
                time.sleep(refresh_interval)
                
        except KeyboardInterrupt:
            print("\nMonitoring stopped.")
    
    def display_log(self, log: Dict):
        """Display a single log entry with formatting."""
        timestamp = log['timestamp']
        
        # Convert timestamp to datetime
        if isinstance(timestamp, str):
            try:
                # Try to parse as ISO format
                timestamp = datetime.fromisoformat(timestamp)
            except:
                try:
                    # Try to parse as Unity tick string
                    timestamp = int(timestamp)
                    if timestamp > 621355968000000000:  # Roughly year 1970 in ticks
                        # This is likely Unity ticks
                        unix_timestamp = (timestamp / 10000000) - 62135596800
                        timestamp = datetime.fromtimestamp(unix_timestamp)
                    else:
                        timestamp = datetime.fromtimestamp(timestamp)
                except:
                    timestamp = datetime.now()
        elif isinstance(timestamp, (int, float)):
            # Handle Unity ticks (100-nanosecond intervals since 0001-01-01)
            try:
                if timestamp > 621355968000000000:  # Roughly year 1970 in ticks
                    # This is likely Unity ticks
                    unix_timestamp = (timestamp / 10000000) - 62135596800
                    timestamp = datetime.fromtimestamp(unix_timestamp)
                else:
                    timestamp = datetime.fromtimestamp(timestamp)
            except Exception as e:
                print(f"Warning: Failed to convert timestamp {log['timestamp']}: {e}")
                timestamp = datetime.now()
        else:
            timestamp = datetime.now()
        
        # Ensure timestamp is a datetime object
        if not isinstance(timestamp, datetime):
            timestamp = datetime.now()
        
        time_str = timestamp.strftime('%H:%M:%S.%f')[:-3]
        
        # Get log level for formatting
        level = log['log_level']
        
        # Format the message
        message = log['message']
        if len(message) > 150:
            message = message[:147] + '...'
        
        # Check for compilation context
        context = log.get('context', '')
        is_compilation = context and 'Compilation:True' in context
        compilation_marker = "[C] " if is_compilation else ""
        
        print(f"[{time_str}] {compilation_marker}[{level:7}] {message}")
        
        # Show source location if available
        if log.get('source_file'):
            source = f"  -> {log['source_file']}"
            if log.get('source_line'):
                source += f":{log['source_line']}"
            try:
                print(source)
            except UnicodeEncodeError:
                # Fallback for encoding issues
                print(source.encode('ascii', 'replace').decode('ascii'))
        
        # Show truncated stack trace for errors
        if level in ['Error', 'Exception'] and log.get('truncated_stack'):
            stack_lines = log['truncated_stack'].split('\n')[:3]
            for line in stack_lines:
                if line.strip():
                    print(f"    {line.strip()}")
    
    def analyze_errors(self, hours: float = 1.0) -> Dict:
        """Analyze error patterns in recent logs."""
        conn = self.connect()
        try:
            cutoff_time = datetime.now() - timedelta(hours=hours)
            unix_timestamp = cutoff_time.timestamp()
            # Adjust for Unity's apparent UTC-7 timezone
            unity_ticks = int((unix_timestamp + 62135596800 - 25200) * 10000000)
            
            # Get error counts by type
            cursor = conn.cursor()
            cursor.execute("""
                SELECT log_level, COUNT(*) as count
                FROM console_logs
                WHERE timestamp > ? AND log_level IN ('Error', 'Exception', 'Assert')
                GROUP BY log_level
            """, [unity_ticks])
            
            error_counts = {row['log_level']: row['count'] for row in cursor.fetchall()}
            
            # Get compilation errors
            cursor.execute("""
                SELECT COUNT(*) as count
                FROM console_logs
                WHERE timestamp > ? AND context LIKE '%Compilation:True%Errors:True%'
            """, [unity_ticks])
            
            compilation_errors = cursor.fetchone()['count']
            
            # Get most frequent error messages
            cursor.execute("""
                SELECT message, COUNT(*) as count
                FROM console_logs
                WHERE timestamp > ? AND log_level IN ('Error', 'Exception')
                GROUP BY message
                ORDER BY count DESC
                LIMIT 10
            """, [unity_ticks])
            
            top_errors = []
            for row in cursor.fetchall():
                message = row['message']
                if len(message) > 100:
                    message = message[:97] + '...'
                top_errors.append({'message': message, 'count': row['count']})
            
            return {
                'error_counts': error_counts,
                'compilation_errors': compilation_errors,
                'top_errors': top_errors,
                'time_range_hours': hours
            }
            
        finally:
            conn.close()
    
    def export_logs(self, output_file: str, hours: float = 1.0, 
                   format: str = 'json') -> int:
        """Export logs to a file."""
        logs = self.get_recent_logs(minutes=int(hours * 60), limit=10000)
        
        if format == 'json':
            with open(output_file, 'w') as f:
                json.dump(logs, f, indent=2, default=str)
        elif format == 'txt':
            with open(output_file, 'w') as f:
                f.write(f"Unity Console Logs Export\n")
                f.write(f"Generated: {datetime.now().isoformat()}\n")
                f.write(f"Time Range: Last {hours} hours\n")
                f.write("=" * 80 + "\n\n")
                
                for log in logs:
                    timestamp = log.get('timestamp', '')
                    if isinstance(timestamp, str):
                        try:
                            timestamp = datetime.fromisoformat(timestamp)
                            timestamp = timestamp.strftime('%Y-%m-%d %H:%M:%S.%f')[:-3]
                        except:
                            pass
                    
                    f.write(f"[{timestamp}] [{log.get('log_level', 'Unknown')}]\n")
                    f.write(f"{log.get('message', '')}\n")
                    
                    if log.get('source_file'):
                        f.write(f"  Source: {log['source_file']}")
                        if log.get('source_line'):
                            f.write(f":{log['source_line']}")
                        f.write("\n")
                    
                    if log.get('truncated_stack'):
                        f.write("  Stack Trace:\n")
                        for line in log['truncated_stack'].split('\n'):
                            if line.strip():
                                f.write(f"    {line}\n")
                    
                    f.write("\n")
        
        print(f"Exported {len(logs)} logs to {output_file}")
        return len(logs)
    
    def get_session_info(self) -> List[Dict]:
        """Get information about all logging sessions."""
        conn = self.connect()
        try:
            cursor = conn.cursor()
            cursor.execute("""
                WITH recent_sessions AS (
                    SELECT DISTINCT session_id, MAX(id) as max_id
                    FROM console_logs
                    GROUP BY session_id
                    ORDER BY max_id DESC
                    LIMIT 4
                )
                SELECT cl.session_id, 
                       MIN(cl.timestamp) as start_time,
                       MAX(cl.timestamp) as end_time,
                       COUNT(*) as log_count,
                       SUM(CASE WHEN cl.log_level = 'Error' THEN 1 ELSE 0 END) as error_count,
                       SUM(CASE WHEN cl.log_level = 'Warning' THEN 1 ELSE 0 END) as warning_count
                FROM console_logs cl
                INNER JOIN recent_sessions rs ON cl.session_id = rs.session_id
                GROUP BY cl.session_id
                ORDER BY rs.max_id DESC
            """)
            
            sessions = []
            for row in cursor.fetchall():
                sessions.append(dict(row))
            
            return sessions
            
        finally:
            conn.close()
    
    def cleanup_old_logs(self, days: int = 7) -> int:
        """Clean up logs older than specified days."""
        conn = self.connect()
        try:
            cutoff_time = datetime.now() - timedelta(days=days)
            unix_timestamp = cutoff_time.timestamp()
            # Adjust for Unity's apparent UTC-7 timezone
            unity_ticks = int((unix_timestamp + 62135596800 - 25200) * 10000000)
            
            cursor = conn.cursor()
            cursor.execute("""
                DELETE FROM console_logs
                WHERE timestamp < ?
            """, [unity_ticks])
            
            deleted = cursor.rowcount
            conn.commit()
            
            if deleted > 0:
                # Vacuum to reclaim space
                cursor.execute("VACUUM")
                conn.commit()
            
            return deleted
            
        finally:
            conn.close()


def main():
    """Main entry point for the log monitor."""
    parser = argparse.ArgumentParser(description='Monitor Unity console logs')
    
    subparsers = parser.add_subparsers(dest='command', help='Commands')
    
    # Live monitoring
    live_parser = subparsers.add_parser('live', help='Monitor logs in real-time')
    live_parser.add_argument('-r', '--refresh', type=float, default=0.5,
                            help='Refresh interval in seconds (default: 0.5)')
    live_parser.add_argument('-l', '--level', nargs='+', 
                            choices=['Info', 'Warning', 'Error', 'Exception', 'Assert'],
                            help='Filter by log levels')
    live_parser.add_argument('-a', '--all', action='store_true',
                            help='Show logs from all sessions (default: current session only)')
    
    # Recent logs
    recent_parser = subparsers.add_parser('recent', help='Show recent logs')
    recent_parser.add_argument('-m', '--minutes', type=int, default=60,
                              help='Time range in minutes (default: 60)')
    recent_parser.add_argument('-n', '--limit', type=int, default=50,
                              help='Maximum number of logs (default: 50)')
    recent_parser.add_argument('-l', '--level', nargs='+',
                              choices=['Info', 'Warning', 'Error', 'Exception', 'Assert'],
                              help='Filter by log levels')
    recent_parser.add_argument('-a', '--all', action='store_true',
                              help='Show logs from all sessions (default: current session only)')
    
    # Error analysis
    analyze_parser = subparsers.add_parser('analyze', help='Analyze error patterns')
    analyze_parser.add_argument('-t', '--hours', type=float, default=1.0,
                               help='Time range in hours (default: 1.0)')
    
    # Export logs
    export_parser = subparsers.add_parser('export', help='Export logs to file')
    export_parser.add_argument('output', help='Output file path')
    export_parser.add_argument('-t', '--hours', type=float, default=1.0,
                              help='Time range in hours (default: 1.0)')
    export_parser.add_argument('-f', '--format', choices=['json', 'txt'],
                              default='txt', help='Output format (default: txt)')
    
    # Session info
    session_parser = subparsers.add_parser('sessions', help='Show session information')
    
    # Cleanup
    cleanup_parser = subparsers.add_parser('cleanup', help='Clean up old logs')
    cleanup_parser.add_argument('-d', '--days', type=int, default=7,
                               help='Delete logs older than days (default: 7)')
    
    args = parser.parse_args()
    
    # Create monitor instance
    monitor = LogMonitor()
    
    if args.command == 'live':
        monitor.monitor_live(args.refresh, args.level, args.all)
    
    elif args.command == 'recent':
        logs = monitor.get_recent_logs(args.minutes, args.limit, args.level, args.all)
        for log in reversed(logs):
            monitor.display_log(log)
    
    elif args.command == 'analyze':
        analysis = monitor.analyze_errors(args.hours)
        
        print(f"\n=== Error Analysis ({analysis['time_range_hours']} hours) ===")
        
        print("\nError Counts:")
        for level, count in analysis['error_counts'].items():
            print(f"  {level}: {count}")
        
        if analysis['compilation_errors'] > 0:
            print(f"\nCompilation Errors: {analysis['compilation_errors']}")
        
        if analysis['top_errors']:
            print("\nTop Error Messages:")
            for i, error in enumerate(analysis['top_errors'], 1):
                print(f"  {i}. ({error['count']}x) {error['message']}")
    
    elif args.command == 'export':
        count = monitor.export_logs(args.output, args.hours, args.format)
        print(f"Exported {count} logs to {args.output}")
    
    elif args.command == 'sessions':
        sessions = monitor.get_session_info()
        
        print("\n=== Logging Sessions ===")
        for session in sessions:
            start = session['start_time']
            end = session['end_time']
            
            try:
                if isinstance(start, str):
                    start = datetime.fromisoformat(start)
                elif isinstance(start, (int, float)):
                    start = datetime.fromtimestamp(start)
                    
                if isinstance(end, str):
                    end = datetime.fromisoformat(end)
                elif isinstance(end, (int, float)):
                    end = datetime.fromtimestamp(end)
                
                duration = (end - start).total_seconds() / 60
            except Exception:
                duration = 0
            
            print(f"\nSession: {session['session_id'][:8]}...")
            try:
                print(f"  Start: {start.strftime('%Y-%m-%d %H:%M:%S')}")
            except:
                print(f"  Start: {start}")
            print(f"  Duration: {duration:.1f} minutes")
            print(f"  Logs: {session['log_count']} total")
            print(f"  Errors: {session['error_count']}, Warnings: {session['warning_count']}")
    
    elif args.command == 'cleanup':
        deleted = monitor.cleanup_old_logs(args.days)
        print(f"Deleted {deleted} logs older than {args.days} days")
    
    else:
        parser.print_help()


if __name__ == '__main__':
    main()