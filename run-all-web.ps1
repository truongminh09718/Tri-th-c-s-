<#
    Khởi động toàn bộ hệ thống web AI Learning Path theo đúng thứ tự:
      1. Start ML service chính thức (FastAPI, AiLearningPath/ml-service) trên cổng 5000.
      2. Chờ /health (hoặc /ready) trả OK trước khi tiếp tục.
      3. Start ứng dụng web .NET (dotnet run).
      4. Mở trình duyệt.

    Lưu ý: KHÔNG còn khởi động AiPrediction Flask cũ. ML service chính thức là
    AiLearningPath/ml-service (FastAPI), thống nhất chạy tại http://127.0.0.1:5000.
#>

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$apiDir = Join-Path $root 'AiLearningPath\src\Api'
$mlDir = Join-Path $root 'AiLearningPath\ml-service'
$mlScript = Join-Path $mlDir 'start-ml-service.ps1'
$mlPort = 5000
$mlBase = "http://127.0.0.1:$mlPort"

# --- 0. Kiểm tra xung đột cổng cho ML service ---
$portInUse = $false
try {
    $conn = Get-NetTCPConnection -LocalPort $mlPort -State Listen -ErrorAction SilentlyContinue
    if ($conn) { $portInUse = $true }
} catch { $portInUse = $false }

if ($portInUse) {
    Write-Warning "[ML] Cổng $mlPort đã có tiến trình khác đang lắng nghe."
    Write-Warning "[ML] Sẽ thử dùng dịch vụ đang chạy ở cổng này. Nếu đó không phải ML service chính thức, hãy dừng nó trước rồi chạy lại."
} else {
    # --- 1. Start ML service chính thức (FastAPI) ---
    Write-Host "[ML] Starting official ML service (FastAPI) on $mlBase ..."
    if (Test-Path $mlScript) {
        Start-Process -FilePath 'powershell' `
            -ArgumentList @('-ExecutionPolicy', 'Bypass', '-File', $mlScript) `
            -WorkingDirectory $mlDir
    } else {
        Write-Warning "[ML] Không tìm thấy $mlScript. Bỏ qua bước start ML; app vẫn chạy bằng dự đoán dự phòng."
    }
}

# --- 2. Chờ ML service sẵn sàng (/health) ---
Write-Host "[ML] Waiting for ML service readiness at $mlBase/health ..."
$mlReady = $false
for ($i = 0; $i -lt 30; $i++) {
    try {
        $resp = Invoke-WebRequest -Uri "$mlBase/health" -UseBasicParsing -TimeoutSec 2
        if ($resp.StatusCode -eq 200) { $mlReady = $true; break }
    } catch {
        Start-Sleep -Seconds 1
    }
}
if ($mlReady) {
    Write-Host "[ML] ML service is ready."
} else {
    Write-Warning "[ML] ML service chưa sẵn sàng sau 30s. App vẫn chạy bằng dự đoán dự phòng (fallback)."
}

# --- 3. Start ứng dụng web .NET ---
Write-Host "[WEB] Starting AiLearningPath web app (dotnet run)..."
Start-Process -FilePath 'dotnet' -ArgumentList 'run' -WorkingDirectory $apiDir

# Chờ web app phản hồi trước khi mở trình duyệt.
Write-Host "[WEB] Waiting for web app at http://localhost:5125 ..."
$webReady = $false
for ($i = 0; $i -lt 40; $i++) {
    try {
        $resp = Invoke-WebRequest -Uri "http://localhost:5125" -UseBasicParsing -TimeoutSec 2
        if ($resp.StatusCode -eq 200) { $webReady = $true; break }
    } catch {
        Start-Sleep -Seconds 1
    }
}

# --- 4. Mở trình duyệt ---
Write-Host "[WEB] Opening the main website..."
Start-Process 'http://localhost:5125'

Write-Host ""
Write-Host "Done. Truy cập:"
Write-Host "  Web app : http://localhost:5125"
Write-Host "  ML API  : $mlBase  (health: $mlBase/health, ready: $mlBase/ready)"
if (-not $webReady) {
    Write-Warning "[WEB] Web app có thể chưa kịp khởi động xong; hãy đợi thêm vài giây rồi tải lại trang."
}
