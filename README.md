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
- [x] Issue 02: Extension toolchain (MV3 + TypeScript + esbuild)
- [x] Issue 03: Shared event schema v1 (TS + C# synchronized)
- [x] Issue 04: Pattern detection and SHA-256 prefix hashing (no raw text)
- [x] Issue 05: Content script clipboard copy/paste capture (signals only)
- [x] Issue 08: Background service worker with native messaging bridge
- [x] Issue 09: .NET solution scaffold (NativeHost + AgentCore + Tests)
- [x] Issue 10: Native messaging framing protocol (4-byte LE + JSON)
- [x] Issue 11: Policy engine (PDP) with JSON rules and priority evaluation
- [x] Issue 12: NDJSON audit logging (no raw content persisted)
- [x] Issue 13: Enforcement MVP — warn banner UI + best-effort block
- [x] Issue 14: Test pages (clipboard, file upload, LLM mock)
- [x] Issue 15: Dev setup docs + native host manifest registration

See [docs/DEV_SETUP.md](docs/DEV_SETUP.md) for the full developer setup guide.
