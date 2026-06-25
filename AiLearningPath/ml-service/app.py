"""
ML Prediction microservice for AI Academic Twin (Requirements 18.1, 18.5).

A small FastAPI service that exposes a deterministic LogisticRegression model
predicting the probability of reaching a learning goal given study inputs.

Contract (matches the C# IPredictionService / MlPredictionService adapter):
  POST /predict
    request : {"currentLevelScore": float, "hoursPerDay": float,
               "goalType": str, "targetDays": int}
    response: {"probability": float}   # clamped to [0, 1]
  GET /health
    response: {"status": "ok"}
  GET /ready
    response: {"status": "ready", "modelLoaded": true}
  GET /version
    response: service/model metadata

Security:
  - GET /health, GET /ready, and GET /version are public.
  - POST /predict requires X-ML-Service-Key when ML_SERVICE_API_KEY is set.
    If ML_SERVICE_API_KEY is unset, /predict remains open for local dev mode.

Determinism & monotonicity guarantee:
  - Training data is generated with a fixed seed (numpy.random.default_rng(42)).
  - Labels are produced by a deterministic rule whose score increases with
    hoursPerDay, so the learned coefficient for hoursPerDay is positive.
  - A positive coefficient on the (positively-scaled) hoursPerDay feature makes
    the predicted probability MONOTONICALLY NON-DECREASING in hoursPerDay,
    holding the other features fixed.
"""

from __future__ import annotations

import os

import numpy as np
from fastapi import Depends, FastAPI, Header, HTTPException, status
from pydantic import BaseModel, Field
from sklearn.linear_model import LogisticRegression
from sklearn.preprocessing import StandardScaler

# ---------------------------------------------------------------------------
# Feature engineering helpers (deterministic, no I/O)
# ---------------------------------------------------------------------------

# Fixed, ordered catalog of supported goal types. Encoding is deterministic:
# the index of the goal type within this list. Unknown goals map to -1 (other).
GOAL_TYPES = ["GPA", "TOEIC", "IELTS", "TOEFL", "SAT", "HSK"]
SERVICE_VERSION = "1.1.0"
MODEL_VERSION = "deterministic-logreg-v1"


def encode_goal(goal_type: str) -> float:
    """Deterministically encode a goal type as a stable numeric code.

    Using a stable ordinal keeps the service simple and reproducible. The goal
    code intentionally carries a near-zero weight in label generation so it does
    not interfere with the hoursPerDay monotonicity guarantee.
    """
    if goal_type is None:
        return -1.0
    normalized = goal_type.strip().upper()
    for idx, name in enumerate(GOAL_TYPES):
        if name == normalized:
            return float(idx)
    return -1.0


def build_feature_vector(
    current_level_score: float,
    hours_per_day: float,
    goal_type: str,
    target_days: float,
) -> np.ndarray:
    """Assemble the raw (un-scaled) feature vector in a fixed column order.

    Column order MUST stay consistent between training and inference:
      [currentLevelScore, hoursPerDay, goalCode, targetDays]
    """
    return np.array(
        [
            float(current_level_score),
            float(hours_per_day),
            encode_goal(goal_type),
            float(target_days),
        ],
        dtype=float,
    )


# ---------------------------------------------------------------------------
# Deterministic synthetic training (runs once at import / startup)
# ---------------------------------------------------------------------------

# Index of the hoursPerDay column inside the feature vector. Used to assert the
# learned coefficient is positive so the monotonicity guarantee holds.
HOURS_COLUMN = 1


def _generate_training_data(n_samples: int = 2000):
    """Generate deterministic synthetic training data.

    The label rule gives hoursPerDay a strong positive influence so the fitted
    LogisticRegression coefficient on hoursPerDay is positive.
    """
    rng = np.random.default_rng(42)

    current_level = rng.uniform(0.0, 100.0, size=n_samples)
    hours_per_day = rng.uniform(0.0, 12.0, size=n_samples)
    goal_codes = rng.integers(0, len(GOAL_TYPES), size=n_samples).astype(float)
    target_days = rng.uniform(1.0, 365.0, size=n_samples)

    # Deterministic "achievement score": higher current level, more study hours,
    # and more available days all help. hoursPerDay has the dominant positive
    # weight. goalType carries negligible weight (keeps monotonicity clean).
    achievement = (
        0.04 * current_level
        + 0.80 * hours_per_day
        + 0.004 * target_days
        + 0.001 * goal_codes
        - 4.5  # bias so the decision boundary sits in a realistic region
    )

    # Deterministic threshold labelling (no random noise) keeps the relationship
    # between hoursPerDay and the label clean and strictly monotone.
    labels = (achievement > 0.0).astype(int)

    features = np.column_stack(
        [current_level, hours_per_day, goal_codes, target_days]
    )
    return features, labels


