#!/usr/bin/env bash

set -euo pipefail

input_file="${1:?Usage: extract-changelog.sh CHANGELOG.md OUTPUT.md}"
output_file="${2:?Usage: extract-changelog.sh CHANGELOG.md OUTPUT.md}"

if [ ! -f "${input_file}" ]; then
  echo "Error: ${input_file} was not found." >&2
  exit 1
fi

start_line="$(grep -nE '^## [0-9]+\.[0-9]+\.[0-9]' "${input_file}" | head -n 1 | cut -d: -f1 || true)"
end_line="$(grep -nE '^## [0-9]+\.[0-9]+\.[0-9]' "${input_file}" | sed -n '2p' | cut -d: -f1 || true)"

if [ -z "${start_line}" ]; then
  echo "Error: No version heading found in ${input_file}." >&2
  exit 1
fi

if [ -z "${end_line}" ]; then
  sed -n "${start_line},\$p" "${input_file}" > "${output_file}"
else
  sed -n "${start_line},$((end_line - 1))p" "${input_file}" > "${output_file}"
fi
