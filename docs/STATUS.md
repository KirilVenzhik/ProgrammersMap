# Статус

**Фаза:** P0 · **Состояние:** в работе · **Следующая задача:** P0-04

## Нужно от человека
- (пока ничего)

## Заметки для следующих задач
- Нет `.gitattributes`: файлы из шаблонов CRLF+BOM, рукописные LF. Внесено в приёмку P0-04.
- Нетестированные ветки валидации Parse (дубликат группы, пустой slug/title, нет sections/level) — добавить тесты попутно в любой задаче Core.
- Нет теста на `InvalidOperationException` при пустом `Content:TopicsPath` (AddTopicCatalog) — добавить попутно.
- Кириллица в HTML выводится как UTF-8 (WebEncoderOptions, UnicodeRanges.All) — не откатывать.

## Лог (последние 5 записей, новые сверху)
- 2026-10-02 — P0-03 done: главная со списком разделов и числом тем, каталог singleton с загрузкой при старте (ADR-6), кириллица без entities; тесты 24/24; ревью APPROVE с 1-го круга (1 доработка до ревью).
- 2026-10-02 — репозиторий запушен на GitHub (origin/main), пуш делает человек.
- 2026-10-02 — P0-02 done: модели Section/Group/Topic/Level, ITopicCatalog, JsonTopicCatalog с валидацией; тесты 21/21; ревью APPROVE с 1-го круга.
- 2026-10-02 — сетап по итогам первого прогона: проверки в SETUP, стоп без сабагентов, коммит через файл, тестовые пакеты в CONVENTIONS, .gitattributes в P0-04.
- 2026-10-02 — P0-01 done: sln (классический формат, ADR-5), Core/Web/Core.Tests/Web.Tests, Directory.Build.props, .editorconfig; build 0 warnings, тесты 2/2; ревью APPROVE с 1-го круга.