def _train_model():
    """Train the scaler + LogisticRegression once and return them.

    StandardScaler uses a positive scale (std dev), so scaling preserves the
    sign of the hoursPerDay relationship. Combined with a positive learned
    coefficient, the predicted probability is non-decreasing in hoursPerDay.
    """
    features, labels = _generate_training_data()

    scaler = StandardScaler()
    scaled = scaler.fit_transform(features)

    model = LogisticRegression(max_iter=1000)

    # Guard against the degenerate case where the deterministic rule produced a
    # single class; widen the bias if needed. With the parameters above both
    # classes are present, but this keeps startup robust.
    if len(np.unique(labels)) < 2:
        labels[0] = 0
        labels[-1] = 1

    model.fit(scaled, labels)

    # Sanity assertion: hoursPerDay coefficient must be positive so probability
    # is monotonically non-decreasing in hoursPerDay (Requirement 18.5).
    hours_coef = model.coef_[0][HOURS_COLUMN]
    assert hours_coef > 0, (
        f"hoursPerDay coefficient must be positive for the monotonicity "
        f"guarantee, got {hours_coef}"
    )

    return scaler, model


# Train a single time at module load (service startup).
_SCALER, _MODEL = _train_model()


def is_model_loaded() -> bool:
    """Return whether the trained model objects are usable for inference."""
    return (
        _SCALER is not None
        and _MODEL is not None
        and hasattr(_SCALER, "transform")
        and hasattr(_MODEL, "predict_proba")
    )


def predict_probability(
    current_level_score: float,
    hours_per_day: float,
    goal_type: str,
    target_days: float,
) -> float:
    """Return the clamped probability of reaching the goal in [0, 1]."""
    raw = build_feature_vector(
        current_level_score, hours_per_day, goal_type, target_days
    ).reshape(1, -1)
    scaled = _SCALER.transform(raw)
    probability = float(_MODEL.predict_proba(scaled)[0][1])
    # Clamp defensively; predict_proba already returns [0,1] but the contract
    # requires an explicit clamp.
    return max(0.0, min(1.0, probability))


# ---------------------------------------------------------------------------
# FastAPI application
# ---------------------------------------------------------------------------

app = FastAPI(title="AI Learning Path - ML Prediction Service", version=SERVICE_VERSION)


class PredictRequest(BaseModel):
    currentLevelScore: float = Field(..., ge=0.0, le=100.0)
    hoursPerDay: float = Field(..., ge=0.0)
    goalType: str
    targetDays: int = Field(..., gt=0)


class PredictResponse(BaseModel):
    probability: float


class PredictBatchRequest(BaseModel):
    items: list[PredictRequest] = Field(..., min_length=1)


class PredictBatchResponse(BaseModel):
    probabilities: list[float]


def require_predict_key(
    x_ml_service_key: str | None = Header(default=None, alias="X-ML-Service-Key"),
) -> None:
    expected_key = os.getenv("ML_SERVICE_API_KEY")
    if not expected_key:
        return

    if x_ml_service_key != expected_key:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Missing or invalid ML service API key",
        )


@app.get("/health")
def health() -> dict:
    return {"status": "ok"}


@app.get("/ready")
def ready() -> dict:
    model_loaded = is_model_loaded()
    if not model_loaded:
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail={"status": "not_ready", "modelLoaded": False},
        )

    return {
        "status": "ready",
        "modelLoaded": True,
        "modelVersion": MODEL_VERSION,
    }


@app.get("/version")
def version() -> dict:
    return {
        "service": "ai-learning-path-ml-service",
        "serviceVersion": SERVICE_VERSION,
        "modelVersion": MODEL_VERSION,
        "modelType": type(_MODEL).__name__ if _MODEL is not None else None,
        "goalTypes": GOAL_TYPES,
        "features": [
            "currentLevelScore",
            "hoursPerDay",
            "goalCode",
            "targetDays",
        ],
    }


@app.post("/predict", response_model=PredictResponse)
def predict(
    req: PredictRequest, _auth: None = Depends(require_predict_key)
) -> PredictResponse:
    probability = predict_probability(
        current_level_score=req.currentLevelScore,
        hours_per_day=req.hoursPerDay,
        goal_type=req.goalType,
        target_days=req.targetDays,
    )
    return PredictResponse(probability=probability)


