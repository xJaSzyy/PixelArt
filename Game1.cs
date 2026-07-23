using System;
using Microsoft.Xna.Framework;
using PixelArt.Scenes;
using PixelArt.Services;

namespace PixelArt;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SceneService _sceneService;
    private MouseService _mouseService;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _mouseService = new MouseService();
        _sceneService = new SceneService(GraphicsDevice, Content, _mouseService);
        _sceneService.SetScene(new MenuScene());
        
        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        if (!IsActive)
        {
            base.Update(gameTime);
            return;
        }
        
        _sceneService.CurrentScene.Update(gameTime);
        
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _sceneService.CurrentScene.Draw(gameTime);
        
        base.Draw(gameTime);
    }
}