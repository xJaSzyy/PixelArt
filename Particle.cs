using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelArt;

public class Particle
{
    private Vector2 _position;
    private readonly Vector2 _velocity;

    private readonly float _lifetime;
    private float _age;

    private readonly float _size;
    private readonly Color _color;

    public bool IsDead => _age >= _lifetime;

    public Particle(Vector2 position, Color color)
    {
        _position = position;

        _velocity = new Vector2(
            Random.Shared.NextSingle() * 60f - 30f,
            Random.Shared.NextSingle() * 60f - 30f
        );

        _lifetime = Random.Shared.NextSingle() * 0.5f + 0.5f;
        _size = Random.Shared.Next(5, 11);
        _color = color;
    }

    public void Update(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _age += deltaTime;
        _position += _velocity * deltaTime;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, Vector2 cameraPosition, float zoom)
    {
        var lifeProgress = _age / _lifetime;
        var alpha = 1f - lifeProgress;

        var screenPosition = cameraPosition + _position * zoom;

        spriteBatch.Draw(
            pixel,
            screenPosition,
            null,
            _color * alpha,
            0f,
            Vector2.Zero,
            _size * zoom,
            SpriteEffects.None,
            0f);
    }
}