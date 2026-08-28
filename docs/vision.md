# Action Plan — AI Software Factory 24/7

## 1. Objective

Build an **AI Software Factory** capable of receiving work from GitHub, running autonomous agents in the cloud, implementing code, running validations, and creating Pull Requests without requiring the laptop to remain powered on.

The laptop is reserved for interactive development and human decisions.

### Target Architecture

```text
                         ┌─────────────────────┐
                         │       HUMAN         │
                         │  Product / Architect│
                         └──────────┬──────────┘
                                    │
                              Requirement
                                    │
                                    ▼
                         ┌─────────────────────┐
                         │       GitHub        │
                         │ Issues / Specs / PR │
                         └──────────┬──────────┘
                                    │
                              Event / Schedule
                                    │
                                    ▼
                         ┌─────────────────────┐
                         │     ORCHESTRATOR    │
                         │        Azure        │
                         └──────────┬──────────┘
                                    │
                   ┌────────────────┼────────────────┐
                   ▼                ▼                ▼
              Spec Agent      Developer Agent   Review Agent
                   │                │                │
                   └────────────────┼────────────────┘
                                    ▼
                         ┌─────────────────────┐
                         │ Ephemeral Sandbox   │
                         │ VM / Container      │
                         └──────────┬──────────┘
                                    │
                         Code + Build + Tests
                                    │
                                    ▼
                         ┌─────────────────────┐
                         │   GitHub Actions    │
                         │ CI/CD + Security    │
                         └──────────┬──────────┘
                                    │
                              PASS / FAIL
                              /          \
                            PASS         FAIL
                             │             │
                             ▼             ▼
                            PR       Agent retries/fixes
                             │
                             ▼
                         HUMAN REVIEW
                             │
                             ▼
                           MERGE
```

---

# 2. Components and Location

| Component | Location | Purpose |
|---|---|---|
| Your VS Code | Laptop | Interactive development and supervision |
| Interactive development | Laptop | AI-assisted coding |
| GitHub | Cloud | Code, issues, specifications, PRs, and workflow |
| Specifications | GitHub | Source of truth for expected behavior |
| Autonomous agents | Cloud | Implementation, testing, review, and maintenance |
| CI/CD | GitHub Actions | Automation and gates |
| Tests | GitHub Actions | Automated validation |
| Orchestrator | Azure | Coordinate agents and jobs |
| Knowledge base | Azure | Technical context, documentation, and system knowledge |
| Secrets | Azure Key Vault | Credentials and secrets |

---

# 3. Design Principles

## 3.1 Specification First

The AI **MUST NOT implement directly from an ambiguous issue**.

Workflow:

```text
Requirement
    ↓
Spec
    ↓
Architecture
    ↓
Tasks
    ↓
Implementation
    ↓
Tests
    ↓
Review
    ↓
PR
```

Fundamental rule:

> Implementation MUST NOT precede Specification.

---

## 3.2 Agents Do Not Have Unlimited Authority

Each agent MUST have:

- a specific responsibility;
- minimum required permissions;
- explicitly defined tools;
- cost limits;
- time limits;
- success criteria;
- human-escalation criteria.

Never grant an agent permanent administrative access to Azure or GitHub.

---

## 3.3 Every Change Goes Through Git

Agents work on branches.

```text
main
  │
  ├── feature/AI-123-text-normalization
  ├── fix/AI-124-parser
  └── chore/AI-125-dependencies
```

Never:

```text
Agent → direct commit → main
```

Always:

```text
Agent → branch → tests → PR → human/automated review → merge
```

---

# 4. Phase 0 — Prepare the Repository

## Objective

Make GitHub the factory's operational center.

### Tasks

- [ ] Define the repository structure.
- [ ] Create `README.md`.
- [ ] Create `CONTRIBUTING.md`.
- [ ] Create `AGENTS.md`.
- [ ] Create the `/specs` directory.
- [ ] Create the `/docs` directory.
- [ ] Create the `/architecture` directory.
- [ ] Create the `/scripts` directory.
- [ ] Define branch conventions.
- [ ] Enable `main` branch protection.
- [ ] Require Pull Requests.
- [ ] Require successful CI before merge.
- [ ] Configure CODEOWNERS.
- [ ] Define the commit policy.
- [ ] Define the versioning policy.

### Result

GitHub becomes the **project source of truth**.

---

# 5. Phase 1 — Formalize Specification-Driven Development

Create a standard structure:

```text
/specs
  /feature-name
    spec.md
    domain.md
    architecture.md
    tasks.md
    acceptance.md
```

## `spec.md`

It MUST describe:

