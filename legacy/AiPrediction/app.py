from flask import Flask, request, jsonify
from flask_cors import CORS
import pickle
import numpy as np

app = Flask(__name__)
CORS(app, resources={r"/api/*": {"origins": "*"}})

# 1. TAI CAC MO HINH LEN BO NHO
print("⏳ Đang nạp các mô hình AI...")
try:
    with open('models/model_gpa.pkl', 'rb') as f:
        model_gpa = pickle.load(f)
    with open('models/model_toeic.pkl', 'rb') as f:
        model_toeic = pickle.load(f)
    with open('models/model_twin_700.pkl', 'rb') as f:
        model_twin_700 = pickle.load(f)
    print("✅ Đã nạp mô hình thành công!")
except FileNotFoundError:
    print("❌ Lỗi: Không tìm thấy file mô hình. Hãy chạy file train.py trước!")

# 2. API 1: DU DOAN DIEM SO (PREDICTION)
@app.route('/api/predict', methods=['POST'])
def predict_scores():
    try:
        # Nhận dữ liệu JSON từ Frontend/Backend gửi lên
        data = request.json
        
        # Sắp xếp dữ liệu đúng thứ tự lúc huấn luyện
        features = np.array([[
            data['study_hours_per_day'],
            data['attendance_rate'],
            data['mock_test_score'],
            data['historical_gpa']
        ]])
        
        # Đưa vào AI dự đoán
        pred_gpa = model_gpa.predict(features)[0]
        pred_toeic = model_toeic.predict(features)[0]
        
        # Trả kết quả về dưới dạng JSON
        return jsonify({
            "status": "success",
            "predicted_gpa": round(float(pred_gpa), 2),
            "predicted_toeic": int(pred_toeic)
        })
    except Exception as e:
        return jsonify({"status": "error", "message": str(e)}), 400

# 3. API 2: MO PHONG KQUA (ACADEMIC TWIN)
@app.route('/api/twin-simulation', methods=['POST'])
def simulate_twin():
    try:
        data = request.json
        features = np.array([[
            data['study_hours_per_day'],
            data['attendance_rate'],
            data['mock_test_score'],
            data['historical_gpa']
        ]])
        
        # Lấy mảng xác suất: [Xác suất trượt (0), Xác suất đỗ (1)]
        probabilities = model_twin_700.predict_proba(features)[0]
        
        # Lấy xác suất đỗ (nhãn 1) và nhân 100 để ra phần trăm
        pass_probability = probabilities[1] * 100 
        
        return jsonify({
            "status": "success",
            "target": "TOEIC 700",
            "probability_success_percent": round(float(pass_probability), 1)
        })
    except Exception as e:
        return jsonify({"status": "error", "message": str(e)}), 400

@app.route('/')
def home():
    return app.send_static_file('index.html')

# KHOI CHAY SERVER (CHAY MAY TINH CA NHAN CONG 5000)
if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000, debug=False)