# Запуск (один раз)

1. Распакуй архив так, чтобы `CLAUDE.md`, `.claude\` и `.git` лежали в одной папке — корне проекта.
   Проверка (должно быть `True True`): `Test-Path .\CLAUDE.md; Test-Path .\.claude\agents\implementer.md`.
   Частая ошибка: архив распаковался во вложенную папку — тогда Claude Code не видит CLAUDE.md, агентов и команды.
   Не создавай папки bash-синтаксисом `mkdir {a,b}` в PowerShell — получится одна папка с именем `{a,b}`.
2. Проверь .NET 10 SDK: `dotnet --version`. Нет — поставь с dot.net.
3. В PowerShell в папке проекта:
   ```
   git init
   git add .
   git commit -m "chore: project scaffolding for Claude Code"
   ```
4. Убедись, что переменная `CLAUDE_CODE_SUBAGENT_MODEL` НЕ задана: `echo $env:CLAUDE_CODE_SUBAGENT_MODEL` (должно быть пусто).
5. Запускай `claude` из корня проекта. Агенты и команды подгружаются только при старте сессии:
   после изменений в `.claude\` или переноса файлов — перезапусти сессию. В `/agents` должны быть `implementer` и `reviewer`.

# Работа

Интерактивно (рекомендую для P0, чтобы посмотреть, как идёт):
```
claude --model opus
> /next
```

Автопилот (несколько задач подряд без тебя):
```
.\scripts\autopilot.ps1 -MaxTasks 5
```
Останавливается сам в конце фазы. Ты смотришь `git log` и `docs/STATUS.md`, проверяешь сайт (`dotnet run --project src/ProgChecklist.Web`), правишь `Текущая фаза` в ROADMAP.md и запускаешь снова.

Перед новой фазой P2+: `claude --model opus` → `/plan-phase P2` → утвердить список задач.
