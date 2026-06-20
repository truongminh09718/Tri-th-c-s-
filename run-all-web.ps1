$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$apiDir = Join-Path $root 'AiLearningPath\src\Api'
$flaskDir = Join-Path $root 'AiPrediction'
$venvPython = Join-Path $root '.venv\Scripts\python.exe'

Write-Host "Starting AiLearningPath web app..."
Start-Process -FilePath 'dotnet' -ArgumentList 'run' -WorkingDirectory $apiDir

Start-Sleep -Seconds 2

Write-Host "Starting AiPrediction Flask app..."
if (Test-Path $venvPython) {
    Start-Process -FilePath $venvPython -ArgumentList 'app.py' -WorkingDirectory $flaskDir
} else {
    Start-Process -FilePath 'python' -ArgumentList 'app.py' -WorkingDirectory $flaskDir
}

Start-Sleep -Seconds 4

Write-Host "Opening the main website in one browser tab..."
Start-Process 'http://localhost:5125'

Write-Host "Done. Bạn có thể truy cập trực tiếp:"
Write-Host "  http://localhost:5125"
Write-Host "Nếu muốn truy cập trang dự đoán riêng của AiPrediction, dùng: http://127.0.0.1:5000"