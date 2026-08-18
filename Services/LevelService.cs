using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PixelArt.Buttons;
using PixelArt.Models;

namespace PixelArt.Services;

public class LevelService
{
    private readonly ContentManager _contentManager;
    private readonly PixelProcessorService _processorService;

    public List<LevelData> Levels { get; set; } = [];
    
    private const int _levelsCount = 28;

    public LevelService(ContentManager contentManager, PixelProcessorService processorService)
    {
        _contentManager = contentManager;
        _processorService = processorService;
    }

    public void TryLoadLevels(int buttonsPerRow, int buttonSize, SaveService saveService)
    {
        if (Levels.Count > 0)
        {
            return;
        }

        var saveData = saveService.Load();

        bool useSaveData = saveData.Levels.Count == _levelsCount;
        
        Levels.Clear();
        for (var i = 0; i < _levelsCount; i++)
        {
            var texture = _contentManager.Load<Texture2D>($"Images/img{i + 1}");

            var column = i % buttonsPerRow;
            var row = i / buttonsPerRow;

            var rectangle = new Rectangle(
                column * buttonSize,
                row * buttonSize,
                buttonSize,
                buttonSize);

            var level = new LevelData();

            if (useSaveData)
            {
                level = saveData.Levels.First(x => x.Id == i);
            }

            level.Id = i;
            level.Texture = texture;
            level.Button = new Button(texture, rectangle);
            
            Levels.Add(level);
            
            _processorService.ChangeLevel(level);
            _processorService.Generate();
        }
    }
}