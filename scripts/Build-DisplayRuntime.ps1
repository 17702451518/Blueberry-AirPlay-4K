param(
    [Parameter(Mandatory=$true)][string]$Upstream,
    [Parameter(Mandatory=$true)][string]$OriginalRuntime,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [string]$MsysRoot = 'C:\msys64'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$upstream = (Resolve-Path -LiteralPath $Upstream).Path
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw 'Choose a new output directory; existing packages are never overwritten.' }
$commit = (& git -C $upstream rev-parse HEAD).Trim()
if ($commit -ne 'f76fe48400916449fd601b1c0444021aaf517082') { throw 'Unexpected upstream revision. See native/README.md.' }
& git -C (Join-Path $upstream 'libuxplay') apply --reverse --check --ignore-space-change (Join-Path $repo 'native\upstream.patch') 2>$null
if ($LASTEXITCODE -ne 0) {
    & git -C (Join-Path $upstream 'libuxplay') apply --ignore-space-change (Join-Path $repo 'native\upstream.patch')
    if ($LASTEXITCODE) { throw 'Native hook patch failed' }
}
Copy-Item -LiteralPath (Join-Path $repo 'native\display_window.c'),(Join-Path $repo 'native\display_window.h') -Destination (Join-Path $upstream 'libuxplay\renderers')
& git -C $upstream apply --reverse --check (Join-Path $repo 'native\upstream-main.patch') 2>$null
if ($LASTEXITCODE -ne 0) {
    & git -C $upstream apply (Join-Path $repo 'native\upstream-main.patch')
    if ($LASTEXITCODE) { throw 'Session logging policy patch failed' }
}
$prefix = Join-Path $MsysRoot 'ucrt64'
& git -C $upstream apply --reverse --check (Join-Path $repo 'native\upstream-ui.patch') 2>$null
if ($LASTEXITCODE -ne 0) {
    & git -C $upstream apply (Join-Path $repo 'native\upstream-ui.patch')
    if ($LASTEXITCODE) { throw 'Chinese interface patch failed' }
}
$env:PATH = "$(Join-Path $prefix 'bin');$env:PATH"
$env:BONJOUR_SDK_HOME = Join-Path $upstream 'Bonjour SDK'
$build = Join-Path $upstream 'build-blueberry'
& (Join-Path $prefix 'bin\cmake.exe') -S $upstream -B $build -G Ninja -DCMAKE_BUILD_TYPE=Release -DNO_MARCH_NATIVE=ON
if ($LASTEXITCODE) { throw 'Native configure failed' }
& (Join-Path $prefix 'bin\cmake.exe') --build $build --parallel 6
if ($LASTEXITCODE) { throw 'Native build failed' }
New-Item -ItemType Directory -Path $output,(Join-Path $output 'resources') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $build 'uxplay-windows.exe') -Destination $output
foreach ($name in @('uxplay-bluetooth-beacon.exe','dnssd.dll','mDNSResponder.exe','LICENSE.rtf')) {
    Copy-Item -LiteralPath (Join-Path $OriginalRuntime $name) -Destination $output
}
Copy-Item -LiteralPath (Join-Path $repo 'src\BlueberryAirPlay4K\Assets\BlueberryAirPlay4K.ico') -Destination (Join-Path $output 'resources\icon.ico')
Copy-Item -LiteralPath (Join-Path $upstream 'stuff\uxplay_arguments_list.txt') -Destination (Join-Path $output 'resources')
$features = Join-Path $upstream 'packaging\gstreamer-features.txt'
Copy-Item -LiteralPath $features -Destination (Join-Path $output 'resources')
& (Join-Path $prefix 'bin\windeployqt.exe') --release --no-translations --no-compiler-runtime --dir $output (Join-Path $output 'uxplay-windows.exe')
if ($LASTEXITCODE) { throw 'Qt deployment failed' }
$env:GST_PLUGIN_PATH_1_0 = ''
$env:GST_PLUGIN_SYSTEM_PATH_1_0 = Join-Path $prefix 'lib\gstreamer-1.0'
$env:GST_REGISTRY_1_0 = Join-Path $output 'build-registry.bin'
& (Join-Path $prefix 'bin\python.exe') (Join-Path $upstream 'scripts\resolve-gstreamer-plugins.py') --features $features --plugin-dir $env:GST_PLUGIN_SYSTEM_PATH_1_0 --destination (Join-Path $output 'lib\gstreamer-1.0') --manifest (Join-Path $output 'resources\gstreamer-plugins.json')
if ($LASTEXITCODE) { throw 'GStreamer deployment failed' }
New-Item -ItemType Directory -Path (Join-Path $output 'libexec\gstreamer-1.0') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $prefix 'libexec\gstreamer-1.0\gst-plugin-scanner.exe') -Destination (Join-Path $output 'libexec\gstreamer-1.0')
foreach ($directory in @('etc\fonts','share\fontconfig','lib\gio\modules','share\licenses')) {
    $targetParent = Split-Path (Join-Path $output $directory) -Parent
    New-Item -ItemType Directory -Path $targetParent -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $prefix $directory) -Destination $targetParent -Recurse
}
[IO.File]::WriteAllText((Join-Path $output 'display-control.version'), "1`n")
@{ upstream = $commit; libuxplay = (& git -C (Join-Path $upstream 'libuxplay') rev-parse HEAD).Trim(); displayControl = 1; packages = @(& (Join-Path $MsysRoot 'usr\bin\pacman.exe') -Q | Where-Object { $_ -like 'mingw-w64-ucrt-x86_64-*' }) } | ConvertTo-Json -Depth 4 | Out-File (Join-Path $output 'resources\build-manifest.json') -Encoding utf8
& (Join-Path $upstream 'scripts\collect-runtime-dependencies.ps1') -StageDir $output -MsysRoot $MsysRoot -EnvironmentName ucrt64 -ManifestPath (Join-Path $output 'resources\bundle-files.json')
if (Test-Path -LiteralPath $env:GST_REGISTRY_1_0) { Remove-Item -LiteralPath $env:GST_REGISTRY_1_0 }
Write-Output "Display runtime: $output"
& (Join-Path $repo 'scripts\Update-RuntimeManifest.ps1') -Runtime $output
