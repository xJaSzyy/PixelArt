using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PixelArt.Scenes;
using PixelArt.Services;

namespace PixelArt;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SceneService _sceneService;
    
    private ServiceProvider _services;
    
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnClientSizeChanged;
        
        _graphics.PreferredBackBufferWidth = 640;
        _graphics.PreferredBackBufferHeight = 640;
        _graphics.ApplyChanges();
    }

    protected override void Initialize()
    {
        var collection = new ServiceCollection();
        
        collection.AddSingleton(GraphicsDevice);
        collection.AddSingleton(new DrawService(Content.Load<SpriteFont>("DefaultFont")));
        collection.AddSingleton<MouseService>();
        collection.AddSingleton<PlayerService>();
        collection.AddSingleton<PopupService>();
        collection.AddSingleton<SceneFactory>();
        collection.AddSingleton<SceneService>();
        collection.AddSingleton<PixelProcessorService>();
        collection.AddSingleton(Content);
        collection.AddSingleton<LevelService>();
        
        collection.AddTransient<MenuScene>();
        collection.AddTransient<GameScene>();
        
        _services = collection.BuildServiceProvider();
        
        _sceneService = _services.GetRequiredService<SceneService>();
        _sceneService.SetScene<MenuScene>();
        
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
        //_popupService.Update(gameTime);
        
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _sceneService.CurrentScene.Draw(gameTime);
        //_popupService.Draw(gameTime);
        
        base.Draw(gameTime);
    }
    
    private void OnClientSizeChanged(object sender, EventArgs e)
    {
        _sceneService.CurrentScene.OnClientSizeChanged(sender, e);
    }
}