#!/usr/bin/env bash
# scripts/diagrams/generate-diagrams.sh — Generate and render mermaid diagrams.
#
# Prerequisites:
#   - dotnet tool restore (installs Dependify.Cli)
#   - Node.js / npx available (for @mermaid-js/mermaid-cli)
#   - EfToMermaid tool project built
#
# Output: SVGs in src/API/Nocturne.API/wwwroot/diagrams/
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
DIAGRAMS_DIR="$REPO_ROOT/docs/diagrams"
OUTPUT_DIR="$REPO_ROOT/src/API/Nocturne.API/wwwroot/diagrams"
MANIFEST="$DIAGRAMS_DIR/diagrams.yaml"

# Ensure output directory exists
mkdir -p "$OUTPUT_DIR"

echo "==> Generating auto diagrams"

# --- Dependify: project dependency graph ---
echo "    Generating project dependency graph"
dotnet dependify graph scan \
  --path "$REPO_ROOT/Nocturne.sln" \
  --format mermaid \
  > "$DIAGRAMS_DIR/project-dependencies.mmd"

# --- EfToMermaid: entity relationship diagram ---
echo "    Generating ER diagram"
dotnet run --project "$REPO_ROOT/tools/Nocturne.Tools.DiagramGen" \
  --no-build -- "$DIAGRAMS_DIR/er-diagram.mmd"

# --- Render all diagrams listed in manifest to SVG ---
echo "==> Rendering diagrams to SVG"

# Parse YAML manifest (simple grep-based — avoids yq dependency)
grep "source:" "$MANIFEST" | sed 's/.*source: *//' | while read -r source; do
  input="$DIAGRAMS_DIR/$source"
  output="$OUTPUT_DIR/${source%.mmd}.svg"

  if [[ ! -f "$input" ]]; then
    echo "ERROR: Diagram source not found: $input" >&2
    exit 1
  fi

  echo "    $source → $(basename "$output")"
  npx --yes @mermaid-js/mermaid-cli mmdc \
    -i "$input" \
    -o "$output" \
    --backgroundColor transparent \
    --theme dark \
    --quiet
done

echo "==> Diagrams complete. Output: $OUTPUT_DIR/"
ls -la "$OUTPUT_DIR/"*.svg
