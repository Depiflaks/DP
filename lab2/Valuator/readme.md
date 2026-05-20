Для бубнты надо поставить редис

```bash
sudo apt install redis-server
```

Запустить
```bash
redis-server
dotnet add package StackExchange.Redis
```

Запустить через докер
```bash
docker run --name redis -p 6379:6379 -d redis
```

Прибить локальный редис
```bash
sudo lsof -i :6379
```

Сбилдить c++++
```bash
dotnet run
```

Зайти в редис
```bash
docker exec -it redis redis-cli
KEYS *
GET "..."
```