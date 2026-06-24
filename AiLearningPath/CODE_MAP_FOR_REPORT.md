# Bản đồ mã nguồn phục vụ viết báo cáo

Tài liệu này chỉ ra vị trí triển khai từng chức năng theo luồng:

`Giao diện → API Controller → Application Contract → Infrastructure Service → Domain/Entity → Test`

## 1. Cấu trúc và điểm khởi động hệ thống

| Thành phần | Vị trí | Nội dung nên mô tả trong báo cáo |
|---|---|---|
| Điểm khởi động API | `src/Api/Program.cs` | Đăng ký Dependency Injection, EF Core, JWT, rate limiting, middleware, migration và static web |
| Cấu hình chung | `src/Api/appsettings.json` | JWT, SQL Server, Gemini, ML Service, AI cache/timeout |
| Cấu hình Development | `src/Api/appsettings.Development.json` | SQLite cục bộ và cấu hình môi trường demo |
| Database context | `src/Infrastructure/Persistence/AppDbContext.cs:11` | Khai báo DbSet, quan hệ và ràng buộc dữ liệu |
| Migration ban đầu | `src/Infrastructure/Persistence/Migrations/20260614180411_InitialCreate.cs` | Schema lõi của hệ thống |
| Migration nền tảng AI | `src/Infrastructure/Persistence/Migrations/20260619052832_AddAiPlatformLayer.cs` | Bảng cache, log AI, feedback, tutor, scheduler, adaptation |
| Giao diện SPA | `src/Api/wwwroot/app.html` | Khung HTML của màn hình xác thực và ứng dụng |
| Router frontend | `src/Api/wwwroot/js/app.js:207` | Danh sách menu và điều hướng các chức năng |

## 2. Đăng ký, đăng nhập và xác thực

**Mục đích:** tạo tài khoản, đăng nhập bằng JWT, bảo vệ API và dữ liệu của từng người dùng.

| Tầng | Vị trí |
|---|---|
| Giao diện đăng nhập/đăng ký | `src/Api/wwwroot/app.html:51` |
| Logic chuyển đăng nhập/đăng ký | `src/Api/wwwroot/js/app.js:121` |
| API | `src/Api/Controllers/AuthController.cs:15` |
| Interface | `src/Application/Auth/IAuthService.cs:8` |
| Service | `src/Infrastructure/Auth/AuthService.cs:29` |
| Chính sách mật khẩu | `src/Domain/Auth/PasswordPolicy.cs` |
| Băm và kiểm tra mật khẩu | `src/Domain/Auth/PasswordHasher.cs` |
| Cấu hình JWT | `src/Application/Auth/JwtSettings.cs` |
| Entity người dùng | `src/Domain/Entities/User.cs:6` |
| Test luồng xác thực | `tests/AiLearningPath.Tests/Auth/AuthServiceFlowTests.cs:25` |
| Test JWT/middleware | `tests/AiLearningPath.Tests/Auth/JwtMiddlewareIntegrationTests.cs:30` |

## 3. Phân quyền theo chủ sở hữu

**Mục đích:** người dùng chỉ được đọc hoặc cập nhật dữ liệu có `userId` của chính mình.

| Tầng | Vị trí |
|---|---|
| Middleware kiểm tra route | `src/Api/Middleware/OwnershipAuthorizationMiddleware.cs:17` |
| Interface phân quyền | `src/Application/Authorization/IResourceAuthorizer.cs:8` |
| Domain service phân quyền | `src/Application/Authorization/ResourceAuthorizer.cs` |
| Đăng ký middleware | `src/Api/Program.cs` |
| Property test | `tests/AiLearningPath.Tests/ResourceAuthorizerPropertyTests.cs:11` |

## 4. Hồ sơ cá nhân

**Mục đích:** lưu thông tin sinh viên, mục tiêu học tập, số giờ học và điểm số mục tiêu.

