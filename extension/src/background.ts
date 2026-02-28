/**
 * Background service worker for the Browser Telemetry DLP extension.
 * Routes events from content scripts to the native messaging host.
 */

console.log("[DLP] Background service worker started.");

chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
  console.log("[DLP] Received message from content script:", message.type);
  sendResponse({ status: "ok" });
  return true;
});
