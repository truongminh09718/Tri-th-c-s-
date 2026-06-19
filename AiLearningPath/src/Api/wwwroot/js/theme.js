// ====================================================================
// Tri Thức Số — Theme_Manager (R15 Dark Mode).
// Vanilla JS, no framework. Works in the browser and is testable in Node
// (resolveInitial / opposite are pure-ish and guard window/localStorage/document access).
// Requirements: 15.1, 15.2, 15.3, 15.4
// ====================================================================
"use strict";

var ThemeManager = (function () {
  var STORAGE_KEY = "tts_theme";
  var LIGHT = "light";
  var DARK = "dark";

  // ---- safe environment accessors (so Node tests don't blow up) ----
  function hasWindow() {
    return typeof window !== "undefined" && window !== null;
  }

  function hasDocument() {
    return typeof document !== "undefined" && document !== null && !!document.documentElement;
  }

  function getStorage() {
    try {
      if (hasWindow() && window.localStorage) {
        return window.localStorage;
      }
    } catch (e) {
      // Accessing localStorage can throw (e.g. disabled cookies / sandbox).
    }
    return null;
  }

  function isValidTheme(theme) {
    return theme === LIGHT || theme === DARK;
  }

  // ---- pure logic ------------------------------------------------------

  // opposite(theme): pure, total. Anything that isn't "dark" maps to "light".
  function opposite(theme) {
    return theme === DARK ? LIGHT : DARK;
  }

  // resolveInitial(): pure logic with respect to its inputs.
  // Priority: a valid persisted value in localStorage, otherwise the OS
  // preference via prefers-color-scheme. Optional `deps` lets tests inject
  // a fake storage and matchMedia without touching globals.
  function resolveInitial(deps) {
    deps = deps || {};

    var storage = deps.storage !== undefined ? deps.storage : getStorage();
    var stored = null;
    if (storage) {
      try {
        stored = storage.getItem(STORAGE_KEY);
      } catch (e) {
        stored = null;
      }
    }
    if (isValidTheme(stored)) {
      return stored;
    }

    var prefersDark;
    if (typeof deps.prefersDark === "boolean") {
      prefersDark = deps.prefersDark;
    } else if (hasWindow() && typeof window.matchMedia === "function") {
      prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
    } else {
      prefersDark = false;
    }

    return prefersDark ? DARK : LIGHT;
  }

  // ---- browser-bound behaviour ----------------------------------------

  // getCurrent(): read data-theme from <html>; default to light when unset/invalid.
  function getCurrent() {
    if (!hasDocument()) {
      return LIGHT;
    }
    var theme = document.documentElement.getAttribute("data-theme");
    return isValidTheme(theme) ? theme : LIGHT;
  }

  // apply(theme): set data-theme on <html>. Falls back to light for bad input.
  function apply(theme) {
    var next = isValidTheme(theme) ? theme : LIGHT;
    if (hasDocument()) {
      document.documentElement.setAttribute("data-theme", next);
    }
    return next;
  }

  // persist(theme): write the chosen theme to localStorage.
  function persist(theme) {
    if (!isValidTheme(theme)) {
      return;
    }
    var storage = getStorage();
    if (storage) {
      try {
        storage.setItem(STORAGE_KEY, theme);
      } catch (e) {
        // Best-effort: ignore quota/availability errors.
      }
    }
  }

  // toggle(): flip current theme, apply + persist, refresh toggle button labels.
  function toggle() {
    var next = opposite(getCurrent());
    apply(next);
    persist(next);
    updateToggles(next);
    return next;
  }

  // updateToggles(theme): keep every .theme-toggle button's aria-label and icon
  // in sync with the active theme. Shows the action the button performs.
  function updateToggles(theme) {
    if (!hasDocument()) {
      return;
    }
    var current = isValidTheme(theme) ? theme : getCurrent();
    var willBe = opposite(current);
    var buttons = document.querySelectorAll(".theme-toggle");
    for (var i = 0; i < buttons.length; i++) {
      var btn = buttons[i];
      var label = willBe === DARK ? "Chuyển sang giao diện tối" : "Chuyển sang giao diện sáng";
      btn.setAttribute("aria-label", label);
      btn.setAttribute("title", label);
      btn.setAttribute("aria-pressed", current === DARK ? "true" : "false");
      // Icon reflects the action: moon when next is dark, sun when next is light.
      btn.setAttribute("data-theme-icon", willBe === DARK ? "moon" : "sun");
    }
  }

  // bindToggle(selector): attach click handlers to every matching button.
  function bindToggle(selector) {
    if (!hasDocument()) {
      return;
    }
    var sel = selector || ".theme-toggle";
    var buttons = document.querySelectorAll(sel);
    for (var i = 0; i < buttons.length; i++) {
      var btn = buttons[i];
      if (btn.getAttribute("data-theme-bound") === "true") {
        continue;
      }
      btn.setAttribute("data-theme-bound", "true");
      btn.addEventListener("click", function (event) {
        if (event && typeof event.preventDefault === "function") {
          event.preventDefault();
        }
        toggle();
      });
    }
  }

  // init(): resolve + apply initial theme, then wire up the toggle buttons.
  function init() {
    var initial = resolveInitial();
    apply(initial);
    bindToggle(".theme-toggle");
    updateToggles(initial);
    return initial;
  }

  return {
    STORAGE_KEY: STORAGE_KEY,
    getCurrent: getCurrent,
    resolveInitial: resolveInitial,
    apply: apply,
    toggle: toggle,
    persist: persist,
    opposite: opposite,
    bindToggle: bindToggle,
    updateToggles: updateToggles,
    init: init
  };
})();

// Expose for the browser.
if (typeof window !== "undefined") {
  window.ThemeManager = ThemeManager;
}

// Expose for Node-based tests.
if (typeof module !== "undefined" && module.exports) {
  module.exports = ThemeManager;
}