@app.post("/predict-batch", response_model=PredictBatchResponse)
def predict_batch(
    req: PredictBatchRequest, _auth: None = Depends(require_predict_key)
) -> PredictBatchResponse:
    probabilities = [
        predict_probability(
            current_level_score=item.currentLevelScore,
            hours_per_day=item.hoursPerDay,
            goal_type=item.goalType,
            target_days=item.targetDays,
        )
        for item in req.items
    ]
    return PredictBatchResponse(probabilities=probabilities)


# ---------------------------------------------------------------------------
# GPA / TOEIC prediction endpoints (used by the "Dự đoán AI" web page)
# ---------------------------------------------------------------------------
#
# The landing page wwwroot/aiprediction.html posts study habits and expects
# GPA + TOEIC predictions, plus a TOEIC-700 success simulation. The C# proxy
# (AiPredictionController) forwards those calls to:
#   POST /api/predict           -> {"predicted_gpa": float, "predicted_toeic": int}
#   POST /api/twin-simulation   -> {"target": str, "probability_success_percent": float}
#
# Inputs (snake_case, matching the proxy payload):
#   study_hours_per_day : float   hours studied per day
#   attendance_rate     : float   attendance percentage (0..100)
#   mock_test_score     : float   mock test score (0..10 scale, e.g. IELTS-like)
#   historical_gpa      : float   current GPA (0..4 scale)
#
# The predictions are DETERMINISTIC closed-form estimates (no randomness), so
# the same inputs always yield the same output. Coefficients are positive on
# study hours, attendance, and mock score so more effort never lowers the
# predicted outcome (consistent with the monotonicity spirit of Requirement 18.5).

TOEIC_GOAL_SCORE = 700.0


class StudyHabitsRequest(BaseModel):
    study_hours_per_day: float = Field(default=0.0, ge=0.0)
    attendance_rate: float = Field(default=0.0, ge=0.0, le=100.0)
    mock_test_score: float = Field(default=0.0, ge=0.0)
    historical_gpa: float = Field(default=0.0, ge=0.0, le=4.0)


class GpaToeicResponse(BaseModel):
    predicted_gpa: float
    predicted_toeic: int


class TwinSimulationResponse(BaseModel):
    target: str
    probability_success_percent: float


def _predict_gpa(req: StudyHabitsRequest) -> float:
    """Deterministic GPA estimate on the 0..4 scale.

    Anchored on the student's historical GPA, then nudged up by study hours,
    attendance and mock performance. Clamped to the valid GPA range.
    """
    gpa = (
        0.55 * req.historical_gpa
        + 0.09 * min(req.study_hours_per_day, 8.0)
        + 0.010 * req.attendance_rate
        + 0.06 * min(req.mock_test_score, 10.0)
    )
    return round(max(0.0, min(4.0, gpa)), 2)


def _predict_toeic(req: StudyHabitsRequest) -> int:
    """Deterministic TOEIC estimate on the 10..990 scale.

    Mock test score (scaled to a TOEIC-like band) is the dominant signal, with
    positive contributions from study hours and attendance.
    """
    toeic = (
        250.0
        + 55.0 * min(req.mock_test_score, 10.0)
        + 22.0 * min(req.study_hours_per_day, 8.0)
        + 1.6 * req.attendance_rate
    )
    return int(round(max(10.0, min(990.0, toeic))))


@app.post("/api/predict", response_model=GpaToeicResponse)
def api_predict(req: StudyHabitsRequest) -> GpaToeicResponse:
    return GpaToeicResponse(
        predicted_gpa=_predict_gpa(req),
        predicted_toeic=_predict_toeic(req),
    )


@app.post("/api/twin-simulation", response_model=TwinSimulationResponse)
def api_twin_simulation(req: StudyHabitsRequest) -> TwinSimulationResponse:
    """Estimate the probability of reaching TOEIC 700.

    Uses a logistic curve over the gap between the predicted TOEIC score and the
    700 goal, so the probability rises smoothly as predicted performance improves
    and is always reported in the [0, 100] percent range.
    """
    predicted_toeic = _predict_toeic(req)
    gap = predicted_toeic - TOEIC_GOAL_SCORE
    probability = 1.0 / (1.0 + np.exp(-gap / 60.0))
    percent = round(max(0.0, min(1.0, float(probability))) * 100.0, 1)
    return TwinSimulationResponse(
        target="TOEIC 700",
        probability_success_percent=percent,
    )
