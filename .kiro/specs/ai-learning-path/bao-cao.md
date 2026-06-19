# Báo cáo Dự án — AI Learning Path

> Tài liệu này tổng hợp các phần báo cáo của dự án **AI Learning Path** (nền tảng cố vấn học tập số ứng dụng AI). Các phần kỹ thuật chi tiết (yêu cầu, thiết kế, kế hoạch triển khai) nằm trong `requirements.md`, `design.md` và `tasks.md`. Báo cáo này bổ sung các phần còn thiếu: Mục tiêu nghiên cứu, Kiến trúc hệ thống (bản tóm tắt báo cáo), Phân chia công việc nhóm và Hướng phát triển.

---

## 1. Mục tiêu nghiên cứu

### 1.1 Mục tiêu tổng quát

Xây dựng một nền tảng web ứng dụng Trí tuệ nhân tạo (AI) đóng vai trò **cố vấn học tập số (AI Learning Mentor)**, giúp sinh viên cá nhân hóa lộ trình học tập, đánh giá năng lực, theo dõi tiến độ và định hướng nghề nghiệp dựa trên dữ liệu học tập của chính họ.

### 1.2 Mục tiêu cụ thể

1. **Cá nhân hóa lộ trình học tập**: Tự động sinh lộ trình theo tháng/tuần/ngày bám sát mục tiêu, trình độ và đặc điểm học tập (Learning DNA) của từng sinh viên.
2. **Đánh giá năng lực đầu vào**: Xây dựng cơ chế AI Assessment tạo và chấm bài kiểm tra, xác định trình độ hiện tại cùng điểm mạnh/điểm yếu.
3. **Mô hình hóa hồ sơ học tập cá nhân (Learning DNA)**: Phân tích phong cách học, khung giờ hiệu quả, tốc độ tiếp thu và khả năng tập trung để làm cơ sở cá nhân hóa.
4. **Dự đoán kết quả học tập (AI Academic Twin)**: Mô phỏng xác suất đạt mục tiêu theo từng mức thời lượng học mỗi ngày, hỗ trợ sinh viên ra quyết định đầu tư thời gian hợp lý.
5. **Theo dõi tiến độ trực quan**: Cung cấp dashboard hiển thị Learning Score, tỷ lệ hoàn thành, tổng giờ học và biểu đồ phân tích.
6. **Định hướng nghề nghiệp (Career Path AI)**: Sinh lộ trình kỹ năng, đề xuất chứng chỉ và dự án thực tế theo nghề nghiệp mục tiêu.
7. **Bảo đảm an toàn và riêng tư dữ liệu**: Áp dụng xác thực JWT và phân quyền theo chủ sở hữu dữ liệu (ownership-based authorization).

### 1.3 Phạm vi nghiên cứu

- **Trong phạm vi (MVP)**: 7 nhóm chức năng cốt lõi — Authentication & Profile, AI Assessment, Learning DNA Engine, AI Learning Path Generator, Progress Tracking Dashboard, AI Academic Twin và Career Path AI.
- **Ngoài phạm vi (định hướng mở rộng)**: Smart Study Scheduler (lập lịch học thông minh), Adaptive Learning System (điều chỉnh lộ trình tự động), AI Tutor (trợ lý hỏi đáp). Các chức năng này đã được phản ánh trong kiến trúc để đảm bảo khả năng mở rộng nhưng chưa triển khai ở giai đoạn MVP.

### 1.4 Ý nghĩa thực tiễn

- Giúp sinh viên chủ động hơn trong việc học, biết rõ mình đang ở đâu và cần làm gì để đạt mục tiêu.
- Giảm tình trạng học dàn trải, thiếu định hướng nhờ lộ trình cá nhân hóa và dự đoán dựa trên dữ liệu.
- Là nền tảng có thể mở rộng cho các trường, trung tâm đào tạo trong việc tư vấn và đồng hành cùng người học.

---

## 2. Kiến trúc hệ thống

### 2.1 Tổng quan

Hệ thống áp dụng **kiến trúc phân lớp (layered architecture)** kết hợp một **microservice ML riêng biệt**. Backend ASP.NET Core đóng vai trò API Gateway và chứa logic nghiệp vụ; các tác vụ sinh nội dung gọi **Gemini API**; các tác vụ dự đoán xác suất gọi **ML Service (Python + Scikit-learn)**.

```
┌──────────────────────────────┐
│   Client (ReactJS SPA)       │
│   TailwindCSS + Chart.js     │
└──────────────┬───────────────┘
               │ HTTPS + JWT
┌──────────────▼───────────────────────────────┐
│      Backend — ASP.NET Core Web API           │
│  ┌─────────────────────────────────────────┐ │
│  │  Authorization Middleware (JWT + owner)  │ │
│  └─────────────────────────────────────────┘ │
│  Auth · Profile · Assessment · Learning DNA   │
│  Path Generator · Dashboard · Twin · Career   │
└───┬───────────────┬───────────────┬───────────┘
    │               │               │
    ▼               ▼               ▼
┌────────┐   ┌────────────┐   ┌───────────────┐
│SQL     │   │ Gemini API │   │  ML Service   │
│Server  │   │ (nội dung) │   │ (dự đoán)     │
└────────┘   └────────────┘   └───────────────┘
```

