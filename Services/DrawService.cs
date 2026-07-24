using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelArt.Services;

public class DrawService(SpriteFont font)
{
    public void DrawString(SpriteBatch spriteBatch,
        string text,
        Vector2 position,
        Color color,
        float scale = 1f)
    {
        var size = font.MeasureString(text);

        spriteBatch.DrawString(
            font,
            text,
            position,
            color,
            0f,
            size / 2f,
            scale,
            SpriteEffects.None,
            0f
        );
    }
    
    public void DrawProgressBar(
        SpriteBatch spriteBatch,
        Texture2D pixelTexture,
        Rectangle bounds,
        float progress,
        Color borderColor,
        Color emptyColor,
        Color fillColor)
    {
        spriteBatch.Draw(
            pixelTexture,
            bounds,
            borderColor);

        var innerBounds = new Rectangle(
            bounds.X + 1,
            bounds.Y + 1,
            bounds.Width - 2,
            bounds.Height - 2);

        spriteBatch.Draw(
            pixelTexture,
            innerBounds,
            emptyColor);

        var fillBounds = new Rectangle(
            innerBounds.X,
            innerBounds.Y,
            (int)(innerBounds.Width * progress),
            innerBounds.Height);

        spriteBatch.Draw(
            pixelTexture,
            fillBounds,
            fillColor);
    }
}