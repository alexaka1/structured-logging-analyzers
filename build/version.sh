#!/usr/bin/env bash

set -euo pipefail

pnpm run changeset:version

command -v jq >/dev/null 2>&1 || {
  echo "Error: jq is required."
  exit 1
}

if sed --version >/dev/null 2>&1; then
  SED_INPLACE=(sed -i)
else
  SED_INPLACE=(sed -i '')
fi

PACKAGE_DIR="pack/Alexaka1.Analyzers.StructuredLogging"
PACKAGE_JSON="${PACKAGE_DIR}/package.json"
VERSION_FILE="${PACKAGE_DIR}/Version.props"
VERSION=$(jq -r '.version // empty' "${PACKAGE_JSON}")

if [ -z "${VERSION}" ]; then
  echo "Error: No version in ${PACKAGE_JSON}"
  exit 1
fi

if [ ! -f "${VERSION_FILE}" ]; then
  echo "Error: Missing ${VERSION_FILE}"
  exit 1
fi

"${SED_INPLACE[@]}" "s#<Version>[^<]*</Version>#<Version>${VERSION}</Version>#" "${VERSION_FILE}"
echo "Synced ${VERSION_FILE} to ${VERSION}"