| Tầng | Vị trí |
|---|---|
| Giao diện | `src/Api/wwwroot/js/app.js:326` |
| API | `src/Api/Controllers/ProfileController.cs:19` |
| Interface/DTO | `src/Application/Profiles/IProfileService.cs:11`, `ProfileDtos.cs` |
| Service | `src/Infrastructure/Services/ProfileService.cs:15` |
| Kiểm tra dữ liệu | `src/Domain/Profiles/ProfileValidator.cs` |
| Danh mục mục tiêu | `src/Domain/Profiles/LearningGoalCatalog.cs` |
| Entity | `src/Domain/Entities/Profile.cs:6` |
| Test | `tests/AiLearningPath.Tests/Profiles/ProfileServiceTests.cs:17` |

## 5. Đánh giá năng lực

**Mục đích:** tạo câu hỏi theo mục tiêu, chấm điểm, phân tích trình độ, điểm mạnh/yếu và kỹ năng chi tiết.

| Tầng | Vị trí |
|---|---|
| Giao diện | `src/Api/wwwroot/js/app.js:412` |
| API bắt đầu/nộp bài | `src/Api/Controllers/AssessmentController.cs:20` |
| Interface/DTO | `src/Application/Assessments/IAssessmentEngine.cs:14`, `AssessmentDtos.cs` |
| Điều phối nghiệp vụ | `src/Infrastructure/Services/AssessmentEngine.cs` |
| Logic chấm điểm | `src/Domain/Assessments/AssessmentGrader.cs:10` |
| Model câu hỏi/kết quả | `src/Domain/Assessments/AssessmentModels.cs` |
| Ngân hàng câu hỏi chính | `src/Application/Assessments/AssessmentQuestionBank.cs` |
| IELTS/TOEIC | `src/Application/Assessments/AssessmentQuestionBank.English.cs` |
| Môn đại học | `src/Application/Assessments/AssessmentQuestionBank.University.cs` |
| Frontend/Backend | `src/Application/Assessments/AssessmentQuestionBank.WebDev.cs` |
| Data Analyst/AI Engineer | `src/Application/Assessments/AssessmentQuestionBank.Data.cs` |
| Entity bài đánh giá | `src/Domain/Entities/Assessment.cs:7` |
| Entity kết quả | `src/Domain/Entities/AssessmentResult.cs:6` |
| Test ngân hàng câu hỏi | `tests/AiLearningPath.Tests/Assessments/AssessmentQuestionBankTests.cs:12` |
| Test engine | `tests/AiLearningPath.Tests/Assessments/AssessmentEngineStartTests.cs:20` |

## 6. Learning DNA

**Mục đích:** xây hồ sơ học tập số từ kết quả đánh giá, phong cách, tốc độ, khả năng tập trung và giờ học hiệu quả.

| Tầng | Vị trí |
|---|---|
| Giao diện | `src/Api/wwwroot/js/app.js:529` |
| API | `src/Api/Controllers/LearningDnaController.cs:19` |
| Interface | `src/Application/LearningDna/ILearningDnaEngine.cs:12` |
| Service | `src/Infrastructure/Services/LearningDnaEngine.cs` |
| Domain builder | `src/Domain/LearningDna/DnaBuilder.cs:15` |
| Domain model | `src/Domain/LearningDna/LearningDnaModels.cs` |
| Entity | `src/Domain/Entities/LearningDnaProfile.cs:7` |
| Test | `tests/AiLearningPath.Tests/LearningDna/LearningDnaEngineTests.cs:27` |

## 7. Sinh lộ trình học cá nhân hóa

**Mục đích:** tạo lộ trình tháng/tuần/nhiệm vụ từ hồ sơ, đánh giá và Learning DNA; kiểm tra tính khả thi.

