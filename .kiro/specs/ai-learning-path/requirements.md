# Requirements Document

## Introduction

AI Learning Path là một nền tảng web ứng dụng Trí tuệ nhân tạo (AI) hỗ trợ sinh viên xây dựng lộ trình học tập cá nhân hóa, đánh giá năng lực, theo dõi tiến độ và định hướng nghề nghiệp. Hệ thống hoạt động như một cố vấn học tập số (AI Learning Mentor), giúp sinh viên xác định điểm mạnh/yếu, xây lộ trình phù hợp, dự đoán kết quả và chuẩn bị cho sự nghiệp.

Tài liệu này mô tả các yêu cầu cho phạm vi MVP (Minimum Viable Product) phục vụ cuộc thi, tập trung vào các chức năng cốt lõi: Authentication & Profile, AI Assessment, Learning DNA Engine, AI Learning Path Generator, Progress Tracking Dashboard, AI Academic Twin và Career Path AI. Các chức năng mở rộng (Smart Study Scheduler, AI Tutor, Adaptive Learning System) được mô tả ở mức yêu cầu để đảm bảo định hướng kiến trúc nhất quán.

## Glossary

- **System**: Toàn bộ nền tảng web AI Learning Path bao gồm frontend, backend API và các dịch vụ AI/ML.
- **Auth_Service**: Thành phần chịu trách nhiệm đăng ký, đăng nhập và quản lý phiên xác thực bằng JWT.
- **Profile_Service**: Thành phần quản lý hồ sơ cá nhân của sinh viên.
- **Assessment_Engine**: Thành phần tạo và chấm bài kiểm tra đánh giá năng lực đầu vào, phân tích trình độ.
- **Learning_DNA_Engine**: Thành phần xây dựng hồ sơ học tập cá nhân (phong cách học, khung giờ hiệu quả, tốc độ tiếp thu, khả năng tập trung, điểm mạnh/yếu).
- **Path_Generator**: Thành phần tự động sinh lộ trình học tập cá nhân hóa theo tháng/tuần/ngày.
- **Study_Scheduler**: Thành phần sắp xếp và điều chỉnh lịch học dựa trên ràng buộc thời gian của sinh viên.
- **AI_Tutor**: Trợ lý học tập AI trả lời câu hỏi và đề xuất tài liệu.
- **Progress_Dashboard**: Thành phần hiển thị tiến độ học tập và các biểu đồ phân tích.
- **Adaptive_Engine**: Thành phần điều chỉnh lộ trình dựa trên dữ liệu tiến độ thực tế.
- **Academic_Twin**: Mô hình bản sao học tập số mô phỏng và dự đoán khả năng đạt mục tiêu theo thời lượng học.
- **Career_Path_AI**: Thành phần xây dựng lộ trình kỹ năng và đề xuất nghề nghiệp.
- **Student**: Người dùng cuối là sinh viên đã đăng ký tài khoản.
- **JWT**: JSON Web Token dùng để xác thực và phân quyền truy cập.
- **Learning_Goal**: Mục tiêu học tập do sinh viên chọn (ví dụ: IELTS, TOEIC, môn đại học, Frontend Development, Backend Development, Data Analyst, AI Engineer).
- **Learning_Score**: Chỉ số tổng hợp phản ánh hiệu quả và tiến độ học tập của sinh viên.
- **Learning_DNA_Profile**: Hồ sơ học tập cá nhân do Learning_DNA_Engine tạo ra.

## Requirements

### Requirement 1: Đăng ký tài khoản

**User Story:** Là một sinh viên, tôi muốn đăng ký tài khoản, để có thể truy cập nền tảng và lưu dữ liệu học tập cá nhân.

#### Acceptance Criteria

1. WHEN một sinh viên gửi yêu cầu đăng ký với email và mật khẩu hợp lệ, THE Auth_Service SHALL tạo tài khoản mới và lưu vào cơ sở dữ liệu.
2. IF email đăng ký đã tồn tại trong cơ sở dữ liệu, THEN THE Auth_Service SHALL từ chối yêu cầu và trả về thông báo lỗi cho biết email đã được sử dụng.
3. IF mật khẩu có độ dài nhỏ hơn 8 ký tự, THEN THE Auth_Service SHALL từ chối yêu cầu và trả về thông báo lỗi về yêu cầu độ dài mật khẩu.
4. WHEN một tài khoản được tạo thành công, THE Auth_Service SHALL lưu mật khẩu dưới dạng băm (hash) thay vì văn bản thuần.

### Requirement 2: Đăng nhập và xác thực JWT

