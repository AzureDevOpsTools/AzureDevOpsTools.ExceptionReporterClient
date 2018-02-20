param(
    $Version,
    $Path
)

$paketExecutable = "paket.bootstrapper.exe"
Write-Host "$paketExecutable will be downloaded and renamed to paket.exe. This will enable `"magic mode`" see: https://fsprojects.github.io/Paket/bootstrapper.html#Magic-mode for more details."



Set-StrictMode -Version Latest
$here = Split-Path -Parent $MyInvocation.MyCommand.Path

if(-not $Path) {
    Write-Host "Path is not set script directory will be used as path"
    $Path = $here
}

Set-StrictMode -Version Latest
$paketRepositoryUrl = "https://github.com/fsprojects/Paket"


if(-not $Version) {
    $latestReleaseUrl = "$paketRepositoryUrl/releases/latest"

    Write-Host "Version not set, determining latest version using $latestReleaseUrl"

    $latestRelease = ConvertFrom-Json (Invoke-WebRequest -Uri "$latestReleaseUrl" -Headers @{"Accept"="application/json"} -UseBasicParsing)
    $Version = $latestRelease.tag_name
}

Write-Host "Installing Paket $Version"

$paketExecutableDownloadUrl = "$paketRepositoryUrl/releases/download/$Version/$paketExecutable"

Write-Host "Searching for paket.dependencies file in Path: `"$Path`""
$paketDependenciesFile = Get-ChildItem -Path $Path -Recurse -Filter "paket.dependencies" | Select-Object -First 1

if(-not $paketDependenciesFile) {
    throw "Could not find a paket.dependencies file in Path: `"$Path`". Make sure Paket is initialized or specify a different path to search using the -Path argument."
}

Write-Host "Found paket.dependencies: `"$($paketDependenciesFile.FullName)`""
$paketDirectory = Join-Path -Path $($paketDependenciesFile.Directory.FullName) -ChildPath ".paket"
$paketExePath = "$paketDirectory\paket.exe"

if(Test-Path $paketDirectory) {
    Write-Host "Removing existing Paket executables"
    Remove-Item -Path "$paketDirectory/paket.exe" -ErrorAction SilentlyContinue  -Force -Confirm:$false |Out-Null 
    Remove-Item -Path "$paketDirectory/paket.bootstrapper.exe" -ErrorAction SilentlyContinue -Force -Confirm:$false |Out-Null
}else{
    Write-Host "Creating Paket directory at `"$paketDirectory`""
    New-Item -ItemType Directory -Path $paketDirectory | Out-Null
}



Write-Host "Downloading $paketExecutable from $paketExecutableDownloadUrl to $paketExePath"
Invoke-WebRequest -Uri "$paketExecutableDownloadUrl" -OutFile "$paketExePath" -UseBasicParsing