#!/usr/bin/env bash

set -euo pipefail

tag="${1:?Usage: resolve-release-version.sh TAG}"

case "${tag}" in
  v[0-9]*.[0-9]*.[0-9]*)
    version="${tag#v}"
    ;;
  *)
    echo "Error: Unexpected tag '${tag}'. Expected v<semver>." >&2
    exit 1
    ;;
esac

if [ -z "${version}" ]; then
  echo "Error: Failed to resolve package version from '${tag}'." >&2
  exit 1
fi

printf '%s\n' "${version}"
