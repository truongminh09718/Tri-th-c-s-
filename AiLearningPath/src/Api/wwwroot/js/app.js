// ====================================================================
// Tri Thức Số — SPA client for the AI Learning Path API
// ====================================================================
"use strict";

const State = {
  token: localStorage.getItem("tts_token") || null,
  userId: localStorage.getItem("tts_userId") || null,
  email: localStorage.getItem("tts_email") || null,
  name: localStorage.getItem("tts_name") || null,
  profile: null,
  lastAssessment: null,
  lastResult: null,
  route: "profile",
};

// ---------- tiny DOM helpers ----------
const $ = (sel, root = document) => root.querySelector(sel);
const el = (tag, attrs = {}, ...kids) => {
  const node = document.createElement(tag);
  for (const [k, v] of Object.entries(attrs)) {
    if (k === "class") node.className = v;
    else if (k === "html") node.innerHTML = v;
    else if (k.startsWith("on") && typeof v === "function") node.addEventListener(k.slice(2), v);
    else if (v !== null && v !== undefined) node.setAttribute(k, v);
  }
  for (const kid of kids) {
    if (kid == null) continue;
    node.append(kid.nodeType ? kid : document.createTextNode(String(kid)));
  }
  return node;
};

// ---------- toast ----------
function toast(msg, kind = "ok") {
  const t = el("div", { class: `toast ${kind}` }, msg);
  $("#toasts").append(t);
  setTimeout(() => { t.style.opacity = "0"; t.style.transform = "translateY(8px)"; }, 3200);
  setTimeout(() => t.remove(), 3600);
}