| Tầng | Vị trí |
|---|---|
| Giao diện | `src/Api/wwwroot/js/app.js:565` |
| API | `src/Api/Controllers/PathController.cs:21` |
| Interface | `src/Application/Paths/IPathGenerator.cs:17` |
| Service | `src/Infrastructure/Services/PathGenerator.cs` |
| Domain builder | `src/Domain/Paths/PathBuilder.cs:22` |
| Domain model | `src/Domain/Paths/PathModels.cs` |
| Entity gốc | `src/Domain/Entities/LearningPath.cs:7` |
| Entity giai đoạn | `src/Domain/Entities/PathPhase.cs:7` |
| Entity nhiệm vụ | `src/Domain/Entities/LearningTask.cs:7` |
| Test | `tests/AiLearningPath.Tests/Paths/PathGeneratorGenerateTests.cs:21` |

## 8. Bắt đầu học

**Mục đích:** mở bài theo nhiệm vụ, cung cấp tài liệu và 10 câu hỏi theo môn, Pomodoro, đánh giá cuối buổi và hoàn thành nhiệm vụ.

| Tầng | Vị trí |
|---|---|
| Route giao diện | `src/Api/wwwroot/js/app.js:664` |
| Tải/chuyển môn | `src/Api/wwwroot/js/app.js` - hàm `loadStudyLesson` |
| Render bài học và nhiệm vụ | `src/Api/wwwroot/js/app.js` - hàm `renderStudyLesson` |
| Render/chấm câu hỏi | `src/Api/wwwroot/js/app.js` - `buildStudyQuestion`, `answerStudyQuiz` |
| Pomodoro | `src/Api/wwwroot/js/app.js` - `StudyTimer`, `toggleStudyTimer`, `resetStudyTimer` |
| Hoàn thành và đánh giá | `src/Api/wwwroot/js/app.js` - `completeStudyLesson` |
| CSS màn hình học | `src/Api/wwwroot/css/app.css` - khối `Start study workspace` |
| API | `src/Api/Controllers/StudyLessonController.cs:12` |
| Interface/DTO | `src/Application/Study/IStudyLessonService.cs:3` |
| Service và dữ liệu bài học | `src/Infrastructure/Services/StudyLessonService.cs:10` |
| Entity phiên học | `src/Domain/Entities/StudySession.cs:6` |
| Entity nhiệm vụ | `src/Domain/Entities/LearningTask.cs:7` |
| Test | `tests/AiLearningPath.Tests/Study/StudyLessonServiceTests.cs:9` |

## 9. Dashboard tiến độ

**Mục đích:** tính Learning Score, tỷ lệ hoàn thành, tổng giờ học và biểu đồ tiến độ.

| Tầng | Vị trí |
|---|---|
| Giao diện | `src/Api/wwwroot/js/app.js:919` |
| API | `src/Api/Controllers/DashboardController.cs:19` |
| Interface/DTO | `src/Application/Progress/IProgressDashboardService.cs:14` |
| Service tổng hợp | `src/Infrastructure/Services/ProgressDashboardService.cs:22` |
| Công thức domain | `src/Domain/Progress/ProgressCalculator.cs:20` |
| Entity mốc tiến độ | `src/Domain/Entities/ProgressSnapshot.cs:6` |
| Entity phiên học | `src/Domain/Entities/StudySession.cs:6` |
| Test empty state | `tests/AiLearningPath.Tests/Progress/ProgressDashboardServiceEmptyStateTests.cs:18` |
| Property tests | `tests/AiLearningPath.Tests/Progress/` |

## 10. AI Coach, AI Insight và AI Tutor

**Mục đích:** sinh nhận xét tiến độ, trả lời theo ngữ cảnh người học và thu thập feedback.

| Tầng | Vị trí |
|---|---|
| Giao diện | `src/Api/wwwroot/js/app.js:959` |
| API AI | `src/Api/Controllers/AiController.cs:13` |
| API trạng thái provider | `src/Api/Controllers/SystemController.cs:12` |
| Interface/DTO | `src/Application/Ai/AiFeatureDtos.cs` |
| AI Tutor | `src/Infrastructure/Services/AiTutorService.cs:10` |
| AI Insight | `src/Infrastructure/Services/AiInsightService.cs:9` |
| AI Feedback | `src/Infrastructure/Services/AiFeedbackService.cs:7` |
| Hội thoại | `src/Domain/Entities/TutorConversation.cs:3` |
| Feedback entity | `src/Domain/Entities/AiFeedback.cs:3` |

