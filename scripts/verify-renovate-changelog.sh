#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RENOVATE_VERSION="${RENOVATE_VERSION:-44.37.1}"
INSTALL_DIR="${TMPDIR:-/tmp}/renovate-verify-${RENOVATE_VERSION}"

mkdir -p "${INSTALL_DIR}"
if [[ ! -d "${INSTALL_DIR}/node_modules/renovate" ]]; then
  npm install --prefix "${INSTALL_DIR}" "renovate@${RENOVATE_VERSION}"
fi

export RENOVATE_ROOT="${INSTALL_DIR}/node_modules/renovate"
exec node "${ROOT}/scripts/verify-renovate-changelog.mjs"
