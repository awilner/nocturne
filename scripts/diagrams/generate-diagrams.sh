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
DIAGRAMGEN="$REPO_ROOT/tools/Nocturne.Tools.DiagramGen"

# Ensure output directory exists
mkdir -p "$OUTPUT_DIR"

echo "==> Generating auto diagrams"

# --- Dependify: project dependency graph ---
echo "    Generating project dependency graph"
dotnet dependify graph scan "$REPO_ROOT" \
  --format mermaid \
  > "$DIAGRAMS_DIR/project-dependencies.mmd"

# --- EfToMermaid: all efcore entries from manifest ---
# Parse manifest for entries with "auto: efcore", extract source and optional module.
echo "    Generating EF Core diagrams"

# Use awk to extract source/module pairs for efcore entries
awk '
  /^  - source:/ { source = $NF }
  /auto: efcore/ { efcore = 1 }
  /module:/ { module = $NF }
  /^  - source:/ && efcore && source {
    # Emit previous entry when we hit the next entry
  }
  /^$/ || /^  - source:/ {
    if (efcore && source) {
      print source ":" module
    }
    efcore = 0; module = ""
  }
  END {
    if (efcore && source) {
      print source ":" module
    }
  }
' "$MANIFEST" | while IFS=: read -r source module; do
  output_mmd="$DIAGRAMS_DIR/$source"

  if [[ -n "$module" ]]; then
    echo "      $source (module: $module)"
    dotnet run --project "$DIAGRAMGEN" --no-launch-profile -- "$output_mmd" --module "$module"
  else
    echo "      $source (full model)"
    dotnet run --project "$DIAGRAMGEN" --no-launch-profile -- "$output_mmd"
  fi
done

# --- Render all diagrams listed in manifest to SVG ---
echo "==> Rendering diagrams to SVG"

grep "source:" "$MANIFEST" | sed 's/.*source: *//' | while read -r source; do
  input="$DIAGRAMS_DIR/$source"
  output="$OUTPUT_DIR/${source%.mmd}.svg"

  if [[ ! -f "$input" ]]; then
    echo "ERROR: Diagram source not found: $input" >&2
    exit 1
  fi

  echo "    $source → $(basename "$output")"
  npx --yes @mermaid-js/mermaid-cli \
    -i "$input" \
    -o "$output" \
    --backgroundColor transparent \
    --theme dark \
    --quiet
done

echo "==> Diagrams complete. Output: $OUTPUT_DIR/"
ls -la "$OUTPUT_DIR/"*.svg
