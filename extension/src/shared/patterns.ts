/**
 * Pattern detector for sensitive data.
 * Runs in-memory only — no raw text is stored or transmitted.
 */

export interface PatternMatch {
  id: string;
  label: string;
}

interface PatternDef {
  id: string;
  label: string;
  regex: RegExp;
}

const PATTERNS: PatternDef[] = [
  {
    id: "KR_RRN",
    label: "Korean Resident Registration Number",
    regex: /\d{6}-[1-4]\d{6}/,
  },
  {
    id: "CREDIT_CARD",
    label: "Credit Card Number",
    regex: /\b(?:4\d{3}|5[1-5]\d{2}|3[47]\d{2}|6(?:011|5\d{2}))[- ]?\d{4}[- ]?\d{4}[- ]?\d{1,4}\b/,
  },
  {
    id: "AWS_ACCESS_KEY",
    label: "AWS Access Key",
    regex: /\bAKIA[0-9A-Z]{16}\b/,
  },
  {
    id: "EMAIL",
    label: "Email Address",
    regex: /\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b/,
  },
  {
    id: "PHONE_KR",
    label: "Korean Phone Number",
    regex: /\b01[016789]-?\d{3,4}-?\d{4}\b/,
  },
];

/**
 * Detect patterns in text. Returns only pattern IDs — never raw text.
 * The input text is used only for matching and must not be persisted.
 */
export function detectPatterns(text: string): string[] {
  const matched: string[] = [];
  for (const p of PATTERNS) {
    if (p.regex.test(text)) {
      matched.push(p.id);
    }
  }
  return matched;
}
