Для бубнты надо поставить dotnet

```bash
sudo apt install -y dotnet-sdk-10.0
dotnet add package Microsoft.AspNetCore.DataProtection.StackExchangeRedis
```

Запустить и опустить:

```bash
./bin/valuator_up
./bin/valuator_down
```

Для тестирования:

```bash
for i in $(seq 1 10); do curl http://localhost:8080/instance; echo; done
```

```mermaid
C4Container
title Диаграмма контейнеров (C4) - Распределенная система Valuator

Person(user, "Пользователь", "Использует веб-интерфейс для загрузки текста")

System_Boundary(system, "Система Valuator") {
    Container(nginx, "Обратный прокси", "Nginx", "Балансирует трафик между экземплярами веб-приложения")
    Container(valuator, "Valuator Web App", "ASP.NET Core", "Обеспечивает UI, вычисляет плагиат, ставит задачи")
    ContainerDb(redis, "Общая база данных", "Redis", "Хранит тексты, вычисленные метрики и ключи шифрования")
    ContainerQueue(rabbitmq, "Брокер сообщений", "RabbitMQ", "Асинхронная очередь задач (rank-calculation)")
    Container(worker, "Rank Calculator", ".NET Worker", "Конкурирующий потребитель, вычисляет ранг текста")
}

Rel_D(user, nginx, "Вводит текст, смотрит результаты", "HTTP")
Rel_D(nginx, valuator, "Проксирует трафик", "HTTP")

Rel_R(valuator, redis, "Пишет текст, читает оценку", "TCP / Redis")
Rel_D(valuator, rabbitmq, "Отправляет TextId", "TCP / AMQP")

Rel_D(rabbitmq, worker, "Доставляет TextId", "TCP / AMQP")
Rel_R(worker, redis, "Запрашивает текст, сохраняет ранг", "TCP / Redis")
```
