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
        # Added errors='replace' to handle potential malformed characters in logs
        with open(filepath, 'r', encoding='utf-8', errors='replace') as f:
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
    """Check if a log message is a C# compilation error."""
    # Pattern for standard C# error codes, e.g., error CS0103:
    cs_pattern = r'error CS\d{4}:'
    if re.search(cs_pattern, message):
        return True
    return False

def is_general_error(log):
    """
    Check if a log entry is a general error, exception, or assertion.
    This is more comprehensive than just checking the log level.
    """
    # First, check the log level provided by Unity
    if log.get('level') in ['Error', 'Exception', 'Assert']:
        return True

    # Second, check the message content for common error-related keywords
    message = log.get('message', '').lower()
    error_keywords = [
        'exception:',          # Catches "NullReferenceException:", etc.
        'error:',              # Catches "Error:", "Shader error:"
        'failed',              # Catches "Assertion failed", "Test failed"
        'assertion failed',
        'unhandled exception',
        'crash'
    ]

    for keyword in error_keywords:
        if keyword in message:
            return True

    return False

def highlight_text(text, search_terms, ignore_case=False):
    """Highlight search terms in text."""
    if not search_terms:
        return text

    highlighted = text
    for term in search_terms:
        if ignore_case:
            # Case-insensitive highlighting
            pattern = re.compile(re.escape(term), re.IGNORECASE)
            highlighted = pattern.sub(lambda m: f'\033[103m\033[30m{m.group()}\033[0m', highlighted)
        else:
            # Case-sensitive highlighting
            highlighted = highlighted.replace(term, f'\033[103m\033[30m{term}\033[0m')

    return highlighted

def search_logs(logs, search_terms, match_any=False, ignore_case=False):
    """Search logs for specified terms."""
    matching_logs = []

    for log in logs:
        # Combine message and stack trace for searching
        search_text = log['message'] + ' '.join(log.get('stack_trace', []))

        if ignore_case:
            search_text = search_text.lower()
            terms_to_check = [term.lower() for term in search_terms]
        else:
            terms_to_check = search_terms

        # Check if terms match based on match_any flag
        if match_any:
            # At least one term must match
            if any(term in search_text for term in terms_to_check):
                matching_logs.append(log)
        else:
            # All terms must match
            if all(term in search_text for term in terms_to_check):
                matching_logs.append(log)

    return matching_logs

def display_logs(logs, show_stack=False, filter_level=None, filter_errors=False, search_terms=None, ignore_case=False):
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
        # Highlight search terms if provided
        message = log['message']
        if search_terms:
            message = highlight_text(message, search_terms, ignore_case)

        print(f"{level_color}[{log['timestamp']}] [{log['level']:9}]{frame_str} {message}{reset_color}")

        # Show stack trace if requested and available
        if show_stack and log['stack_trace']:
            for line in log['stack_trace']:
                # Highlight search terms in stack trace too
                if search_terms:
                    line = highlight_text(line, search_terms, ignore_case)
                print(f"  {line}")

