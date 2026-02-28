/**
 * Build text signals from raw text, then discard the text.
 * This is the single entry point for converting raw content to safe signals.
 */

import { TextSignals } from "./schema.js";
import { sha256Prefix } from "./hashing.js";
import { detectPatterns } from "./patterns.js";

/**
 * Extract signals from text. The caller MUST discard the raw text after calling this.
 * Returns only: length, sha256 prefix (8 bytes), and matched pattern IDs.
 */
export async function extractTextSignals(text: string): Promise<TextSignals> {
  const length = text.length;
  const hash = await sha256Prefix(text);
  const patterns = detectPatterns(text);

  return {
    length,
    sha256_prefix: hash,
    patterns,
  };
}
