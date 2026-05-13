#!/usr/bin/env python3
"""
Test Results Viewer - View and analyze Unity test results from XML files
"""

# Prevent Python from creating .pyc files
import sys
import os
sys.dont_write_bytecode = True
os.environ['PYTHONDONTWRITEBYTECODE'] = '1'

import argparse
import xml.etree.ElementTree as ET
from pathlib import Path
from datetime import datetime
from typing import Optional, List, Dict
import json

def get_project_root():
    """Find Unity project root by looking for Assets folder"""
    current = Path.cwd()
    while current != current.parent:
        if (current / "Assets").exists():
            return current
        current = current.parent
    return Path.cwd()

def get_test_results_path():
    """Get the primary TestResults directory path (PerSpec/TestResults/)."""
    project_root = get_project_root()
    return project_root / "PerSpec" / "TestResults"


def _appdata_unity_candidates():
    """Mirror C# TestExecutor.cs:602-616 AppData fallback enumeration.

    Returns directories under %LocalAppData%Low that contain a TestResults.xml.
    Each is a candidate source for the Unity-default results XML.
    """
    local_app = os.environ.get("LOCALAPPDATA")
    if not local_app:
        return []
    appdata_low = Path(local_app + "Low")
    if not appdata_low.exists():
        return []

    project_name = get_project_root().name
    preferred = {project_name, "TestFramework"}
    candidates = []
    try:
        for company_dir in appdata_low.iterdir():
            if not company_dir.is_dir():
                continue
            for product_dir in company_dir.iterdir():
                if not product_dir.is_dir():
                    continue
                if (product_dir / "TestResults.xml").exists():
                    score = (2 if product_dir.name in preferred else 0) + \
                            (1 if company_dir.name == "DefaultCompany" else 0)
                    candidates.append((score, product_dir))
    except OSError:
        return []
    candidates.sort(key=lambda t: t[0], reverse=True)
    return [c for _, c in candidates]


def _import_appdata_xml_into_perspec(source_xml: Path, dest_dir: Path) -> Optional[Path]:
    """Copy Unity's AppData TestResults.xml into PerSpec/TestResults with a timestamp."""
    try:
        import shutil
        mtime = datetime.fromtimestamp(source_xml.stat().st_mtime)
        dest = dest_dir / f"TestResults_{mtime.strftime('%Y%m%d_%H%M%S')}.xml"
        dest_dir.mkdir(parents=True, exist_ok=True)
        shutil.copy2(str(source_xml), str(dest))
        print(f"[INFO] Imported {source_xml} -> {dest}")
        return dest
    except OSError as e:
        print(f"[WARN] Failed to import {source_xml}: {e}")
        return None

def parse_xml_file(xml_path: Path) -> Dict:
    """Parse a test results XML file"""
    try:
        tree = ET.parse(xml_path)
        root = tree.getroot()
        
        # Extract summary from root attributes
        summary = {
            'file': xml_path.name,
            'timestamp': datetime.fromtimestamp(xml_path.stat().st_mtime).strftime('%Y-%m-%d %H:%M:%S'),
            'total': int(root.get('total', 0)),
            'passed': int(root.get('passed', 0)),
            'failed': int(root.get('failed', 0)),
            'inconclusive': int(root.get('inconclusive', 0)),
            'skipped': int(root.get('skipped', 0)),
            'duration': float(root.get('duration', 0))
        }
        
        # Extract individual test results
        tests = []
        for test_case in root.findall('.//test-case'):
            test_info = {
                'name': test_case.get('fullname', test_case.get('name', 'Unknown')),
                'result': test_case.get('result', 'Unknown'),
                'duration': float(test_case.get('duration', 0)),
                'classname': test_case.get('classname', ''),
                'methodname': test_case.get('methodname', '')
            }
            
            # Get failure message if present
            failure = test_case.find('failure')
            if failure is not None:
                test_info['message'] = failure.find('message').text if failure.find('message') is not None else ''
                test_info['stack_trace'] = failure.find('stack-trace').text if failure.find('stack-trace') is not None else ''
            
            tests.append(test_info)
        
        summary['tests'] = tests
        return summary
        
    except Exception as e:
        return {
            'file': xml_path.name,
            'error': str(e),
            'timestamp': datetime.fromtimestamp(xml_path.stat().st_mtime).strftime('%Y-%m-%d %H:%M:%S')
        }

