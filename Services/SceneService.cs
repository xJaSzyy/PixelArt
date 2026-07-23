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
    
    public SceneService(GraphicsDevice graphics, ContentManager content, MouseService mouseService, DrawService drawService)
    {
        _graphics = graphics;
        _content = content;
        _mouseService = mouseService;
        _drawService = drawService;
    }
    
    public void SetScene(IScene scene)
    {
        CurrentScene = scene;
        CurrentScene.Initialize(this, _mouseService,  _drawService);
        CurrentScene.LoadContent(_graphics, _content);
    }
}