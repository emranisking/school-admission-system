-- Initial schema. Applied once by DatabaseInitializer (tracked in schema_migrations).

CREATE TABLE admins (
    id UUID PRIMARY KEY,
    username VARCHAR(100) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE students (
    id UUID PRIMARY KEY,
    birth_registration_number VARCHAR(17) NOT NULL UNIQUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CHECK (birth_registration_number ~ '^[0-9]{17}$')
);

CREATE TABLE classes (
    id UUID PRIMARY KEY,
    name VARCHAR(150) NOT NULL,
    academic_year INTEGER NOT NULL,
    shift VARCHAR(50) NOT NULL,
    total_seats INTEGER NOT NULL,
    hold_duration_seconds INTEGER NOT NULL DEFAULT 600,
    admission_open BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CHECK (total_seats >= 0),
    CHECK (hold_duration_seconds > 0)
);

CREATE TABLE admission_holds (
    id UUID PRIMARY KEY,
    class_id UUID NOT NULL REFERENCES classes(id),
    student_id UUID NOT NULL REFERENCES students(id),
    status VARCHAR(20) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ,
    confirmed_at TIMESTAMPTZ,
    cancelled_at TIMESTAMPTZ,
    CHECK (status IN ('HELD', 'CONFIRMED', 'EXPIRED', 'CANCELLED'))
);

CREATE TABLE payments (
    id UUID PRIMARY KEY,
    hold_id UUID NOT NULL UNIQUE REFERENCES admission_holds(id),
    amount NUMERIC(12,2) NOT NULL,
    payment_status VARCHAR(20) NOT NULL,
    transaction_reference VARCHAR(100) UNIQUE,
    paid_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CHECK (amount >= 0),
    CHECK (payment_status IN ('PENDING', 'SUCCESS', 'FAILED'))
);

CREATE TABLE audit_logs (
    id UUID PRIMARY KEY,
    actor_type VARCHAR(30) NOT NULL,
    actor_id VARCHAR(100),
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(50) NOT NULL,
    entity_id UUID,
    details JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- A student can have at most one active (HELD or CONFIRMED) application per class.
CREATE UNIQUE INDEX ux_admission_holds_active_student_class
    ON admission_holds (class_id, student_id)
    WHERE status IN ('HELD', 'CONFIRMED');

CREATE INDEX ix_admission_holds_class_status_expires ON admission_holds (class_id, status, expires_at);
CREATE INDEX ix_admission_holds_held_expires ON admission_holds (expires_at) WHERE status = 'HELD';
CREATE INDEX ix_admission_holds_student ON admission_holds (student_id);
CREATE INDEX ix_admission_holds_created_at ON admission_holds (created_at DESC);
CREATE INDEX ix_audit_logs_created_at ON audit_logs (created_at DESC);
CREATE INDEX ix_audit_logs_entity ON audit_logs (entity_type, entity_id);
