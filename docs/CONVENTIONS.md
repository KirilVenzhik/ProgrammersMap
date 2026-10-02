# Соглашения

## Код
- C#: nullable enabled, file-scoped namespaces, `TreatWarningsAsErrors` в src.
- Одна публичная сущность на файл. Имена — по смыслу, без сокращений.
- Логика в `ProgChecklist.Core`, страницы тонкие.
- Без новых NuGet-пакетов без записи в DECISIONS.md.

## Тесты
- Каждая задача с логикой — с тестами. Имя: `Method_Condition_Expected`. Структура Arrange–Act–Assert.
- Интеграционные тесты страниц: статус 200 и наличие ключевого текста.

## Коммиты
Conventional Commits: `feat:`, `fix:`, `test:`, `docs:`, `chore:`, `refactor:`. В теле — ID задачи (`P1-03`).

## Frontend
- Без фреймворков и сборщиков. CSS-переменные для цветов (как в прототипе).
- Доступность: видимый focus, `label` у чекбоксов, `prefers-reduced-motion`.
