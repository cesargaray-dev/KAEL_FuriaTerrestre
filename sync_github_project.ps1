<#
.SYNOPSIS
    Sincroniza y vincula todos los Issues de KAEL: Furia Terrestre al tablero de GitHub Projects V2.
.DESCRIPTION
    Requiere que GitHub CLI ('gh') cuente con el permiso de project:
    gh auth refresh -s project
#>

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  SINCRONIZADOR DE GITHUB PROJECTS — KAEL: FURIA TERRESTRE" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Verificar si gh tiene el scope de project
$authCheck = gh project list --owner cesargaray-dev 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "[!] Tu token de GitHub CLI no tiene el permiso 'project'." -ForegroundColor Yellow
    Write-Host "    Para otorgarlo en 10 segundos, ejecuta:" -ForegroundColor White
    Write-Host "    gh auth refresh -s project" -ForegroundColor Green
    Write-Host ""
    Write-Host "    (Se abrirá una ventana en tu navegador para confirmar)." -ForegroundColor Gray
    exit 1
}

# 2. Buscar si ya existe el proyecto o crearlo
Write-Host "[1/4] Comprobando existencia del tablero 'KAEL: Furia Terrestre - Roadmap'..." -ForegroundColor White
$projectsJson = gh project list --owner cesargaray-dev --format json | ConvertFrom-Json
$targetProject = $projectsJson.projects | Where-Object { $_.title -like "*KAEL: Furia Terrestre*" }

if (-not $targetProject) {
    Write-Host "[+] Creando tablero GitHub Projects V2..." -ForegroundColor Green
    $createOutput = gh project create --owner cesargaray-dev --title "KAEL: Furia Terrestre - Roadmap" --format json | ConvertFrom-Json
    $projectNumber = $createOutput.number
} else {
    $projectNumber = $targetProject.number
    Write-Host "[✓] Tablero existente encontrado: #$projectNumber ($($targetProject.title))" -ForegroundColor Green
}

# 3. Obtener la lista de issues del repositorio
Write-Host "[2/4] Obteniendo lista de issues del repositorio..." -ForegroundColor White
$issues = gh issue list --limit 50 --json number,title,url,state,milestone | ConvertFrom-Json
Write-Host "[✓] Se encontraron $($issues.Count) issues." -ForegroundColor Green

# 4. Vincular cada issue al proyecto
Write-Host "[3/4] Vinculando issues al tablero de proyecto #$projectNumber..." -ForegroundColor White

foreach ($iss in $issues) {
    Write-Host "  -> Agregando Issue #$($iss.number): $($iss.title.Substring(0, [Math]::Min(50, $iss.title.Length)))..." -NoNewline
    $addRes = gh project item-add $projectNumber --owner cesargaray-dev --url $iss.url 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host " [OK]" -ForegroundColor Green
    } else {
        Write-Host " [EXISTE / OMITIDO]" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "[4/4] Sincronización completada con éxito." -ForegroundColor Cyan
Write-Host "Puedes ver y gestionar tu tablero Kanban en:" -ForegroundColor White
Write-Host "https://github.com/users/cesargaray-dev/projects/$projectNumber" -ForegroundColor Yellow
