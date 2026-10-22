$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'src\PuzzleBobble4ArchipelagoClient.csproj'
$out = Join-Path $PSScriptRoot 'publish'

dotnet publish $project -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=None -p:DebugSymbols=false -o $out

Write-Host "Build listo: $out\PuzzleBobble4ArchipelagoClient.exe"
