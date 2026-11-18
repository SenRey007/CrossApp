using System.Text.Encodings.Web;
using System.Text.Json;

namespace Core.Storage;

public static class JsonFile
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true, // читаємо й старі файли з Id/Isbn (лаба 5)
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // кирилиця без \uXXXX
    };

    // Атомарний запис: спершу тимчасовий файл поруч, потім підміна цілого файлу.
    public static void Save<TDto>(string path, IEnumerable<TDto> items)
    {
        string full = Path.GetFullPath(path);
        string tmp = full + ".tmp";
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);

        File.WriteAllText(tmp, JsonSerializer.Serialize(items.ToList(), Options));
        File.Move(tmp, full, overwrite: true);
    }

    // Читає список DTO й перетворює на сутності: ті самі інваріанти (FromDto), номер запису в помилці.
    public static IReadOnlyList<TEntity> Load<TDto, TEntity>(string path, Func<TDto, TEntity> fromDto)
        where TDto : class
    {
        string full = Path.GetFullPath(path);
        if (!File.Exists(full))
            return [];

        List<TDto?> dtos = JsonSerializer.Deserialize<List<TDto?>>(File.ReadAllText(full), Options) ?? [];

        var result = new List<TEntity>(dtos.Count);
        for (int i = 0; i < dtos.Count; i++)
        {
            TDto? dto = dtos[i];
            if (dto is null)
                throw new DomainException($"Запис {i + 1}: порожній елемент.");

            try
            {
                result.Add(fromDto(dto));
            }
            catch (ArgumentException ex)
            {
                throw new DomainException($"Запис {i + 1}: {ex.Message.ReplaceLineEndings(" ")}", ex);
            }
        }

        return result;
    }
}
