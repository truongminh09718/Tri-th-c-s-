namespace AiLearningPath.Application.Assessments;

/// <summary>
/// Phần dữ liệu ngân hàng câu hỏi cho Data Analyst và AI Engineer.
/// </summary>
public static partial class AssessmentQuestionBank
{
    private static readonly IReadOnlyList<AssessmentQuestionTemplate> DataAnalyst = new[]
    {
        // SQL
        Q("SQL",
            "Mệnh đề SQL nào dùng để lọc các bản ghi theo điều kiện?",
            "ORDER BY", "WHERE", "GROUP BY", "HAVING", "WHERE"),
        Q("SQL",
            "Hàm tổng hợp nào tính giá trị trung bình của một cột số?",
            "SUM", "COUNT", "AVG", "MAX", "AVG"),
        Q("SQL",
            "Để đếm số bản ghi theo từng nhóm, ta thường kết hợp COUNT với mệnh đề nào?",
            "WHERE", "GROUP BY", "JOIN", "LIMIT", "GROUP BY"),
        // Statistics
        Q("Statistics",
            "Giá trị xuất hiện nhiều nhất trong một tập dữ liệu được gọi là gì?",
            "Trung bình (mean)", "Trung vị (median)", "Yếu vị (mode)", "Phương sai", "Yếu vị (mode)"),
        Q("Statistics",
            "Độ đo nào phản ánh mức độ phân tán của dữ liệu quanh giá trị trung bình?",
            "Trung vị", "Độ lệch chuẩn", "Yếu vị", "Tần số", "Độ lệch chuẩn"),
        Q("Statistics",
            "Hệ số tương quan bằng 0 cho biết điều gì giữa hai biến?",
            "Tương quan dương mạnh", "Tương quan âm mạnh", "Không có tương quan tuyến tính",
            "Quan hệ nhân quả", "Không có tương quan tuyến tính"),
        // Data Visualization
        Q("Data Visualization",
            "Loại biểu đồ nào phù hợp nhất để thể hiện xu hướng theo thời gian?",
            "Biểu đồ tròn", "Biểu đồ đường", "Biểu đồ phân tán", "Biểu đồ cột chồng",
            "Biểu đồ đường"),
        Q("Data Visualization",
            "Biểu đồ nào phù hợp để so sánh tỷ lệ phần trăm của các phần trong một tổng thể?",
            "Biểu đồ đường", "Biểu đồ tròn", "Histogram", "Boxplot", "Biểu đồ tròn"),
        // Spreadsheets
        Q("Spreadsheets",
            "Hàm Excel nào dùng để tra cứu một giá trị theo cột?",
            "SUMIF", "VLOOKUP", "CONCAT", "TRIM", "VLOOKUP"),
        // Python/Pandas
        Q("Python/Pandas",
            "Trong pandas, cấu trúc dữ liệu hai chiều dạng bảng được gọi là gì?",
            "Series", "DataFrame", "Array", "List", "DataFrame"),
        // SQL (mở rộng)
        Q("SQL",
            "Mệnh đề nào dùng để sắp xếp kết quả truy vấn?",
            "GROUP BY", "ORDER BY", "WHERE", "HAVING", "ORDER BY"),
        Q("SQL",
            "Để lọc kết quả sau khi đã GROUP BY, ta dùng mệnh đề nào?",
            "WHERE", "HAVING", "LIMIT", "ON", "HAVING"),
        Q("SQL",
            "Từ khóa nào loại bỏ các giá trị trùng lặp trong kết quả truy vấn?",
            "UNIQUE", "DISTINCT", "DELETE", "TRIM", "DISTINCT"),
        // Statistics (mở rộng)
        Q("Statistics",
            "Giá trị nằm chính giữa của một tập dữ liệu đã sắp xếp gọi là gì?",
            "Trung bình", "Trung vị (median)", "Yếu vị", "Phương sai", "Trung vị (median)"),
        Q("Statistics",
            "Phân phối chuẩn (normal distribution) có hình dạng đặc trưng nào?",
            "Hình chữ U", "Hình chuông", "Hình bậc thang", "Đường thẳng", "Hình chuông"),
        Q("Statistics",
            "Giá trị p (p-value) nhỏ hơn mức ý nghĩa thường dẫn đến kết luận gì?",
            "Chấp nhận giả thuyết H0", "Bác bỏ giả thuyết H0", "Không kết luận được",
            "Tăng cỡ mẫu", "Bác bỏ giả thuyết H0"),
        // Data Visualization (mở rộng)
        Q("Data Visualization",
            "Biểu đồ nào phù hợp để xem phân bố và các giá trị ngoại lai của một biến số?",
            "Biểu đồ tròn", "Boxplot", "Biểu đồ đường", "Bản đồ nhiệt", "Boxplot"),
        Q("Data Visualization",
            "Biểu đồ histogram chủ yếu dùng để thể hiện điều gì?",
            "Xu hướng thời gian", "Phân bố tần suất", "Tỷ lệ phần trăm", "Quan hệ nhân quả",
            "Phân bố tần suất"),
        // Spreadsheets (mở rộng)
        Q("Spreadsheets",
            "Hàm nào tính tổng các ô thỏa một điều kiện trong Excel?",
            "SUM", "SUMIF", "COUNT", "AVERAGE", "SUMIF"),
        Q("Spreadsheets",
            "PivotTable trong Excel chủ yếu dùng để làm gì?",
            "Vẽ ảnh", "Tổng hợp và phân tích dữ liệu", "Gửi email", "Nén file",
            "Tổng hợp và phân tích dữ liệu"),
        // Python/Pandas (mở rộng)
        Q("Python/Pandas",
            "Phương thức pandas nào hiển thị vài dòng đầu của DataFrame?",
            "tail()", "head()", "info()", "describe()", "head()"),
        Q("Python/Pandas",
            "Để gom nhóm dữ liệu rồi tính tổng hợp trong pandas, ta dùng phương thức nào?",
            "merge()", "groupby()", "sort_values()", "drop()", "groupby()"),
        Q("Python/Pandas",
            "Giá trị thiếu trong pandas thường được biểu diễn bằng gì?",
            "0", "NaN", "null", "empty", "NaN"),
        // Statistics (mở rộng thêm)
        Q("Statistics",
            "Đại lượng nào đo mức độ phân tán bằng bình phương độ lệch so với trung bình?",
            "Trung vị", "Phương sai", "Yếu vị", "Tần số", "Phương sai"),
        // SQL (mở rộng thêm)
        Q("SQL",
            "Phép JOIN nào chỉ trả về các bản ghi khớp ở cả hai bảng?",
            "LEFT JOIN", "INNER JOIN", "RIGHT JOIN", "FULL JOIN", "INNER JOIN"),
    };