- problem;
- objective;
- scope;
- behavior;
- rules;
- error cases;
- acceptance criteria.

## `domain.md`

It MUST describe:

- entities;
- value objects;
- aggregates;
- domain services;
- events;
- invariants.

## `architecture.md`

It MUST describe:

- components;
- dependencies;
- APIs;
- persistence;
- events;
- security;
- architectural decisions.

## `tasks.md`

It MUST transform the specification into implementable tasks.

---

# 6. Phase 2 — Create the Constitution for Agents

Create rules that every agent MUST follow.

Ejemplo:

```text
ARTICLE I — SPECIFICATION FIRST

Implementation MUST NOT precede specification.

ARTICLE II — ARCHITECTURE

Agents MUST respect the approved architecture.

ARTICLE III — SECURITY

Secrets MUST NOT be committed to Git.

Agents MUST use least privilege.

ARTICLE IV — TESTING

Every feature MUST include automated tests.

ARTICLE V — QUALITY

Build MUST succeed before PR creation.

ARTICLE VI — GIT

Agents MUST NOT push directly to main.

ARTICLE VII — HUMAN CONTROL

Agents MUST escalate ambiguous architectural or business decisions.
```

This Constitution becomes mandatory agent context.

---

# 7. Phase 3 — Build CI/CD

Use GitHub Actions.

Pipeline mínimo:

```text
Pull Request
     ↓
Restore
     ↓
Build
     ↓
Unit Tests
     ↓
Integration Tests
     ↓
Coverage
     ↓
Static Analysis
     ↓
Security Scan
     ↓
Architecture Validation
     ↓
PASS / FAIL
```

## Initial Gates

- Build: required.
- Unit tests: required.
- Integration tests: required when applicable.
- Coverage: target >= 90%.
- Security scan: required.
- Architecture checks: required.

The agent **MUST NOT consider a task complete merely because the code compiles**.

---

# 8. Phase 4 — First Autonomous Agent

Do not start with ten agents.

Build one agent first:

## Developer Agent

Responsibility:

> Implement a specific task from an approved specification.

Input:

```text
spec.md
domain.md
architecture.md
tasks.md
Constitution
```

Output:

```text
branch
code
tests
commit
Pull Request
```

### Constraints

The agent:

- MUST NOT modify `main`;
- MUST NOT change the approved architecture without authorization;
- MUST NOT delete tests to achieve coverage;
- MUST NOT modify secrets;
- MUST NOT deploy to production;
- MUST run tests before creating the PR.

---

# 9. Phase 5 — Run Agents in the Cloud

This removes the dependency on the laptop.

Each agent execution MUST use an ephemeral environment:

```text
Agent Job
   ↓
Create sandbox
   ↓
Clone repository
   ↓
Load specification
   ↓
Load agent instructions
   ↓
Implement
   ↓
Run tests
   ↓
Commit
   ↓
Push branch
   ↓
Create PR
   ↓
Destroy sandbox
```

The sandbox may be:

- container;
- Azure Container Apps Job;
- Azure VM temporal;
- GitHub-hosted runner;
- otro runtime efímero apropiado.

The initial preference MUST be **ephemeral**, not a permanently running VM.

---

# 10. Phase 6 — Azure Orchestrator

Build a small service that controls the agent lifecycle.

Responsibilities:

```text
Receive event
    ↓
Determine task
    ↓
Select agent
    ↓
Prepare context
    ↓
Launch execution
    ↓
Monitor
    ↓
Collect result
    ↓
Retry / escalate / finish
```

## Possible Events

- Issue created.
- Issue labeled `ai-ready`.
- Specification approved.
- PR created.
- CI failed.
- Vulnerable dependency detected.
- Scheduled task.
- Manual request.

---

# 11. Phase 7 — Specialized Agents

When the Developer Agent is stable, add additional agents.

## Spec Agent

Convert requirements into specifications.

```text
Requirement
    ↓
Spec Agent
    ↓
spec.md
    ↓
Human approval
```

## Testing Agent

Analyze coverage and generate tests.

```text
Code
 ↓
Testing Agent
 ↓
Tests
 ↓
CI
```

## Review Agent

Review:

- architecture;
- security;
- quality;
- tests;
- conventions;
- the Constitution.

## Security Agent

Search for:

- secrets;
- vulnerable dependencies;
- insecure configurations;
- excessive permissions;
- authentication/authorization issues.

## Maintenance Agent

Run scheduled tasks such as:

- update dependencies;
- detect technical debt;
- review documentation;
- detect duplicate code;
- identify missing tests.

---

# 12. Phase 8 — Knowledge Base

Create a centralized knowledge base.

