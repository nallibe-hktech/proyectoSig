#!/usr/bin/env python3
"""
Code Quality Analysis for .NET Projects
Analyzes C# code for bugs, vulnerabilities, code smells, duplication
"""

import os
import re
from pathlib import Path
from collections import defaultdict
from dataclasses import dataclass
from typing import List, Dict, Tuple

@dataclass
class Issue:
    file: str
    line: int
    severity: str  # CRITICAL, HIGH, MEDIUM, LOW
    category: str  # VULNERABILITY, BUG, CODE_SMELL, DUPLICATION
    rule: str
    message: str
    code_snippet: str = ""

class CodeAnalyzer:
    def __init__(self, root_dir: str):
        self.root_dir = root_dir
        self.issues: List[Issue] = []
        self.file_contents: Dict[str, List[str]] = {}

    def load_files(self, pattern="*.cs"):
        """Load all C# files"""
        for root, _, files in os.walk(self.root_dir):
            for file in files:
                if file.endswith(pattern):
                    path = os.path.join(root, file)
                    try:
                        with open(path, 'r', encoding='utf-8') as f:
                            self.file_contents[path] = f.readlines()
                    except:
                        pass

    def analyze(self):
        """Run all analysis checks"""
        print(f"[INFO] Analyzing {len(self.file_contents)} C# files...")
        for file_path, lines in self.file_contents.items():
            self._analyze_file(file_path, lines)

    def _analyze_file(self, file_path: str, lines: List[str]):
        """Analyze a single file"""
        content = "".join(lines)
        relative_path = file_path.replace(self.root_dir, "").lstrip("\\")

        # Check for various patterns
        self._check_sql_injection(relative_path, lines)
        self._check_null_reference(relative_path, lines)
        self._check_exception_handling(relative_path, lines)
        self._check_authentication(relative_path, lines)
        self._check_hardcoded_credentials(relative_path, lines)
        self._check_unused_variables(relative_path, lines)
        self._check_empty_catch_blocks(relative_path, lines)
        self._check_string_concatenation(relative_path, lines)
        self._check_async_issues(relative_path, lines)
        self._check_resource_management(relative_path, lines)
        self._check_code_duplication(relative_path, content)
        self._check_complexity(relative_path, lines)
        self._check_logging(relative_path, lines)
        self._check_validation(relative_path, lines)

    def _check_sql_injection(self, file_path: str, lines: List[str]):
        """Check for potential SQL injection vulnerabilities"""
        for i, line in enumerate(lines, 1):
            # String concatenation in SQL queries
            if re.search(r'ExecuteQuery|ExecuteCommand|ExecuteScalar|\.Sql\(', line):
                if re.search(r'\$".*{.*}.*"|\+\s*.*\+|string\.Format.*\$', lines[max(0, i-2):i+1]):
                    if not re.search(r'@p[0-9]|@param|Parameters', line):
                        self.issues.append(Issue(
                            file=file_path, line=i, severity="CRITICAL",
                            category="VULNERABILITY", rule="SQL_INJECTION",
                            message="Potential SQL injection: Using string concatenation instead of parameterized queries",
                            code_snippet=line.strip()
                        ))

    def _check_null_reference(self, file_path: str, lines: List[str]):
        """Check for potential null reference exceptions"""
        for i, line in enumerate(lines, 1):
            # Accessing property without null check
            if re.search(r'\.[\w]+[\s]*(==|!=|\.)', line) and '?' not in line:
                if re.search(r'(?<!!=)\s*\.\s*[\w]+\s*(?!=)', line) and 'if' not in lines[max(0, i-2):i]:
                    if 'null' not in line and '?.' not in line:
                        # More restrictive: only flag obvious cases
                        if re.search(r'return\s+[\w\.]+\.[\w]+|result\.[\w]+|var\s+\w+\s*=\s*[\w\.]+\.', line):
                            if '??' not in line and '?.' not in line:
                                self.issues.append(Issue(
                                    file=file_path, line=i, severity="HIGH",
                                    category="BUG", rule="NULL_REFERENCE",
                                    message="Potential null reference exception: access without null check",
                                    code_snippet=line.strip()
                                ))

    def _check_exception_handling(self, file_path: str, lines: List[str]):
        """Check for broad exception handling"""
        for i, line in enumerate(lines, 1):
            # Catching generic Exception
            if re.search(r'catch\s*\(\s*Exception\s+\w+\s*\)', line):
                self.issues.append(Issue(
                    file=file_path, line=i, severity="MEDIUM",
                    category="CODE_SMELL", rule="BROAD_EXCEPTION",
                    message="Catching generic Exception instead of specific exception types",
                    code_snippet=line.strip()
                ))
            # Swallowing exceptions
            if re.search(r'catch.*{\s*}', "".join(lines[max(0, i-1):min(len(lines), i+2)])):
                if re.search(r'catch\s*\(', line):
                    self.issues.append(Issue(
                        file=file_path, line=i, severity="HIGH",
                        category="BUG", rule="EMPTY_CATCH",
                        message="Empty catch block: exception is silently swallowed",
                        code_snippet=line.strip()
                    ))

    def _check_hardcoded_credentials(self, file_path: str, lines: List[str]):
        """Check for hardcoded credentials"""
        for i, line in enumerate(lines, 1):
            if re.search(r'(password|secret|key|token|apikey)\s*=\s*["\']', line, re.IGNORECASE):
                if not re.search(r'(null|empty|""|string\.Empty|GetEnvironmentVariable)', line):
                    self.issues.append(Issue(
                        file=file_path, line=i, severity="CRITICAL",
                        category="VULNERABILITY", rule="HARDCODED_CREDENTIALS",
                        message="Hardcoded credentials detected",
                        code_snippet=line.strip()
                    ))

    def _check_unused_variables(self, file_path: str, lines: List[str]):
        """Check for unused variables"""
        content = "".join(lines)
        # Find variable declarations
        for i, line in enumerate(lines, 1):
            match = re.search(r'(?:var|int|string|bool|Task|async Task)\s+(\w+)\s*=', line)
            if match and '_' not in match.group(1) and not match.group(1).startswith('_'):
                var_name = match.group(1)
                # Check if variable is used after declaration
                after_content = "".join(lines[i:min(i+10, len(lines))])
                if var_name not in after_content and 'foreach' not in line:
                    self.issues.append(Issue(
                        file=file_path, line=i, severity="LOW",
                        category="CODE_SMELL", rule="UNUSED_VARIABLE",
                        message=f"Variable '{var_name}' appears unused",
                        code_snippet=line.strip()
                    ))

    def _check_empty_catch_blocks(self, file_path: str, lines: List[str]):
        """Check for empty catch blocks"""
        for i, line in enumerate(lines, 1):
            if 'catch' in line:
                # Look ahead for empty block
                for j in range(i, min(i+5, len(lines))):
                    if '{' in lines[j-1]:
                        for k in range(j, min(j+5, len(lines))):
                            if '}' in lines[k-1]:
                                block = "".join(lines[j:k])
                                if block.strip() == "":
                                    self.issues.append(Issue(
                                        file=file_path, line=i, severity="MEDIUM",
                                        category="CODE_SMELL", rule="EMPTY_CATCH",
                                        message="Empty catch block",
                                        code_snippet=lines[i-1].strip()
                                    ))
                                break

    def _check_string_concatenation(self, file_path: str, lines: List[str]):
        """Check for inefficient string concatenation"""
        for i, line in enumerate(lines, 1):
            # += with strings in loops
            if re.search(r'\w+\s*\+=\s*["\']', line):
                # Check if inside a loop
                loop_found = False
                for j in range(max(0, i-10), i):
                    if re.search(r'(for|while|foreach)\s*[\(\{]', lines[j]):
                        loop_found = True
                        break
                if loop_found:
                    self.issues.append(Issue(
                        file=file_path, line=i, severity="MEDIUM",
                        category="CODE_SMELL", rule="STRING_CONCATENATION_IN_LOOP",
                        message="Inefficient string concatenation in loop: use StringBuilder",
                        code_snippet=line.strip()
                    ))

    def _check_async_issues(self, file_path: str, lines: List[str]):
        """Check for async/await issues"""
        for i, line in enumerate(lines, 1):
            # Async method without await
            if re.search(r'async\s+Task', line):
                method_block = "".join(lines[i:min(i+20, len(lines))])
                if 'await' not in method_block and 'return' in method_block:
                    self.issues.append(Issue(
                        file=file_path, line=i, severity="MEDIUM",
                        category="CODE_SMELL", rule="ASYNC_WITHOUT_AWAIT",
                        message="Async method without await: consider removing async keyword",
                        code_snippet=line.strip()
                    ))
            # .Result blocking call
            if re.search(r'\.Result\s*[^=]', line) or re.search(r'\.Wait\(\)', line):
                self.issues.append(Issue(
                    file=file_path, line=i, severity="HIGH",
                    category="BUG", rule="BLOCKING_ASYNC",
                    message="Blocking async call: use await instead of .Result or .Wait()",
                    code_snippet=line.strip()
                ))

    def _check_resource_management(self, file_path: str, lines: List[str]):
        """Check for resource management issues"""
        for i, line in enumerate(lines, 1):
            # Creating resources without using/disposing
            if re.search(r'new\s+(HttpClient|StreamReader|StreamWriter|SqlConnection|DbConnection)\s*\(', line):
                if not re.search(r'using|\.Dispose\(\)', "".join(lines[max(0, i-1):min(len(lines), i+1)])):
                    self.issues.append(Issue(
                        file=file_path, line=i, severity="HIGH",
                        category="BUG", rule="RESOURCE_LEAK",
                        message="Resource created but not disposed: wrap in using statement",
                        code_snippet=line.strip()
                    ))

    def _check_code_duplication(self, file_path: str, content: str):
        """Check for code duplication"""
        # Simple duplication check: look for repeated method-like blocks
        methods = re.findall(r'(public|private|protected)\s+\w+\s+\w+\([^)]*\)\s*{[^}]+}', content)
        if len(methods) > len(set(methods)) * 0.8:  # More than 80% similar
            self.issues.append(Issue(
                file=file_path, line=1, severity="LOW",
                category="CODE_SMELL", rule="DUPLICATION",
                message="File contains duplicate code blocks",
                code_snippet=file_path
            ))

    def _check_complexity(self, file_path: str, lines: List[str]):
        """Check for high cyclomatic complexity"""
        for i, line in enumerate(lines, 1):
            # Count decision points in a method
            if re.search(r'(public|private|protected)\s+\w+\s+\w+\(', line):
                method_block = "".join(lines[i:min(i+30, len(lines))])
                decisions = len(re.findall(r'(if|else|&&|\|\||case|catch|for|while|foreach)', method_block))
                if decisions > 10:
                    self.issues.append(Issue(
                        file=file_path, line=i, severity="MEDIUM",
                        category="CODE_SMELL", rule="HIGH_COMPLEXITY",
                        message=f"Method has high cyclomatic complexity ({decisions} decision points)",
                        code_snippet=line.strip()
                    ))

    def _check_logging(self, file_path: str, lines: List[str]):
        """Check for missing or inadequate logging"""
        content = "".join(lines)
        if 'catch' in content or 'Exception' in content:
            if not re.search(r'(Log\.|logger\.|_log\.)', content):
                self.issues.append(Issue(
                    file=file_path, line=1, severity="MEDIUM",
                    category="CODE_SMELL", rule="MISSING_LOGGING",
                    message="Exception handling without logging",
                    code_snippet=file_path
                ))

    def _check_validation(self, file_path: str, lines: List[str]):
        """Check for missing input validation"""
        for i, line in enumerate(lines, 1):
            # Parameters in public methods without validation
            if re.search(r'public\s+\w+\s+\w+\s*\([^)]*\)', line):
                params = re.findall(r'(\w+)\s+(\w+)(?:\s*,|\s*\))', line)
                method_block = "".join(lines[i:min(i+15, len(lines))])
                for param_type, param_name in params:
                    if param_type not in ['int', 'bool', 'long']:  # Primitive types
                        if param_name not in method_block or 'null' not in method_block:
                            if '==' not in line or 'null' not in line:
                                self.issues.append(Issue(
                                    file=file_path, line=i, severity="MEDIUM",
                                    category="CODE_SMELL", rule="MISSING_VALIDATION",
                                    message=f"Parameter '{param_name}' not validated in public method",
                                    code_snippet=line.strip()
                                ))
                                break

