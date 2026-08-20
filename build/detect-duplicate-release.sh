#!/usr/bin/env bash

set -euo pipefail

tag="${1:?Usage: detect-duplicate-release.sh TAG VERSION NUGET_PACKAGE_ID}"
version="${2:?Usage: detect-duplicate-release.sh TAG VERSION NUGET_PACKAGE_ID}"
package_id="${3:?Usage: detect-duplicate-release.sh TAG VERSION NUGET_PACKAGE_ID}"

if [ -z "${GH_TOKEN:-}${GITHUB_TOKEN:-}" ]; then
  echo "Error: GH_TOKEN or GITHUB_TOKEN is required to query GitHub releases." >&2
  exit 1
fi

if [ -z "${NUGET_SOURCE:-}" ]; then
  echo "Error: NUGET_SOURCE is required to query NuGet." >&2
  exit 1
fi

err_file="${RUNNER_TEMP:-/tmp}/gh-release.err"
if gh release view "${tag}" --json tagName >/dev/null 2>"${err_file}"; then
  echo "Error: GitHub release '${tag}' already exists." >&2
  exit 1
fi
if ! grep -qiE 'release not found|not found' "${err_file}"; then
  echo "Error: Failed to query GitHub release '${tag}'." >&2
  cat "${err_file}" >&2
  exit 1
fi

search_json="$(
  dotnet package search "${package_id}" \
    --source "${NUGET_SOURCE}" \
    --exact-match \
    --prerelease \
    --format json \
    --verbosity minimal
)"

if printf '%s' "${search_json}" | jq -e --arg id "${package_id}" --arg v "${version}" '
  .searchResult[]?.packages[]?
  | select((.id | ascii_downcase) == ($id | ascii_downcase))
  | select((.version | ascii_downcase) == ($v | ascii_downcase))
' >/dev/null; then
  echo "Error: NuGet package ${package_id} ${version} already exists on ${NUGET_SOURCE}." >&2
  exit 1
fi
