// ====================================================================
// Tri Thức Số — Landing (no heavy libraries).
// Stacking = pure CSS sticky. Horizontal = native CSS scroll-snap.
// Reveals + count-up = IntersectionObserver. Native scroll (no Lenis).
// Fully reduced-motion safe. No GSAP, no scroll listeners on the hot path.
// ====================================================================
"use strict";

const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

// ---------- icons (Tabler Icons, MIT-licensed official outline paths, one family) ----------
const SVG = (body) => `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">${body}</svg>`;
const ICONS = {
  user: SVG(`<path d="M8 7a4 4 0 1 0 8 0a4 4 0 0 0 -8 0"/><path d="M6 21v-2a4 4 0 0 1 4 -4h4a4 4 0 0 1 4 4v2"/>`),
  clipboard: SVG(`<path d="M9 5h-2a2 2 0 0 0 -2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2 -2v-12a2 2 0 0 0 -2 -2h-2"/><path d="M9 5a2 2 0 0 1 2 -2h2a2 2 0 0 1 2 2a2 2 0 0 1 -2 2h-2a2 2 0 0 1 -2 -2"/><path d="M9 14l2 2l4 -4"/>`),
  dna: SVG(`<path d="M14.828 14.828a4 4 0 1 0 -5.656 -5.656a4 4 0 0 0 5.656 5.656"/><path d="M9.172 20.485a4 4 0 1 0 -5.657 -5.657"/><path d="M14.828 3.515a4 4 0 0 0 5.657 5.657"/>`),
  route: SVG(`<path d="M3 19a2 2 0 1 0 4 0a2 2 0 0 0 -4 0"/><path d="M19 7a2 2 0 1 0 0 -4a2 2 0 0 0 0 4"/><path d="M11 19h5.5a3.5 3.5 0 0 0 0 -7h-8a3.5 3.5 0 0 1 0 -7h4.5"/>`),
  chart: SVG(`<path d="M3 13a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v6a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1l0 -6"/><path d="M15 9a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v10a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1l0 -10"/><path d="M9 5a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v14a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1l0 -14"/><path d="M4 20h14"/>`),
  spark: SVG(`<path d="M16 18a2 2 0 0 1 2 2a2 2 0 0 1 2 -2a2 2 0 0 1 -2 -2a2 2 0 0 1 -2 2m0 -12a2 2 0 0 1 2 2a2 2 0 0 1 2 -2a2 2 0 0 1 -2 -2a2 2 0 0 1 -2 2m-7 12a6 6 0 0 1 6 -6a6 6 0 0 1 -6 -6a6 6 0 0 1 -6 6a6 6 0 0 1 6 6"/>`),
  briefcase: SVG(`<path d="M3 9a2 2 0 0 1 2 -2h14a2 2 0 0 1 2 2v9a2 2 0 0 1 -2 2h-14a2 2 0 0 1 -2 -2l0 -9"/><path d="M8 7v-2a2 2 0 0 1 2 -2h4a2 2 0 0 1 2 2v2"/><path d="M12 12l0 .01"/><path d="M3 13a20 20 0 0 0 18 0"/>`),
};

// ---------- nav shadow (IntersectionObserver sentinel) ----------
const lnav = document.getElementById("lnav");
if (lnav) {
  const sentinel = document.createElement("div");
  sentinel.style.cssText = "position:absolute;top:0;left:0;height:14px;width:1px;pointer-events:none";
  document.body.prepend(sentinel);
  new IntersectionObserver(
    ([e]) => lnav.classList.toggle("scrolled", !e.isIntersecting),
    { threshold: 0 }
  ).observe(sentinel);
}

