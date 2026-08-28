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
    dotnet new sln -n PVirtualSoftwareFactory -o . --force
    if ($LASTEXITCODE -ne 0) { Write-Error "Fallo 'dotnet new sln'."; exit 1 }

    dotnet new webapi -n Api -o backend
    if ($LASTEXITCODE -ne 0) { Write-Error "Fallo 'dotnet new webapi'."; exit 1 }

    dotnet sln PVirtualSoftwareFactory.sln add backend/Api.csproj
    if ($LASTEXITCODE -ne 0) { Write-Error "Fallo agregar el proyecto a la solucion."; exit 1 }
}

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
# 3. .gitignore - generar el oficial de dotnet y combinarlo con el actual
#    (el de Vite ya quedo copiado dentro de frontend/.gitignore por create-vite)
# ---------------------------------------------------------------------------
Write-Host "`n== Regenerando .gitignore ==" -ForegroundColor Cyan

$dotnetGitignoreDir = Join-Path $PSScriptRoot ".dotnet-gitignore-tmp"
New-Item -ItemType Directory -Path $dotnetGitignoreDir -Force | Out-Null

dotnet new gitignore -o $dotnetGitignoreDir --force
if ($LASTEXITCODE -ne 0) { Write-Error "Fallo 'dotnet new gitignore'."; exit 1 }

$dotnetGitignoreContent = Get-Content (Join-Path $dotnetGitignoreDir ".gitignore") -Raw
Remove-Item $dotnetGitignoreDir -Recurse -Force

$rootGitignore = Join-Path $PSScriptRoot ".gitignore"
Add-Content -Path $rootGitignore -Value "`n## --- .NET (generado por 'dotnet new gitignore', Fase 1) ---`n"
Add-Content -Path $rootGitignore -Value $dotnetGitignoreContent

$frontendGitignore = Join-Path $PSScriptRoot "frontend\.gitignore"
if (Test-Path $frontendGitignore) {
    $viteGitignoreContent = Get-Content $frontendGitignore -Raw
    Add-Content -Path $rootGitignore -Value "`n## --- Node / Vite (generado por create-vite, Fase 1) ---`n"
    Add-Content -Path $rootGitignore -Value $viteGitignoreContent
    Remove-Item $frontendGitignore
    Write-Host "Se consolido frontend/.gitignore dentro del .gitignore raiz." -ForegroundColor Yellow
}

Write-Host "`n== Listo ==" -ForegroundColor Green
Write-Host "backend/, frontend/ y .gitignore actualizados. No se hizo git add/commit todavia."
Write-Host "Revisa los cambios (git status / git diff) antes de avisarle a Claude para que los commitee."
