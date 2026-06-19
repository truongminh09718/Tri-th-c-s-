import pandas as pd
import numpy as np

# Thiết lập hạt giống ngẫu nhiên để dữ liệu không đổi sau mỗi lần chạy
np.random.seed(42)
n_samples = 1000

# Tạo ngẫu nhiên các tính năng đầu vào (Features)
study_hours = np.random.uniform(0.5, 6.0, n_samples)  # Học từ 0.5 đến 6 tiếng/ngày
attendance = np.random.uniform(0.6, 1.0, n_samples)   # Đi học từ 60% đến 100%
mock_test = np.random.randint(200, 600, n_samples)    # Điểm thi thử từ 200 đến 600
historical_gpa = np.random.uniform(2.0, 4.0, n_samples) # GPA cũ từ 2.0 đến 4.0

# Tạo công thức logic để tính đầu ra (Targets) + thêm một chút nhiễu (noise) ngẫu nhiên
# Dự đoán GPA
final_gpa = historical_gpa * 0.6 + (study_hours / 6.0) * 1.2 + (attendance * 0.4) + np.random.normal(0, 0.1, n_samples)
final_gpa = np.clip(final_gpa, 0.0, 4.0) # Giới hạn GPA tối đa là 4.0

# Dự đoán TOEIC
final_toeic = mock_test + (study_hours * 60) + (attendance * 100) + np.random.normal(0, 20, n_samples)
final_toeic = np.clip(final_toeic, 10, 990).astype(int) # Giới hạn điểm TOEIC

# Tạo DataFrame bằng Pandas
df = pd.DataFrame({
    'study_hours_per_day': study_hours,
    'attendance_rate': attendance,
    'mock_test_score': mock_test,
    'historical_gpa': historical_gpa,
    'final_gpa': final_gpa,
    'final_toeic': final_toeic
})

# Lưu vào thư mục data/
df.to_csv('data/student_training_data.csv', index=False)
print("Đã tạo thành công dữ liệu giả lập tại 'data/student_training_data.csv'!")