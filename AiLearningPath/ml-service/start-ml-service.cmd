@echo off
REM ============================================================
REM  Tu dong chay ML Prediction Service (AI Learning Path).
REM  Self-contained: tu tao venv + cai dependencies neu thieu,
REM  roi chay uvicorn truc tiep (khong qua PowerShell).
REM ============================================================
cd /d "%~dp0"

set "VENV_PY=%~dp0.venv\Scripts\python.exe"

REM 1. Tao venv neu chua co.
if not exist "%VENV_PY%" (
    python -m venv ".venv"
)

REM 2. Cai dependencies neu uvicorn chua co trong venv.
"%VENV_PY%" -c "import uvicorn" 1>nul 2>nul
if errorlevel 1 (
    "%VENV_PY%" -m pip install --upgrade pip
    "%VENV_PY%" -m pip install -r "%~dp0requirements.txt"
)

REM 3. Chay service, ghi log ra file.
"%VENV_PY%" -m uvicorn app:app --host 127.0.0.1 --port 5000 >> "%~dp0ml-service.log" 2>&1
