# PA6. Сегментирование базы данных

Реализация на основе `lab5`: сохранены RabbitMQ, журнал событий и обновление рейтинга через SignalR.

## Запуск

```bash
cd lab6
docker compose up --build
```

Приложение: http://localhost:8080. RabbitMQ Management: http://localhost:15672, логин и пароль `valuator`.

Для запуска нескольких экземпляров компонентов:

```bash
./scripts/valuator_up 3 4
```

## Распределение данных

| Страна | Сегмент | Переменная окружения | Адрес вне Docker |
| --- | --- | --- | --- |
| Russia | RU | DB_RU | localhost:6001 |
| France, Germany | EU | DB_EU | localhost:6002 |
| UAE, India | ASIA | DB_ASIA | localhost:6003 |

`DB_MAIN` по умолчанию равна `localhost:6000`. В Docker Compose все четыре переменные заданы адресами соответствующих контейнеров с портом `6379`. Redis работают независимо, без Redis Cluster.

Основная БД хранит только `SHARD-{id} → RU/EU/ASIA`. В выбранном сегменте хранятся `TEXT-{id}`, `COUNTRY-{id}`, `SIMILARITY-{id}` и `RANK-{id}`.

Общий код маршрутизации находится в `Sharding` и подключён к обоим проектам. Веб-приложение и обработчик рейтинга при каждом чтении или обновлении по ID сначала читают карту в `DB_MAIN` и выводят `LOOKUP: {id}, {region}`. Для отсутствующего ID выводится `UNKNOWN`. При создании текста сегмент определяется по выбранной стране, после сохранения данных публикуется запись карты.

Проверка плагиата сравнивает текст со всеми ранее сохранёнными текстами: перечисляет ID из карты, затем читает каждый текст через маршрутизатор. Ключи ASP.NET Data Protection хранятся в общем томе `data-protection-keys`, доступном всем экземплярам веб-приложения.

## Проверка

Отправьте тексты с разными странами и дождитесь рейтинга. Подставьте ID со страницы результата:

```bash
TEXT_ID=идентификатор-со-страницы-результата
docker compose exec redis-main redis-cli GET "SHARD-$TEXT_ID"
docker compose exec redis-eu redis-cli MGET "TEXT-$TEXT_ID" "COUNTRY-$TEXT_ID" "SIMILARITY-$TEXT_ID" "RANK-$TEXT_ID"
docker compose exec redis-main redis-cli --scan
docker compose logs valuator-app rank-calculator | rg 'LOOKUP:'
```

Для France и Germany данные должны находиться только в `redis-eu`; для остальных стран используйте соответствующий контейнер из таблицы. В `redis-main` должны присутствовать только ключи `SHARD-*`.

Остановка:

```bash
docker compose down
```
