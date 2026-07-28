using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelArt.Buttons;
using PixelArt.Interfaces;
using PixelArt.Models;
using PixelArt.Services;

namespace PixelArt.Scenes;

public class MenuScene : IScene
{
    private GraphicsDevice _graphicsDevice;
    private SpriteBatch _spriteBatch;
    
    private SceneService _sceneService;
    private MouseService _mouseService;
    private PixelProcessorService _processorService;
    
    private readonly List<LevelData> _levels = [];
    private const int _buttonSize = 128;
    
    public void Initialize(SceneService sceneService, MouseService mouseService, DrawService drawService)
    {
        
        _sceneService = sceneService;
        _mouseService = mouseService;
    }

    public void LoadContent(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _graphicsDevice = graphicsDevice;
        
        _spriteBatch = new SpriteBatch(graphicsDevice);

        const int buttonsCount = 25;

        if (_levels.Count != buttonsCount)
        {
            _levels.Clear();
            
            var buttonsPerRow = Math.Max(1, graphicsDevice.Viewport.Width / _buttonSize);

            _processorService = new PixelProcessorService();
        
            for (var i = 0; i < buttonsCount; i++)
            {
                var texture = content.Load<Texture2D>($"Images/img{i + 1}");

                var column = i % buttonsPerRow;
                var row = i / buttonsPerRow;

                var rectangle = new Rectangle(
                    column * _buttonSize,
                    row * _buttonSize,
                    _buttonSize,
                    _buttonSize);

                var newLevel = new LevelData
                {
                    Texture = texture,
                    IsGenerated = true,
                    Button = new Button(texture, rectangle)
                };
            
                _processorService.ChangeLevel(newLevel);
                _processorService.Generate();
            
                _levels.Add(newLevel);
            }
        }
    }

    public void Update(GameTime gameTime)
    {
        var mouse = Mouse.GetState();

        _levels.ForEach(l => l.Button.Update(mouse));

        if (_mouseService.IsLeftMouseButtonClicked(mouse))
        {
            foreach (var level in _levels.Where(l => l.Button.IsHovered))
            {
                _processorService.ChangeLevel(level);
                _sceneService.SetScene(new GameScene(level, _processorService, this));
                break;
            }
        }

        _mouseService.SetMouse(mouse);
    }

    public void Draw(GameTime gameTime)
    {
        _graphicsDevice.Clear(new Color(25, 25, 25));
        
        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp
        );

        _levels.ForEach(l => l.Button.Draw(_spriteBatch));

        _spriteBatch.End();
    }
    
    /*private Texture2D CloneTexture(Texture2D source)
    {
        var clone = new Texture2D(
            _graphicsDevice,
            source.Width,
            source.Height
        );
        
        var pixels = new Color[source.Width * source.Height];

        source.GetData(pixels);
        clone.SetData(pixels);

        return clone;
    }*/
}