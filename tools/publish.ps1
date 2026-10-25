# Builds a self-contained release and optionally zips it.
#   -Runtime win-x64    publish\S2HD-win-x64 (ready to run)
#   -Runtime osx-arm64  publish\S2HD-macos-kit: copy it (or the -Zip archive) to an Apple Silicon Mac and run
#                       ./build-app.sh there to produce S2HD.app
# Requires the original game install for assets (see GameDir in S2HD.csproj); Windows builds also need
# FFmpeg (tools\get-ffmpeg.ps1, fetched automatically).
param(
  [ValidateSet('win-x64', 'osx-arm64')]
  [string]$Runtime = 'win-x64',
  [switch]$Zip,
  [string]$GameDir
)
$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')

# Compress-Archive (Windows PowerShell 5.1) writes '\' into entry names, which macOS extracts as flat
# filenames; write '/' separators and Unix permissions (executable bits for launchers and scripts) instead.
function New-PortableZip([string]$SourceDir, [string]$ZipPath, [string[]]$ExecutableNames) {
  Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
  $base = (Resolve-Path $SourceDir).Path.TrimEnd('\') + '\'
  $zip = [IO.Compression.ZipFile]::Open($ZipPath, 'Create')
  try {
    foreach ($file in Get-ChildItem $SourceDir -Recurse -File) {
      $name = $file.FullName.Substring($base.Length).Replace('\', '/')
      $entry = [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $file.FullName, $name, 'Optimal')
      $mode = if ($ExecutableNames -contains $file.Name) { 0x81ED } else { 0x81A4 }  # 0100755 / 0100644
      $entry.ExternalAttributes = $mode -shl 16
    }
  }
  finally { $zip.Dispose() }
}
$isWindowsBuild = $Runtime -like 'win-*'

if ($isWindowsBuild -and -not (Test-Path (Join-Path $root 'lib\ffmpeg\avcodec-61.dll'))) {
  & (Join-Path $PSScriptRoot 'get-ffmpeg.ps1')
}

$out = Join-Path $root "publish\S2HD-$Runtime"
# DeleteExistingFiles leaves empty folders behind, so start clean
if (Test-Path $out) { Remove-Item -Recurse -Force $out }

$publishArgs = @('publish', (Join-Path $root 'S2HD.csproj'), "-p:PublishProfile=Release-$Runtime", '-nologo')
if ($GameDir) { $publishArgs += "-p:GameDir=$($GameDir.TrimEnd('\'))\" }
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)" }

$required = @('data\sonicorca.dat', 'shaders', 'credits.film')
$required += if ($isWindowsBuild) { 'S2HD.exe', 'SDL2.dll', 'ffmpeg\avcodec-61.dll' } else { 'S2HD' }
foreach ($file in $required) {
  if (-not (Test-Path (Join-Path $out $file))) { throw "Release is missing '$file'; check GameDir." }
}

$release = $out
if (-not $isWindowsBuild) {
  # Everything build-app.sh needs on the Mac, in one folder
  $release = Join-Path $root 'publish\S2HD-macos-kit'
  if (Test-Path $release) { Remove-Item -Recurse -Force $release }
  New-Item -ItemType Directory $release | Out-Null
  Move-Item $out (Join-Path $release "S2HD-$Runtime")
  Copy-Item (Join-Path $root 'tools\macos\build-app.sh') $release
  Copy-Item (Join-Path $root 'assets\S2HD.png') $release
  Copy-Item (Join-Path $root 'assets\S2HD.icon') $release -Recurse
}

if ($Zip) {
  $zipPath = "$release.zip"
  if (Test-Path $zipPath) { Remove-Item $zipPath }
  New-PortableZip $release $zipPath @('S2HD', 'createdump', 'build-app.sh')
  Write-Host "Zipped to $zipPath"
}
$size = (Get-ChildItem $release -Recurse -File | Measure-Object Length -Sum).Sum / 1MB
Write-Host ("Release folder: {0} ({1:N0} MB)" -f $release, $size)
if (-not $isWindowsBuild) { Write-Host "On the Mac: unzip/copy the kit, then run: bash build-app.sh" }
