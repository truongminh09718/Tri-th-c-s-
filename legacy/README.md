# legacy/ — Mã nguồn nguyên mẫu (không dùng trong sản phẩm chính)

Thư mục này chứa các thành phần **nguyên mẫu (prototype)** được phát triển ở giai đoạn
thử nghiệm ban đầu và **không còn được sản phẩm chính sử dụng**. Giữ lại để minh bạch
quá trình nghiên cứu và để tham chiếu dữ liệu huấn luyện.

## AiPrediction/

Dịch vụ dự đoán **nguyên mẫu** viết bằng **Flask + scikit-learn (RandomForest)**, huấn luyện
trên tập dữ liệu `data/student_training_data.csv` để dự đoán GPA, TOEIC và phân loại
khả năng đạt TOEIC ≥ 700.

**Vì sao nằm ở legacy:** sản phẩm chính (`AiLearningPath`) gọi dịch vụ ML chính thức tại
`AiLearningPath/ml-service/` (FastAPI). `AiPrediction` không được backend .NET gọi tới và
chỉ tồn tại như một bước thử nghiệm mô hình.

**Quan hệ với sản phẩm chính:**

| | `AiLearningPath/ml-service/` (CHÍNH THỨC) | `legacy/AiPrediction/` (NGUYÊN MẪU) |
|---|---|---|
| Framework | FastAPI | Flask |
| Mô hình | LogisticRegression (đảm bảo đơn điệu theo giờ học) | RandomForest |
| Được app .NET gọi | ✅ Có (qua `MlPredictionService`) | ❌ Không |
| Vai trò | Academic Twin – xác suất đạt mục tiêu | Thử nghiệm dự đoán GPA/TOEIC |

Xem `AiLearningPath/ml-service/README.md` để biết dịch vụ ML đang vận hành.
