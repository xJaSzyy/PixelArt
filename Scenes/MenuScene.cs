using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelArt.Buttons;
using PixelArt.Interfaces;
using PixelArt.Services;

namespace PixelArt.Scenes;

public class MenuScene : IScene
{
    private GraphicsDevice _graphicsDevice;
    private SpriteBatch _spriteBatch;
    
    private SceneService _sceneService;
    private MouseService _mouseService;
    
    private readonly List<Button> _buttons = [];
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

        const int buttonsCount = 3;

        for (var i = 0; i < buttonsCount; i++)
        {
            var texture = content.Load<Texture2D>($"Images/img{i + 1}");
            var rectangle = new Rectangle(0 + i * _buttonSize, 0, _buttonSize, _buttonSize);
            
            _buttons.Add(new Button(texture, rectangle));
        }
    }

    public void Update(GameTime gameTime)
    {
        var mouse = Mouse.GetState();

        _buttons.ForEach(x => x.Update(mouse));

        if (_mouseService.IsLeftMouseButtonClicked(mouse))
        {
            foreach (var button in _buttons.Where(button => button.IsHovered))
            {
                _sceneService.SetScene(new GameScene(CloneTexture(button.Texture), button.Bounds));
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

        _buttons.ForEach(x => x.Draw(_spriteBatch));

        _spriteBatch.End();
    }
    
    private Texture2D CloneTexture(Texture2D source)
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
    }
}