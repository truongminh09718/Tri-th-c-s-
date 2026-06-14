---
title: Taste Skill — Anti-Slop Frontend Design
inclusion: fileMatch
fileMatchPattern: ["**/*.html", "**/*.css", "**/*.tsx", "**/*.jsx", "**/*.vue", "**/wwwroot/**", "**/*.svelte"]
source: https://github.com/Leonxlnx/taste-skill
---

# Taste Skill (design-taste-frontend) — Kiro Steering

Khi tạo bất kỳ giao diện frontend nào (landing page, portfolio, redesign, hoặc app UI), ÁP DỤNG bộ quy tắc dưới đây. Đây là bản tích hợp skill `design-taste-frontend` từ repo Leonxlnx/taste-skill.

## 0. Đọc brief trước khi code
Trước khi viết code, suy ra: loại trang, vibe, audience, brand assets có sẵn, ràng buộc ngầm. Phát biểu một câu "Design Read": *"Reading this as: <page kind> for <audience>, with a <vibe> language, leaning toward <design system/aesthetic>."* Nếu brief mơ hồ và hai hướng khác hẳn nhau, hỏi đúng MỘT câu; nếu suy được thì cứ làm.

Anti-default: KHÔNG mặc định AI-purple gradient, hero căn giữa trên dark mesh, 3 feature card bằng nhau, glassmorphism khắp nơi, micro-animation lặp vô hạn, Inter + slate-900.

## 1. Ba dial (cấu hình lõi)
- `DESIGN_VARIANCE` (1 đối xứng → 10 hỗn loạn nghệ thuật)
- `MOTION_INTENSITY` (1 tĩnh → 10 cinematic/physics)
- `VISUAL_DENSITY` (1 thoáng → 10 dày data)
Baseline 8/6/4. Suy dial từ brief: minimalist/clean → 5-6/3-4/2-3; premium consumer → 7-8/5-7/3-4; playful/agency → 9-10/8-10/3-4; trust-first/public-sector → 3-4/2-3/4-5.

## 2. Brief → Design System
Nếu brief khớp một hệ chính thống, cài và dùng package CHÍNH THỐNG (Fluent, Material 3, Carbon, Polaris, Atlaskit, Primer, GOV.UK, USWDS, Bootstrap, Radix Themes, shadcn/ui, Tailwind v4). Không tự viết lại CSS của hệ đó. MỘT hệ thiết kế cho mỗi project. Nếu brief là một "aesthetic" (glass, bento, brutalism, editorial, dark tech, aurora, kinetic type) thì dựng bằng CSS thuần + Tailwind, ghi chú trung thực phần nào là approximation.

## 3. Default stack & conventions
- React/Next (RSC mặc định); motion isolate trong client leaf `'use client'`.
- Tailwind v4 (hoặc CSS thuần khi project static). Fonts self-host / `next/font`, không `<link>` Google Fonts trong production.
- Animation: Motion (`motion/react`). Scroll-heavy: GSAP ScrollTrigger trong leaf riêng, cleanup chặt.
- Icons: Phosphor / HugeIcons / Radix / Tabler. KHÔNG tự vẽ SVG icon. MỘT family/project. Lucide chỉ khi được yêu cầu.
- Emoji: hạn chế trong code/markup/text trừ khi brief playful.
- Responsive: breakpoints chuẩn (sm640/md768/lg1024/xl1280/2xl1536), `max-w-[1400px] mx-auto`, hero dùng `min-h-[100dvh]` (KHÔNG `h-screen`), dùng CSS Grid thay vì flex %-math.
- Kiểm tra `package.json` trước khi import bất kỳ lib nào.