// ---------- count-up (IntersectionObserver, rAF, reduced-motion safe) ----------
function countUp(elm) {
  const target = parseFloat(elm.dataset.count);
  const div = parseFloat(elm.dataset.div || "1");
  const dec = parseInt(elm.dataset.dec || "0", 10);
  const fmt = (n) => (n / div).toLocaleString("vi-VN", { minimumFractionDigits: dec, maximumFractionDigits: dec });
  if (reduce) { elm.textContent = fmt(target); return; }
  const dur = 1000, t0 = performance.now();
  const tick = (now) => {
    const p = Math.min(1, (now - t0) / dur);
    const eased = 1 - Math.pow(1 - p, 3);
    elm.textContent = fmt(target * eased);
    if (p < 1) requestAnimationFrame(tick);
    else elm.textContent = fmt(target);
  };
  requestAnimationFrame(tick);
}
const countObs = new IntersectionObserver((entries) => {
  entries.forEach((e) => {
    if (e.isIntersecting) { countUp(e.target); countObs.unobserve(e.target); }
  });
}, { threshold: 0.5 });
document.querySelectorAll("[data-count]").forEach((n) => countObs.observe(n));

// ---------- scroll reveal (one-shot, then unobserve = no ongoing cost) ----------
const revealObs = new IntersectionObserver((entries) => {
  entries.forEach((e) => {
    if (e.isIntersecting) { e.target.classList.add("in"); revealObs.unobserve(e.target); }
  });
}, { threshold: 0.12 });
document.querySelectorAll(".reveal-up").forEach((n) => revealObs.observe(n));
document.querySelectorAll("[data-stagger]").forEach((group) => {
  [...group.children].forEach((k, i) => { k.classList.add("reveal-up"); k.style.transitionDelay = `${i * 70}ms`; revealObs.observe(k); });
});

// reveal the INNER content of each step (never the sticky container itself —
// a transform on a position:sticky element breaks the sticky stacking).
document.querySelectorAll(".story-step").forEach((s) => {
  s.querySelectorAll(".story-text, .story-panel").forEach((n) => { n.classList.add("reveal-up"); revealObs.observe(n); });
});

// ---------- story progress fill (IntersectionObserver per step, no scroll handler) ----------
const fill = document.getElementById("storyFill");
const steps = [...document.querySelectorAll(".story-step")];
if (fill && steps.length) {
  const active = new Set();
  const stepObs = new IntersectionObserver((entries) => {
    entries.forEach((e) => {
      const idx = steps.indexOf(e.target);
      if (e.isIntersecting) active.add(idx); else active.delete(idx);
    });
    const reached = active.size ? Math.max(...active) + 1 : 0;
    fill.style.height = `${(reached / steps.length * 100).toFixed(1)}%`;
  }, { threshold: 0.5 });
  steps.forEach((s) => stepObs.observe(s));
}

// ====================================================================
// STORY demo panels (lightweight structural mocks)
// ====================================================================
(function storyPanels() {
  const panel = (sel) => document.querySelector(`.story-panel[data-demo="${sel}"]`);
  const set = (sel, html) => { const p = panel(sel); if (p) p.innerHTML = html; };
  set("profile", `
    <div class="demo-card">
      <div class="demo-row"><span class="demo-k">Mục tiêu</span><span class="demo-pill">IELTS 7.0</span></div>
      <div class="demo-row"><span class="demo-k">Ngành</span><span class="demo-v">Khoa học máy tính</span></div>
      <div class="demo-row"><span class="demo-k">Giờ học mỗi ngày</span><span class="demo-v">3.0 giờ</span></div>
      <div class="demo-bar"><span style="width:42%"></span></div>
    </div>`);
  set("assess", `
    <div class="demo-card">
      <div class="demo-q">Chọn đáp án đúng nhất cho cấu trúc câu điều kiện loại 2.</div>
      <div class="demo-opt sel">A. If I were you</div>
      <div class="demo-opt">B. If I am you</div>
      <div class="demo-opt">C. If I was you</div>
      <div class="demo-foot"><span class="demo-k">Câu 4 / 10</span><span class="demo-pill">Grammar</span></div>
    </div>`);
  set("dna", `
    <div class="demo-card">
      <div class="demo-grid3">
        <div><div class="demo-big">Trực quan</div><div class="demo-k">Phong cách</div></div>
        <div><div class="demo-big">1.4×</div><div class="demo-k">Tốc độ</div></div>
        <div><div class="demo-big">72%</div><div class="demo-k">Tập trung</div></div>
      </div>
      <div class="demo-chips"><span>06:00-08:00</span><span>20:00-22:00</span></div>
    </div>`);
  set("path", `
    <div class="demo-card">
      <div class="demo-phase"><b>Tháng 1, Tuần 1</b><span class="demo-pill">7 nhiệm vụ</span></div>
      <div class="demo-task"><span class="demo-chk"></span> Luyện nghe học thuật</div>
      <div class="demo-task"><span class="demo-chk on"></span> Câu điều kiện trong ngữ pháp</div>
      <div class="demo-task"><span class="demo-chk"></span> Viết mô tả biểu đồ</div>
    </div>`);
  set("twin", `
    <div class="demo-card">
      <div class="demo-k" style="text-align:center">Với 3 giờ mỗi ngày</div>
      <div class="demo-prob">78%</div>
      <div class="demo-k" style="text-align:center">xác suất đạt mục tiêu</div>
      <div class="demo-spark"><i style="height:30%"></i><i style="height:45%"></i><i style="height:58%"></i><i style="height:70%"></i><i style="height:78%"></i><i style="height:86%"></i></div>
    </div>`);
})();

