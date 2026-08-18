using System.Collections.Generic;

namespace PixelArt.Models;

public class SaveData
{
    public int Coins { get; set; } = 0;
    public List<LevelData> Levels { get; set; } = [];
}