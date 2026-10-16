# Статус

**Фаза:** P3 · **Состояние:** в работе · **Следующая задача:** P3-04 Вход, выход и шапка

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
- P3-04 (из ревью P3-03): у формы регистрации подсказка пароля без `aria-describedby`, у невалидных полей нет `aria-invalid` — сделать в форме входа и поправить регистрацию заодно. После регистрации вход непостоянный (сессионная cookie); 30 дней — только с «Запомнить меня». `AccessDeniedPath` пока `/account/login`. Можно добавить тесты: returnUrl `/\evil.example`, флаги `httponly`/`samesite=lax` в Set-Cookie.
- P5: ключи Data Protection (cookie входа) хранить персистентно, иначе после перезапуска контейнера все разлогинятся.
- Postgres-тесты на Windows: `Host=127.0.0.1`, не `localhost` (иначе IPv6-попытка, 1+ мин на тест). Пароль в `.env` без пробелов, `;`, `$`, кавычек (файл исполняется bash).
- P3 (ревью P3-01): одна `SqliteConnection` на `AppFactory` и все её `WithWebHostBuilder`-потомки — состояние БД общее, соединение не потокобезопасно; при параллельных запросах в тестах API (P3-06) возможна нестабильность — пересмотреть (соединение на фабрику-потомка или уникальные пользователи в каждом тесте).
- P3-05: Npgsql не пишет `DateTimeOffset` с ненулевым смещением в `timestamptz` — нормализовать `CompletedAt` в UTC; SQLite этого не ловит, только Postgres-тесты (P3-02).
- Smart App Control в этой сессии не блокировал DLL — `dotnet test` на Windows прошёл; WSL-скрипт остаётся запасным путём.
- `scripts/test-wsl.ps1` запускать как `powershell -ExecutionPolicy Bypass -File scripts/test-wsl.ps1` (иначе политика выполнения блокирует).

## Лог (последние 5 записей, новые сверху)
- 2026-10-16 — P3-03 done: Identity по ADR-13 (`AddIdentityCore` + cookie `.ProgChecklist.Auth`, 401/403 для `/api`), русские ошибки Identity, страница `/account/register`, цвет ошибок через `--danger`; тесты 576 + 4 skipped; ревью APPROVE с 1-го круга.
- 2026-10-16 — P3-02 done: `compose.yaml` (postgres:17, 127.0.0.1), `.env.example`, `run-wsl.sh` берёт подключение из `.env`, `MigrateOnStartup` в Development, `UserSecretsId`, `PostgresFact` + 4 теста на настоящем Postgres (локально прошли на 5433), CI-джоба `test-postgres` с проверкой, что тесты реально выполнились; тесты 559 + 4 skipped; ревью APPROVE со 2-го круга (испорченная команда в README); CI зелёный, `test-postgres` выполнил тесты (run 38948765241).
- 2026-10-16 — P3-01 done: проект `ProgChecklist.Data` (AppDbContext с Identity + `UserTopicProgress`), миграция `InitialCreate`, `AddAppDatabase` с fail-fast, тесты на SQLite через общую `AppFactory`; EF Core 10.0.13, Npgsql 10.0.3; тесты 559/559 (Windows и WSL); ревью APPROVE с 1-го круга.
- 2026-10-16 — План P3 (P3-01…P3-10, ADR-12…14) одобрен человеком; юр. требования — GDPR/DSGVO (оператор в Германии).
- Ранее: P0, P1, P2 (итоги одобрены 2026-10-02 и 2026-10-16).
