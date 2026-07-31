using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework.Content;
using PixelArt.Interfaces;

namespace PixelArt.Services;

public class SceneService
{
    public IScene CurrentScene { get; private set; }
    
    private readonly IServiceProvider _services;
    private readonly ContentManager _contentManager;
    
    public SceneService(IServiceProvider services, ContentManager contentManager)
    {
        _services = services;
        _contentManager = contentManager;
    }

    public void SetScene<T>() where T : IScene
    {
        var scene = ActivatorUtilities.CreateInstance<T>(_services);

        CurrentScene = scene;
        CurrentScene.LoadContent(_contentManager);
    }
    
    public void SetScene(IScene scene)
    {
        CurrentScene = scene;
        CurrentScene.LoadContent(_contentManager);
    }
}