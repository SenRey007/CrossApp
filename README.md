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

## Доменні події (lab06)

LendingService.Changed — event EventHandler<LendingChangedEventArgs>: BookAdded, CopyIssued, CopyReturned.
Дані події: CopyId, ReaderId, DueDate, час. Подія піднімається лише після успішної зміни стану.
Core не пише в консоль і лог: підписники живуть у Cli (локальний метод + лямбда-лічильник), лог — logs/app.log.
FindAll(Func<BookCopy,bool>) — пошук з критерієм від викликача.

## Додаткове завдання (lab06)

INotificationSink (Core) + ConsoleSink/FileSink (Cli): sink передається LendingService через конструктор, CompositeSink розсилає на кілька.
BookCsvImporter.Load(path, Action<string> onSkipped): звіт про пропущений рядок у момент пропуску.
Порівняння event і sink, а також пояснення про async void — у звіті.

## Звіти LINQ (lab07)

ReportService (Core/Reports): TopReaders, Overdue, BooksByDecade, CopyTotals; приймають IEnumerable<T>, повертають IReadOnlyList<DTO> або DTO.
Підкоманди Cli: report-top, report-overdue, report-groups, report-sum, report-deferred, report-empty.

Коли виконується запит: усі методи ReportService закінчуються ToList(), тому запит виконується всередині виклику методу, а в Cli потрапляє готовий список.
У демо report-deferred запит source.Where(...) лише описано в момент оголошення. Перший Count() обробляє джерело й дає 8, потім у джерело додано примірник, і другий Count() виконує запит заново та дає 9.
Знімок ToList() зафіксував 9 і не змінюється після source.Clear(), тоді як сам запит після очищення бачить 0.

## Додаткове завдання (lab07)

Join/GroupJoin: BookAvailability (GroupJoin видань і примірників: видання без примірників лишаються з нулями), PopularTitles (Join видачі-примірники-видання + GroupBy + топ-N).
TopBy<T>(key, n, thenBy) — власний метод-розширення, використаний у TopReaders і PopularTitles; другий ключ гарантує однаковий порядок при нічиїх.

Синтаксис запитів: BooksByDecadeQuery записано через from/group/into/orderby/select. Результат збігається з методним варіантом (report-compare).
Для групування із сортуванням і проєкцією запитний синтаксис читається ближче до SQL і трохи зрозуміліший; методний коротший для простих ланцюжків і потрібен для операторів без ключових слів (Take, ToList, TopBy), тому в запитного варіанту довелося брати все в дужки заради .ToList().

Вимірювання Stopwatch (100 000 видач, Release, медіана із 7 замірів, report-bench):
- ланцюжок без проміжного ToList: <ваше число> мс
- з ToList у середині: <ваше число> мс
- запит, перебраний двічі: <ваше число> мс
- запит, матеріалізований один раз: <ваше число> мс
Висновок: <1–2 речення своїми словами за вашими числами>.

## Async, скасування, тести (lab08)