// ---------- API client ----------
async function api(path, { method = "GET", body, auth = true, suppressAuthRedirect = false } = {}) {
  const headers = { "Content-Type": "application/json" };
  if (auth && State.token) headers["Authorization"] = `Bearer ${State.token}`;
  let res;
  try {
    res = await fetch(path, { method, headers, body: body ? JSON.stringify(body) : undefined });
  } catch (e) {
    throw new Error("Không kết nối được máy chủ. Vui lòng thử lại.");
  }
  if (res.status === 401 && !suppressAuthRedirect) {
    doLogout();
    throw new Error("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
  }
  const text = await res.text();
  const data = text ? safeJson(text) : null;
  if (!res.ok) {
    const m = data?.error?.message || data?.error || data?.title || `Lỗi máy chủ (${res.status}).`;
    throw new Error(m);
  }
  return data;
}
function safeJson(t) { try { return JSON.parse(t); } catch { return null; } }

// ---------- auth ----------
function persistAuth() {
  localStorage.setItem("tts_token", State.token);
  localStorage.setItem("tts_userId", State.userId);
  localStorage.setItem("tts_email", State.email || "");
  localStorage.setItem("tts_name", State.name || "");
}
function doLogout() {
  State.token = State.userId = State.email = State.name = null;
  State.profile = State.lastAssessment = State.lastResult = null;
  localStorage.clear();
  $("#appShell").classList.add("hidden");
  $("#authScreen").classList.remove("hidden");
}

// JWT carries the user id in "sub" / nameidentifier — decode it client-side.
function decodeUserId(token) {
  try {
    const payload = JSON.parse(atob(token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/")));
    return payload.sub
      || payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"]
      || payload.nameid || payload.userId || null;
  } catch { return null; }
}

// ---------- shared format helpers ----------
const pct = (x) => `${Math.round((x || 0) * 100)}%`;
const num = (x, d = 0) => (x ?? 0).toLocaleString("vi-VN", { maximumFractionDigits: d });
const GOALS = [
  { id: "IELTS", label: "IELTS", score: true },
  { id: "TOEIC", label: "TOEIC", score: true },
  { id: "UniversitySubject", label: "Môn đại học", score: false },
  { id: "FrontendDevelopment", label: "Frontend Development", score: false },
  { id: "BackendDevelopment", label: "Backend Development", score: false },
  { id: "DataAnalyst", label: "Data Analyst", score: false },
  { id: "AIEngineer", label: "AI Engineer", score: false },
];
const goalLabel = (id) => GOALS.find((g) => g.id === id)?.label || id || "Chưa chọn";

function spinner(label = "Đang tải") {
  return el("div", { class: "loading" }, el("span", { class: "spin" }), label + "…");
}
function emptyState(icon, title, desc, actionLabel, onAction) {
  const box = el("div", { class: "empty" },
    el("div", { class: "empty-ic", html: icon }),
    el("h3", {}, title),
    el("p", {}, desc));
  if (actionLabel) box.append(el("button", { class: "btn btn-primary", onclick: onAction }, actionLabel));
  return box;
}


// ====================================================================
// AUTH SCREEN
// ====================================================================
function initAuth() {
  const tabs = document.querySelectorAll(".auth-tab");
  const form = $("#authForm");
  const hint = $("#authSwitchHint");
  let mode = "login";

  // Áp dụng chế độ (login/register) cho toàn bộ giao diện auth: tab active, tiêu đề,
  // nút submit và dòng gợi ý chuyển đổi. Dùng chung cho click tab lẫn click dòng gợi ý.
  function setMode(next) {
    mode = next;
    tabs.forEach((t) => t.classList.toggle("active", t.dataset.mode === mode));
    $("#authSubmit").textContent = mode === "login" ? "Đăng nhập" : "Tạo tài khoản";
    const title = $("#authTitle"), sub = $("#authSub");
    if (title) title.textContent = mode === "login" ? "Chào bạn trở lại." : "Bắt đầu hành trình.";
    if (sub) sub.textContent = mode === "login"
      ? "Đăng nhập để tiếp tục lộ trình học của bạn."
      : "Tạo tài khoản để nhận lộ trình cá nhân hóa.";
    if (hint) {
      hint.innerHTML = mode === "login"
        ? `Chưa có tài khoản? <button type="button" class="auth-switch-link">Chuyển sang Đăng ký</button>`
        : `Đã có tài khoản? <button type="button" class="auth-switch-link">Chuyển sang Đăng nhập</button>`;
    }
    const pwd = $("#authPassword");
    if (pwd) pwd.setAttribute("autocomplete", mode === "login" ? "current-password" : "new-password");
  }

  tabs.forEach((tab) => tab.addEventListener("click", () => setMode(tab.dataset.mode)));

  // Dòng gợi ý: bấm để chuyển sang chế độ còn lại (login ↔ register).
  if (hint) {
    hint.addEventListener("click", (e) => {
      if (e.target.closest(".auth-switch-link")) {
        setMode(mode === "login" ? "register" : "login");
      }
    });
  }

  setMode("login");

  form.addEventListener("submit", async (e) => {
    e.preventDefault();
    const email = $("#authEmail").value.trim();
    const password = $("#authPassword").value;
    const btn = $("#authSubmit");
    btn.disabled = true;
    btn.textContent = mode === "login" ? "Đang đăng nhập…" : "Đang tạo…";
    try {
      if (mode === "register") {
        await api("/api/auth/register", { method: "POST", auth: false, body: { email, password } });
        toast("Tạo tài khoản thành công. Đang đăng nhập…");
      }
      const res = await api("/api/auth/login", {
        method: "POST", auth: false, suppressAuthRedirect: true, body: { email, password },
      });
      State.token = res.token;
      State.userId = decodeUserId(res.token);
      State.email = email;
      persistAuth();
      await enterApp();
    } catch (err) {
      // Đang ĐĂNG KÝ nhưng email đã có tài khoản → chuyển sang ĐĂNG NHẬP.
      if (mode === "register" && /đã được sử dụng|already|EMAIL_ALREADY_USED/i.test(err.message)) {
        setMode("login");
        $("#authEmail").value = email;
        $("#authPassword").focus();
        toast("Email này đã có tài khoản. Hãy đăng nhập.", "ok");
        return;
      }
      // Đang ĐĂNG NHẬP nhưng thông tin không đúng → gợi ý chuyển sang ĐĂNG KÝ.
      if (mode === "login" && /không đúng|Unauthorized|INVALID_CREDENTIALS/i.test(err.message)) {
        setMode("register");
        $("#authEmail").value = email;
        toast("Chưa có tài khoản? Hãy tạo tài khoản mới.", "ok");
        return;
      }
      toast(err.message, "err");
    } finally {
      btn.disabled = false;
      btn.textContent = mode === "login" ? "Đăng nhập" : "Tạo tài khoản";
    }
  });
}

// ====================================================================
// APP SHELL + ROUTER
// ====================================================================
const NAV = [
  { id: "profile", label: "Hồ sơ", icon: "user" },
  { id: "assessment", label: "Đánh giá năng lực", icon: "clipboard" },
  { id: "study", label: "Bắt đầu học", icon: "book" },
  { id: "dna", label: "Learning DNA", icon: "dna" },
  { id: "path", label: "Lộ trình học", icon: "route" },
  { id: "dashboard", label: "Tiến độ", icon: "chart" },
  { id: "ai", label: "AI Coach", icon: "bot" },
  { id: "schedule", label: "Smart Scheduler", icon: "calendar" },
  { id: "adaptive", label: "Adaptive", icon: "adjust" },
  { id: "twin", label: "Academic Twin", icon: "spark" },
  { id: "career", label: "Hướng nghiệp", icon: "briefcase" },
];

async function enterApp() {
  $("#authScreen").classList.add("hidden");
  $("#appShell").classList.remove("hidden");
  $("#navEmail").textContent = State.email || "Sinh viên";
  buildNav();
  // Tải hồ sơ để hiện tên người dùng (nếu có).
  try {
    State.profile = await api(`/api/users/${State.userId}/profile`);
    if (State.profile?.fullName) {
      State.name = State.profile.fullName;
      persistAuth();
      $("#navName").textContent = State.name;
    }
  } catch { /* chưa có hồ sơ — bình thường */ }
  go(State.route || "profile");
}

function buildNav() {
  const nav = $("#navLinks");
  nav.innerHTML = "";
  NAV.forEach((item) => {
    nav.append(el("button", {
      class: "nav-link", "data-route": item.id,
      onclick: () => go(item.id),
    },
      el("span", { class: "nav-ic", html: ICONS[item.icon] || "" }),
      el("span", {}, item.label)));
  });
}

function go(route) {
  stopStudyTimer();
  State.route = route;
  document.querySelectorAll(".nav-link").forEach((l) =>
    l.classList.toggle("active", l.dataset.route === route));
  $("#viewTitle").textContent = NAV.find((n) => n.id === route)?.label || "";
  const view = $("#view");
  view.innerHTML = "";
  view.append(spinner());
  const render = ROUTES[route];
  if (render) render(view);
}

// ====================================================================
// ICONS (Tabler-style stroke set, one family, strokeWidth 2)
// ====================================================================
const ICONS = {
  user: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="8" r="4"/><path d="M4 21v-1a6 6 0 0 1 6-6h4a6 6 0 0 1 6 6v1"/></svg>`,
  clipboard: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="8" y="3" width="8" height="4" rx="1"/><path d="M8 5H6a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-2"/><path d="M9 13l2 2 4-4"/></svg>`,
  dna: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M5 3c0 6 14 6 14 12M19 3c0 6-14 6-14 12M5 21c0-2 14-2 14 0M6 7h12M6 17h12"/></svg>`,
  route: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="6" cy="19" r="3"/><circle cx="18" cy="5" r="3"/><path d="M9 19h5a4 4 0 0 0 4-4V8M6 16V9a4 4 0 0 1 4-4h5"/></svg>`,
  chart: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 3v18h18"/><rect x="7" y="11" width="3" height="6"/><rect x="13" y="7" width="3" height="10"/></svg>`,
  spark: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 3v4M12 17v4M3 12h4M17 12h4M6 6l2 2M16 16l2 2M18 6l-2 2M8 16l-2 2"/><circle cx="12" cy="12" r="3"/></svg>`,
  briefcase: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="7" width="18" height="13" rx="2"/><path d="M8 7V5a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M3 13h18"/></svg>`,
  bot: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="5" y="8" width="14" height="11" rx="2"/><path d="M12 8V4M9 4h6"/><circle cx="9" cy="13" r="1"/><circle cx="15" cy="13" r="1"/><path d="M9 17h6"/></svg>`,
  calendar: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="4" y="5" width="16" height="16" rx="2"/><path d="M16 3v4M8 3v4M4 11h16M8 15h2M13 15h3"/></svg>`,
  adjust: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 7h10M18 7h2M4 17h2M10 17h10"/><circle cx="16" cy="7" r="2"/><circle cx="8" cy="17" r="2"/></svg>`,
  check: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M5 12l4 4L19 6"/></svg>`,
  target: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="9"/><circle cx="12" cy="12" r="5"/><circle cx="12" cy="12" r="1"/></svg>`,
  clock: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 3"/></svg>`,
  award: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="9" r="6"/><path d="M9 14l-1 7 4-2 4 2-1-7"/></svg>`,
  folder: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/></svg>`,
  book: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 5a3 3 0 0 1 3-3h5v18H7a3 3 0 0 0-3 3z"/><path d="M20 5a3 3 0 0 0-3-3h-5v18h5a3 3 0 0 1 3 3z"/></svg>`,
};

// ====================================================================
// ROUTE VIEWS
// ====================================================================
const ROUTES = {};

// ---- helper: section header ----
function viewHead(title, desc) {
  return el("div", { class: "view-head" },
    el("h1", {}, title),
    desc ? el("p", {}, desc) : null);
}
function card(...kids) { return el("div", { class: "card" }, ...kids); }
function statTile(label, value, sub) {
  return el("div", { class: "stat" },
    el("span", { class: "stat-label" }, label),
    el("span", { class: "stat-value" }, value),
    sub ? el("span", { class: "stat-sub" }, sub) : null);
}

// ---- small building blocks reused across views ----
function metric(label, value, sub) {
  return el("div", { class: "metric" },
    el("div", { class: "k" }, label),
    el("div", { class: "v", html: value }),
    sub ? el("div", { class: "faint", style: "font-size:13px;margin-top:4px" }, sub) : null);
}
function goalSelect(current) {
  const sel = el("select", { class: "select", id: "f_goal" });
  sel.append(el("option", { value: "" }, "Chọn mục tiêu học tập"));
  GOALS.forEach((g) => {
    const o = el("option", { value: g.id }, g.label);
    if (g.id === current) o.setAttribute("selected", "selected");
    sel.append(o);
  });
  return sel;
}

// ====================================================================
// 1. PROFILE
// ====================================================================
ROUTES.profile = async (view) => {
  let p = State.profile;
  try { p = await api(`/api/users/${State.userId}/profile`); State.profile = p; }
  catch { p = null; }

  view.innerHTML = "";
  const f = (id, val) => $("#" + id).value;
  const goalNow = p?.learningGoal || "";

  const form = el("form", { class: "card card-pad-lg reveal", id: "profileForm" },
    el("div", { class: "grid cols-2" },
      field("f_name", "Họ và tên", "text", p?.fullName || "", "Nguyễn Văn An"),
      field("f_code", "Mã sinh viên", "text", p?.studentCode || "", "SV2024001"),
    ),
    el("div", { class: "grid cols-2" },
      field("f_major", "Ngành học", "text", p?.major || "", "Khoa học máy tính"),
      field("f_career", "Mục tiêu nghề nghiệp", "text", p?.careerGoal || "", "Kỹ sư phần mềm"),
    ),
    el("div", { class: "grid cols-2" },
      el("div", { class: "field" },
        el("label", { for: "f_goal" }, "Mục tiêu học tập"),
        goalSelect(goalNow)),
      field("f_hours", "Số giờ học mỗi ngày", "number", p?.studyHoursPerDay ?? 2, "0 - 24"),
    ),
    el("div", { class: "field", id: "scoreWrap", style: scoreNeeded(goalNow) ? "" : "display:none" },
      el("label", { for: "f_score" }, "Điểm số mục tiêu"),
      el("input", { class: "input", id: "f_score", type: "number", value: p?.targetScore ?? "", placeholder: "Ví dụ: 7.0 (IELTS) hoặc 800 (TOEIC)" }),
      el("span", { class: "help" }, "Chỉ áp dụng cho mục tiêu yêu cầu điểm như IELTS, TOEIC.")),
    el("div", { class: "row", style: "margin-top:8px" },
      el("button", { class: "btn btn-primary", type: "submit" }, "Lưu hồ sơ")),
  );

  // show/hide target score by goal
  form.addEventListener("change", (e) => {
    if (e.target.id === "f_goal") {
      $("#scoreWrap").style.display = scoreNeeded(e.target.value) ? "" : "none";
    }
  });

  form.addEventListener("submit", async (e) => {
    e.preventDefault();
    const goal = $("#f_goal").value || null;
    const hours = parseFloat($("#f_hours").value);
    if (isNaN(hours) || hours < 0 || hours > 24) { toast("Số giờ học phải trong khoảng 0 đến 24.", "err"); return; }
    const score = $("#f_score").value ? parseInt($("#f_score").value, 10) : null;
    const body = {
      fullName: $("#f_name").value.trim(),
      studentCode: $("#f_code").value.trim(),
      major: $("#f_major").value.trim(),
      careerGoal: $("#f_career").value.trim(),
      learningGoal: goal,
      targetScore: goal && scoreNeeded(goal) ? score : null,
      studyHoursPerDay: hours,
    };
    const btn = e.submitter; btn.disabled = true; btn.textContent = "Đang lưu…";
    try {
      State.profile = await api(`/api/users/${State.userId}/profile`, { method: "PUT", body });
      // also persist goal selection (sets target score when required)
      if (goal) {
        await api(`/api/users/${State.userId}/profile/goal`, { method: "PUT", body: { goal, targetScore: body.targetScore } });
      }
      State.name = body.fullName; persistAuth();
      $("#navName").textContent = body.fullName || "Sinh viên";
      toast("Đã lưu hồ sơ.");
    } catch (err) { toast(err.message, "err"); }
    finally { btn.disabled = false; btn.textContent = "Lưu hồ sơ"; }
  });

  view.append(
    viewHeadDesc("Thiết lập hồ sơ để hệ thống cá nhân hóa lộ trình theo bạn. Mục tiêu học tập quyết định bộ câu hỏi đánh giá và lộ trình sinh ra."),
    form,
  );
};
function scoreNeeded(goal) { return goal === "IELTS" || goal === "TOEIC"; }
function field(id, label, type, val, ph) {
  return el("div", { class: "field" },
    el("label", { for: id }, label),
    el("input", { class: "input", id, type, value: val ?? "", placeholder: ph || "" }));
}
function viewHeadDesc(text) {
  return el("p", { class: "muted", style: "margin:-6px 0 20px;max-width:64ch" }, text);
}

// ====================================================================
// 2. ASSESSMENT
// ====================================================================
ROUTES.assessment = async (view) => {
  view.innerHTML = "";
  const goal = State.profile?.learningGoal;
  view.append(viewHeadDesc("Làm bài đánh giá năng lực đầu vào để hệ thống xác định trình độ, điểm mạnh và điểm yếu của bạn. Kết quả sẽ dùng để xây Learning DNA và sinh lộ trình."));

  if (!goal) {
    view.append(emptyState(ICONS.target, "Chưa có mục tiêu học tập",
      "Hãy chọn một mục tiêu học tập trong Hồ sơ trước khi bắt đầu bài đánh giá.",
      "Tới Hồ sơ", () => go("profile")));
    return;
  }

  if (State.lastResult) { renderAssessmentResult(view, State.lastResult); return; }

  const start = el("div", { class: "card card-pad-lg reveal" },
    el("div", { class: "row between" },
      el("div", {},
        el("div", { class: "section-title" }, "Mục tiêu hiện tại"),
        el("h3", { style: "font-size:22px;margin-top:2px" }, goalLabel(goal))),
      el("span", { class: "pill pill-accent" }, "Sẵn sàng")),
    el("p", { class: "muted", style: "margin:16px 0 20px" }, "Bài đánh giá gồm các câu hỏi trắc nghiệm theo từng lĩnh vực kỹ năng. Trả lời hết tất cả câu hỏi rồi nộp bài."),
    el("button", { class: "btn btn-primary", onclick: (e) => beginAssessment(view, goal, e.target) }, "Bắt đầu bài đánh giá"),
  );
  view.append(start);
};

async function beginAssessment(view, goal, btn) {
  btn.disabled = true; btn.textContent = "Đang tạo câu hỏi…";
  try {
    const a = await api(`/api/users/${State.userId}/assessments/start`, { method: "POST", body: { learningGoal: goal } });
    State.lastAssessment = a;
    renderQuestions(view, a);
  } catch (err) { toast(err.message, "err"); btn.disabled = false; btn.textContent = "Bắt đầu bài đánh giá"; }
}

function renderQuestions(view, a) {
  view.innerHTML = "";
  const answers = {};
  const list = el("div", { class: "reveal" });
  a.questions.forEach((q, i) => {
    const optWrap = el("div", {});
    q.options.forEach((opt) => {
      const id = `q_${q.id}_${opt}`;
      const row = el("label", { class: "opt", for: id },
        el("input", { type: "radio", name: q.id, id, value: opt,
          onchange: () => { answers[q.id] = opt; row.parentElement.querySelectorAll(".opt").forEach((o) => o.classList.remove("sel")); row.classList.add("sel"); } }),
        el("span", {}, opt));
      optWrap.append(row);
    });
    list.append(el("div", { class: "q-card" },
      el("div", { class: "q-skill" }, `Câu ${i + 1} · ${q.skillArea}`),
      el("div", { class: "q-prompt" }, q.prompt),
      optWrap));
  });

  const submit = el("button", { class: "btn btn-primary", style: "margin-top:20px",
    onclick: async (e) => {
      if (Object.keys(answers).length < a.questions.length) { toast("Vui lòng trả lời tất cả câu hỏi trước khi nộp.", "err"); return; }
      e.target.disabled = true; e.target.textContent = "Đang chấm bài…";
      try {
        const payload = { answers: a.questions.map((q) => ({ questionId: q.id, selectedOption: answers[q.id] })) };
        const r = await api(`/api/users/${State.userId}/assessments/${a.id}/submit`, { method: "POST", body: payload });
        State.lastResult = r;
        toast("Đã chấm bài. Learning DNA của bạn đã được tạo.");
        renderAssessmentResult(view, r);
      } catch (err) { toast(err.message, "err"); e.target.disabled = false; e.target.textContent = "Nộp bài"; }
    } }, "Nộp bài");

  view.append(
    el("div", { class: "row between", style: "margin-bottom:16px" },
      el("span", { class: "pill" }, `${a.questions.length} câu hỏi · ${goalLabel(a.learningGoal)}`)),
    list, submit);
}

function renderAssessmentResult(view, r) {
  view.innerHTML = "";
  const chips = (arr, cls) => arr.length
    ? el("div", { class: "chip-row" }, ...arr.map((s) => el("span", { class: `pill ${cls}` }, s)))
    : el("span", { class: "faint" }, "Không có lĩnh vực nào ở nhóm này.");
  view.append(
    el("div", { class: "grid cols-3 reveal" },
      metric("Trình độ", levelLabel(r.level)),
      metric("Điểm số", `${num(r.score, 1)}<small>/100</small>`),
      metric("Lĩnh vực mạnh", String(r.strengths.length))),
    el("div", { class: "grid cols-2", style: "margin-top:18px" },
      el("div", { class: "card" }, el("div", { class: "section-title" }, "Điểm mạnh"), chips(r.strengths, "pill-accent")),
      el("div", { class: "card" }, el("div", { class: "section-title" }, "Cần cải thiện"), chips(r.weaknesses, "pill-warn"))),
    skillBreakdownCard(r.skillBreakdown),
    el("div", { class: "row", style: "margin-top:20px;gap:10px" },
      el("button", { class: "btn btn-primary", onclick: () => go("dna") }, "Xem Learning DNA"),
      el("button", { class: "btn btn-ghost", onclick: () => go("path") }, "Sinh lộ trình học")),
  );
}

// Bảng chi tiết độ chính xác theo từng lĩnh vực kỹ năng (luôn có dữ liệu sau khi nộp bài).
function skillBreakdownCard(breakdown) {
  if (!breakdown || !breakdown.length) return el("span", { style: "display:none" });
  const sorted = [...breakdown].sort((a, b) => b.accuracy - a.accuracy);
  const rows = sorted.map((s) => {
    const pctVal = Math.round((s.accuracy || 0) * 100);
    const tone = s.accuracy >= 0.7 ? "ok" : s.accuracy < 0.5 ? "warn" : "mid";
    return el("div", { class: "skill-row" },
      el("div", { class: "skill-row-head" },
        el("span", { class: "skill-name" }, s.skillArea),
        el("span", { class: `skill-acc skill-acc-${tone}` }, `${s.correctCount}/${s.totalCount} · ${pctVal}%`)),
      el("div", { class: "skill-bar" }, el("div", { class: `skill-bar-fill skill-bar-${tone}`, style: `width:${pctVal}%` })));
  });
  return el("div", { class: "card", style: "margin-top:18px" },
    el("div", { class: "section-title" }, "Chi tiết theo lĩnh vực"),
    el("p", { class: "muted", style: "margin:0 0 14px" }, "Độ chính xác của bạn ở từng lĩnh vực kỹ năng trong bài đánh giá."),
    ...rows);
}
function levelLabel(l) { return { Beginner: "Cơ bản", Intermediate: "Trung cấp", Advanced: "Nâng cao" }[l] || l; }

// ====================================================================
// 3. LEARNING DNA
// ====================================================================
ROUTES.dna = async (view) => {
  view.innerHTML = "";
  view.append(viewHeadDesc("Learning DNA là hồ sơ học tập cá nhân: phong cách học, khung giờ hiệu quả, tốc độ tiếp thu và khả năng tập trung, suy ra từ kết quả đánh giá của bạn."));
  let d;
  try { d = await api(`/api/users/${State.userId}/learning-dna`); }
  catch {
    view.append(emptyState(ICONS.dna, "Chưa có Learning DNA",
      "Hoàn thành bài đánh giá năng lực để hệ thống tự động xây Learning DNA cho bạn.",
      "Làm bài đánh giá", () => go("assessment")));
    return;
  }
  const styleLabel = { Visual: "Trực quan", Auditory: "Thính giác", Kinesthetic: "Vận động", ReadingWriting: "Đọc viết" }[d.learningStyle] || d.learningStyle;
  view.append(
    el("div", { class: "grid cols-3 reveal" },
      metric("Phong cách học", styleLabel),
      metric("Tốc độ tiếp thu", `${num(d.learningSpeed, 2)}×`),
      metric("Khả năng tập trung", pct(d.focusAbility))),
    el("div", { class: "grid cols-2", style: "margin-top:18px" },
      el("div", { class: "card" },
        el("div", { class: "section-title" }, "Khung giờ học hiệu quả"),
        d.effectiveHours.length
          ? el("div", { class: "chip-row" }, ...d.effectiveHours.map((h) => el("span", { class: "pill" }, h)))
          : el("span", { class: "faint" }, "Chưa xác định.")),
      el("div", { class: "card" },
        el("div", { class: "section-title" }, "Điểm mạnh / cần cải thiện"),
        (d.strengths.length || d.weaknesses.length)
          ? el("div", {},
              el("div", { class: "chip-row", style: "margin-bottom:10px" }, ...d.strengths.map((s) => el("span", { class: "pill pill-accent" }, s))),
              el("div", { class: "chip-row" }, ...d.weaknesses.map((s) => el("span", { class: "pill pill-warn" }, s))))
          : el("span", { class: "faint" }, "Làm lại bài đánh giá để cập nhật điểm mạnh và điểm yếu."))),
  );
};

// ====================================================================
// 4. LEARNING PATH
// ====================================================================
ROUTES.path = async (view) => {
  view.innerHTML = "";
  view.append(viewHeadDesc("Sinh lộ trình học tập cá nhân hóa theo tháng, tuần và ngày. Cần hoàn thành bài đánh giá trước. Hệ thống sẽ cảnh báo nếu thời gian mục tiêu không đủ."));

  const goal = State.profile?.learningGoal;
  if (!goal) {
    view.append(emptyState(ICONS.target, "Chưa có mục tiêu học tập",
      "Chọn mục tiêu học tập trong Hồ sơ trước khi sinh lộ trình.",
      "Tới Hồ sơ", () => go("profile")));
    return;
  }

  const gen = el("div", { class: "card card-pad-lg reveal" },
    el("div", { class: "grid cols-2" },
      el("div", { class: "field" },
        el("label", { for: "f_days" }, "Số ngày mục tiêu"),
        el("input", { class: "input", id: "f_days", type: "number", value: "60", min: "1", placeholder: "Ví dụ: 60" }),
        el("span", { class: "help" }, "Tổng số ngày bạn muốn hoàn thành lộ trình.")),
      el("div", { class: "field" },
        el("label", {}, "Mục tiêu"),
        el("div", { class: "input", style: "display:flex;align-items:center;color:var(--text-muted)" }, goalLabel(goal)))),
    el("button", { class: "btn btn-primary", onclick: (e) => generatePath(view, e.target) }, "Sinh lộ trình học"));

  view.append(gen);
  // show last generated path if present this session
  if (State._lastPath) renderPath(view, State._lastPath, false);
};

async function generatePath(view, btn) {
  const days = parseInt($("#f_days").value, 10);
  if (isNaN(days) || days < 1) { toast("Số ngày mục tiêu phải lớn hơn 0.", "err"); return; }
  btn.disabled = true; btn.textContent = "Đang sinh lộ trình…";
  try {
    const p = await api(`/api/users/${State.userId}/learning-paths/generate`, { method: "POST", body: { targetDays: days } });
    State._lastPath = p;
    toast("Đã sinh lộ trình học.");
    renderPath(view, p, true);
  } catch (err) {
    toast(err.message, "err");
    if (/đánh giá|assessment/i.test(err.message)) {
      view.append(emptyState(ICONS.clipboard, "Cần hoàn thành đánh giá",
        "Bạn cần hoàn thành bài đánh giá năng lực trước khi sinh lộ trình.",
        "Làm bài đánh giá", () => go("assessment")));
    }
  } finally { btn.disabled = false; btn.textContent = "Sinh lộ trình học"; }
}

function renderPath(view, p, scroll) {
  // remove previous render
  view.querySelectorAll("[data-path]").forEach((n) => n.remove());
  const wrap = el("div", { "data-path": "1", class: "reveal", style: "margin-top:22px" });

  const totalTasks = p.phases.reduce((s, ph) => s + ph.tasks.length, 0);
  wrap.append(el("div", { class: "grid cols-3" },
    metric("Giai đoạn", String(p.phases.length)),
    metric("Tổng nhiệm vụ", String(totalTasks)),
    metric("Số ngày", String(p.targetDays))));

  if (p.feasibilityWarning) {
    wrap.append(el("div", { class: "card", style: "margin-top:16px;border-color:var(--warning)" },
      el("div", { class: "row", style: "gap:10px" },
        el("span", { class: "pill pill-warn" }, "Cảnh báo khả thi"),
        el("span", { class: "muted" }, "Thời gian mục tiêu có thể không đủ cho khối lượng học ước lượng. Cân nhắc tăng số ngày hoặc số giờ học mỗi ngày."))));
  }

  const phases = el("div", { style: "margin-top:18px" });
  p.phases.forEach((ph, i) => {
    const body = el("div", { class: "phase-body" },
      ...ph.tasks.map((t) => el("div", { class: "task" },
        el("span", { class: "chk", html: ICONS.check }),
        el("div", { class: "tx" }, el("b", {}, t.skill), el("span", {}, " · " + t.description)),
        el("span", { class: "faint", style: "font-size:13px" }, `${num(t.estimatedHours, 1)}h`))));
    const phase = el("div", { class: "phase" + (i === 0 ? " open" : "") },
      el("div", { class: "phase-head", onclick: (e) => e.currentTarget.parentElement.classList.toggle("open") },
        el("b", {}, ph.title),
        el("span", { class: "pill" }, `${ph.tasks.length} nhiệm vụ`)),
      body);
    phases.append(phase);
  });
  wrap.append(phases);
  view.append(wrap);
  if (scroll) wrap.scrollIntoView({ behavior: "smooth", block: "start" });
}

// ====================================================================
// 5. START STUDY
// ====================================================================
const StudyTimer = {
  initialSeconds: 25 * 60,
  secondsLeft: 25 * 60,
  elapsedSeconds: 0,
  interval: null,
};

function stopStudyTimer() {
  if (StudyTimer.interval) clearInterval(StudyTimer.interval);
  StudyTimer.interval = null;
}

ROUTES.study = async (view) => {
  await loadStudyLesson(view);
};

async function loadStudyLesson(view, taskId = null) {
  stopStudyTimer();
  view.innerHTML = "";
  view.append(viewHeadDesc("Học nhiệm vụ tiếp theo trong lộ trình, tập trung theo Pomodoro và ghi nhận tiến độ sau mỗi buổi."));
  view.append(spinner(taskId ? "Đang mở bài học" : "Đang tải bài học"));

  let data;
  const query = taskId ? `?taskId=${encodeURIComponent(taskId)}` : "";
  try { data = await api(`/api/users/${State.userId}/study-lessons/today${query}`); }
  catch (err) { toast(err.message, "err"); return; }

  view.innerHTML = "";
  view.append(viewHeadDesc("Học nhiệm vụ tiếp theo trong lộ trình, tập trung theo Pomodoro và ghi nhận tiến độ sau mỗi buổi."));

  if (data.status !== "ready" || !data.lesson) {
    const states = {
      requiresAssessment: [ICONS.clipboard, "Cần đánh giá năng lực", "Làm bài đánh giá", "assessment"],
      requiresPath: [ICONS.route, "Cần tạo lộ trình học", "Sinh lộ trình", "path"],
      allCompleted: [ICONS.award, "Đã hoàn thành lộ trình", "Xem tiến độ", "dashboard"],
    };
    const state = states[data.status] || [ICONS.book, "Chưa có bài học", "Xem lộ trình", "path"];
    view.append(emptyState(state[0], state[1], data.message, state[2], () => go(state[3])));
    return;
  }

  StudyTimer.secondsLeft = StudyTimer.initialSeconds;
  StudyTimer.elapsedSeconds = 0;
  stopStudyTimer();
  renderStudyLesson(view, data.lesson);
  if (taskId) {
    view.querySelector(".study-hero")?.scrollIntoView({ behavior: "smooth", block: "start" });
    toast(`Đã mở bài ${data.lesson.skill}. Tài liệu và quiz đã được cập nhật.`);
  }
}

function renderStudyLesson(view, lesson) {
  const dateText = new Intl.DateTimeFormat("vi-VN", {
    weekday: "long", day: "2-digit", month: "2-digit", year: "numeric"
  }).format(new Date());

  const hero = el("div", { class: "study-hero reveal" },
    el("div", {},
      el("span", { class: "eyebrow" }, `BÀI HỌC HÔM NAY · ${dateText.toUpperCase()}`),
      el("h2", {}, lesson.skill),
      el("p", {}, lesson.description),
      el("div", { class: "chip-row" },
        el("span", { class: "pill pill-accent" }, lesson.phaseTitle),
        el("span", { class: "pill" }, `${num(lesson.estimatedHours, 1)} giờ dự kiến`))),
    el("div", { class: "study-goal" },
      el("span", { class: "faint" }, "Mục tiêu lộ trình"),
      el("b", {}, goalLabel(lesson.learningGoal))));

  const tasks = el("div", { class: "card study-panel" },
    el("div", { class: "section-title" }, "Nhiệm vụ cần hoàn thành"),
    el("p", { class: "study-task-help" }, "Chọn Mở bài để chuyển tài liệu, quiz và Pomodoro sang môn bạn muốn học."),
    ...lesson.tasks.map((task) => el("div", { class: `study-task${task.isToday ? " is-today" : ""}` },
      el("span", { class: "study-task-mark", html: task.isToday ? ICONS.target : ICONS.clock }),
      el("div", {},
        el("b", {}, task.skill),
        el("span", {}, task.description)),
      task.isToday
        ? el("span", { class: "pill pill-accent" }, "Đang học")
        : el("button", {
            class: "btn btn-primary btn-sm study-task-action",
            type: "button",
            onclick: () => loadStudyLesson(view, task.id)
          }, `Mở bài ${task.skill}`))));

  const materials = el("div", { class: "card study-panel" },
    el("div", { class: "section-title" }, "Tài liệu học"),
    ...lesson.materials.map((material) => el("article", { class: "study-material" },
      el("span", { class: "study-material-icon", html: ICONS.book }),
      el("div", {},
        el("span", { class: "faint" }, material.type),
        el("b", {}, material.title),
        el("p", {}, material.description)))));

  const quizProgress = el("span", { class: "pill", id: "studyQuizProgress" }, `0/${lesson.quizzes.length} câu`);
  const quiz = el("div", { class: "card study-panel" },
    el("div", { class: "row between", style: "margin-bottom:16px" },
      el("div", { class: "section-title", style: "margin-bottom:0" }, "Câu hỏi kiểm tra"),
      quizProgress),
    ...lesson.quizzes.map((question, questionIndex) => buildStudyQuestion(question, questionIndex, lesson.quizzes.length)));

  const timerDisplay = el("div", { class: "pomodoro-time", id: "pomodoroTime" }, "25:00");
  const timerStatus = el("span", { class: "faint", id: "pomodoroStatus" }, "Sẵn sàng tập trung");
  const pomodoro = el("div", { class: "card study-panel pomodoro" },
    el("div", { class: "section-title" }, "Pomodoro"),
    el("div", { class: "pomodoro-ring" }, timerDisplay, timerStatus),
    el("div", { class: "row", style: "justify-content:center;gap:10px" },
      el("button", { class: "btn btn-primary", id: "pomodoroToggle", type: "button", onclick: toggleStudyTimer }, "Bắt đầu"),
      el("button", { class: "btn btn-ghost", type: "button", onclick: resetStudyTimer }, "Đặt lại")));

  const evaluation = buildStudyEvaluation(lesson);
  const completeBar = el("div", { class: "study-complete card" },
    el("div", {},
      el("b", {}, "Bạn đã học xong?"),
      el("span", { class: "muted" }, "Hoàn thành quiz và gửi đánh giá cuối buổi để cập nhật tiến độ.")),
    el("button", { class: "btn btn-primary", type: "button", onclick: () => {
      evaluation.classList.remove("hidden");
      evaluation.scrollIntoView({ behavior: "smooth", block: "center" });
    } }, "Hoàn thành bài học"));

  view.append(hero,
    el("div", { class: "grid cols-2 study-grid", style: "margin-top:18px" }, tasks, materials),
    el("div", { class: "grid cols-2 study-grid", style: "margin-top:18px" }, quiz, pomodoro),
    completeBar,
    evaluation);
}

function buildStudyQuestion(quiz, questionIndex, total) {
  const result = el("div", { class: "quiz-result hidden", "data-quiz-result": String(questionIndex) });
  const options = el("div", { class: "quiz-options" },
    ...quiz.options.map((option, optionIndex) => el("button", {
      class: "quiz-option", type: "button", "data-option": String(optionIndex),
      onclick: (e) => answerStudyQuiz(e.currentTarget, optionIndex, quiz)
    }, el("span", {}, String.fromCharCode(65 + optionIndex)), el("b", {}, option))));
  return el("section", { class: "study-question" },
    el("span", { class: "quiz-number" }, `Câu ${questionIndex + 1}/${total}`),
    el("p", { class: "quiz-question" }, quiz.question),
    options,
    result);
}

function answerStudyQuiz(button, selected, quiz) {
  const question = button.closest(".study-question");
  const options = question.querySelectorAll(".quiz-option");
  options.forEach((option) => {
    option.disabled = true;
    const index = Number(option.dataset.option);
    option.classList.toggle("correct", index === quiz.correctOption);
    option.classList.toggle("wrong", index === selected && index !== quiz.correctOption);
  });
  const correct = selected === quiz.correctOption;
  const result = question.querySelector("[data-quiz-result]");
  result.classList.remove("hidden");
  result.className = `quiz-result ${correct ? "is-correct" : "is-wrong"}`;
  result.textContent = `${correct ? "Chính xác. " : "Chưa chính xác. "}${quiz.explanation}`;
  result.dataset.correct = correct ? "true" : "false";
  result.dataset.answered = "true";

  const allResults = [...document.querySelectorAll("[data-quiz-result]")];
  const answered = allResults.filter((item) => item.dataset.answered === "true").length;
  const progress = $("#studyQuizProgress");
  if (progress) progress.textContent = `${answered}/${allResults.length} câu`;
}

function updateStudyTimerDisplay() {
  const display = $("#pomodoroTime");
  if (!display) return;
  const minutes = Math.floor(StudyTimer.secondsLeft / 60);
  const seconds = StudyTimer.secondsLeft % 60;
  display.textContent = `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
}

function toggleStudyTimer() {
  const button = $("#pomodoroToggle");
  const status = $("#pomodoroStatus");
  if (StudyTimer.interval) {
    stopStudyTimer();
    button.textContent = "Tiếp tục";
    status.textContent = "Đang tạm dừng";
    return;
  }
  button.textContent = "Tạm dừng";
  status.textContent = "Đang tập trung";
  StudyTimer.interval = setInterval(() => {
    StudyTimer.secondsLeft = Math.max(0, StudyTimer.secondsLeft - 1);
    StudyTimer.elapsedSeconds += 1;
    updateStudyTimerDisplay();
    if (StudyTimer.secondsLeft === 0) {
      stopStudyTimer();
      button.textContent = "Bắt đầu lại";
      status.textContent = "Hoàn thành một Pomodoro";
      toast("Hết 25 phút. Hãy nghỉ ngắn trước khi tiếp tục.");
    }
  }, 1000);
}

function resetStudyTimer() {
  stopStudyTimer();
  StudyTimer.secondsLeft = StudyTimer.initialSeconds;
  StudyTimer.elapsedSeconds = 0;
  updateStudyTimerDisplay();
  const button = $("#pomodoroToggle");
  const status = $("#pomodoroStatus");
  if (button) button.textContent = "Bắt đầu";
  if (status) status.textContent = "Sẵn sàng tập trung";
}

function buildStudyEvaluation(lesson) {
  const stars = el("div", { class: "study-stars", role: "group", "aria-label": "Đánh giá buổi học" },
    ...[1, 2, 3, 4, 5].map((rating) => el("button", {
      type: "button", "data-rating": String(rating), "aria-label": `${rating} sao`,
      onclick: (e) => selectStudyRating(e.currentTarget, rating)
    }, "★")));
  return el("div", { class: "card card-pad-lg study-evaluation hidden", id: "studyEvaluation" },
    el("div", { class: "section-title" }, "Đánh giá cuối buổi học"),
    el("p", { class: "muted" }, "Buổi học hôm nay hiệu quả thế nào?"),
    stars,
    el("div", { class: "field", style: "margin-top:16px" },
      el("label", { for: "studyReflection" }, "Bạn đã học được gì?"),
      el("textarea", { class: "input study-reflection", id: "studyReflection", maxlength: "500", rows: "3", placeholder: "Ghi lại một điều bạn hiểu rõ hơn sau buổi học..." })),
    el("button", { class: "btn btn-primary btn-block", type: "button", onclick: (e) => completeStudyLesson(lesson, e.currentTarget) }, "Lưu đánh giá và hoàn thành"));
}

function selectStudyRating(button, rating) {
  const group = button.parentElement;
  group.dataset.rating = String(rating);
  group.querySelectorAll("button").forEach((star) =>
    star.classList.toggle("active", Number(star.dataset.rating) <= rating));
}

async function completeStudyLesson(lesson, button) {
  const rating = Number($("#studyEvaluation .study-stars")?.dataset.rating || 0);
  if (!rating) { toast("Vui lòng đánh giá buổi học từ 1 đến 5 sao.", "err"); return; }
  const quizResults = [...document.querySelectorAll("[data-quiz-result]")];
  if (quizResults.length !== lesson.quizzes.length || quizResults.some((item) => item.dataset.answered !== "true")) {
    toast(`Hãy trả lời đủ ${lesson.quizzes.length} câu hỏi trước.`, "err");
    return;
  }

  stopStudyTimer();
  button.disabled = true;
  button.textContent = "Đang cập nhật tiến độ...";
  try {
    const result = await api(`/api/users/${State.userId}/study-lessons/${lesson.taskId}/complete`, {
      method: "POST",
      body: {
        durationMinutes: Math.max(1, Math.ceil(StudyTimer.elapsedSeconds / 60)),
        rating,
        reflection: $("#studyReflection").value.trim(),
        quizCorrect: quizResults.every((item) => item.dataset.correct === "true"),
      }
    });
    toast(result.message);
    const view = $("#view");
    view.innerHTML = "";
    view.append(emptyState(ICONS.award, "Hoàn thành bài học!",
      `Tiến độ hiện tại ${pct(result.completionRate)} · Learning Score ${num(result.learningScore, 1)}/100.`,
      "Học bài tiếp theo", () => go("study")));
  } catch (err) {
    toast(err.message, "err");
    button.disabled = false;
    button.textContent = "Lưu đánh giá và hoàn thành";
  }
}

// ====================================================================
// 6. PROGRESS DASHBOARD
// ====================================================================
ROUTES.dashboard = async (view) => {
  view.innerHTML = "";
  view.append(viewHeadDesc("Theo dõi Learning Score, tỷ lệ hoàn thành kế hoạch, tổng số giờ học và tiến độ theo tuần, tháng."));
  let d;
  try { d = await api(`/api/users/${State.userId}/dashboard`); }
  catch (err) { toast(err.message, "err"); return; }

  if (d.isEmpty) {
    view.append(emptyState(ICONS.chart, "Chưa có dữ liệu học tập",
      d.guidance || "Hãy sinh lộ trình học và bắt đầu hoàn thành các nhiệm vụ để theo dõi tiến độ.",
      "Sinh lộ trình học", () => go("path")));
    return;
  }

  view.append(el("div", { class: "grid cols-3 reveal" },
    metric("Learning Score", `${num(d.learningScore, 1)}<small>/100</small>`),
    metric("Tỷ lệ hoàn thành", pct(d.completionRate)),
    metric("Tổng giờ học", `${num(d.totalStudyHours, 1)}<small>h</small>`)));

  view.append(el("div", { class: "card", style: "margin-top:18px" },
    el("div", { class: "row between", style: "margin-bottom:12px" },
      el("div", { class: "section-title" }, "Hoàn thành nhiệm vụ"),
      el("span", { class: "faint" }, `${d.completedTasks}/${d.totalTasks} nhiệm vụ`)),
    el("div", { class: "bar" }, el("span", { style: `width:${pct(d.completionRate)}` }))));

  // weekly chart (simple bars)
  if (d.chart?.weekly?.length) {
    const max = Math.max(...d.chart.weekly.map((w) => w.averageLearningScore), 1);
    view.append(el("div", { class: "card", style: "margin-top:18px" },
      el("div", { class: "section-title", style: "margin-bottom:14px" }, "Learning Score theo tuần"),
      el("div", { class: "row weekly-chart", style: "align-items:flex-end;gap:10px;height:140px" },
        ...d.chart.weekly.map((w) => el("div", { class: "wk-col", style: "flex:1;display:flex;flex-direction:column;align-items:center;gap:8px;height:100%;justify-content:flex-end" },
          el("div", { style: `width:100%;max-width:42px;background:linear-gradient(180deg,var(--accent),var(--accent-strong));border-radius:6px 6px 0 0;height:${Math.max(6, (w.averageLearningScore / max) * 100)}%` }),
          el("span", { class: "faint", style: "font-size:11px" }, w.label))))));
  }
};

// ====================================================================
// 6. AI COACH + INSIGHT
// ====================================================================
ROUTES.ai = async (view) => {
  view.innerHTML = "";
  view.append(viewHeadDesc("Trung tâm AI dùng chung: trạng thái provider, insight tiến độ và tutor cá nhân hóa theo hồ sơ, assessment, DNA và lộ trình hiện tại."));

  const statusSlot = el("div", { class: "grid cols-3 reveal" });
  view.append(statusSlot);
  try {
    const s = await api("/api/system/ai-status", { auth: false });
    statusSlot.append(
      aiStatusTile("Gemini", s.gemini?.state || "unknown", s.gemini?.detail),
      aiStatusTile("ML", s.mlService?.state || "unknown", s.mlService?.detail),
      aiStatusTile("Cache", s.cache?.enabled ? "enabled" : "disabled", s.cache ? `${s.cache.ttlMinutes} minute TTL` : null));
  } catch {
    statusSlot.append(aiStatusTile("AI", "unknown", "Không đọc được trạng thái runtime."));
  }

  const insightSlot = el("div", { "data-ai-insight": "1", style: "margin-top:18px" });
  const tutorSlot = el("div", { "data-ai-tutor": "1", style: "margin-top:18px" });
  const question = el("textarea", {
    class: "input ai-textarea",
    id: "f_ai_question",
    rows: "4",
    placeholder: "Hỏi AI tutor về bài học, kỹ năng yếu, cách học tuần này..."
  });

  view.append(
    el("div", { class: "card card-pad-lg reveal", style: "margin-top:18px" },
      el("div", { class: "row between ai-toolbar" },
        el("div", {},
          el("div", { class: "section-title" }, "AI Dashboard Insight"),
          el("div", { class: "muted" }, "Sinh nhận xét ngắn về rủi ro, kỹ năng yếu và khuyến nghị tuần tới.")),
        el("button", { class: "btn btn-primary", onclick: (e) => loadAiInsight(insightSlot, e.target) }, "Tạo insight")),
      insightSlot),
    el("div", { class: "card card-pad-lg reveal", style: "margin-top:18px" },
      el("div", { class: "section-title" }, "AI Tutor"),
      question,
      el("div", { class: "row between ai-toolbar" },
        el("span", { class: "help" }, "Câu trả lời được lưu vào conversation để phục vụ feedback loop."),
        el("button", { class: "btn btn-primary", onclick: (e) => askTutor(question, tutorSlot, e.target) }, "Gửi câu hỏi")),
      tutorSlot));
};

function aiStatusTile(label, status, detail) {
  const live = /configured|ready|enabled|live/i.test(status);
  const warn = /fallback|disabled|unreachable|unknown/i.test(status);
  return el("div", { class: "metric ai-status" },
    el("div", { class: "k" }, label),
    el("div", { class: "v" }, status),
    el("span", { class: `pill ${live ? "pill-accent" : warn ? "pill-warn" : ""}` }, live ? "AI live" : status),
    detail ? el("div", { class: "faint", style: "font-size:13px;margin-top:10px" }, detail) : null);
}

async function loadAiInsight(slot, btn) {
  btn.disabled = true; btn.textContent = "Đang phân tích...";
  slot.innerHTML = ""; slot.append(spinner("Đang sinh insight"));
  try {
    const r = await api(`/api/users/${State.userId}/ai/insights/dashboard`);
    slot.innerHTML = "";
    slot.append(el("div", { class: "ai-answer" },
      el("div", { class: "row", style: "gap:8px;margin-bottom:10px" },
        el("span", { class: `pill ${r.fromCache ? "" : "pill-accent"}` }, r.fromCache ? "cached result" : r.usedFallback ? "fallback deterministic" : "AI live"),
        el("span", { class: "faint" }, `confidence ${num((r.confidence || 0) * 100)}%`)),
      el("p", {}, r.summary || "Chưa có insight."),
      aiList("Rủi ro", r.risks),
      aiList("Kỹ năng yếu", r.weakSkills),
      aiList("Khuyến nghị tuần tới", r.nextWeekRecommendations)));
  } catch (err) {
    slot.innerHTML = "";
    toast(err.message, "err");
  } finally {
    btn.disabled = false; btn.textContent = "Tạo insight";
  }
}

async function askTutor(input, slot, btn) {
  const message = input.value.trim();
  if (!message) { toast("Nhập câu hỏi cho AI Tutor trước.", "err"); return; }
  btn.disabled = true; btn.textContent = "Đang trả lời...";
  slot.innerHTML = ""; slot.append(spinner("AI Tutor đang suy luận"));
  try {
    const r = await api(`/api/users/${State.userId}/ai/tutor/messages`, { method: "POST", body: { message } });
    slot.innerHTML = "";
    slot.append(el("div", { class: "ai-answer" },
      el("div", { class: "row", style: "gap:8px;margin-bottom:10px" },
        el("span", { class: `pill ${r.usedFallback ? "pill-warn" : "pill-accent"}` }, r.usedFallback ? "fallback deterministic" : "AI live"),
        el("span", { class: "faint" }, `confidence ${num((r.confidence || 0) * 100)}%`)),
      el("p", {}, r.answer),
      aiList("Tài liệu gợi ý", r.suggestedResources),
      aiList("Task liên quan", r.relatedTasks),
      el("div", { class: "row", style: "margin-top:12px;gap:8px" },
        el("button", { class: "btn btn-sm btn-ghost", onclick: () => sendAiFeedback("Tutor", r.conversationId, 5) }, "Hữu ích"),
        el("button", { class: "btn btn-sm btn-ghost", onclick: () => sendAiFeedback("Tutor", r.conversationId, 2) }, "Cần cải thiện"))));
  } catch (err) {
    slot.innerHTML = "";
    toast(err.message, "err");
  } finally {
    btn.disabled = false; btn.textContent = "Gửi câu hỏi";
  }
}

function aiList(title, items) {
  return el("div", { class: "ai-list" },
    el("div", { class: "section-title" }, title),
    items?.length
      ? el("div", { class: "chip-row" }, ...items.map((x) => el("span", { class: "pill" }, x)))
      : el("span", { class: "faint" }, "Chưa có dữ liệu."));
}

async function sendAiFeedback(targetType, targetId, rating) {
  try {
    await api(`/api/users/${State.userId}/ai/feedback`, {
      method: "POST",
      body: { targetType, targetId, rating, comment: rating >= 4 ? "Useful demo answer" : "Needs improvement" }
    });
    toast("Đã lưu feedback AI.");
  } catch (err) { toast(err.message, "err"); }
}

// ====================================================================
// 7. SMART SCHEDULER
// ====================================================================
ROUTES.schedule = async (view) => {
  view.innerHTML = "";
  view.append(viewHeadDesc("Sinh lịch học thông minh từ lộ trình mới nhất, số giờ học mỗi tuần, deadline và các khung giờ ưu tiên."));

  const result = el("div", { "data-schedule": "1", style: "margin-top:18px" });
  view.append(el("div", { class: "card card-pad-lg reveal" },
    el("div", { class: "grid cols-3" },
      el("div", { class: "field" },
        el("label", { for: "f_sched_hours" }, "Giờ học mỗi tuần"),
        el("input", { class: "input", id: "f_sched_hours", type: "number", value: "10", min: "1", max: "80" })),
      el("div", { class: "field" },
        el("label", { for: "f_sched_deadline" }, "Deadline"),
        el("input", { class: "input", id: "f_sched_deadline", type: "date" })),
      el("div", { class: "field" },
        el("label", { for: "f_sched_windows" }, "Khung giờ ưu tiên"),
        el("input", { class: "input", id: "f_sched_windows", value: "Mon 19:00, Wed 19:00, Sat 09:00" }))),
    el("button", { class: "btn btn-primary", onclick: (e) => generateSchedule(result, e.target) }, "Tạo lịch học")),
    result);
};

async function generateSchedule(slot, btn) {
  const weeklyHours = parseFloat($("#f_sched_hours").value);
  const deadline = $("#f_sched_deadline").value || null;
  const preferredWindows = $("#f_sched_windows").value.split(",").map((x) => x.trim()).filter(Boolean);
  btn.disabled = true; btn.textContent = "Đang tạo lịch...";
  slot.innerHTML = ""; slot.append(spinner("AI Scheduler đang xếp lịch"));
  try {
    const r = await api(`/api/users/${State.userId}/study-schedule/generate`, {
      method: "POST",
      body: { weeklyHours, deadline, preferredWindows }
    });
    slot.innerHTML = "";
    slot.append(el("div", { class: "stack-lg reveal" },
      el("div", { class: "row", style: "gap:8px" },
        el("span", { class: `pill ${r.usedFallback ? "pill-warn" : "pill-accent"}` }, r.usedFallback ? "fallback deterministic" : "AI live"),
        el("span", { class: "faint" }, r.rationale || "Lịch đã được tạo.")),
      ...r.items.map((x) => el("div", { class: "task" },
        el("span", { class: "chk", html: ICONS.calendar }),
        el("div", { class: "tx" }, el("b", {}, x.title), el("span", {}, ` · ${x.scheduledDate} · ${x.skill}`)),
        el("span", { class: "faint", style: "font-size:13px" }, `${num(x.plannedHours, 1)}h`)))));
  } catch (err) {
    slot.innerHTML = "";
    toast(err.message, "err");
  } finally {
    btn.disabled = false; btn.textContent = "Tạo lịch học";
  }
}

// ====================================================================
// 8. ADAPTIVE LEARNING
// ====================================================================
ROUTES.adaptive = async (view) => {
  view.innerHTML = "";
  view.append(viewHeadDesc("Đề xuất patch lộ trình dựa trên tiến độ và kỹ năng yếu. Bản cũ không bị ghi đè; hệ thống lưu AdaptationEvent để audit."));
  const slot = el("div", { "data-adaptive": "1", style: "margin-top:18px" });

  if (!State._lastPath?.id) {
    view.append(emptyState(ICONS.route, "Chưa có lộ trình trong phiên demo",
      "Sinh lộ trình ở tab Lộ trình học trước, sau đó quay lại đây để AI đề xuất adaptive patch.",
      "Sinh lộ trình học", () => go("path")));
    return;
  }

  view.append(el("div", { class: "card card-pad-lg reveal" },
    el("div", { class: "grid cols-2" },
      el("div", { class: "field" },
        el("label", { for: "f_adapt_weak" }, "Kỹ năng yếu"),
        el("input", { class: "input", id: "f_adapt_weak", value: "foundation, practice consistency" })),
      el("div", { class: "field" },
        el("label", { for: "f_adapt_note" }, "Tín hiệu tiến độ"),
        el("input", { class: "input", id: "f_adapt_note", value: "Learner needs more spaced practice this week" }))),
    el("button", { class: "btn btn-primary", onclick: (e) => adaptPath(slot, e.target) }, "Đề xuất adaptive patch")),
    slot);
};

async function adaptPath(slot, btn) {
  const weakSkills = $("#f_adapt_weak").value.split(",").map((x) => x.trim()).filter(Boolean);
  const progressSignals = [$("#f_adapt_note").value.trim()].filter(Boolean);
  btn.disabled = true; btn.textContent = "Đang adapt...";
  slot.innerHTML = ""; slot.append(spinner("AI đang đề xuất patch"));
  try {
    const r = await api(`/api/users/${State.userId}/learning-paths/${State._lastPath.id}/adapt`, {
      method: "POST",
      body: { weakSkills, progressSignals }
    });
    slot.innerHTML = "";
    slot.append(el("div", { class: "ai-answer reveal" },
      el("div", { class: "row", style: "gap:8px;margin-bottom:10px" },
        el("span", { class: `pill ${r.usedFallback ? "pill-warn" : "pill-accent"}` }, r.usedFallback ? "fallback deterministic" : "AI live"),
        el("span", { class: "faint" }, `${r.addedTasks.length} task mới`)),
      el("p", {}, r.rationale || "Đã tạo adaptation event."),
      aiList("Task thêm vào", r.addedTasks),
      aiList("Task ưu tiên", r.prioritizedTasks)));
  } catch (err) {
    slot.innerHTML = "";
    toast(err.message, "err");
  } finally {
    btn.disabled = false; btn.textContent = "Đề xuất adaptive patch";
  }
}

// ====================================================================
// 9. ACADEMIC TWIN
// ====================================================================
ROUTES.twin = async (view) => {
  view.innerHTML = "";
  view.append(viewHeadDesc("Mô phỏng khả năng đạt mục tiêu theo các mức thời lượng học mỗi ngày. Cần có mục tiêu học tập và kết quả đánh giá."));

  const card1 = el("div", { class: "card card-pad-lg reveal" },
    el("div", { class: "field" },
      el("label", { for: "f_hpd" }, "Số giờ học mỗi ngày"),
      el("input", { class: "input", id: "f_hpd", type: "number", value: "3", min: "0", max: "24", step: "0.5" })),
    el("button", { class: "btn btn-primary", onclick: (e) => simulateTwin(view, e.target) }, "Mô phỏng dự đoán"),
    el("button", { class: "btn btn-ghost", style: "margin-left:10px", onclick: (e) => simulateRange(view, e.target) }, "So sánh nhiều mức (1-6h)"));
  view.append(card1, el("div", { "data-twin": "1" }));
};

async function simulateTwin(view, btn) {
  const h = parseFloat($("#f_hpd").value);
  btn.disabled = true; btn.textContent = "Đang mô phỏng…";
  try {
    const r = await api(`/api/users/${State.userId}/twin/simulate`, { method: "POST", body: { hoursPerDay: h } });
    const slot = view.querySelector("[data-twin]"); slot.innerHTML = "";
    slot.append(el("div", { class: "card reveal", style: "margin-top:18px;text-align:center" },
      el("div", { class: "section-title" }, `Với ${num(h, 1)} giờ/ngày`),
      el("div", { style: "font-size:54px;font-weight:700;color:var(--accent);letter-spacing:-0.03em;margin:8px 0" }, pct(r.successProbability)),
      el("div", { class: "muted" }, "xác suất đạt mục tiêu ước lượng")));
  } catch (err) { handleTwinErr(view, err); }
  finally { btn.disabled = false; btn.textContent = "Mô phỏng dự đoán"; }
}

async function simulateRange(view, btn) {
  btn.disabled = true; btn.textContent = "Đang mô phỏng…";
  try {
    const opts = [1, 2, 3, 4, 5, 6];
    const r = await api(`/api/users/${State.userId}/twin/simulate-range`, { method: "POST", body: { hoursOptions: opts } });
    const slot = view.querySelector("[data-twin]"); slot.innerHTML = "";
    slot.append(el("div", { class: "twin-grid reveal", style: "margin-top:18px" },
      ...r.map((p) => el("div", { class: "twin-cell" },
        el("div", { class: "h" }, `${num(p.hoursPerDay, 1)} giờ/ngày`),
        el("div", { class: "p" }, pct(p.successProbability))))));
  } catch (err) { handleTwinErr(view, err); }
  finally { btn.disabled = false; btn.textContent = "So sánh nhiều mức (1-6h)"; }
}

function handleTwinErr(view, err) {
  toast(err.message, "err");
  if (/đánh giá|mục tiêu|tiên quyết/i.test(err.message)) {
    const slot = view.querySelector("[data-twin]"); slot.innerHTML = "";
    slot.append(emptyState(ICONS.spark, "Chưa đủ điều kiện mô phỏng",
      "Cần có mục tiêu học tập và hoàn thành bài đánh giá năng lực trước khi mô phỏng.",
      "Làm bài đánh giá", () => go("assessment")));
  }
}

// ====================================================================
// 10. CAREER PATH
// ====================================================================
ROUTES.career = async (view) => {
  view.innerHTML = "";
  view.append(viewHeadDesc("Chọn một nghề nghiệp để nhận lộ trình kỹ năng kèm đề xuất chứng chỉ và dự án thực tế."));
  let list = [];
  try { const r = await api(`/api/users/${State.userId}/careers`); list = r.careers || []; }
  catch (err) { toast(err.message, "err"); return; }

  const labels = { Frontend: "Frontend Developer", Backend: "Backend Developer", DataAnalyst: "Data Analyst", AIEngineer: "AI Engineer", Tester: "Tester / QA" };
  const picker = el("div", { class: "grid cols-3 reveal" },
    ...list.map((c) => el("button", { class: "card", style: "text-align:left;cursor:pointer;border-color:var(--border);color:var(--text);font-family:inherit",
      onclick: (e) => generateCareer(view, c, e.currentTarget) },
      el("span", { class: "nav-ic", style: "color:var(--accent)", html: ICONS.briefcase }),
      el("h3", { style: "font-size:17px;margin-top:12px" }, labels[c] || c),
      el("span", { class: "faint", style: "font-size:13px" }, "Xem lộ trình kỹ năng"))));
  view.append(picker, el("div", { "data-career": "1" }));
};

async function generateCareer(view, career, cardEl) {
  cardEl.style.borderColor = "var(--accent)";
  const slot = view.querySelector("[data-career]"); slot.innerHTML = ""; slot.append(spinner("Đang sinh lộ trình nghề nghiệp"));
  try {
    const r = await api(`/api/users/${State.userId}/careers/generate`, { method: "POST", body: { career } });
    slot.innerHTML = "";
    const block = (title, icon, arr, cls) => el("div", { class: "card" },
      el("div", { class: "row", style: "gap:8px;margin-bottom:12px" }, el("span", { class: "nav-ic", style: "color:var(--accent)", html: icon }), el("div", { class: "section-title", style: "margin:0" }, title)),
      arr.length ? el("div", { class: "chip-row" }, ...arr.map((s) => el("span", { class: `pill ${cls}` }, s))) : el("span", { class: "faint" }, "Chưa có dữ liệu."));
    slot.append(el("div", { class: "stack-lg reveal", style: "margin-top:18px" },
      block("Kỹ năng cần học", ICONS.route, r.skills, "pill-accent"),
      el("div", { class: "grid cols-2" },
        block("Chứng chỉ đề xuất", ICONS.award, r.certifications, ""),
        block("Dự án thực tế", ICONS.folder, r.projects, ""))));
  } catch (err) { toast(err.message, "err"); slot.innerHTML = ""; }
}

// ====================================================================
// BOOTSTRAP
// ====================================================================
document.addEventListener("DOMContentLoaded", () => {
  initAuth();
  $("#logoutBtn")?.addEventListener("click", doLogout);
  initSideToggle();
  if (State.token && State.userId) {
    enterApp().catch(() => doLogout());
  } else {
    $("#authScreen").classList.remove("hidden");
  }
});

// ---------- hamburger sidebar toggle (mobile <768px) ----------
function initSideToggle() {
  const toggle = $("#sideToggle");
  const shell = $("#appShell");
  const links = $("#navLinks");
  if (!toggle || !shell) return;
  const setOpen = (open) => {
    shell.classList.toggle("side-open", open);
    toggle.setAttribute("aria-expanded", open ? "true" : "false");
    toggle.setAttribute("aria-label", open ? "Đóng menu" : "Mở menu");
  };
  toggle.addEventListener("click", () => setOpen(!shell.classList.contains("side-open")));
  // close after a nav item is chosen (links are built dynamically)
  links?.addEventListener("click", (e) => { if (e.target.closest(".nav-link")) setOpen(false); });
  document.addEventListener("keydown", (e) => { if (e.key === "Escape") setOpen(false); });
}

