#!/bin/bash
# Exit if command exits with non-zero status
set -e
# Pipelines fail if any command fails
set -o pipefail

PROJECT_PATH="./CoverLetterGenerator.csproj"
OUTPUT_DIR="./publish"

declare -A RUNTIMES
RUNTIMES["win-x64"]="Windows-x64"
RUNTIMES["linux-x64"]="Linux-x64"
RUNTIMES["osx-x64"]="macOS-x64"
RUNTIMES["osx-arm64"]="macOS-ARM64"

trap 'echo "Build failed! Exiting."; exit 1' ERR

rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

for RID in "${!RUNTIMES[@]}"; do
    BUILD_DIR="$OUTPUT_DIR/${RUNTIMES[$RID]}"
    ZIP_FILE="$OUTPUT_DIR/${RUNTIMES[$RID]}.zip"

    echo "Publishing for ${RUNTIMES[$RID]} ($RID)..."
    dotnet publish "$PROJECT_PATH" \
        -c Release \
        -r $RID \
        --self-contained true \
        -o "$BUILD_DIR"
        
    echo "Creating zip for ${RUNTIMES[$RID]}..."
    (cd "$BUILD_DIR" && zip -r "../${RUNTIMES[$RID]}.zip" .)
done

echo "Build complete! Published files are in $OUTPUT_DIR"