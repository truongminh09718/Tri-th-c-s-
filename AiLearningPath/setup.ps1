$ErrorActionPreference = 'Stop'
$dotnet = 'C:\Program Files\dotnet\dotnet.exe'

function Run($args) {
    Write-Output (">> dotnet " + ($args -join ' '))
    & $dotnet @args 2>&1 | ForEach-Object { Write-Output $_ }
    if ($LASTEXITCODE -ne 0) { throw ("dotnet failed with exit code " + $LASTEXITCODE) }
}

# PBT library for the xUnit test project
Run @('add','tests/AiLearningPath.Tests/AiLearningPath.Tests.csproj','package','FsCheck.Xunit','--version','2.16.6')

# EF Core + SQL Server in Infrastructure
Run @('add','src/Infrastructure/AiLearningPath.Infrastructure.csproj','package','Microsoft.EntityFrameworkCore.SqlServer','--version','8.0.8')
Run @('add','src/Infrastructure/AiLearningPath.Infrastructure.csproj','package','Microsoft.EntityFrameworkCore.Design','--version','8.0.8')

# JWT bearer authentication in Api
Run @('add','src/Api/AiLearningPath.Api.csproj','package','Microsoft.AspNetCore.Authentication.JwtBearer','--version','8.0.8')

Write-Output "PACKAGES_DONE"
