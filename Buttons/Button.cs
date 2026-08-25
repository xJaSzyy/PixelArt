using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PixelArt.Buttons;

public class Button(Texture2D texture, Rectangle bounds)
{
    private Texture2D Texture { get; set; } = texture;
    public Rectangle Bounds { get; set; } = bounds;

    public bool IsHovered { get; set; }
    public bool PrevIsHovered { get; set; }
    public bool StartHovered { get; set; }

    public void Update(MouseState mouse)
    {
        IsHovered = Bounds.Contains(mouse.Position);

        if (IsHovered && !PrevIsHovered)
        {
            StartHovered = true;
        }
        else
        {
            StartHovered = false;
        }
        
        PrevIsHovered = IsHovered;
    }

    public void Draw(SpriteBatch spriteBatch, Color? color = null)
    {
        var rect = Bounds;

        color ??= Color.White;

        if (IsHovered)
        {
            rect.Y -= 4;

            if (color == Color.White)
            {
                color = Colors.Yellow;
            }
            else
            {
                color = new Color(
                    Math.Min(color.Value.R + 40, 255),
                    Math.Min(color.Value.G + 40, 255),
                    Math.Min(color.Value.B + 40, 255)
                );
            }
        }

        spriteBatch.Draw(Texture, rect, (Color)color);
    }
}