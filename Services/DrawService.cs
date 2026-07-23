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

    public void DrawString(SpriteBatch spriteBatch, string test)
    {
        spriteBatch.DrawString(
            _font,
            "12345",
            new Vector2(100, 100),
            Color.White
        );
    }
}