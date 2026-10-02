# Programmer's Map

[![CI](https://github.com/KirilVenzhik/ProgrammersMap/actions/workflows/ci.yml/badge.svg)](https://github.com/KirilVenzhik/ProgrammersMap/actions/workflows/ci.yml)

A detailed, Russian-language checklist of programming topics: 254 topics in 14 sections, each marked Junior, Middle or Senior. The goal is to let self-taught developers see what they have already covered and what is still missing. Each topic is planned to get its own SEO-friendly page; lessons, quizzes and user accounts come later. The UI and the content are in Russian.

This is also the author's portfolio project (looking for a Junior .NET Backend position), so code quality, tests and CI are treated as features.

## Status

Early stage: phase P0 (skeleton). Right now the home page renders the list of sections with their topic counts. Topic pages, progress tracking and everything else below are not implemented yet.

| Phase | Goal |
|---|---|
| P0 Skeleton | Solution, projects, CI, basic page (in progress) |
| P1 Public checklist | Port the HTML prototype to Razor Pages, a page per topic, SEO, sitemap |
| P2 Content scaffolding | Topic page template: "where to learn", a place for the lesson |
| P3 Accounts | Identity, PostgreSQL, progress sync |
| P4 Monetization | Pro subscription, per-topic quizzes, tracks |
| P5 Deployment | Hosting, domain, monitoring |

## Tech stack

- .NET 10, ASP.NET Core Razor Pages (server-side rendering, for SEO)
- Plain HTML/CSS/JS, no SPA framework
- Topics live in `content/topics.json` and are loaded into memory at startup
- Tests: xUnit, `WebApplicationFactory` for integration tests
- CI: GitHub Actions, build and test on Windows and Ubuntu
- Planned (phase 3, not present yet): PostgreSQL, EF Core, ASP.NET Core Identity

## Project structure

```
ProgChecklist.sln
src/
  ProgChecklist.Core/        Models (Section, Group, Topic, Level), topic catalog; no ASP.NET dependency
  ProgChecklist.Web/         Razor Pages app, wwwroot, Program.cs
tests/
  ProgChecklist.Core.Tests/
  ProgChecklist.Web.Tests/
content/topics.json          Source of truth for the topics
docs/                        Architecture, conventions, roadmap, backlog, ADRs (in Russian)
prototype/index.html         Static HTML prototype used as the visual reference
.claude/                     Claude Code subagents and commands
scripts/autopilot.ps1        Runs the task loop in headless mode
```

Planned: `src/ProgChecklist.Data` (EF Core, migrations) in phase 3.

## Getting started

Prerequisite: the .NET 10 SDK.

```
git clone https://github.com/KirilVenzhik/ProgrammersMap.git
cd ProgrammersMap
dotnet build
dotnet test
dotnet run --project src/ProgChecklist.Web
```

Then open http://localhost:5043 (port from the `http` launch profile).

The location of the topics file can be overridden with the configuration key `Content:TopicsPath` (environment variable `Content__TopicsPath`). By default it is `content/topics.json`, copied next to the build output; a relative path is resolved from the build output folder, not from the current directory.

## Key design decisions

The full list is in [docs/DECISIONS.md](docs/DECISIONS.md) (in Russian).

- **Razor Pages instead of an SPA.** Organic search is the main traffic channel, so pages are rendered on the server; JavaScript is limited to progress tracking.
- **JSON as the source of truth.** Content changes rarely and is edited through git, so no database is needed until accounts appear. A topic `slug` is a stable ID used in URLs and in saved progress and must not change without a migration.
- **Fail-fast catalog loading.** The catalog is loaded once at startup and registered as a singleton. A missing or broken JSON file stops the application at startup instead of failing on the first request.
- **Line endings.** The repository normalizes line endings through `.gitattributes` so builds behave the same on Windows and Linux CI.

## AI-assisted workflow

The project is developed with [Claude Code](https://claude.com/claude-code) using three roles:

- An Opus orchestrator plans, splits work into tasks and writes a spec for each one.
- A Sonnet `implementer` subagent writes code and tests for a single backlog task.
- An independent Opus `reviewer` subagent checks the diff against the acceptance criteria.

One task is one commit, and a task is done only when build and tests are green. The `/next` command runs one such cycle; `scripts/autopilot.ps1` repeats it in headless mode. A human approves phase transitions and owns pushes, payments, secrets and deployment.

See [CLAUDE.md](CLAUDE.md) for the rules, [docs/BACKLOG.md](docs/BACKLOG.md) for the task list and [docs/STATUS.md](docs/STATUS.md) for the current state. The documents in `docs/` are written in Russian.