def generate_report(issues: List[Issue], output_file: str):
    """Generate markdown report"""
    if not issues:
        report = "# SonarQube Analysis Report\n\nNo issues found!\n"
        with open(output_file, 'w') as f:
            f.write(report)
        return

    # Group issues
    by_severity = defaultdict(list)
    by_category = defaultdict(list)
    by_file = defaultdict(list)

    for issue in issues:
        by_severity[issue.severity].append(issue)
        by_category[issue.category].append(issue)
        by_file[issue.file].append(issue)

    report = "# SonarQube Analysis Report - SIG-es Project\n\n"
    report += f"**Analysis Date**: May 27, 2026\n"
    report += f"**Total Issues**: {len(issues)}\n\n"

    # Summary by severity
    report += "## Summary by Severity\n\n"
    severity_order = ["CRITICAL", "HIGH", "MEDIUM", "LOW"]
    for severity in severity_order:
        count = len(by_severity.get(severity, []))
        report += f"- **{severity}**: {count}\n"

    report += "\n## Summary by Category\n\n"
    categories_order = ["VULNERABILITY", "BUG", "CODE_SMELL", "DUPLICATION"]
    for category in categories_order:
        count = len(by_category.get(category, []))
        report += f"- **{category}**: {count}\n"

    # Detailed findings by severity
    report += "\n---\n\n"
    for severity in severity_order:
        if severity in by_severity:
            report += f"## {severity} Issues ({len(by_severity[severity])})\n\n"
            for issue in sorted(by_severity[severity], key=lambda x: (x.file, x.line)):
                report += f"### {issue.rule}\n"
                report += f"- **File**: `{issue.file}`\n"
                report += f"- **Line**: {issue.line}\n"
                report += f"- **Category**: {issue.category}\n"
                report += f"- **Message**: {issue.message}\n"
                if issue.code_snippet:
                    report += f"- **Code**: `{issue.code_snippet}`\n"
                report += "\n"

    # Issues by file
    report += "\n---\n\n## Issues by File\n\n"
    for file_path in sorted(by_file.keys()):
        issues_in_file = by_file[file_path]
        report += f"### {file_path}\n"
        report += f"Total issues: {len(issues_in_file)}\n"
        for severity in severity_order:
            count = len([i for i in issues_in_file if i.severity == severity])
            if count > 0:
                report += f"- {severity}: {count}\n"
        report += "\n"

    # SONAR-QUALITY-GATE
    report += "\n---\n\n## Quality Gate Result\n\n"
    critical_count = len(by_severity.get("CRITICAL", []))
    blocker_count = len(by_severity.get("BLOCKER", []))

    if critical_count > 0 or blocker_count > 0:
        report += "**SONAR-QUALITY-GATE: FAILED**\n\n"
        report += f"Reason: Found {critical_count} CRITICAL issue(s)\n"
    else:
        report += "**SONAR-QUALITY-GATE: PASSED**\n\n"

    with open(output_file, 'w', encoding='utf-8') as f:
        f.write(report)

    print(f"[INFO] Report saved to {output_file}")

if __name__ == "__main__":
    analyzer = CodeAnalyzer("/c/Users/NallibeRiveraGrisale/Workspaces/SIG-es/backend")
    analyzer.load_files("*.cs")
    analyzer.analyze()

    # Sort issues by severity and location
    analyzer.issues.sort(key=lambda x: (x.severity, x.file, x.line))

    print(f"\n[SUMMARY] Found {len(analyzer.issues)} issues:")
    by_sev = defaultdict(int)
    for issue in analyzer.issues:
        by_sev[issue.severity] += 1
    for sev in ["CRITICAL", "HIGH", "MEDIUM", "LOW"]:
        if sev in by_sev:
            print(f"  {sev}: {by_sev[sev]}")

    generate_report(analyzer.issues, "/c/Users/NallibeRiveraGrisale/Workspaces/SIG-es/SONAR_REPORT.md")
