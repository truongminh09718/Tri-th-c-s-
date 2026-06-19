namespace AiLearningPath.Application.Assessments;

/// <summary>
/// Phần dữ liệu ngân hàng câu hỏi cho các môn đại học phổ biến (UniversitySubject):
/// Toán cao cấp, Lập trình, Cấu trúc dữ liệu, Cơ sở dữ liệu, Mạng máy tính.
/// </summary>
public static partial class AssessmentQuestionBank
{
    private static readonly IReadOnlyList<AssessmentQuestionTemplate> UniversitySubject = new[]
    {
        // Toán cao cấp
        Q("Toán cao cấp",
            "Đạo hàm của hàm số f(x) = x² là gì?",
            "x", "2x", "x²/2", "2", "2x"),
        Q("Toán cao cấp",
            "Tích phân bất định của hàm f(x) = 1 (theo x) là gì?",
            "0", "x + C", "1 + C", "x² + C", "x + C"),
        Q("Toán cao cấp",
            "Giới hạn của (1/x) khi x tiến tới vô cùng là bao nhiêu?",
            "Vô cùng", "1", "0", "Không tồn tại", "0"),
        // Lập trình
        Q("Lập trình",
            "Cấu trúc điều khiển nào dùng để lặp lại một khối lệnh nhiều lần?",
            "if", "switch", "vòng lặp (loop)", "return", "vòng lặp (loop)"),
        Q("Lập trình",
            "Biến (variable) trong lập trình dùng để làm gì?",
            "Lưu trữ dữ liệu", "Vẽ giao diện", "Kết nối mạng", "Nén file", "Lưu trữ dữ liệu"),
        Q("Lập trình",
            "Hàm (function) giúp ích gì trong lập trình?",
            "Làm chậm chương trình", "Tái sử dụng và tổ chức mã", "Tăng dung lượng file",
            "Xóa dữ liệu", "Tái sử dụng và tổ chức mã"),
        // Cấu trúc dữ liệu
        Q("Cấu trúc dữ liệu",
            "Cấu trúc dữ liệu nào hoạt động theo nguyên tắc LIFO (vào sau ra trước)?",
            "Hàng đợi (Queue)", "Ngăn xếp (Stack)", "Cây (Tree)", "Đồ thị (Graph)",
            "Ngăn xếp (Stack)"),
        Q("Cấu trúc dữ liệu",
            "Độ phức tạp thời gian trung bình của tìm kiếm nhị phân là bao nhiêu?",
            "O(n)", "O(log n)", "O(n²)", "O(1)", "O(log n)"),
        // Cơ sở dữ liệu
        Q("Cơ sở dữ liệu",
            "Trong mô hình quan hệ, dữ liệu được tổ chức chủ yếu dưới dạng nào?",
            "Bảng (table)", "Cây", "Đồ thị", "Tệp văn bản", "Bảng (table)"),
        // Mạng máy tính
        Q("Mạng máy tính",
            "Giao thức nào là nền tảng truyền tải trang web trên Internet?",
            "FTP", "HTTP", "SMTP", "SSH", "HTTP"),
        // Toán cao cấp (mở rộng)
        Q("Toán cao cấp",
            "Đạo hàm của hàm số f(x) = sin(x) là gì?",
            "cos(x)", "-cos(x)", "-sin(x)", "tan(x)", "cos(x)"),
        Q("Toán cao cấp",
            "Một ma trận vuông khả nghịch khi và chỉ khi định thức của nó _____.",
            "bằng 0", "khác 0", "âm", "dương", "khác 0"),
        Q("Toán cao cấp",
            "Đạo hàm của hằng số c (theo x) bằng bao nhiêu?",
            "c", "1", "0", "x", "0"),
        // Lập trình (mở rộng)
        Q("Lập trình",
            "Cấu trúc điều kiện nào dùng để rẽ nhánh theo nhiều trường hợp giá trị?",
            "for", "switch", "while", "do-while", "switch"),
        Q("Lập trình",
            "Mảng (array) là cấu trúc lưu trữ điều gì?",
            "Một giá trị duy nhất", "Tập các phần tử cùng kiểu", "Một hàm", "Một file",
            "Tập các phần tử cùng kiểu"),
        Q("Lập trình",
            "Lỗi xảy ra khi chương trình đang chạy được gọi là gì?",
            "Lỗi cú pháp (syntax)", "Lỗi thời gian chạy (runtime)", "Lỗi biên dịch", "Cảnh báo",
            "Lỗi thời gian chạy (runtime)"),
        // Cấu trúc dữ liệu (mở rộng)
        Q("Cấu trúc dữ liệu",
            "Cấu trúc dữ liệu nào hoạt động theo nguyên tắc FIFO (vào trước ra trước)?",
            "Ngăn xếp (Stack)", "Hàng đợi (Queue)", "Cây nhị phân", "Bảng băm",
            "Hàng đợi (Queue)"),
        Q("Cấu trúc dữ liệu",
            "Cấu trúc nào cho phép tra cứu theo khóa với độ phức tạp trung bình O(1)?",
            "Danh sách liên kết", "Bảng băm (hash table)", "Cây cân bằng", "Mảng tuyến tính",
            "Bảng băm (hash table)"),
        Q("Cấu trúc dữ liệu",
            "Độ phức tạp thời gian xấu nhất của thuật toán sắp xếp nổi bọt (bubble sort) là gì?",
            "O(n)", "O(n log n)", "O(n²)", "O(log n)", "O(n²)"),
        // Cơ sở dữ liệu (mở rộng)
        Q("Cơ sở dữ liệu",
            "Khóa ngoại (foreign key) dùng để làm gì?",
            "Xác định duy nhất bản ghi", "Liên kết tới khóa chính của bảng khác",
            "Tăng tốc sắp xếp", "Mã hóa dữ liệu", "Liên kết tới khóa chính của bảng khác"),
        Q("Cơ sở dữ liệu",
            "Ngôn ngữ nào dùng để truy vấn cơ sở dữ liệu quan hệ?",
            "HTML", "SQL", "CSS", "XML", "SQL"),
        // Mạng máy tính (mở rộng)
        Q("Mạng máy tính",
            "Thiết bị nào định tuyến gói tin giữa các mạng khác nhau?",
            "Switch", "Router", "Hub", "Repeater", "Router"),
        Q("Mạng máy tính",
            "Giao thức nào chuyển tên miền thành địa chỉ IP?",
            "HTTP", "DNS", "FTP", "SMTP", "DNS"),
        Q("Mạng máy tính",
            "Tầng nào trong mô hình OSI chịu trách nhiệm định tuyến (routing)?",
            "Tầng vật lý", "Tầng mạng (Network)", "Tầng ứng dụng", "Tầng liên kết dữ liệu",
            "Tầng mạng (Network)"),
        // Toán cao cấp (mở rộng thêm)
        Q("Toán cao cấp",
            "Tích phân xác định của một hàm dương trên [a, b] biểu thị điều gì?",
            "Độ dốc", "Diện tích dưới đường cong", "Giới hạn", "Đạo hàm cấp hai",
            "Diện tích dưới đường cong"),
    };
}
