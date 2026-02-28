/**
 * Shared event schema v1 for Browser Telemetry DLP.
 * Must stay in sync with agent/src/Dlp.AgentCore/Schema/*.cs
 */

export enum EventType {
  CLIPBOARD_COPY = "CLIPBOARD_COPY",
  CLIPBOARD_PASTE = "CLIPBOARD_PASTE",
  FILE_UPLOAD_ATTEMPT = "FILE_UPLOAD_ATTEMPT",
  LLM_PROMPT_PASTE = "LLM_PROMPT_PASTE",
}

export interface TextSignals {
  length: number;
  sha256_prefix: string;
  patterns: string[];
}

export interface FileSignals {
  file_name: string;
  extension: string;
  mime_type: string;
  size_bytes: number;
}

export interface TelemetryEvent {
  event_id: string;
  timestamp: string;
  event_type: EventType;
  url: string;
  domain: string;
  tab_id: number;
  correlation_id: string;
  text_signals?: TextSignals;
  file_signals?: FileSignals;
}

export enum Decision {
  ALLOW = "allow",
  WARN = "warn",
  BLOCK = "block",
}

export interface PolicyDecision {
  event_id: string;
  decision: Decision;
  policy_id: string;
  policy_version: string;
  decision_reason: string;
}

/** Envelope for native messaging */
export interface NativeMessage {
  type: "event" | "decision";
  payload: TelemetryEvent | PolicyDecision;
}
