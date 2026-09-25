# Tasks + Kanban

Snapshot date: 2026-09-25. This markdown board tracks the Module 1 deliverables for the Library API. The GitHub repository remains the source of truth for branch history and any team review/merge decision.

| Status | Task | Evidence / acceptance criteria |
|---|---|---|
| Done | Git repository | Repository has `main`, `develop`, and `feature/validation` branches and an `origin` remote. |
| Done | README | Root README documents purpose, stack, setup, existing features, and links to this documentation set. |
| Done | Tasks + Kanban | This file records status, evidence, and the outstanding team workflow gate. |
| Done | Database design | [database-design.md](database-design.md) describes entities, relationships, keys, and constraints from the EF model/migrations. |
| Done | API documentation | [API.md](API.md) lists routes, access rules, inputs, and outcomes from the controllers. |
| Done | Branches + commits | Work is on `feature/validation`; current branch history includes `66933b3` (`add validation rules for books, DTOs, and chat requests with EF constraints ;-)`) on top of `main`/`develop`. |
| Review | Code review | [code-review.md](code-review.md) records the local review finding; teammate review and approval are still required before integration. |
| Blocked | Merge | Resolve/accept the migration finding and agree on a non-`main` integration target first. Do not push or merge this work into `main`. |

## Suggested next cards

| Priority | Task | Acceptance criteria |
|---|---|---|
| High | Peer review feature validation | Validate DTO rules, EF constraints, existing-data migration behavior, and regression coverage; resolve or document findings. |
| High | Confirm integration target | Team agrees whether `develop` is the integration target; keep `main` out of scope. |
| Medium | Exercise documented API | Run representative requests from `libraryAPI.http` against Development Swagger and correct documentation mismatches. |
| Medium | Add automated API tests | Cover validation failures and success cases for book create/update and chat request validation. |

## Branch workflow

Current assignment branch: `feature/validation`. Keep changes for this task on that branch. Follow [git-workflow.md](git-workflow.md) for branch/commit commands. Use a pull request with review before integration; never push directly to `main`. This task does not switch branches, push commits, or perform a merge.