It MUST contain:

```text
Architecture
Domain
ADRs
Specifications
Coding Standards
API contracts
Security policies
Glossary
Technology decisions
Past incidents
Known limitations
```

The agent should not have to discover the entire architecture by reading thousands of files on every execution.

The recommended strategy is:

```text
GitHub
   ↓
Documents
   ↓
Indexing
   ↓
Embeddings / Search
   ↓
Knowledge Base
   ↓
Agent Context
```

Azure can host the components required for search and indexing.

Important:

> The vector database does not replace GitHub as the source of truth.

GitHub stores the normative artifacts; the knowledge base facilitates their retrieval.

---

# 13. Phase 9 — Secrets and Security

Use Azure Key Vault.

Never place the following:

```text
API_KEY=xxxxx
```

inside:

- code;
- specifications;
- prompts;
- the repository;
- logs.

Flujo:

```text
Agent
  ↓
Managed Identity
  ↓
Azure
  ↓
Key Vault
  ↓
Secret
```

Apply:

- Managed Identity;
- RBAC;
- least privilege;
- rotation;
- auditing;
- environment separation.

---

# 14. Phase 10 — Autonomous Feedback Loop

This phase enables near-continuous 24/7 operation.

```text
             ┌───────────────┐
             │     TASK      │
             └───────┬───────┘
                     ↓
                  AGENT
                     ↓
                   CODE
                     ↓
                  TESTS
                     ↓
              ┌──────┴──────┐
              │             │
             FAIL          PASS
              │             │
              ↓             ↓
          FIX AGENT         PR
              │             │
              └──────┐      │
                     ↓      ↓
                   TESTS   REVIEW
                     │      │
                     └──┬───┘
                        ↓
                      MERGE
```

## Retry Policy

For example:

```text
Maximum autonomous retries: 3
```

After 3 attempts:

```text
AI → HUMAN ESCALATION
```

Never allow infinite loops.

---

# 15. Phase 11 — 24/7 Scheduler

Add scheduled jobs.

Example:

### 00:00

Dependency Agent

```text
Check dependencies
Check vulnerabilities
Create PR if needed
```

### 02:00

Testing Agent

```text
Analyze coverage
Find missing tests
Create PR
```

### 03:00

Documentation Agent

```text
Detect stale documentation
Create update PR
```

### 04:00

Architecture Agent

```text
Analyze architecture violations
Report / create issue
```

### 05:00

Maintenance Agent

```text
Analyze technical debt
Create prioritized issues
```

### 06:00

Daily report

```text
AI Engineering Report

Completed:
  5 tasks

PRs:
  3

Failed:
  1

Human decisions:
  2

Coverage:
  93.4%

Security:
  PASS
```

---

# 16. Phase 12 — Human-in-the-Loop

The goal **MUST NOT be to eliminate the human**.

The goal is for the human to stop performing mechanical work.

### The Agent May Decide

- how to implement a task within an approved architecture;
- which tests to create;
- how to fix a failing test;
- how to update a dependency;
- how to generate documentation.

### The Human MUST Decide

- architecture changes;
- domain changes;
- ambiguous business decisions;
- significant security changes;
- production access;
- changes with significant financial impact.

---

# 17. Phase 13 — Observability

Record every execution.

```text
Agent ID
Task ID
Model
Start time
End time
Tokens
Estimated cost
Files changed
Tests executed
Tests passed
Tests failed
PR
Retries
Human escalation
```

Dashboard:

```text
AI SOFTWARE FACTORY

Agents running:       4
Tasks today:         37
Completed:            29
Failed:                5
Escalated:             3
PRs created:          21
PRs merged:           17

Avg task time:       18m
Avg retries:          0.8
Estimated cost:      $XX
```

---

# 18. Phase 14 — Cost Control

Autonomous agents can generate costs quickly.

Implement:

- daily budget;
- monthly budget;
- per-task limit;
- token limit;
- per-execution timeout;
- maximum retries;
- different models based on complexity.

Ejemplo:

```text
Simple task
    → modelo económico

Normal development
    → modelo estándar

Architecture / difficult debugging
    → modelo de razonamiento avanzado
```

Do not use the most expensive model for every task.

---

# 19. Phase 15 — Evolve Toward a True AI Software Factory

When the system is stable:

