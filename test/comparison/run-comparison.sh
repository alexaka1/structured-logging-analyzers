#!/usr/bin/env bash
# Compare marketplace ReSharper plugin (InspectCode) with StructuredLogging.Analyzers.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PLUGIN_DIR="$ROOT/test/comparison/plugins"
REPORTS_DIR="$ROOT/test/comparison/reports"
CACHE_DIR="${TMPDIR:-/tmp}/rsl-inspect-cache"
PLUGIN_VERSION="2025.1.0.373"
PLUGIN_ID="ReSharper.Structured.Logging"
PLUGIN_NUPKG="$PLUGIN_DIR/${PLUGIN_ID}.${PLUGIN_VERSION}.nupkg"
PLUGIN_URL="https://plugins.jetbrains.com/files/${PLUGIN_ID}/${PLUGIN_VERSION}/resharper.structured.logging.${PLUGIN_VERSION}.nupkg"
CLI_LATEST_DIR="${CLI_LATEST_DIR:-/tmp/jb}"
CLI_WAVE251_DIR="${CLI_WAVE251_DIR:-/tmp/jb-2025.1}"
CLI_LATEST_VERSION="${CLI_LATEST_VERSION:-2025.3.5}"
CLI_WAVE251_VERSION="${CLI_WAVE251_VERSION:-2025.1.9}"

mkdir -p "$PLUGIN_DIR" "$REPORTS_DIR" "$CACHE_DIR"

if [[ ! -f "$PLUGIN_DIR/nuget.config" ]]; then
  cat > "$PLUGIN_DIR/nuget.config" <<'EOF'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="structured-logging-plugin" value="." />
  </packageSources>
</configuration>
EOF
fi

if [[ ! -f "$PLUGIN_NUPKG" ]]; then
  echo "Downloading $PLUGIN_ID $PLUGIN_VERSION"
  curl -fsSL "$PLUGIN_URL" -o "$PLUGIN_NUPKG"
fi

install_cli() {
  local dest="$1"
  local version="$2"
  if [[ -x "$dest/jb" ]]; then
    return 0
  fi
  echo "Installing JetBrains.ReSharper.GlobalTools $version into $dest"
  dotnet tool install JetBrains.ReSharper.GlobalTools --version "$version" --tool-path "$dest"
}

install_cli "$CLI_LATEST_DIR" "$CLI_LATEST_VERSION"

if [[ ! -f "$ROOT/test/comparison/ComparisonCorpus.sln" ]]; then
  dotnet new sln -n ComparisonCorpus -o "$ROOT/test/comparison" --format sln --force
  dotnet sln "$ROOT/test/comparison/ComparisonCorpus.sln" add "$ROOT/test/comparison/corpus/ComparisonCorpus.csproj"
fi

echo "Building comparison corpus"
dotnet build "$ROOT/test/comparison/corpus/ComparisonCorpus.csproj" -c Release --nologo

run_inspect() {
  local jb="$1"
  local label="$2"
  local out_xml="$REPORTS_DIR/inspectcode-${label}.xml"
  local out_types="$REPORTS_DIR/inspectcode-${label}-issuetypes.xml"
  local log="$REPORTS_DIR/inspectcode-${label}.log"
  echo "Running InspectCode ($label) via $jb"
  "$jb" inspectcode "$ROOT/test/comparison/ComparisonCorpus.sln" \
    -o="$out_xml" \
    -f=Xml \
    -e=WARNING \
    -x="$PLUGIN_ID" \
    --source="$PLUGIN_DIR" \
    --properties=RunAnalyzers=false \
    --no-swea \
    --no-updates \
    --caches-home="$CACHE_DIR/$label" \
    --verbosity=INFO \
    --build \
    >"$log" 2>&1 || true
  "$jb" inspectcode "$ROOT/test/comparison/ComparisonCorpus.sln" \
    --dumpIssuesTypes \
    -o="$out_types" \
    -f=Xml \
    -x="$PLUGIN_ID" \
    --source="$PLUGIN_DIR" \
    --properties=RunAnalyzers=false \
    --no-swea \
    --no-updates \
    --caches-home="$CACHE_DIR/$label" \
    --verbosity=WARN \
    >>"$log" 2>&1 || true
}

plugin_loaded() {
  local xml="$1"
  grep -q "AnonymousObjectDestructuringProblem\|InconsistentLogPropertyNaming\|ContextualLoggerProblem" "$xml" 2>/dev/null
}

# The report path is deterministic (we pass it to jb via -o=), so we reference it
# directly instead of capturing it from run_inspect's stdout.
CHOSEN_LABEL="2025.3.5"
CHOSEN_XML="$REPORTS_DIR/inspectcode-$CHOSEN_LABEL.xml"
run_inspect "$CLI_LATEST_DIR/jb" "$CHOSEN_LABEL"
if ! plugin_loaded "$CHOSEN_XML"; then
  echo "Plugin issue types not found in $CLI_LATEST_VERSION report; installing Wave-matching CLI $CLI_WAVE251_VERSION"
  install_cli "$CLI_WAVE251_DIR" "$CLI_WAVE251_VERSION"
  CHOSEN_LABEL="2025.1.9"
  CHOSEN_XML="$REPORTS_DIR/inspectcode-$CHOSEN_LABEL.xml"
  run_inspect "$CLI_WAVE251_DIR/jb" "$CHOSEN_LABEL"
fi

echo "Using InspectCode report $CHOSEN_XML (CLI $CHOSEN_LABEL)"
cp "$CHOSEN_XML" "$REPORTS_DIR/inspectcode.xml"

dotnet run --project "$ROOT/test/comparison/runner/ComparisonRunner.csproj" -c Release --nologo -- \
  --inspectcode "$REPORTS_DIR/inspectcode.xml"

if [[ -d /opt/cursor/artifacts ]]; then
  cp -f "$REPORTS_DIR/comparison.md" /opt/cursor/artifacts/resharper_vs_roslyn.md
  cp -f "$REPORTS_DIR/comparison.json" /opt/cursor/artifacts/resharper_vs_roslyn.json
  cp -f "$REPORTS_DIR/inspectcode.xml" /opt/cursor/artifacts/inspectcode.xml
  if [[ -f "$REPORTS_DIR/inspectcode-${CHOSEN_LABEL}.log" ]]; then
    cp -f "$REPORTS_DIR/inspectcode-${CHOSEN_LABEL}.log" /opt/cursor/artifacts/inspectcode.log
  fi
fi
