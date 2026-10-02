# Архитектура

## Стек
- .NET 10, ASP.NET Core **Razor Pages** (серверный рендеринг — важно для SEO).
- Frontend: чистый HTML/CSS/JS без SPA-фреймворка. Визуальный эталон — `prototype/index.html`.
- Данные фазы 1: `content/topics.json`, загружается при старте в память (singleton).
- Контент фазы 2 (ADR-11): уроки `content/lessons/{section}/{topic}.md` (Markdig, ADR-10) и ресурсы «Где изучить» `content/resources.json`; загружаются и валидируются при старте, HTML уроков кешируется.
- С фазы 3: PostgreSQL + EF Core, ASP.NET Core Identity. Локально через `docker compose`.
- Тесты: xUnit, `WebApplicationFactory` для интеграционных.
- CI: GitHub Actions (build + test на push).

## Структура решения
```
ProgChecklist.sln
src/
  ProgChecklist.Web/        Razor Pages, wwwroot (css, js), Program.cs
  ProgChecklist.Core/       модели (Section, Group, Topic, Level, Resource), каталог, контент, SEO-тексты; без зависимостей от ASP.NET
  ProgChecklist.Data/       (фаза 3) EF Core, миграции
tests/
  ProgChecklist.Core.Tests/
  ProgChecklist.Web.Tests/
content/
  topics.json               каталог тем (источник правды, slug'и)
  resources.json            (фаза 2) ресурсы «Где изучить» по ключу "{section}/{topic}"
  lessons/{section}/{topic}.md   (фаза 2) уроки, необязательны
scripts/                    autopilot, запуск/тесты через WSL (ADR-9)
```

## URL
- `/` — весь чеклист (как прототип)
- `/{sectionSlug}` — раздел
- `/{sectionSlug}/{topicSlug}` — страница темы: урок (или заглушка «Урок в разработке») + блок «Где изучить»
- `/sitemap.xml`, `/robots.txt`

## Прогресс
- Фазы 1–2: localStorage, ключ `prog-checklist-v2`, значение — массив `"{sectionSlug}/{topicSlug}"`.
- Фаза 3: таблица `UserTopicProgress(UserId, TopicKey, CompletedAt)`; при входе localStorage сливается с сервером (объединение).

## Frontend
Цвета, шрифты (Unbounded + Golos Text), карта-сетка прогресса, подсветка маркером — взять из прототипа.
Светлая/тёмная тема через `prefers-color-scheme`. Адаптив до 360px. JS — один модуль `wwwroot/js/progress.js`.
