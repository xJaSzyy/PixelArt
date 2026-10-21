using System;
using System.Linq;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace PixelArt.Services.Common;

public class SoundService
{
    private readonly SoundEffectInstance[] _paintingInstances;
    private readonly SoundEffectInstance[] _buttonHoveredInstances;
    
    private readonly SoundEffect _dialogOpenSound;
    private readonly SoundEffect _dialogCloseSound;
    private readonly SoundEffect _removeMoneySound;
    private readonly SoundEffect _levelSelectSound;
    private readonly SoundEffect _noMoneySound;
    private readonly SoundEffect _finishSound;
    private readonly SoundEffect _addMoneySound;

    private double _lastPaintingSoundTime;
    private double _lastButtonHoveredSoundTime;
    private const double _soundInterval = 45; 
    
    private int _currentPaintingInstance;
    private int _currentButtonHoveredInstance;

    private float _soundVolume;

    public SoundService(ContentManager content)
    {
        _dialogOpenSound = content.Load<SoundEffect>("Sounds/UI/dialog_open");
        _dialogCloseSound = content.Load<SoundEffect>("Sounds/UI/dialog_close");
        _removeMoneySound = content.Load<SoundEffect>("Sounds/UI/remove_money");
        _levelSelectSound = content.Load<SoundEffect>("Sounds/UI/level_select");
        _noMoneySound = content.Load<SoundEffect>("Sounds/UI/no_money");
        _finishSound = content.Load<SoundEffect>("Sounds/UI/finish");
        _addMoneySound = content.Load<SoundEffect>("Sounds/UI/add_money");

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
    
    public void PlayAddMoneySound()
    {
        var instance = _addMoneySound.CreateInstance();

        instance.Pitch = Random.Shared.NextSingle() * 0.16f - 0.08f;
        
        var randomVolume = 0.9f + Random.Shared.NextSingle() * 0.1f;
        instance.Volume = randomVolume * _soundVolume;

        instance.Play();
    }
    
    public void PlayFinishSound()
    {
        var instance = _finishSound.CreateInstance();

        instance.Pitch = Random.Shared.NextSingle() * 0.16f - 0.08f;
        
        var randomVolume = 0.9f + Random.Shared.NextSingle() * 0.1f;
        instance.Volume = randomVolume * _soundVolume;

        instance.Play();
    }
    
    public void PlayNoMoneySound()
    {
        var instance = _noMoneySound.CreateInstance();

        instance.Pitch = Random.Shared.NextSingle() * 0.16f - 0.08f;
        
        var randomVolume = 0.9f + Random.Shared.NextSingle() * 0.1f;
        instance.Volume = randomVolume * _soundVolume;

        instance.Play();
    }

    public void PlayLevelSelectSound()
    {
        var instance = _levelSelectSound.CreateInstance();

        instance.Pitch = Random.Shared.NextSingle() * 0.16f - 0.08f;
        
        var randomVolume = 0.9f + Random.Shared.NextSingle() * 0.1f;
        instance.Volume = randomVolume * _soundVolume;

        instance.Play();
    }
    
    public void PlayRemoveMoneySound()
    {
        var instance = _removeMoneySound.CreateInstance();

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
    
    public void PlayDialogOpenSound()
    {
        var instance = _dialogOpenSound.CreateInstance();

        instance.Pitch = Random.Shared.NextSingle() * 0.16f - 0.08f;
        
        var randomVolume = 0.9f + Random.Shared.NextSingle() * 0.1f;
        instance.Volume = randomVolume * _soundVolume;

        instance.Play();
    }
    
    public void PlayDialogCloseSound()
    {
        var instance = _dialogCloseSound.CreateInstance();

        instance.Pitch = Random.Shared.NextSingle() * 0.16f - 0.08f;
        
        var randomVolume = 0.9f + Random.Shared.NextSingle() * 0.1f;
        instance.Volume = randomVolume * _soundVolume;

        instance.Play();
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