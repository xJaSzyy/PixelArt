using System;
using Microsoft.Extensions.DependencyInjection;
using PixelArt.Models;
using PixelArt.Scenes;

namespace PixelArt;

public class SceneFactory
{
    private readonly IServiceProvider _services;

    public SceneFactory(IServiceProvider services)
    {
        _services = services;
    }

    public GameScene CreateGameScene(LevelData level)
    {
        return ActivatorUtilities.CreateInstance<GameScene>(
            _services,
            level
        );
    }
}