def list_result_files(limit: int = 10) -> List[Path]:
    """List available test result files.

    Looks in PerSpec/TestResults/ first. If empty (or if Unity's AppData copy
    is newer than anything we have), imports the AppData TestResults.xml into
    PerSpec/TestResults/ first so subsequent reads are consistent.
    """
    results_path = get_test_results_path()
    results_path.mkdir(parents=True, exist_ok=True)

    perspec_xmls = list(results_path.glob("*.xml"))
    perspec_latest_mtime = max(
        (x.stat().st_mtime for x in perspec_xmls),
        default=0.0,
    )

    # If AppData has a newer TestResults.xml than anything in PerSpec, import it.
    for appdata_dir in _appdata_unity_candidates():
        source = appdata_dir / "TestResults.xml"
        if source.exists() and source.stat().st_mtime > perspec_latest_mtime:
            imported = _import_appdata_xml_into_perspec(source, results_path)
            if imported is not None:
                perspec_xmls.append(imported)
                break  # only import from the highest-priority candidate

    xml_files = sorted(
        perspec_xmls,
        key=lambda x: x.stat().st_mtime,
        reverse=True,
    )

    return xml_files[:limit] if limit > 0 else xml_files

def display_summary(data: Dict, verbose: bool = False):
    """Display test results summary"""
    if 'error' in data:
        print(f"\n[ERROR] Failed to parse {data['file']}: {data['error']}")
        return
    
    # Header
    print(f"\n{'='*60}")
    print(f"Test Results: {data['file']}")
    print(f"Timestamp: {data['timestamp']}")
    print(f"{'='*60}")
    
    # Summary stats
    print(f"\nSummary:")
    print(f"  Total:        {data['total']}")
    print(f"  Passed:       {data['passed']}")
    print(f"  Failed:       {data['failed']}")
    print(f"  Inconclusive: {data['inconclusive']}")
    print(f"  Skipped:      {data['skipped']}")
    print(f"  Duration:     {data['duration']:.2f} seconds")
    
    # Show failed tests
    if data.get('tests'):
        failed_tests = [t for t in data['tests'] if t['result'] in ['Failed', 'Error']]
        if failed_tests:
            print(f"\nFailed Tests ({len(failed_tests)}):")
            for test in failed_tests:
                print(f"  [FAILED] {test['name']}")
                if verbose and test.get('message'):
                    print(f"     Message: {test['message']}")
                if verbose and test.get('stack_trace'):
                    print(f"     Stack: {test['stack_trace'][:200]}...")
        
        # Show inconclusive tests
        inconclusive_tests = [t for t in data['tests'] if t['result'] == 'Inconclusive']
        if inconclusive_tests:
            print(f"\nInconclusive Tests ({len(inconclusive_tests)}):")
            for test in inconclusive_tests:
                print(f"  [INCONCLUSIVE] {test['name']}")
        
        # Show passed tests if verbose
        if verbose:
            passed_tests = [t for t in data['tests'] if t['result'] == 'Passed']
            if passed_tests:
                print(f"\nPassed Tests ({len(passed_tests)}):")
                for test in passed_tests:
                    print(f"  [PASSED] {test['name']} ({test['duration']:.3f}s)")

