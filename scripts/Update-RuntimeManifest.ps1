param([Parameter(Mandatory=$true)][string]$Runtime)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $Runtime).Path
$destination = Join-Path $root 'resources\bundle-files.json'
# Generated build artifact. Never hash the manifest itself or transient settings.
$items = Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object {
    $_.FullName -ne $destination -and $_.Name -notin @('build-registry.bin','display-mode.txt')
} | Sort-Object FullName | ForEach-Object {
    [ordered]@{
        path = [IO.Path]::GetRelativePath($root, $_.FullName).Replace('\','/')
        bytes = $_.Length
        sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}
$items | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $destination -Encoding utf8
