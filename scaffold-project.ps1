<#
.SYNOPSIS
  Fase 1 (extendida) - crea el esqueleto minimo de backend (.NET) y frontend
  (React + Vite + TS), y regenera el .gitignore combinando los templates
  oficiales de dotnet y de Vite con el que ya tenemos.
.DESCRIPTION
  Correr desde PowerShell, parado en la carpeta del repo (o donde vive este
  script):

      cd C:\proyectos\programacion_agentica
      .\scaffold-project.ps1

  Esto NO implementa ninguna feature real (Article I de la Constitution:
  "Implementation MUST NOT precede specification"). Solo crea el esqueleto
  de proyecto (csproj / package.json de arranque) para tener una base real
  sobre la que trabajar cuando haya una spec aprobada, y para poder generar
  un .gitignore preciso en vez de uno tipeado a mano.

  Requiere: .NET SDK y Node/npm instalados y en el PATH.
#>

$ErrorActionPreference = "Stop"

Set-Location -Path $PSScriptRoot

function Test-Command($name) {
    return [bool](Get-Command $name -ErrorAction SilentlyContinue)
}

Write-Host "== Verificando herramientas ==" -ForegroundColor Cyan

if (-not (Test-Command "dotnet")) {
    Write-Error "No se encontro 'dotnet' en el PATH. Instala el .NET SDK y volve a correr este script."
    exit 1
}
if (-not (Test-Command "npm")) {
    Write-Error "No se encontro 'npm' en el PATH. Instala Node.js y volve a correr este script."
    exit 1
}

Write-Host ("dotnet: {0}" -f (dotnet --version))
Write-Host ("node:   {0}" -f (node --version))
Write-Host ("npm:    {0}" -f (npm --version))

# ---------------------------------------------------------------------------
# 1. Backend (.NET) - solucion + Web API minima
# ---------------------------------------------------------------------------
Write-Host "`n== Backend (.NET) ==" -ForegroundColor Cyan

if (Test-Path "backend") {
    Write-Host "La carpeta 'backend' ya existe, salteo la creacion del proyecto." -ForegroundColor Yellow
} else {
    dotnet new webapi -n Api -o backend
    if ($LASTEXITCODE -ne 0) { Write-Error "Fallo 'dotnet new webapi'."; exit 1 }
}

# El SDK 10+ crea .slnx por default (antes era .sln) - buscamos el archivo real
# en vez de asumir la extension, para que esto funcione en cualquier version.
$slnFile = Get-ChildItem -Path $PSScriptRoot -Filter "*.sln*" -File | Select-Object -First 1
if (-not $slnFile) {
    dotnet new sln -n PVirtualSoftwareFactory -o .
    if ($LASTEXITCODE -ne 0) { Write-Error "Fallo 'dotnet new sln'."; exit 1 }
    $slnFile = Get-ChildItem -Path $PSScriptRoot -Filter "*.sln*" -File | Select-Object -First 1
}

if (-not $slnFile) {
    Write-Error "No se encontro el archivo de solucion generado (.sln/.slnx)."
    exit 1
}

# Idempotente: si el proyecto ya esta en la solucion, dotnet sln add no rompe nada.
dotnet sln $slnFile.FullName add backend/Api.csproj
if ($LASTEXITCODE -ne 0) { Write-Error "Fallo agregar el proyecto a la solucion."; exit 1 }

# ---------------------------------------------------------------------------
# 2. Frontend (React + Vite + TypeScript)
# ---------------------------------------------------------------------------
Write-Host "`n== Frontend (React + Vite + TS) ==" -ForegroundColor Cyan

if (Test-Path "frontend") {
    Write-Host "La carpeta 'frontend' ya existe, salteo la creacion del proyecto." -ForegroundColor Yellow
} else {
    npx --yes create-vite@latest frontend --template react-ts
    if ($LASTEXITCODE -ne 0) { Write-Error "Fallo 'create-vite'."; exit 1 }
}

# ---------------------------------------------------------------------------
# 3. .gitignore - el .gitignore de la raiz ya cubre a mano los patrones
#    estandar de .NET SDK-style y de Vite/React. Este paso solo consolida
#    frontend/.gitignore (generado por create-vite) adentro del de la raiz,
#    agregando unicamente las lineas que todavia no esten, para terminar con
#    un solo .gitignore en el repo.
#    (Se dejo de usar 'dotnet new gitignore' aca: no es consistente entre
#    versiones del SDK -- ver el mismo problema que con .slnx/.sln arriba.)
# ---------------------------------------------------------------------------
Write-Host "`n== Consolidando .gitignore ==" -ForegroundColor Cyan

$rootGitignore = Join-Path $PSScriptRoot ".gitignore"
$frontendGitignore = Join-Path $PSScriptRoot "frontend\.gitignore"

if (Test-Path $frontendGitignore) {
    $rootLines = Get-Content $rootGitignore
    $newLines = Get-Content $frontendGitignore | Where-Object {
        $line = $_.Trim()
        ($line -ne "") -and (-not $line.StartsWith("#")) -and ($rootLines -notcontains $_)
    }
    if ($newLines) {
        Add-Content -Path $rootGitignore -Value "`n## --- agregado desde frontend/.gitignore (create-vite) ---"
        Add-Content -Path $rootGitignore -Value $newLines
        Write-Host ("Se agregaron {0} linea(s) nuevas desde frontend/.gitignore." -f $newLines.Count) -ForegroundColor Yellow
    } else {
        Write-Host "frontend/.gitignore no tenia lineas nuevas para agregar." -ForegroundColor Yellow
    }
    Remove-Item $frontendGitignore
    Write-Host "frontend/.gitignore eliminado (consolidado en el .gitignore de la raiz)." -ForegroundColor Yellow
} else {
    Write-Host "No hay frontend/.gitignore para consolidar." -ForegroundColor Yellow
}

Write-Host "`n== Listo ==" -ForegroundColor Green
Write-Host "backend/, frontend/ y .gitignore actualizados. No se hizo git add/commit todavia."
Write-Host "Revisa los cambios (git status / git diff) antes de avisarle a Claude para que los commitee."
