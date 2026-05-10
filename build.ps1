# ─────────────────────────────────────────────────────────────────
#  TI DESTROYER 9000 — Cross-platform publish script (PowerShell)
# ─────────────────────────────────────────────────────────────────

$ErrorActionPreference = "Stop"

$Project = "TIDestroyer9000.csproj"
$Out     = "./publish"

$Targets = @(
    @{ rid = "win-x64";   ext = ".exe" },
    @{ rid = "linux-x64"; ext = ""     },
    @{ rid = "osx-x64";   ext = ""     },
    @{ rid = "osx-arm64"; ext = ""     }
)

if (Test-Path $Out) { Remove-Item -Recurse -Force $Out }
New-Item -ItemType Directory -Path $Out | Out-Null

foreach ($t in $Targets) {
    $rid = $t.rid
    $ext = $t.ext
    Write-Host ""
    Write-Host "═══════════════════════════════════════════"
    Write-Host "  Publishing for $rid"
    Write-Host "═══════════════════════════════════════════"

    dotnet publish $Project `
        -c Release `
        -r $rid `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -o "$Out/$rid"

    $oldName = "$Out/$rid/TIDestroyer9000$ext"
    $newName = "$Out/TIDestroyer9000-$rid$ext"
    if (Test-Path $oldName) {
        Move-Item $oldName $newName
        Get-ChildItem "$Out/$rid" | Remove-Item -Force
        Remove-Item "$Out/$rid"
    }
}

Write-Host ""
Write-Host "═══════════════════════════════════════════"
Write-Host "  Build complete. Binaries in ./publish/"
Write-Host "═══════════════════════════════════════════"
Get-ChildItem $Out | Format-Table Name, @{Label='Size (MB)'; Expression={[math]::Round($_.Length/1MB,1)}}
