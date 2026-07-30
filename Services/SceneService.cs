using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PixelArt.Interfaces;

namespace PixelArt.Services;

public class SceneService
{
    public IScene CurrentScene { get; private set; }
    
    private readonly GraphicsDevice _graphics;
    private readonly ContentManager _content;
    
    private readonly MouseService _mouseService;
    private readonly DrawService _drawService;
    private readonly PlayerService _playerService;
    
    public SceneService(GraphicsDevice graphics, ContentManager content, MouseService mouseService,
        DrawService drawService, PlayerService playerService)
    {
        _graphics = graphics;
        _content = content;
        _mouseService = mouseService;
        _drawService = drawService;
        _playerService = playerService;
    }
    
    public void SetScene(IScene scene)
    {
        CurrentScene = scene;
        CurrentScene.Initialize(this, _mouseService,  _drawService, _playerService);
        CurrentScene.LoadContent(_graphics, _content);
    }
}