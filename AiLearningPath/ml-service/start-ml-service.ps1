<#
    Khởi động ML Prediction Service (FastAPI) cho AI Learning Path.

    Script này tự bảo đảm môi trường chạy được, kể cả sau khi khởi động lại máy:
      1. Tạo virtualenv (.venv) nếu chưa có.
      2. Cài dependencies từ requirements.txt nếu thiếu (kiểm tra nhanh bằng uvicorn).
      3. Chạy uvicorn phục vụ app:app tại 127.0.0.1:5000.

    Dùng trực tiếp:
        powershell -ExecutionPolicy Bypass -File start-ml-service.ps1

    Được Scheduled Task "AiLearningPath-MLService" gọi tự động khi đăng nhập Windows.
#>

$ErrorActionPreference = "Stop"

# Thư mục chứa script này (ml-service), bảo đảm chạy đúng nơi dù gọi từ đâu.
$serviceDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $serviceDir

$venvPython = Join-Path $serviceDir ".venv\Scripts\python.exe"

# 1. Tạo venv nếu chưa có.
if (-not (Test-Path $venvPython)) {
    Write-Host "[ML] Creating virtual environment..."
    python -m venv .venv
}

# 2. Cài dependencies nếu uvicorn chưa có trong venv.
& $venvPython -c "import uvicorn" 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ML] Installing dependencies..."
    & $venvPython -m pip install --upgrade pip -q
    & $venvPython -m pip install -r (Join-Path $serviceDir "requirements.txt") -q
}

# 3. Chạy service.
Write-Host "[ML] Starting ML service on http://127.0.0.1:5000 ..."
# uvicorn ghi log (INFO) ra stderr; với ErrorActionPreference=Stop, PowerShell sẽ coi đó là
# lỗi nghiêm trọng và dừng tiến trình khi chạy ẩn. Chuyển về Continue và ghi log ra file.
$ErrorActionPreference = "Continue"
$logFile = Join-Path $serviceDir "ml-service.log"
& $venvPython -m uvicorn app:app --host 127.0.0.1 --port 5000 *>> $logFile