## 11. AI Gateway, Gemini, cache và fallback

**Mục đích:** tạo một cổng dùng chung quản lý prompt, provider, timeout, cache, log, fallback và giới hạn dữ liệu.

| Thành phần | Vị trí |
|---|---|
| Interface Gateway | `src/Application/ExternalServices/IAiGateway.cs:30` |
| Gateway điều phối | `src/Infrastructure/Services/AiGateway.cs` |
| Adapter Gemini | `src/Infrastructure/Services/GeminiContentGenerator.cs` |
| Adapter nội dung | `src/Infrastructure/Services/AiContentGeneratorAdapter.cs` |
| Fallback nội dung | `src/Infrastructure/Services/PlaceholderContentGenerator.cs` |
| Resilient wrapper | `src/Infrastructure/Services/ResilientContentGenerator.cs` |
| Cấu hình AI | `src/Infrastructure/Services/AiOptions.cs`, `ExternalServiceOptions.cs` |
| Cache entity | `src/Domain/Entities/AiCacheEntry.cs:3` |
| Log tương tác | `src/Domain/Entities/AiInteractionLog.cs:3` |
| Test fallback | `tests/AiLearningPath.Tests/ExternalServices/ResilientContentGeneratorFallbackProperties.cs` |

## 12. Smart Scheduler

**Mục đích:** phân bổ nhiệm vụ chưa hoàn thành vào lịch học dựa trên số giờ, deadline và khung giờ ưu tiên.

| Tầng | Vị trí |
|---|---|
| Giao diện | `src/Api/wwwroot/js/app.js:1080` |
| API | `src/Api/Controllers/StudyScheduleController.cs:13` |
| Interface/DTO | `src/Application/Scheduling/SchedulingDtos.cs:3` |
| Service | `src/Infrastructure/Services/StudyScheduleService.cs:10` |
| Entity lịch | `src/Domain/Entities/StudySchedule.cs:3` |
| Entity mục lịch | `src/Domain/Entities/StudySchedule.cs:16` |

## 13. Adaptive Learning

**Mục đích:** điều chỉnh lộ trình dựa trên kỹ năng yếu và tín hiệu tiến độ mà không ghi đè bản gốc.

| Tầng | Vị trí |
|---|---|
| Giao diện | `src/Api/wwwroot/js/app.js:1131` |
| API | `src/Api/Controllers/PathController.cs` - action `Adapt` |
| Interface/DTO | `src/Application/Adaptive/AdaptiveDtos.cs:3` |
| Service | `src/Infrastructure/Services/AdaptiveLearningService.cs:10` |
| Audit entity | `src/Domain/Entities/AdaptationEvent.cs:3` |

## 14. Academic Twin và Machine Learning

**Mục đích:** mô phỏng xác suất đạt mục tiêu khi thay đổi số giờ học.

| Tầng | Vị trí |
|---|---|
| Giao diện | `src/Api/wwwroot/js/app.js:1184` |
| API | `src/Api/Controllers/TwinController.cs:18` |
| Interface | `src/Application/Twin/IAcademicTwinService.cs:13` |
| Service điều phối | `src/Infrastructure/Services/AcademicTwinService.cs:24` |
| Prediction interface | `src/Application/ExternalServices/IPredictionService.cs:24` |
| HTTP ML adapter | `src/Infrastructure/Services/MlPredictionService.cs:11` |
| Resilient/fallback | `src/Infrastructure/Services/ResilientPredictionService.cs:14` |
| Fallback xác định | `src/Infrastructure/Services/PlaceholderPredictionService.cs:18` |
| Domain validation/model | `src/Domain/Twin/TwinValidator.cs`, `TwinModels.cs` |
| Python ML API | `ml-service/app.py` |
| Python smoke test | `ml-service/test_smoke.py` |
| Test tích hợp | `tests/AiLearningPath.Tests/Twin/AcademicTwinSimulateIntegrationTests.cs:25` |
| Property tests | `tests/AiLearningPath.Tests/Twin/` |

