# Browser Telemetry Agent

Browser-based DLP (Data Loss Prevention) system using a Chrome MV3 extension and .NET native messaging host.

Detects sensitive data exfiltration (paste, file upload, LLM prompt paste) in the browser, evaluates policies via a local agent, and enforces warn/block decisions — **without keylogging or storing raw content**.

## Quick Start

```bash
# Build everything
./scripts/build.sh        # Linux/macOS
.\scripts\build.ps1       # Windows PowerShell
```

## Project Structure

```
/extension    # Chrome MV3 extension (TypeScript)
/agent        # .NET native messaging host + policy engine
/test-pages   # Local HTML pages for manual testing
/scripts      # Build and utility scripts
/docs         # Documentation
```

## Progress

- [x] Issue 01: Repo skeleton and build scripts
