# Статус

**Фаза:** P1 · **Состояние:** в работе · **Следующая задача:** P1-08 (итог фазы)

## Нужно от человека
- (не блокирует) Smart App Control на Windows блокирует свежесобранные DLL (0x800711C7) — тесты: `.scripts	est-wsl.ps1` (ADR-9). Для полной глобализации в WSL: `sudo apt-get install -y libicu-dev`. Стоит добавить в CLAUDE.md (раздел «Команды») — это файл человека.
- При деплое (P5): задать `Site__BaseUrl` и сузить `AllowedHosts` (ADR-8).
- (не блокирует) Сверить глазами http://localhost:5043 с `prototype/index.html` (светлая/тёмная тема, ширина 360px) — тестами это не проверить.

## Заметки для следующих задач
- Нетестированные ветки валидации Parse (дубликат группы, пустой slug/title, нет sections/level) — добавить тесты попутно в любой задаче Core.
- Нет теста на `InvalidOperationException` при пустом `Content:TopicsPath` (AddTopicCatalog) — добавить попутно.
- Кириллица в HTML выводится как UTF-8 (WebEncoderOptions, UnicodeRanges.All) — не откатывать.
- Slug'и теперь английские (ADR-7), формат закреплён тестом `Slugs_RealFile_MatchFormat`. Новые темы — по тем же правилам. Тест `TopicKeys_RealFile_AreUnique` содержит жёсткое 254 — обновлять при добавлении тем.
- Контракт разметки для progress.js: `li[data-key][data-level]`, `input[data-key]`, `#map span[data-key]`, `#pct`, `#count`, `[data-sc|data-nc|data-nr=slug]`, `.group[data-group]`, `section.sec[data-section]`.
- Агенты, запускающие `dotnet run`, обязаны останавливать процесс: в P1-03 остался сирота на :5199 и блокировал Debug-сборку.
- UX на потом: «Сбросить» на странице раздела сбрасывает весь прогресс (так и написано в confirm).
- Гигиена тестов (попутно): в новых тестах P1-06 нет комментариев `// Arrange / Act / Assert`; `TopicPageTests.LoadCatalog()` парсит JSON в каждом из 254 кейсов — можно кешировать.
- SEO-хвосты (попутно): нет теста, что завершающее предложение TopicDescription добавляется, когда влезает; сообщение об ошибке BaseUrl не упоминает userinfo.

## Лог (последние 5 записей, новые сверху)
- 2026-10-02 — P1-07 done: title/description (Core SeoText, русские склонения), canonical, Open Graph, sitemap.xml (269 URL), robots.txt, JSON-LD BreadcrumbList; базовый URL — ADR-8; тесты 395/395 (в WSL из-за Smart App Control); ревью: 1 круг правок, APPROVE со 2-го.
- 2026-10-02 — P1-06 done: страница темы `/{s}/{t}` (крошки, уровень, группа, «Изучено», «Урок в разработке», соседние темы в общем порядке курса), Core: FindTopicContext; все 254 URL → 200; тесты 349/349; ревью APPROVE с 1-го круга + усилен тест маршрута ошибки.
- 2026-10-02 — P1-05 done: страница раздела `/{slug}` (крошки, h1, прогресс, тулбар-partial, темы), 301 на канонический регистр, страница 404 через StatusCodePages; на главной заголовки разделов — ссылки; тесты 75/75; ревью APPROVE с 1-го круга + правка ErrorModel.
- 2026-10-02 — P1-04 done: progress.js (localStorage `prog-checklist-v2`, фильтры, сброс, синхронизация вкладок), тулбар скрыт без JS, `label` у чекбоксов; ручной чеклист в браузере пройден; тесты 53/53; ревью: 1 круг правок (одиночный чекбокс), APPROVE со 2-го.
- 2026-10-02 — P1-03 done: главная = чеклист на сервере (карта 254 клеток, сайдбар с прогрессом, разделы/группы/темы со ссылкой и чекбоксом), partial `_SectionChecklist`, Core: TopicKey, LevelExtensions, TopicCount; тесты 51/51; ревью APPROVE с 1-го круга.
