using System;
using Microsoft.Xna.Framework;
using PixelArt.Scenes;

namespace PixelArt;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager graphics;
    private SceneManager sceneManager;

    public Game1()
    {
        graphics = new GraphicsDeviceManager(this);

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        sceneManager = new SceneManager(GraphicsDevice, Content);
        sceneManager.SetScene(new MenuScene());
        
        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        sceneManager.CurrentScene.Update(gameTime);
        
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        sceneManager.CurrentScene.Draw(gameTime);
        
        base.Draw(gameTime);
    }
}