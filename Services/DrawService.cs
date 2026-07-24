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
    
    public void DrawProgressBar(SpriteBatch spriteBatch, Texture2D pixelTexture, Rectangle bounds, float progress, Color emptyColor, Color fillColor)
    {
        spriteBatch.Draw(
            pixelTexture,
            bounds,
            emptyColor);

        var fill = new Rectangle(
            bounds.X,
            bounds.Y,
            (int)(bounds.Width * progress),
            bounds.Height);

        spriteBatch.Draw(
            pixelTexture,
            fill,
            fillColor);
    }
}