```text
                    PRODUCT
                       │
                       ▼
                 SPEC AGENT
                       │
                       ▼
              ARCHITECTURE AGENT
                       │
                       ▼
                TASK PLANNER
                       │
          ┌────────────┼────────────┐
          ▼            ▼            ▼
       BACKEND      FRONTEND      INFRA
        AGENT         AGENT       AGENT
          │            │            │
          └────────────┼────────────┘
                       ▼
                 TESTING AGENT
                       │
                       ▼
                SECURITY AGENT
                       │
                       ▼
                 REVIEW AGENT
                       │
                       ▼
                      PR
                       │
                 HUMAN APPROVAL
                       │
                       ▼
                     MERGE
                       │
                       ▼
                    DEPLOY
```

---

# 20. Recommended Roadmap

## Week 1 — GitHub + SDD

- [ ] Constitution
- [ ] `AGENTS.md`
- [ ] Specs
- [ ] Architecture docs
- [ ] Branch protection
- [ ] CODEOWNERS

## Week 2 — CI/CD

- [ ] Build
- [ ] Unit tests
- [ ] Integration tests
- [ ] Coverage
- [ ] Security scan
- [ ] Architecture validation

## Week 3 — First Agent

- [ ] Developer Agent
- [ ] Sandbox
- [ ] Branch creation
- [ ] Code generation
- [ ] Test execution
- [ ] PR creation

## Week 4 — Orchestrator

- [ ] Azure orchestration
- [ ] Event handling
- [ ] Job queue
- [ ] Retry policy
- [ ] Agent lifecycle
- [ ] Logging

## Week 5 — Specialized Agents

- [ ] Spec Agent
- [ ] Testing Agent
- [ ] Review Agent
- [ ] Security Agent

## Week 6 — Knowledge Base

- [ ] Document ingestion
- [ ] Indexing
- [ ] Search
- [ ] Agent context
- [ ] Architecture knowledge

## Week 7 — Automation

- [ ] Scheduled tasks
- [ ] Maintenance
- [ ] Dependency checks
- [ ] Technical debt detection
- [ ] Daily reports

## Week 8 — Hardening

- [ ] Managed Identity
- [ ] Key Vault
- [ ] RBAC
- [ ] Cost limits
- [ ] Observability
- [ ] Failure recovery
- [ ] Human escalation

---

# 21. Recommended MVP

Do not try to build everything initially.

The first MVP SHOULD be:

```text
GitHub Issue
     ↓
"ai-ready"
     ↓
Orchestrator
     ↓
Developer Agent
     ↓
Ephemeral Sandbox
     ↓
Implement
     ↓
Tests
     ↓
GitHub Actions
     ↓
PR
     ↓
Human Review
```

If this works end to end, you already have the first version of an **AI Software Factory**.

---

# 22. MVP Success Criteria

Consider the MVP successful when you can:

1. Create an Issue.
2. Associate it with an approved specification.
3. Label it `ai-ready`.
4. Lanzar un agente sin encender tu laptop.
5. The agent creates a branch.
6. It reads the Constitution and specifications.
7. It implements the code.
8. It creates tests.
9. It runs the tests.
10. It runs CI.
11. It automatically fixes simple errors.
12. It creates a Pull Request.
13. A human reviews the result.
14. The PR can be merged.
15. The sandbox is destroyed when execution ends.

---

# 23. Golden Rule

Do not initially build:

> "Una IA que programa sola."

Construye:

> **"A controlled system in which specialized agents can execute software engineering work autonomously, verifiably, and auditably."**

Autonomy will be a consequence of the architecture, not the starting point.

---

# 24. Suggested Initial Technology Architecture

```text
LAPTOP
├── VS Code
├── Git
└── AI Coding Assistant
        │
        ▼
GITHUB
├── Repository
├── Specs
├── Issues
├── Pull Requests
├── Actions
└── Branch Protection
        │
        ▼
AZURE
├── Orchestrator
├── Container Jobs / Ephemeral Runners
├── Knowledge Base
├── Search / Vector Store
├── Key Vault
├── Managed Identity
└── Application Insights / Monitoring
        │
        ▼
AI MODELS
├── Coding Agent
├── Reasoning Agent
└── Review / Classification Agents
```

---

# 25. Próximo paso concreto

The next step **SHOULD NOT** be to build all agents.

It SHOULD be to build the first vertical slice:

```text
GitHub Issue
    ↓
Spec aprobada
    ↓
AI Developer Agent
    ↓
Cloud Sandbox
    ↓
Code + Tests
    ↓
GitHub Actions
    ↓
Pull Request
```

Once this flow is reliable, adding specialized agents becomes an incremental evolution.

**Final objective:**

> You define what must be built.
> Agents determine how to execute it within the rules.
> CI/CD demonstrates that it works.
> GitHub records everything.
> Azure provides the execution environment.
> You make the decisions that genuinely require human judgment.
> The laptop does not need to remain powered on.
