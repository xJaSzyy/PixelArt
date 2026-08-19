using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelArt.Services;

public class PopupTextService
{
    private string? _text;
    private Vector2 _position;
    private Color _color;

    private float _timer;
    private float _duration;

    public bool IsVisible => _text != null;

    public void Show(
        string text,
        Vector2 position,
        float duration = 0.5f,
        Color? color = null)
    {
        _text = text;
        _position = position;
        _color = color ?? Color.White;

        _duration = duration;
        _timer = duration;
    }

    public void Update(GameTime gameTime)
    {
        if (!IsVisible)
            return;

        _timer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_timer <= 0)
        {
            _text = null;
            return;
        }

        _position.Y -= 20f * (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font)
    {
        if (!IsVisible)
            return;

        var progress = _timer / _duration;

        var alpha = MathHelper.Clamp(progress / 0.3f, 0f, 1f);

        var color = _color * alpha;

        var size = font.MeasureString(_text!);
        var position = _position - size / 2f;

        spriteBatch.DrawString(
            font,
            _text!,
            position,
            color);
    }
}