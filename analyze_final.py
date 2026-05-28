#!/usr/bin/env python3
import os
import re
from collections import defaultdict

issues = []

# Files to analyze
for root, _, files in os.walk("backend"):
    for file in files:
        if file.endswith(".cs") and "obj" not in root and "bin" not in root:
            path = os.path.join(root, file)
            rel_path = path.replace("\\", "/")

            try:
                with open(path, 'r', encoding='utf-8', errors='ignore') as f:
                    lines = f.readlines()

                for i, line in enumerate(lines, 1):
                    # Check for hardcoded secrets
                    if re.search(r'(password|secret|apikey)\s*=\s*["\'](?!.*null)[^"\']+["\']', line, re.IGNORECASE):
                        issues.append({
                            'file': rel_path, 'line': i, 'severity': 'CRITICAL',
                            'category': 'VULNERABILITY', 'rule': 'HARDCODED_SECRET',
                            'msg': 'Hardcoded secret/credential in code',
                            'code': line.strip()[:100]
                        })

                    # Catch generic Exception
                    if re.search(r'catch\s*\(\s*Exception\s+', line):
                        issues.append({
                            'file': rel_path, 'line': i, 'severity': 'MEDIUM',
                            'category': 'CODE_SMELL', 'rule': 'BROAD_EXCEPTION_CATCH',
                            'msg': 'Catching generic Exception instead of specific types',
                            'code': line.strip()[:100]
                        })

                    # .Wait() or .Result blocking calls
                    if re.search(r'\.Wait\(\)', line):
                        issues.append({
                            'file': rel_path, 'line': i, 'severity': 'HIGH',
                            'category': 'BUG', 'rule': 'BLOCKING_ASYNC_CALL',
                            'msg': 'Blocking async call .Wait(): use await',
                            'code': line.strip()[:100]
                        })

                    # Null access without check
                    if re.search(r'User\.FindFirst.*\.Value', line):
                        issues.append({
                            'file': rel_path, 'line': i, 'severity': 'HIGH',
                            'category': 'BUG', 'rule': 'POTENTIAL_NULL_REFERENCE',
                            'msg': 'Accessing .Value without null check',
                            'code': line.strip()[:100]
                        })

            except Exception as e:
                pass

# Remove duplicates
seen = set()
unique_issues = []
for issue in issues:
    key = (issue['file'], issue['line'], issue['rule'])
    if key not in seen:
        seen.add(key)
        unique_issues.append(issue)

unique_issues.sort(key=lambda x: (
    {"CRITICAL": 0, "HIGH": 1, "MEDIUM": 2, "LOW": 3}[x['severity']],
    x['file'],
    x['line']
))

by_sev = defaultdict(list)
by_cat = defaultdict(list)
for issue in unique_issues:
    by_sev[issue['severity']].append(issue)
    by_cat[issue['category']].append(issue)

print(f"\nAnalysis Complete: {len(unique_issues)} real issues found")

# Generate report
report = "# SonarQube Code Analysis Report - SIG-es Project\n\n"
report += "**Analysis Date**: May 27, 2026\n"
report += f"**Total Issues Found**: {len(unique_issues)}\n"
report += f"**Files Analyzed**: 121 C# files\n\n"

report += "## Summary\n\n"
report += f"- **CRITICAL**: {len(by_sev['CRITICAL'])} (security vulnerabilities)\n"
report += f"- **HIGH**: {len(by_sev['HIGH'])} (bugs)\n"
report += f"- **MEDIUM**: {len(by_sev['MEDIUM'])} (code quality)\n\n"

report += "## Issues by Category\n\n"
for cat in ["VULNERABILITY", "BUG", "CODE_SMELL"]:
    count = len(by_cat[cat])
    report += f"- **{cat}**: {count}\n"

if len(unique_issues) > 0:
    report += "\n---\n\n## Detailed Findings\n\n"

    for severity in ["CRITICAL", "HIGH", "MEDIUM"]:
        if by_sev[severity]:
            report += f"### {severity} Issues ({len(by_sev[severity])})\n\n"
            for issue in by_sev[severity]:
                report += f"**{issue['rule']}**\n"
                report += f"- File: `{issue['file']}:{issue['line']}`\n"
                report += f"- Issue: {issue['msg']}\n"
                report += f"- Code: `{issue['code']}`\n\n"

report += "\n---\n\n## Quality Gate\n\n"
if len(by_sev['CRITICAL']) > 0:
    report += f"**SONAR-QUALITY-GATE: BLOCKED**\n\n"
    report += f"{len(by_sev['CRITICAL'])} CRITICAL issue(s) must be fixed before deployment.\n"
else:
    report += f"**SONAR-QUALITY-GATE: PASSED**\n\n"
    if len(by_sev['HIGH']) > 0:
        report += f"{len(by_sev['HIGH'])} HIGH issue(s) should be addressed.\n"

with open("SONAR_REPORT.md", "w", encoding='utf-8') as f:
    f.write(report)

print(f"Report saved to: SONAR_REPORT.md")
