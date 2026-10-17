using Core.Domain;

namespace Core;

public static class SampleData
{
    // Дата для звітів: фіксована, щоб вивід був відтворюваним.
    // У бойовому коді було б DateOnly.FromDateTime(DateTime.Today).
    public static readonly DateOnly ReferenceDate = new(2026, 10, 15);

    // З лаб. 5–6: НЕ змінювати.
    public static IEnumerable<BookCopy> Copies() =>
    [
        BookCopy.Create("C-001", "978-966-03-1234-5"),
        BookCopy.Create("C-002", "978-617-12-5678-9"),
        BookCopy.Create("C-003", "978-966-97-1111-2"),
        BookCopy.Create("C-004", "978-966-97-2222-3"),
        BookCopy.Create("C-005", "978-617-12-3333-4"),
        BookCopy.Create("C-006", "978-966-03-4444-5"),
    ];

    // 15 видань із різних десятиліть (для групування).
    public static IReadOnlyList<Book> Books() =>
    [
        Book.Create("B-001", "978-966-03-1234-5", "Кобзар", 1840, "Тарас Шевченко"),
        Book.Create("B-002", "978-617-12-5678-9", "Тіні забутих предків", 1912, "Михайло Коцюбинський"),
        Book.Create("B-003", "978-966-97-1111-2", "Лісова пісня", 1911, "Леся Українка"),
        Book.Create("B-004", "978-966-97-2222-3", "Місто", 1928, "Валер'ян Підмогильний"),
        Book.Create("B-005", "978-617-12-3333-4", "Собор", 1968, "Олесь Гончар"),
        Book.Create("B-006", "978-966-03-4444-5", "Захар Беркут", 1883, "Іван Франко"),
        Book.Create("B-007", "978-617-12-5555-6", "Земля", 1902, "Ольга Кобилянська"),
        Book.Create("B-008", "978-966-97-6666-7", "Хіба ревуть воли, як ясла повні?", 1875, "Панас Мирний"),
        Book.Create("B-009", "978-617-12-7777-8", "Тигролови", 1944, "Іван Багряний"),
        Book.Create("B-010", "978-966-03-8888-9", "Дикий Мед", 1963, "Леонід Первомайський"),
        Book.Create("B-011", "978-966-03-1010-1", "Intermezzo", 1908, "Михайло Коцюбинський"),
        Book.Create("B-012", "978-966-03-1212-3", "Fata morgana", 1910, "Михайло Коцюбинський"),
        Book.Create("B-013", "978-617-12-1414-5", "Мина Мазайло", 1929, "Микола Куліш"),
        Book.Create("B-014", "978-966-97-1515-6", "Лис Микита", 1890, "Іван Франко"),
        Book.Create("B-015", "978-966-03-1616-7", "Маруся Чурай", 1979, "Ліна Костенко"),
    ];

    // 18 примірників на 10 різних видань (від 1 до 3 примірників на видання).
    public static IReadOnlyList<BookCopy> ReportCopies() =>
    [
        BookCopy.Create("RC-01", "978-966-03-1234-5"),
        BookCopy.Create("RC-02", "978-966-03-1234-5"),
        BookCopy.Create("RC-03", "978-966-03-1234-5"),
        BookCopy.Create("RC-04", "978-617-12-5678-9"),
        BookCopy.Create("RC-05", "978-617-12-5678-9"),
        BookCopy.Create("RC-06", "978-966-97-1111-2"),
        BookCopy.Create("RC-07", "978-966-97-1111-2"),
        BookCopy.Create("RC-08", "978-966-97-2222-3"),
        BookCopy.Create("RC-09", "978-617-12-3333-4"),
        BookCopy.Create("RC-10", "978-617-12-3333-4"),
        BookCopy.Create("RC-11", "978-617-12-3333-4"),
        BookCopy.Create("RC-12", "978-966-03-4444-5"),
        BookCopy.Create("RC-13", "978-966-03-4444-5"),
        BookCopy.Create("RC-14", "978-617-12-5555-6"),
        BookCopy.Create("RC-15", "978-617-12-7777-8"),
        BookCopy.Create("RC-16", "978-617-12-7777-8"),
        BookCopy.Create("RC-17", "978-966-03-1212-3"),
        BookCopy.Create("RC-18", "978-966-03-1616-7"),
    ];

    // 14 видач п'яти читачів. Метод видає відповідні примірники зі списку (стан змінюється!).
    public static IReadOnlyList<Loan> ReportLoans(IReadOnlyList<BookCopy> copies)
    {
        static DateOnly Day(int month, int day) => new(2026, month, day);

        Loan Make(string id, string copyId, string readerId, DateOnly issuedOn, DateOnly? returnedOn = null)
        {
            Loan loan = Loan.Open(id, copies.First(c => c.Id == copyId), readerId, issuedOn);
            if (returnedOn is { } returned)
                loan.Close(returned);
            return loan;
        }

        return
        [
            Make("L-01", "RC-01", "R-001", Day(9, 1),  Day(9, 10)),
            Make("L-02", "RC-02", "R-001", Day(9, 5)),
            Make("L-03", "RC-04", "R-001", Day(10, 8)),
            Make("L-04", "RC-06", "R-002", Day(9, 10), Day(9, 20)),
            Make("L-05", "RC-07", "R-002", Day(9, 20)),
            Make("L-06", "RC-09", "R-002", Day(10, 10)),
            Make("L-07", "RC-10", "R-003", Day(9, 2),  Day(9, 15)),
            Make("L-08", "RC-11", "R-003", Day(9, 12)),
            Make("L-09", "RC-12", "R-004", Day(10, 1)),
            Make("L-10", "RC-14", "R-004", Day(9, 28), Day(10, 5)),
            Make("L-11", "RC-15", "R-005", Day(10, 12)),
            Make("L-12", "RC-03", "R-001", Day(10, 2)),
            Make("L-13", "RC-08", "R-002", Day(10, 3)),
            Make("L-14", "RC-17", "R-003", Day(9, 25)),
        ];
    }
}