# Chronos в Docker

Как развернуть Chronos на Linux-сервере в контейнере, обновлять его и откатывать (issue #325).

## Как это устроено

1. Push тега `v*` запускает [Release](../../.github/workflows/release.yml). Он собирает и тестирует
   проект, затем собирает образ из [Dockerfile](../../Dockerfile) и пушит его в
   `ghcr.io/tolmachev-pravo/chronos` с тегами `X.Y.Z` и `latest`. Pre-release (`v1.9.0-rc.1`)
   получает только тег своей версии, `latest` не трогается.
2. Публикация релиза запускает [Deploy to Docker](../../.github/workflows/deploy-docker.yml) на
   self-hosted раннере, который стоит на Docker-сервере. Workflow скачивает образ нужной версии,
   записывает её в `.env`, перезапускает контейнер и ждёт `Healthy` от `/health/live`.

В контейнере приложение слушает HTTP на порту `8080` и работает от непривилегированного пользователя
`app`. Всё, что должно пережить пересоздание контейнера, лежит в томе `/data`:

| Что | Путь в контейнере | Настройка |
| --- | --- | --- |
| База SQLite | `/data/JiraCopilot.sqlite3` | `ConnectionStrings__Chronos` |
| Ключи DataProtection | `/data/DataProtection-Keys` | `DataProtection__KeysPath` |
| Логи | `/data/logs/log-YYYYMMDD.txt` | `Serilog__WriteTo__0__Args__path` |

Ключи DataProtection шифруют секреты, которые пользователи сохраняют в расширениях. **Без ключей эти
секреты не расшифровать**, поэтому база и ключи переносятся и бэкапятся только вместе.

## Первоначальная настройка сервера

Нужны Docker Engine и Docker Compose v2.24 или новее (`docker compose version`).

1. Создайте папку, например `/opt/chronos`, и положите в неё файлы из
   этой папки ([deploy/docker](.)):
   - `docker-compose.yml`, а за общим Traefik также `docker-compose.edge.yml` (см. «Обратный прокси»);
   - `.env.example` под именем `.env`. Укажите в нём `CHRONOS_TAG` (версию без `v`), порт и
     часовой пояс;
   - при необходимости `chronos.env.example` под именем `chronos.env`, чтобы переопределить настройки
     из `appsettings.json`, например `Jira__Url`.
2. Если образ в GHCR приватный, один раз выполните на сервере `docker login ghcr.io` с PAT, у которого
   есть право `read:packages`. Публичный образ скачивается без логина. Видимость меняется в настройках
   пакета на GitHub: Packages → chronos → Package settings → Change visibility.
3. Запустите:

   ```bash
   cd /opt/chronos && docker compose pull && docker compose up -d
   ```

4. Проверьте: `curl http://127.0.0.1:8080/health/live` должен ответить `Healthy`.

### Обратный прокси

По умолчанию порт открыт только на `127.0.0.1`. HTTPS завершается на прокси перед контейнером.
Blazor Server работает через WebSocket, поэтому прокси должен пропускать `Upgrade`.

**Общий Traefik на сервере.** Подходит, когда на хосте несколько сайтов или внешний балансировщик
ходит на сервер по HTTPS на 443: запросы по имени хоста разводит общий Traefik из
[deploy/edge](../edge), и рядом можно поднимать другие сайты. Поставьте Traefik по его README, затем
раскомментируйте в `.env` две строки:

```bash
COMPOSE_FILE=docker-compose.yml:docker-compose.edge.yml
CHRONOS_HOST=chronos.example.com
```

Положите `docker-compose.edge.yml` рядом с `docker-compose.yml` и выполните `docker compose up -d`.

**Свой прокси на хосте.** Пример для nginx:

```nginx
location / {
    proxy_pass         http://127.0.0.1:8080;
    proxy_http_version 1.1;
    proxy_set_header   Upgrade $http_upgrade;
    proxy_set_header   Connection $http_connection;
    proxy_set_header   Host $host;
    proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header   X-Forwarded-Proto $scheme;
    proxy_read_timeout 120s;
}
```

Контейнер запускается с `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`, поэтому видит исходную схему
`https` и адрес клиента из заголовков прокси.

### Раннер для автодеплоя

1. Зарегистрируйте на сервере self-hosted раннер для репозитория (Settings → Actions → Runners).
   Метки `self-hosted` и `linux` раннер получает автоматически. Пользователь, от которого он работает,
   должен быть в группе `docker`.
2. Задайте переменную репозитория `COMPOSE_DIR` с путём к папке из шага 1, например `/opt/chronos`
   (Settings → Secrets and variables → Actions → Variables).

Пока `COMPOSE_DIR` не задана, публикация релиза Docker-деплой не запускает. Deploy to IIS теперь
требует раннер с меткой `windows`, поэтому Linux-раннер его задания не возьмёт.

## Обновление и откат

Обычный путь — опубликовать релиз: Deploy to Docker развернёт его сам.

Откат и повторный деплой: запустите Deploy to Docker вручную (Actions → Deploy to Docker → Run workflow)
и укажите тег, например `v1.7.2`. То же самое можно сделать руками на сервере: поменяйте `CHRONOS_TAG` в
`.env` и выполните `docker compose pull && docker compose up -d`.

Миграции базы применяются при старте приложения. Откат на версию старше последней миграции оставит
базу в новой схеме, поэтому перед таким откатом нужен бэкап.

## Бэкап

Все данные лежат в томе `chronos-data`. Копия делается одним архивом:

```bash
docker run --rm -v chronos-data:/data -v "$PWD":/backup debian:bookworm-slim \
  tar czf /backup/chronos-data-$(date +%F).tgz -C /data .
```

## Перенос данных с IIS

1. Остановите пул приложений на IIS-сервере, чтобы база не менялась во время копирования.
2. Заберите из папки сайта файл `JiraCopilot.sqlite3` и папку `DataProtection-Keys` целиком.
3. На Docker-сервере создайте том, положите в него файлы и отдайте их пользователю `app`
   (UID 1654 в образах .NET 8):

   ```bash
   docker compose down
   docker run --rm -v chronos-data:/data -v /path/to/copied:/src debian:bookworm-slim \
     sh -c 'cp -a /src/JiraCopilot.sqlite3 /src/DataProtection-Keys /data/ && chown -R 1654:1654 /data'
   docker compose up -d
   ```

4. Войдите в приложение и откройте настройки расширения с сохранённым паролем. Если секрет
   читается, ключи перенеслись правильно.

Имя приложения DataProtection (`Chronos`) задано в коде одинаково для IIS и Docker, поэтому ключи с
IIS подходят контейнеру без изменений.

## Локальная сборка образа

```bash
docker build --build-arg VERSION=0.0.0-local -t chronos:local .
docker run --rm -p 8080:8080 -v chronos-local:/data chronos:local
```

Без `VERSION` приложение покажет версию `0.0.0-alpha.0`: в контекст сборки не попадает `.git`, и
MinVer не видит теги.
