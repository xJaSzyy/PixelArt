using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelArt.Services;

public class DrawService
{
    private SpriteFont _font;

    public DrawService(SpriteFont font)
    {
        _font = font;
    }

    public void DrawString(
        SpriteBatch spriteBatch,
        string text,
        Vector2 position,
        Color color)
    {
        var size = _font.MeasureString(text);

        spriteBatch.DrawString(
            _font,
            text,
            position,
            color,
            0f,
            size / 2f,
            1f,
            SpriteEffects.None,
            0f
        );
    }
}