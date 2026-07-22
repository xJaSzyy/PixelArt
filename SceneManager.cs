using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PixelArt.Interfaces;

namespace PixelArt;

public class SceneManager
{
    public IScene CurrentScene { get; private set; }

    private readonly GraphicsDevice graphics;
    private readonly ContentManager content;
    
    public SceneManager(GraphicsDevice graphics, ContentManager content)
    {
        this.graphics = graphics;
        this.content = content;
    }
    
    public void SetScene(IScene scene)
    {
        CurrentScene = scene;
        CurrentScene.Initialize(this);
        CurrentScene.LoadContent(graphics, content);
    }
}