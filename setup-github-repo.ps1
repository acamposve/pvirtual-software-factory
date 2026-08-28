<#
.SYNOPSIS
  Fase 0 - crea el repo remoto en GitHub (pvirtual-software-factory) y configura
  branch protection en main.
.DESCRIPTION
  Ejecutar una sola vez desde PowerShell, parado en cualquier carpeta
  (usa la carpeta donde vive este script como raiz del repo):

      cd C:\proyectos\programacion_agentica
      .\setup-github-repo.ps1

  Requiere: gh CLI instalado y autenticado (gh auth login).
#>

$ErrorActionPreference = "Stop"

$RepoName = "pvirtual-software-factory"
$Owner    = "acamposve"
$FullRepo = "$Owner/$RepoName"

# Pararse en la carpeta donde vive este script (= raiz del repo)
Set-Location -Path $PSScriptRoot

Write-Host "== Fase 0: creando repo remoto '$FullRepo' ==" -ForegroundColor Cyan

# Verificar que gh esta autenticado
gh auth status *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Error "gh no esta autenticado. Corre 'gh auth login' primero."
    exit 1
}

# Verificar que estamos parados en un repo git
git rev-parse --is-inside-work-tree *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Error "Esta carpeta no es un repo git. Corre este script desde programacion_agentica."
    exit 1
}

# 1. Crear el repo remoto (publico) y pushear lo que ya esta commiteado.
#    Si el repo ya existe en GitHub (por si el script se corre dos veces), lo saltea
#    y solo pushea.
gh repo view $FullRepo *> $null
if ($LASTEXITCODE -eq 0) {
    Write-Host "El repo '$FullRepo' ya existe en GitHub, salteo la creacion." -ForegroundColor Yellow
    git remote get-url origin *> $null
    if ($LASTEXITCODE -ne 0) {
        git remote add origin "https://github.com/$FullRepo.git"
    }
    git push -u origin main
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Fallo el push a origin/main."
        exit 1
    }
} else {
    gh repo create $FullRepo --public --source=. --remote=origin --push
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Fallo 'gh repo create'."
        exit 1
    }
}

Write-Host "== Configurando branch protection en 'main' ==" -ForegroundColor Cyan

# 2. Branch protection: PR obligatorio + 1 review + review de CODEOWNERS.
#    enforce_admins = false -> vos (owner) podes pushear directo a main o
#    bypassear el PR mientras trabajas solo. Poner esto en $true mas adelante
#    cuando sumes gente al repo o agentes con permisos de push.
#    Sin required_status_checks todavia -> se agrega en la Fase 3 cuando exista CI.
$protectionJson = @'
{
  "required_status_checks": null,
  "enforce_admins": false,
  "required_pull_request_reviews": {
    "required_approving_review_count": 1,
    "require_code_owner_reviews": true
  },
  "restrictions": null
}
'@

$tempFile = Join-Path $PSScriptRoot "protection.json"
# Escribe SIN BOM: Set-Content -Encoding utf8 en Windows PowerShell 5.1 agrega un BOM
# al archivo, y gh api falla con "Problems parsing JSON" al leerlo. .NET WriteAllText
# con UTF8Encoding($false) evita el BOM en cualquier version de PowerShell.
[System.IO.File]::WriteAllText($tempFile, $protectionJson, (New-Object System.Text.UTF8Encoding($false)))

gh api "repos/$FullRepo/branches/main/protection" -X PUT --input $tempFile
$protectionExit = $LASTEXITCODE

Remove-Item $tempFile -ErrorAction SilentlyContinue

if ($protectionExit -ne 0) {
    Write-Error "Fallo la configuracion de branch protection."
    exit 1
}

Write-Host "== Listo. Repo creado y branch protection configurada. ==" -ForegroundColor Green
Write-Host "https://github.com/$FullRepo" -ForegroundColor Green
