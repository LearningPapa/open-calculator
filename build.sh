#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────
#  Open Calculator — Cross-platform publish script (bash)
# ─────────────────────────────────────────────────────────────────
#  Produces self-contained single-file binaries for:
#    win-x64, linux-x64, osx-x64, osx-arm64
#  Output: ./publish/OpenCalculator-<platform>(.exe)
# ─────────────────────────────────────────────────────────────────

set -e

PROJECT="Open Calculator.csproj"
OUT="./publish"

declare -a TARGETS=(
    "win-x64:.exe"
    "linux-x64:"
    "osx-x64:"
    "osx-arm64:"
)

# Clean previous output
rm -rf "$OUT"
mkdir -p "$OUT"

for entry in "${TARGETS[@]}"; do
    rid="${entry%%:*}"
    ext="${entry##*:}"

    echo ""
    echo "═══════════════════════════════════════════"
    echo "  Publishing for $rid"
    echo "═══════════════════════════════════════════"

    dotnet publish "$PROJECT" \
        -c Release \
        -r "$rid" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -p:EnableCompressionInSingleFile=true \
        -o "$OUT/$rid"

    # Rename binary to a friendly name and clean up the folder
    old="$OUT/$rid/Open Calculator$ext"
    new="$OUT/OpenCalculator-$rid$ext"
    if [ -f "$old" ]; then
        mv "$old" "$new"
        rm -rf "$OUT/$rid"
    fi
done

echo ""
echo "═══════════════════════════════════════════"
echo "  Build complete. Binaries in ./publish/"
echo "═══════════════════════════════════════════"
ls -lh "$OUT" | awk 'NR>1 {printf "  %-40s %s\n", $9, $5}'
