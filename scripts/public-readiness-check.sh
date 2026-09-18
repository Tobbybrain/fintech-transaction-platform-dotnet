#!/usr/bin/env bash
set -euo pipefail

fail=0

for pattern in   'ChangeMe123!'   'dev-only-secret-key'   'dev-webhook-secret-change-me'   'local-container-jwt-secret-change-me'   'local-container-webhook-secret-change-me'
do
  if grep -RIn --exclude='public-readiness-check.sh' --exclude-dir='.git' "$pattern" .; then
    echo "FAIL: public repo contains fixed demo credential/secret pattern: $pattern"
    fail=1
  fi
done

if [ -f PROJECT_AUDIT.md ]; then
  echo "FAIL: PROJECT_AUDIT.md is internal/job-application material and should not be public"
  fail=1
fi

if grep -RIn --exclude='public-readiness-check.sh' --exclude-dir='.git' 'Tobbybrain/capitalos' .; then
  echo "FAIL: public docs reference the private temporary CapitalOS verification host"
  fail=1
fi

if grep -RIn --exclude='public-readiness-check.sh' --exclude-dir='.git' 'SSRF-aware' README.md VERIFICATION.md 2>/dev/null; then
  echo "FAIL: SSRF claim is stronger than the current destination-validation guarantee"
  fail=1
fi

if [ "$fail" -ne 0 ]; then
  exit 1
fi

echo "PASS: public-readiness static checks"
