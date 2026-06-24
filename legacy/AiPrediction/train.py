import pandas as pd
import numpy as np
import pickle
import os
from sklearn.model_selection import train_test_split
from sklearn.ensemble import RandomForestRegressor, RandomForestClassifier
from sklearn.metrics import mean_absolute_error, accuracy_score

# Tao tu dong thu muc Models (khi chua ton tai)
if not os.path.exists('models'):
    os.makedirs('models')

# 1. Doc du lieu
print("⏳ Đang nạp dữ liệu từ 'data/student_training_data.csv'...")
df = pd.read_csv('data/student_training_data.csv')
X = df[['study_hours_per_day', 'attendance_rate', 'mock_test_score', 'historical_gpa']]

# 2. Huan luyen AI Prediction (Dự đoán GPA và TOEIC)
print("\n🚀 ĐANG HUẤN LUYỆN AI PREDICTION...")
y_gpa = df['final_gpa']
y_toeic = df['final_toeic']

# Chia tap du lieu (80% hoc, 20% thi)
X_train_g, X_test_g, y_train_g, y_test_g = train_test_split(X, y_gpa, test_size=0.2, random_state=42)
X_train_t, X_test_t, y_train_t, y_test_t = train_test_split(X, y_toeic, test_size=0.2, random_state=42)

# Mo hinh du doan GPA
model_gpa = RandomForestRegressor(n_estimators=100, random_state=42)
model_gpa.fit(X_train_g, y_train_g)
mae_gpa = mean_absolute_error(y_test_g, model_gpa.predict(X_test_g))
print(f"✅ Sai số mô hình dự đoán GPA (MAE): {mae_gpa:.2f} điểm.")

# Mo hinh du doan TOEIC
model_toeic = RandomForestRegressor(n_estimators=100, random_state=42)
model_toeic.fit(X_train_t, y_train_t)
mae_toeic = mean_absolute_error(y_test_t, model_toeic.predict(X_test_t))
print(f"✅ Sai số mô hình dự đoán TOEIC (MAE): {mae_toeic:.1f} điểm.")

# 3. Huan luyen AI Academic Twin (Phan loai sinh vien dat TOEIC >= 700)
print("\n🧬 ĐANG HUẤN LUYỆN AI ACADEMIC TWIN...")
# Nhan: 1 (Dat >= 700) và 0 (Duoi 700)
y_twin_700 = (df['final_toeic'] >= 700).astype(int)
X_train_twin, X_test_twin, y_train_twin, y_test_twin = train_test_split(X, y_twin_700, test_size=0.2, random_state=42)

model_twin_700 = RandomForestClassifier(n_estimators=100, random_state=42)
model_twin_700.fit(X_train_twin, y_train_twin)
acc_twin = accuracy_score(y_test_twin, model_twin_700.predict(X_test_twin))
print(f"✅ Độ chính xác phân loại của mô hình (Accuracy): {acc_twin * 100:.1f}%")

# 4. Xuat mo hinh thanh file
print("\n💾 ĐANG LƯU MÔ HÌNH...")
with open('models/model_gpa.pkl', 'wb') as f:
    pickle.dump(model_gpa, f)
with open('models/model_toeic.pkl', 'wb') as f:
    pickle.dump(model_toeic, f)
with open('models/model_twin_700.pkl', 'wb') as f:
    pickle.dump(model_twin_700, f)

print("🎉 Hoàn tất! Đã lưu 3 file .pkl vào thư mục 'models/'.")