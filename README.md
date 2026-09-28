# CrossApp

Наскрізний проєкт з крос-платформного програмування.

Предметна область: Бібліотека. Сутності: Book, BookCopy, Reader, Loan.

Призначення: облік видач примірників книг читачам і повернень.

Структура solution

```
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
```

## Інваріанти доменної моделі (lab04)

**BookCopy**
- Id примірника не порожній (ArgumentException) — Create
- ISBN не порожній (ArgumentException) — Create
- Не можна видати вже виданий примірник (InvalidOperationException) — Issue
- Не можна повернути невиданий примірник (InvalidOperationException) — Return

**Loan**
- Id видачі та читача не порожні (ArgumentException) — Open, FromDto
- Дата повернення не раніше дати видачі (ArgumentOutOfRangeException) — Close, FromDto
- Не можна закрити вже закриту видачу (InvalidOperationException) — Close
- FromDto проходить ті самі перевірки, що й Open

## Додаткове завдання (lab04)

**Book** (сутність з імпорту): id, ISBN, назва не порожні; ISBN має 10 або 13 цифр; рік 1450..поточний.
`BookEntityMapper` повертає `ImportResult<Book>` із помилками імпорту й доменних перевірок.

**LendingService**: читач не може мати більше 5 відкритих видач (InvalidOperationException).

**Loan: стани** Open → Returned, Open → Lost, Lost → Returned; решта переходів заборонена
(InvalidOperationException). FromDto перевіряє узгодженість стану, дати повернення й примірника.


## Команди
 
Build:
dotnet build
 
Run:
dotnet run --project src/Cli


Publish:

dotnet publish src/Cli -c Release -r win-x64 --self-contained true

dotnet publish src/Cli -c Release -r win-x64 --self-contained false


| RID | Режим | Розмір publish | Потрібен runtime |
|---------|---------------------|----------|-----------------------|
| win-x64 |    self-contained   | 70.67 МБ |         ні            |
| win-x64 | framework-dependent | 0.17 МБ  |    так (.NET 8)       |

\## Середовище

.NET SDK 9.0.100, runtime .NET 8.0.11, Windows x64



