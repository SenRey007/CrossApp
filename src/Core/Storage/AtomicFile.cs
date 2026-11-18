namespace Core.Storage;

public static class AtomicFile
{
    // Спершу тимчасовий файл поруч, потім підміна цілого файлу.
    public static void WriteAllText(string path, string content)
    {
        string full = Path.GetFullPath(path);
        string tmp = full + ".tmp";
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);

        File.WriteAllText(tmp, content); // UTF-8 без BOM
        File.Move(tmp, full, overwrite: true);
    }
}