def main():
    # Ensure UTF-8 encoding for emoji/Unicode characters
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')

    parser = argparse.ArgumentParser(description='View Unity test results')
    
    # Subcommands
    subparsers = parser.add_subparsers(dest='command', help='Commands')
    
    # Latest command
    latest_parser = subparsers.add_parser('latest', help='Show latest test results')
    latest_parser.add_argument('-v', '--verbose', action='store_true', help='Show detailed output')
    latest_parser.add_argument('--json', action='store_true', help='Output as JSON')
    
    # List command
    list_parser = subparsers.add_parser('list', help='List available test result files')
    list_parser.add_argument('-n', '--number', type=int, default=10, help='Number of files to list')
    
    # Show command (specific file)
    show_parser = subparsers.add_parser('show', help='Show specific test result file')
    show_parser.add_argument('filename', help='Name of the XML file to show')
    show_parser.add_argument('-v', '--verbose', action='store_true', help='Show detailed output')
    show_parser.add_argument('--json', action='store_true', help='Output as JSON')
    
    # Failed command (show only failed tests)
    failed_parser = subparsers.add_parser('failed', help='Show failed tests from recent runs')
    failed_parser.add_argument('-n', '--number', type=int, default=5, help='Number of recent files to check')
    failed_parser.add_argument('-v', '--verbose', action='store_true', help='Show error messages')
    
    # Stats command
    stats_parser = subparsers.add_parser('stats', help='Show statistics from recent test runs')
    stats_parser.add_argument('-n', '--number', type=int, default=10, help='Number of recent runs to analyze')
    
    # Clean command
    clean_parser = subparsers.add_parser('clean', help='Clean old test result files')
    clean_parser.add_argument('--keep', type=int, default=10, help='Number of recent files to keep')
    clean_parser.add_argument('--confirm', action='store_true', help='Confirm deletion')
    
    args = parser.parse_args()
    
    # Default to 'latest' if no command specified
    if not args.command:
        args.command = 'latest'
        args.verbose = False
        args.json = False
    
    # Execute commands
    if args.command == 'latest':
        files = list_result_files(1)
        if files:
            data = parse_xml_file(files[0])
            if args.json:
                print(json.dumps(data, indent=2, default=str))
            else:
                display_summary(data, args.verbose)
        else:
            print("No test result files found")
    
    elif args.command == 'list':
        files = list_result_files(args.number)
        if files:
            print(f"\nFound {len(files)} test result files:")
            print("-" * 60)
            for i, file in enumerate(files, 1):
                mtime = datetime.fromtimestamp(file.stat().st_mtime)
                size_kb = file.stat().st_size / 1024
                print(f"{i:3}. {file.name:<40} {mtime.strftime('%Y-%m-%d %H:%M:%S')} ({size_kb:.1f} KB)")
        else:
            print("No test result files found")
    
    elif args.command == 'show':
        results_path = get_test_results_path()
        file_path = results_path / args.filename
        if file_path.exists():
            data = parse_xml_file(file_path)
            if args.json:
                print(json.dumps(data, indent=2, default=str))
            else:
                display_summary(data, args.verbose)
        else:
            print(f"File not found: {args.filename}")
            print(f"Looking in: {results_path}")
    
    elif args.command == 'failed':
        files = list_result_files(args.number)
        all_failed = []
        
        for file in files:
            data = parse_xml_file(file)
            if 'tests' in data:
                failed = [t for t in data['tests'] if t['result'] in ['Failed', 'Error']]
                for test in failed:
                    test['file'] = file.name
                    test['timestamp'] = data['timestamp']
                    all_failed.extend(failed)
        
        if all_failed:
            print(f"\nFailed tests from {len(files)} recent runs:")
            print("=" * 60)
            
            # Group by file
            current_file = None
            for test in all_failed:
                if test['file'] != current_file:
                    current_file = test['file']
                    print(f"\n{test['file']} ({test['timestamp']}):")
                
                print(f"  [FAILED] {test['name']}")
                if args.verbose and test.get('message'):
                    print(f"     {test['message']}")
        else:
            print("No failed tests found in recent runs")
    
    elif args.command == 'stats':
        files = list_result_files(args.number)
        
        if files:
            total_stats = {
                'runs': len(files),
                'total_tests': 0,
                'total_passed': 0,
                'total_failed': 0,
                'total_duration': 0,
                'pass_rate': 0
            }
            
            print(f"\nStatistics from {len(files)} test runs:")
            print("=" * 60)
            
            for file in files:
                data = parse_xml_file(file)
                if 'error' not in data:
                    total_stats['total_tests'] += data['total']
                    total_stats['total_passed'] += data['passed']
                    total_stats['total_failed'] += data['failed']
                    total_stats['total_duration'] += data['duration']
            
            if total_stats['total_tests'] > 0:
                total_stats['pass_rate'] = (total_stats['total_passed'] / total_stats['total_tests']) * 100
            
            print(f"Total Runs:     {total_stats['runs']}")
            print(f"Total Tests:    {total_stats['total_tests']}")
            print(f"Total Passed:   {total_stats['total_passed']}")
            print(f"Total Failed:   {total_stats['total_failed']}")
            print(f"Pass Rate:      {total_stats['pass_rate']:.1f}%")
            print(f"Total Duration: {total_stats['total_duration']:.1f} seconds")
            print(f"Avg Duration:   {total_stats['total_duration']/total_stats['runs']:.1f} seconds per run")
        else:
            print("No test result files found")
    
    elif args.command == 'clean':
        files = list_result_files(0)  # Get all files
        
        if len(files) > args.keep:
            files_to_delete = files[args.keep:]
            
            print(f"\nFiles to delete ({len(files_to_delete)}):")
            for file in files_to_delete:
                print(f"  - {file.name}")
            
            if args.confirm or input("\nDelete these files? (y/N): ").lower() == 'y':
                for file in files_to_delete:
                    try:
                        file.unlink()
                        print(f"Deleted: {file.name}")
                    except Exception as e:
                        print(f"Error deleting {file.name}: {e}")
                print(f"\nDeleted {len(files_to_delete)} files, kept {args.keep} most recent")
            else:
                print("Deletion cancelled")
        else:
            print(f"Only {len(files)} files found, keeping all (threshold: {args.keep})")

if __name__ == "__main__":
    main()