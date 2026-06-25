// ====================================================================
// CoachQuiz — Tab "Chữa bài & giải thích lỗi" và "Kế hoạch học tiếp"
// --------------------------------------------------------------------
// Đọc kết quả các bài Quiz từ vựng IELTS (lưu bởi vocab-quiz.js) để:
//   - mountReview : hiển thị lịch sử luyện tập gần đây.
//   - mountPlan    : tổng hợp điểm yếu và đề xuất kế hoạch học tiếp.
//
// Nguồn dữ liệu: localStorage "tts_vocab_quiz_results", mỗi mục:
//   { topic, score, total, accuracy, wrongQuestions:[...], completedAt }
// ====================================================================
"use strict";

(function (global) {
  const VOCAB_RESULTS_KEY = "tts_vocab_quiz_results";

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

  function getResults() {
    try { return JSON.parse(localStorage.getItem(VOCAB_RESULTS_KEY)) || []; } catch { return []; }
  }

  // ------------------------------------------------------------------
  // Tab "Chữa bài & giải thích lỗi" — lịch sử luyện tập gần đây
  // ------------------------------------------------------------------
  function mountReview(mountSlot) {
    const results = getResults();
    mountSlot.innerHTML = "";
    if (!results.length) {
      mountSlot.append(h("div", { class: "card card-pad-lg" },
        h("div", { class: "section-title" }, "Chữa bài & giải thích lỗi"),
        h("p", { class: "muted", style: "margin:0" }, "Chưa có bài luyện nào được hoàn thành. Hãy làm một bài ở tab Quiz từ vựng IELTS, kết quả và các từ cần ôn sẽ xuất hiện ở đây.")));
      return;
    }
    mountSlot.append(h("div", { class: "card card-pad-lg" },
      h("div", { class: "section-title" }, "Lịch sử luyện tập gần đây"),
      h("p", { class: "muted", style: "margin:0 0 14px" }, "Xem lại kết quả các bài Quiz từ vựng đã làm và những từ cần ôn lại."),
      ...results.slice(0, 12).map((r) => {
        const acc = r.accuracy != null ? r.accuracy : Math.round((r.score / r.total) * 100);
        const wrong = Array.isArray(r.wrongQuestions) ? r.wrongQuestions : [];
        return h("div", { class: "coach-review-item" },
          h("div", { class: "coach-history-row" },
            h("div", {},
              h("b", {}, `Chủ đề: ${r.topic || "Tổng hợp"}`),
              h("span", { class: "muted" }, new Date(r.completedAt).toLocaleString("vi-VN"))),
            h("span", { class: `pill ${acc >= 80 ? "pill-accent" : acc >= 50 ? "" : "pill-warn"}` }, `${r.score}/${r.total} · ${acc}%`)),
          wrong.length
            ? h("div", { class: "chip-row", style: "margin-top:8px" }, ...wrong.map((w) => h("span", { class: "pill pill-warn" }, w)))
            : h("span", { class: "faint", style: "font-size:13px" }, "Không có từ sai. Tuyệt vời!"));
      })));
  }

  // ------------------------------------------------------------------
  // Tab "Kế hoạch học tiếp" — gợi ý từ kết quả
  // ------------------------------------------------------------------
  function mountPlan(mountSlot) {
    const results = getResults();
    mountSlot.innerHTML = "";

    // Tổng hợp chủ đề yếu (accuracy < 70%) và các từ hay sai.
    const weakTopics = {};
    const weakWords = {};
    results.forEach((r) => {
      const acc = r.accuracy != null ? r.accuracy : Math.round((r.score / r.total) * 100);
      if (acc < 70) weakTopics[r.topic || "Tổng hợp"] = (weakTopics[r.topic || "Tổng hợp"] || 0) + 1;
      (r.wrongQuestions || []).forEach((w) => { weakWords[w] = (weakWords[w] || 0) + 1; });
    });
    const topics = Object.entries(weakTopics).sort((a, b) => b[1] - a[1]).map(([t]) => t);
    const words = Object.entries(weakWords).sort((a, b) => b[1] - a[1]).map(([w]) => w).slice(0, 12);

    const steps = [];
    if (topics.length) steps.push(`Ưu tiên ôn lại chủ đề: ${topics.join(", ")} (độ chính xác dưới 70%).`);
    steps.push("Mỗi ngày làm một bài Quiz từ vựng IELTS 10-15 câu để duy trì thói quen.");
    steps.push("Dùng nút \"Luyện lại câu sai\" để củng cố các từ chưa nhớ.");
    steps.push("Khi đạt trên 80%, tăng độ khó hoặc đổi sang chủ đề mới.");

    mountSlot.append(h("div", { class: "card card-pad-lg" },
      h("div", { class: "section-title" }, "Kế hoạch học tiếp"),
      h("p", { class: "muted", style: "margin:0 0 14px" }, results.length
        ? "Gợi ý dựa trên kết quả các bài Quiz từ vựng gần đây của bạn."
        : "Chưa có dữ liệu luyện tập. Hãy hoàn thành vài bài Quiz từ vựng IELTS để AI Coach đề xuất kế hoạch phù hợp."),
      h("ol", { class: "coach-plan-list" }, ...steps.map((s) => h("li", {}, s))),
      words.length
        ? h("div", { style: "margin-top:16px" },
            h("div", { class: "section-title" }, "Từ vựng cần ôn lại"),
            h("div", { class: "chip-row" }, ...words.map((w) => h("span", { class: "pill pill-warn" }, w))))
        : null));
  }

  global.CoachQuiz = {
    getResults,
    mountReview,
    mountPlan,
  };
})(window);
