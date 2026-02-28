/**
 * Content script for the Browser Telemetry DLP extension.
 * Captures low-frequency events (paste, copy, file-change) — NO keylogging.
 * Raw text is converted to signals immediately and discarded.
 * File contents are NEVER read — only metadata (extension, mime, size).
 */

import { EventType, TelemetryEvent, FileSignals } from "./shared/schema.js";
import { extractTextSignals } from "./shared/signals.js";

function generateEventId(): string {
  return `evt_${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;
}

function generateCorrelationId(tabInfo: string): string {
  return `cor_${tabInfo}_${Date.now()}`;
}

// --- Enforcement UI ---

let bannerTimeout: ReturnType<typeof setTimeout> | null = null;

function showBanner(decision: string, reason: string): void {
  // Debounce: skip if a banner is already showing
  if (document.getElementById("dlp-banner")) return;

  const isBlock = decision === "block";
  const banner = document.createElement("div");
  banner.id = "dlp-banner";
  banner.style.cssText = `
    position: fixed; top: 0; left: 0; right: 0; z-index: 2147483647;
    padding: 12px 20px; font-family: -apple-system, sans-serif; font-size: 14px;
    color: white; display: flex; align-items: center; justify-content: space-between;
    box-shadow: 0 2px 8px rgba(0,0,0,0.3); animation: dlp-slide-in 0.3s ease-out;
    background: ${isBlock ? "#d32f2f" : "#f57c00"};
  `;

  const icon = isBlock ? "\u26D4" : "\u26A0\uFE0F";
  const title = isBlock ? "Blocked" : "Warning";

  banner.innerHTML = `
    <div style="flex:1">
      <strong>${icon} DLP ${title}:</strong> ${escapeHtml(reason)}
    </div>
    <button id="dlp-banner-close" style="
      background: none; border: 1px solid rgba(255,255,255,0.5); color: white;
      padding: 4px 12px; border-radius: 4px; cursor: pointer; font-size: 13px;
      margin-left: 16px;
    ">Dismiss</button>
  `;

  // Add slide-in animation
  const style = document.createElement("style");
  style.textContent = `
    @keyframes dlp-slide-in {
      from { transform: translateY(-100%); opacity: 0; }
      to { transform: translateY(0); opacity: 1; }
    }
  `;
  banner.appendChild(style);

  document.body.appendChild(banner);

  banner.querySelector("#dlp-banner-close")?.addEventListener("click", () => {
    removeBanner();
  });

  // Auto-dismiss warnings after 8 seconds, blocks stay longer
  const autoMs = isBlock ? 15000 : 8000;
  bannerTimeout = setTimeout(removeBanner, autoMs);
}

function removeBanner(): void {
  const banner = document.getElementById("dlp-banner");
  if (banner) {
    banner.remove();
  }
  if (bannerTimeout) {
    clearTimeout(bannerTimeout);
    bannerTimeout = null;
  }
}

function escapeHtml(str: string): string {
  const div = document.createElement("div");
  div.textContent = str;
  return div.innerHTML;
}

// --- Best-effort block ---

function blockFormSubmits(): void {
  // Prevent form submissions on the page for a short window
  const handler = (e: Event) => {
    e.preventDefault();
    e.stopPropagation();
    console.log("[DLP] Form submit blocked by policy.");
  };
  document.addEventListener("submit", handler, { capture: true, once: true });

  // Auto-remove after 10 seconds (one-shot protection)
  setTimeout(() => {
    document.removeEventListener("submit", handler, { capture: true } as any);
  }, 10000);
}

function clearFileInputs(): void {
  const inputs = document.querySelectorAll<HTMLInputElement>('input[type="file"]');
  inputs.forEach((input) => {
    input.value = "";
  });
}

// --- Decision handler ---

function handleDecision(decision: string, reason: string, eventId: string): void {
  console.log(`[DLP] Decision for ${eventId}: ${decision} — ${reason}`);

  if (decision === "warn") {
    showBanner("warn", reason);
  } else if (decision === "block") {
    showBanner("block", reason);
    blockFormSubmits();
    clearFileInputs();
  }
  // "allow" — do nothing
}

// --- Send event to background ---

function sendEvent(event: TelemetryEvent): void {
  try {
    chrome.runtime.sendMessage(
      { type: "event", payload: event },
      (response) => {
        if (chrome.runtime.lastError) {
          console.warn("[DLP] Failed to send event:", chrome.runtime.lastError.message);
          return;
        }
        if (response?.decision) {
          const reason = response.full?.decision_reason ?? "Policy decision";
          handleDecision(response.decision, reason, event.event_id);
        }
      }
    );
  } catch (err) {
    console.warn("[DLP] Error sending message:", err);
  }
}

// --- LLM domain allowlist (MVP: constant array) ---

const LLM_DOMAIN_ALLOWLIST: string[] = [
  "chat.openai.com",
  "chatgpt.com",
  "claude.ai",
  "gemini.google.com",
  "bard.google.com",
  "copilot.microsoft.com",
  "localhost",
  "",  // empty hostname = file:// URLs (test pages)
];

function isLlmDomain(hostname: string): boolean {
  return LLM_DOMAIN_ALLOWLIST.some(
    (d) => d === hostname || (d !== "" && hostname.endsWith("." + d))
  );
}

/** Check if the paste target is a prompt-like input field */
function isPromptField(target: EventTarget | null): boolean {
  if (!target || !(target instanceof HTMLElement)) return false;
  if (target instanceof HTMLTextAreaElement) return true;
  if (target instanceof HTMLInputElement && target.type === "text") return true;
  if (target.isContentEditable) return true;
  return false;
}

// --- Clipboard event handlers ---

async function handleClipboardEvent(
  e: ClipboardEvent,
  eventType: EventType
): Promise<void> {
  const text = e.clipboardData?.getData("text/plain") ?? "";
  if (!text) return;

  // Classify as LLM_PROMPT_PASTE if on allowlisted domain + prompt field
  let resolvedType = eventType;
  if (
    eventType === EventType.CLIPBOARD_PASTE &&
    isLlmDomain(window.location.hostname) &&
    isPromptField(e.target)
  ) {
    resolvedType = EventType.LLM_PROMPT_PASTE;
  }

  // Extract signals immediately — raw text is NOT stored
  const textSignals = await extractTextSignals(text);
  // text variable goes out of scope here — never persisted

  const event: TelemetryEvent = {
    event_id: generateEventId(),
    timestamp: new Date().toISOString(),
    event_type: resolvedType,
    url: window.location.href,
    domain: window.location.hostname,
    tab_id: 0,
    correlation_id: generateCorrelationId(window.location.hostname),
    text_signals: textSignals,
  };

  sendEvent(event);
}

// Listen for paste events — low frequency, not keylogging
document.addEventListener("paste", (e: Event) => {
  handleClipboardEvent(e as ClipboardEvent, EventType.CLIPBOARD_PASTE);
});

// Listen for copy events — low frequency, not keylogging
document.addEventListener("copy", (e: Event) => {
  handleClipboardEvent(e as ClipboardEvent, EventType.CLIPBOARD_COPY);
});

// --- File upload detection (metadata only, NEVER reads file content) ---

function extractFileExtension(name: string): string {
  const dot = name.lastIndexOf(".");
  return dot >= 0 ? name.substring(dot).toLowerCase() : "";
}

function handleFileInputChange(input: HTMLInputElement): void {
  const files = input.files;
  if (!files || files.length === 0) return;

  for (let i = 0; i < files.length; i++) {
    const file = files[i];
    // Extract metadata signals only — NEVER read file bytes
    const fileSignals: FileSignals = {
      file_name: "",          // Omit filename by default for privacy
      extension: extractFileExtension(file.name),
      mime_type: file.type || "application/octet-stream",
      size_bytes: file.size,
    };

    const event: TelemetryEvent = {
      event_id: generateEventId(),
      timestamp: new Date().toISOString(),
      event_type: EventType.FILE_UPLOAD_ATTEMPT,
      url: window.location.href,
      domain: window.location.hostname,
      tab_id: 0,
      correlation_id: generateCorrelationId(window.location.hostname),
      file_signals: fileSignals,
    };

    sendEvent(event);
  }
}

function attachFileListener(input: HTMLInputElement): void {
  if ((input as any).__dlpFileListenerAttached) return;
  (input as any).__dlpFileListenerAttached = true;
  input.addEventListener("change", () => handleFileInputChange(input));
}

// Attach to existing file inputs
document.querySelectorAll<HTMLInputElement>('input[type="file"]').forEach(attachFileListener);

// Watch for dynamically inserted file inputs
const fileObserver = new MutationObserver((mutations) => {
  for (const mutation of mutations) {
    for (const node of mutation.addedNodes) {
      if (node instanceof HTMLInputElement && node.type === "file") {
        attachFileListener(node);
      }
      if (node instanceof HTMLElement) {
        node.querySelectorAll<HTMLInputElement>('input[type="file"]').forEach(attachFileListener);
      }
    }
  }
});
fileObserver.observe(document.documentElement, { childList: true, subtree: true });

 * Captures low-frequency events (paste, file upload, etc.) — NO keylogging.
 */

console.log("[DLP] Content script loaded on:", window.location.href);
