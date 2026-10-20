using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PixelArt.Services;

public class CursorService
{
    private SpriteBatch _spriteBatch;
    private Texture2D _cursorTexture;
    private Vector2 _cursorPosition;

    public void Initialize(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _cursorTexture = content.Load<Texture2D>("Cursors/cursor1");
    }

    public void Update()
    {
        var mouseState = Mouse.GetState();
        _cursorPosition = new Vector2(mouseState.X, mouseState.Y);
    }

    public void Draw()
    {
        var mouseState = Mouse.GetState();

        _cursorPosition = new Vector2(
            mouseState.X - _cursorTexture.Width,
            mouseState.Y - _cursorTexture.Height
        );

        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            blendState: BlendState.AlphaBlend
        );

        _spriteBatch.Draw(
            _cursorTexture,
            _cursorPosition,
            null,
            Color.White,
            0f,
            Vector2.Zero,
            3f,
            SpriteEffects.None,
            0f
        );

        _spriteBatch.End();
    }
}