# Arma el instalador de SuraChih en Instalador\salida\SuraChih-Instalador.exe.
#   1. Publica la app (con .NET adentro: la otra computadora no necesita instalar nada) en ..\publicar.
#   2. Compila SuraChih.iss con Inno Setup 6 (https://jrsoftware.org/isinfo.php).
# Uso, desde la raíz del repositorio:  powershell -ExecutionPolicy Bypass -File Instalador\crear-instalador.ps1

$ErrorActionPreference = 'Stop'
$raiz = Split-Path $PSScriptRoot -Parent
$publicado = Join-Path $raiz 'publicar'

$iscc = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    Write-Host 'Falta Inno Setup 6. Instálalo con:  winget install JRSoftware.InnoSetup' -ForegroundColor Yellow
    exit 1
}
if (-not (Test-Path (Join-Path $raiz 'dbsettings.local.json'))) {
    Write-Host 'Aviso: no hay dbsettings.local.json; el instalador no llevará la conexión a la base de datos.' -ForegroundColor Yellow
}

Write-Host '1/2 Publicando la app...'
if (Test-Path $publicado) { Remove-Item -Recurse -Force $publicado }
dotnet publish (Join-Path $raiz 'SuraChihIntegrativeProject.csproj') -c Release -r win-x64 --self-contained `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $publicado -nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'No se pudo publicar la app.' }

Write-Host '2/2 Armando el instalador...'
& $iscc /Q (Join-Path $PSScriptRoot 'SuraChih.iss')
if ($LASTEXITCODE -ne 0) { throw 'Inno Setup no pudo crear el instalador.' }

$exe = Join-Path $PSScriptRoot 'salida\SuraChih-Instalador.exe'
Write-Host ("Listo: {0} ({1:N0} MB)" -f $exe, ((Get-Item $exe).Length / 1MB)) -ForegroundColor Green
