// ====================================================================
// VocabQuiz — "Quiz từ vựng IELTS" cho AI Coach
// --------------------------------------------------------------------
// Luyện từ vựng IELTS theo chủ đề hoặc ngẫu nhiên. Trả lời từng câu,
// feedback đúng/sai ngay, giải thích sâu, kết thúc có phản hồi cá nhân.
// Không mở link ngoài, không iframe Gemini.
//
// Components (vanilla JS factory, dễ thay mock bằng AI API sau này):
//   - generateQuiz()         : lọc/ghép câu hỏi từ vocabularyBank.
//   - QuizSetupPanel          : chủ đề, số câu, độ khó, dạng luyện.
//   - QuizQuestionCard        : câu hỏi + 4 đáp án + feedback.
//   - QuizExplanationPanel    : giải thích sâu (nghĩa, vì sao, ví dụ...).
//   - QuizResultPanel         : điểm, nhận xét, các hành động tiếp theo.
//
// type VocabularyQuestion = {
//   id, topic, word, meaning, question, options[], correctAnswer,
//   explanation, wrongAnswerExplanations?, exampleSentence?,
//   collocations?, mnemonic?, difficulty }
// type QuizResult = { topic, score, total, wrongQuestions[], accuracy, completedAt }
//
// Kết quả lưu localStorage (tts_vocab_quiz_results) cho Progress /
// Learning DNA / Smart Scheduler.
// ====================================================================
"use strict";

