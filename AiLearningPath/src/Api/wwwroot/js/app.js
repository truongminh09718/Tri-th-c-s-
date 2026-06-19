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
async function api(path, { method = "GET", body, auth = true } = {}) {
  const headers = { "Content-Type": "application/json" };
  if (auth && State.token) headers["Authorization"] = `Bearer ${State.token}`;
  let res;
  try {
    res = await fetch(path, { method, headers, body: body ? JSON.stringify(body) : undefined });
  } catch (e) {
    throw new Error("Không kết nối được máy chủ. Vui lòng thử lại.");
  }
  if (res.status === 401) {
    doLogout();
    throw new Error("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
  }
  const text = await res.text();
  const data = text ? safeJson(text) : null;
  if (!res.ok) {
    const m = data?.error?.message || data?.title || `Lỗi máy chủ (${res.status}).`;
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
  let mode = "login";

  tabs.forEach((tab) => tab.addEventListener("click", () => {
    tabs.forEach((t) => t.classList.remove("active"));
    tab.classList.add("active");
    mode = tab.dataset.mode;
    $("#authSubmit").textContent = mode === "login" ? "Đăng nhập" : "Tạo tài khoản";
    const title = $("#authTitle"), sub = $("#authSub");
    if (title) title.textContent = mode === "login" ? "Chào bạn trở lại." : "Bắt đầu hành trình.";
    if (sub) sub.textContent = mode === "login"
      ? "Đăng nhập để tiếp tục lộ trình học của bạn."
      : "Tạo tài khoản để nhận lộ trình cá nhân hóa.";
    $("#authSwitchHint").textContent = mode === "login"
      ? "Chưa có tài khoản? Chuyển sang Đăng ký."
      : "Đã có tài khoản? Chuyển sang Đăng nhập.";
  }));

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
      const res = await api("/api/auth/login", { method: "POST", auth: false, body: { email, password } });
      State.token = res.token;
      State.userId = decodeUserId(res.token);
      State.email = email;
      persistAuth();
      await enterApp();
    } catch (err) {
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
  { id: "dna", label: "Learning DNA", icon: "dna" },
  { id: "path", label: "Lộ trình học", icon: "route" },
  { id: "dashboard", label: "Tiến độ", icon: "chart" },
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
  check: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M5 12l4 4L19 6"/></svg>`,
  target: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="9"/><circle cx="12" cy="12" r="5"/><circle cx="12" cy="12" r="1"/></svg>`,
  clock: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 3"/></svg>`,
  award: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="9" r="6"/><path d="M9 14l-1 7 4-2 4 2-1-7"/></svg>`,
  folder: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/></svg>`,
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
    const currentGoal = (a.learningGoal || State.profile?.learningGoal || State.learningGoal || "TOEIC").toUpperCase();

const payload = {
  goal: currentGoal.includes("IELTS") ? "IELTS" : "TOEIC",
  answers: a.questions.map((q, index) => {
    const selected = answers[q.id];
    const optionIndex = q.options.indexOf(selected);
    const answerLetter = ["A", "B", "C", "D"][optionIndex] || "";

    return {
      questionId: index + 1,
      answer: answerLetter
    };
  })
};

console.log("GRADE PAYLOAD:", payload);
       const r = await api(`/api/ai-engineer/grade`, {
  method: "POST",
  body: payload
});
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
  if (r.score !== undefined || r.correctAnswers !== undefined) {
  view.append(
    el("div", { class: "grid cols-3 reveal" },
      metric("Score", `${r.score || 0}/100`),
metric("Correct", `${r.correctAnswers || 0}/${r.totalQuestions || 0}`),
metric("Level", r.level || "N/A")
    ),
    el("div", { class: "grid cols-2", style: "margin-top:16px" },
      el("div", { class: "card" },
        el("h3", {}, "Strength"),
        el("p", {}, r.strength || "No data")
      ),
      el("div", { class: "card" },
        el("h3", {}, "Weakness"),
        el("p", {}, r.weakness || "No data")
      )
    ),
    el("div", { class: "card", style: "margin-top:16px" },
      el("h3", {}, "AI Recommendation"),
      el("p", {}, r.advice || "No recommendation")
    ),
    el("div", { class: "card", style: "margin-top:16px" },
      el("h3", {}, "Daily Plan"),
      el("ul", {}, ...(r.dailyPlan || []).map(x => el("li", {}, x)))
    ),
    el("div", { class: "card", style: "margin-top:16px" },
      el("h3", {}, "Weekly Plan"),
      el("ul", {}, ...(r.weeklyPlan || []).map(x => el("li", {}, x)))
    ),
    el("div", { class: "card", style: "margin-top:16px" },
      el("h3", {}, "Monthly Plan"),
      el("ul", {}, ...(r.monthlyPlan || []).map(x => el("li", {}, x)))
    ),
    el("div", { class: "card", style: "margin-top:16px" },
      el("h3", {}, "Badges"),
      el("div", { class: "chip-row" }, ...(r.badges || []).map(x => el("span", { class: "pill" }, x)))
    )
  );
  return;
}
  const chips = (arr, cls) => arr.length
    ? el("div", { class: "chip-row" }, ...arr.map((s) => el("span", { class: `pill ${cls}` }, s)))
    : el("span", { class: "faint" }, "Không có dữ liệu.");
  view.append(
    el("div", { class: "grid cols-3 reveal" },
      metric("Trình độ", levelLabel(r.level)),
      metric("Điểm số", `${num(r.score, 1)}<small>/100</small>`),
      metric("Lĩnh vực mạnh", String(r.strengths.length))),
    el("div", { class: "grid cols-2", style: "margin-top:18px" },
      el("div", { class: "card" }, el("div", { class: "section-title" }, "Điểm mạnh"), chips(r.strengths, "pill-accent")),
      el("div", { class: "card" }, el("div", { class: "section-title" }, "Cần cải thiện"), chips(r.weaknesses, "pill-warn"))),
    el("div", { class: "row", style: "margin-top:20px;gap:10px" },
      el("button", { class: "btn btn-primary", onclick: () => go("dna") }, "Xem Learning DNA"),
      el("button", { class: "btn btn-ghost", onclick: () => go("path") }, "Sinh lộ trình học")),
  );
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
        el("div", { class: "chip-row", style: "margin-bottom:10px" }, ...d.strengths.map((s) => el("span", { class: "pill pill-accent" }, s))),
        el("div", { class: "chip-row" }, ...d.weaknesses.map((s) => el("span", { class: "pill pill-warn" }, s))))),
  );
};

