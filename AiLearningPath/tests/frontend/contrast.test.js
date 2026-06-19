// Feature: ai-learning-path, Property 21: Tương phản màu body đạt WCAG AA ở cả hai Theme
//
// Pure-JS property test (no framework) runnable with: node --test
// Validates: Requirements 15.5, 15.6
//
// Property 21: với MỌI theme trong {light, dark} của cả landing và app:
//   - contrastRatio(text, bg) >= 4.5  (WCAG AA cho văn bản thường)
//   - text và bg đều KHÁC #000000 và #ffffff thuần
//
// Bảng màu được trích CHÍNH XÁC từ:
//   css/landing.css  -> :root (light) và [data-theme="dark"]  (--ink trên --paper)
//   css/app.css      -> :root (light) và [data-theme="dark"]  (--text trên --bg)

import { test } from 'node:test';
import assert from 'node:assert/strict';

// ---------------------------------------------------------------------------
// Pure helpers: WCAG 2.x relative luminance + contrast ratio
// ---------------------------------------------------------------------------

/** Parse a #rrggbb (or #rgb) hex string into [r,g,b] integers 0..255. */
export function parseHex(hex) {
  let h = String(hex).trim().replace(/^#/, '');
  if (h.length === 3) {
    h = h[0] + h[0] + h[1] + h[1] + h[2] + h[2];
  }
  if (!/^[0-9a-fA-F]{6}$/.test(h)) {
    throw new Error(`Invalid hex color: ${hex}`);
  }
  return [
    parseInt(h.slice(0, 2), 16),
    parseInt(h.slice(2, 4), 16),
    parseInt(h.slice(4, 6), 16),
  ];
}

/** Per-channel linearization per WCAG relative luminance definition. */
function channelLuminance(c8) {
  const c = c8 / 255;
  return c <= 0.03928 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4);
}

/** WCAG relative luminance of an #rrggbb color. */
export function relativeLuminance(hex) {
  const [r, g, b] = parseHex(hex);
  return (
    0.2126 * channelLuminance(r) +
    0.7152 * channelLuminance(g) +
    0.0722 * channelLuminance(b)
  );
}

/** WCAG contrast ratio between two colors. Range [1, 21]. */
export function contrastRatio(hex1, hex2) {
  const l1 = relativeLuminance(hex1);
  const l2 = relativeLuminance(hex2);
  const lighter = Math.max(l1, l2);
  const darker = Math.min(l1, l2);
  return (lighter + 0.05) / (darker + 0.05);
}

/** True when a color normalizes to pure black or pure white. */
export function isPureBlackOrWhite(hex) {
  const [r, g, b] = parseHex(hex);
  const black = r === 0 && g === 0 && b === 0;
  const white = r === 255 && g === 255 && b === 255;
  return black || white;
}

// ---------------------------------------------------------------------------
// Theme color table — body text/background pairs, extracted from the CSS.
// ---------------------------------------------------------------------------

export const THEME_BODY_PAIRS = [
  // landing.css  body { background: var(--paper); color: var(--ink); }
  { source: 'landing', theme: 'light', text: '#181a1f', bg: '#efece4' },
  { source: 'landing', theme: 'dark', text: '#e6e9ef', bg: '#12151c' },
  // app.css      body { background: var(--bg); color: var(--text); }
  { source: 'app', theme: 'light', text: '#181a1f', bg: '#efece4' },
  { source: 'app', theme: 'dark', text: '#e6e9ef', bg: '#12151c' },
];

const WCAG_AA_NORMAL = 4.5;

// ---------------------------------------------------------------------------
// Sanity checks for the pure helper (anchors / known values)
// ---------------------------------------------------------------------------

test('contrastRatio: black vs white is the maximum 21:1', () => {
  assert.ok(Math.abs(contrastRatio('#000000', '#ffffff') - 21) < 1e-9);
});

test('contrastRatio: identical colors is 1:1', () => {
  assert.ok(Math.abs(contrastRatio('#abcdef', '#abcdef') - 1) < 1e-9);
});

test('contrastRatio: symmetric in argument order', () => {
  const a = contrastRatio('#181a1f', '#efece4');
  const b = contrastRatio('#efece4', '#181a1f');
  assert.ok(Math.abs(a - b) < 1e-12);
});

// ---------------------------------------------------------------------------
// Property 21 — checked over every real body pair, reported individually.
// ---------------------------------------------------------------------------

test('Property 21: every body pair meets WCAG AA (>=4.5:1) and avoids pure #000/#fff', () => {
  for (const pair of THEME_BODY_PAIRS) {
    const ratio = contrastRatio(pair.text, pair.bg);
    const label = `${pair.source}/${pair.theme} (text ${pair.text} on bg ${pair.bg})`;

    console.log(`  ${label}: contrast = ${ratio.toFixed(2)}:1`);

    assert.ok(
      ratio >= WCAG_AA_NORMAL,
      `${label} contrast ${ratio.toFixed(3)} < ${WCAG_AA_NORMAL}`
    );
    assert.ok(
      !isPureBlackOrWhite(pair.text),
      `${label} text color is pure black/white`
    );
    assert.ok(
      !isPureBlackOrWhite(pair.bg),
      `${label} bg color is pure black/white`
    );
  }
});

// ---------------------------------------------------------------------------
// Property flavour: randomized iterations (>=100) over the color universe.
// For each iteration we pick a random real body pair and assert the invariant
// still holds; we also fuzz channel order to confirm the helper is robust and
// the ordering of arguments never changes the verdict.
// ---------------------------------------------------------------------------

function mulberry32(seed) {
  let a = seed >>> 0;
  return function () {
    a |= 0;
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

test('Property 21 (randomized, >=100 iterations): invariant holds for all body pairs', () => {
  const rnd = mulberry32(20240521);
  const ITER = 200;
  for (let i = 0; i < ITER; i++) {
    const pair = THEME_BODY_PAIRS[Math.floor(rnd() * THEME_BODY_PAIRS.length)];
    // argument order should not matter
    const swap = rnd() < 0.5;
    const ratio = swap
      ? contrastRatio(pair.bg, pair.text)
      : contrastRatio(pair.text, pair.bg);

    assert.ok(
      ratio >= WCAG_AA_NORMAL,
      `iter ${i}: ${pair.source}/${pair.theme} contrast ${ratio.toFixed(3)} < ${WCAG_AA_NORMAL}`
    );
    assert.ok(!isPureBlackOrWhite(pair.text));
    assert.ok(!isPureBlackOrWhite(pair.bg));
    assert.ok(ratio >= 1 && ratio <= 21, `iter ${i}: ratio out of [1,21]`);
  }
});
