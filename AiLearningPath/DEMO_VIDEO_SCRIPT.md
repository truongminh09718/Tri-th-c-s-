# Kịch bản video demo - Tri Thức Số

## 1. Mở đầu (30-45 giây)

**Lời thuyết minh:**

"Tri Thức Số là nền tảng học tập cá nhân hóa ứng dụng trí tuệ nhân tạo. Hệ thống đánh giá năng lực đầu vào, xây dựng hồ sơ học tập, tạo lộ trình, tổ chức bài học hằng ngày và dự đoán khả năng đạt mục tiêu của từng người học."

**Thao tác:**

- Mở trang chủ.
- Lướt qua phần giới thiệu và các tính năng nổi bật.
- Chuyển thử giao diện sáng/tối.

## 2. Đăng ký và đăng nhập (30 giây)

**Công dụng:**

- Tạo tài khoản mới bằng email và mật khẩu.
- Xác thực người dùng bằng JWT.
- Bảo vệ dữ liệu để mỗi người chỉ truy cập được hồ sơ và lộ trình của mình.

**Lời thuyết minh:**

"Người dùng chưa có tài khoản có thể chuyển sang đăng ký. Sau khi tạo tài khoản, hệ thống tự đăng nhập và lưu phiên xác thực an toàn."

## 3. Hồ sơ cá nhân (45 giây)

**Công dụng:**

- Lưu họ tên, mã sinh viên, ngành học và mục tiêu nghề nghiệp.
- Chọn mục tiêu học tập như IELTS, TOEIC, Frontend, Backend, Data Analyst hoặc AI Engineer.
- Khai báo số giờ học mỗi ngày và điểm số mục tiêu.
- Cung cấp dữ liệu nền để cá nhân hóa toàn bộ hệ thống.

**Lời thuyết minh:**

"Hồ sơ là đầu vào quan trọng. Mục tiêu, thời gian học và điểm mong muốn sẽ ảnh hưởng trực tiếp đến bài đánh giá, lộ trình và dự đoán Academic Twin."

## 4. Đánh giá năng lực (1-2 phút)

**Công dụng:**

- Tạo 20 câu hỏi theo đúng mục tiêu đã chọn.
- Đánh giá nhiều nhóm kỹ năng thay vì chỉ tính một điểm tổng.
- Chấm điểm tự động và xác định trình độ.
- Phân tích điểm mạnh, điểm yếu và độ chính xác theo từng kỹ năng.
- Tự động tạo lại Learning DNA sau khi nộp bài.

**Thao tác:**

- Chọn **Đánh giá năng lực**.
- Bấm **Bắt đầu bài đánh giá**.
- Trả lời một vài câu để minh họa.
- Có thể dùng tài khoản đã hoàn thành sẵn để tiết kiệm thời gian video.

**Lời thuyết minh:**

"Ngân hàng câu hỏi thay đổi theo mục tiêu. Ví dụ IELTS gồm Grammar, Vocabulary, Reading, Listening và Writing. Kết quả không chỉ cho biết điểm số mà còn tìm ra kỹ năng người học cần ưu tiên."

## 5. Learning DNA (45 giây)

**Công dụng:**

- Biểu diễn đặc điểm học tập riêng của người dùng.
- Tổng hợp phong cách học, tốc độ học, khả năng tập trung và giờ học hiệu quả.
- Lưu điểm mạnh và kỹ năng cần cải thiện.
- Làm đầu vào cho AI Tutor, lộ trình và hệ thống thích ứng.

**Lời thuyết minh:**

"Learning DNA có thể hiểu là hồ sơ học tập số. Hai người cùng học IELTS nhưng có điểm yếu và khả năng tập trung khác nhau sẽ nhận được định hướng khác nhau."

## 6. Lộ trình học cá nhân hóa (1 phút)

**Công dụng:**

- Sinh lộ trình dựa trên mục tiêu, kết quả đánh giá và Learning DNA.
- Chia kế hoạch thành tháng, tuần và từng nhiệm vụ.
- Gắn kỹ năng, nội dung và thời lượng dự kiến cho mỗi nhiệm vụ.
- Cảnh báo nếu thời gian mục tiêu không khả thi so với số giờ học mỗi ngày.

