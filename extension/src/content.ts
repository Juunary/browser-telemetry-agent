/**
 * Content script for the Browser Telemetry DLP extension.
 * Captures low-frequency events (paste, copy) — NO keylogging.
 * Raw text is converted to signals immediately and discarded.
 */

import { EventType, TelemetryEvent } from "./shared/schema.js";
import { extractTextSignals } from "./shared/signals.js";

function generateEventId(): string {
  return `evt_${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;
}

function generateCorrelationId(tabInfo: string): string {
  return `cor_${tabInfo}_${Date.now()}`;
}

async function handleClipboardEvent(
  e: ClipboardEvent,
  eventType: EventType
): Promise<void> {
  const text = e.clipboardData?.getData("text/plain") ?? "";
  if (!text) return;

  // Extract signals immediately — raw text is NOT stored
  const textSignals = await extractTextSignals(text);
  // text variable goes out of scope here — never persisted

  const event: TelemetryEvent = {
    event_id: generateEventId(),
    timestamp: new Date().toISOString(),
    event_type: eventType,
    url: window.location.href,
    domain: window.location.hostname,
    tab_id: 0, // Will be enriched by background
    correlation_id: generateCorrelationId(window.location.hostname),
    text_signals: textSignals,
  };

  try {
    chrome.runtime.sendMessage(
      { type: "event", payload: event },
      (response) => {
        if (chrome.runtime.lastError) {
          console.warn("[DLP] Failed to send event:", chrome.runtime.lastError.message);
          return;
        }
        if (response?.decision) {
          handleDecision(response.decision, event.event_id);
        }
      }
    );
  } catch (err) {
    console.warn("[DLP] Error sending message:", err);
  }
}

function handleDecision(decision: string, eventId: string): void {
  // Enforcement will be implemented in Issue 13
  console.log(`[DLP] Decision for ${eventId}: ${decision}`);
}

// Listen for paste events — low frequency, not keylogging
document.addEventListener("paste", (e: Event) => {
  handleClipboardEvent(e as ClipboardEvent, EventType.CLIPBOARD_PASTE);
});

// Listen for copy events — low frequency, not keylogging
document.addEventListener("copy", (e: Event) => {
  handleClipboardEvent(e as ClipboardEvent, EventType.CLIPBOARD_COPY);
});

console.log("[DLP] Content script loaded on:", window.location.href);