## 15. Hướng nghiệp

**Mục đích:** tạo lộ trình kỹ năng nghề nghiệp, gợi ý chứng chỉ và dự án.

| Tầng | Vị trí |
|---|---|
| Giao diện | `src/Api/wwwroot/js/app.js:1238` |
| API | `src/Api/Controllers/CareerController.cs:20` |
| Interface/DTO | `src/Application/Career/ICareerPathService.cs:14`, `CareerDtos.cs` |
| Service | `src/Infrastructure/Services/CareerPathService.cs:25` |
| Danh mục nghề | `src/Domain/Careers/CareerCatalog.cs` |
| Entity | `src/Domain/Entities/CareerPath.cs:6` |
| Test | `tests/AiLearningPath.Tests/Career/CareerPathServiceTests.cs:26` |

## 16. Giao diện sáng/tối và responsive

| Thành phần | Vị trí |
|---|---|
| Theme manager | `src/Api/wwwroot/js/theme.js:9` |
| CSS ứng dụng | `src/Api/wwwroot/css/app.css` |
| CSS landing page | `src/Api/wwwroot/css/landing.css` |
| Landing interaction | `src/Api/wwwroot/js/landing.js` |
| Kiểm tra contrast | `tests/frontend/contrast.test.js` |
| Kiểm tra theme | `tests/frontend/theme.resolve.test.js` |

## 17. Kiểm thử xuyên suốt hệ thống

| Mục tiêu | Vị trí |
|---|---|
| Luồng end-to-end | `tests/AiLearningPath.Tests/Integration/EndToEndFlowIntegrationTests.cs:31` |
| Auth/JWT | `tests/AiLearningPath.Tests/Auth/` |
| Assessment | `tests/AiLearningPath.Tests/Assessments/` |
| Learning DNA | `tests/AiLearningPath.Tests/LearningDna/` |
| Lộ trình | `tests/AiLearningPath.Tests/Paths/` |
| Bắt đầu học | `tests/AiLearningPath.Tests/Study/` |
| Tiến độ | `tests/AiLearningPath.Tests/Progress/` |
| Academic Twin | `tests/AiLearningPath.Tests/Twin/` |
| External service/fallback | `tests/AiLearningPath.Tests/ExternalServices/` |
| Frontend | `tests/frontend/` |

## 18. Cách trình bày một chức năng trong báo cáo

Có thể dùng cấu trúc sau cho mỗi chức năng:

1. **Mục tiêu:** chức năng giải quyết vấn đề gì.
2. **Đầu vào:** dữ liệu người dùng hoặc dữ liệu từ chức năng trước.
3. **Luồng xử lý:** frontend gọi endpoint nào, controller gọi service nào, domain xử lý quy tắc gì.
4. **Dữ liệu lưu trữ:** entity và bảng liên quan.
5. **Đầu ra:** dữ liệu hoặc giao diện trả về cho người dùng.
6. **An toàn và lỗi:** authorization, validation, fallback, exception filter.
7. **Kiểm thử:** test file chứng minh hành vi.

Ví dụ với **Bắt đầu học**:

> Frontend tại `app.js` gọi `GET /api/users/{userId}/study-lessons/today`. `StudyLessonController` chuyển yêu cầu cho `StudyLessonService`. Service truy vấn `AssessmentResult`, `LearningPath`, `PathPhase` và `LearningTask`, sau đó ghép tài liệu cùng 10 câu hỏi theo kỹ năng. Khi kết thúc, frontend gọi endpoint complete; service tạo `StudySession`, đánh dấu `LearningTask.Completed` và yêu cầu `ProgressDashboardService` tính lại tiến độ.