**Thao tác:**

- Chọn số ngày mục tiêu.
- Bấm **Sinh lộ trình học**.
- Mở một giai đoạn để giới thiệu các nhiệm vụ.

**Lời thuyết minh:**

"Hệ thống không chỉ tạo danh sách nội dung mà còn kiểm tra tính khả thi. Nếu khối lượng học lớn hơn thời gian người dùng có thể dành ra, hệ thống sẽ đưa ra cảnh báo."

## 7. Bắt đầu học (2 phút - phần nên nhấn mạnh)

**Công dụng:**

- Chọn nhiệm vụ chưa hoàn thành từ lộ trình mới nhất.
- Hiển thị bài học hôm nay và các môn tiếp theo.
- Cho phép bấm **Mở bài [tên môn]** để đổi bài học.
- Tải mục tiêu, tài liệu và bộ 10 câu hỏi riêng của từng môn.
- Chấm đúng/sai và giải thích ngay sau mỗi câu.
- Hiển thị tiến độ làm quiz từ `0/10` đến `10/10`.
- Tích hợp đồng hồ Pomodoro 25 phút, tạm dừng và đặt lại.
- Nhận đánh giá 1-5 sao và ghi chú cuối buổi.
- Khi hoàn thành, cập nhật nhiệm vụ, thời gian học và Learning Score.

**Thao tác:**

- Mở **Bắt đầu học**.
- Bấm **Mở bài Grammar** hoặc một môn khác.
- Chỉ ra rằng tiêu đề, tài liệu và 10 câu hỏi đều thay đổi theo môn.
- Trả lời một câu để hiển thị giải thích và bộ đếm `1/10`.
- Bấm thử Pomodoro.

**Lời thuyết minh:**

"Đây là không gian học tập chính. Người học có thể chuyển giữa các nhiệm vụ, đọc tài liệu, làm 10 câu kiểm tra và tập trung bằng Pomodoro. Hệ thống chỉ cho hoàn thành khi đã trả lời đủ câu hỏi và gửi đánh giá cuối buổi."

## 8. Tiến độ học tập (45 giây)

**Công dụng:**

- Tổng hợp tỷ lệ hoàn thành nhiệm vụ.
- Tính tổng số giờ học từ các phiên học.
- Tính Learning Score từ tiến độ, kết quả đánh giá và mức độ học đều đặn.
- Hiển thị biểu đồ thay đổi theo tuần.

**Lời thuyết minh:**

"Sau mỗi bài học, dashboard được cập nhật tự động. Learning Score không chỉ dựa vào điểm bài kiểm tra mà còn tính tỷ lệ hoàn thành và tính đều đặn của việc học."

## 9. AI Coach và AI Tutor (1 phút)

**Công dụng:**

- Hiển thị trạng thái Gemini, ML Service và bộ nhớ đệm AI.
- Sinh insight về rủi ro, kỹ năng yếu và khuyến nghị tuần tới.
- Cho phép đặt câu hỏi cho AI Tutor theo ngữ cảnh hồ sơ và lộ trình.
- Lưu hội thoại và nhận phản hồi hữu ích/chưa hữu ích.
- Có fallback xác định để demo vẫn chạy khi dịch vụ AI bên ngoài không khả dụng.

**Lời thuyết minh:**

"AI không trả lời chung chung mà nhận ngữ cảnh từ hồ sơ, kết quả đánh giá, Learning DNA và các nhiệm vụ hiện tại. Hệ thống cũng có cache, timeout và fallback để bảo đảm tính ổn định."

## 10. Smart Scheduler (45 giây)

**Công dụng:**

- Tạo lịch học từ các nhiệm vụ chưa hoàn thành.
- Nhận số giờ học mỗi tuần, deadline và khung giờ ưu tiên.
- Phân bổ nhiệm vụ thành các phiên học cụ thể.
- Trả về lý do sắp xếp và cho biết AI hay fallback đã được sử dụng.

**Lời thuyết minh:**

"Smart Scheduler biến lộ trình dài hạn thành lịch học có thể thực hiện trong đời sống hằng ngày, dựa trên thời gian thật của người dùng."

## 11. Adaptive Learning (45 giây)

**Công dụng:**

