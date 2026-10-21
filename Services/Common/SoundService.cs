using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;
using PixelArt.Enums;

namespace PixelArt.Services.Common;

public class SoundService
{
    private readonly SoundEffectInstance[] _paintingInstances;
    private readonly SoundEffectInstance[] _buttonHoveredInstances;

    private readonly Dictionary<SoundType, SoundEffect> _soundByType = [];

    private double _lastPaintingSoundTime;
    private double _lastButtonHoveredSoundTime;
    private const double _soundInterval = 45; 
    
    private int _currentPaintingInstance;
    private int _currentButtonHoveredInstance;

    private float _soundVolume;

    public SoundService(ContentManager content)
    {
        _soundByType.Add(SoundType.AddMoney, content.Load<SoundEffect>("Sounds/UI/add_money"));
        _soundByType.Add(SoundType.Finish, content.Load<SoundEffect>("Sounds/UI/finish"));
        _soundByType.Add(SoundType.NoMoney, content.Load<SoundEffect>("Sounds/UI/no_money"));
        _soundByType.Add(SoundType.ButtonClick, content.Load<SoundEffect>("Sounds/UI/level_select"));
        _soundByType.Add(SoundType.RemoveMoney, content.Load<SoundEffect>("Sounds/UI/remove_money"));
        _soundByType.Add(SoundType.DialogOpen, content.Load<SoundEffect>("Sounds/UI/dialog_open"));
        _soundByType.Add(SoundType.DialogClose, content.Load<SoundEffect>("Sounds/UI/dialog_close"));
        
        _buttonHoveredInstances = Enumerable.Range(0, 4)
            .Select(_ => content.Load<SoundEffect>("Sounds/UI/hover").CreateInstance())
            .ToArray();
        
        _paintingInstances = Enumerable.Range(0, 8)
            .Select(_ => content.Load<SoundEffect>("Sounds/painting").CreateInstance())
            .ToArray();

        var backgroundMusic = content.Load<Song>("Sounds/background");

        MediaPlayer.IsRepeating = true;
        MediaPlayer.Volume = 0f;
        MediaPlayer.Play(backgroundMusic);
    }
    
    public void PlaySoundByType(SoundType soundType)
    {
        var instance = _soundByType[soundType].CreateInstance();
        
        instance.Pitch = Random.Shared.NextSingle() * 0.16f - 0.08f;
        
        var randomVolume = 0.9f + Random.Shared.NextSingle() * 0.1f;
        instance.Volume = randomVolume * _soundVolume;

        instance.Play();
    }
    
    public void PlayButtonHoveredSound()
    {
        var now = Environment.TickCount64;

        if (now - _lastButtonHoveredSoundTime < _soundInterval)
        {
            return;
        }

        _lastButtonHoveredSoundTime = now;
        
        var instance = _buttonHoveredInstances[_currentButtonHoveredInstance];

        instance.Stop();

        instance.Pitch = Random.Shared.NextSingle() * 0.16f - 0.08f;

        var randomVolume = 0.9f + Random.Shared.NextSingle() * 0.1f;
        instance.Volume = randomVolume * _soundVolume;

        instance.Play();

        _currentButtonHoveredInstance = (_currentButtonHoveredInstance + 1) % _buttonHoveredInstances.Length;
    }
    
    public void PlayPaintingSound()
    {
        var now = Environment.TickCount64;

        if (now - _lastPaintingSoundTime < _soundInterval)
        {
            return;
        }

        _lastPaintingSoundTime = now;
        
        var instance = _paintingInstances[_currentPaintingInstance];

        instance.Stop();

        instance.Pitch = Random.Shared.NextSingle() * 0.16f - 0.08f;

        var randomVolume = 0.9f + Random.Shared.NextSingle() * 0.1f;
        instance.Volume = randomVolume * _soundVolume;

        instance.Play();

        _currentPaintingInstance = (_currentPaintingInstance + 1) % _paintingInstances.Length;
    }

    public void UpdateVolume(float musicVolume, float soundVolume)
    {
        MediaPlayer.Volume = Math.Clamp(musicVolume, 0f, 1f);
        _soundVolume = soundVolume;
    }
}