**User Story:** Là một sinh viên, tôi muốn đăng nhập an toàn, để truy cập các chức năng cá nhân hóa của hệ thống.

#### Acceptance Criteria

1. WHEN một sinh viên gửi email và mật khẩu đúng, THE Auth_Service SHALL tạo một JWT và trả về cho sinh viên.
2. IF email hoặc mật khẩu không đúng, THEN THE Auth_Service SHALL từ chối đăng nhập và trả về thông báo lỗi xác thực.
3. WHEN một yêu cầu truy cập tài nguyên được bảo vệ kèm theo JWT hợp lệ, THE System SHALL cho phép truy cập tài nguyên đó.
4. IF một yêu cầu truy cập tài nguyên được bảo vệ kèm theo JWT không hợp lệ hoặc đã hết hạn, THEN THE System SHALL từ chối yêu cầu và trả về mã trạng thái 401.

### Requirement 3: Quản lý hồ sơ cá nhân

**User Story:** Là một sinh viên, tôi muốn quản lý hồ sơ cá nhân, để hệ thống cá nhân hóa lộ trình theo thông tin của tôi.

#### Acceptance Criteria

1. WHEN một sinh viên đã đăng nhập gửi thông tin hồ sơ gồm họ tên, mã sinh viên, ngành học, mục tiêu học tập, mục tiêu nghề nghiệp và thời gian học mỗi ngày, THE Profile_Service SHALL lưu thông tin hồ sơ liên kết với tài khoản của sinh viên đó.
2. WHEN một sinh viên đã đăng nhập yêu cầu xem hồ sơ, THE Profile_Service SHALL trả về thông tin hồ sơ hiện tại của sinh viên đó.
3. WHEN một sinh viên đã đăng nhập cập nhật một trường hồ sơ với giá trị hợp lệ, THE Profile_Service SHALL lưu giá trị mới và trả về hồ sơ đã cập nhật.
4. IF trường thời gian học mỗi ngày nhận giá trị nhỏ hơn 0 hoặc lớn hơn 24 giờ, THEN THE Profile_Service SHALL từ chối cập nhật và trả về thông báo lỗi về khoảng giá trị hợp lệ.

### Requirement 4: Lựa chọn mục tiêu học tập

**User Story:** Là một sinh viên, tôi muốn chọn mục tiêu học tập, để hệ thống xây dựng lộ trình phù hợp với mục tiêu đó.

#### Acceptance Criteria

1. THE System SHALL cung cấp danh sách Learning_Goal gồm IELTS, TOEIC, các môn đại học, Frontend Development, Backend Development, Data Analyst và AI Engineer.
2. WHEN một sinh viên chọn một Learning_Goal từ danh sách, THE System SHALL lưu lựa chọn đó liên kết với hồ sơ của sinh viên.
3. WHERE một Learning_Goal yêu cầu điểm số mục tiêu, THE System SHALL cho phép sinh viên nhập điểm số mục tiêu kèm theo lựa chọn.
4. IF một sinh viên gửi mục tiêu học tập không nằm trong danh sách Learning_Goal được hỗ trợ, THEN THE System SHALL từ chối lựa chọn và trả về thông báo lỗi.

### Requirement 5: AI Assessment — Đánh giá năng lực đầu vào

**User Story:** Là một sinh viên, tôi muốn làm bài kiểm tra đánh giá năng lực đầu vào, để hệ thống xác định trình độ hiện tại của tôi.

#### Acceptance Criteria

1. WHEN một sinh viên bắt đầu bài đánh giá cho một Learning_Goal đã chọn, THE Assessment_Engine SHALL tạo một bộ câu hỏi tương ứng với Learning_Goal đó.
2. WHEN một sinh viên nộp bài đánh giá đã hoàn thành, THE Assessment_Engine SHALL chấm bài và tạo kết quả gồm trình độ hiện tại, điểm mạnh và điểm yếu.
3. WHEN kết quả đánh giá được tạo, THE Assessment_Engine SHALL lưu kết quả liên kết với hồ sơ của sinh viên.
4. IF sinh viên nộp bài đánh giá còn câu hỏi chưa trả lời, THEN THE Assessment_Engine SHALL trả về thông báo yêu cầu hoàn thành tất cả câu hỏi trước khi nộp.

### Requirement 6: Learning DNA Engine — Hồ sơ học tập cá nhân

**User Story:** Là một sinh viên, tôi muốn hệ thống xây dựng hồ sơ học tập riêng của tôi, để lộ trình phản ánh đúng phong cách và đặc điểm học tập của tôi.