### 2.2 Phân lớp backend

Mỗi service backend được tách thành 3 lớp với trách nhiệm rõ ràng:

| Lớp | Vai trò | Thư mục mã nguồn |
|-----|---------|------------------|
| **Controller (API)** | Nhận request, xác thực JWT, ủy quyền cho service | `src/Api/Controllers` |
| **Application Service** | Điều phối nghiệp vụ, gọi domain logic + repository, tích hợp dịch vụ ngoài qua interface | `src/Application` |
| **Domain Logic (pure functions)** | Chấm điểm, tính Learning Score, validation, ước lượng khả thi, dựng lộ trình — không phụ thuộc I/O | `src/Domain` |
| **Infrastructure** | EF Core (persistence), tích hợp Gemini/ML, JWT | `src/Infrastructure` |

Nguyên tắc cốt lõi: **tách logic nghiệp vụ thuần khỏi I/O**. Dịch vụ ngoài (Gemini, ML) được trừu tượng hóa qua interface `IContentGenerator` và `IPredictionService`, cho phép mock trong kiểm thử và dễ thay thế.

### 2.3 Các thành phần chính

- **Auth Service** — Đăng ký, đăng nhập, sinh/xác minh JWT; băm mật khẩu (bcrypt/PBKDF2).
- **Profile Service** — Quản lý hồ sơ và lựa chọn Learning Goal; validation số giờ học và mục tiêu.
- **Assessment Engine** — Sinh bộ câu hỏi (qua Gemini), chấm bài, xác định điểm mạnh/yếu.
- **Learning DNA Engine** — Xây dựng và cập nhật hồ sơ học tập cá nhân.
- **Path Generator** — Sinh lộ trình phân cấp tháng → tuần → ngày, kèm đánh giá tính khả thi.
- **Progress Dashboard Service** — Tổng hợp Learning Score, tỷ lệ hoàn thành, giờ học, dữ liệu biểu đồ.
- **Academic Twin Service** — Mô phỏng xác suất đạt mục tiêu theo thời lượng học (qua ML Service).
- **Career Path AI Service** — Sinh lộ trình kỹ năng, đề xuất chứng chỉ/dự án.
- **Authorization Middleware** — Xác minh JWT (401) và phân quyền theo chủ sở hữu dữ liệu (403).

### 2.4 Công nghệ sử dụng

