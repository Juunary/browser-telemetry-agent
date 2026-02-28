# Developer Setup Guide

## Prerequisites

- **Node.js** 18+ (for extension build)
- **.NET 8 SDK** (for native host)
- **Chrome** or **Edge** (Chromium-based)
- **Windows 10/11** (primary target; macOS/Linux adaptable)

## 1. Clone and Build

```bash
git clone <repo-url>
cd browser-telemetry-agent

# Build extension
cd extension
npm ci
npm run build
cd ..

# Build .NET agent
cd agent
dotnet build
dotnet test   # Should pass all tests
cd ..
```

Or use the build script:
```powershell
.\scripts\build.ps1
```

## 2. Load the Extension in Chrome

1. Open Chrome and navigate to `chrome://extensions`
2. Enable **Developer mode** (toggle in top-right)
3. Click **Load unpacked**
4. Select the `extension/` folder (the one containing `manifest.json`)
5. Note the **Extension ID** shown on the card (e.g., `abcdefghijklmnopqrstuvwxyz123456`)
6. The background service worker should show "Started" in the console

## 3. Register the Native Host (Windows)

### Automated (PowerShell)

```powershell
.\scripts\register-native-host.ps1 -ExtensionId "YOUR_EXTENSION_ID_HERE"
```

This will:
- Build the native host in Release mode
- Create `agent/native-host-manifest.json` with the correct paths
- Register it in `HKCU\Software\Google\Chrome\NativeMessagingHosts\`

### Manual Setup

1. Build the native host:
   ```
   cd agent
   dotnet build src\Dlp.NativeHost --configuration Release
   ```

2. Edit `agent/native-host-manifest.json`:
   - Set `path` to the full path of `Dlp.NativeHost.exe`
   - Set `allowed_origins` to `["chrome-extension://YOUR_EXTENSION_ID/"]`

3. Register in Windows Registry:
   ```
   reg add "HKCU\Software\Google\Chrome\NativeMessagingHosts\com.browser_telemetry.agent" ^
       /ve /t REG_SZ /d "C:\full\path\to\agent\native-host-manifest.json" /f
   ```

### Edge (Chromium)

Same steps, but the registry path is:
```
HKCU\Software\Microsoft\Edge\NativeMessagingHosts\com.browser_telemetry.agent
```

## 4. Test the Pipeline

1. Open `test-pages/clipboard.html` in Chrome (as a `file://` URL or served locally)
2. Paste a credit card number: `4532-1234-5678-9012`
3. You should see:
   - **Background console**: Event received + decision logged
   - **Orange warning banner** on the page: "Paste contains sensitive data pattern"
   - **agent/logs/**: NDJSON audit log entry with signals only (no raw text)

## 5. Policy Configuration

Edit `agent/policy/policy.json` to customize rules:
- `exceptions`: domains that are always allowed
- `rules`: evaluated by priority (highest first)
- Supported conditions: `event_type_in`, `domain_in`, `domain_not_in`, `patterns_any`, `text_length_min`, `file_extension_in`

## Troubleshooting

### Extension doesn't load
- Check `chrome://extensions` for errors
- Verify `extension/dist/` contains `background.js` and `content.js`
- Run `npm run build` in the extension folder

### Native host not connecting
- Check the background service worker console for "[DLP] Native host disconnected" errors
- Verify the registry key exists: `reg query "HKCU\Software\Google\Chrome\NativeMessagingHosts\com.browser_telemetry.agent"`
- Verify the manifest path in the registry points to a valid JSON file
- Verify the `path` in the manifest points to the actual `.exe`
- Verify the `allowed_origins` contains your extension ID with the `chrome-extension://` prefix

### Native host crashes
- Check stderr output: the host logs to stderr
- Run the host manually to test: pipe a sample message via stdin
- Check `agent/logs/` for any partial log entries

### No banner appearing
- Open DevTools (F12) on the test page
- Check the console for `[DLP]` messages
- If you see "Decision: allow", the policy didn't match — check `policy.json`

### Permission denied (file://URLs)
- Go to `chrome://extensions` > your extension > Details
- Enable "Allow access to file URLs"