#### Acceptance Criteria

1. WHEN kết quả AI Assessment của một sinh viên được tạo, THE Learning_DNA_Engine SHALL tạo một Learning_DNA_Profile gồm phong cách học, khung giờ học hiệu quả, tốc độ tiếp thu, khả năng tập trung và điểm mạnh/yếu.
2. WHEN một Learning_DNA_Profile được tạo, THE Learning_DNA_Engine SHALL lưu hồ sơ đó liên kết với tài khoản của sinh viên.
3. WHEN dữ liệu tiến độ học tập mới của sinh viên được ghi nhận, THE Learning_DNA_Engine SHALL cập nhật Learning_DNA_Profile dựa trên dữ liệu mới.
4. WHEN một sinh viên yêu cầu xem Learning_DNA_Profile, THE Learning_DNA_Engine SHALL trả về hồ sơ hiện tại của sinh viên đó.

### Requirement 7: AI Learning Path Generator — Sinh lộ trình cá nhân hóa

**User Story:** Là một sinh viên, tôi muốn nhận một lộ trình học tập cá nhân hóa, để biết cần học gì theo từng giai đoạn nhằm đạt mục tiêu.

#### Acceptance Criteria

1. WHEN một sinh viên yêu cầu sinh lộ trình và đã có Learning_Goal, kết quả đánh giá và khoảng thời gian mục tiêu, THE Path_Generator SHALL tạo một lộ trình cá nhân hóa chia theo tháng, tuần và ngày.
2. WHEN một lộ trình được tạo, THE Path_Generator SHALL chia lộ trình thành các giai đoạn tương ứng với mục tiêu của sinh viên.
3. WHEN một lộ trình được tạo, THE Path_Generator SHALL lưu lộ trình liên kết với hồ sơ của sinh viên.
4. IF một sinh viên yêu cầu sinh lộ trình khi chưa hoàn thành AI Assessment, THEN THE Path_Generator SHALL trả về thông báo yêu cầu hoàn thành bài đánh giá trước.
5. IF khoảng thời gian mục tiêu không đủ để hoàn thành lộ trình theo ước lượng của hệ thống, THEN THE Path_Generator SHALL tạo lộ trình kèm cảnh báo về tính khả thi của khoảng thời gian.

### Requirement 8: Progress Tracking Dashboard — Theo dõi tiến độ

**User Story:** Là một sinh viên, tôi muốn theo dõi tiến độ học tập của mình, để biết mình đang ở đâu so với mục tiêu.

#### Acceptance Criteria

1. WHEN một sinh viên mở dashboard, THE Progress_Dashboard SHALL hiển thị Learning_Score, tỷ lệ hoàn thành kế hoạch, tổng số giờ học và tiến độ theo tuần và tháng.
2. WHEN một sinh viên đánh dấu hoàn thành một nhiệm vụ học tập, THE Progress_Dashboard SHALL cập nhật tỷ lệ hoàn thành kế hoạch và Learning_Score.
3. WHEN một sinh viên mở dashboard, THE Progress_Dashboard SHALL hiển thị biểu đồ phân tích tiến độ học tập.
4. WHERE một sinh viên chưa có dữ liệu học tập, THE Progress_Dashboard SHALL hiển thị trạng thái rỗng kèm hướng dẫn bắt đầu học.

### Requirement 9: AI Academic Twin — Mô phỏng và dự đoán kết quả

**User Story:** Là một sinh viên, tôi muốn xem dự đoán khả năng đạt mục tiêu theo thời lượng học khác nhau, để quyết định mức đầu tư thời gian phù hợp.

#### Acceptance Criteria

1. WHEN một sinh viên yêu cầu mô phỏng kết quả với một thời lượng học mỗi ngày, THE Academic_Twin SHALL trả về xác suất đạt mục tiêu ước lượng tương ứng với thời lượng đó.
2. WHEN một sinh viên cung cấp nhiều mức thời lượng học mỗi ngày, THE Academic_Twin SHALL trả về xác suất đạt mục tiêu tương ứng cho từng mức.
3. WHEN dữ liệu tiến độ học tập của sinh viên được cập nhật, THE Academic_Twin SHALL tính lại dự đoán dựa trên dữ liệu mới nhất.
4. IF một sinh viên yêu cầu mô phỏng khi chưa có Learning_Goal hoặc chưa hoàn thành AI Assessment, THEN THE Academic_Twin SHALL trả về thông báo yêu cầu hoàn thành các bước tiên quyết.

