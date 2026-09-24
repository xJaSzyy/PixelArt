using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelArt.Models;
using PixelArt.Services.Common;

namespace PixelArt.Services;

public class SettingsService
{
    public bool IsActive { get; private set; }

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

    public void Update(MouseState mouse)
    {
        if (!IsActive)
        {
            return;
        }

        HandleCheckboxes(mouse);
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

        _drawService.DrawStringLeft(spriteBatch, "ARROW HINT", new Vector2(left, bounds.Y + 76), Color.White);

        _drawService.DrawCheckbox(
            spriteBatch,
            GetArrowBounds(bounds, right),
            Data.ArrowHintEnabled,
            Color.White,
            Color.Black,
            Color.White);

        _drawService.DrawStringLeft(spriteBatch, "ANIMATION", new Vector2(left, bounds.Y + 120), Color.White);

        _drawService.DrawCheckbox(
            spriteBatch,
            GetAnimationBounds(bounds, right),
            Data.DrawAnimationEnabled,
            Color.White,
            Color.Black,
            Color.White);

        _drawService.DrawStringLeft(spriteBatch, "LANGUAGE", new Vector2(left, bounds.Y + 164), Color.White);

        var languageBounds = GetLanguageBounds(bounds);

        _drawService.DrawRoundedRectangle(spriteBatch, languageBounds, Colors.PanelOuter, 4);

        _drawService.DrawRoundedRectangle(
            spriteBatch,
            new Rectangle(
                languageBounds.X + 2,
                languageBounds.Y + 2,
                languageBounds.Width - 4,
                languageBounds.Height - 4),
            Colors.PanelInner,
            3);

        _drawService.DrawString(spriteBatch, Data.Language.ShortName, languageBounds.Center.ToVector2(), Color.White);

        _drawService.DrawString(spriteBatch, "MUSIC", new Vector2(bounds.Center.X, bounds.Y + 208),
            Color.White);

        _drawService.DrawSlider(
            spriteBatch,
            new Rectangle(left, bounds.Y + 228, bounds.Width - 48, 12),
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
                bounds.Y + 272),
            Color.White);

        _drawService.DrawSlider(
            spriteBatch,
            new Rectangle(
                left,
                bounds.Y + 292,
                bounds.Width - 48,
                12),
            Data.SoundVolume,
            Color.White,
            Color.DarkGray,
            Color.White,
            Color.White);
    }

    private Rectangle GetArrowBounds(Rectangle bounds, int right)
    {
        return new Rectangle(
            right - 18,
            bounds.Y + 76,
            18,
            18);
    }

    private Rectangle GetAnimationBounds(Rectangle bounds, int right)
    {
        return new Rectangle(
            right - 18,
            bounds.Y + 120,
            18,
            18);
    }

    private void HandleCheckboxes(MouseState mouse)
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

        var languageBounds = GetLanguageBounds(bounds);

        if (languageBounds.Contains(mouse.Position))
        {
            ChangeLanguage();
            return;
        }
    }

    private void HandleSliders(MouseState mouse)
    {
        var bounds = GetBounds();

        var musicBounds = GetMusicSliderInputBounds(bounds);
        var soundBounds = GetSoundSliderInputBounds(bounds);

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

    private static Rectangle GetMusicSliderInputBounds(Rectangle bounds)
    {
        return new Rectangle(bounds.X + 24, bounds.Y + 180 + 6, bounds.Width - 48, 24);
    }

    private static Rectangle GetSoundSliderInputBounds(Rectangle bounds)
    {
        return new Rectangle(bounds.X + 24, bounds.Y + 240 + 6, bounds.Width - 48, 24);
    }

    private Rectangle GetBounds()
    {
        var size = new Point(256, 320);

        var position = new Point(
            (int)(_graphicsDevice.Viewport.Width / 2f - size.X / 2f),
            (int)(_graphicsDevice.Viewport.Height / 2f - size.Y / 2f));

        return new Rectangle(position, size);
    }

    private Rectangle GetLanguageBounds(Rectangle bounds)
    {
        return new Rectangle(
            bounds.Right - 70,
            bounds.Y + 145,
            46,
            24);
    }

    private void ChangeLanguage()
    {
        _languageService.ChangeLanguage();
        /*_dialogService.SetText($"{_languageService.GetText("Menu.Pay")} ${_unlockLevelCost}?");
        ResizeTypeButtons();*/
    }
}