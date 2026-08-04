- Шрифт
-- Тест на картинках: Perpetia Titling MT 26
- Цвета:
-- Фиолетовый: 8260E5
-- Тень: EFF4F0
-- Фон: F9FFFB. Opacity: 185
- Иконки для Markdown: https://gist.github.com/rxaviers/7360908
- Компоненты Blazor: https://mudblazor.com/
- Компонент Markdown: https://github.com/MyNihongo/MudBlazor.Markdown
- Safe storage of app secrets in development in ASP.NET Core: https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-6.0&tabs=windows


Описание хранилищ
- IMemoryCache - хранение данных в памяти. Первый уровень доступа
- ILocalStorage - хранение в браузере. Второй уровень доступа
- IDataSource - источник данных. Используется если необходимо акутализировать IMemoryCache и ILocalStorage
- IStorage - общий интерфейс хранения данных. Сслается на 3 уровня выше

При хранения UserProfile
- Нужно завести IMemoryCache<string, UserProfile> - используется для быстрого доступа к данным из памяти по ключу.
- Нужно завести ILocalStorage<UserProfile> - доступ к записи в LocalStorage браузера
- Нужно завести IDataSource<string, UserProfile> - получение данных из Jira для синхронизации с другими хранилищами
- Нужно завести IStorage<string, UserProfile> - единая точка входа в хранилище


Создание миграции
```
dotnet ef migrations add Migration_Name --startup-project ../Chronos.Web --context ApplicationDbContext
```

Применение миграции
```
dotnet ef database update --startup-project ../Chronos.Web --context ApplicationDbContext
```

Prompts
```
Translate the text into English
format it as an article for publication on the website
Come up with a short title for this text to be published on the website
Summarize the article in 1-2 sentences for publication on the website
```