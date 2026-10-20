using System;
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
    
    private CameraService _camera;

    public void Initialize(GraphicsDevice graphicsDevice, ContentManager content, CameraService camera)
    {
        _camera = camera;
        
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _cursorTexture = content.Load<Texture2D>("Cursors/cursor3");
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

        var scale = 2f * _camera.Zoom;
        scale = Math.Max(2.2f, scale);
        
        _spriteBatch.Draw(
            _cursorTexture,
            _cursorPosition,
            null,
            Color.White,
            0f,
            Vector2.Zero,
            scale,
            SpriteEffects.None,
            0f
        );

        _spriteBatch.End();
    }
}