# ML Prediction Service (AI Academic Twin)

Independent Python microservice (FastAPI + scikit-learn) for predicting the
probability that a learner reaches a goal. The C# backend calls this service
over HTTP; this service can also run by itself for local development.

## API

### `GET /health`

Public liveness check.

```json
{ "status": "ok" }
```

### `GET /ready`

Public readiness check. Verifies the scaler/model objects were loaded and are
usable for inference.

```json
{
  "status": "ready",
  "modelLoaded": true,
  "modelVersion": "deterministic-logreg-v1"
}
```

### `GET /version`

Public metadata endpoint with service and model information.

### `POST /predict`

Predicts the probability of reaching a learning goal. Response probability is
always clamped to `[0, 1]`.

```json
{
  "currentLevelScore": 65.0,
  "hoursPerDay": 2.5,
  "goalType": "IELTS",
  "targetDays": 90
}
```

```json
{ "probability": 0.73 }
```

## Auth

`/health`, `/ready`, and `/version` are public.

`/predict` uses simple internal API-key auth:

- If `ML_SERVICE_API_KEY` is unset, requests are allowed for local dev mode.
- If `ML_SERVICE_API_KEY` is set, callers must send the same value in the
  `X-ML-Service-Key` header.

Example:

```bash
set ML_SERVICE_API_KEY=dev-secret
curl -X POST http://localhost:8000/predict ^
  -H "Content-Type: application/json" ^
  -H "X-ML-Service-Key: dev-secret" ^
  -d "{\"currentLevelScore\":65,\"hoursPerDay\":2.5,\"goalType\":\"IELTS\",\"targetDays\":90}"
```

## Model

- Model: `LogisticRegression` from scikit-learn.
- Training runs once at module import/startup.
- Synthetic training data is deterministic using `numpy.random.default_rng(42)`.
- Features are standardized with `StandardScaler`.
- The learned `hoursPerDay` coefficient is asserted positive at startup, so
  predictions are monotonic non-decreasing as study hours increase while other
  fields stay fixed.

## Run Locally

Requires Python 3.10+.

```bash
python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
uvicorn app:app --port 8000
```

Useful URLs:

- Health: `http://localhost:8000/health`
- Ready: `http://localhost:8000/ready`
- Version: `http://localhost:8000/version`
- Swagger UI: `http://localhost:8000/docs`

## Smoke Test

```bash
pytest -q
```

The smoke test checks `/health`, `/ready`, `/predict` probability range,
monotonicity by `hoursPerDay`, and `/predict` API-key enforcement when
`ML_SERVICE_API_KEY` is configured.
