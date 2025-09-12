#!/usr/bin/env python3
"""
View and analyze PlayMode logs captured by PerSpec framework.
Logs are stored in PerSpec/PlayModeLogs/ directory.
"""

import os
import sys
import argparse
from datetime import datetime
from pathlib import Path
import glob
import re

def get_perspec_root():
    """Get the PerSpec root directory."""
    # Script is in PerSpec/Coordination/Scripts/
    script_dir = Path(__file__).parent
    return script_dir.parent.parent  # Go up to PerSpec root

def format_timestamp(timestamp_str):
    """Format timestamp for display."""
    try:
        # Parse timestamp if it's in a specific format
        if '.' in timestamp_str:
            time_part = timestamp_str.split('[')[1].split(']')[0] if '[' in timestamp_str else timestamp_str
            return time_part
        return timestamp_str
    except:
        return timestamp_str

def parse_log_file(filepath):
    """Parse a PlayMode log file and return structured data."""
    logs = []
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            current_log = None
            for line in f:
                line = line.rstrip()
                if not line:
                    continue
                    
                # Check if this is a new log entry (starts with timestamp)
                if line.startswith('[') and '] [' in line:
                    if current_log:
                        logs.append(current_log)
                    
                    # Parse the log line
                    parts = line.split('] [', 2)
                    if len(parts) >= 3:
                        timestamp = parts[0].lstrip('[')
                        level = parts[1].rstrip(']').strip()
                        
                        # Extract frame number or thread indicator if present
                        frame = None
                        message = parts[2]
                        if message.startswith('Frame:') or message.startswith('Thread'):
                            frame_parts = message.split('] ', 1)
                            if len(frame_parts) == 2:
                                if message.startswith('Frame:'):
                                    frame = frame_parts[0].replace('Frame:', '').strip()
                                else:
                                    frame = 'Thread'
                                message = frame_parts[1]
                        
                        current_log = {
                            'timestamp': timestamp,
                            'level': level,
                            'frame': frame,
                            'message': message,
                            'stack_trace': []
                        }
                elif current_log:
                    # This is a continuation (stack trace)
                    current_log['stack_trace'].append(line)
            
            # Don't forget the last log
            if current_log:
                logs.append(current_log)
                
    except Exception as e:
        print(f"Error reading {filepath}: {e}")
    
    return logs

def is_compilation_error(message):
    """Check if a log message is a compilation error (CS error)."""
    # CS error codes: CS0001-CS9999
    cs_pattern = r'\bCS\d{4}\b'
    
    # Additional compilation-specific patterns
    compilation_patterns = [
        r'error CS\d{4}',
        r': error CS\d{4}',
        r'Compiler Error',
        r'compilation failed',
        r'All compiler errors'
    ]
    
    # Check for CS error code
    if re.search(cs_pattern, message):
        return True
    
    # Check for compilation patterns
    for pattern in compilation_patterns:
        if re.search(pattern, message, re.IGNORECASE):
            return True
    
    return False

def display_logs(logs, show_stack=False, filter_level=None, filter_errors=False):
    """Display logs in a formatted way."""
    for log in logs:
        # Filter by level if specified
        if filter_level and log['level'].lower() != filter_level.lower():
            continue
        
        # Filter for errors/exceptions if specified
        if filter_errors and log['level'] not in ['Error', 'Exception', 'Assert']:
            continue
            
        # Color coding for different log levels
        level_colors = {
            'Error': '\033[91m',      # Red
            'Exception': '\033[91m',  # Red
            'Warning': '\033[93m',    # Yellow
            'Info': '\033[92m',       # Green
            'Debug': '\033[94m'       # Blue
        }
        reset_color = '\033[0m'
        
        level_color = level_colors.get(log['level'], '')
        
        # Format the output
        if log['frame']:
            if log['frame'] == 'Thread':
                frame_str = "[Thread    ]"
            else:
                frame_str = f"[Frame: {log['frame']:>4}]"
        else:
            frame_str = ""
        print(f"{level_color}[{log['timestamp']}] [{log['level']:9}]{frame_str} {log['message']}{reset_color}")
        
        # Show stack trace if requested and available
        if show_stack and log['stack_trace']:
            for line in log['stack_trace']:
                print(f"  {line}")

