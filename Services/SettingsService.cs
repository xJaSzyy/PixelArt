using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelArt.Models;
using PixelArt.Services.Common;

namespace PixelArt.Services;

public class SettingsService
{
    public bool IsActive { get; private set; }
    public bool LanguageChanged { get; set; }

    private SettingsData Data { get; } = new();

    private readonly GraphicsDevice _graphicsDevice;
    private readonly DrawService _drawService;
    private readonly InputService _inputService;
    private readonly LanguageService _languageService;

    private bool _draggingMusic;
    private bool _draggingSound;

    public SettingsService(GraphicsDevice graphicsDevice, DrawService drawService, InputService inputService,
        LanguageService languageService)
    {
        _graphicsDevice = graphicsDevice;
        _drawService = drawService;
        _inputService = inputService;
        _languageService = languageService;
    }

    public void Toggle()
    {
        IsActive = !IsActive;
    }

    public void Update(MouseState mouse, Action action)
    {
        if (!IsActive)
        {
            return;
        }

        HandleCheckboxes(mouse, action);
        HandleSliders(mouse);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActive)
        {
            return;
        }

        var bounds = GetBounds();

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

        var arrowBounds = GetArrowBounds(bounds, right);
        
        _drawService.DrawStringLeft(spriteBatch, "ARROW HINT", new Vector2(left, bounds.Y + 76 + arrowBounds.Height / 4f), Color.White);
        _drawService.DrawCheckbox(spriteBatch, arrowBounds, Data.ArrowHintEnabled, Color.White, Color.Black, Color.White);

        var animationBounds = GetAnimationBounds(bounds, right);
        _drawService.DrawStringLeft(spriteBatch, "ANIMATION", new Vector2(left, bounds.Y + 120 + animationBounds.Height / 4f), Color.White);
        _drawService.DrawCheckbox(spriteBatch, animationBounds, Data.DrawAnimationEnabled, Color.White, Color.Black, Color.White);

        var languageBounds = GetLanguageBounds(bounds, right);
        _drawService.DrawStringLeft(spriteBatch, "LANGUAGE", new Vector2(left, languageBounds.Y + languageBounds.Height / 4f), Color.White);
        _drawService.DrawRoundedRectangle(spriteBatch, languageBounds, Color.White, 0);
        _drawService.DrawRoundedRectangle(spriteBatch, new Rectangle(languageBounds.X + 2, languageBounds.Y + 2, languageBounds.Width - 4, languageBounds.Height - 4), Color.Black, 0);
        _drawService.DrawString(spriteBatch, Data.Language.ShortName, languageBounds.Center.ToVector2(), Color.White);

        var musicSliderInputBounds = GetMusicSliderInputBounds(bounds, left);
        _drawService.DrawString(spriteBatch, "MUSIC", new Vector2(bounds.Center.X, bounds.Y + 208), Color.White);
        _drawService.DrawSlider(spriteBatch, musicSliderInputBounds, Data.MusicVolume, Color.White, Color.DarkGray, Color.White, Color.White);

        var soundSliderInputBounds = GetSoundSliderInputBounds(bounds, left);
        _drawService.DrawString(spriteBatch, "SOUND", new Vector2(bounds.Center.X, bounds.Y + 272), Color.White);
        _drawService.DrawSlider(spriteBatch, soundSliderInputBounds, Data.SoundVolume, Color.White, Color.DarkGray, Color.White, Color.White);
    }

    private void HandleCheckboxes(MouseState mouse, Action action)
    {
        if (!_inputService.IsLeftMouseButtonClicked(mouse))
        {
            return;
        }

        var bounds = GetBounds();

        var right = bounds.Right - 24;
        
        var arrowHintBounds = GetArrowBounds(bounds, right);

        if (arrowHintBounds.Contains(mouse.Position))
        {
            Data.ArrowHintEnabled = !Data.ArrowHintEnabled;
            return;
        }

        var animationBounds = GetAnimationBounds(bounds, right);

        if (animationBounds.Contains(mouse.Position))
        {
            Data.DrawAnimationEnabled = !Data.DrawAnimationEnabled;
            return;
        }

        var languageBounds = GetLanguageBounds(bounds, right);

        if (languageBounds.Contains(mouse.Position))
        {
            ChangeLanguage(action);
            return;
        }
    }

    private void HandleSliders(MouseState mouse)
    {
        var bounds = GetBounds();

        var left = bounds.X + 24;
        
        var musicBounds = GetMusicSliderInputBounds(bounds, left);
        var soundBounds = GetSoundSliderInputBounds(bounds, left);

        if (_inputService.IsLeftMouseButtonClicked(mouse))
        {
            if (musicBounds.Contains(mouse.Position))
            {
                _draggingMusic = true;
            }
            else if (soundBounds.Contains(mouse.Position))
            {
                _draggingSound = true;
            }
        }

        if (_inputService.IsLeftMouseButtonPressed(mouse))
        {
            if (_draggingMusic)
            {
                Data.MusicVolume = CalculateSliderValue(
                    mouse.Position.X,
                    musicBounds);
            }

            if (_draggingSound)
            {
                Data.SoundVolume = CalculateSliderValue(
                    mouse.Position.X,
                    soundBounds);
            }
        }
        else
        {
            _draggingMusic = false;
            _draggingSound = false;
        }
    }

    private static float CalculateSliderValue(int mouseX, Rectangle bounds)
    {
        var value = (mouseX - bounds.X) / (float)bounds.Width;
        return MathHelper.Clamp(value, 0f, 1f);
    }

    private static Rectangle GetMusicSliderInputBounds(Rectangle bounds, int left)
    {
        return new Rectangle(left, bounds.Y + 228, bounds.Width - 48, 12);
    }

    private static Rectangle GetSoundSliderInputBounds(Rectangle bounds, int left)
    {
        return new Rectangle(left, bounds.Y + 292, bounds.Width - 48, 12);
    }

    private Rectangle GetBounds()
    {
        var size = new Point(256, 320);

        var position = new Point(
            (int)(_graphicsDevice.Viewport.Width / 2f - size.X / 2f),
            (int)(_graphicsDevice.Viewport.Height / 2f - size.Y / 2f));

        return new Rectangle(position, size);
    }

    private static Rectangle GetArrowBounds(Rectangle bounds, int right)
    {
        const int width = 24;
        const int height = 24;
        
        return new Rectangle(right - 18, bounds.Y + 76, width, height);
    }

    private static Rectangle GetAnimationBounds(Rectangle bounds, int right)
    {
        const int width = 24;
        const int height = 24;
        
        return new Rectangle(right - 18, bounds.Y + 120, width, height);
    }
    
    private static Rectangle GetLanguageBounds(Rectangle bounds, int right)
    {
        const int width = 48;
        const int height = 36;
        
        return new Rectangle(right - 18 - width / 2, bounds.Y + 164, width, height);
    }

    private void ChangeLanguage(Action action)
    {
        _languageService.ChangeLanguage();
        Data.Language = _languageService.CurrentLanguage;
        action.Invoke();
    }
}