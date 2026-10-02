# Статус

**Фаза:** P0 · **Состояние:** в работе · **Следующая задача:** P0-03

## Нужно от человека
- Создать репозиторий на GitHub и запушить (для CI).
- Добавить в `.claude/settings.json` PowerShell-аналоги разрешений (`PowerShell(dotnet *)`, `PowerShell(git status*)` и т.д.) и deny для `git push`/`git reset --hard` — сейчас там только `Bash(...)`, а основной shell — PowerShell.

## Заметки для следующих задач
- Нет `.gitattributes`: файлы из шаблонов CRLF+BOM, рукописные LF. Внесено в приёмку P0-04.
- Web smoke-тест проверяет только 2xx; в P0-03 перейти на 200 + ключевой текст по CONVENTIONS.
- P0-03: путь к topics.json взять из конфигурации (напр. `Content:TopicsPath`, относительно ContentRoot) и передать в `JsonTopicCatalog.LoadFromFile`; `LoadFromFile` оборачивает только FileNotFound/DirectoryNotFound.
- Нетестированные ветки валидации Parse (дубликат группы, пустой slug/title, нет sections/level) — добавить тесты попутно в любой задаче Core.

## Лог (последние 5 записей, новые сверху)
- 2026-10-02 — P0-02 done: модели Section/Group/Topic/Level, ITopicCatalog, JsonTopicCatalog с валидацией; тесты 21/21; ревью APPROVE с 1-го круга.
- 2026-10-02 — сетап по итогам первого прогона: проверки в SETUP, стоп без сабагентов, коммит через файл, тестовые пакеты в CONVENTIONS, .gitattributes в P0-04.
- 2026-10-02 — P0-01 done: sln (классический формат, ADR-5), Core/Web/Core.Tests/Web.Tests, Directory.Build.props, .editorconfig; build 0 warnings, тесты 2/2; ревью APPROVE с 1-го круга.
- 2026-10-02 — сетап: проект перенесён из вложенной папки в корень репо, удалена мусорная папка `{.claude...}`, первый коммит. .NET 10.0.400 подтверждён.
- 2026-10-02 — проект подготовлен: документация, агенты, topics.json (254 темы) из прототипа.
