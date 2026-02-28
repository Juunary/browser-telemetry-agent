/**
 * SHA-256 hashing utility for text signals.
 * Only the prefix is retained — never the full hash or raw text.
 */

/**
 * Compute SHA-256 of text and return the first 8 bytes as a base64 prefix.
 * Uses the Web Crypto API available in extension contexts.
 */
export async function sha256Prefix(text: string): Promise<string> {
  const encoder = new TextEncoder();
  const data = encoder.encode(text);
  const hashBuffer = await crypto.subtle.digest("SHA-256", data);
  const hashArray = new Uint8Array(hashBuffer).slice(0, 8);
  return btoa(String.fromCharCode(...hashArray));
}
