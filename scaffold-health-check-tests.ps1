<#
.SYNOPSIS
  Fase 4 (MVP vertical) - completa el proyecto backend/Api.Tests que
  necesita el test de specs/health-check/, lo agrega a la solucion, y
  corre build + test para validar el endpoint GET /health.
.DESCRIPTION
  Correr desde PowerShell, parado en la carpeta del repo:

      cd C:\proyectos\programacion_agentica
      git checkout feature/1-health-check
      .\scaffold-health-check-tests.ps1

  backend/Api.Tests/HealthEndpointTests.cs ya existe (lo escribio Claude).
  Este script solo genera el resto del proyecto de test (.csproj, paquetes)
  con las herramientas reales, porque este entorno no tiene dotnet/acceso
  a NuGet para hacerlo de forma confiable.
#>

$ErrorActionPreference = "Stop"

Set-Location -Path $PSScriptRoot

function Test-Command($name) {
    return [bool](Get-Command $name -ErrorAction SilentlyContinue)
}

if (-not (Test-Command "dotnet")) {
    Write-Error "No se encontro 'dotnet' en el PATH."
    exit 1
}

if (-not (Test-Path "backend\Api.Tests\HealthEndpointTests.cs")) {
    Write-Error "No encuentro backend\Api.Tests\HealthEndpointTests.cs - estas en la branch correcta? (feature/1-health-check)"
    exit 1
}

# ---------------------------------------------------------------------------
# 1. Crear el proyecto de test (si todavia no existe el .csproj)
# ---------------------------------------------------------------------------
$testCsproj = "backend\Api.Tests\Api.Tests.csproj"

if (Test-Path $testCsproj) {
    Write-Host "Api.Tests.csproj ya existe, salteo 'dotnet new xunit'." -ForegroundColor Yellow
} else {
    Write-Host "== Creando proyecto de test (dotnet new xunit) ==" -ForegroundColor Cyan
    dotnet new xunit -o backend/Api.Tests
    if ($LASTEXITCODE -ne 0) { Write-Error "Fallo 'dotnet new xunit'."; exit 1 }

    # El template trae un test de ejemplo que no necesitamos.
    $sampleTest = "backend\Api.Tests\UnitTest1.cs"
    if (Test-Path $sampleTest) {
        Remove-Item $sampleTest
        Write-Host "Elimine el test de ejemplo UnitTest1.cs" -ForegroundColor Yellow
    }
}

# ---------------------------------------------------------------------------
# 2. Referencias: proyecto Api + paquete Microsoft.AspNetCore.Mvc.Testing
# ---------------------------------------------------------------------------
Write-Host "`n== Agregando referencias ==" -ForegroundColor Cyan

dotnet add $testCsproj reference backend/Api.csproj
if ($LASTEXITCODE -ne 0) { Write-Error "Fallo agregar la referencia a Api.csproj."; exit 1 }

dotnet add $testCsproj package Microsoft.AspNetCore.Mvc.Testing
if ($LASTEXITCODE -ne 0) { Write-Error "Fallo agregar el paquete Microsoft.AspNetCore.Mvc.Testing."; exit 1 }

# ---------------------------------------------------------------------------
# 3. Agregar Api.Tests a la solucion (mismo detector de .sln/.slnx que
#    scaffold-project.ps1, por si el formato cambia entre versiones del SDK)
# ---------------------------------------------------------------------------
Write-Host "`n== Agregando Api.Tests a la solucion ==" -ForegroundColor Cyan

$slnFile = Get-ChildItem -Path $PSScriptRoot -Filter "*.sln*" -File | Select-Object -First 1
if (-not $slnFile) {
    Write-Error "No se encontro ningun archivo de solucion (.sln/.slnx) en la raiz."
    exit 1
}

dotnet sln $slnFile.FullName add $testCsproj
if ($LASTEXITCODE -ne 0) { Write-Error "Fallo agregar Api.Tests a la solucion."; exit 1 }

# ---------------------------------------------------------------------------
# 4. Restore (forzado, sin cache) + build + test
#    No usamos --no-restore en el build: si el restore anterior quedo
#    desactualizado (paso tipico al agregar un proyecto/paquete nuevo),
#    --no-restore compila contra ese estado viejo y tira "no se encontro
#    Xunit / WebApplicationFactory" aunque el .csproj este bien.
# ---------------------------------------------------------------------------
Write-Host "`n== Limpiando obj/bin para evitar restore en cache viejo ==" -ForegroundColor Cyan

Get-ChildItem -Path $PSScriptRoot -Recurse -Directory -Include "obj", "bin" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch "\node_modules\" } |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "`n== Restore (forzado) ==" -ForegroundColor Cyan

dotnet restore $slnFile.FullName --force --no-cache
if ($LASTEXITCODE -ne 0) { Write-Error "Fallo 'dotnet restore'."; exit 1 }

Write-Host "`n== Build ==" -ForegroundColor Cyan

dotnet build $slnFile.FullName --configuration Release
if ($LASTEXITCODE -ne 0) { Write-Error "Fallo 'dotnet build'."; exit 1 }

Write-Host "`n== Test ==" -ForegroundColor Cyan

dotnet test $slnFile.FullName --configuration Release
if ($LASTEXITCODE -ne 0) { Write-Error "Los tests fallaron. Revisa el output de arriba."; exit 1 }

Write-Host "`n== Listo: build y tests en verde ==" -ForegroundColor Green
Write-Host "No se hizo git add/commit todavia. Revisa 'git status' / 'git diff' y avisale a Claude para que lo commitee en la branch feature/1-health-check."
