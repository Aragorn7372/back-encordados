#!/bin/bash
set -e

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TEST_DIR="$PROJECT_ROOT/TestEncordados"
OUTPUT_DIR="$PROJECT_ROOT/coverage-report"

mkdir -p "$OUTPUT_DIR"

FILTER="FullyQualifiedName~Unit"

echo "Running unit tests with coverage..."

dotnet test "$TEST_DIR" \
    --filter "$FILTER" \
    --collect:"XPlat Code Coverage" \
    -- RunConfiguration.DisableAutoOutputBuffers=true

echo "Finding coverage files..."
LATEST_COVERAGE=$(find "$TEST_DIR/TestResults" -name "coverage.cobertura.xml" -type f -printf '%T@ %p\n' 2>/dev/null | sort -rn | head -1 | cut -d' ' -f2)

if [ -z "$LATEST_COVERAGE" ]; then
    echo "No coverage file found!"
    exit 1
fi

echo "Generating HTML report..."
reportgenerator "-reports:$LATEST_COVERAGE" "-targetdir:$OUTPUT_DIR" "-reporttypes:Html;Badges"

echo "Coverage report generated at: $OUTPUT_DIR/index.html"