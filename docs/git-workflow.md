# Git Branch Workflow

This project has `main`, `develop`, and `feature/validation`. The current work belongs on `feature/validation`; do not switch to or push to `main`.

## Check the working branch

```powershell
git status --short --branch
git branch --show-current
```

Before editing or committing, confirm the current branch is `feature/validation` and inspect the working-tree status so unrelated team changes remain untouched.

## Review and commit changes

```powershell
git diff --check
git diff
git add README.md docs/API.md docs/database-design.md docs/tasks-kanban.md docs/git-workflow.md docs/code-review.md
git diff --cached
git commit -m "docs: complete module 1 project artifacts"
```

Stage only the files belonging to the task. Review both the unstaged and staged diffs before creating a commit. Use a short imperative commit subject with a scope prefix such as `docs:` or `fix:`.

## Share and integrate

When the team asks to publish the work, push only the feature branch:

```powershell
git push origin feature/validation
```

Open a pull request from `feature/validation` to the team's explicitly approved non-`main` target. Request teammate review, resolve findings, and merge only after approval. Never use `git push origin main`, and never merge this work into `main`.

The commands above describe the team workflow; this documentation task itself does not push or merge anything.