## 4. Design engineering directives
- **Typography:** display `text-4xl md:text-6xl tracking-tighter leading-none`; body `text-base leading-relaxed max-w-[65ch]`. Tránh Inter làm mặc định (ưu tiên Geist/Outfit/Cabinet Grotesk/Satoshi). SERIF rất hạn chế làm mặc định; CẤM mặc định Fraunces và Instrument_Serif. Emphasis trong headline: dùng italic/bold CÙNG font, không chèn serif lạ.
- **Italic descender clearance:** từ italic có `y g j p q` cần `leading-[1.1]` min + `pb-1`.
- **Color:** tối đa 1 accent, saturation < 80%. THE LILA RULE: tránh AI purple/blue glow mặc định; dùng nền neutral (Zinc/Slate/Stone) + 1 accent tương phản cao. COLOR CONSISTENCY LOCK: một accent dùng cho cả trang. Cấm palette beige+brass+oxblood+espresso làm mặc định cho premium-consumer.
- **Layout:** ANTI-CENTER khi VARIANCE>4 (split, asymmetric, scroll-pinned). Card chỉ khi elevation có nghĩa. SHAPE CONSISTENCY LOCK: một thang bo góc cho cả trang. Shadow tint theo nền, không đen thuần.
- **Interactive states:** luôn có loading (skeleton), empty, error; tactile `:active` (`-translate-y-[1px]`/`scale-[0.98]`). BUTTON CONTRAST CHECK (WCAG AA 4.5:1), không CTA chữ trắng nền trắng. CTA không wrap 2 dòng ở desktop. Không 2 CTA cùng intent. FORM CONTRAST CHECK đầy đủ.
- **Form:** label TRÊN input, error DƯỚI input, không placeholder-as-label.
- **Hard layout rules:** Hero lọt viewport (headline ≤2 dòng, subtext ≤20 từ/≤4 dòng, CTA thấy không cần scroll, `pt-24` max). Hero tối đa 4 text element. Nav 1 dòng desktop, cao ≤80px. Bento có nhịp + đúng số cell. Cấm lặp layout family (≥4 family cho 8 section). Zigzag image+text tối đa 2 liên tiếp. EYEBROW tối đa 1 / 3 section. Cấm split-header (headline trái + đoạn nhỏ phải).
- **Images:** trang landing/portfolio là sản phẩm thị giác — ưu tiên image-gen tool, rồi `picsum.photos/seed/...`, cuối cùng để slot TODO. CẤM fake screenshot bằng div, CẤM hand-rolled decorative SVG. Logo wall dùng Simple Icons/devicon, chỉ logo, không label ngành.
- **Content density:** headline ≤8 từ, sub ≤25 từ/section. Không bảng 20 dòng. List >5 item dùng UI khác (`<ul> divide-y` là lười). Copy self-audit mọi string. Không số fake-precise. Quote ≤3 dòng, attribution đủ tên+vai trò.
- **Theme lock:** một theme cho cả trang (light/dark/auto), section không tự đảo màu.

## 5–8. Motion, performance, dark mode
- Motion phải có lý do (hierarchy/storytelling/feedback/state). "Motion claimed = motion shown". Marquee tối đa 1/trang. GSAP sticky-stack/horizontal-pan dùng `start:"top top"`, `pin:true`, scrub đúng.
- CẤM `window.addEventListener('scroll')` — dùng `useScroll()`/ScrollTrigger/IntersectionObserver/CSS scroll-driven.
- Animate chỉ `transform`/`opacity`. `prefers-reduced-motion` bắt buộc khi MOTION>3. Cleanup `useEffect`.
- Dark mode dual từ đầu; không `#000`/`#fff` thuần; WCAG AA body, AAA hero.
- Core Web Vitals: LCP<2.5s, INP<200ms, CLS<0.1. Grain/noise chỉ trên pseudo-element fixed `pointer-events-none`. Z-index có hệ thống.

## 9. AI Tells bị cấm (trừ khi brief yêu cầu)
Neon/outer glow mặc định; `#000000`; accent quá bão hòa; gradient text cho header lớn; custom cursor; Inter mặc định; H1 quá khổ; 3 card bằng nhau; tên "John Doe"/avatar egg; số fake-perfect (99.99%); brand slop (Acme/Nexus/SmartFlow); filler verbs (Elevate/Seamless/Unleash/Next-Gen); hand-rolled SVG icon; div fake screenshot; broken Unsplash; shadcn default state; version label trong hero (V0.6/BETA); section-number eyebrow (00/INDEX, 001·); middle-dot `·` lạm dụng; status dot trang trí; em-dash; `<br>`-broken italic headline; vertical rotated text; crosshair lines; "Quietly in use at"; "Field notes"/"From the field"; weather/locale strip; micro-meta-sentence dưới eyebrow; pill overlay trên ảnh; photo-credit trang trí; version footer; scroll cue ("Scroll to explore"); `border-t`+`border-b` mọi row; progress bar có track nền làm so sánh; decoration text strip cuối hero (BRAND. MOTION. SPATIAL.); generic step label (Stage 1/Step 1/Phase 01).

### 9.G EM-DASH BAN (tuyệt đối)
KHÔNG dùng `—` (em-dash) HAY `–` (en-dash làm separator) ở BẤT KỲ ĐÂU hiển thị: headline, eyebrow, pill, body, quote, attribution, caption, button, alt text. Chỉ dùng hyphen thường `-`. Vi phạm = fail pre-flight.

## 14. Pre-flight check (chạy trước khi giao)
Bắt buộc tick từng mục: design read đã phát biểu; dial có lý do; design system/aesthetic trung thực; ZERO em-dash; theme lock; color/shape consistency lock; button & form contrast (WCAG AA); CTA không wrap & không trùng intent; serif discipline; hero lọt viewport + ≤4 text element + `pt-24`; eyebrow ≤ ceil(section/3); không split-header; zigzag ≤2; logo wall chỉ logo & dưới hero; bento đa dạng nền & đúng cell; copy self-audit; motion có lý do & reduced-motion; nav 1 dòng ≤80px; ≥4 layout family; ảnh thật (không div fake/SVG tự vẽ); không các AI Tell ở Section 9; CWV đạt; một design system. Nếu một ô không tick được trung thực → CHƯA xong, sửa trước khi giao.

## Lưu ý phạm vi
Skill tối ưu cho landing/portfolio/redesign. Với product UI (dashboard, multi-step, data table) áp dụng tinh thần taste + ưu tiên design system phù hợp (Section 2), không ép aesthetic landing-page lên dashboard.