def main():
    parser = argparse.ArgumentParser(description='View and analyze PlayMode logs')
    parser.add_argument('session', nargs='?', help='Specific session ID to view')
    parser.add_argument('-l', '--list', action='store_true', help='List all available sessions')
    parser.add_argument('-n', '--lines', type=int, default=50, help='Number of lines to show (default: 50)')
    parser.add_argument('--level', choices=['Info', 'Warning', 'Error', 'Exception', 'Debug'], 
                       help='Filter by log level')
    parser.add_argument('--errors', action='store_true', help='Show only compilation errors (CS errors)')
    parser.add_argument('--all-errors', action='store_true', help='Show all errors and exceptions')
    parser.add_argument('-s', '--stack', action='store_true', help='Show stack traces')
    parser.add_argument('-a', '--all', action='store_true', help='Show all logs (no limit)')
    parser.add_argument('--no-limit', action='store_true', help='Bypass default 50 line limit (useful with grep)')
    parser.add_argument('--tail', action='store_true', help='Show only the most recent logs')
    
    args = parser.parse_args()
    
    # Handle --no-limit flag
    if args.no_limit:
        args.lines = None
        args.all = True  # Treat --no-limit like --all
    
    # Handle --errors and --all-errors flags
    if args.errors:
        filter_errors = 'compilation'
        filter_level = None
    elif args.all_errors:
        filter_errors = 'all'
        filter_level = None
    else:
        filter_errors = False
        filter_level = args.level
    
    # Get PlayMode logs directory
    perspec_root = get_perspec_root()
    logs_dir = perspec_root / 'PlayModeLogs'
    
    if not logs_dir.exists():
        print(f"PlayMode logs directory not found: {logs_dir}")
        print("No PlayMode logs have been captured yet.")
        print("\nPlayMode logs are automatically captured when:")
        print("  1. You enter Play Mode in Unity")
        print("  2. Tests run in Play Mode")
        print("\nLogs are saved every 5 seconds and on Play Mode exit.")
        print("\nTo filter errors when logs exist:")
        print("  python test_playmode_logs.py --errors")
        return
    
    # Get all log files
    log_files = sorted(logs_dir.glob('*.txt'), key=lambda f: f.stat().st_mtime, reverse=True)
    
    if not log_files:
        print(f"No log files found in: {logs_dir}")
        print("\nTo filter errors when logs exist:")
        print("  python test_playmode_logs.py --errors     # Compilation errors only")
        print("  python test_playmode_logs.py --all-errors # All errors")
        return
    
    if args.list:
        # List all sessions
        print(f"\n=== Available PlayMode Log Sessions ===")
        print(f"Directory: {logs_dir}\n")
        
        sessions = {}
        for f in log_files:
            # Extract session ID from filename
            fname = f.name
            if 'session_' in fname:
                session_id = fname.split('session_')[1].split('_')[0]
                if session_id not in sessions:
                    sessions[session_id] = []
                sessions[session_id].append(f)
        
        for session_id, files in sessions.items():
            print(f"\nSession: {session_id}")
            total_size = sum(f.stat().st_size for f in files)
            print(f"  Files: {len(files)}")
            print(f"  Size: {total_size / 1024:.1f} KB")
            print(f"  Latest: {max(f.stat().st_mtime for f in files)}")
            
            # Show file list
            for f in sorted(files):
                size_kb = f.stat().st_size / 1024
                print(f"    - {f.name} ({size_kb:.1f} KB)")
        return
    
    # Process log files
    all_logs = []
    
    if args.session:
        # Filter to specific session
        session_files = [f for f in log_files if args.session in f.name]
        if not session_files:
            print(f"No logs found for session: {args.session}")
            return
        files_to_process = session_files
    else:
        # Use all files or just the most recent
        if args.tail:
            # Get the most recent session
            if log_files:
                latest_session = None
                for f in log_files:
                    if 'session_' in f.name:
                        session_id = f.name.split('session_')[1].split('_')[0]
                        latest_session = session_id
                        break
                
                if latest_session:
                    files_to_process = [f for f in log_files if latest_session in f.name]
                else:
                    files_to_process = [log_files[0]]  # Just the most recent file
            else:
                files_to_process = []
        else:
            files_to_process = log_files
    
    # Parse all selected files
    for filepath in files_to_process:
        logs = parse_log_file(filepath)
        all_logs.extend(logs)
    
    if not all_logs:
        print("No logs found to display")
        return
    
    # Sort by timestamp
    all_logs.sort(key=lambda x: x['timestamp'])
    
    # Apply error filtering BEFORE line limit and summary
    if filter_errors == 'compilation':
        # First filter to errors, then to compilation errors
        all_logs = [log for log in all_logs if log['level'] in ['Error', 'Exception', 'Assert']]
        all_logs = [log for log in all_logs if is_compilation_error(log.get('message', ''))]
    elif filter_errors == 'all':
        all_logs = [log for log in all_logs if log['level'] in ['Error', 'Exception', 'Assert']]
    elif filter_level:
        all_logs = [log for log in all_logs if log['level'] == filter_level]
    
    # Apply line limit unless --all is specified
    if not args.all and args.lines > 0:
        all_logs = all_logs[-args.lines:]
    
    # Display summary
    if filter_errors == 'compilation':
        print(f"\n=== PlayMode Compilation Errors ===")
        print(f"Compilation errors found: {len(all_logs)}")
    elif filter_errors == 'all':
        print(f"\n=== PlayMode All Errors ===")
        print(f"All errors found: {len(all_logs)}")
    else:
        print(f"\n=== PlayMode Logs ===")
        print(f"Total logs: {len(all_logs)}")
    
    # Count by level
    level_counts = {}
    for log in all_logs:
        level_counts[log['level']] = level_counts.get(log['level'], 0) + 1
    
    if level_counts:
        print(f"Log levels: ", end="")
        for level, count in sorted(level_counts.items()):
            print(f"{level}: {count}  ", end="")
        print("\n")
    else:
        if filter_errors == 'compilation':
            print("No compilation errors (CS errors) found.\n")
        elif filter_errors == 'all':
            print("No errors found.\n")
        else:
            print("No logs found matching filter criteria.\n")
    
    # Display the logs (no filtering needed here, already filtered above)
    display_logs(all_logs, show_stack=args.stack, filter_level=None, filter_errors=False)
    
    # Footer
    print(f"\n{'='*60}")
    print(f"Displayed {len(all_logs)} log entries")
    if not args.all and len(all_logs) == args.lines:
        print(f"(Limited to {args.lines} lines. Use --all to see everything)")

if __name__ == '__main__':
    main()