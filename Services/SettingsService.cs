using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelArt.Models;
using PixelArt.Services.Common;

namespace PixelArt.Services;

public class SettingsService
{
    public bool IsActive { get; private set; }

    public SettingsData Data { get; } = new();

    private readonly GraphicsDevice _graphicsDevice;
    private readonly DrawService _drawService;

    public SettingsService(
        GraphicsDevice graphicsDevice,
        DrawService drawService)
    {
        _graphicsDevice = graphicsDevice;
        _drawService = drawService;
    }

    public void Toggle()
    {
        IsActive = !IsActive;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActive)
        {
            return;
        }
        
        var size = new Point(256, 400);
        
        var position = new Point(
            (int)(_graphicsDevice.Viewport.Width / 2f - size.X / 2f),
            (int)(_graphicsDevice.Viewport.Height / 2f - size.Y / 2f));

        var bounds = new Rectangle(position, size);

        DrawPanel(spriteBatch, bounds);
        DrawSettings(spriteBatch, bounds);
    }

    private void DrawPanel(SpriteBatch spriteBatch, Rectangle bounds)
    {
        _drawService.DrawRoundedRectangle(
            spriteBatch,
            new Rectangle(
                bounds.Location.X + 6,
                bounds.Location.Y + 6,
                bounds.Size.X,
                bounds.Size.Y),
            new Color(Colors.Black, 100),
            6);

        _drawService.DrawRoundedRectangle(
            spriteBatch,
            new Rectangle(
                bounds.Location.X,
                bounds.Location.Y,
                bounds.Size.X,
                bounds.Size.Y),
            Colors.PanelOuter,
            6);

        _drawService.DrawRoundedRectangle(
            spriteBatch,
            new Rectangle(
                bounds.Location.X + 6,
                bounds.Location.Y + 6,
                bounds.Size.X - 12,
                bounds.Size.Y - 12),
            Colors.PanelInner,
            6);
    }

    private void DrawSettings(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var left = bounds.X + 24;
        var right = bounds.Right - 24;

        _drawService.DrawStringLeft(
            spriteBatch,
            "ARROW HINT",
            new Vector2(left, bounds.Y + 76),
            Color.White);

        _drawService.DrawCheckbox(
            spriteBatch,
            new Rectangle(
                right - 18,
                bounds.Y + 70,
                18,
                18),
            Data.ArrowHintEnabled,
            Color.White,
            Color.Black,
            Color.White);

        _drawService.DrawStringLeft(
            spriteBatch,
            "ANIMATION",
            new Vector2(left, bounds.Y + 120),
            Color.White);

        _drawService.DrawCheckbox(
            spriteBatch,
            new Rectangle(
                right - 18,
                bounds.Y + 110,
                18,
                18),
            Data.DrawAnimationEnabled,
            Color.White,
            Color.Black,
            Color.White);

        _drawService.DrawString(
            spriteBatch,
            "MUSIC",
            new Vector2(
                bounds.Center.X,
                bounds.Y + 170),
            Color.White);

        _drawService.DrawSlider(
            spriteBatch,
            new Rectangle(
                left,
                bounds.Y + 190,
                bounds.Width - 48,
                12),
            Data.MusicVolume,
            Color.White,
            Color.DarkGray,
            Color.White,
            Color.White);

        _drawService.DrawString(
            spriteBatch,
            "SOUND",
            new Vector2(
                bounds.Center.X,
                bounds.Y + 230),
            Color.White);

        _drawService.DrawSlider(
            spriteBatch,
            new Rectangle(
                left,
                bounds.Y + 250,
                bounds.Width - 48,
                12),
            Data.SoundVolume,
            Color.White,
            Color.DarkGray,
            Color.White,
            Color.White);
    }
}