using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using PixelArt.Models;

namespace PixelArt.Services;

public class SaveService
{
    private readonly string _path;

    public SaveService()
    {
        _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PixelArt",
            "save.json.gz"
        );
    }

    public void Save(SaveData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

        using var file = File.Create(_path);
        using var gzip = new GZipStream(file, CompressionLevel.Optimal);

        JsonSerializer.Serialize(gzip, data);
    }

    public SaveData Load()
    {
        if (!File.Exists(_path))
        {
            return new SaveData();
        }

        using var file = File.OpenRead(_path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);

        return JsonSerializer.Deserialize<SaveData>(gzip) ?? new SaveData();
    }
}