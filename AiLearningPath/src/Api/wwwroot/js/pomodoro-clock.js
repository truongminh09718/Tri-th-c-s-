// ====================================================================
// FloatingPomodoroClock — đồng hồ Pomodoro nổi, tái sử dụng được
// --------------------------------------------------------------------
// - Cố định trên màn hình, luôn thấy khi cuộn trang (position: fixed).
// - Trạng thái: idle | running | paused | completed.
// - Nút: bắt đầu / tạm dừng / tiếp tục / đặt lại.
// - Thu nhỏ thành pill "25:00", kéo thả tự do, lưu vị trí + trạng thái
//   thu nhỏ vào localStorage.
// - Vòng tiến trình mini (SVG) quanh đồng hồ.
// - Âm báo nhẹ khi hết giờ (WebAudio, không cần file).
//
// Cách dùng:
//   const clock = createFloatingPomodoroClock({ durationSeconds: 1500 });
//   clock.mount();              // gắn vào trang
//   clock.getElapsedSeconds();  // số giây đã trôi qua (cho thống kê)
//   clock.destroy();            // gỡ khỏi trang
// ====================================================================
"use strict";

(function (global) {
  const STORE_KEY = "tts_pomodoro_clock";

  const SVG = {
    play: '<svg viewBox="0 0 24 24" fill="currentColor" stroke="none"><path d="M8 5v14l11-7z"/></svg>',
    pause: '<svg viewBox="0 0 24 24" fill="currentColor" stroke="none"><rect x="6" y="5" width="4" height="14" rx="1"/><rect x="14" y="5" width="4" height="14" rx="1"/></svg>',
    reset: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 12a9 9 0 1 0 3-6.7L3 8"/><path d="M3 3v5h5"/></svg>',
    minimize: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M5 12h14"/></svg>',
    expand: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 14h6v6"/><path d="M20 10h-6V4"/><path d="M14 10l7-7"/><path d="M3 21l7-7"/></svg>',
  };

  // SVG ring geometry (radius 36 trên viewBox 84) → chu vi cố định.
  const RING_RADIUS = 36;
  const RING_CIRCUMFERENCE = 2 * Math.PI * RING_RADIUS;

  function loadStore() {
    try { return JSON.parse(localStorage.getItem(STORE_KEY)) || {}; }
    catch { return {}; }
  }
  function saveStore(patch) {
    const next = Object.assign(loadStore(), patch);
    try { localStorage.setItem(STORE_KEY, JSON.stringify(next)); } catch { /* bỏ qua */ }
  }

  function fmt(totalSeconds) {
    const m = Math.floor(totalSeconds / 60);
    const s = totalSeconds % 60;
    return `${String(m).padStart(2, "0")}:${String(s).padStart(2, "0")}`;
  }

  function playChime() {
    try {
      const Ctx = global.AudioContext || global.webkitAudioContext;
      if (!Ctx) return;
      const ctx = new Ctx();
      const notes = [880, 1175]; // hai nốt nhẹ
      notes.forEach((freq, i) => {
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.type = "sine";
        osc.frequency.value = freq;
        const t0 = ctx.currentTime + i * 0.18;
        gain.gain.setValueAtTime(0, t0);
        gain.gain.linearRampToValueAtTime(0.12, t0 + 0.02);
        gain.gain.exponentialRampToValueAtTime(0.0001, t0 + 0.35);
        osc.connect(gain).connect(ctx.destination);
        osc.start(t0);
        osc.stop(t0 + 0.4);
      });
      setTimeout(() => ctx.close(), 1200);
    } catch { /* âm báo là tùy chọn, lỗi thì bỏ qua */ }
  }

  /**
   * Tạo một đồng hồ Pomodoro nổi.
   * @param {Object} opts
   * @param {number} [opts.durationSeconds=1500] thời lượng đếm ngược (mặc định 25:00)
   * @param {string} [opts.title="Pomodoro"] nhãn tiêu đề
   * @param {boolean} [opts.sound=true] phát âm báo khi hết giờ
   * @param {Function} [opts.onComplete] callback khi đếm ngược về 0
   */
  function createFloatingPomodoroClock(opts = {}) {
    const duration = Math.max(1, Math.floor(opts.durationSeconds || 25 * 60));
    const title = opts.title || "Pomodoro";
    const soundEnabled = opts.sound !== false;
    const onComplete = typeof opts.onComplete === "function" ? opts.onComplete : null;

    const state = {
      status: "idle",          // idle | running | paused | completed
      secondsLeft: duration,
      elapsedSeconds: 0,
      interval: null,
      minimized: !!loadStore().minimized,
    };

    let root = null;
    const refs = {};

    // ---------- DOM ----------
    function h(tag, attrs = {}, ...kids) {
      const node = document.createElement(tag);
      for (const [k, v] of Object.entries(attrs)) {
        if (k === "class") node.className = v;
        else if (k === "html") node.innerHTML = v;
        else if (k.startsWith("on") && typeof v === "function") node.addEventListener(k.slice(2), v);
        else if (v != null) node.setAttribute(k, v);
      }
      kids.forEach((kid) => kid != null && node.append(kid.nodeType ? kid : document.createTextNode(String(kid))));
      return node;
    }

    function build() {
      refs.time = h("div", { class: "fpc-time" }, fmt(state.secondsLeft));
      refs.ringProg = document.createElementNS("http://www.w3.org/2000/svg", "circle");
      refs.ringProg.setAttribute("class", "fpc-ring-prog");
      refs.ringProg.setAttribute("cx", "42");
      refs.ringProg.setAttribute("cy", "42");
      refs.ringProg.setAttribute("r", String(RING_RADIUS));
      refs.ringProg.setAttribute("stroke-dasharray", String(RING_CIRCUMFERENCE));
      refs.ringProg.setAttribute("stroke-dashoffset", "0");

      const ringTrack = document.createElementNS("http://www.w3.org/2000/svg", "circle");
      ringTrack.setAttribute("class", "fpc-ring-track");
      ringTrack.setAttribute("cx", "42");
      ringTrack.setAttribute("cy", "42");
      ringTrack.setAttribute("r", String(RING_RADIUS));

      const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
      svg.setAttribute("class", "fpc-ring");
      svg.setAttribute("viewBox", "0 0 84 84");
      svg.append(ringTrack, refs.ringProg);

      refs.status = h("div", { class: "fpc-status" }, "Sẵn sàng tập trung");
      refs.mainBtn = h("button", {
        class: "fpc-btn fpc-btn-main", type: "button",
        "aria-label": "Bắt đầu", title: "Bắt đầu", html: SVG.play,
        onclick: onMainClick,
      });
      refs.resetBtn = h("button", {
        class: "fpc-btn", type: "button",
        "aria-label": "Đặt lại", title: "Đặt lại", html: SVG.reset,
        onclick: reset,
      });
      refs.minBtn = h("button", {
        class: "fpc-min", type: "button",
        "aria-label": "Thu nhỏ", title: "Thu nhỏ", html: SVG.minimize,
        onclick: toggleMinimize,
      });
      refs.dot = h("span", { class: "fpc-dot" });
      refs.pillDot = h("span", { class: "fpc-dot" });
      refs.pillTime = h("span", {}, fmt(state.secondsLeft));

      const head = h("div", { class: "fpc-head" },
        h("span", { class: "fpc-title" }, refs.dot, title),
        refs.minBtn);

      const body = h("div", { class: "fpc-body" },
        h("div", { class: "fpc-ring-wrap" }, svg, refs.time),
        refs.status,
        h("div", { class: "fpc-actions" }, refs.resetBtn, refs.mainBtn));

      // Pill khi thu nhỏ: bấm để mở rộng lại.
      const pill = h("div", { class: "fpc-pill", onclick: toggleMinimize, title: "Mở rộng" },
        refs.pillDot, refs.pillTime);

      root = h("div", { class: "fpc", role: "timer", "aria-live": "polite" }, head, body, pill);
      refs.head = head;
      refs.pill = pill;

      enableDrag(head);
      enableDrag(pill);
      applyStoredPosition();
      render();
    }

    // ---------- render trạng thái ----------
    function render() {
      if (!root) return;
      const text = fmt(state.secondsLeft);
      refs.time.textContent = text;
      refs.pillTime.textContent = text;

      const ratio = 1 - state.secondsLeft / duration;
      refs.ringProg.setAttribute("stroke-dashoffset", String(RING_CIRCUMFERENCE * (1 - ratio)));

      root.classList.toggle("is-running", state.status === "running");
      root.classList.toggle("is-completed", state.status === "completed");
      root.classList.toggle("is-minimized", state.minimized);

      const labels = {
        idle: "Sẵn sàng tập trung",
        running: "Đang tập trung",
        paused: "Đang tạm dừng",
        completed: "Hoàn thành",
      };
      refs.status.textContent = labels[state.status];

      const showPause = state.status === "running";
      refs.mainBtn.innerHTML = showPause ? SVG.pause : SVG.play;
      const mainLabel = state.status === "paused" ? "Tiếp tục"
        : state.status === "running" ? "Tạm dừng"
        : state.status === "completed" ? "Bắt đầu lại" : "Bắt đầu";
      refs.mainBtn.setAttribute("aria-label", mainLabel);
      refs.mainBtn.setAttribute("title", mainLabel);
      refs.minBtn.innerHTML = state.minimized ? SVG.expand : SVG.minimize;
    }

    // ---------- điều khiển timer ----------
    function tick() {
      state.secondsLeft = Math.max(0, state.secondsLeft - 1);
      state.elapsedSeconds += 1;
      if (state.secondsLeft === 0) {
        stopInterval();
        state.status = "completed";
        render();
        if (soundEnabled) playChime();
        if (onComplete) onComplete();
      } else {
        render();
      }
    }
    function stopInterval() {
      if (state.interval) clearInterval(state.interval);
      state.interval = null;
    }
    function start() {
      if (state.status === "completed") { state.secondsLeft = duration; }
      stopInterval();
      state.status = "running";
      state.interval = setInterval(tick, 1000);
      render();
    }
    function pause() {
      stopInterval();
      if (state.status === "running") state.status = "paused";
      render();
    }
    function reset() {
      stopInterval();
      state.status = "idle";
      state.secondsLeft = duration;
      state.elapsedSeconds = 0;
      render();
    }
    function onMainClick() {
      if (state.status === "running") pause();
      else start();
    }
    function toggleMinimize() {
      state.minimized = !state.minimized;
      saveStore({ minimized: state.minimized });
      render();
    }

    // ---------- kéo thả ----------
    function enableDrag(handle) {
      let startX, startY, originLeft, originTop, dragging = false;
      handle.addEventListener("pointerdown", (e) => {
        // Không bắt đầu kéo khi bấm vào nút (thu nhỏ).
        if (e.target.closest("button") && e.target.closest(".fpc-min")) return;
        dragging = true;
        root.classList.add("is-dragging");
        const rect = root.getBoundingClientRect();
        originLeft = rect.left; originTop = rect.top;
        startX = e.clientX; startY = e.clientY;
        // Chuyển sang định vị bằng left/top để kéo tự do.
        root.style.left = `${originLeft}px`;
        root.style.top = `${originTop}px`;
        root.style.right = "auto";
        root.style.bottom = "auto";
        handle.setPointerCapture(e.pointerId);
      });
      handle.addEventListener("pointermove", (e) => {
        if (!dragging) return;
        const dx = e.clientX - startX;
        const dy = e.clientY - startY;
        const w = root.offsetWidth, hgt = root.offsetHeight;
        const left = Math.min(Math.max(8, originLeft + dx), global.innerWidth - w - 8);
        const top = Math.min(Math.max(8, originTop + dy), global.innerHeight - hgt - 8);
        root.style.left = `${left}px`;
        root.style.top = `${top}px`;
      });
      const endDrag = (e) => {
        if (!dragging) return;
        dragging = false;
        root.classList.remove("is-dragging");
        try { handle.releasePointerCapture(e.pointerId); } catch { /* noop */ }
        saveStore({ left: parseInt(root.style.left, 10), top: parseInt(root.style.top, 10) });
      };
      handle.addEventListener("pointerup", endDrag);
      handle.addEventListener("pointercancel", endDrag);
    }

    function applyStoredPosition() {
      const s = loadStore();
      if (Number.isFinite(s.left) && Number.isFinite(s.top)) {
        // Giữ trong khung nhìn hiện tại (phòng khi đổi kích thước màn hình).
        const left = Math.min(Math.max(8, s.left), global.innerWidth - 60);
        const top = Math.min(Math.max(8, s.top), global.innerHeight - 60);
        root.style.left = `${left}px`;
        root.style.top = `${top}px`;
        root.style.right = "auto";
        root.style.bottom = "auto";
      }
    }

    // ---------- API công khai ----------
    function mount(parent = document.body) {
      if (root) return api;
      build();
      parent.append(root);
      requestAnimationFrame(() => root.classList.add("is-visible"));
      return api;
    }
    function destroy() {
      stopInterval();
      if (root && root.parentNode) root.parentNode.removeChild(root);
      root = null;
    }

    const api = {
      mount,
      destroy,
      start,
      pause,
      reset,
      getElapsedSeconds: () => state.elapsedSeconds,
      getStatus: () => state.status,
      isMounted: () => !!root,
    };
    return api;
  }

  global.createFloatingPomodoroClock = createFloatingPomodoroClock;
})(window);
