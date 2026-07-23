using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelArt.Interfaces;
using PixelArt.Services;

namespace PixelArt.Scenes;

public class GameScene : IScene
{
    private GraphicsDevice _graphicsDevice;
    private SceneService _sceneService;
    private MouseService _mouseService;
    private SpriteBatch _spriteBatch;

    private readonly Texture2D _imageTexture;
    private readonly Rectangle _imageBounds;

    public GameScene(Texture2D imageTexture, Rectangle imageBounds)
    {
        _imageTexture = imageTexture;
        _imageBounds = imageBounds;
    }

    public void Initialize(SceneService sceneService, MouseService mouseService)
    {
        _sceneService = sceneService;
        _mouseService = mouseService;
    }

    public void LoadContent(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _graphicsDevice = graphicsDevice;
        
        _spriteBatch = new SpriteBatch(graphicsDevice);
    }

    public void Update(GameTime gameTime)
    {
        var mouse = Mouse.GetState();

        if (_mouseService.IsRightMouseButtonClicked(mouse))
        {
            _sceneService.SetScene(new MenuScene());
        }
        
        _mouseService.SetMouse(mouse);
    }

    public void Draw(GameTime gameTime)
    {
        _graphicsDevice.Clear(new Color(45, 45, 45));
        
        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp
        );

        _spriteBatch.Draw(
            _imageTexture,
            _imageBounds,
            Color.White
        );

        _spriteBatch.End();
    }
}