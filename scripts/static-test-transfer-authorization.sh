#!/usr/bin/env bash
set -euo pipefail
file='src/FintechPlatform.Api/Controllers/TransfersController.cs'
if ! grep -q 'NameIdentifier' "$file" || ! grep -q 'transfer.FromWalletId' "$file" || ! grep -q 'return Forbid()' "$file"; then
  echo 'FAIL: transfer GET endpoint does not show object-level ownership authorization'
  exit 1
fi
echo 'PASS: transfer GET endpoint contains object-level ownership authorization'
