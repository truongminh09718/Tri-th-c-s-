' Tu dong chay ML Prediction Service (AI Learning Path) khi dang nhap Windows.
' Goi thang PowerShell chay script start-ml-service.ps1, an hoan toan (cua so = 0).
Dim shell, scriptPath
scriptPath = "d:\Tri thức số\AiLearningPath\ml-service\start-ml-service.ps1"
Set shell = CreateObject("WScript.Shell")
shell.Run "powershell.exe -WindowStyle Hidden -ExecutionPolicy Bypass -File """ & scriptPath & """", 0, False
Set shell = Nothing