// ====================================================================
// 4. LEARNING PATH
// ====================================================================
ROUTES.path = async (view) => {
  try {
    State.profile = await api(
        `/api/users/${State.userId}/profile`
    );
} catch {}
  view.innerHTML = "";
  view.append(viewHeadDesc("Sinh lộ trình học tập cá nhân hóa theo tháng, tuần và ngày. Cần hoàn thành bài đánh giá trước. Hệ thống sẽ cảnh báo nếu thời gian mục tiêu không đủ."));

  const goal =
    State.profile?.learningGoal ||
    State.profile?.goal ||
    "TOEIC";
  if (!goal) {
    view.append(emptyState(ICONS.target, "Chưa có mục tiêu học tập",
      "Chọn mục tiêu học tập trong Hồ sơ trước khi sinh lộ trình.",
      "Tới Hồ sơ", () => go("profile")));
    return;
  }
if (State.lastResult) {
  view.append(
    el("div", { class: "card" },
      el("h3", {}, "Daily Plan"),
      el("ul", {}, ...(State.lastResult.dailyPlan || []).map(x => el("li", {}, x)))
    ),
    el("div", { class: "card", style: "margin-top:16px" },
      el("h3", {}, "Weekly Plan"),
      el("ul", {}, ...(State.lastResult.weeklyPlan || []).map(x => el("li", {}, x)))
    ),
    el("div", { class: "card", style: "margin-top:16px" },
      el("h3", {}, "Monthly Plan"),
      el("ul", {}, ...(State.lastResult.monthlyPlan || []).map(x => el("li", {}, x)))
    )
  );
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
 // if (State._lastPath) renderPath(view, State._lastPath, false);
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
// 5. PROGRESS DASHBOARD
// ====================================================================
ROUTES.dashboard = async (view) => {
  view.innerHTML = "";
  const r = State.lastResult;

  view.append(viewHeadDesc("Theo dõi kết quả sau bài đánh giá năng lực."));

  if (!r) {
    view.append(emptyState(ICONS.chart, "Chưa có dữ liệu tiến độ",
      "Hãy làm bài đánh giá năng lực trước.",
      "Làm bài đánh giá", () => go("assessment")));
    return;
  }

  view.append(
    el("div", { class: "grid cols-3 reveal" },
      metric("Score", `${r.score || 0}/100`),
      metric("Correct", `${r.correctAnswers || 0}/${r.totalQuestions || 0}`),
      metric("Wrong", `${r.wrongAnswers || 0}`)
    ),
    el("div", { class: "grid cols-2", style: "margin-top:18px" },
      el("div", { class: "card" },
        el("h3", {}, "Strength"),
        el("p", {}, r.strength || "No data")
      ),
      el("div", { class: "card" },
        el("h3", {}, "Weakness"),
        el("p", {}, r.weakness || "No data")
      )
    ),
    el("div", { class: "card", style: "margin-top:18px" },
      el("h3", {}, "Progress Evaluation"),
      el("p", {}, r.evaluation || "No evaluation")
    ),
    el("div", { class: "card", style: "margin-top:18px" },
      el("h3", {}, "Next Step"),
      el("p", {}, r.nextStep || "Keep practicing")
    )
  );
};
// ====================================================================
// 6. ACADEMIC TWIN
// ====================================================================
ROUTES.twin = async (view) => {
  view.innerHTML = "";
  const r = State.lastResult;

  view.append(viewHeadDesc("Mô phỏng khả năng cải thiện dựa trên điểm đánh giá AI Engineer."));

  if (!r) {
    view.append(emptyState(ICONS.spark, "Chưa có dữ liệu mô phỏng",
      "Hãy làm bài đánh giá năng lực trước.",
      "Làm bài đánh giá", () => go("assessment")));
    return;
  }

  const score = r.score || 0;
  const weak = r.weakness || "weak skill";

  view.append(
    el("div", { class: "grid cols-3 reveal" },
      metric("Current Score", `${score}/100`),
      metric("Target Score", `${Math.min(score + 25, 100)}/100`),
      metric("Focus Skill", weak)
    ),
    el("div", { class: "card", style: "margin-top:18px" },
      el("h3", {}, "Prediction"),
      el("p", {}, `If you study 2 hours per day and focus on ${weak}, your score may improve by 15-25 points in 4 weeks.`)
    )
  );
};
// ====================================================================
// 7. CAREER PATH
// ====================================================================
ROUTES.career = async (view) => {
  view.innerHTML = "";
  view.append(viewHeadDesc("Chọn một nghề nghiệp để nhận lộ trình kỹ năng kèm đề xuất chứng chỉ và dự án thực tế."));
  let list = [];
  try { const r = await api(`/api/users/${State.userId}/careers`); list = r.careers || []; }
  catch (err) { toast(err.message, "err"); return; }

  const labels = { Frontend: "Frontend Developer", Backend: "Backend Developer", DataAnalyst: "Data Analyst", AIEngineer: "AI Engineer", Tester: "Tester / QA" };
  const picker = el("div", { class: "grid cols-3 reveal" },
    ...list.map((c) => el("button", { class: "card", style: "text-align:left;cursor:pointer;border-color:var(--border)",
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
  if (State.token && State.userId) {
    enterApp().catch(() => doLogout());
  } else {
    $("#authScreen").classList.remove("hidden");
  }
});
