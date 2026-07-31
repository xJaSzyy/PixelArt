using System.Collections.Generic;
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

    public void LoadLevels(int buttonsPerRow, int buttonSize)
    {
        if (Levels.Count > 0)
        {
            return;
        }
        
        Levels.Clear();
        for (var i = 0; i < _levelsCount; i++)
        {
            var texture = _contentManager.Load<Texture2D>($"Images/img{i}");

            var column = i % buttonsPerRow;
            var row = i / buttonsPerRow;

            var rectangle = new Rectangle(
                column * buttonSize,
                row * buttonSize,
                buttonSize,
                buttonSize);

            var level = new LevelData
            {
                Id = i,
                Texture = texture,
                Button = new Button(texture, rectangle)
            };
            
            Levels.Add(level);
            
            _processorService.ChangeLevel(level);
            _processorService.Generate();
        }
    }
}