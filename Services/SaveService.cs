using System;
using System.IO;
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
            "save.json"
        );
    }
    
    public void Save(SaveData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

        var json = JsonSerializer.Serialize(data);
        File.WriteAllText(_path, json);
    }
    
    public SaveData Load()
    {
        if (!File.Exists(_path))
        {
            return new SaveData();
        }

        var json = File.ReadAllText(_path);
        
        return JsonSerializer.Deserialize<SaveData>(json) ?? new SaveData();
    }
}