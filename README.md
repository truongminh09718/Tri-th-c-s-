# Tri Thức Số — Nền tảng học tập cá nhân hóa ứng dụng AI

> Dự thi **IT Solution Challenge 2026** — Khoa CNTT, Trường ĐH Nguyễn Tất Thành.
> Lĩnh vực: **Giáo dục thông minh / AI**.

Tri Thức Số là một nền tảng web đóng vai trò **cố vấn học tập số**: đánh giá năng lực
đầu vào, xây dựng hồ sơ học tập cá nhân (Learning DNA), sinh lộ trình học, tổ chức bài học
hằng ngày, theo dõi tiến độ, thích ứng lộ trình và dự đoán khả năng đạt mục tiêu.

## Kiến trúc tổng thể

```
Trình duyệt (SPA: HTML + CSS + JavaScript thuần, biểu đồ SVG)
        │  HTTPS + JWT
        ▼
Backend — ASP.NET Core 8 Web API  (Clean Architecture 4 tầng)
   Api ─ Application ─ Domain ─ Infrastructure
        │                 │                  │
        ▼                 ▼                  ▼
   EF Core +        Gemini API         ML Service (FastAPI)
   SQLite/SQLServer (sinh nội dung)    (dự đoán Academic Twin)
```

### Tầng kỹ thuật (đúng theo mã nguồn)

| Lớp | Công nghệ thực tế |
|-----|-------------------|
| Frontend | HTML5 + CSS3 thuần + JavaScript (vanilla), biểu đồ vẽ bằng **SVG**, light/dark theme, responsive |
| Backend API | ASP.NET Core 8 (C#), kiến trúc phân tầng Domain/Application/Infrastructure/Api |
| Cơ sở dữ liệu | Entity Framework Core — **SQLite** (môi trường demo), **SQL Server** (cấu hình production) |
| Sinh nội dung AI | Gemini API qua lớp **AI Gateway** (cache + timeout + log + fallback) |
| Dự đoán ML | Python + FastAPI + scikit-learn (`AiLearningPath/ml-service/`) |
| Xác thực | JWT Bearer + băm mật khẩu **PBKDF2-HMAC-SHA256** (100.000 vòng) |
| Phân quyền | Middleware kiểm soát theo chủ sở hữu dữ liệu (ownership-based) |
| Kiểm thử | xUnit + property-based test (.NET) và test frontend |

## Cấu trúc thư mục

```
AiLearningPath/
  src/
    Api/             # Controllers, Program.cs, wwwroot (frontend), cấu hình
    Application/     # Interface, DTO, ngân hàng câu hỏi, điều phối nghiệp vụ
    Domain/          # Logic thuần: chấm điểm, Learning Score, validation, dựng lộ trình
    Infrastructure/  # EF Core, AI Gateway/Gemini, ML adapter, Auth
  ml-service/        # Dịch vụ ML CHÍNH THỨC (FastAPI) — Academic Twin
  tests/             # Test .NET + frontend
legacy/
  AiPrediction/      # Nguyên mẫu Flask + RandomForest (KHÔNG dùng trong sản phẩm chính)
```

## Chạy thử

### Backend + Frontend
```bash
cd AiLearningPath/src/Api
dotnet run
# Mở http://localhost:5000  (frontend được phục vụ tĩnh từ wwwroot)
```

### ML Service (tùy chọn — đã có fallback nếu không chạy)
```bash
cd AiLearningPath/ml-service
pip install -r requirements.txt
uvicorn app:app --port 8000
```

> Hệ thống có **fallback xác định** cho cả Gemini và ML Service, nên demo vẫn chạy ổn định
> khi không có mạng hoặc khi dịch vụ ngoài không khả dụng.

## Tài liệu liên quan

- `AiLearningPath/CODE_MAP_FOR_REPORT.md` — bản đồ mã nguồn theo từng chức năng.
- `AiLearningPath/DEMO_VIDEO_SCRIPT.md` — kịch bản video demo.
- `legacy/README.md` — giải thích thành phần nguyên mẫu.