    private static readonly IReadOnlyList<AssessmentQuestionTemplate> AiEngineer = new[]
    {
        // Machine Learning
        Q("Machine Learning",
            "Học có giám sát (supervised learning) yêu cầu dữ liệu phải có yếu tố nào?",
            "Không cần nhãn", "Có nhãn (labels)", "Chỉ ảnh", "Chỉ văn bản", "Có nhãn (labels)"),
        Q("Machine Learning",
            "Hiện tượng mô hình học quá khớp dữ liệu huấn luyện và kém trên dữ liệu mới gọi là gì?",
            "Underfitting", "Overfitting", "Regularization", "Normalization", "Overfitting"),
        Q("Machine Learning",
            "Thuật toán nào thường dùng cho bài toán phân loại nhị phân cơ bản?",
            "Linear Regression", "Logistic Regression", "K-Means", "PCA", "Logistic Regression"),
        // Deep Learning
        Q("Deep Learning",
            "Thành phần nào đưa tính phi tuyến vào mạng nơ-ron?",
            "Hàm kích hoạt (activation)", "Learning rate", "Batch size", "Epoch",
            "Hàm kích hoạt (activation)"),
        Q("Deep Learning",
            "Kiến trúc mạng nào thường dùng cho xử lý ảnh?",
            "RNN", "CNN", "Transformer", "GAN", "CNN"),
        // Math/Linear Algebra
        Q("Math/Linear Algebra",
            "Phép toán cốt lõi trong lan truyền tiến của mạng nơ-ron là gì?",
            "Nhân ma trận", "Đạo hàm riêng phần", "Tích phân", "Phép chia", "Nhân ma trận"),
        Q("Math/Linear Algebra",
            "Gradient descent dùng đại lượng nào để cập nhật trọng số?",
            "Tích phân", "Đạo hàm (gradient)", "Định thức", "Hạng ma trận", "Đạo hàm (gradient)"),
        // Python
        Q("Python",
            "Thư viện Python nào phổ biến cho tính toán mảng số học hiệu năng cao?",
            "NumPy", "Flask", "Requests", "Pillow", "NumPy"),
        // MLOps
        Q("MLOps",
            "Thuật ngữ nào mô tả việc theo dõi hiệu năng mô hình suy giảm theo thời gian?",
            "Model drift", "Feature scaling", "Data leakage", "Cross validation", "Model drift"),
        Q("MLOps",
            "Việc đóng gói mô hình thành dịch vụ để phục vụ dự đoán gọi là gì?",
            "Training", "Deployment/Serving", "Labeling", "Cleaning", "Deployment/Serving"),
        // Machine Learning (mở rộng)
        Q("Machine Learning",
            "Học không giám sát (unsupervised learning) thường dùng cho bài toán nào?",
            "Phân loại có nhãn", "Phân cụm (clustering)", "Hồi quy tuyến tính có nhãn",
            "Dự đoán giá có nhãn", "Phân cụm (clustering)"),
        Q("Machine Learning",
            "Kỹ thuật nào giúp đánh giá mô hình ổn định hơn bằng cách chia dữ liệu nhiều lần?",
            "Overfitting", "Cross validation", "Normalization", "Dropout", "Cross validation"),
        Q("Machine Learning",
            "Việc dữ liệu kiểm tra vô tình rò rỉ vào huấn luyện gây đánh giá sai gọi là gì?",
            "Data leakage", "Model drift", "Underfitting", "Regularization", "Data leakage"),
        // Deep Learning (mở rộng)
        Q("Deep Learning",
            "Kỹ thuật nào giảm overfitting bằng cách ngẫu nhiên tắt một số nơ-ron khi huấn luyện?",
            "Batch norm", "Dropout", "Pooling", "Padding", "Dropout"),
        Q("Deep Learning",
            "Kiến trúc nào là nền tảng của các mô hình ngôn ngữ lớn (LLM) hiện đại?",
            "CNN", "RNN", "Transformer", "SVM", "Transformer"),
        Q("Deep Learning",
            "Thuật toán nào dùng để cập nhật trọng số dựa trên lan truyền ngược lỗi?",
            "Backpropagation", "K-Means", "PCA", "A* search", "Backpropagation"),
        // Math/Linear Algebra (mở rộng)
        Q("Math/Linear Algebra",
            "Hàm nào thường dùng để chuyển đầu ra thành phân phối xác suất nhiều lớp?",
            "ReLU", "Softmax", "Sigmoid đơn", "Tanh", "Softmax"),
        Q("Math/Linear Algebra",
            "Learning rate trong gradient descent điều khiển điều gì?",
            "Số lớp mạng", "Kích thước bước cập nhật", "Số epoch", "Cỡ batch",
            "Kích thước bước cập nhật"),
        // Python (mở rộng)
        Q("Python",
            "Thư viện nào phổ biến để huấn luyện mô hình machine learning cổ điển trong Python?",
            "scikit-learn", "Flask", "Django", "Requests", "scikit-learn"),
        Q("Python",
            "Framework nào do Google phát triển cho deep learning?",
            "TensorFlow", "NumPy", "Pandas", "Matplotlib", "TensorFlow"),
        // MLOps (mở rộng)
        Q("MLOps",
            "Việc theo dõi phiên bản dữ liệu và mô hình qua thời gian gọi chung là gì?",
            "Versioning", "Caching", "Indexing", "Hashing", "Versioning"),
        Q("MLOps",
            "Quy trình tự động hóa huấn luyện và triển khai mô hình thường gọi là gì?",
            "CI/CD cho ML (pipeline)", "SQL join", "Web scraping", "Load testing",
            "CI/CD cho ML (pipeline)"),
        // Machine Learning (mở rộng thêm)
        Q("Machine Learning",
            "Tập dữ liệu nào dùng để đánh giá cuối cùng và KHÔNG dùng khi huấn luyện?",
            "Training set", "Test set", "Validation khi tune", "Toàn bộ dữ liệu", "Test set"),
    };
}
