using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using PixelArt.Models;

namespace PixelArt.Services;

public sealed class BackgroundParticleService
{
    private readonly List<BackgroundParticle> _particles = [];
    private readonly Texture2D _pixel;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Random _random = new();

    private static readonly Color[] _palette =
    [
        new(180, 195, 210, 7),
        new(160, 180, 200, 6),
        new(200, 210, 220, 8),
        new(140, 170, 195, 5),
    ];

    public BackgroundParticleService(GraphicsDevice graphicsDevice, int particleCount = 32)
    {
        _graphicsDevice = graphicsDevice;

        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        
        for (var i = 0; i < particleCount; i++)
        {
            var particle = new BackgroundParticle();
            _particles.Add(particle);
            SpawnParticle(particle);
        }
    }

    private void SpawnParticle(BackgroundParticle particle)
    {
        var viewport = _graphicsDevice.Viewport;

        var position = new Vector2(
            RandomFloat(0, viewport.Width),
            RandomFloat(0, viewport.Height)
        );

        var lifetime = RandomFloat(1f, 2f);

        particle.Position = position;
        particle.StartX = position.X;
        particle.Size = _random.Next(1, 4);
        particle.Speed = RandomFloat(4f, 8f);
        particle.WaveSpeed = RandomFloat(0.3f, 0.8f);
        particle.WaveAmplitude = RandomFloat(5f, 9f);
        particle.Phase = RandomFloat(0f, MathF.PI * 2f);
        particle.Time = RandomFloat(0f, 20f);
        particle.Life = lifetime;
        particle.MaxLife = lifetime;
        particle.Color = _palette[_random.Next(_palette.Length)];
    }

    public void Update(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        foreach (var particle in _particles)
        {
            particle.Time += deltaTime;
            particle.Life -= deltaTime;

            particle.Position.Y -= particle.Speed * deltaTime;

            particle.Position.X =
                particle.StartX +
                MathF.Sin(particle.Time * particle.WaveSpeed + particle.Phase) *
                particle.WaveAmplitude;

            if (particle.Life <= 0 ||
                particle.Position.Y < -10)
            {
                SpawnParticle(particle);
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var particle in _particles)
        {
            var lifeProgress = particle.Life / particle.MaxLife;
            var fade = CalculateFade(lifeProgress);

            const float backgroundOpacity = 0.7f;

            var color = particle.Color * fade * backgroundOpacity;

            spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    (int)particle.Position.X,
                    (int)particle.Position.Y,
                    particle.Size,
                    particle.Size
                ),
                color
            );
        }
    }

    private static float CalculateFade(float lifeProgress)
    {
        return MathF.Sin(lifeProgress * MathF.PI);
    }

    private float RandomFloat(float min, float max)
    {
        return min + (float)_random.NextDouble() * (max - min);
    }
}