#!/usr/bin/env bash
# Persistence test (plan section 13.4). Requires: docker compose, curl, python3.
#   ./scripts/persistence-test.sh
# Uses `docker compose down` (NOT `down -v`) so the named volume survives.
set -euo pipefail

API="${API:-http://localhost:8080}"
ADMIN_USER="${ADMIN_USERNAME:-admin}"
ADMIN_PASS="${ADMIN_PASSWORD:-Admin@12345}"
STUDENT="20152691234567890"
STUDENT2="20152691234567891"

json() { python3 -c "import sys,json; print(json.load(sys.stdin)$1)"; }
wait_up() { for _ in $(seq 1 60); do curl -fs "$API/health" >/dev/null 2>&1 && return 0; sleep 2; done; echo "backend did not become healthy"; exit 1; }

echo "1. Start Docker Compose"; docker compose up -d --build; wait_up

TOKEN=$(curl -fs -X POST "$API/admin/auth/login" -H 'Content-Type: application/json' \
  -d "{\"username\":\"$ADMIN_USER\",\"password\":\"$ADMIN_PASS\"}" | json "['token']")

echo "2. Create two classes (long and 20-second hold)"
LONG=$(curl -fs -X POST "$API/classes" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"name":"Persist Long","academicYear":2027,"shift":"MORNING","totalSeats":5,"holdDurationSeconds":1800}' | json "['id']")
SHORT=$(curl -fs -X POST "$API/classes" -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"name":"Persist Short","academicYear":2027,"shift":"DAY","totalSeats":1,"holdDurationSeconds":20}' | json "['id']")

echo "3. Create reservations"
H1=$(curl -fs -X POST "$API/classes/$LONG/holds" -H "X-Student-Id: $STUDENT" | json "['holdId']")
BEFORE=$(curl -fs "$API/holds/$H1" -H "X-Student-Id: $STUDENT" | json "['expiresAt']")
H2=$(curl -fs -X POST "$API/classes/$SHORT/holds" -H "X-Student-Id: $STUDENT2" | json "['holdId']")

echo "4. Stop containers (volume kept); wait for the 20 s hold to pass while OFFLINE"
docker compose down; sleep 25

echo "5. Start again"; docker compose up -d; wait_up
sleep 8 # worker recovery pass

echo "6/7/8. Verify"
AFTER=$(curl -fs "$API/holds/$H1" -H "X-Student-Id: $STUDENT" | json "['expiresAt']")
STATUS1=$(curl -fs "$API/holds/$H1" -H "X-Student-Id: $STUDENT" | json "['status']")
STATUS2=$(curl -fs "$API/holds/$H2" -H "X-Student-Id: $STUDENT2" | json "['status']")
AVAIL=$(curl -fs "$API/classes/$SHORT" | json "['availableSeats']")

[ "$BEFORE" = "$AFTER" ] || { echo "FAIL: expiresAt changed ($BEFORE -> $AFTER)"; exit 1; }
[ "$STATUS1" = "HELD" ] || { echo "FAIL: long hold status is $STATUS1"; exit 1; }
[ "$STATUS2" = "EXPIRED" ] || { echo "FAIL: offline-expired hold status is $STATUS2"; exit 1; }
[ "$AVAIL" = "1" ] || { echo "FAIL: seat not released (available=$AVAIL)"; exit 1; }
echo "PASS: data and timestamps survived restart; offline-expired hold released its seat."
