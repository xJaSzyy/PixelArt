using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace PixelArt.Services;

public class ParticleService
{
    private readonly List<Particle> _particles = new();
    private readonly Texture2D _pixel;
    private readonly CameraService _cameraService;

    public ParticleService(GraphicsDevice graphicsDevice, CameraService cameraService)
    {
        _cameraService = cameraService;
        
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
    }

    public void Spawn(Vector2 position, Color color, int count = 1)
    {
        for (var i = 0; i < count; i++)
        {
            _particles.Add(new Particle(position, color));
        }
    }

    public void Update(GameTime gameTime)
    {
        for (var i = _particles.Count - 1; i >= 0; i--)
        {
            _particles[i].Update(gameTime);

            if (_particles[i].IsDead)
            {
                _particles.RemoveAt(i);
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var particle in _particles)
        {
            particle.Draw(spriteBatch, _pixel, _cameraService.GetPosition(), _cameraService.Zoom);
        }
    }
}