def main():
    parser = argparse.ArgumentParser(description='View and analyze PlayMode logs')
    parser.add_argument('session', nargs='?', help='Specific session ID to view')
    parser.add_argument('-l', '--list', action='store_true', help='List all available sessions')
    parser.add_argument('-n', '--lines', type=int, default=50, help='Number of lines to show (default: 50)')
    parser.add_argument('--level', choices=['Info', 'Warning', 'Error', 'Exception', 'Debug'],
                       help='Filter by log level')
    parser.add_argument('--errors', action='store_true', help='Show all errors and exceptions')
    parser.add_argument('--cs-errors', action='store_true', help='Show only compilation errors (CS errors)')
    parser.add_argument('--all-errors', action='store_true', help='[Deprecated] Same as --errors')
    parser.add_argument('-s', '--stack', action='store_true', help='Show stack traces')
    parser.add_argument('-a', '--all', action='store_true', help='Show all logs (no limit)')
    parser.add_argument('--no-limit', action='store_true', help='Bypass default 50 line limit (useful with grep)')
    parser.add_argument('--tail', action='store_true', help='Show only the most recent logs')
    parser.add_argument('-S', '--search', nargs='+', metavar='KEYWORD',
                       help='Search for keywords in logs (multiple keywords allowed)')
    parser.add_argument('-i', '--ignore-case', action='store_true',
                       help='Case-insensitive search')
    parser.add_argument('--any', action='store_true',
                       help='Match ANY keyword instead of ALL keywords')

    args = parser.parse_args()

    # Handle --no-limit flag
    if args.no_limit:
        args.lines = None
        args.all = True  # Treat --no-limit like --all

    # Handle --errors, --cs-errors and --all-errors flags
    if args.cs_errors:
        filter_errors = 'compilation'
        filter_level = None
    elif args.errors or args.all_errors:  # --all-errors is now same as --errors
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
        print("  python test_playmode_logs.py --errors     # All errors and exceptions")
        print("  python test_playmode_logs.py --cs-errors  # Compilation errors only")
        print("\nTo search logs:")
        print("  python test_playmode_logs.py --search 'keyword'           # Search for single keyword")
        print("  python test_playmode_logs.py -S 'error' 'timeout' --any   # Search for ANY of the keywords")
        print("  python test_playmode_logs.py -S 'player' -i               # Case-insensitive search")
        return

    # Get all log files
    log_files = sorted(logs_dir.glob('*.txt'), key=lambda f: f.stat().st_mtime, reverse=True)

    if not log_files:
        print(f"No log files found in: {logs_dir}")
        print("\nTo filter errors when logs exist:")
        print("  python test_playmode_logs.py --errors     # All errors and exceptions")
        print("  python test_playmode_logs.py --cs-errors  # Compilation errors only")
        print("\nTo search logs:")
        print("  python test_playmode_logs.py --search 'keyword'           # Search for single keyword")
        print("  python test_playmode_logs.py -S 'error' 'timeout' --any   # Search for ANY of the keywords")
        print("  python test_playmode_logs.py -S 'player' -i               # Case-insensitive search")
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
        # Filter for compilation errors only
        all_logs = [log for log in all_logs if is_compilation_error(log.get('message', ''))]
    elif filter_errors == 'all':
        # Use the new, more comprehensive general error check
        all_logs = [log for log in all_logs if is_general_error(log)]
    elif filter_level:
        all_logs = [log for log in all_logs if log['level'] == filter_level]

    # Apply search filtering if specified
    if args.search:
        original_count = len(all_logs)
        all_logs = search_logs(all_logs, args.search, match_any=args.any, ignore_case=args.ignore_case)

        # Show search summary
        if args.search:
            search_mode = "ANY" if args.any else "ALL"
            case_mode = "case-insensitive" if args.ignore_case else "case-sensitive"
            print(f"\n=== Search Results ===\n")
            print(f"Keywords: {', '.join(args.search)}")
            print(f"Mode: {search_mode} keywords must match ({case_mode})")
            print(f"Found: {len(all_logs)} matches out of {original_count} logs\n")

    # Apply line limit unless --all is specified
    if not args.all and args.lines > 0:
        all_logs = all_logs[-args.lines:]

    # Display summary (only if not searching, as search has its own summary)
    if not args.search:
        if filter_errors == 'compilation':
            print(f"\n=== PlayMode Compilation Errors (CS Errors) ===")
            print(f"Compilation errors found: {len(all_logs)}")
        elif filter_errors == 'all':
            print(f"\n=== PlayMode Errors and Exceptions ===")
            print(f"Errors found: {len(all_logs)}")
        else:
            print(f"\n=== PlayMode Logs ===")
            print(f"Total logs: {len(all_logs)}")

    # Count by level
    level_counts = {}
    for log in all_logs:
        level = log['level']
        level_counts[level] = level_counts.get(level, 0) + 1

    if level_counts:
        print("\nLog counts by level:")
        for level, count in sorted(level_counts.items()):
            print(f"  {level}: {count}")

    print(f"\nShowing {len(all_logs)} logs")
    print("-" * 80)

    # Display the logs
    display_logs(all_logs, show_stack=args.stack, search_terms=args.search, ignore_case=args.ignore_case)

if __name__ == "__main__":
    main()