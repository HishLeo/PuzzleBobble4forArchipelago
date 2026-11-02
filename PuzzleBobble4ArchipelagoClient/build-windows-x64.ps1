$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'src\PuzzleBobble4ArchipelagoClient.csproj'
$out = Join-Path $PSScriptRoot 'publish'

dotnet publish $project -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=None -p:DebugSymbols=false -o $out

if ($LASTEXITCODE -ne 0) {
  throw "La compilacion fallo (dotnet publish exit code: $LASTEXITCODE). No uses el EXE de publish; revisa los errores anteriores."
}

$exe = Join-Path $out 'PuzzleBobble4ArchipelagoClient.exe'
if (-not (Test-Path $exe)) {
  throw "dotnet publish termino sin error, pero no se encontro el ejecutable esperado: $exe"
}

Write-Host "Build listo: $exe"
