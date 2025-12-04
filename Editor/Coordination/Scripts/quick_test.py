#!/usr/bin/env python3
"""
Quick Test Runner - Simple interface for common test operations
"""


# Prevent Python from creating .pyc files
import sys
import os
sys.dont_write_bytecode = True
os.environ['PYTHONDONTWRITEBYTECODE'] = '1'
import sys
import argparse
import subprocess
import json
from test_coordinator import TestCoordinator, TestPlatform, TestRequestType

def check_compilation_errors():
    """Check if there are any compilation errors in Unity"""
    try:
        # Run quick_logs.py errors command and capture output
        script_dir = os.path.dirname(os.path.abspath(__file__))
        result = subprocess.run(
            [sys.executable, os.path.join(script_dir, 'quick_logs.py'), 'errors', '--json'],
            capture_output=True,
            text=True,
            timeout=10
        )
        
        if result.returncode == 0:
            # No errors found
            return False, None
        else:
            # Parse error output if in JSON format
            try:
                errors = json.loads(result.stdout)
                error_count = len(errors) if isinstance(errors, list) else 0
                return True, f"Found {error_count} compilation error(s)"
            except:
                # If not JSON, just check if there's output
                if result.stdout.strip():
                    return True, "Compilation errors detected"
                return False, None
    except subprocess.TimeoutExpired:
        print("Warning: Compilation check timed out")
        return False, None
    except Exception as e:
        print(f"Warning: Could not check for compilation errors: {e}")
        return False, None

def main():
    # Ensure UTF-8 encoding for emoji/Unicode characters
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')

    parser = argparse.ArgumentParser(description='Quick Unity test runner')
    parser.add_argument('action', choices=['all', 'class', 'method', 'category', 'status', 'cancel'],
                       help='Action to perform')
    parser.add_argument('target', nargs='?', help='Target (class/method/category name or request ID)')
    parser.add_argument('-p', '--platform', choices=['edit', 'play', 'both'], default='edit',
                       help='Test platform (default: edit)')
    parser.add_argument('--priority', type=int, default=0,
                       help='Priority level (higher runs first)')
    parser.add_argument('--wait', action='store_true',
                       help='Wait for test request processing (NOT full test execution)')
    parser.add_argument('--timeout', type=int, default=300,
                       help='Timeout in seconds (default: 300)')
    parser.add_argument('--focus', action='store_true',
                       help='Focus Unity window before running tests (Windows only)')
    parser.add_argument('--skip-compilation-check', action='store_true',
                       help='Skip checking for compilation errors before running tests')
    
    args = parser.parse_args()
    
    # Map platform strings to enum
    platform_map = {
        'edit': TestPlatform.EDIT_MODE,
        'play': TestPlatform.PLAY_MODE,
        'both': TestPlatform.BOTH
    }
    platform = platform_map[args.platform]
    
    coordinator = TestCoordinator()
    
    try:
        if args.action == 'status':
            if not args.target:
                # Show all pending requests
                requests = coordinator.get_pending_requests()
                if requests:
                    print("Pending test requests:")
                    for req in requests:
                        print(f"  #{req['id']}: {req['request_type']} on {req['test_platform']} "
                              f"(priority: {req['priority']})")
                else:
                    print("No pending test requests")
            else:
                # Show specific request status
                request_id = int(args.target)
                status = coordinator.get_request_status(request_id)
                if status:
                    coordinator.print_summary(request_id)
                else:
                    print(f"Request {request_id} not found")
        
        elif args.action == 'cancel':
            if not args.target:
                print("Error: Request ID required for cancel")
                sys.exit(1)
            request_id = int(args.target)
            coordinator.cancel_request(request_id)
        
        else:
            # Submit test request
            request_type_map = {
                'all': TestRequestType.ALL,
                'class': TestRequestType.CLASS,
                'method': TestRequestType.METHOD,
                'category': TestRequestType.CATEGORY
            }
            request_type = request_type_map[args.action]
            
            # For 'all' tests, target is optional
            test_filter = args.target if args.action != 'all' else None
            
            if args.action != 'all' and not test_filter:
                print(f"Error: {args.action} requires a target")
                sys.exit(1)
            
            # Check for compilation errors unless explicitly skipped
            if not args.skip_compilation_check:
                print("Checking for compilation errors...")
                has_errors, error_msg = check_compilation_errors()
                
                if has_errors:
                    print("\n" + "="*60)
                    print("WARNING: COMPILATION ERRORS DETECTED!")
                    print("="*60)
                    print(f"\n{error_msg}")
                    print("\nTests cannot run with compilation errors.")
                    print("Tests will be marked as INCONCLUSIVE.")
                    print("\nTo fix:")
                    print("1. Run: python PerSpec/Coordination/Scripts/quick_logs.py errors")
                    print("2. Fix the compilation errors")
                    print("3. Refresh Unity again")
                    print("4. Check for errors again")
                    print("5. Then run tests")
                    print("\nTo skip this check (not recommended):")
                    print("  Add --skip-compilation-check flag")
                    print("="*60)
                    sys.exit(2)  # Exit code 2 for compilation errors
                else:
                    print("[OK] No compilation errors found")
            
            # Focus Unity BEFORE submitting request for immediate processing
            if args.focus:
                try:
                    import unity_focus
                    print("Focusing Unity window...")
                    if unity_focus.focus_unity():
                        print("Unity window focused")
                    else:
                        print("Could not focus Unity window")
                except ImportError:
                    print("Warning: unity_focus module not found")
                except Exception as e:
                    print(f"Could not focus Unity: {e}")
            
            # Submit the request
            request_id = coordinator.submit_test_request(
                request_type,
                platform,
                test_filter,
                args.priority
            )
            
            # Wait if requested
            if args.wait:
                print(f"Waiting for completion (timeout: {args.timeout}s)...")
                final_status = coordinator.wait_for_completion(request_id, args.timeout)
                coordinator.print_summary(request_id)
            else:
                print(f"Use 'python quick_test.py status {request_id}' to check progress")
    
    except KeyboardInterrupt:
        print("\nOperation cancelled")
        sys.exit(1)
    except Exception as e:
        print(f"Error: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()