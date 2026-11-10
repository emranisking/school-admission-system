# Assumptions and decisions

1. **Student identity** is the `X-Student-Id` header (17-digit birth registration number). It is a simulated identity, not production authentication. Ownership is still checked on every student operation (403 for another student's hold).
2. **Payment is simulated.** Confirmation always succeeds if the hold is still valid. The fee comes from `ADMISSION_FEE` (default 5000) and is stored in `payments`. No external call is made while locks are held.
3. **Admin-configured hold duration wins.** `HOLD_DURATION_SECONDS` (default 600) is only used when a class is created without an explicit duration. Allowed range 1-86400 seconds. Changing it affects future reservations only.
4. **Schema management:** the schema is a plain SQL script (`Persistence/Sql/001_init.sql`) applied at startup under a PostgreSQL advisory lock, tracked in `schema_migrations`. Several instances can start together safely. EF Core migrations were not used, to keep the schema identical to the plan's DDL (including the partial unique index and check constraints); EF is used for queries and writes only.
5. **No repository layer:** the EF Core `DbContext` (behind `IAppDbContext`) is the unit of work used by application services, which keeps the lock, expire, check, insert logic in one place inside one transaction.
6. **Class naming:** `name` is stored as entered and `shift` separately. The API adds `displayName` ("Class 6 - Morning Shift"); the combined name is not duplicated if `name` already contains the shift.
7. **Public reads:** `GET /classes` and `GET /classes/{id}` are public. `POST /classes` requires an admin token.
8. **Cancellation policy:** students may cancel only `HELD` applications. A `CONFIRMED` admission is not cancellable online (needs an admin or refund process, out of scope).
9. **Expired holds in reads:** a `HELD` hold past `expires_at` is reported as `EXPIRED` immediately, even before the worker persists it.
10. **Frontend API access:** nginx proxies `/api/*` to the backend, so no API URL is baked into the image. The backend also exposes CORS (any origin) for direct use on :8080.
11. **Multi-instance test (Test D)** starts two independent in-process backend hosts (separate DI containers and workers) on one database. For OS-level separate processes use `docker compose --profile multi`.
12. **Default credentials and keys** in `.env.example` are for development only; change `JWT_KEY`, `ADMIN_PASSWORD` and `POSTGRES_PASSWORD` for any real deployment.