Тести: `dotnet test` (tests/Core.Tests, 15 перевірок; сховище в пам'яті, диск лише в одному тесті з тимчасовим файлом).
Імпорт: `dotnet run --project src/Cli -- --import [шлях]` (за замовчуванням data/copies.json; повторний запуск з --file додає 0 записів).
Перевірка скасування: `--import --slow --timeout=1000` (таймаут) або `--import --slow` і Ctrl+C -> «Імпорт скасовано.».
Поганий файл: `--import data/copies-bad.json` -> «Дані відхилено: Запис 3: ...».
Помилки: DomainException — бізнес-правила; ArgumentException — поганий аргумент; IOException/JsonException — технічні, не маскуються.

## Додаткове завдання (lab08)

ICopyStreamImporter / JsonCopyStreamImporter: потоковий імпорт через IAsyncEnumerable з [EnumeratorCancellation]; LendingService.ImportStreamAsync додає записи по мірі надходження (не атомарно).
ImportWithTimeoutAsync: CreateLinkedTokenSource(userToken, timeoutToken); таймаут -> TimeoutException, скасування користувачем -> OperationCanceledException (тести це розрізняють).
ImportAsync(..., IProgress<int> progress): відсотки; у Cli власна синхронна реалізація (Progress<T> у консолі викликає обробник у пулі потоків).
Покриття: dotnet test --collect:"XPlat Code Coverage" (звіт у tests/Core.Tests/TestResults, не комітиться).
Запуск демо: dotnet run --project src/Cli -- --async-extra

## Конфігурація (lab09)

Джерела (кожне наступне перекриває попереднє): appsettings.json -> appsettings.{Профіль}.json -> user-secrets -> змінні CROSSAPP_… -> CROSSAPP_DATA_PATH -> аргументи (--Ключ=значення).
Профіль обирає CROSSAPP_ENVIRONMENT (за замовчуванням Development).

| Ключ | Змінна середовища | За замовчуванням | Призначення |
|---|---|---|---|
| Storage:DataPath | CROSSAPP_Storage__DataPath (або проста CROSSAPP_DATA_PATH) | data | каталог даних: відносний рахується від каталогу застосунку, абсолютний береться як є |
| Storage:FileName | CROSSAPP_Storage__FileName | catalog.json | файл сховища примірників |
| Storage:BooksFileName | CROSSAPP_Storage__BooksFileName | books.json | файл експорту видань |
| Library:LoanDays | CROSSAPP_Library__LoanDays | 14 | строк видачі в днях |
| Logging:MinimumLevel | CROSSAPP_Logging__MinimumLevel | Information (Development: Debug, Production: Warning) | докладність логу |
| Integration:ApiKey | CROSSAPP_Integration__ApiKey | не задано | навчальний секрет (user-secrets або змінна, НЕ в appsettings) |

Профілі: Development (Debug, data), Production (Warning, data-prod).

PowerShell:  $env:CROSSAPP_DATA_PATH = "C:\Users\me\data"; dotnet run --project src/Cli -- --config
bash:        CROSSAPP_DATA_PATH=/tmp/data dotnet run --project src/Cli -- --config
Вкладений ключ: $env:CROSSAPP_Library__LoanDays = "7"   /   CROSSAPP_Library__LoanDays=7 dotnet run ...
Секрет: dotnet user-secrets set "Integration:ApiKey" "<ваш-ключ>" --project src/Cli
Команди: --config (показати налаштування), --export (записати видання й примірники в JSON), --load (прочитати назад).

## Додаткове завдання (lab09)

Валідація на старті: некоректні налаштування (порожній Storage:DataPath, Storage:FileName без .json, невідомий Storage:Format, Library:LoanDays <= 0, невідомий Logging:MinimumLevel, нечитабельна конфігурація) дають список помилок і код завершення 2 без винятку.
Аргументи: --data-path=<шлях> (= Storage:DataPath), --format=<json|csv> (= Storage:Format), будь-який ключ у вигляді --Розділ:Ключ=значення. Приймається лише форма --Ключ=значення; аргумент має найвищий пріоритет.
Формат видань: Storage:Format = json | csv (IBookCatalogFormat: JsonBookCatalogFormat, CsvBookCatalogFormat). Для csv Storage:BooksFileName має закінчуватися на .csv; CSV: роздільник «;», UTF-8, рядок заголовків, поля з «;» беруться в лапки.
Тести файлового сховища використовують Path.GetTempPath() і прибирають каталог у Dispose.

## HTTP API (lab10)

Запуск: dotnet run --project src/Api   (порт 5080 з launchSettings.json; альтернатива: --urls http://localhost:5080)
Scalar UI: http://localhost:5080/scalar/v1, документ OpenAPI: http://localhost:5080/openapi/v1.json (лише Development).

| Метод | Шлях | Тіло | Коди |
|---|---|---|---|
| GET | /api/books[?author=] | — | 200 |
| GET | /api/books/{id} | — | 200, 404 |
| POST | /api/books | SaveBookRequest | 201 (Location), 400 |
| PUT | /api/books/{id} | SaveBookRequest | 200, 400, 404 |
| DELETE | /api/books/{id} | — | 204, 404 |

SaveBookRequest: { "isbn", "title", "author", "year" }. Відповідь BookResponse: { id, isbn, title, author, year, copies }.
Приклад (PowerShell): $body | curl.exe -i -X POST http://localhost:5080/api/books -H "Content-Type: application/json" --data-binary "@-"
Помилки валідації: 400 application/problem+json зі словником errors { Поле: [повідомлення] }.

## Додаткове завдання (lab10)
| GET | /api/books?author=&search=&page=&pageSize= | — | 200 ({ items, total, page, pageSize }), 400 |
| POST | /api/books | SaveBookRequest | 201 (Location), 400, 409 (дубль ISBN) |
| PUT | /api/books/{id} | SaveBookRequest | 200, 400, 404, 409 |

Пагінація: page >= 1 (за замовчуванням 1), pageSize 1..100 (за замовчуванням 20); порядок: назва без урахування регістру, потім id.
Валідація: FluentValidation + ValidationFilter (AddEndpointFilter); DomainException -> 409 через DomainExceptionHandler.
Інтеграційні тести: dotnet test (tests/Api.Tests, WebApplicationFactory<Program>).

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



