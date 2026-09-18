#!/usr/bin/env bash
set -euo pipefail
status=0
for file in tests/FintechPlatform.Tests/*.cs; do
  if grep -Eq '\[(Fact|Theory)\]' "$file"; then
    if ! grep -Eq '^using Xunit;' "$file" && ! grep -Rqs '^global using Xunit;' tests/FintechPlatform.Tests; then
      echo "FAIL: $file uses xUnit attributes but has no Xunit import"
      status=1
    fi
  fi
done
exit "$status"
