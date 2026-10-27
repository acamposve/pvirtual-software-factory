-- specs/orchestrator/architecture.md — schema iteración 1
-- Idempotente: seguro de correr más de una vez.

CREATE TABLE IF NOT EXISTS agent_jobs (
  id uuid PRIMARY KEY,
  type text NOT NULL,
  state text NOT NULL,
  trigger_type text NOT NULL,
  payload jsonb NOT NULL,
  attempt_count int NOT NULL DEFAULT 0,
  delivery_id text NOT NULL UNIQUE,
  github_run_id bigint NULL,
  created_at timestamptz NOT NULL,
  dispatched_at timestamptz NULL,
  completed_at timestamptz NULL
);

CREATE INDEX IF NOT EXISTS idx_agent_jobs_state ON agent_jobs (state);

-- Idempotencia de webhooks (specs/orchestrator/architecture.md, decisión 3)
CREATE TABLE IF NOT EXISTS processed_deliveries (
  delivery_id text PRIMARY KEY,
  processed_at timestamptz NOT NULL DEFAULT now()
);
