/**
 * Background service worker for the Browser Telemetry DLP extension.
 * Routes events from content scripts to the native messaging host.
 * Handles graceful degradation when native host is unavailable.
 */

import { TelemetryEvent, PolicyDecision, Decision, NativeMessage } from "./shared/schema.js";

const NATIVE_HOST_NAME = "com.browser_telemetry.agent";

let nativePort: chrome.runtime.Port | null = null;
let nativeConnected = false;
const pendingRequests = new Map<string, (decision: PolicyDecision) => void>();

function connectNative(): boolean {
  if (nativePort && nativeConnected) return true;

  try {
    nativePort = chrome.runtime.connectNative(NATIVE_HOST_NAME);
    nativeConnected = true;

    nativePort.onMessage.addListener((msg: NativeMessage) => {
      if (msg.type === "decision") {
        const decision = msg.payload as unknown as PolicyDecision;
        const resolve = pendingRequests.get(decision.event_id);
        if (resolve) {
          resolve(decision);
          pendingRequests.delete(decision.event_id);
        }
      }
    });

    nativePort.onDisconnect.addListener(() => {
      const error = chrome.runtime.lastError?.message ?? "unknown";
      console.warn("[DLP] Native host disconnected:", error);
      nativePort = null;
      nativeConnected = false;
      // Reject all pending requests
      for (const [id, resolve] of pendingRequests) {
        resolve(fallbackDecision(id));
        pendingRequests.delete(id);
      }
    });

    console.log("[DLP] Connected to native host.");
    return true;
  } catch (err) {
    console.warn("[DLP] Failed to connect to native host:", err);
    nativePort = null;
    nativeConnected = false;
    return false;
  }
}

function fallbackDecision(eventId: string): PolicyDecision {
  return {
    event_id: eventId,
    decision: Decision.ALLOW,
    policy_id: "fallback",
    policy_version: "0",
    decision_reason: "Native host unavailable — fallback to allow",
  };
}

async function sendToNativeHost(event: TelemetryEvent): Promise<PolicyDecision> {
  if (!connectNative() || !nativePort) {
    console.warn("[DLP] Native host not available, using fallback policy.");
    return fallbackDecision(event.event_id);
  }

  return new Promise<PolicyDecision>((resolve) => {
    const timeout = setTimeout(() => {
      pendingRequests.delete(event.event_id);
      console.warn("[DLP] Native host timeout for event:", event.event_id);
      resolve(fallbackDecision(event.event_id));
    }, 5000);

    pendingRequests.set(event.event_id, (decision) => {
      clearTimeout(timeout);
      resolve(decision);
    });

    const message: NativeMessage = {
      type: "event",
      payload: event as any,
    };
    nativePort!.postMessage(message);
  });
}

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (message.type !== "event") {
    sendResponse({ status: "ignored" });
    return true;
  }

  const event = message.payload as TelemetryEvent;

  // Enrich with tab info from sender
  if (sender.tab?.id) {
    event.tab_id = sender.tab.id;
  }

  console.log(`[DLP] Event received: ${event.event_type} from ${event.domain}`);

  sendToNativeHost(event).then((decision) => {
    console.log(`[DLP] Decision: ${decision.decision} (${decision.decision_reason})`);
    sendResponse({ decision: decision.decision, full: decision });
  });

  return true; // Keep channel open for async response
});

console.log("[DLP] Background service worker started.");
