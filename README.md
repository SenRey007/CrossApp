\# CrossApp

Наскрізний проєкт з крос-платформного програмування.

Предметна область: Бібліотека. Сутності: Book, BookCopy, Reader, Loan.

Призначення: облік видач примірників книг читачам і повернень.

Структура solution

CrossApp/
├── CrossApp.sln
├── README.md
├── .gitignore
└── src/
    ├── Core/
    │   ├── Core.csproj
    │   └── EnvironmentInfo.cs
    └── Cli/
        ├── Cli.csproj
        └── Program.cs

\## Запуск

dotnet build

dotnet run --project src/Cli

Publish:

dotnet publish src/Cli -c Release -r win-x64 --self-contained true
dotnet publish src/Cli -c Release -r win-x64 --self-contained false


| RID | Режим | Розмір publish | Потрібен runtime |
|---------|----------------|---------|-----------------------|
| win-x64 | self-contained | 77 МБ   |         ні            |
| win-x64 | self-contained | 0.2 МБ  |    так (.NET 8,10)    |

\## Середовище

.NET SDK 8.0, Windows x64	



