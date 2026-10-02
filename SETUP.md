# Запуск (один раз)

1. Распакуй архив в `C:\Users\venzh\WorkArea\Projects\ProgChecklist` (так, чтобы CLAUDE.md лежал в корне).
2. Проверь .NET 10 SDK: `dotnet --version`. Нет — поставь с dot.net.
3. В PowerShell в папке проекта:
   ```
   git init
   git add .
   git commit -m "chore: project scaffolding for Claude Code"
   ```
4. Убедись, что переменная `CLAUDE_CODE_SUBAGENT_MODEL` НЕ задана: `echo $env:CLAUDE_CODE_SUBAGENT_MODEL` (должно быть пусто).

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