| Lớp | Công nghệ |
|-----|-----------|
| Frontend | ReactJS, TailwindCSS, Chart.js |
| Backend API | ASP.NET Core Web API (C#) |
| Database | SQL Server (Entity Framework Core) |
| AI sinh nội dung | Gemini API |
| ML dự đoán | Python + Scikit-learn (microservice) |
| Xác thực | JWT (Bearer token) |
| Kiểm thử | xUnit + FsCheck (property-based testing) |
| Triển khai | Docker, Azure (App Service + Azure SQL) |

### 2.5 Bảo mật

- Mật khẩu lưu dưới dạng hash (không lưu văn bản thuần).
- Mọi tài nguyên dữ liệu học tập gắn `UserId`; truy cập được kiểm soát theo chủ sở hữu.
- JWT không hợp lệ/hết hạn → trả 401; truy cập dữ liệu của người khác → trả 403.

> Chi tiết sơ đồ kiến trúc (Mermaid), luồng nghiệp vụ và mô hình dữ liệu xem trong `design.md`.

---

## 3. Phân chia công việc nhóm

> Phần này phân chia công việc theo các hạng mục lớn của dự án. Tên thành viên là placeholder — nhóm điền lại theo thực tế.

### 3.1 Bảng phân công theo hạng mục

| STT | Hạng mục công việc | Mô tả | Phụ trách chính | Hỗ trợ |
|-----|--------------------|-------|------------------|--------|
| 1 | Thiết kế kiến trúc tổng thể | Xác định kiến trúc phân lớp, microservice ML, lựa chọn công nghệ | Thành viên A (Lead) | Cả nhóm |
| 2 | Thiết kế Database | Thiết kế ERD, entity, ràng buộc, migration EF Core | Thành viên B | Thành viên A |
| 3 | Backend — Auth & Profile | Đăng ký/đăng nhập, JWT, hồ sơ, Learning Goal, middleware phân quyền | Thành viên A | Thành viên C |
| 4 | Backend — Assessment & Learning DNA | Sinh/chấm bài đánh giá, xây dựng Learning DNA Profile | Thành viên C | Thành viên B |
| 5 | Backend — Path Generator & Dashboard | Sinh lộ trình cá nhân hóa, tính Learning Score, dashboard | Thành viên B | Thành viên C |
| 6 | Backend — Academic Twin & Career Path | Mô phỏng dự đoán (ML), lộ trình nghề nghiệp | Thành viên C | Thành viên A |
| 7 | Tích hợp dịch vụ AI/ML | Tích hợp Gemini API và ML Service qua interface | Thành viên D | Thành viên C |
| 8 | Frontend | Giao diện React, biểu đồ Chart.js, tích hợp API + JWT | Thành viên D | Cả nhóm |
| 9 | Tích hợp các module | Đăng ký DI, cấu hình pipeline, đảm bảo luồng end-to-end | Thành viên A | Cả nhóm |
| 10 | Kiểm thử cuối cùng | Property test (FsCheck), unit test, integration test | Cả nhóm | — |
| 11 | Viết báo cáo & tài liệu | Mục tiêu, kiến trúc, hướng phát triển, hướng dẫn sử dụng | Cả nhóm | — |

### 3.2 Phân chia theo giai đoạn (gợi ý)

1. **Giai đoạn 1 — Nền tảng**: Khởi tạo solution, thiết kế DB, Auth & Authorization.
2. **Giai đoạn 2 — Hồ sơ & đánh giá**: Profile, Learning Goal, Assessment, Learning DNA.
3. **Giai đoạn 3 — Lộ trình & theo dõi**: Path Generator, Dashboard.
4. **Giai đoạn 4 — Dự đoán & nghề nghiệp**: Academic Twin, Career Path.
5. **Giai đoạn 5 — Tích hợp & kiểm thử**: Wiring toàn hệ thống, frontend, test end-to-end, viết báo cáo.

> Đồ thị phụ thuộc nhiệm vụ chi tiết (waves) đã được mô tả trong `tasks.md` để hỗ trợ làm việc song song.

---

## 4. Hướng phát triển

### 4.1 Hoàn thiện các chức năng mở rộng (đã có trong kiến trúc)

- **Smart Study Scheduler**: Lập lịch học tự động quanh lịch đại học, lịch thi và deadline; tự động sắp xếp lại khi sinh viên bỏ lỡ nhiệm vụ.
- **Adaptive Learning System**: Tự điều chỉnh lộ trình dựa trên tiến độ thực tế — tăng bài tập cho kỹ năng yếu, giảm khối lượng cho kỹ năng đã đạt.
- **AI Tutor**: Trợ lý hỏi đáp học tập theo thời gian thực, gợi ý tài liệu theo chủ đề.

### 4.2 Nâng cao chất lượng mô hình AI/ML

- Thay thế mô hình dự đoán heuristic bằng mô hình ML huấn luyện trên dữ liệu học tập thực tế của người dùng.
- Cải thiện chất lượng sinh nội dung (câu hỏi, lộ trình) bằng prompt engineering và fine-tuning.
- Bổ sung cơ chế phản hồi (feedback loop) để mô hình học liên tục từ kết quả thực tế.

### 4.3 Mở rộng nền tảng

- **Đa nền tảng**: Phát triển ứng dụng mobile (React Native/Flutter).
- **Đa ngôn ngữ**: Hỗ trợ giao diện và nội dung đa ngôn ngữ.
- **Tích hợp LMS**: Kết nối với các hệ thống quản lý học tập của trường (Moodle, Google Classroom).
- **Gamification**: Huy hiệu, bảng xếp hạng, chuỗi ngày học để tăng động lực.
- **Cộng đồng học tập**: Nhóm học, chia sẻ lộ trình, học cùng nhau.

### 4.4 Vận hành và mở rộng quy mô

- Bổ sung caching (Redis), tối ưu truy vấn và đánh chỉ mục cơ sở dữ liệu.
- Áp dụng circuit breaker, retry, rate limiting cho dịch vụ ngoài.
- Giám sát (logging, monitoring, alerting) và CI/CD tự động.
- Triển khai container hóa và mở rộng theo chiều ngang trên Azure.

### 4.5 Tăng cường bảo mật và tuân thủ

- Refresh token và cơ chế thu hồi token.
- Mã hóa dữ liệu nhạy cảm, kiểm thử bảo mật định kỳ.
- Tuân thủ quy định bảo vệ dữ liệu cá nhân của người học.

---

## 5. Tổng kết tình trạng dự án

| Hạng mục | Trạng thái |
|----------|-----------|
| Thiết kế kiến trúc tổng thể | ✅ Hoàn thành (`design.md`) |
| Thiết kế Database | ✅ Hoàn thành (ERD + entity + migration) |
| Xây dựng Backend chính | ✅ Hoàn thành (`src/`) |
| Tích hợp các module | ✅ Hoàn thành (task 15) |
| Kiểm thử cuối cùng | ✅ Hoàn thành (task 16) |
| Phân chia công việc nhóm | ✅ Bổ sung trong báo cáo này |
| Viết: Kiến trúc hệ thống | ✅ Bổ sung trong báo cáo này |
| Viết: Mục tiêu nghiên cứu | ✅ Bổ sung trong báo cáo này |
| Viết: Hướng phát triển | ✅ Bổ sung trong báo cáo này |