(function (global) {
  const RESULTS_KEY = "tts_vocab_quiz_results";

  const MODE_LABELS = {
    auto: "Tự động", meaning2word: "Nghĩa sang từ", word2meaning: "Từ sang nghĩa",
    fill: "Điền từ", collocation: "Collocation",
  };
  const DIFFICULTY_LABELS = { easy: "Dễ", medium: "Vừa", hard: "Khó" };

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

  function shuffle(arr) {
    const a = arr.slice();
    for (let i = a.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [a[i], a[j]] = [a[j], a[i]];
    }
    return a;
  }

  // ==================================================================
  // MOCK VOCABULARY BANK — dễ thay bằng AI-generated questions.
  // Mỗi từ có nghĩa, ví dụ, collocations, mnemonic, độ khó.
  // ==================================================================
  const vocabularyBank = [
    // environment
    { word: "sustainable", meaning: "bền vững", topic: "environment", difficulty: "medium",
      example: "We need a sustainable approach to energy consumption.",
      collocations: ["sustainable development", "sustainable growth", "environmentally sustainable"],
      mnemonic: "sustain (duy trì) + able → có thể duy trì lâu dài." },
    { word: "emission", meaning: "sự phát thải (khí)", topic: "environment", difficulty: "medium",
      example: "Carbon emissions must be reduced to fight climate change.",
      collocations: ["carbon emissions", "reduce emissions", "greenhouse gas emissions"],
      mnemonic: "emit (phát ra) → emission là danh từ của nó." },
    { word: "conservation", meaning: "sự bảo tồn", topic: "environment", difficulty: "medium",
      example: "Wildlife conservation protects endangered species.",
      collocations: ["wildlife conservation", "energy conservation", "conservation efforts"],
      mnemonic: "conserve (giữ gìn) → conservation." },
    { word: "renewable", meaning: "có thể tái tạo", topic: "environment", difficulty: "easy",
      example: "Solar power is a renewable source of energy.",
      collocations: ["renewable energy", "renewable resources"],
      mnemonic: "re (lại) + new (mới) → làm mới lại được." },
    { word: "deforestation", meaning: "nạn phá rừng", topic: "environment", difficulty: "hard",
      example: "Deforestation contributes to the loss of biodiversity.",
      collocations: ["rapid deforestation", "cause deforestation"],
      mnemonic: "de (mất) + forest (rừng) → mất rừng." },
    // education
    { word: "curriculum", meaning: "chương trình học", topic: "education", difficulty: "medium",
      example: "The school updated its curriculum to include coding.",
      collocations: ["national curriculum", "core curriculum", "design a curriculum"],
      mnemonic: "Liên tưởng 'curri' như 'khóa' học." },
    { word: "literacy", meaning: "khả năng đọc viết", topic: "education", difficulty: "medium",
      example: "Improving literacy rates is a national priority.",
      collocations: ["literacy rate", "digital literacy", "financial literacy"],
      mnemonic: "literature (văn học) → literacy là biết đọc viết." },
    { word: "tuition", meaning: "học phí; sự dạy kèm", topic: "education", difficulty: "medium",
      example: "University tuition has risen sharply in recent years.",
      collocations: ["tuition fees", "private tuition"],
      mnemonic: "tuition fees = học phí." },
    { word: "vocational", meaning: "thuộc về dạy nghề", topic: "education", difficulty: "hard",
      example: "Vocational training prepares students for specific jobs.",
      collocations: ["vocational training", "vocational course"],
      mnemonic: "vocation (nghề nghiệp) → vocational." },
    { word: "scholarship", meaning: "học bổng", topic: "education", difficulty: "easy",
      example: "She earned a scholarship to study abroad.",
      collocations: ["full scholarship", "win a scholarship", "scholarship program"],
      mnemonic: "scholar (học giả) + ship → hỗ trợ học giả." },
    // technology
    { word: "innovation", meaning: "sự đổi mới", topic: "technology", difficulty: "medium",
      example: "Innovation drives the growth of the tech industry.",
      collocations: ["technological innovation", "drive innovation", "innovation hub"],
      mnemonic: "innovate (đổi mới) → innovation." },
    { word: "automation", meaning: "sự tự động hóa", topic: "technology", difficulty: "medium",
      example: "Automation has replaced many manual jobs.",
      collocations: ["industrial automation", "automation technology"],
      mnemonic: "auto (tự động) + mation → tự động hóa." },
    { word: "artificial", meaning: "nhân tạo", topic: "technology", difficulty: "easy",
      example: "Artificial intelligence is transforming healthcare.",
      collocations: ["artificial intelligence", "artificial light"],
      mnemonic: "art (nghệ thuật/nhân tạo) → do con người tạo ra." },
    { word: "obsolete", meaning: "lỗi thời", topic: "technology", difficulty: "hard",
      example: "Older smartphones quickly become obsolete.",
      collocations: ["become obsolete", "render obsolete"],
      mnemonic: "ob + solete nghe như 'sold out' → hàng cũ không còn dùng." },
    { word: "device", meaning: "thiết bị", topic: "technology", difficulty: "easy",
      example: "Mobile devices are now essential for communication.",
      collocations: ["mobile device", "electronic device", "smart device"],
      mnemonic: "device = thiết bị điện tử." },
    // health
    { word: "nutrition", meaning: "dinh dưỡng", topic: "health", difficulty: "medium",
      example: "Good nutrition is vital for children's growth.",
      collocations: ["good nutrition", "poor nutrition", "balanced nutrition"],
      mnemonic: "nutrient (chất dinh dưỡng) → nutrition." },
    { word: "epidemic", meaning: "dịch bệnh", topic: "health", difficulty: "hard",
      example: "The government acted quickly to contain the epidemic.",
      collocations: ["flu epidemic", "contain an epidemic"],
      mnemonic: "epi (trên) + demic (dân) → lan trên dân chúng." },
    { word: "wellbeing", meaning: "sự khỏe mạnh, hạnh phúc", topic: "health", difficulty: "medium",
      example: "Exercise improves both physical and mental wellbeing.",
      collocations: ["mental wellbeing", "general wellbeing"],
      mnemonic: "well (tốt) + being (trạng thái) → trạng thái tốt." },
  ];

  const TOPIC_FALLBACK = "tổng hợp";

  // Sinh một câu hỏi từ một mục từ vựng theo dạng luyện.
  function buildQuestion(entry, mode, allEntries, index) {
    const resolvedMode = mode === "auto"
      ? ["word2meaning", "meaning2word", "fill", "collocation"][index % 4]
      : mode;

    let question, correctAnswer, options;
    const distractorPool = allEntries.filter((e) => e.word !== entry.word);

    if (resolvedMode === "meaning2word") {
      question = `Từ tiếng Anh nào có nghĩa "${entry.meaning}"?`;
      correctAnswer = entry.word;
      options = buildOptions(correctAnswer, shuffle(distractorPool).slice(0, 3).map((e) => e.word));
    } else if (resolvedMode === "fill") {
      const blanked = entry.example.replace(new RegExp(`\\b${escapeRe(entry.word)}\\b`, "i"), "_____");
      question = `Điền từ thích hợp vào chỗ trống: "${blanked}"`;
      correctAnswer = entry.word;
      options = buildOptions(correctAnswer, shuffle(distractorPool).slice(0, 3).map((e) => e.word));
    } else if (resolvedMode === "collocation") {
      const col = (entry.collocations && entry.collocations[0]) || `${entry.word} use`;
      question = `Collocation nào sau đây tự nhiên với "${entry.word}"?`;
      correctAnswer = col;
      const wrongCols = shuffle(distractorPool)
        .map((e) => (e.collocations && e.collocations[0]) || `${e.word} thing`)
        .slice(0, 3);
      options = buildOptions(correctAnswer, wrongCols);
    } else {
      // word2meaning (mặc định)
      question = `"${entry.word}" có nghĩa là gì?`;
      correctAnswer = entry.meaning;
      options = buildOptions(correctAnswer, shuffle(distractorPool).slice(0, 3).map((e) => e.meaning));
    }

    const wrongAnswerExplanations = {};
    options.forEach((opt) => {
      if (opt !== correctAnswer) {
        wrongAnswerExplanations[opt] = `"${opt}" không khớp với ${resolvedMode === "word2meaning" ? "nghĩa của" : "từ"} "${entry.word}".`;
      }
    });

    return {
      id: `vq_${entry.word}_${index}`,
      topic: entry.topic,
      word: entry.word,
      meaning: entry.meaning,
      question,
      options,
      correctAnswer,
      explanation: `"${entry.word}" nghĩa là "${entry.meaning}".`,
      wrongAnswerExplanations,
      exampleSentence: entry.example,
      collocations: entry.collocations || [],
      mnemonic: entry.mnemonic || "",
      difficulty: entry.difficulty,
    };
  }

  function escapeRe(s) { return s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&"); }
  function buildOptions(correct, wrongs) {
    const set = [correct];
    for (const w of wrongs) { if (w && !set.includes(w)) set.push(w); }
    // Bổ sung nếu thiếu (bank nhỏ).
    while (set.length < 4) set.push(`lựa chọn ${set.length + 1}`);
    return shuffle(set.slice(0, 4));
  }

  // generateQuiz — lọc theo topic + difficulty, fallback random.
  function generateQuiz({ topic, count, difficulty, mode }) {
    const wantTopic = (topic || "").trim().toLowerCase();
    let pool = vocabularyBank;
    if (wantTopic) pool = vocabularyBank.filter((e) => e.topic.toLowerCase() === wantTopic);
    if (difficulty && difficulty !== "any") {
      const filtered = pool.filter((e) => e.difficulty === difficulty);
      if (filtered.length >= 4) pool = filtered;
    }
    let usedTopic = wantTopic && pool.length ? topic.trim() : TOPIC_FALLBACK;
    if (pool.length < 4) { pool = vocabularyBank; usedTopic = TOPIC_FALLBACK; }

    const entries = shuffle(pool);
    const n = Math.min(count, entries.length);
    const questions = entries.slice(0, n).map((e, i) => buildQuestion(e, mode, vocabularyBank, i));
    return { topic: usedTopic, difficulty, mode, questions };
  }

  // ==================================================================
  // Lưu kết quả
  // ==================================================================
  function saveResult(result) {
    let list = [];
    try { list = JSON.parse(localStorage.getItem(RESULTS_KEY)) || []; } catch { list = []; }
    list.unshift(result);
    try { localStorage.setItem(RESULTS_KEY, JSON.stringify(list.slice(0, 50))); } catch { /* bỏ qua */ }
  }

  // ==================================================================
  // QuizExplanationPanel — giải thích sâu
  // ==================================================================
  function QuizExplanationPanel(q) {
    const wrongList = Object.entries(q.wrongAnswerExplanations || {});
    return h("div", { class: "vq-explain reveal" },
      h("span", { class: "lesson-section-kind" }, "Giải thích sâu hơn"),
      h("div", { class: "vq-explain-grid" },
        explainRow("Từ đúng", q.word),
        explainRow("Nghĩa tiếng Việt", q.meaning)),
      h("p", { class: "vq-explain-line" }, h("b", {}, "Vì sao đúng: "), q.explanation),
      wrongList.length
        ? h("div", {}, h("b", { class: "vq-explain-sub" }, "Vì sao đáp án khác sai:"),
            h("ul", { class: "vq-explain-ul" }, ...wrongList.map(([opt, why]) => h("li", {}, `${opt}: ${why}`))))
        : null,
      q.exampleSentence ? h("p", { class: "vq-explain-line" }, h("b", {}, "Ví dụ IELTS: "), h("i", {}, q.exampleSentence)) : null,
      q.collocations && q.collocations.length
        ? h("div", {}, h("b", { class: "vq-explain-sub" }, "Collocations thường gặp:"),
            h("div", { class: "chip-row" }, ...q.collocations.map((c) => h("span", { class: "pill" }, c))))
        : null,
      q.mnemonic ? h("p", { class: "vq-explain-line vq-mnemonic" }, h("b", {}, "Mẹo ghi nhớ: "), q.mnemonic) : null);
  }
  function explainRow(k, v) {
    return h("div", { class: "vq-explain-cell" },
      h("span", { class: "coach-meta-k" }, k),
      h("span", { class: "coach-meta-v" }, v));
  }

  // ==================================================================
  // QuizQuestionCard + Runner
  // ==================================================================
  function QuizRunner(quiz, mountSlot, onReplay) {
    let index = 0;
    let score = 0;
    const answers = new Array(quiz.questions.length).fill(null);

    function renderQuestion() {
      const q = quiz.questions[index];
      mountSlot.innerHTML = "";

      const head = h("div", { class: "vq-head" },
        h("span", { class: "pill" }, `Câu ${index + 1} / ${quiz.questions.length}`),
        h("span", { class: "pill pill-accent" }, `Điểm: ${score}`));

      const bar = h("div", { class: "quiz-progressbar" },
        h("div", { class: "quiz-progressbar-fill", style: `width:${Math.round((index / quiz.questions.length) * 100)}%` }));

      const feedback = h("div", { class: "vq-feedback hidden" });
      const explainSlot = h("div", { class: "vq-explain-slot" });
      const nextBtn = h("button", { class: "btn btn-primary hidden", type: "button" },
        index + 1 < quiz.questions.length ? "Câu tiếp theo" : "Xem kết quả");
      const explainBtn = h("button", { class: "btn btn-ghost hidden", type: "button" }, "Giải thích sâu hơn");

      const optionGrid = h("div", { class: "vq-options" },
        ...q.options.map((opt) => h("button", { class: "vq-option", type: "button",
          onclick: (e) => onAnswer(e.currentTarget, opt) }, opt)));

      function onAnswer(btn, chosen) {
        const buttons = [...optionGrid.querySelectorAll(".vq-option")];
        buttons.forEach((b) => {
          b.disabled = true;
          if (b.textContent === q.correctAnswer) b.classList.add("correct");
          else if (b === btn) b.classList.add("wrong");
        });
        const ok = chosen === q.correctAnswer;
        if (ok) score += 1;
        answers[index] = chosen;
        feedback.classList.remove("hidden");
        feedback.className = `vq-feedback ${ok ? "is-correct" : "is-wrong"}`;
        feedback.textContent = ok ? "Chính xác!" : `Chưa đúng. Đáp án đúng: ${q.correctAnswer}.`;
        head.querySelector(".pill-accent").textContent = `Điểm: ${score}`;
        explainBtn.classList.remove("hidden");
        nextBtn.classList.remove("hidden");
      }

      explainBtn.addEventListener("click", () => {
        if (explainSlot.childElementCount) { explainSlot.innerHTML = ""; explainBtn.textContent = "Giải thích sâu hơn"; }
        else { explainSlot.append(QuizExplanationPanel(q)); explainBtn.textContent = "Ẩn giải thích"; }
      });
      nextBtn.addEventListener("click", () => {
        if (index + 1 < quiz.questions.length) { index += 1; renderQuestion(); mountSlot.scrollIntoView({ behavior: "smooth", block: "start" }); }
        else { showResult(); }
      });

      mountSlot.append(h("div", { class: "card card-pad-lg vq-card reveal" },
        head, bar,
        h("p", { class: "vq-question" }, q.question),
        optionGrid,
        feedback,
        h("div", { class: "practice-actions", style: "margin-top:14px" }, explainBtn, nextBtn),
        explainSlot));
    }

    function showResult() {
      const total = quiz.questions.length;
      const accuracy = Math.round((score / total) * 100);
      const wrongQuestions = quiz.questions.filter((q, i) => answers[i] !== q.correctAnswer);
      saveResult({ topic: quiz.topic, score, total, accuracy, wrongQuestions: wrongQuestions.map((q) => q.word), completedAt: new Date().toISOString() });
      mountSlot.innerHTML = "";
      mountSlot.append(QuizResultPanel(quiz, score, answers, wrongQuestions, onReplay));
    }

    renderQuestion();
  }

  // ==================================================================
  // QuizResultPanel
  // ==================================================================
  function QuizResultPanel(quiz, score, answers, wrongQuestions, onReplay) {
    const total = quiz.questions.length;
    const accuracy = Math.round((score / total) * 100);
    const verdict = accuracy >= 80 ? "Rất tốt! Vốn từ chủ đề này của bạn khá vững."
      : accuracy >= 50 ? "Có nền tảng, cần ôn thêm các từ còn sai."
      : "Nên luyện lại chủ đề này để nhớ từ chắc hơn.";

    const note = h("div", { class: "vq-coach-note hidden" });

    function personalFeedback() {
      const weak = wrongQuestions.map((q) => q.word).slice(0, 6);
      note.classList.remove("hidden");
      note.innerHTML = "";
      note.append(
        h("span", { class: "lesson-section-kind" }, "Phản hồi cá nhân hóa"),
        h("p", {}, weak.length
          ? `Bạn cần tập trung vào ${weak.length} từ: ${weak.join(", ")}. Hãy đọc ví dụ IELTS và collocation của từng từ, sau đó luyện lại câu sai.`
          : "Bạn trả lời đúng toàn bộ. Hãy thử chủ đề khác hoặc tăng độ khó để mở rộng vốn từ."));
    }
    function reviewStory() {
      const words = quiz.questions.map((q) => q.word).slice(0, 6);
      note.classList.remove("hidden");
      note.innerHTML = "";
      note.append(
        h("span", { class: "lesson-section-kind" }, "Câu chuyện ôn tập"),
        h("p", {}, buildStory(words, quiz.topic)));
    }

    return h("div", { class: "vq-result reveal" },
      h("div", { class: "coach-result-score" },
        h("div", { class: "coach-result-pct" }, `${score}/${total}`),
        h("div", {},
          h("b", {}, `${accuracy}% chính xác`),
          h("span", { class: "muted" }, verdict))),
      wrongQuestions.length
        ? h("div", { class: "coach-result-wrong" },
            h("div", { class: "section-title" }, `Từ cần ôn lại (${wrongQuestions.length})`),
            h("div", { class: "chip-row" }, ...wrongQuestions.map((q) => h("span", { class: "pill pill-warn" }, `${q.word} (${q.meaning})`))))
        : h("p", { class: "coach-allcorrect" }, "Bạn trả lời đúng tất cả. Tuyệt vời!"),
      note,
      h("div", { class: "practice-actions" },
        h("button", { class: "btn btn-primary", type: "button", onclick: personalFeedback }, "Nhận phản hồi cá nhân hóa"),
        h("button", { class: "btn btn-ghost", type: "button", onclick: reviewStory }, "Tạo câu chuyện ôn tập"),
        wrongQuestions.length
          ? h("button", { class: "btn btn-ghost", type: "button", onclick: (e) => {
              const slot = e.currentTarget.closest(".vq-result").parentElement;
              const retryQuiz = { topic: quiz.topic, difficulty: quiz.difficulty, mode: quiz.mode, questions: wrongQuestions };
              QuizRunner(retryQuiz, slot, onReplay);
            } }, "Luyện lại câu sai")
          : null,
        h("button", { class: "btn btn-ghost", type: "button", onclick: () => exportReport(quiz, score, total, accuracy, wrongQuestions) }, "Xuất báo cáo"),
        h("button", { class: "btn btn-ghost", type: "button", onclick: onReplay }, "Chơi lại")));
  }

  function buildStory(words, topic) {
    if (!words.length) return "Chưa có từ để tạo câu chuyện.";
    return `Một câu chuyện ngắn về ${topic}: ` +
      words.map((w) => `từ "${w}"`).join(", ") +
      ` xuất hiện cùng nhau. Hãy thử viết một đoạn 3-4 câu của riêng bạn dùng tất cả các từ này để ghi nhớ tốt hơn.`;
  }

  function exportReport(quiz, score, total, accuracy, wrongQuestions) {
    const lines = [
      `BÁO CÁO QUIZ TỪ VỰNG IELTS`,
      `Chủ đề: ${quiz.topic}`,
      `Điểm: ${score}/${total} (${accuracy}%)`,
      `Thời gian: ${new Date().toLocaleString("vi-VN")}`,
      ``,
      `Từ cần ôn lại:`,
      ...(wrongQuestions.length ? wrongQuestions.map((q) => `- ${q.word}: ${q.meaning} | ${q.exampleSentence || ""}`) : ["(không có)"]),
    ];
    const blob = new Blob([lines.join("\n")], { type: "text/plain;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url; a.download = `bao-cao-quiz-${quiz.topic}.txt`;
    document.body.append(a); a.click(); a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }

  // ==================================================================
  // QuizSetupPanel + entry point
  // ==================================================================
  function mountVocabularyQuiz(mountSlot) {
    function showSetup() {
      mountSlot.innerHTML = "";
      const topicInput = h("input", { class: "input", type: "text", placeholder: "Ví dụ: environment, education, technology, health" });
      const countSelect = h("select", { class: "select" }, ...[5, 10, 15].map((n) => h("option", { value: String(n) }, `${n} câu`)));
      const diffSelect = h("select", { class: "select" }, ...Object.entries(DIFFICULTY_LABELS).map(([v, l]) => h("option", { value: v }, l)));
      diffSelect.value = "medium";
      const modeSelect = h("select", { class: "select" }, ...Object.entries(MODE_LABELS).map(([v, l]) => h("option", { value: v }, l)));
      const runSlot = h("div", { style: "margin-top:18px" });
      const errorSlot = h("div", { class: "practice-error hidden", style: "margin-top:10px" });

      const start = (topic, requireTopic) => {
        if (requireTopic && !(topic || "").trim()) {
          errorSlot.textContent = "Vui lòng nhập chủ đề IELTS bạn muốn luyện.";
          errorSlot.classList.remove("hidden");
          return;
        }
        errorSlot.classList.add("hidden");
        const quiz = generateQuiz({
          topic, count: Number(countSelect.value), difficulty: diffSelect.value, mode: modeSelect.value,
        });
        if (!quiz.questions.length) { return; }
        runSlot.innerHTML = "";
        QuizRunner(quiz, runSlot, showSetup);
        runSlot.scrollIntoView({ behavior: "smooth", block: "start" });
      };

      mountSlot.append(
        h("div", { class: "card card-pad-lg reveal" },
          h("div", { class: "section-title" }, "Quiz từ vựng IELTS"),
          h("p", { class: "muted", style: "margin:0 0 14px" }, "Luyện từ vựng IELTS theo chủ đề hoặc ngẫu nhiên. Trả lời từng câu, nhận feedback và giải thích sâu, kết thúc có phản hồi cá nhân hóa."),
          h("div", { class: "field" }, h("label", {}, "Chủ đề"), topicInput),
          h("div", { class: "grid cols-3" },
            h("div", { class: "field" }, h("label", {}, "Số câu"), countSelect),
            h("div", { class: "field" }, h("label", {}, "Độ khó"), diffSelect),
            h("div", { class: "field" }, h("label", {}, "Dạng luyện"), modeSelect)),
          errorSlot,
          h("div", { class: "practice-actions", style: "margin-top:14px" },
            h("button", { class: "btn btn-primary", type: "button", onclick: () => start(topicInput.value, true) }, "Tạo quiz theo chủ đề"),
            h("button", { class: "btn btn-ghost", type: "button", onclick: () => start("", false) }, "Quiz ngẫu nhiên"))),
        runSlot);
    }
    showSetup();
  }

  global.VocabQuiz = {
    generateQuiz,
    vocabularyBank,
    mount: (slot) => mountVocabularyQuiz(slot),
  };
})(window);
