# Programmer's Map

[![CI](https://github.com/KirilVenzhik/ProgrammersMap/actions/workflows/ci.yml/badge.svg)](https://github.com/KirilVenzhik/ProgrammersMap/actions/workflows/ci.yml)

A detailed, Russian-language checklist of programming topics: 254 topics in 14 sections, each marked Junior, Middle or Senior. The goal is to let self-taught developers see what they have already covered and what is still missing. Each topic is planned to get its own SEO-friendly page; lessons, quizzes and user accounts come later. The UI and the content are in Russian.

This is also the author's portfolio project (looking for a Junior .NET Backend position), so code quality, tests and CI are treated as features.

## Status

Phases P0 to P2 are done: the public checklist, a page per topic and section, SEO and sitemap, and pilot lessons with learning resources. Phase P3 (accounts) is in progress: the PostgreSQL data layer and local setup exist; sign-in and server-side progress are not implemented yet.

| Phase | Goal |
|---|---|
| P0 Skeleton | Solution, projects, CI, basic page (done) |
| P1 Public checklist | Port the HTML prototype to Razor Pages, a page per topic, SEO, sitemap (done) |
| P2 Content scaffolding | Topic page template: "where to learn", a place for the lesson (done) |
| P3 Accounts | Identity, PostgreSQL, progress sync (in progress) |
| P4 Monetization | Pro subscription, per-topic quizzes, tracks |
| P5 Deployment | Hosting, domain, monitoring |

## Tech stack

- .NET 10, ASP.NET Core Razor Pages (server-side rendering, for SEO)
- Plain HTML/CSS/JS, no SPA framework
- Topics live in `content/topics.json` and are loaded into memory at startup
- Tests: xUnit, `WebApplicationFactory` for integration tests
- CI: GitHub Actions, build and test on Windows and Ubuntu
- Data: PostgreSQL + EF Core (`ProgChecklist.Data`); ASP.NET Core Identity is planned

## Project structure

```
ProgChecklist.sln
src/
  ProgChecklist.Core/        Models (Section, Group, Topic, Level), topic catalog; no ASP.NET dependency
  ProgChecklist.Data/        EF Core DbContext, entities, migrations (PostgreSQL)
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

## Getting started

Prerequisites: the .NET 10 SDK and Docker (the local database is PostgreSQL).

```
git clone https://github.com/KirilVenzhik/ProgrammersMap.git
cd ProgrammersMap
cp .env.example .env        # set POSTGRES_PASSWORD in .env (git-ignored)
docker compose up -d        # PostgreSQL on 127.0.0.1:$POSTGRES_PORT (default 5432)
dotnet build
dotnet test
```

If port 5432 is taken or Windows refuses to bind it ("bind: forbidden by its access permissions"), set `POSTGRES_PORT=5433` (or another free port) in `.env`. Use that same port in the connection strings below.

Run the site with one of the following (migrations are applied automatically in Development):

- WSL (see ADR-9, also works when Smart App Control blocks local DLLs): `.\scripts\run-wsl.ps1`. The script builds the connection string from `.env`.
- Native: store the connection string in user secrets once, then run:

  ```
  dotnet user-secrets set "ConnectionStrings:Default" "Host=127.0.0.1;Port=<POSTGRES_PORT>;Database=progchecklist;Username=progchecklist;Password=<your password>" --project src/ProgChecklist.Web
  dotnet run --project src/ProgChecklist.Web
  ```

Then open http://localhost:5043 (port from the `http` launch profile). `docker compose down` stops the database (the data volume is kept).

Tests use SQLite in memory by default. Tests marked `PostgresFact` need a real server and are skipped unless `PROGCHECKLIST_TEST_POSTGRES` holds a connection string (each test creates and drops its own database):

```
# PowerShell
$env:PROGCHECKLIST_TEST_POSTGRES = "Host=127.0.0.1;Port=<POSTGRES_PORT>;Database=postgres;Username=progchecklist;Password=<your password>"
dotnet test --filter "FullyQualifiedName~PostgresMigrationTests"
```

CI runs them on Ubuntu against a PostgreSQL service container.

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
