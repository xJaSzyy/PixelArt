namespace PixelArt.Models;

public class SettingsData
{
    public bool ArrowHintEnabled { get; set; } = true;
    public bool FillAnimationEnabled { get; set; } = true;

    public float MusicVolume { get; set; } = 1f;
    public float SoundVolume { get; set; } = 1f;

    public Language Language { get; set; } = new()
    {
        Name = "English",
        ShortName = "EN"
    };
}