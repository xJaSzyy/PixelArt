using Microsoft.Xna.Framework;

namespace PixelArt.Models;

public sealed class BackgroundParticle
{
    public Vector2 Position;
    public Vector2 Velocity { get; set; }
    public float StartX;
    public int Size;
    public float Speed;
    public float WaveSpeed;
    public float WaveAmplitude;
    public float Phase;
    public float Time;
    public float Life;
    public float MaxLife;
    public Color Color;
}