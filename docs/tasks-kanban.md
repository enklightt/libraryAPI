# Tasks + Kanban

Snapshot date: 2026-09-25. This is a local checklist, not a GitHub Projects board. The GitHub repository remains the source of truth for branch history and any team review/merge decision.

| Status | Task | Evidence / acceptance criteria |
|---|---|---|
| In Progress | Git repository | Local repository and `origin` exist; branch history includes `main`, `develop`, and `feature/validation`. Adding all team members requires their GitHub usernames and repository-owner access. |
| In Progress | README and team | Project purpose, structure, dependencies, and startup are documented. Team names, roles, and GitHub usernames still need to be supplied and added by the team. |
| In Progress | Tasks + Kanban | This file is a local draft only. A linked GitHub Projects board with Todo / In Progress / Review / Done columns is not yet created. |
| Done | Database design review | [database-design.md](database-design.md) records relationships, constraints, normalization notes, findings, and a read-only migration preflight. Unique email/ISBN indexes are modeled and migration-generated, but not applied. |
| Done | API documentation | All 50 controller routes have XML summaries/response codes/examples; Swagger includes the generated XML comments. [API.md](API.md) remains the route index. |
| In Progress | Branches + commits | Current work stays on `feature/validation` by explicit instruction. Existing history includes validation commit `66933b3` and documentation commit `7a025bd`; per-task branches from `develop` and pushes are not performed in this task. |
| Pending | Learn Git Branching | The local workflow guide is not completion evidence. Each student must complete at least [modules 1–4](https://learngitbranching.js.org/) and record their result. |
| Review | Code review | [code-review.md](code-review.md) records local findings; one or two teammate reviews through a PR are still required. |
| Blocked | Merge | Merge to `develop` only after peer approval, database preflight, and authorization to integrate. Never push or merge this work into `main`. |

## Suggested next cards

| Priority | Task | Acceptance criteria |
|---|---|---|
| High | Add team roster and collaborators | Record each member's name, role, and GitHub username in README and grant repository access. |
| High | Create GitHub Projects board | Link a board to the repository and add Todo, In Progress, Review, and Done columns. |
| High | Assign team tasks | Create at least two issues per participant and assign them after the roster is confirmed. |
| High | Complete Learn Git Branching | Each participant completes the first four modules and records evidence. |
| High | Peer review and integrate | Review the feature PR, resolve findings, run migration preflight, then merge only to approved `develop`. |
| Medium | Exercise documented API | Run representative requests against Development Swagger and correct documentation mismatches. |
| Medium | Add automated API tests | Cover validation failures and success cases for book create/update and chat request validation. |

## Branch workflow

Current assignment branch: `feature/validation`. Keep changes for this task on that branch. Follow [git-workflow.md](git-workflow.md) for branch/commit commands. Use a pull request with review before integration; never push directly to `main`. This task does not switch branches, push commits, or perform a merge.