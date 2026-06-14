# Implementation Plan: AI Learning Path (Kế hoạch Triển khai)

## Overview

Kế hoạch này chuyển thiết kế kỹ thuật của AI Learning Path thành chuỗi nhiệm vụ lập trình tăng tiến cho backend **ASP.NET Core Web API (C#)** với **Entity Framework Core + SQL Server**, kiểm thử bằng **xUnit** và **property-based testing với FsCheck**. Mỗi nhiệm vụ xây dựng dựa trên nhiệm vụ trước và kết thúc bằng việc kết nối các thành phần lại với nhau, không để lại mã mồ côi.

Phạm vi MVP gồm 7 nhóm chức năng: Authentication & Profile, AI Assessment, Learning DNA Engine, AI Learning Path Generator, Progress Dashboard, AI Academic Twin và Career Path AI. Logic nghiệp vụ thuần (validation, chấm điểm, tính toán, ước lượng khả thi, phân quyền, serialization) được tách khỏi I/O để áp dụng PBT. Dịch vụ ngoài (Gemini, ML Service) được trừu tượng hóa qua interface và mock trong kiểm thử.

Quy ước PBT: mỗi property test chạy tối thiểu 100 iteration và gắn comment tham chiếu theo định dạng
`// Feature: ai-learning-path, Property {số}: {nội dung property}`.

## Tasks

- [x] 1. Khởi tạo cấu trúc dự án và nền tảng chung
  - [x] 1.1 Tạo solution và project backend
    - Tạo solution ASP.NET Core Web API (C#), cấu trúc thư mục theo lớp: `Api` (Controllers), `Application` (Services), `Domain` (pure functions + models), `Infrastructure` (EF Core, dịch vụ ngoài)
    - Tạo project test xUnit và thêm gói FsCheck (FsCheck.Xunit) cho property-based testing
    - Cấu hình `appsettings.json` (chuỗi kết nối SQL Server, cấu hình JWT), bật DI container
    - _Requirements: 1.1, 2.1_

  - [x] 1.2 Định nghĩa kiểu kết quả dùng chung và interface dịch vụ ngoài
    - Định nghĩa `ValidationResult` (IsValid + thông báo lỗi), kiểu lỗi API nhất quán `{ error: { code, message, details } }`
    - Định nghĩa interface `IContentGenerator` (bọc Gemini) và `IPredictionService` (bọc ML Service) để mock trong kiểm thử
    - _Requirements: 5.1, 7.1, 9.1_

- [x] 2. Định nghĩa data models và lớp persistence (EF Core)
  - [x] 2.1 Tạo các entity miền và DbContext
    - Hiện thực entity: `User`, `Profile`, `Assessment`, `AssessmentResult`, `LearningDnaProfile`, `LearningPath`, `PathPhase`, `LearningTask`, `CareerPath`, `StudySession`, `ProgressSnapshot` theo sơ đồ ERD
    - Cấu hình `AppDbContext` với EF Core, ràng buộc `Email` duy nhất, mọi entity dữ liệu học tập có `UserId`
    - Tạo migration ban đầu cho SQL Server
    - _Requirements: 1.1, 3.1, 5.3, 6.2, 7.3, 10.4, 14.1_

  - [x] 2.2 Hiện thực helper serialize/deserialize JSON cho trường danh sách
    - Viết helper serialize các trường phức tạp (QuestionsJson, StrengthsJson, WeaknessesJson, EffectiveHoursJson, SkillsJson, CertificationsJson, ProjectsJson) sang `nvarchar(max)`
    - _Requirements: 5.3, 6.2, 7.3, 10.4_

  - [x] 2.3 Viết property test round-trip serialization JSON
    - **Property 20: Round-trip serialization dữ liệu JSON**
    - **Validates: Requirements 5.3, 6.2, 7.3, 10.4**

- [x] 3. Hiện thực Auth Service (R1, R2)
  - [x] 3.1 Hiện thực logic thuần PasswordPolicy và PasswordHasher
    - Viết `PasswordPolicy.Validate` (mật khẩu >= 8 ký tự)
    - Viết `PasswordHasher.Hash` / `Verify` dùng bcrypt hoặc PBKDF2 với salt
    - _Requirements: 1.3, 1.4_

  - [x] 3.2 Viết property test cho chính sách mật khẩu
    - **Property 1: Mật khẩu ngắn luôn bị từ chối**
    - **Validates: Requirements 1.3**

  - [x] 3.3 Viết property test cho băm mật khẩu round-trip
    - **Property 2: Băm mật khẩu round-trip**
    - **Validates: Requirements 1.4**

  - [x] 3.4 Hiện thực IAuthService (đăng ký, đăng nhập, JWT)
    - Viết `RegisterAsync` kiểm tra trùng email + băm mật khẩu + lưu DB
    - Viết `LoginAsync` xác minh mật khẩu và sinh JWT; `ValidateToken` xác minh JWT
    - Viết `AuthController` với endpoint `/register` và `/login`
    - _Requirements: 1.1, 1.2, 2.1, 2.2_

  - [x] 3.5 Viết property test cho đăng ký email trùng
    - **Property 3: Đăng ký email trùng bị từ chối**
    - **Validates: Requirements 1.1, 1.2**

  - [x] 3.6 Viết unit test cho luồng đăng ký/đăng nhập
    - Test tạo tài khoản thành công (R1.1), trả JWT khi đăng nhập đúng (R2.1), từ chối khi sai thông tin (R2.2)
    - _Requirements: 1.1, 2.1, 2.2_

- [x] 4. Hiện thực Authorization Middleware (R2, R14)
  - [x] 4.1 Hiện thực IResourceAuthorizer và middleware xác thực
    - Viết `IResourceAuthorizer.CanAccess` (ownership-based: chỉ true khi requesterId == resourceOwnerId)
    - Viết middleware xác minh JWT: không hợp lệ/hết hạn → 401, truy cập tài nguyên người khác → 403
    - Gắn middleware vào pipeline và bảo vệ các route cần xác thực
    - _Requirements: 2.3, 2.4, 14.1, 14.2, 14.3_

  - [x] 4.2 Viết property test cho phân quyền theo chủ sở hữu
    - **Property 19: Phân quyền theo chủ sở hữu dữ liệu**
    - **Validates: Requirements 14.1, 14.2**

  - [x] 4.3 Viết integration test cho middleware JWT
    - Test pipeline thực trả 401 với JWT không hợp lệ/hết hạn và 403 khi truy cập dữ liệu tài khoản khác
    - _Requirements: 2.4, 14.2, 14.3_

- [x] 5. Checkpoint — Nền tảng Auth & Authorization
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Hiện thực Profile Service và Learning Goal (R3, R4)
  - [x] 6.1 Hiện thực logic thuần ProfileValidator và LearningGoalCatalog
    - Viết `ProfileValidator.ValidateStudyHours` (chỉ hợp lệ khi 0 <= h <= 24)
    - Viết `LearningGoalCatalog.Supported` + `RequiresTargetScore` và `ProfileValidator.ValidateGoal`
    - _Requirements: 3.4, 4.1, 4.4_

  - [x] 6.2 Viết property test cho validation số giờ học
    - **Property 4: Validation số giờ học mỗi ngày**
    - **Validates: Requirements 3.4**

  - [x] 6.3 Viết property test cho validation Learning Goal
    - **Property 5: Chỉ chấp nhận Learning Goal được hỗ trợ**
    - **Validates: Requirements 4.1, 4.2, 4.4**

  - [x] 6.4 Hiện thực IProfileService và endpoint hồ sơ
    - Viết `CreateOrUpdateAsync`, `GetAsync`, `UpdateFieldAsync` (áp dụng validation số giờ học), `SelectGoalAsync` (áp dụng validation goal + lưu target score khi cần)
    - Viết `ProfileController` với endpoint xem/cập nhật hồ sơ và chọn Learning Goal, áp dụng ownership-based authorization
    - _Requirements: 3.1, 3.2, 3.3, 4.2, 4.3_

  - [x] 6.5 Viết unit test cho CRUD hồ sơ và chọn goal
    - Test lưu/đọc/cập nhật hồ sơ (R3.1–3.3), lưu lựa chọn goal và nhập điểm mục tiêu cho goal yêu cầu điểm (R4.2–4.3)
    - _Requirements: 3.1, 3.2, 3.3, 4.2, 4.3_

- [x] 7. Hiện thực Assessment Engine (R5)
  - [x] 7.1 Hiện thực logic thuần AssessmentGrader
    - Viết `AssessmentGrader.Grade` (chấm điểm theo skill area, suy ra trình độ, điểm mạnh/yếu — tách biệt, xác định)
    - Viết `AssessmentGrader.ValidateCompleteness` (mọi câu hỏi phải được trả lời)
    - _Requirements: 5.2, 5.4_

  - [x] 7.2 Viết property test cho tính xác định khi chấm bài
    - **Property 6: Chấm bài đánh giá có tính xác định**
    - **Validates: Requirements 5.2**

  - [x] 7.3 Viết property test cho kiểm tra đầy đủ câu trả lời
    - **Property 7: Nộp bài thiếu câu trả lời bị từ chối**
    - **Validates: Requirements 5.4**

  - [x] 7.4 Viết property test cho điểm mạnh/điểm yếu tách biệt
    - **Property 8: Điểm mạnh và điểm yếu tách biệt**
    - **Validates: Requirements 5.2**

  - [x] 7.5 Hiện thực IAssessmentEngine và endpoint
    - Viết `StartAsync` gọi `IContentGenerator` sinh bộ câu hỏi theo Learning Goal và lưu Assessment
    - Viết `SubmitAsync` áp dụng `ValidateCompleteness` + `Grade`, lưu `AssessmentResult` liên kết hồ sơ
    - Viết `AssessmentController` với endpoint bắt đầu/nộp bài, áp dụng authorization
    - _Requirements: 5.1, 5.2, 5.3_

  - [x] 7.6 Viết integration test sinh câu hỏi qua Gemini (mock)
    - Test `StartAsync` sinh bộ câu hỏi theo Learning Goal với `IContentGenerator` được mock
    - _Requirements: 5.1_

- [x] 8. Hiện thực Learning DNA Engine (R6)
  - [x] 8.1 Hiện thực logic thuần DnaBuilder
    - Viết `DnaBuilder.Build` (tạo Learning DNA Profile từ AssessmentResult + ProfileInput)
    - Viết `DnaBuilder.Merge` (cập nhật DNA từ ProgressData mới)
    - _Requirements: 6.1, 6.3_

  - [x] 8.2 Hiện thực ILearningDnaEngine và endpoint
    - Viết `BuildAsync` (tạo và lưu DNA khi có AssessmentResult), `UpdateAsync`, `GetAsync`
    - Wire: sau khi `SubmitAsync` của Assessment hoàn tất, kích hoạt `BuildAsync` tạo DNA Profile
    - Viết endpoint xem Learning DNA Profile, áp dụng authorization
    - _Requirements: 6.1, 6.2, 6.3, 6.4_

  - [x] 8.3 Viết unit test cho build và cập nhật DNA
    - Test build DNA từ kết quả đánh giá và merge từ dữ liệu tiến độ mới
    - _Requirements: 6.1, 6.3_

- [x] 9. Checkpoint — Profile, Assessment & Learning DNA
  - Ensure all tests pass, ask the user if questions arise.

- [x] 10. Hiện thực Path Generator (R7)
  - [x] 10.1 Hiện thực logic thuần PathBuilder
    - Viết `CheckPrerequisites` (yêu cầu có AssessmentResult)
    - Viết `Build` (xây cấu trúc lộ trình tháng → tuần → ngày, bao phủ toàn bộ targetDays không trùng/hở)
    - Viết `AssessFeasibility` (gắn cảnh báo khi tổng giờ ước lượng vượt giờ khả dụng)
    - _Requirements: 7.1, 7.2, 7.4, 7.5_

  - [x] 10.2 Viết property test cho tiên quyết sinh lộ trình
    - **Property 9: Sinh lộ trình yêu cầu hoàn thành đánh giá**
    - **Validates: Requirements 7.4**

  - [x] 10.3 Viết property test cho bao phủ thời gian mục tiêu
    - **Property 10: Lộ trình bao phủ toàn bộ thời gian mục tiêu**
    - **Validates: Requirements 7.1, 7.2**

  - [x] 10.4 Viết property test cho cảnh báo tính khả thi
    - **Property 11: Cảnh báo khả thi khi thời gian không đủ**
    - **Validates: Requirements 7.5**

  - [x] 10.5 Hiện thực IPathGenerator và endpoint
    - Viết `GenerateAsync`: kiểm tra tiên quyết, gọi `IContentGenerator` sinh nội dung, `Build` + `AssessFeasibility`, lưu LearningPath (kèm feasibilityWarning)
    - Viết `PathController` với endpoint sinh lộ trình, áp dụng authorization
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

  - [x] 10.6 Viết integration test sinh nội dung lộ trình qua Gemini (mock)
    - Test `GenerateAsync` với `IContentGenerator` được mock
    - _Requirements: 7.1_

- [x] 11. Hiện thực Progress Dashboard Service (R8)
  - [x] 11.1 Hiện thực logic thuần ProgressCalculator
    - Viết `ComputeCompletionRate` (trả [0,1] = số task hoàn thành / tổng task), `ComputeLearningScore` (trả [0,100]), `ComputeTotalStudyHours` (tổng thời lượng, không âm), `BuildChartData`
    - _Requirements: 8.1, 8.2_

  - [x] 11.2 Viết property test cho tỷ lệ hoàn thành
    - **Property 12: Tỷ lệ hoàn thành luôn nằm trong [0, 1]**
    - **Validates: Requirements 8.1, 8.2**

  - [x] 11.3 Viết property test cho Learning Score
    - **Property 13: Learning Score luôn nằm trong [0, 100]**
    - **Validates: Requirements 8.1, 8.2**

  - [x] 11.4 Viết property test cho tổng số giờ học
    - **Property 14: Tổng số giờ học bằng tổng các phiên học**
    - **Validates: Requirements 8.1**

  - [x] 11.5 Hiện thực IProgressDashboardService và endpoint
    - Viết `GetDashboardAsync` (Learning Score, tỷ lệ hoàn thành, tổng giờ, dữ liệu biểu đồ; trạng thái rỗng khi chưa có dữ liệu)
    - Viết `CompleteTaskAsync` (đánh dấu task hoàn thành, cập nhật tỷ lệ + Learning Score)
    - Viết `DashboardController` với endpoint dashboard/hoàn thành task, áp dụng authorization
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

  - [x] 11.6 Viết property test cho hoàn thành task không làm giảm tỷ lệ
    - **Property 15: Hoàn thành nhiệm vụ làm tỷ lệ hoàn thành không giảm**
    - **Validates: Requirements 8.2**

  - [x] 11.7 Viết unit test cho dashboard trạng thái rỗng
    - Test hiển thị trạng thái rỗng kèm hướng dẫn khi chưa có dữ liệu học tập
    - _Requirements: 8.4_

- [x] 12. Checkpoint — Path Generator & Dashboard
  - Ensure all tests pass, ask the user if questions arise.

- [x] 13. Hiện thực Academic Twin Service (R9)
  - [x] 13.1 Hiện thực logic thuần TwinValidator
    - Viết `TwinValidator.CheckPrerequisites` (yêu cầu có cả Learning Goal lẫn AssessmentResult)
    - _Requirements: 9.4_

  - [x] 13.2 Viết property test cho tiên quyết mô phỏng Twin
    - **Property 16: Mô phỏng Academic Twin yêu cầu tiên quyết**
    - **Validates: Requirements 9.4**

  - [x] 13.3 Hiện thực IAcademicTwinService và endpoint
    - Viết `SimulateAsync` (gọi `IPredictionService`, kiểm tra tiên quyết, trả xác suất)
    - Viết `SimulateRangeAsync` (một dự đoán cho mỗi mức thời lượng, đảm bảo đơn điệu không giảm theo thời lượng)
    - Wire: tính lại dự đoán khi dữ liệu tiến độ cập nhật
    - Viết `TwinController` với endpoint mô phỏng, áp dụng authorization
    - _Requirements: 9.1, 9.2, 9.3, 9.4_

  - [x] 13.4 Viết property test cho giá trị xác suất hợp lệ
    - **Property 17: Xác suất đạt mục tiêu là một giá trị xác suất hợp lệ**
    - **Validates: Requirements 9.1, 9.2**

  - [x] 13.5 Viết property test cho mô phỏng nhiều mức thời lượng
    - **Property 18: Mô phỏng nhiều mức thời lượng đầy đủ và đơn điệu**
    - **Validates: Requirements 9.1, 9.2**

  - [x] 13.6 Viết integration test dự đoán qua ML Service (mock)
    - Test `SimulateAsync` end-to-end với `IPredictionService` được mock
    - _Requirements: 9.1_

- [x] 14. Hiện thực Career Path AI Service (R10)
  - [x] 14.1 Hiện thực CareerCatalog và ICareerPathService
    - Viết `CareerCatalog.Careers` (Frontend, Backend, DataAnalyst, AIEngineer, Tester) và `ListCareers`
    - Viết `GenerateAsync`: gọi `IContentGenerator` sinh lộ trình kỹ năng + đề xuất chứng chỉ/dự án, lưu CareerPath liên kết hồ sơ
    - Viết `CareerController` với endpoint danh sách nghề/sinh lộ trình, áp dụng authorization
    - _Requirements: 10.1, 10.2, 10.3, 10.4_

  - [x] 14.2 Viết unit test cho Career Path
    - Test danh mục nghề nghiệp (R10.1), sinh lộ trình kỹ năng kèm chứng chỉ/dự án (R10.2–10.3)
    - _Requirements: 10.1, 10.2, 10.3_

- [x] 15. Tích hợp và kết nối toàn hệ thống
  - [x] 15.1 Đăng ký DI và cấu hình pipeline cuối
    - Đăng ký toàn bộ service, repository, `IContentGenerator` (Gemini), `IPredictionService` (ML) trong DI container
    - Cấu hình middleware xác thực/authorization áp dụng nhất quán cho mọi controller dữ liệu học tập
    - Đảm bảo luồng end-to-end: đăng ký → profile → goal → assessment → DNA → path → dashboard → twin → career
    - _Requirements: 2.3, 14.1_

  - [x] 15.2 Viết integration test cho luồng end-to-end (mock dịch vụ ngoài)
    - Test luồng chính từ đăng nhập tới sinh lộ trình và xem dashboard với Gemini/ML được mock; lưu/đọc dữ liệu qua EF Core
    - _Requirements: 2.3, 5.1, 7.1, 9.1, 14.1_

- [x] 16. Checkpoint cuối — Toàn bộ test pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Các nhiệm vụ đánh dấu `*` là tùy chọn (unit/property/integration test) và có thể bỏ qua để dựng MVP nhanh, nhưng được khuyến nghị thực hiện để đảm bảo đúng đắn.
- Mỗi property test hiện thực đúng **một** correctness property (Property 1–20), chạy tối thiểu **100 iteration** với FsCheck, kèm comment tham chiếu: `// Feature: ai-learning-path, Property {số}: {nội dung}`.
- Dịch vụ ngoài (Gemini, ML Service) luôn được **mock** trong property/unit test để kiểm thử logic độc lập với I/O.
- Mỗi nhiệm vụ tham chiếu requirements cụ thể để đảm bảo truy vết; các checkpoint đảm bảo kiểm chứng tăng tiến.

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2", "2.1"] },
    { "id": 2, "tasks": ["2.2", "3.1", "4.1", "6.1"] },
    { "id": 3, "tasks": ["2.3", "3.2", "3.3", "4.2", "6.2", "6.3", "7.1"] },
    { "id": 4, "tasks": ["3.4", "4.3", "6.4", "7.2", "7.3", "7.4", "8.1"] },
    { "id": 5, "tasks": ["3.5", "3.6", "6.5", "7.5", "8.2", "10.1", "13.1"] },
    { "id": 6, "tasks": ["7.6", "8.3", "10.2", "10.3", "10.4", "11.1", "13.2"] },
    { "id": 7, "tasks": ["10.5", "11.2", "11.3", "11.4", "13.3", "14.1"] },
    { "id": 8, "tasks": ["10.6", "11.5", "13.4", "13.5", "14.2"] },
    { "id": 9, "tasks": ["11.6", "11.7", "13.6"] },
    { "id": 10, "tasks": ["15.1"] },
    { "id": 11, "tasks": ["15.2"] }
  ]
}
```
