# Rental Manager SaaS — Repository Instructions

These instructions apply to the entire repository. More specific `AGENTS.md` files under the
frontend and backend directories extend these rules for their own subtree.

## Source of truth

- Jira project: `SCRUM` at `https://van123872000.atlassian.net`.
- Repository: `Anh467/Rental-Manager-SaaS-Product-Roadmap`.
- Jira issue descriptions, acceptance criteria, comments, dependencies, and linked Confluence
  pages are the current business source of truth.
- Before implementing a Jira issue, read its live Jira data and relevant Confluence pages through
  the configured connection. Do not implement from an old prompt or memory alone.
- If live Jira/Confluence materially conflicts with these instructions or with the user's request,
  stop and report the exact conflict. Do not silently choose a business rule.

## Required preflight

Before editing code for a Story:

1. Read its description, acceptance criteria, comments, parent, labels, dependencies, and blockers.
2. Read linked architecture, business-rule, data-model, API, permission, and message-catalog pages.
3. Pull/fetch the latest target branch and record the baseline commit SHA.
4. Inspect `AGENTS.md` files that cover the files being changed.
5. Search the repository for existing components, abstractions, helpers, contracts, tests, and
   migrations before creating anything.
6. Check for an open PR or branch that overlaps the same issue, schema, or contract. Do not create
   competing implementations.
7. State the planned scope and targeted tests. Keep unrelated cleanup out of the change.

If Jira or Confluence cannot be read, do not invent missing requirements. Report the access blocker.

## Implementation principles

- Reuse and improve existing common code. Do not create a second response envelope, message
  catalog, error writer, exception middleware, organization context, permission framework,
  repository base, API client, form framework, table framework, or test host with the same job.
- If shared code is in the wrong module, move it and update references; do not copy it and leave two
  active versions.
- Prefer the smallest coherent change that fully satisfies the accepted issue.
- Preserve clean dependency direction and module ownership. Do not put HTTP, SQL, provider-specific
  code, or localized UI text into domain code.
- Do not hard-code an identity provider, tenant, organization, role, permission, environment URL,
  credential, or secret.
- Never weaken authentication, authorization, tenant isolation, RLS, validation, logging redaction,
  or tests merely to make a check pass.
- Do not delete or weaken an existing test unless an approved requirement intentionally replaces
  the behavior; explain such updates in the commit/PR.
- Do not make destructive production-data changes. Data migrations must be explicit, verifiable,
  idempotent where practical, and safe to stop when validation fails.

## API contract shared by frontend and backend

- The backend returns stable `SCS-xxx`/`ERR-xxx` message keys and safe parameters, never localized
  display text.
- JSON success responses (`200`, `201`, `202`) use one canonical envelope with `success=true`,
  `messageKey`, `data` (nullable), and `correlationId`; `parameters` is optional.
- JSON error responses (`4xx`, `5xx`) use one canonical envelope with `success=false`, `messageKey`,
  and `correlationId`; `parameters` and `fieldErrors` are optional.
- `204` has no body. Successful file/blob/stream responses are not wrapped; their failures still use
  the canonical JSON error envelope.
- The response-body `correlationId` must match the `X-Correlation-ID` response header.
- Active and Deprecated message keys remain distinguishable. Deprecated keys are reserved and must
  not be emitted by new code.
- Backend and frontend contracts, message catalogs, and Vietnamese/English locales must remain in
  sync and be protected by tests.

## Jira and Git workflow

- Work on at most one Story at a time unless the user explicitly requests otherwise.
- Respect issue dependencies; do not start a blocked Story simply because it appears next in a list.
- When implementation begins, move the Story to the Jira workflow equivalent of `In Progress` and
  comment with the baseline SHA and scope.
- Use focused commits containing the Jira key, for example:
  `SCRUM-123: implement organization selector`.
- After the Story's code and required tests pass, comment with the commit SHA, implemented scope,
  test results, and any migration notes; move it to the workflow equivalent of `In Review`.
- Do not change parent, dependency, priority, Story Point, labels, requirements, or Confluence content
  unless the user or accepted refinement explicitly authorizes that change.
- Do not merge a PR or move an issue to `Done` unless the user explicitly asks.
- Do not push directly to `main` unless the user explicitly asks. Normally use a focused branch and
  a Draft PR.
- Never expose Jira/GitHub tokens, cookies, authorization headers, email addresses, secrets, or real
  environment values in code, logs, Jira comments, commits, or responses.

## Definition of done

- Acceptance criteria are traceable to code and tests.
- Targeted tests pass, then the relevant CI-equivalent suite passes.
- `git diff --check` passes and `git status --short` contains only intended files.
- New projects, routes, SQL objects, locale namespaces, and tests are registered in the appropriate
  solution, project, router, catalog, or CI configuration.
- Schema changes include safe migration/cutover validation and do not leave two runtime models.
- Security- and audit-relevant events contain a correlation ID and safe structured metadata, never
  tokens, secrets, stack traces, raw SQL, or cross-organization data.
- The final report lists the branch/commit/PR, Jira state, changed areas, exact checks and results,
  migration result, known limitations, and any unmet acceptance criterion.
