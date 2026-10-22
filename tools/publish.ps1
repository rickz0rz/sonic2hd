# Builds a self-contained release folder (publish\S2HD-win-x64) and optionally zips it.
# Requires the original game install for assets (see GameDir in S2HD.csproj) and FFmpeg (tools\get-ffmpeg.ps1).
param(
  [switch]$Zip,
  [string]$GameDir
)
$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')

if (-not (Test-Path (Join-Path $root 'lib\ffmpeg\avcodec-61.dll'))) {
  & (Join-Path $PSScriptRoot 'get-ffmpeg.ps1')
}

$publishArgs = @('publish', (Join-Path $root 'S2HD.csproj'), '-p:PublishProfile=Release-win-x64', '-nologo')
if ($GameDir) { $publishArgs += "-p:GameDir=$($GameDir.TrimEnd('\'))\" }
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)" }

$out = Join-Path $root 'publish\S2HD-win-x64'
foreach ($required in 'S2HD.exe', 'SDL2.dll', 'data\sonicorca.dat', 'Shaders', 'credits.film', 'ffmpeg\avcodec-61.dll') {
  if (-not (Test-Path (Join-Path $out $required))) { throw "Release is missing '$required'; check GameDir." }
}

if ($Zip) {
  $zipPath = Join-Path $root 'publish\S2HD-win-x64.zip'
  if (Test-Path $zipPath) { Remove-Item $zipPath }
  Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zipPath -CompressionLevel Optimal
  Write-Host "Zipped to $zipPath"
}
$size = (Get-ChildItem $out -Recurse -File | Measure-Object Length -Sum).Sum / 1MB
Write-Host ("Release folder: {0} ({1:N0} MB)" -f $out, $size)
