#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelArt.Services;

namespace PixelArt.Buttons;

public class Button(DrawService drawService, Texture2D? texture, Rectangle bounds)
{
    private Texture2D? Texture { get; set; } = texture;
    public Rectangle Bounds { get; set; } = bounds;
    public bool IsHovered { get; private set; }
    public string? Text { get; set; }
    public float TextScale { get; set; } = 1f;
    public SpriteFont? Font { get; set; }
    public Color TextColor { get; set; } = Color.White;

    public void Update(MouseState mouse)
    {
        IsHovered = Bounds.Contains(mouse.Position);
    }

    public void Draw(SpriteBatch spriteBatch, Color? color = null)
    {
        var buttonColor = color ?? Color.White;

        if (IsHovered)
        {
            buttonColor = buttonColor == Color.White ? Colors.Yellow : Lighten(buttonColor);
        }

        if (Texture != null)
        {
            var rect = Bounds;

            if (IsHovered)
            {
                rect.Y -= 4;
            }

            spriteBatch.Draw(Texture, rect, buttonColor);
        }

        if (Text != null && Font != null)
        {
            var rect = Bounds;
            var textColor = TextColor;

            if (IsHovered)
            {
                rect.Y -= 4;
                textColor = Lighten(textColor);
            }
            
            drawService.DrawString(spriteBatch, Text, rect.Center.ToVector2(), textColor, TextScale);
        }
    }

    private static Color Lighten(Color color)
    {
        return new Color(
            Math.Min(color.R + 40, 255),
            Math.Min(color.G + 40, 255),
            Math.Min(color.B + 40, 255)
        );
    }
}