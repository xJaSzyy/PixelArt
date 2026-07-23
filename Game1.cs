using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelArt.Scenes;
using PixelArt.Services;

namespace PixelArt;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SceneService _sceneService;
    private MouseService _mouseService;
    private DrawService _drawService;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        
        _graphics.PreferredBackBufferWidth = 640;
        _graphics.PreferredBackBufferHeight = 640;
        _graphics.ApplyChanges();
    }

    protected override void Initialize()
    {
        _drawService = new DrawService(Content.Load<SpriteFont>("DefaultFont"));
        
        _mouseService = new MouseService();
        _sceneService = new SceneService(GraphicsDevice, Content, _mouseService, _drawService);
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