### Requirement 10: Career Path AI — Định hướng nghề nghiệp

**User Story:** Là một sinh viên, tôi muốn nhận lộ trình kỹ năng nghề nghiệp, để chuẩn bị cho công việc tương lai.

#### Acceptance Criteria

1. THE Career_Path_AI SHALL cung cấp danh sách nghề nghiệp gồm Frontend, Backend, Data Analyst, AI Engineer và Tester.
2. WHEN một sinh viên chọn một nghề nghiệp từ danh sách, THE Career_Path_AI SHALL tạo một lộ trình kỹ năng tương ứng với nghề nghiệp đó.
3. WHEN một lộ trình nghề nghiệp được tạo, THE Career_Path_AI SHALL đề xuất các chứng chỉ liên quan và các dự án thực tế gợi ý.
4. WHEN một lộ trình nghề nghiệp được tạo, THE Career_Path_AI SHALL lưu lộ trình liên kết với hồ sơ của sinh viên.

### Requirement 11: Smart Study Scheduler — Sắp xếp lịch học

**User Story:** Là một sinh viên, tôi muốn hệ thống sắp xếp lịch học tự động, để cân bằng giữa lộ trình và các ràng buộc thời gian của tôi.

#### Acceptance Criteria

1. WHEN một sinh viên cung cấp lịch học đại học, lịch thi và các deadline, THE Study_Scheduler SHALL tạo lịch học sắp xếp các nhiệm vụ của lộ trình quanh các ràng buộc đó.
2. IF một sinh viên bỏ lỡ một nhiệm vụ học tập đã lên lịch, THEN THE Study_Scheduler SHALL sắp xếp lại các nhiệm vụ còn lại để duy trì mục tiêu.
3. WHEN lịch học được tạo hoặc cập nhật, THE Study_Scheduler SHALL lưu lịch liên kết với hồ sơ của sinh viên.

### Requirement 12: Adaptive Learning System — Điều chỉnh lộ trình

**User Story:** Là một sinh viên, tôi muốn lộ trình tự điều chỉnh theo tiến độ thực tế, để tập trung vào các phần tôi còn yếu.

#### Acceptance Criteria

1. WHEN dữ liệu tiến độ cho thấy một kỹ năng tiến bộ chậm hơn ngưỡng dự kiến, THE Adaptive_Engine SHALL tăng số lượng bài tập cho kỹ năng đó trong lộ trình.
2. WHEN dữ liệu tiến độ cho thấy một kỹ năng đã đạt mục tiêu, THE Adaptive_Engine SHALL giảm khối lượng nội dung của kỹ năng đó trong lộ trình.
3. WHEN lộ trình được điều chỉnh, THE Adaptive_Engine SHALL lưu phiên bản lộ trình cập nhật liên kết với hồ sơ của sinh viên.

### Requirement 13: AI Tutor — Trợ lý học tập

**User Story:** Là một sinh viên, tôi muốn đặt câu hỏi cho trợ lý AI, để được giải thích kiến thức và gợi ý tài liệu bất cứ lúc nào.

#### Acceptance Criteria

1. WHEN một sinh viên đã đăng nhập gửi một câu hỏi học tập, THE AI_Tutor SHALL trả về câu trả lời giải thích liên quan đến câu hỏi.
2. WHEN một sinh viên yêu cầu tài liệu cho một chủ đề, THE AI_Tutor SHALL đề xuất danh sách tài liệu liên quan đến chủ đề đó.
3. IF dịch vụ AI bên ngoài không phản hồi trong thời gian chờ tối đa, THEN THE AI_Tutor SHALL trả về thông báo lỗi và đề nghị sinh viên thử lại.

### Requirement 14: Bảo mật và phân quyền dữ liệu

**User Story:** Là một sinh viên, tôi muốn dữ liệu cá nhân của tôi được bảo vệ, để chỉ tôi mới truy cập được dữ liệu học tập của mình.

#### Acceptance Criteria

1. WHEN một sinh viên yêu cầu truy cập dữ liệu học tập, THE System SHALL chỉ trả về dữ liệu thuộc về tài khoản đã xác thực của sinh viên đó.
2. IF một sinh viên đã xác thực yêu cầu truy cập dữ liệu thuộc về tài khoản khác, THEN THE System SHALL từ chối yêu cầu và trả về mã trạng thái 403.
3. WHEN một phiên đăng nhập vượt quá thời gian hết hạn của JWT, THE System SHALL yêu cầu sinh viên xác thực lại trước khi truy cập tài nguyên được bảo vệ.
