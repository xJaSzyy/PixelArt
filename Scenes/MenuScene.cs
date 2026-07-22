using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelArt.Interfaces;

namespace PixelArt.Scenes;

public class MenuScene : IScene
{
    private GraphicsDevice _graphicsDevice;
    private SceneManager _sceneManager;
    private SpriteBatch _spriteBatch;
    
    private List<Button> _buttons = [];
    private int _buttonSize = 128;
    
    private MouseState _prevMouse;
    
    public void Initialize(SceneManager sceneManager)
    {
        _sceneManager = sceneManager;
    }

    public void LoadContent(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _graphicsDevice = graphicsDevice;
        
        _spriteBatch = new SpriteBatch(graphicsDevice);

        const int buttonsCount = 2;

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

        var clicked = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;

        if (clicked)
        {
            foreach (var button in _buttons)
            {
                if (button.IsHovered)
                {
                    _sceneManager.SetScene(new GameScene(button.Texture, button.Bounds));
                    break;
                }
            }
        }

        _prevMouse = mouse;
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
}