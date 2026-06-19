from fastapi.testclient import TestClient

from app import app


client = TestClient(app)


def _predict(hours_per_day: float) -> float:
    response = client.post(
        "/predict",
        json={
            "currentLevelScore": 65.0,
            "hoursPerDay": hours_per_day,
            "goalType": "IELTS",
            "targetDays": 90,
        },
    )
    assert response.status_code == 200
    probability = response.json()["probability"]
    assert 0.0 <= probability <= 1.0
    return probability


def test_health_ready_predict_probability_range_and_monotonicity(monkeypatch):
    monkeypatch.delenv("ML_SERVICE_API_KEY", raising=False)

    health_response = client.get("/health")
    assert health_response.status_code == 200
    assert health_response.json() == {"status": "ok"}

    ready_response = client.get("/ready")
    assert ready_response.status_code == 200
    ready_payload = ready_response.json()
    assert ready_payload["status"] == "ready"
    assert ready_payload["modelLoaded"] is True

    low_hours_probability = _predict(1.0)
    high_hours_probability = _predict(4.0)
    assert high_hours_probability >= low_hours_probability


def test_predict_requires_api_key_when_configured(monkeypatch):
    monkeypatch.setenv("ML_SERVICE_API_KEY", "test-secret")

    unauthorized_response = client.post(
        "/predict",
        json={
            "currentLevelScore": 65.0,
            "hoursPerDay": 2.0,
            "goalType": "IELTS",
            "targetDays": 90,
        },
    )
    assert unauthorized_response.status_code == 401

    authorized_response = client.post(
        "/predict",
        headers={"X-ML-Service-Key": "test-secret"},
        json={
            "currentLevelScore": 65.0,
            "hoursPerDay": 2.0,
            "goalType": "IELTS",
            "targetDays": 90,
        },
    )
    assert authorized_response.status_code == 200


def test_predict_batch_probability_range_and_monotonicity(monkeypatch):
    monkeypatch.delenv("ML_SERVICE_API_KEY", raising=False)

    response = client.post(
        "/predict-batch",
        json={
            "items": [
                {
                    "currentLevelScore": 65.0,
                    "hoursPerDay": 1.0,
                    "goalType": "IELTS",
                    "targetDays": 90,
                },
                {
                    "currentLevelScore": 65.0,
                    "hoursPerDay": 4.0,
                    "goalType": "IELTS",
                    "targetDays": 90,
                },
            ]
        },
    )

    assert response.status_code == 200
    probabilities = response.json()["probabilities"]
    assert len(probabilities) == 2
    assert all(0.0 <= p <= 1.0 for p in probabilities)
    assert probabilities[1] >= probabilities[0]
