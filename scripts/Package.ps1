param(
    [Parameter(Mandatory=$true)][string]$BasePackage,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [string]$Dotnet = 'dotnet'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw 'Output directory already exists; choose a new directory.' }
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
& $Dotnet build (Join-Path $repo 'src\BlueberryAirPlay4K\BlueberryAirPlay4K.csproj') -c Release --warnaserror
if ($LASTEXITCODE) { throw 'Controller build failed' }
New-Item -ItemType Directory -Path $output | Out-Null
$stage = Join-Path $output 'source-package'
Expand-Archive -LiteralPath $BasePackage -DestinationPath $stage
$roots = @(Get-ChildItem -LiteralPath $stage -Directory)
if ($roots.Count -ne 1) { throw 'Expected exactly one package root' }
$source = $roots[0].FullName
if (!(Test-Path -LiteralPath (Join-Path $source 'uxplay-windows.exe'))) { throw 'Expected a flat input package' }
$package = Join-Path $output 'Blueberry-AirPlay-4K-v3.2.0-preview.4-win64'
foreach ($folder in @('app','runtime','docs')) { New-Item -ItemType Directory -Path (Join-Path $package $folder) -Force | Out-Null }
foreach ($item in Get-ChildItem -LiteralPath $source) {
    if ($item.Name -like '蓝莓AirPlay4K控制台.*' -or $item.Name -eq 'docs' -or $item.Extension -eq '.md' -or $item.Name -eq 'LICENSE') { continue }
    Copy-Item -LiteralPath $item.FullName -Destination (Join-Path $package 'runtime') -Recurse
}
$built = Join-Path $repo 'src\BlueberryAirPlay4K\bin\Release\net8.0-windows'
foreach ($extension in @('exe','dll','deps.json','runtimeconfig.json')) {
    Copy-Item -LiteralPath (Join-Path $built "蓝莓AirPlay4K控制台.$extension") -Destination (Join-Path $package 'app')
}
Copy-Item (Join-Path $repo 'docs\*.md') (Join-Path $package 'docs')
Copy-Item (Join-Path $repo '*.md') (Join-Path $package 'docs')
Copy-Item (Join-Path $repo 'LICENSE') $package
Copy-Item (Join-Path $repo 'docs\PORTABLE-README.md') (Join-Path $package 'README.md')
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:winexe /optimize+ /reference:System.Windows.Forms.dll "/win32icon:$(Join-Path $repo 'src\BlueberryAirPlay4K\Assets\BlueberryAirPlay4K.ico')" "/out:$(Join-Path $package '蓝莓投屏.exe')" (Join-Path $repo 'src\Launcher\Program.cs')
if ($LASTEXITCODE) { throw 'Launcher build failed' }
# Verify every retained upstream file is byte-for-byte identical after relocation.
foreach ($file in Get-ChildItem (Join-Path $package 'runtime') -File -Recurse) {
    $relative = [IO.Path]::GetRelativePath((Join-Path $package 'runtime'), $file.FullName)
    if ((Get-FileHash $file.FullName).Hash -ne (Get-FileHash (Join-Path $source $relative)).Hash) { throw "Dependency mismatch: $relative" }
}
$zip = "$package.zip"
Compress-Archive -LiteralPath $package -DestinationPath $zip
Write-Output "Package: $zip"
Get-FileHash -LiteralPath $zip
