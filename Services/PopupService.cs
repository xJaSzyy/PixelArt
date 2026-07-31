using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelArt.Services;

public class PopupService
{
    private bool IsActive { get; set; }
    
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly DrawService _drawService;

    private const float _popupDuration = .5f;
    private float _timer;

    public PopupService(GraphicsDevice graphicsDevice, DrawService drawService)
    {
        _graphicsDevice = graphicsDevice;
        _drawService = drawService;
        
        _spriteBatch = new SpriteBatch(graphicsDevice);
    }

    public void Update(GameTime gameTime)
    {
        if (!IsActive)
        {
            return;
        }

        _timer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_timer <= 0f)
        {
            IsActive = false;
        }
    }

    public void Draw(GameTime gameTime)
    {
        if (!IsActive)
        {
            return;
        }
        
        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp
        );
        
        _drawService.DrawString(
            _spriteBatch,
            "Hello, World!",
                new Vector2(_graphicsDevice.Viewport.Width / 2, _graphicsDevice.Viewport.Height / 2),
                Color.GreenYellow,
                2.5f
            );
        
        _spriteBatch.End();
    }
    
    public void ShowPopup()
    {
        IsActive = true;
        _timer = _popupDuration;
    }
}