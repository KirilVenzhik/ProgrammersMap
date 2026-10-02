---
description: Take the next backlog task through plan → implement (Sonnet) → review (Opus) → commit
---

You are the orchestrator (Opus). Run one full cycle:

0. Check that the `implementer` and `reviewer` subagents are available. If not: do NOT substitute other agents; add "restart the session from the project root" to "Нужно от человека" in `docs/STATUS.md` and STOP.
1. Read `docs/STATUS.md`, `docs/ROADMAP.md` (current phase), `docs/BACKLOG.md`.
2. Pick the first `todo` task of the current phase. If none: if the phase is finished, do the phase-summary task: write the phase result to STATUS.md with the literal marker `REVIEW-NEEDED`, then STOP. Skip `needs-human` and `blocked`.
3. Mark it `in-progress` in BACKLOG.md.
4. If the task needs an architectural decision not in `docs/DECISIONS.md`, add an ADR first.
5. Write a precise spec for the implementer: goal, files to create/change, signatures, edge cases, acceptance criteria, what NOT to do. Delegate to the `implementer` subagent.
6. Run `dotnet build` and `dotnet test` yourself to verify.
7. Delegate to the `reviewer` subagent with the task ID and acceptance criteria.
8. If CHANGES_REQUIRED: send the blocking issues back to `implementer`. Max 2 rounds. Still failing → revert with `git restore` and `git clean` limited to files from this task, mark `blocked` with the reason, go to step 10.
9. If APPROVE: commit with a Conventional Commit message including the task ID. Mark the task `done`.
   In PowerShell write a multi-line message to a temp file and use `git commit -F <file>` (`-F -` with a here-string does not work in PowerShell 5.1).
10. Update `docs/STATUS.md`: next task, one log line (date, task, result). Keep max 5 log lines. Commit docs changes in the same commit if possible.
11. Print a 3-line summary: task, result, next task.

$ARGUMENTS
