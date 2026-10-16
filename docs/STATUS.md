# Статус

**Фаза:** P3 · **Состояние:** в работе · **Следующая задача:** P3-07 Синхронизация в progress.js

## Нужно от человека
- (не блокирует) Smart App Control на Windows блокирует свежесобранные DLL (0x800711C7) — тесты: `.\scripts\test-wsl.ps1` (ADR-9). Для полной глобализации в WSL: `sudo apt-get install -y libicu-dev`. Стоит добавить в CLAUDE.md (раздел «Команды») — это файл человека.
- Запуск сайта теперь требует Docker Desktop и `.env` (скопировать из `.env.example`, задать пароль; на этой машине порт 5432 запрещён Windows — поставить `POSTGRES_PORT=5433`). Локальный `.env`, созданный агентом, можно удалить и создать заново: его пароль никто не знает. Затем `docker compose up -d` и `.\scripts\run-wsl.ps1`.
- При деплое (P5): задать `Site__BaseUrl` и сузить `AllowedHosts` (ADR-8); cookie `SecurePolicy=Always`, миграции как шаг деплоя (ADR-12/13); GDPR-тексты и хостинг в ЕС (P3-09).
- P2-08 (партнёры, маркировка рекламы), P3-09 (почта, OAuth, правовые тексты) — когда будете готовы.

Итог P2 (одобрен 2026-10-16): уроки Markdown + «Где изучить» с валидацией, бейдж и SEO из урока, 3 пилотных урока и 28 ресурсов, `docs/CONTENT.md`; 549 тестов, CI зелёный. Вёрстка уроков глазами пока не проверена.
Итог P1 (одобрен 2026-10-02): 254 темы по URL, прогресс, sitemap; 395 тестов, CI зелёный.

## Заметки для следующих задач
- Нетестированные ветки валидации Parse (дубликат группы, пустой slug/title, нет sections/level) — добавить тесты попутно в любой задаче Core.
- Нет теста на `InvalidOperationException` при пустом `Content:TopicsPath` (AddTopicCatalog) — добавить попутно.
- Кириллица в HTML выводится как UTF-8 (WebEncoderOptions, UnicodeRanges.All) — не откатывать.
- Slug'и английские (ADR-7), формат закреплён тестом `Slugs_RealFile_MatchFormat`. Тест `TopicKeys_RealFile_AreUnique` содержит жёсткое 254 — обновлять при добавлении тем.
- Контракт разметки для progress.js: `li[data-key][data-level]`, `input[data-key]`, `#map span[data-key]`, `#pct`, `#count`, `[data-sc|data-nc|data-nr=slug]`, `.group[data-group]`, `section.sec[data-section]`.
- Агенты, запускающие `dotnet run`, обязаны останавливать процесс (в P1-03 остался сирота на :5199).
- UX на потом: «Сбросить» на странице раздела сбрасывает весь прогресс (так и написано в confirm).
- Гигиена тестов (попутно): в тестах P1-06 нет комментариев `// Arrange / Act / Assert`; `TopicPageTests.LoadCatalog()` парсит JSON в каждом из 254 кейсов.
- SEO-хвост (попутно): сообщение об ошибке BaseUrl не упоминает userinfo.
- Типографика урока (`.lesson-body`) глазами не проверена: Chrome-расширение не было подключено, проверено только по HTML (уроки, таблицы, код, ресурсы, бейджи). Нужен взгляд человека: `/git/init-add-commit`, `/databases/join`, `/fundamentals/binary-bits-bytes` — светлая/тёмная тема, узкий экран. Моно-шрифт — системный стек `ui-monospace,…`.
- Ресурсы P2-07: MDN-руководство по Flexbox на русском — устаревший перевод сообщества (ревью); можно заменить, когда появится лучше.
- После регистрации вход непостоянный (сессионная cookie). `AccessDeniedPath` пока `/account/login`. Тест-хелперы — `AuthTestHelpers.cs`.
- Панель аккаунта (P3-04) и форма входа глазами не проверены (тёмная тема, 360px) — только тесты и CSS на переменных.
- P3-07: футер «Прогресс сохраняется в этом браузере.» для вошедшего неверен — поменять текст вместе с синхронизацией.
- P5: ключи Data Protection (cookie входа) хранить персистентно, иначе после перезапуска контейнера все разлогинятся.
- Postgres-тесты на Windows: `Host=127.0.0.1`, не `localhost` (иначе IPv6-попытка, 1+ мин на тест). Пароль в `.env` без пробелов, `;`, `$`, кавычек (файл исполняется bash).
- P3-07: id пользователя и CSRF-токен читать из `meta[name="user-id"]` и `meta[name="csrf-token"]` (только у вошедшего), токен слать в заголовке `RequestVerificationToken`. Ответы API: GET/merge → `{ "keys": [...] }`, 204 на изменения, 401 — сессия истекла.
- P3-08: удалённый аккаунт с живой cookie → 500 в API (FK в `IProgressService`); при удалении аккаунта обязательно выходить из него.
- Smart App Control блокирует DLL нестабильно (в P3-01…03 не блокировал, в P3-04 снова 0x800711C7) — при ошибке тесты через WSL-скрипт.
- `scripts/test-wsl.ps1` запускать как `powershell -ExecutionPolicy Bypass -File scripts/test-wsl.ps1` (иначе политика выполнения блокирует).

## Лог (последние 5 записей, новые сверху)
- 2026-10-16 — P3-06 done: `/api/progress` (GET, PUT/DELETE темы, POST merge, DELETE всего) по ADR-14, CSRF-заголовок `RequestVerificationToken`, 401 для гостя, `no-store` + `noindex` для `/api`, merge ≤ 128 КБ и ≤ 1000 ключей; в layout для вошедшего `meta csrf-token` и `meta user-id` (вместо `data-user` из BACKLOG — Razor не умеет опускать `data-*`); тесты 667 + 5 skipped; ревью APPROVE с 1-го круга.
- 2026-10-16 — P3-05 done: `TopicKey.TryParse/IsKnown/Normalize` (ординальное сравнение slug'ов), `IProgressService` (Get/Mark/Unmark/Clear/Merge, лимит 1000, `CompletedAt` в UTC через `TimeProvider`, разовый повтор вставки при гонке); тесты 640 + 5 skipped (новый Postgres-тест — в CI); ревью APPROVE с 1-го круга.
- 2026-10-16 — P3-04 done: `/account/login` (одинаковое сообщение для неверного пароля и неизвестной почты, блокировка, «Запомнить меня»), `/account/logout` (GET — подтверждение, выход только POST), панель аккаунта в layout, `aria-invalid`/`aria-describedby` в формах; тесты 597 + 4 skipped (WSL — SAC снова блокирует DLL); ревью APPROVE с 1-го круга.
- 2026-10-16 — P3-03 done: Identity по ADR-13 (`AddIdentityCore` + cookie `.ProgChecklist.Auth`, 401/403 для `/api`), русские ошибки Identity, страница `/account/register`, цвет ошибок через `--danger`; тесты 576 + 4 skipped; ревью APPROVE с 1-го круга.
- Ранее: P0, P1, P2 (итоги одобрены 2026-10-02 и 2026-10-16); P3-01, P3-02 done (слой данных, локальный Postgres и CI); план P3 одобрен 2026-10-16 (юр. требования — GDPR/DSGVO).
