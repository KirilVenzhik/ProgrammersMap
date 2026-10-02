# Статус

**Фаза:** P0 · **Состояние:** в работе · **Следующая задача:** P0-05

## Нужно от человека
- (пока ничего)

## Заметки для следующих задач
- Нетестированные ветки валидации Parse (дубликат группы, пустой slug/title, нет sections/level) — добавить тесты попутно в любой задаче Core.
- Нет теста на `InvalidOperationException` при пустом `Content:TopicsPath` (AddTopicCatalog) — добавить попутно.
- Кириллица в HTML выводится как UTF-8 (WebEncoderOptions, UnicodeRanges.All) — не откатывать.

## Лог (последние 5 записей, новые сверху)
- 2026-10-02 — первый прогон CI на GitHub зелёный: windows-latest и ubuntu-latest (run 37007627024).
- 2026-10-02 — P0-04 done: CI (windows+ubuntu, Release, checkout/setup-dotnet v5), .gitattributes (LF, crlf для sln/ps1/cmd/bat), end_of_line в .editorconfig, ренормализация; тесты 24/24; ревью APPROVE с 1-го круга. Реальный прогон CI — после пуша.
- 2026-10-02 — P0-03 done: главная со списком разделов и числом тем, каталог singleton с загрузкой при старте (ADR-6), кириллица без entities; тесты 24/24; ревью APPROVE с 1-го круга (1 доработка до ревью).
- 2026-10-02 — репозиторий запушен на GitHub (origin/main), пуш делает человек.
- 2026-10-02 — P0-02 done: модели Section/Group/Topic/Level, ITopicCatalog, JsonTopicCatalog с валидацией; тесты 21/21; ревью APPROVE с 1-го круга.
