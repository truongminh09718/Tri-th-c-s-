// Feature: ai-learning-path, Property 22: Giải quyết và chuyển đổi Theme có tính xác định
// Validates: Requirements 15.1, 15.2, 15.4
//
// Property test (node:test + node:assert) cho ThemeManager.resolveInitial và opposite.
// Chạy: `node --test` trong thư mục này.
"use strict";

const test = require("node:test");
const assert = require("node:assert/strict");
const path = require("node:path");

const ThemeManager = require(path.join(
  __dirname,
  "..",
  "..",
  "src",
  "Api",
  "wwwroot",
  "js",
  "theme.js"
));

const LIGHT = "light";
const DARK = "dark";

// Tạo storage giả với một giá trị tts_theme cho trước (deps.storage cần getItem).
function makeStorage(value) {
  return {
    getItem: function (key) {
      // ThemeManager đọc khóa "tts_theme"; trả value cho mọi khóa là đủ cho test.
      return value;
    }
  };
}

// Hành vi mong đợi của resolveInitial theo đặc tả Property 22.
function expectedResolve(stored, prefersDark) {
  if (stored === LIGHT || stored === DARK) {
    return stored;
  }
  return prefersDark ? DARK : LIGHT;
}

// --- Phần 1: resolveInitial xác định trên toàn tổ hợp hữu hạn ---------------

test("Property 22.1: resolveInitial xác định trên tổ hợp stored cố định × prefersDark", () => {
  const storedValues = [null, undefined, LIGHT, DARK, "", "Light", "DARK", "blue", "  dark  ", "0", "rác"];
  const prefersValues = [true, false];

  for (const stored of storedValues) {
    for (const prefersDark of prefersValues) {
      const result = ThemeManager.resolveInitial({
        storage: makeStorage(stored),
        prefersDark: prefersDark
      });
      const expected = expectedResolve(stored, prefersDark);
      assert.equal(
        result,
        expected,
        `resolveInitial(stored=${JSON.stringify(stored)}, prefersDark=${prefersDark}) = ` +
          `${JSON.stringify(result)}, kỳ vọng ${JSON.stringify(expected)}`
      );
      // Kết quả luôn là một theme hợp lệ.
      assert.ok(result === LIGHT || result === DARK, `Kết quả phải là light/dark, nhận ${result}`);
    }
  }
});

test("Property 22.1: stored hợp lệ luôn được tôn trọng, bất kể prefersDark", () => {
  for (const stored of [LIGHT, DARK]) {
    for (const prefersDark of [true, false]) {
      const result = ThemeManager.resolveInitial({
        storage: makeStorage(stored),
        prefersDark: prefersDark
      });
      assert.equal(result, stored);
    }
  }
});

// PBT: lặp >=100 lần với chuỗi rác ngẫu nhiên để khẳng định fallback theo prefersDark.
test("Property 22.1 (PBT): stored rác ngẫu nhiên → fallback theo prefersDark", () => {
  const ITER = 200;
  for (let i = 0; i < ITER; i++) {
    const stored = randomGarbage();
    const prefersDark = Math.random() < 0.5;
    const result = ThemeManager.resolveInitial({
      storage: makeStorage(stored),
      prefersDark: prefersDark
    });
    const expected = prefersDark ? DARK : LIGHT;
    assert.equal(
      result,
      expected,
      `Với stored rác ${JSON.stringify(stored)} & prefersDark=${prefersDark}, ` +
        `kỳ vọng fallback ${expected}, nhận ${result}`
    );
  }
});

// --- Phần 2: opposite đối xứng (involution) --------------------------------

test("Property 22.2: opposite ánh xạ đúng light<->dark", () => {
  assert.equal(ThemeManager.opposite(LIGHT), DARK);
  assert.equal(ThemeManager.opposite(DARK), LIGHT);
});

test("Property 22.2 (PBT): opposite(opposite(t)) == t với mọi t hợp lệ", () => {
  for (const t of [LIGHT, DARK]) {
    assert.equal(ThemeManager.opposite(ThemeManager.opposite(t)), t);
  }
  // Lặp để nhấn mạnh tính involution trên tập hữu hạn.
  for (let i = 0; i < 100; i++) {
    const t = Math.random() < 0.5 ? LIGHT : DARK;
    assert.equal(ThemeManager.opposite(ThemeManager.opposite(t)), t);
  }
});

// --- Generator chuỗi rác (không trùng "light"/"dark") ----------------------

function randomGarbage() {
  // Đôi khi trả null/undefined/chuỗi rỗng để bao phủ nhánh fallback.
  const roll = Math.random();
  if (roll < 0.1) return null;
  if (roll < 0.2) return undefined;
  if (roll < 0.3) return "";

  const alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 _-#";
  const len = 1 + Math.floor(Math.random() * 12);
  let s = "";
  for (let i = 0; i < len; i++) {
    s += alphabet[Math.floor(Math.random() * alphabet.length)];
  }
  // Đảm bảo không vô tình trùng giá trị hợp lệ.
  if (s === LIGHT || s === DARK) {
    s = s + "_x";
  }
  return s;
}
