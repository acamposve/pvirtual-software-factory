<#
.SYNOPSIS
  Fase 3 (parte 2) - una vez que el workflow de CI corrio con exito al menos
  una vez en 'main', esto agrega los checks de CI como required_status_checks
  en la branch protection de main (conservando lo que ya configuramos en
  setup-github-repo.ps1: PR obligatorio + 1 review + CODEOWNERS).
.DESCRIPTION
  Correr DESPUES de que:
    1. Hiciste 'git push' con los workflows de .github/workflows/.
    2. En GitHub -> pestana Actions, el workflow "CI" corrio y esta en verde
       (o al menos corrio una vez, para que GitHub sepa que esos checks existen).

      cd C:\proyectos\programacion_agentica
      .\enable-ci-branch-protection.ps1
#>

$ErrorActionPreference = "Stop"

$RepoName = "pvirtual-software-factory"
$Owner    = "acamposve"
$FullRepo = "$Owner/$RepoName"

Set-Location -Path $PSScriptRoot

gh auth status *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Error "gh no esta autenticado. Corre 'gh auth login' primero."
    exit 1
}

Write-Host "== Actualizando branch protection en 'main' (agregando required status checks) ==" -ForegroundColor Cyan

# Mismo criterio que setup-github-repo.ps1: enforce_admins = false, porque
# segun lo que definimos el owner puede pushear directo / bypassear el PR
# mientras se trabaja solo. Si eso cambia, poner enforce_admins en true aca
# tambien.
$protectionJson = @'
{
  "required_status_checks": {
    "strict": true,
    "checks": [
      { "context": "Backend (.NET)" },
      { "context": "Frontend (React)" }
    ]
  },
  "enforce_admins": false,
  "required_pull_request_reviews": {
    "required_approving_review_count": 1,
    "require_code_owner_reviews": true
  },
  "restrictions": null
}
'@

$tempFile = Join-Path $PSScriptRoot "protection.json"
[System.IO.File]::WriteAllText($tempFile, $protectionJson, (New-Object System.Text.UTF8Encoding($false)))

gh api "repos/$FullRepo/branches/main/protection" -X PUT --input $tempFile
$protectionExit = $LASTEXITCODE

Remove-Item $tempFile -ErrorAction SilentlyContinue

if ($protectionExit -ne 0) {
    Write-Error "Fallo la actualizacion de branch protection. Nota: los 'context' deben coincidir EXACTO con el nombre del job tal cual aparece en la pestana Actions (Backend (.NET) / Frontend (React))."
    exit 1
}

Write-Host "== Listo. 'main' ahora exige CI verde (Backend + Frontend) antes de mergear un PR. ==" -ForegroundColor Green
