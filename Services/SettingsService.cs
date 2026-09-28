using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelArt.Models;
using PixelArt.Services.Common;

namespace PixelArt.Services;

public class SettingsService
{
    public bool IsActive { get; private set; }

    private SettingsData Settings { get; set; } = new();

    private readonly GraphicsDevice _graphicsDevice;
    private readonly DrawService _drawService;
    private readonly InputService _inputService;
    private readonly LanguageService _languageService;

    private bool _draggingMusic;
    private bool _draggingSound;

    public SettingsService(GraphicsDevice graphicsDevice, DrawService drawService, InputService inputService, LanguageService languageService)
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
        _drawService.DrawRoundedRectangle(spriteBatch,
            new Rectangle(bounds.Location.X + 6, bounds.Location.Y + 6,
                bounds.Size.X, bounds.Size.Y),
            new Color(Colors.Black, 100), 6);

        _drawService.DrawRoundedRectangle(spriteBatch,
            new Rectangle(bounds.Location.X, bounds.Location.Y,
                bounds.Size.X, bounds.Size.Y),
            Colors.PanelOuter, 6);

        _drawService.DrawRoundedRectangle(spriteBatch, 
            new Rectangle(bounds.Location.X + 6, bounds.Location.Y + 6, 
                bounds.Size.X - 12, bounds.Size.Y - 12), 
            Colors.PanelInner, 6);
    }

    private void DrawSettings(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var top = 24;
        var step = 36;
        var left = bounds.X + 24;
        var right = bounds.Right - 24;

        var arrowBounds = GetArrowBounds(bounds, right);
        
        _drawService.DrawStringLeft(spriteBatch, _languageService.GetText("Settings.ArrowHint"), 
            new Vector2(left, bounds.Y + top + arrowBounds.Height / 4f), 
            Colors.Text);
        
        _drawService.DrawCheckbox(spriteBatch, arrowBounds, Settings.ArrowHintEnabled, 
            Colors.Text, Color.Black, Colors.Text);

        var animationBounds = GetAnimationBounds(bounds, right);
        
        _drawService.DrawStringLeft(spriteBatch, _languageService.GetText("Settings.FillAnimation"), 
            new Vector2(left, bounds.Y + top + step + animationBounds.Height / 4f), 
            Colors.Text);
        
        _drawService.DrawCheckbox(spriteBatch, animationBounds, Settings.FillAnimationEnabled, 
            Colors.Text, Color.Black, Colors.Text);

        var languageBounds = GetLanguageBounds(bounds, top, step, right);
        
        _drawService.DrawStringLeft(spriteBatch, _languageService.GetText("Settings.Language"), 
            new Vector2(left, languageBounds.Y + languageBounds.Height / 4f), 
            Colors.Text);
        
        _drawService.DrawRoundedRectangle(spriteBatch, languageBounds, Colors.Text, 0);
        _drawService.DrawRoundedRectangle(spriteBatch, 
            new Rectangle(languageBounds.X + 2, languageBounds.Y + 2,
                languageBounds.Width - 4, languageBounds.Height - 4),
            Color.Black, 0);
        
        _drawService.DrawString(spriteBatch, Settings.Language.ShortName, 
            languageBounds.Center.ToVector2(), Colors.Text);

        var musicSliderInputBounds = GetMusicSliderInputBounds(bounds, top, step, left);
        
        _drawService.DrawString(spriteBatch, _languageService.GetText("Settings.Music"), 
            new Vector2(bounds.Center.X, bounds.Y + top + step * 4), 
            Colors.Text);
        
        _drawService.DrawSlider(spriteBatch, musicSliderInputBounds, Settings.MusicVolume, 
            Colors.Text, Color.DarkGray, Colors.Text, Colors.Text);

        var soundSliderInputBounds = GetSoundSliderInputBounds(bounds, top, step, left);
        
        _drawService.DrawString(spriteBatch, _languageService.GetText("Settings.Sound"), 
            new Vector2(bounds.Center.X, bounds.Y + top + step * 5.5f), 
            Colors.Text);
        
        _drawService.DrawSlider(spriteBatch, soundSliderInputBounds, Settings.SoundVolume, 
            Colors.Text, Color.DarkGray, Colors.Text, Colors.Text);
    }

    private void HandleCheckboxes(MouseState mouse, Action action)
    {
        if (!_inputService.IsLeftMouseButtonClicked(mouse))
        {
            return;
        }

        var bounds = GetBounds();

        var top = 24;
        var step = 36;
        var right = bounds.Right - 24;
        
        var arrowHintBounds = GetArrowBounds(bounds, right);

        if (arrowHintBounds.Contains(mouse.Position))
        {
            Settings.ArrowHintEnabled = !Settings.ArrowHintEnabled;
            return;
        }

        var animationBounds = GetAnimationBounds(bounds, right);

        if (animationBounds.Contains(mouse.Position))
        {
            Settings.FillAnimationEnabled = !Settings.FillAnimationEnabled;
            return;
        }

        var languageBounds = GetLanguageBounds(bounds, top, step, right);

        if (languageBounds.Contains(mouse.Position))
        {
            ChangeLanguage(action);
            return;
        }
    }

    private void HandleSliders(MouseState mouse)
    {
        var bounds = GetBounds();

        var top = 24;
        var step = 36;
        var left = bounds.X + 24;
        
        var musicBounds = GetMusicSliderInputBounds(bounds, top, step, left);
        var soundBounds = GetSoundSliderInputBounds(bounds, top, step, left);

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
                Settings.MusicVolume = CalculateSliderValue(
                    mouse.Position.X,
                    musicBounds);
            }

            if (_draggingSound)
            {
                Settings.SoundVolume = CalculateSliderValue(
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

    private Rectangle GetBounds()
    {
        const int minWidth = 256;
        const int height = 288;
        const int horizontalPadding = 24;
        const int checkboxWidth = 48;

        var maxTextWidth = _drawService.MeasureString(_languageService.GetText("Settings.FillAnimation")).X;
        var width = Math.Max(minWidth, (int)Math.Ceiling(maxTextWidth + horizontalPadding * 2 + checkboxWidth));

        var position = new Point((int)(_graphicsDevice.Viewport.Width / 2f - width / 2f),
            (int)(_graphicsDevice.Viewport.Height / 2f - height / 2f));

        return new Rectangle(position, new Point(width, height));
    }

    private static Rectangle GetArrowBounds(Rectangle bounds, int right)
    {
        const int width = 24;
        const int height = 24;
        
        return new Rectangle(right - 18, bounds.Y + 24, 
            width, height);
    }

    private static Rectangle GetAnimationBounds(Rectangle bounds, int right)
    {
        const int width = 24;
        const int height = 24;
        
        return new Rectangle(right - 18, bounds.Y + 60, 
            width, height);
    }
    
    private static Rectangle GetLanguageBounds(Rectangle bounds, int top, int step, int right)
    {
        const int width = 48;
        const int height = 36;
        
        return new Rectangle(right - 18 - width / 2, bounds.Y + top + step * 2, 
            width, height);
    }
    
    private static Rectangle GetMusicSliderInputBounds(Rectangle bounds, int top, int step, int left)
    {
        return new Rectangle(left, bounds.Y + top + step * 4 + 20, 
            bounds.Width - 48, 12);
    }

    private static Rectangle GetSoundSliderInputBounds(Rectangle bounds, int top, int step, int left)
    {
        return new Rectangle(left, (int)(bounds.Y + top + step * 5.5f + 20), 
            bounds.Width - 48, 12);
    }

    private void ChangeLanguage(Action action)
    {
        _languageService.ChangeLanguage();
        Settings.Language = _languageService.CurrentLanguage;
        action.Invoke();
    }

    public void SetSettings(SettingsData settings)
    {
        Settings = settings;

        _languageService.SetLanguage(Settings.Language);
    }

    public SettingsData GetSettings()
    {
        return Settings;
    }
}