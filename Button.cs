using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PixelArt;

public class Button(Texture2D texture, Rectangle bounds)
{
    public Texture2D Texture { get; set; } = texture;
    public Rectangle Bounds { get; set; } = bounds;

    public bool IsHovered { get; set; }

    public void Update(MouseState mouse)
    {
        IsHovered = Bounds.Contains(mouse.Position);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        var rect = Bounds;

        var color = Color.White;

        if (IsHovered)
        {
            rect.Y -= 4;
            color = new Color(255, 255, 255, 20);
        }

        spriteBatch.Draw(Texture, rect, color);
    }
}