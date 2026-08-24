using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace PixelArt.Services;

public class SoundService
{
    private readonly SoundEffect _paintingSound;
    
    public SoundService(ContentManager content)
    {
        _paintingSound = content.Load<SoundEffect>("Sounds/painting");
    }

    public void PlayPaintingSound()
    {
        _paintingSound.Play();
    }
}