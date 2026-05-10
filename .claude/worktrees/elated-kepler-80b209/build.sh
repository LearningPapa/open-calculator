#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────
#  TI DESTROYER 9000 — Cross-platform publish script (bash)
# ─────────────────────────────────────────────────────────────────

set -e

PROJECT="TIDestroyer9000.csproj"
OUT="./publish"

declare -a TARGETS=(
    "win-x64:.exe"
    "linux-x64:"
    "osx-x64:"
    "osx-arm64:"
)

rm -rf "$OUT"
mkdir -p "$OUT"

# Unlock keychain for codesigning on macOS, if a Developer ID cert is configured.
# Skipped silently if you don't have signing set up yet.
if [[ "$OSTYPE" == "darwin"* ]]; then
    if security find-identity -v -p codesigning 2>/dev/null | grep -q "Developer ID"; then
        security unlock-keychain ~/Library/Keychains/login.keychain-db
    fi
fi

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

    old="$OUT/$rid/TIDestroyer9000$ext"
    new="$OUT/TIDestroyer9000-$rid$ext"
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
