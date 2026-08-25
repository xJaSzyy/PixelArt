using System;
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
        Rectangle bounds,
        float progress,
        Color borderColor,
        Color emptyColor,
        Color fillColor)
    {
        spriteBatch.Draw(
            _pixelTexture,
            bounds,
            borderColor);

        var innerBounds = new Rectangle(
            bounds.X + 1,
            bounds.Y + 1,
            bounds.Width - 2,
            bounds.Height - 2);

        spriteBatch.Draw(
            _pixelTexture,
            innerBounds,
            emptyColor);

        var fillBounds = new Rectangle(
            innerBounds.X,
            innerBounds.Y,
            (int)(innerBounds.Width * progress),
            innerBounds.Height);

        spriteBatch.Draw(
            _pixelTexture,
            fillBounds,
            fillColor);
    }

    public void DrawRectangle(SpriteBatch spriteBatch, Rectangle bounds, Color color)
    {
        spriteBatch.Draw(_pixelTexture, bounds, color);
    }

    public void DrawRoundedRectangle(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        Color color,
        int radius)
    {
        radius = Math.Min(
            radius,
            Math.Min(bounds.Width, bounds.Height) / 2);

        spriteBatch.Draw(
            _pixelTexture,
            new Rectangle(
                bounds.X + radius,
                bounds.Y + radius,
                bounds.Width - radius * 2,
                bounds.Height - radius * 2),
            color);

        spriteBatch.Draw(
            _pixelTexture,
            new Rectangle(
                bounds.X + radius,
                bounds.Y,
                bounds.Width - radius * 2,
                radius),
            color);

        spriteBatch.Draw(
            _pixelTexture,
            new Rectangle(
                bounds.X + radius,
                bounds.Bottom - radius,
                bounds.Width - radius * 2,
                radius),
            color);

        spriteBatch.Draw(
            _pixelTexture,
            new Rectangle(
                bounds.X,
                bounds.Y + radius,
                radius,
                bounds.Height - radius * 2),
            color);

        spriteBatch.Draw(
            _pixelTexture,
            new Rectangle(
                bounds.Right - radius,
                bounds.Y + radius,
                radius,
                bounds.Height - radius * 2),
            color);

        DrawCircle(spriteBatch, new Vector2(bounds.X + radius, bounds.Y + radius), radius, color);
        DrawCircle(spriteBatch, new Vector2(bounds.Right - radius - 1, bounds.Y + radius), radius, color);
        DrawCircle(spriteBatch, new Vector2(bounds.X + radius, bounds.Bottom - radius - 1), radius, color);
        DrawCircle(spriteBatch, new Vector2(bounds.Right - radius - 1, bounds.Bottom - radius - 1), radius, color);
    }

    private void DrawCircle(
        SpriteBatch spriteBatch,
        Vector2 center,
        int radius,
        Color color)
    {
        for (var y = -radius; y <= radius; y++)
        {
            var width = (int)Math.Sqrt(radius * radius - y * y);

            spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(
                    (int)center.X - width,
                    (int)center.Y + y,
                    width * 2 + 1,
                    1),
                color);
        }
    }

    public SpriteFont GetFont() => _font;
}