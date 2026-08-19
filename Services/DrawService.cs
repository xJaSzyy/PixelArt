using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelArt.Services;

public class DrawService
{
    private readonly Texture2D _pixelTexture;
    private readonly SpriteFont _font;
    
    public DrawService(GraphicsDevice graphicsDevice, SpriteFont font)
    {
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData([Color.White]);

        _font = font;
    }
    
    public Vector2 MeasureString(string text) => _font.MeasureString(text);
    
    public void DrawString(SpriteBatch spriteBatch,
        string text,
        Vector2 position,
        Color color,
        float scale = 1f)
    {
        var size = MeasureString(text);

        spriteBatch.DrawString(
            _font,
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
    
    public void DrawStringWithBackground(
        SpriteBatch spriteBatch,
        string text,
        Vector2 position,
        Color textColor,
        Color backgroundColor,
        float scale = 1f,
        int padding = 8)
    {
        var size = MeasureString(text) * scale;

        var backgroundRectangle = new Rectangle(
            (int)(position.X - size.X / 2f - padding),
            (int)(position.Y - size.Y / 2f - padding),
            (int)(size.X + padding * 2),
            (int)(size.Y + padding * 2)
        );

        spriteBatch.Draw(
            _pixelTexture,
            backgroundRectangle,
            backgroundColor
        );

        spriteBatch.DrawString(
            _font,
            text,
            position,
            textColor,
            0f,
            MeasureString(text) / 2f,
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