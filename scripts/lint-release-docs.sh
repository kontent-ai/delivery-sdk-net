#!/usr/bin/env bash

set -euo pipefail

readonly RULES=(
  "AI generated guide"
  "currently in beta"
  "19.0.0-beta"
  "10.0.0-beta"
)

for rule in "${RULES[@]}"; do
  if rg --line-number --ignore-case --glob "README.md" --glob "docs/*.md" "${rule}" >/tmp/release-docs-lint.out 2>/dev/null; then
    echo "Release docs lint failed: found disallowed marker '${rule}'."
    cat /tmp/release-docs-lint.out
    exit 1
  fi
done

echo "Release docs lint passed."
