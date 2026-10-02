# Статус

**Фаза:** P1 · **Состояние:** в работе · **Следующая задача:** P1-01

## Нужно от человека
- (пока ничего)

## Заметки для следующих задач
- Нетестированные ветки валидации Parse (дубликат группы, пустой slug/title, нет sections/level) — добавить тесты попутно в любой задаче Core.
- Нет теста на `InvalidOperationException` при пустом `Content:TopicsPath` (AddTopicCatalog) — добавить попутно.
- Кириллица в HTML выводится как UTF-8 (WebEncoderOptions, UnicodeRanges.All) — не откатывать.

## Лог (последние 5 записей, новые сверху)
- 2026-10-02 — человек одобрил итог P0 и переход на P1; ROADMAP: текущая фаза P1.
- 2026-10-02 — фаза P0 завершена: build 0 warnings, тесты 24/24, CI зелёный на windows+ubuntu; 5/5 задач приняты ревью с 1-го круга.
- 2026-10-02 — P0-05 done: README (overview, stack, run, ADR, AI-воркфлоу), команды проверены; ревью APPROVE с 1-го круга + 2 правки формулировок.
- 2026-10-02 — P0-04 done: CI (windows+ubuntu, Release, checkout/setup-dotnet v5), .gitattributes, end_of_line в .editorconfig, ренормализация; тесты 24/24; ревью APPROVE с 1-го круга.
- 2026-10-02 — P0-03 done: главная со списком разделов, каталог singleton (ADR-6), кириллица без entities; тесты 24/24; ревью APPROVE с 1-го круга.
- Ранее: P0-01, P0-02 done; репозиторий запушен на GitHub; сетап проекта.
