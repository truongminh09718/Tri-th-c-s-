# Demo Runbook - AI Learning Path

## 1. Che do demo khuyen nghi

Co hai che do demo:

1. **Reliable fallback demo**: de `Gemini:Endpoint`, `Gemini:ApiKey`, `MlService:Endpoint` trong `appsettings*.json` rong. App dung deterministic fallback, luong end-to-end on dinh, khong phu thuoc mang.
2. **AI/ML live demo**: cau hinh Gemini va ML service that qua environment variables/user-secrets. Neu service loi, resilient wrapper fallback de demo khong gay.

Khong commit API key vao repo.

## 2. Chay backend

```powershell
dotnet run --project AiLearningPath/src/Api/AiLearningPath.Api.csproj
```

Sau khi chay, mo:

- Web UI: `https://localhost:<port>/`
- Swagger: `https://localhost:<port>/swagger`
- AI status: `https://localhost:<port>/api/system/ai-status`

`/api/system/ai-status` cho biet Gemini dang configured/fallback va ML service dang ready/unreachable/fallback.

## 3. Chay ML service

```powershell
cd AiLearningPath/ml-service
python -m venv .venv
.\\.venv\\Scripts\\Activate.ps1
pip install -r requirements.txt
uvicorn app:app --port 8001
```

Neu muon bat auth noi bo cho `/predict`:

```powershell
$env:ML_SERVICE_API_KEY="local-demo-key"
uvicorn app:app --port 8001
```

Khi bat auth, backend C# cung phai co:

```powershell
$env:MlService__Endpoint="http://localhost:8001"
$env:MlService__ApiKey="local-demo-key"
```

## 4. Smoke test ML

```powershell
Invoke-RestMethod http://localhost:8001/health
Invoke-RestMethod http://localhost:8001/ready
Invoke-RestMethod http://localhost:8001/version
Invoke-RestMethod http://localhost:8001/predict -Method Post -ContentType 'application/json' -Body '{"currentLevelScore":60,"hoursPerDay":2,"goalType":"GPA","targetDays":90}'
Invoke-RestMethod http://localhost:8001/predict -Method Post -ContentType 'application/json' -Body '{"currentLevelScore":60,"hoursPerDay":4,"goalType":"GPA","targetDays":90}'
```

Ky vong: probability nam trong `[0,1]` va muc `hoursPerDay=4` khong nho hon muc `hoursPerDay=2`.

Neu bat auth:

```powershell
Invoke-RestMethod http://localhost:8001/predict `
  -Method Post `
  -Headers @{ "X-ML-Service-Key" = "local-demo-key" } `
  -ContentType 'application/json' `
  -Body '{"currentLevelScore":60,"hoursPerDay":4,"goalType":"GPA","targetDays":90}'
```

## 5. Test backend

```powershell
dotnet test AiLearningPath/AiLearningPath.sln
```

Neu DLL bi lock do API dang chay, tat process API hoac dung output path rieng:

```powershell
dotnet test AiLearningPath/tests/AiLearningPath.Tests/AiLearningPath.Tests.csproj -p:BaseOutputPath=.codex-test-bin/
```

## 6. Demo fallback nhanh

### Gemini fallback

De `Gemini:Endpoint` hoac `Gemini:ApiKey` rong. Goi flow Assessment/Path/Career. Noi voi giam khao:

- Application service chi goi `IContentGenerator`.
- DI bind sang placeholder khi Gemini chua configured.
- Neu configured nhung loi, `ResilientContentGenerator` fallback.

### ML fallback

De `MlService:Endpoint` rong hoac cau hinh sai endpoint. Goi Academic Twin. Noi voi giam khao:

- Application service chi goi `IPredictionService`.
- Xac suat van trong `[0,1]`.
- Caller cancellation duoc propagate, internal timeout/transient loi moi fallback.

## 7. Production guard can nho

Khi `ASPNETCORE_ENVIRONMENT=Production`:

- JWT key khong duoc la placeholder va phai toi thieu 32 bytes.
- Neu bat `MlService:Endpoint`, phai cau hinh `MlService:ApiKey`.
- Gemini endpoint khong duoc chua `key=` trong URL; key gui qua `Gemini:ApiKey`.

## 8. Cau noi ngan khi trinh bay

"He thong nay khong chi goi AI truc tiep tu UI. AI/ML nam sau interface, co adapter that, deterministic fallback, readiness/status endpoint, auth noi bo cho ML, va property tests cho cac invariant nhu probability range va monotonicity."

