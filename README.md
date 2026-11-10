# School Admission Management System

A monolithic school-admission platform focused on **database-level concurrency, transaction safety, seat reservation and persistent admission records**.

- **Backend:** ASP.NET Core (.NET 8) modular monolith: Domain / Application / Infrastructure / Api
- **Database:** PostgreSQL 16 (pessimistic row locking, partial unique index, named Docker volume)
- **Frontend:** React 18 + TypeScript + Vite, served by nginx (admin panel and student portal)
- **Deployment:** Docker Compose (frontend, backend, PostgreSQL)

## Run everything

```bash
cp .env.example .env        # optional, every value has a default
docker compose up --build
```

| URL | What |
| --- | --- |
| http://localhost:5173 | Web app (student portal at `/`, admin at `/admin/login`) |
| http://localhost:8080 | Backend API directly (all endpoints from the assignment) |
| localhost:5432 | PostgreSQL (`postgres` / `change_me`, db `school_admission`) |

**Admin login:** `admin` / `Admin@12345` (change via `ADMIN_USERNAME` / `ADMIN_PASSWORD`).
With `SEED_DEMO_DATA=true` (default) four demo classes are created on first start.

Restart without losing data: `docker compose down && docker compose up -d`. **Never** use `down -v` unless you want to delete the database volume.

Second backend instance (same database) to demonstrate multi-instance safety:

```bash
docker compose --profile multi up -d --build   # backend :8080 and backend2 :8081
```

## How the student flow works

1. Enter the 17-digit birth registration number (sent as the `X-Student-Id` header).
2. Browse classes with live availability and apply. The backend reserves a seat in one PostgreSQL transaction.
3. The server returns `expiresAt`. The UI shows a countdown computed from that timestamp, corrected for browser clock skew via `serverTime`.
4. **Pay admission fee** (simulated) makes `HELD` into `CONFIRMED` and records a payment. Otherwise the hold becomes `EXPIRED` and the seat is free again.

## Admin features

Dashboard statistics, create or edit classes, change capacity (never below held + confirmed), set the **payment window per class** (minutes or seconds, stored as `hold_duration_seconds`), open or close admissions, and search all applications. A duration change applies to *future* reservations only.

## Concurrency design

- Every operation that changes seat availability (reserve, confirm, cancel, expire, capacity change) takes **`SELECT ... FOR UPDATE` on the class row first** and the hold row second, a single lock order that avoids deadlocks.
- Inside that lock: expire overdue holds, check the duplicate, count seats, insert. Different classes never block each other.
- Expiration is enforced in two layers: a `BackgroundService` (every 5 s, also recovers after downtime) and transactional expiry inside every reservation, confirmation and capacity operation. Time decisions use PostgreSQL `clock_timestamp()`, not the browser or app clock, and not `now()` (which is frozen at transaction start and can be stale after waiting on a lock).
- Business rejections (409) are raised **after commit** so expirations processed on the way are never rolled back.
- Confirmation is idempotent (`payments.hold_id` is unique) and cancellation only applies to `HELD`.
- Partial unique index `ux_admission_holds_active_student_class` backs the one-active-application rule.
- Lock and statement timeouts (10 s / 30 s) turn pathological waits into `503 BUSY` instead of hung requests.

## API

Assignment endpoints: `POST/GET /classes`, `GET /classes/{id}`, `POST /classes/{id}/holds`, `GET|DELETE /holds/{id}`, `POST /holds/{id}/confirm`, `GET /me/holds`.
Admin endpoints (JWT): `POST /admin/auth/login`, `GET /admin/dashboard`, `GET /admin/classes`, `PUT /admin/classes/{id}`, `PATCH /admin/classes/{id}/capacity|hold-duration|admission-status`, `GET /admin/classes/{id}/applications`, `GET /admin/applications`, `GET /admin/applications/{holdId}`. `POST /classes` requires an admin token. Health: `GET /health`.

Errors: `{ "error": { "code": "SEAT_UNAVAILABLE", "message": "..." } }` with codes such as `SEAT_UNAVAILABLE`, `DUPLICATE_APPLICATION`, `RESERVATION_EXPIRED`, `ADMISSION_CLOSED`, `CAPACITY_BELOW_COMMITMENTS`, `FORBIDDEN`, `BUSY`.

## Tests

Integration and concurrency tests run against **real PostgreSQL** (no in-memory provider).

```bash
docker compose -f docker-compose.test.yml up -d        # throw-away DB on :5433
cd backend && dotnet test                              # override with TEST_DB_CONNECTION
```

- Unit: seat math, expiry calculation, validation.
- Integration: create class, reserve, duplicate, confirm and repeat confirm, expired confirm, cancel, capacity and duration rules, ownership checks.
- Concurrency (A-F): 1000/120, 500/10, 100/1, two backend instances, confirm-vs-expire race, cancel-vs-reserve race. Invariants (no overselling, one active application per student, one payment per hold) are verified after each.
- Persistence: `./scripts/persistence-test.sh` stops and restarts the stack, including a hold that expires while offline.

## Repository layout

```
backend/   SchoolAdmission.{Domain,Application,Infrastructure,Api,Tests}, Dockerfile
frontend/  React app, Dockerfile, nginx.conf
scripts/   persistence-test.sh
docker-compose.yml, docker-compose.test.yml, .env.example
README.md, ASSUMPTIONS.md, AI_USAGE.md
```
