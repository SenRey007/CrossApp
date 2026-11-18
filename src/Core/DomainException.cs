namespace Core;

public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}




// dotnet test
//# звичайний імпорт у пам'ять: 10 записів, 10 подій
/*
dotnet run --project src/Cli -- --import

# файлове сховище: перший запуск 10, другий 0 (ідемпотентність)
    dotnet run --project src/Cli -- --import --file
dotnet run --project src/Cli -- --import --file

# скасування за таймаутом (1000 мс) -> «Імпорт скасовано.»
dotnet run --project src/Cli -- --import --slow --timeout=1000

# скасування через Ctrl+C: запустіть і натисніть Ctrl+C протягом 3 секунд
dotnet run --project src/Cli -- --import --slow

# зіпсований файл -> «Дані відхилено: Запис 3: ...»
    dotnet run --project src/Cli -- --import data/copies-bad.json

# неіснуючий файл -> «Файл не знайдено: ...»
dotnet run --project src/Cli -- --import data/no-such.json
*/

/*
# демо: потоковий імпорт, зв'язані токени, смужка прогресу
dotnet run --project src/Cli -- --async-extra

# покриття
Select-String -Path tests\Core.Tests\Core.Tests.csproj -Pattern coverlet
dotnet test --collect:"XPlat Code Coverage"
*/

