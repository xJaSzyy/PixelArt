using System;
using System.Linq;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace PixelArt.Services.Common;

public class SoundService
{
    private readonly SettingsService _settingsService;
    private readonly SoundEffectInstance[] _instances;
    private int _currentInstance;
    
    public SoundService(ContentManager content, SettingsService settingsService)
    {
        _settingsService = settingsService;

        var paintingSound = content.Load<SoundEffect>("Sounds/painting");

        _instances = Enumerable.Range(0, 8)
            .Select(_ => paintingSound.CreateInstance())
            .ToArray();
    }

    public void PlayPaintingSound()
    {
        var instance = _instances[_currentInstance];

        instance.Stop();

        instance.Pitch = Random.Shared.NextSingle() * 0.16f - 0.08f;

        var randomVolume = 0.9f + Random.Shared.NextSingle() * 0.1f;
        instance.Volume = randomVolume * _settingsService.GetSettings().SoundVolume;

        instance.Play();

        _currentInstance = (_currentInstance + 1) % _instances.Length;
    }
}