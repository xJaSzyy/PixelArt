using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using PixelArt.Models;

namespace PixelArt.Services;

public class SaveService
{
    public void Save(SaveData data)
    {
        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PixelArt",
            "save.json"
        );

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        string json = JsonSerializer.Serialize(data);

        File.WriteAllText(path, json);
        
        Console.WriteLine("Level saved!");
    }
    
    public SaveData Load()
    {
        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PixelArt",
            "save.json"
        );

        if (!File.Exists(path))
            return new SaveData();

        string json = File.ReadAllText(path);

        Console.WriteLine("Level loaded!");
        
        return JsonSerializer.Deserialize<SaveData>(json)
               ?? new SaveData();
    }
}

public class SaveData
{
    public List<LevelData> Levels { get; set; } = [];
}