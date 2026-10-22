# Downloads the FFmpeg 7.1 shared libraries required by FFMediaToolkit 4.8.1 (via FFmpeg.AutoGen 7.1.1) for credits film playback
# into lib\ffmpeg, which the build copies to the output's ffmpeg\ folder.
# Note: Gyan's "full" builds are GPL-licensed.
param(
  [string]$Version = '7.1.1',
  [string]$DownloadDir = (Join-Path ([IO.Path]::GetTempPath()) "s2hd-ffmpeg-$Version")
)
$ErrorActionPreference = 'Stop'

$name = "ffmpeg-$Version-full_build-shared"
$url = "https://github.com/GyanD/codexffmpeg/releases/download/$Version/$name.zip"
$dest = Join-Path $PSScriptRoot '..\lib\ffmpeg'
$zip = Join-Path $DownloadDir "$name.zip"

New-Item -ItemType Directory -Force $DownloadDir | Out-Null
if (-not (Test-Path $zip)) {
  Write-Host "Downloading $url"
  [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
  (New-Object Net.WebClient).DownloadFile($url, $zip)
}

$extract = Join-Path $DownloadDir 'extracted'
if (Test-Path $extract) { Remove-Item -Recurse -Force $extract }
Expand-Archive -Path $zip -DestinationPath $extract

New-Item -ItemType Directory -Force $dest | Out-Null
Copy-Item (Join-Path $extract "$name\bin\*.dll") $dest -Force
Copy-Item (Join-Path $extract "$name\LICENSE") (Join-Path $dest 'LICENSE.txt') -Force -ErrorAction SilentlyContinue
Write-Host "FFmpeg $Version libraries copied to $((Resolve-Path $dest).Path)"
