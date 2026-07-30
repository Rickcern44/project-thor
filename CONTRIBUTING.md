# Contributing

This project is planned and tracked with [OpenSpec](openspec/) — every unit of work is an OpenSpec **change** before it's code. This document covers the git workflow around that.

## Branching: one branch per OpenSpec change

Each OpenSpec change (`openspec/changes/<change-name>/`) gets its own branch, named after the change:

```
git checkout -b <change-name>   # e.g. git checkout -b repo-init
```

Work for that change — implementation, fixes, follow-ups — happens on that branch until the change is complete and archived (`openspec archive <change-name>`). Don't mix work from two different changes on one branch.

## Committing: one commit per completed task

Each change has a `tasks.md` checklist. Commit after completing each task (each `- [ ]` → `- [x]`), not in one large commit at the end. This keeps a granular work log tied directly to the task list, so `git log` reads as a build-out narrative of the change.

## Commit messages: Conventional Commits

```
<type>(<scope>): <description>

[optional body]
```

- **type**: `feat`, `fix`, `chore`, `docs`, `test`, `refactor`, `build`, `ci`
- **scope**: the change name or capability area, e.g. `repo-init`, `game-signup`
- **description**: imperative, lower-case, references the task where useful

Example:
```
feat(repo-init): 2.3 add health-check vertical slice
```

## Pull requests

Open a PR from the change branch once its tasks are complete and verified locally. Reference the OpenSpec change name in the PR description.
