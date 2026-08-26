using System;
using System.Linq;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace PixelArt.Services;

public class SoundService
{
    private readonly SoundEffectInstance[] _instances;
    private int _currentInstance;

    public SoundService(ContentManager content)
    {
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
        instance.Volume = 0.9f + Random.Shared.NextSingle() * 0.1f;

        instance.Play();

        _currentInstance = (_currentInstance + 1) % _instances.Length;
    }
}