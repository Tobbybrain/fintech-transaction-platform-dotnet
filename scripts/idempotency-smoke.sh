#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:8080}"
TOKEN="${TOKEN:?Set TOKEN to a JWT from /api/auth/token}"
FROM_WALLET="${FROM_WALLET:?Set FROM_WALLET}"
TO_WALLET="${TO_WALLET:?Set TO_WALLET}"
KEY="same-request-$(date +%s)"

body=$(cat <<JSON
{"fromWalletId":"$FROM_WALLET","toWalletId":"$TO_WALLET","amount":1000.00,"webhookUrl":null}
JSON
)

for i in 1 2 3 4 5; do
  curl -sS -X POST "$BASE_URL/api/transfers"     -H "Authorization: Bearer $TOKEN"     -H "Content-Type: application/json"     -H "Idempotency-Key: $KEY"     -d "$body" > "/tmp/transfer-$i.json" &
done
wait

cat /tmp/transfer-*.json
printf '\nAll responses should resolve to the same transfer id/reference.\n'
