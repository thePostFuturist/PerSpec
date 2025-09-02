#!/usr/bin/env python3
"""
Unity Compilation Error Parser
Parses various Unity compilation error formats
"""

import re
from typing import Optional, Dict, List, Tuple
from dataclasses import dataclass
from datetime import datetime

# Prevent Python from creating .pyc files
import sys
import os
sys.dont_write_bytecode = True
os.environ['PYTHONDONTWRITEBYTECODE'] = '1'

@dataclass
class ParsedError:
    """Structured compilation error data"""
    error_code: str
    file_path: str
    line_number: int
    column_number: int
    error_message: str
    severity: str  # error, warning, info
    full_text: str
    
    def to_display_string(self) -> str:
        """Format for display"""
        if self.file_path:
            return f"{self.error_code}: {self.file_path}:{self.line_number},{self.column_number} - {self.error_message}"
        else:
            return f"{self.error_code}: {self.error_message}"
    
    def to_short_string(self) -> str:
        """Short format for summary"""
        if self.file_path:
            filename = self.file_path.split('/')[-1]
            return f"{filename}:{self.line_number} - {self.error_code}"
        else:
            return self.error_code

class CompilationErrorParser:
    """Parser for Unity compilation errors"""
    
    # Common C# error codes and their descriptions
    ERROR_DESCRIPTIONS = {
        'CS0029': 'Cannot implicitly convert type',
        'CS0103': 'Name does not exist in current context',
        'CS0117': 'Type does not contain definition',
        'CS0119': 'Is a type but used like a variable',
        'CS0246': 'Type or namespace not found',
        'CS0266': 'Cannot implicitly convert (cast required)',
        'CS1061': 'Type does not contain definition and no extension method found',
        'CS1501': 'No overload takes N arguments',
        'CS1502': 'Best overloaded method has invalid arguments',
        'CS1503': 'Cannot convert argument',
        'CS1519': 'Invalid token in declaration',
        'CS1525': 'Invalid expression term',
        'CS1644': 'Feature not available in C# version',
        'CS7036': 'No argument given for required parameter',
    }
    
    def __init__(self):
        # Compilation error patterns
        self.patterns = [
            # Standard C# error: Assets/Scripts/Example.cs(10,5): error CS0117: message
            re.compile(r'^(.+?)\((\d+),(\d+)\): (error|warning) (CS\d+): (.+)$'),
            
            # Unity-specific error without column: Assets/Scripts/Example.cs(10): error CS0117: message
            re.compile(r'^(.+?)\((\d+)\): (error|warning) (CS\d+): (.+)$'),
            
            # Generic error: Assets/Scripts/Example.cs: error: message
            re.compile(r'^(.+?): (error|warning): (.+)$'),
            
            # Compilation failed summary
            re.compile(r'^Compilation failed: (\d+) error\(s\), (\d+) warning\(s\)'),
            
            # Assembly compilation error
            re.compile(r"^Assembly '(.+?)' will not be loaded due to errors:"),
            
            # Missing assembly reference
            re.compile(r"^The type or namespace name '(.+?)' could not be found"),
        ]
    
    def parse(self, line: str) -> Optional[ParsedError]:
        """Parse a single line for compilation error"""
        line = line.strip()
        if not line:
            return None
        
        # Try standard C# error with line and column
        match = self.patterns[0].match(line)
        if match:
            return ParsedError(
                file_path=match.group(1),
                line_number=int(match.group(2)),
                column_number=int(match.group(3)),
                severity=match.group(4),
                error_code=match.group(5),
                error_message=match.group(6),
                full_text=line
            )
        
        # Try C# error without column
        match = self.patterns[1].match(line)
        if match:
            return ParsedError(
                file_path=match.group(1),
                line_number=int(match.group(2)),
                column_number=0,
                severity=match.group(3),
                error_code=match.group(4),
                error_message=match.group(5),
                full_text=line
            )
        
        # Try generic error
        match = self.patterns[2].match(line)
        if match:
            return ParsedError(
                file_path=match.group(1),
                line_number=0,
                column_number=0,
                severity=match.group(2),
                error_code="",
                error_message=match.group(3),
                full_text=line
            )
        
        # Check for compilation summary
        if "Compilation failed:" in line:
            return ParsedError(
                file_path="",
                line_number=0,
                column_number=0,
                severity="error",
                error_code="COMPILATION_FAILED",
                error_message=line,
                full_text=line
            )
        
        return None
    
    def parse_multi_line(self, lines: List[str]) -> List[ParsedError]:
        """Parse multiple lines, handling multi-line errors"""
        errors = []
        current_error = None
        
        for line in lines:
            parsed = self.parse(line)
            
            if parsed:
                if current_error:
                    errors.append(current_error)
                current_error = parsed
            elif current_error and line.strip():
                # Continuation of previous error
                current_error.error_message += "\n" + line.strip()
                current_error.full_text += "\n" + line
        
        if current_error:
            errors.append(current_error)
        
        return errors
    
    def get_error_description(self, error_code: str) -> str:
        """Get human-friendly description for error code"""
        return self.ERROR_DESCRIPTIONS.get(error_code, "Unknown error")
    
    def is_compilation_error_line(self, line: str) -> bool:
        """Quick check if line contains compilation error"""
        indicators = [
            ": error CS",
            ": warning CS",
            "Compilation failed:",
            "error: ",
            "will not be loaded due to errors",
            "could not be found"
        ]
        
        return any(indicator in line for indicator in indicators)
    
    def extract_file_reference(self, error_message: str) -> Optional[Tuple[str, int, int]]:
        """Extract file path, line, and column from error message"""
        # Pattern for file references in error messages
        pattern = re.compile(r"'(.+?)' \(at (.+?):(\d+)\)")
        match = pattern.search(error_message)
        
        if match:
            return (match.group(2), int(match.group(3)), 0)
        
        return None
    
    def group_related_errors(self, errors: List[ParsedError]) -> Dict[str, List[ParsedError]]:
        """Group errors by file for better organization"""
        grouped = {}
        
        for error in errors:
            key = error.file_path if error.file_path else "General"
            if key not in grouped:
                grouped[key] = []
            grouped[key].append(error)
        
        # Sort errors within each file by line number
        for file_path in grouped:
            grouped[file_path].sort(key=lambda e: (e.line_number, e.column_number))
        
        return grouped
    
    def format_error_summary(self, errors: List[ParsedError]) -> str:
        """Format errors into a readable summary"""
        if not errors:
            return "No compilation errors found"
        
        grouped = self.group_related_errors(errors)
        summary = []
        
        for file_path, file_errors in grouped.items():
            if file_path != "General":
                summary.append(f"\n{file_path}:")
            else:
                summary.append(f"\nGeneral Errors:")
            
            for error in file_errors:
                if error.line_number > 0:
                    summary.append(f"  Line {error.line_number}: {error.error_code} - {error.error_message[:80]}")
                else:
                    summary.append(f"  {error.error_code}: {error.error_message[:80]}")
        
        total = len(errors)
        error_count = sum(1 for e in errors if e.severity == "error")
        warning_count = sum(1 for e in errors if e.severity == "warning")
        
        summary.insert(0, f"Found {error_count} error(s) and {warning_count} warning(s)")
        
        return "\n".join(summary)


# Testing utility
if __name__ == "__main__":
    parser = CompilationErrorParser()
    
    # Test cases
    test_lines = [
        "Assets/Scripts/Player.cs(45,12): error CS0117: 'GameObject' does not contain a definition for 'NonExistent'",
        "Assets/UI/Menu.cs(10): error CS0246: The type or namespace name 'UIButton' could not be found",
        "Compilation failed: 2 error(s), 0 warning(s)",
        "Assets/Scripts/Test.cs: error: Unexpected symbol 'public'",
    ]
    
    print("Testing compilation error parser:\n")
    
    for line in test_lines:
        error = parser.parse(line)
        if error:
            print(f"Parsed: {error.to_display_string()}")
            print(f"  Code: {error.error_code}")
            print(f"  File: {error.file_path}")
            print(f"  Location: Line {error.line_number}, Column {error.column_number}")
            print(f"  Severity: {error.severity}")
            print()
    
    # Test grouping
    errors = parser.parse_multi_line(test_lines)
    print("\nFormatted Summary:")
    print(parser.format_error_summary(errors))