// ====================================================================
// CAPABILITIES — native horizontal scroll-snap (no pin, no scroll-hijack)
// ====================================================================
(function capabilities() {
  const track = document.getElementById("panTrack");
  if (!track) return;
  const CAPS = [
    { ic: "user", t: "Hồ sơ học tập", d: "Thiết lập mục tiêu, ngành học và quỹ thời gian làm nền cho mọi đề xuất." },
    { ic: "clipboard", t: "Đánh giá năng lực", d: "Bộ câu hỏi thích ứng xác định trình độ, điểm mạnh và điểm yếu thật." },
    { ic: "dna", t: "Learning DNA", d: "Phong cách học, khung giờ hiệu quả, tốc độ tiếp thu và khả năng tập trung." },
    { ic: "route", t: "Lộ trình cá nhân hóa", d: "Kế hoạch theo tháng, tuần, ngày, ưu tiên đúng điểm cần cải thiện." },
    { ic: "chart", t: "Theo dõi tiến độ", d: "Learning Score, tỷ lệ hoàn thành và biểu đồ theo tuần, tháng." },
    { ic: "spark", t: "Academic Twin", d: "Mô phỏng khả năng đạt mục tiêu theo từng mức thời lượng học." },
    { ic: "briefcase", t: "Hướng nghiệp", d: "Lộ trình kỹ năng kèm đề xuất chứng chỉ và dự án theo nghề." },
  ];
  CAPS.forEach((c, i) => {
    const card = document.createElement("article");
    card.className = "pan-card";
    card.innerHTML = `<span class="pic">${ICONS[c.ic]}</span><h3>${c.t}</h3><p>${c.d}</p>`;
    track.append(card);
  });

  // drag-to-scroll for mouse users (native momentum for trackpad/touch already)
  let down = false, startX = 0, startScroll = 0;
  track.addEventListener("pointerdown", (e) => {
    down = true; startX = e.clientX; startScroll = track.scrollLeft; track.setPointerCapture(e.pointerId);
    track.classList.add("dragging");
  });
  track.addEventListener("pointermove", (e) => {
    if (!down) return; track.scrollLeft = startScroll - (e.clientX - startX);
  });
  const end = () => { down = false; track.classList.remove("dragging"); };
  track.addEventListener("pointerup", end);
  track.addEventListener("pointercancel", end);
})();

// ---------- smooth anchor scroll (native) ----------
document.querySelectorAll('a[href^="#"]').forEach((a) => {
  a.addEventListener("click", (e) => {
    const id = a.getAttribute("href");
    if (id.length < 2) return;
    const t = document.querySelector(id);
    if (t) { e.preventDefault(); t.scrollIntoView({ behavior: reduce ? "auto" : "smooth", block: "start" }); }
  });
});
