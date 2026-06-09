# FitnessHouseNewsBot

Blazor Server приложение и background service для мониторинга новостей Fitness House и отправки важных уведомлений в чат VK.

## Что делает проект

- Периодически читает страницу новостей Fitness House.
- Фильтрует новости по ключевым словам клуба.
- Отправляет в VK только новости с alert-ключевыми словами.
- Сохраняет уже отправленные сообщения в `storage/sent-news.json`, чтобы не слать дубли.
- Показывает статус парсера и последние отправленные новости в web-интерфейсе.
- Поддерживает светлую и темную тему с сохранением выбора в браузере.

## Технологии

- .NET 10
- ASP.NET Core / Blazor Server
- Hosted Service
- HtmlAgilityPack
- Serilog
- VK Messages API

## Конфигурация

Конфигурация читается из секций `Parser` и `Vk`.

Пример локального `FitnessHouseNewsBot/appsettings.Development.json`:

```json
{
  "Parser": {
    "Url": "https://www.fitnesshouse.ru/clubs-news.html?id=9",
    "IntervalMinutes": 15,
    "ClubKeywords": [
      "Ярославль",
      "Нагорный"
    ],
    "AlertKeywords": [
      "режим работы",
      "технические работы",
      "недоступен"
    ]
  },
  "UiLock": {
    "Password": "admin"
  },
  "Vk": {
    "Token": "vk-token",
    "PeerId": 2000000
  }
}
```

## Запуск

```powershell
cd FitnessHouseNewsBot
dotnet run
```

## Сборка

```powershell
dotnet build .\FitnessHouseNewsBot.sln
```

## Доступ к UI

- `/` - страница ввода пароля.
- `/dashboard` - панель управления парсером.
- Пароль хранится в секции `UiLock:Password`.
- После успешного входа в `sessionStorage` сохраняется только флаг доступа для текущей вкладки.
- Сам пароль в браузер не сохраняется.

## Docker

Build image:

```powershell
docker build -t fitness-house-news-bot .
```

Run container:

```powershell
docker run --rm `
  -p 8080:8080 `
  -e UiLock__Password="change-me" `
  -e Vk__Token="vk-token" `
  -e Vk__PeerId="2000000" `
  -v ${PWD}/storage:/app/storage `
  fitness-house-news-bot
```

Parser arrays can also be overridden with environment variables:

```powershell
-e Parser__ClubKeywords__0="Ярославль" `
-e Parser__ClubKeywords__1="Нагорный" `
-e Parser__AlertKeywords__0="режим работы" `
-e Parser__AlertKeywords__1="технические работы"
```
