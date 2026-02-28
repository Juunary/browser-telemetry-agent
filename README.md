# Browser Telemetry Agent

[![CI](https://github.com/your-org/browser-telemetry-agent/actions/workflows/ci.yml/badge.svg)](https://github.com/your-org/browser-telemetry-agent/actions/workflows/ci.yml)

Browser-based DLP (Data Loss Prevention) system using a Chrome MV3 extension and .NET native messaging host.

Detects sensitive data exfiltration (paste, file upload, LLM prompt paste) in the browser, evaluates policies via a local agent, and enforces warn/block decisions — **without keylogging or storing raw content**.

---

## Privacy & Security Guarantees

| Guarantee | Implementation |
|---|---|
| **No keylogging** | Only `paste`, `copy`, and `input[type=file] change` events — never continuous `input`/`keydown` |
| **No raw text stored** | Clipboard text → signals (length + sha256 prefix + pattern IDs) immediately, then discarded |
| **No file content read** | Only `File.name` extension, `File.type`, `File.size` — `FileReader` never called |
| **Signals only on wire** | `TelemetryEvent` carries `text_signals` / `file_signals`, never raw strings |
| **Minimal permissions** | Extension declares only `nativeMessaging`; no `tabs`, no `storage`, no `cookies` |
| **No `host_permissions`** | No broad host grants; content script scope is controlled by `manifest.json` `matches` |
| **Audit log no raw text** | NDJSON log entries store only signals and decision metadata — see sample below |

### Threat model in one sentence
> We detect *that* sensitive data was pasted/uploaded; we never see, store, or transmit *what* it was.

---

## Audit Log Sample

Each event + decision produces one NDJSON line in `agent/logs/events-YYYYMMDD.ndjson`:

```json
{"timestamp":"2025-01-15T10:23:45.123Z","event_id":"evt_1705314225_a3f8k2","event_type":"CLIPBOARD_PASTE","domain":"example.com","url":"https://example.com/upload","tab_id":3,"correlation_id":"cor_example.com_1705314225000","text_length":42,"sha256_prefix":"3q2+7w==","patterns":["CREDIT_CARD"],"decision":"warn","policy_id":"mvp-policy","policy_version":"1.0.0","decision_reason":"[rule-sensitive-paste] Paste contains sensitive data pattern"}
```

Fields present: `text_length`, `sha256_prefix`, `patterns` — **no clipboard text**.

---

## Extension Permissions & Scope

**`manifest.json` permissions declared:**
```json
{ "permissions": ["nativeMessaging"] }
```

**No `host_permissions`** — the extension does not request broad URL access grants.

**Content script `matches`:** `<all_urls>` — required so the content script can inject into any page the user visits. This is the minimum needed for DLP coverage. The `nativeMessaging` permission is gated by the native host manifest's `allowed_origins`, which lists only the specific extension ID.

**LLM prompt classification** (`LLM_PROMPT_PASTE`) is further restricted by an in-code domain allowlist (`LLM_DOMAIN_ALLOWLIST` in `content.ts`). Paste events on non-allowlisted domains are classified as `CLIPBOARD_PASTE`, not `LLM_PROMPT_PASTE`, regardless of the target element.

---

## Correlation ID

Format: `cor_{hostname}_{unix_ms}`

Example: `cor_example.com_1705314225000`

A new correlation ID is generated per event. It ties events from the same page session together for audit trail review without requiring persistent session state. Future hardening could use a tab-scoped UUID refreshed on navigation.

---

## How to Run

### Prerequisites
- Node.js 18+ and npm
- .NET 8 SDK
- Google Chrome

### 1. Build

```powershell
# Extension (TypeScript → dist/)
cd extension
npm ci
npm run build

# Agent (.NET)
cd ../agent/src
dotnet build Dlp.NativeHost -c Release
```

### 2. Load Extension in Chrome

1. Open `chrome://extensions`
2. Enable **Developer mode** (top right)
3. Click **Load unpacked** → select the `extension/` folder
4. Note the **Extension ID** shown (e.g. `pigonkbnlpiecabnanhkomnbmcabmgcf`)

### 3. Register Native Messaging Host

```powershell
# Run from project root (PowerShell as Administrator)
.\scripts\register-native-host.ps1 -ExtensionId "YOUR_EXTENSION_ID"
```

This builds the agent in Release mode and writes the registry key Chrome needs to find the native host.

### 4. Test the Pipeline

1. Open `test-pages/clipboard.html` in Chrome
2. Paste a credit card number (e.g. `4111 1111 1111 1111`) into the text box
3. A **yellow warn banner** appears at the top of the page
4. Check `agent/logs/events-YYYYMMDD.ndjson` — a log entry is written with signals only (no raw text)

For executable upload blocking, try attaching a `.exe` file on `test-pages/file-upload.html`.
For LLM prompt detection, open `test-pages/llm-mock.html` and paste sensitive data into the prompt field.

---

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

