namespace AiLearningPath.Application.Assessments;

/// <summary>
/// Phần dữ liệu ngân hàng câu hỏi cho lập trình web: Frontend và Backend Development.
/// </summary>
public static partial class AssessmentQuestionBank
{
    private static readonly IReadOnlyList<AssessmentQuestionTemplate> FrontendDevelopment = new[]
    {
        // HTML/CSS
        Q("HTML/CSS",
            "Thẻ HTML nào dùng để tạo danh sách không có thứ tự?",
            "<ol>", "<ul>", "<li>", "<dl>", "<ul>"),
        Q("HTML/CSS",
            "Thuộc tính CSS nào dùng để thay đổi khoảng cách bên trong giữa nội dung và viền?",
            "margin", "border", "padding", "gap", "padding"),
        Q("HTML/CSS",
            "Bố cục nào của CSS phù hợp nhất để dàn các item theo một chiều (hàng hoặc cột)?",
            "Flexbox", "float", "table", "position: absolute", "Flexbox"),
        // JavaScript
        Q("JavaScript",
            "Từ khóa nào khai báo biến có phạm vi khối (block scope) và không gán lại được?",
            "var", "let", "const", "function", "const"),
        Q("JavaScript",
            "Kết quả của biểu thức `typeof null` trong JavaScript là gì?",
            "\"null\"", "\"object\"", "\"undefined\"", "\"number\"", "\"object\""),
        Q("JavaScript",
            "Phương thức mảng nào tạo mảng mới bằng cách biến đổi từng phần tử?",
            "forEach", "map", "filter", "reduce", "map"),
        // React
        Q("React",
            "Hook nào dùng để khai báo state trong một function component?",
            "useEffect", "useState", "useRef", "useMemo", "useState"),
        Q("React",
            "Trong React, thuộc tính dùng để giúp danh sách render hiệu quả là gì?",
            "id", "ref", "key", "name", "key"),
        // Web Performance
        Q("Web Performance",
            "Kỹ thuật nào giúp giảm thời gian tải ban đầu bằng cách chỉ tải mã khi cần?",
            "minify", "code splitting", "gzip", "caching", "code splitting"),
        // Accessibility
        Q("Accessibility",
            "Thuộc tính nào cung cấp văn bản thay thế cho hình ảnh phục vụ trình đọc màn hình?",
            "title", "alt", "aria-hidden", "role", "alt"),
        // HTML/CSS (mở rộng)
        Q("HTML/CSS",
            "Thẻ nào dùng để nhúng một liên kết (hyperlink) trong HTML?",
            "<link>", "<a>", "<href>", "<nav>", "<a>"),
        Q("HTML/CSS",
            "Đơn vị CSS nào là tương đối theo cỡ chữ gốc của trang?",
            "px", "rem", "pt", "cm", "rem"),
        Q("HTML/CSS",
            "Thuộc tính CSS nào dùng để xếp các phần tử theo lưới hai chiều?",
            "flex", "grid", "float", "inline", "grid"),
        // JavaScript (mở rộng)
        Q("JavaScript",
            "Toán tử nào so sánh cả giá trị lẫn kiểu dữ liệu trong JavaScript?",
            "==", "===", "=", "!=", "==="),
        Q("JavaScript",
            "Phương thức nào dùng để chuyển một chuỗi JSON thành đối tượng JavaScript?",
            "JSON.stringify", "JSON.parse", "JSON.toObject", "JSON.read", "JSON.parse"),
        Q("JavaScript",
            "Promise trong JavaScript dùng để xử lý điều gì?",
            "Tác vụ bất đồng bộ", "Định dạng CSS", "Vẽ canvas", "Nén ảnh", "Tác vụ bất đồng bộ"),
        // React (mở rộng)
        Q("React",
            "Hook nào dùng để chạy hiệu ứng phụ (side effect) sau khi render?",
            "useState", "useEffect", "useContext", "useReducer", "useEffect"),
        Q("React",
            "Dữ liệu truyền từ component cha xuống con trong React gọi là gì?",
            "state", "props", "ref", "context", "props"),
        Q("React",
            "JSX về bản chất được biên dịch thành lời gọi nào?",
            "React.createElement", "document.write", "innerHTML", "render()", "React.createElement"),
        // Web Performance (mở rộng)
        Q("Web Performance",
            "Việc trì hoãn tải ảnh cho đến khi cần hiển thị được gọi là gì?",
            "lazy loading", "prefetch", "minify", "bundling", "lazy loading"),
        Q("Web Performance",
            "Kỹ thuật nào giảm kích thước file bằng cách loại bỏ khoảng trắng và comment?",
            "caching", "minification", "lazy loading", "prefetch", "minification"),
        // Accessibility (mở rộng)
        Q("Accessibility",
            "Thuộc tính ARIA nào mô tả nhãn cho phần tử khi không có văn bản hiển thị?",
            "aria-label", "data-id", "class", "tabindex", "aria-label"),
        Q("Accessibility",
            "Mức tương phản màu tối thiểu cho văn bản thường theo WCAG AA là bao nhiêu?",
            "2:1", "3:1", "4.5:1", "7:1", "4.5:1"),
        // HTML/CSS (mở rộng thêm)
        Q("HTML/CSS",
            "Thuộc tính CSS nào điều khiển khoảng cách bên ngoài giữa các phần tử?",
            "padding", "margin", "border", "gap", "margin"),
    };

