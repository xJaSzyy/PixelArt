using Microsoft.Xna.Framework;

namespace PixelArt.Models;

public class SettingsData
{
    public bool ArrowHintEnabled { get; set; } = true;
    public bool DrawAnimationEnabled { get; set; } = true;

    public float MusicVolume { get; set; } = 1f;
    public float SoundVolume { get; set; } = 1f;
    
    public void SetMusicVolume(float value)
    {
        MusicVolume = MathHelper.Clamp(value, 0f, 1f);
    }

    public void SetSoundVolume(float value)
    {
        SoundVolume = MathHelper.Clamp(value, 0f, 1f);
    }
}