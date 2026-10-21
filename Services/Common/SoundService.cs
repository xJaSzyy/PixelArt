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
    private readonly Dictionary<SoundType, SoundEffect> _soundByType = [];

    private double _lastSoundTime;

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
        _soundByType.Add(SoundType.OnHover, content.Load<SoundEffect>("Sounds/UI/hover"));
        _soundByType.Add(SoundType.Paint, content.Load<SoundEffect>("Sounds/painting"));
        
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

    public void PlaySoundWithInterval(SoundType soundType, double soundInterval = 45d)
    {
        var now = Environment.TickCount64;

        if (now - _lastSoundTime < soundInterval)
        {
            return;
        }

        _lastSoundTime = now;
        
        var instance = _soundByType[soundType].CreateInstance();

        instance.Stop();

        instance.Pitch = Random.Shared.NextSingle() * 0.16f - 0.08f;

        var randomVolume = 0.9f + Random.Shared.NextSingle() * 0.1f;
        instance.Volume = randomVolume * _soundVolume;

        instance.Play();
    }

    public void UpdateVolume(float musicVolume, float soundVolume)
    {
        MediaPlayer.Volume = Math.Clamp(musicVolume, 0f, 1f);
        _soundVolume = soundVolume;
    }
}