    private static readonly IReadOnlyList<AssessmentQuestionTemplate> BackendDevelopment = new[]
    {
        // API Design
        Q("API Design",
            "Phương thức HTTP nào phù hợp để tạo mới một tài nguyên?",
            "GET", "POST", "DELETE", "HEAD", "POST"),
        Q("API Design",
            "Mã trạng thái HTTP nào cho biết tài nguyên không được tìm thấy?",
            "200", "301", "404", "500", "404"),
        Q("API Design",
            "Trong REST, một endpoint dạng /users/{id} thường dùng để làm gì?",
            "Tạo nhiều user", "Truy cập một user cụ thể", "Xóa toàn bộ user", "Đăng nhập",
            "Truy cập một user cụ thể"),
        // Databases
        Q("Databases",
            "Câu lệnh SQL nào dùng để lấy dữ liệu từ bảng?",
            "INSERT", "UPDATE", "SELECT", "DROP", "SELECT"),
        Q("Databases",
            "Khóa nào xác định duy nhất mỗi bản ghi trong một bảng quan hệ?",
            "Foreign key", "Primary key", "Index", "Constraint", "Primary key"),
        Q("Databases",
            "Phép JOIN nào trả về tất cả bản ghi ở bảng trái và các bản ghi khớp ở bảng phải?",
            "INNER JOIN", "LEFT JOIN", "RIGHT JOIN", "CROSS JOIN", "LEFT JOIN"),
        // Authentication
        Q("Authentication",
            "JWT thường được dùng để làm gì trong API?",
            "Nén dữ liệu", "Xác thực và mang thông tin phiên", "Lưu trữ file", "Tối ưu truy vấn",
            "Xác thực và mang thông tin phiên"),
        Q("Authentication",
            "Mật khẩu nên được lưu trong cơ sở dữ liệu dưới dạng nào?",
            "Văn bản thuần", "Mã hóa base64", "Băm (hash) có salt", "Nén gzip",
            "Băm (hash) có salt"),
        // Caching
        Q("Caching",
            "Mục đích chính của caching trong backend là gì?",
            "Tăng bảo mật", "Giảm độ trễ và tải cho hệ thống", "Mã hóa dữ liệu", "Sao lưu dữ liệu",
            "Giảm độ trễ và tải cho hệ thống"),
        // Concurrency
        Q("Concurrency",
            "Tình trạng nhiều tiến trình tranh chấp cùng một tài nguyên gây kết quả sai gọi là gì?",
            "Deadlock", "Race condition", "Timeout", "Cache miss", "Race condition"),
        // API Design (mở rộng)
        Q("API Design",
            "Phương thức HTTP nào dùng để cập nhật toàn bộ một tài nguyên đã tồn tại?",
            "GET", "PUT", "POST", "OPTIONS", "PUT"),
        Q("API Design",
            "Mã trạng thái nào cho biết yêu cầu thành công và đã tạo tài nguyên mới?",
            "200", "201", "204", "400", "201"),
        Q("API Design",
            "Định dạng dữ liệu nào phổ biến nhất cho REST API hiện nay?",
            "XML", "JSON", "CSV", "YAML", "JSON"),
        // Databases (mở rộng)
        Q("Databases",
            "Câu lệnh SQL nào dùng để thay đổi dữ liệu của bản ghi đã tồn tại?",
            "INSERT", "UPDATE", "SELECT", "CREATE", "UPDATE"),
        Q("Databases",
            "Quá trình tổ chức dữ liệu để giảm dư thừa trong CSDL quan hệ gọi là gì?",
            "Indexing", "Normalization", "Sharding", "Caching", "Normalization"),
        Q("Databases",
            "Thành phần nào giúp tăng tốc độ truy vấn bằng cách tra cứu nhanh theo cột?",
            "View", "Index", "Trigger", "Schema", "Index"),
        // Authentication (mở rộng)
        Q("Authentication",
            "Sự khác biệt cốt lõi: Authentication xác định _____, còn Authorization xác định _____.",
            "quyền / danh tính", "danh tính / quyền", "mật khẩu / email", "token / cookie",
            "danh tính / quyền"),
        Q("Authentication",
            "Cơ chế nào thêm một lớp bảo mật ngoài mật khẩu (ví dụ mã OTP)?",
            "SQL injection", "Multi-factor authentication", "Caching", "Load balancing",
            "Multi-factor authentication"),
        Q("Authentication",
            "Giá trị ngẫu nhiên thêm vào mật khẩu trước khi băm để chống tấn công bảng tra gọi là gì?",
            "token", "salt", "cookie", "nonce", "salt"),
        // Caching (mở rộng)
        Q("Caching",
            "Khi dữ liệu cần không có trong cache, ta gọi đó là gì?",
            "Cache hit", "Cache miss", "Cache flush", "Cache warm", "Cache miss"),
        Q("Caching",
            "Khoảng thời gian một mục cache còn hiệu lực thường được điều khiển bởi gì?",
            "TTL (time to live)", "TCP", "TLS", "DNS", "TTL (time to live)"),
        // Concurrency (mở rộng)
        Q("Concurrency",
            "Tình trạng hai tiến trình chờ lẫn nhau mãi mãi và không tiến triển gọi là gì?",
            "Race condition", "Deadlock", "Starvation", "Timeout", "Deadlock"),
        Q("Concurrency",
            "Cơ chế nào bảo đảm chỉ một luồng truy cập vùng tới hạn tại một thời điểm?",
            "Lock/Mutex", "Cache", "Index", "Cookie", "Lock/Mutex"),
        // API Design (mở rộng thêm)
        Q("API Design",
            "Mã trạng thái HTTP nào cho biết người dùng chưa xác thực?",
            "400", "401", "403", "404", "401"),
    };
}
