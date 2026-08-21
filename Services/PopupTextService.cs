using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelArt.Services;

public class PopupTextService
{
    private string _text;
    private Vector2 _position;
    private Color _color;
    private float _scale;

    private float _timer;
    private float _duration;

    private float _delayTimer;

    public bool IsVisible => _text != null && _delayTimer <= 0f;

    public void Show(
        string text,
        Vector2 position,
        float duration = 0.5f,
        Color? color = null,
        float scale = 1f)
    {
        if (IsVisible)
        {
            return;
        }
        
        _text = text;
        _position = position;
        _color = color ?? Color.White;
        _duration = duration;
        _timer = duration;
        _scale = scale;
        _delayTimer = 0f;
    }

    public void ShowDelayed(
        string text,
        Vector2 position,
        float delay,
        float duration = 0.5f,
        Color? color = null,
        float scale = 1f)
    {
        _text = text;
        _position = position;
        _color = color ?? Color.White;
        _duration = duration;
        _timer = duration;
        _scale = scale;
        _delayTimer = delay;
    }

    public void Update(GameTime gameTime)
    {
        if (_text == null)
        {
            return;
        }

        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_delayTimer > 0f)
        {
            _delayTimer -= deltaTime;

            if (_delayTimer > 0f)
            {
                return;
            }

            _timer = _duration;
        }

        _timer -= deltaTime;

        if (_timer <= 0f)
        {
            _text = null;
            return;
        }

        _position.Y -= 20f * deltaTime;
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font)
    {
        if (!IsVisible)
        {
            return;
        }

        var progress = _timer / _duration;
        var alpha = MathHelper.Clamp(progress / 0.3f, 0f, 1f);

        var color = _color * alpha;

        var size = font.MeasureString(_text!) * _scale;
        var position = _position - size / 2f;

        spriteBatch.DrawString(
            font,
            _text!,
            position,
            color,
            0f,
            Vector2.Zero,
            _scale,
            SpriteEffects.None,
            0f);
    }
}