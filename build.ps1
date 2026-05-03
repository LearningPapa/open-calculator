# ─────────────────────────────────────────────────────────────────
#  Open Calculator — Cross-platform publish script (PowerShell)
# ─────────────────────────────────────────────────────────────────
#  Produces self-contained single-file binaries for:
#    win-x64, linux-x64, osx-x64, osx-arm64
#  Output: ./publish/<platform>/OpenCalculator(.exe)
# ─────────────────────────────────────────────────────────────────

$ErrorActionPreference = "Stop"

$Project = "Open Calculator.csproj"
$Out     = "./publish"

# Platforms to build — comment any out to skip
$Targets = @(
    @{ rid = "win-x64";   ext = ".exe" },
    @{ rid = "linux-x64"; ext = ""     },
    @{ rid = "osx-x64";   ext = ""     },
    @{ rid = "osx-arm64"; ext = ""     }
)

# Clean previous output
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

    # Rename the binary to a friendly name
    $oldName = "$Out/$rid/Open Calculator$ext"
    $newName = "$Out/OpenCalculator-$rid$ext"
    if (Test-Path $oldName) {
        Move-Item $oldName $newName
        # Remove the now-empty per-platform folder (single-file = no leftover deps)
        # Note: a couple of small native libs may remain on Linux/macOS — that's normal
        Get-ChildItem "$Out/$rid" | Remove-Item -Force
        Remove-Item "$Out/$rid"
    }
}

Write-Host ""
Write-Host "═══════════════════════════════════════════"
Write-Host "  Build complete. Binaries in ./publish/"
Write-Host "═══════════════════════════════════════════"
Get-ChildItem $Out | Format-Table Name, @{Label='Size (MB)'; Expression={[math]::Round($_.Length/1MB,1)}}
