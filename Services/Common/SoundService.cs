using System;
using System.Linq;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace PixelArt.Services.Common;

public class SoundService
{
    private readonly SoundEffectInstance[] _instances;

    private double _lastPaintingSoundTime;
    private const double _paintingSoundInterval = 45; 
    
    private int _currentInstance;

    private float _soundVolume;

    public SoundService(ContentManager content)
    {
        var paintingSound = content.Load<SoundEffect>("Sounds/painting");

        _instances = Enumerable.Range(0, 8)
            .Select(_ => paintingSound.CreateInstance())
            .ToArray();

        var backgroundMusic = content.Load<Song>("Sounds/background");

        MediaPlayer.IsRepeating = true;
        MediaPlayer.Volume = 0f;
        MediaPlayer.Play(backgroundMusic);
    }
    
    public void PlayPaintingSound()
    {
        var now = Environment.TickCount64;

        if (now - _lastPaintingSoundTime < _paintingSoundInterval)
        {
            return;
        }

        _lastPaintingSoundTime = now;
        
        var instance = _instances[_currentInstance];

        instance.Stop();

        instance.Pitch = Random.Shared.NextSingle() * 0.16f - 0.08f;

        var randomVolume = 0.9f + Random.Shared.NextSingle() * 0.1f;
        instance.Volume = randomVolume * _soundVolume;

        instance.Play();

        _currentInstance = (_currentInstance + 1) % _instances.Length;
    }

    public void UpdateVolume(float musicVolume, float soundVolume)
    {
        MediaPlayer.Volume = Math.Clamp(musicVolume, 0f, 1f);
        _soundVolume = soundVolume;
    }
}