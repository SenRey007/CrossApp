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

## Сервісний шар (lab05)

IBookCopyStore — контракт: List, GetById, Add, Update, Remove.
Реалізації: InMemoryBookCopyStore (у пам'яті), FileBookCopyStore (JSON-файл, кеш + Flush після зміни).
LendingService(IBookCopyStore) — AddBook, IssueCopy, ReturnCopy, All, Find; залежить лише від інтерфейсу.
Composition root — LendingDemo.Run у Cli: вибір реалізації прапорцем --file.


## Додаткове завдання (lab05)

CachingBookCopyStore — декоратор над IBookCopyStore: кешує List() до наступної зміни (Add/Update/Remove).
IBookCopyStore.Find(Func<BookCopy,bool>) — default interface method, пошук без змін у реалізаціях.
StoreFactory.Create(args) — вибір і композиція реалізацій (file/cache) в одному місці замість Program.cs.


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