- Nhận kỹ năng yếu và tín hiệu tiến độ mới.
- Đề xuất bổ sung hoặc ưu tiên lại nhiệm vụ.
- Không ghi đè lộ trình gốc.
- Lưu Adaptation Event để có thể kiểm tra lịch sử thay đổi.

**Lời thuyết minh:**

"Lộ trình không cố định. Khi người học tiến bộ chậm hoặc xuất hiện kỹ năng yếu mới, Adaptive Learning đề xuất bản điều chỉnh nhưng vẫn giữ dữ liệu gốc để bảo đảm khả năng truy vết."

## 12. Academic Twin (1 phút)

**Công dụng:**

- Tạo bản sao học thuật số từ dữ liệu người học.
- Mô phỏng xác suất đạt mục tiêu theo số giờ học mỗi tuần.
- So sánh nhiều kịch bản thời lượng học.
- Sử dụng ML Service và có fallback khi service không kết nối được.

**Thao tác:**

- Nhập hai hoặc ba mức giờ học khác nhau.
- So sánh xác suất dự đoán.

**Lời thuyết minh:**

"Academic Twin giúp trả lời câu hỏi: nếu duy trì số giờ học này, khả năng đạt mục tiêu là bao nhiêu? Người học có thể thay đổi thời lượng để tìm kịch bản phù hợp."

## 13. Hướng nghiệp (45 giây)

**Công dụng:**

- Đề xuất lộ trình kỹ năng theo nghề nghiệp mục tiêu.
- Gợi ý kỹ năng cần học, chứng chỉ và dự án thực hành.
- Liên kết định hướng nghề nghiệp với quá trình học hiện tại.

**Lời thuyết minh:**

"Ngoài mục tiêu học ngắn hạn, hệ thống còn kết nối người học với định hướng nghề nghiệp thông qua kỹ năng, chứng chỉ và dự án nên thực hiện."

## 14. Điểm kỹ thuật nổi bật (45 giây)

**Lời thuyết minh:**

"Backend được xây dựng bằng ASP.NET Core, Entity Framework Core và SQLite trong môi trường demo. Hệ thống dùng JWT và phân quyền theo chủ sở hữu dữ liệu. Lớp AI Gateway quản lý provider, cache, timeout, log và fallback. ML Service được tách riêng bằng Python. Dự án có kiểm thử tự động cho domain, API, AI fallback và giao diện."

**Thông tin có thể đưa lên màn hình:**

- 120 test .NET đạt.
- 10 test frontend đạt.
- Có migration bảo toàn dữ liệu SQLite cũ.
- Responsive trên desktop và mobile.
- Hỗ trợ giao diện sáng/tối.

## 15. Kết luận (20-30 giây)

**Lời thuyết minh:**

"Tri Thức Số xây dựng một vòng lặp học tập hoàn chỉnh: hiểu người học, lập kế hoạch, tổ chức bài học, theo dõi tiến độ, thích ứng và dự đoán tương lai. Mục tiêu của dự án là giúp mỗi sinh viên biết mình đang ở đâu, nên học gì tiếp theo và cần đầu tư bao nhiêu thời gian để đạt mục tiêu."

## Thứ tự demo rút gọn 5 phút

1. Giới thiệu trang chủ và đăng nhập.
2. Mở hồ sơ đã chuẩn bị sẵn.
3. Giới thiệu kết quả đánh giá và Learning DNA.
4. Mở lộ trình học.
5. Trình diễn **Bắt đầu học**, chuyển môn và trả lời một câu quiz.
6. Mở dashboard tiến độ.
7. Tạo AI Insight hoặc hỏi AI Tutor.
8. Mô phỏng Academic Twin.
9. Kết luận.

## Lưu ý khi quay

- Dùng tài khoản đã có hồ sơ, kết quả đánh giá và lộ trình để tránh chờ lâu.
- Không hiển thị API key, mật khẩu hoặc file cấu hình bí mật.
- Phóng to trình duyệt khoảng 110-125% nếu chữ nhỏ.
- Chỉ trình diễn một đến hai thao tác tiêu biểu ở mỗi chức năng.
- Nhấn mạnh luồng dữ liệu liên kết giữa các chức năng, không chỉ liệt kê màn hình.
