# Общий входной прокси сервера

Один Traefik на Docker-хост. Он держит порт 443 и по имени хоста отдаёт каждый запрос нужному
контейнеру. Chronos подключается к нему как один из сайтов. Любой другой сайт на этом же сервере
подключается так же, и [docker-compose.yml](docker-compose.yml) здесь при этом не меняется.

## Схема

```
браузер ──HTTPS──> балансировщик (публичный сертификат *.example.com)
                         │ HTTPS на <сервер>:443
                         ▼
                  edge-traefik ──HTTP──> chronos:8080
                         │        ──HTTP──> другой-сайт:порт
```

Если публичный TLS завершает внешний балансировщик, который ходит на сервер тоже по HTTPS, но
сертификат бэкенда не проверяет, Traefik отвечает ему своим самоподписанным сертификатом. Его он
генерирует сам. Когда DNS-запись вида `*.example.com` указывает на балансировщик, для нового сайта
DNS и сертификат трогать не нужно.

## Установка, один раз на сервер

```bash
docker network create edge
```
```bash
mkdir -p ~/edge && cd ~/edge   # сюда кладётся docker-compose.yml из этой папки
```
```bash
docker compose up -d
```

Traefik читает метки контейнеров через `/var/run/docker.sock`, смонтированный только на чтение. Учтите,
что доступ к сокету Docker по силе равен root на хосте.

## Как подключить сайт

В compose-файле сайта сервис подключается к внешней сети `edge` и получает метки:

```yaml
services:
  mysite:
    image: ...
    networks: [ default, edge ]
    labels:
      - traefik.enable=true
      - traefik.http.routers.mysite.rule=Host(`mysite.example.com`)
      - traefik.http.routers.mysite.entrypoints=websecure
      - traefik.http.routers.mysite.tls=true
      - traefik.http.services.mysite.loadbalancer.server.port=8080   # порт внутри контейнера

networks:
  edge:
    external: true
```

После `docker compose up -d` сайт сразу доступен на `https://mysite.example.com`, Traefik
перезапускать не нужно. Имя роутера (`mysite`) должно быть уникальным на сервере. Публиковать порт
сайта наружу (`ports:`) не нужно: Traefik ходит к нему через сеть `edge`.

Для Chronos эти метки уже есть в [deploy/docker/docker-compose.edge.yml](../docker/docker-compose.edge.yml),
они включаются через `.env` (см. [deploy/docker/README.md](../docker/README.md)).

## Если на сервере уже есть свой nginx на 443

Два прокси на одном порту работать не могут. Если другой проект приносит собственный nginx, который
сам разводит свои сайты, поставьте его за Traefik:

1. Уберите у этого nginx `ports:` и подключите его контейнер к сети `edge`.
2. Добавьте контейнеру метки, по которым Traefik отдаёт ему все хосты того проекта:

   ```yaml
   labels:
     - traefik.enable=true
     - traefik.http.routers.otherproject.rule=HostRegexp(`^.+\.other\.example\.com$`)
     - traefik.http.routers.otherproject.entrypoints=websecure
     - traefik.http.routers.otherproject.tls=true
     - traefik.http.services.otherproject.loadbalancer.server.port=80   # HTTP-порт nginx в контейнере
   ```

   TLS в этой схеме завершает Traefik, поэтому nginx за ним достаточно слушать обычный HTTP.
3. Остальная механика того nginx (свои конфиги сайтов, `nginx -s reload`) работает как раньше.

## Проверка

- Список контейнеров, которые Traefik видит: `docker logs edge-traefik`. Ошибки разбора меток
  попадают туда же.
- Проверка прямо с сервера, минуя балансировщик:
  `curl -sk https://127.0.0.1/ -H "Host: mysite.example.com"`.
- Если на хост нет ни одного роутера, Traefik отвечает `404 page not found`.
