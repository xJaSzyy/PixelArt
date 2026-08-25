using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelArt.Buttons;

namespace PixelArt.Services;

public class DialogService
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly DrawService _drawService;
    private readonly MouseService _mouseService;

    public bool IsDialogOpen { get; private set; }

    private string _text = string.Empty;
    private Func<bool> _onConfirm;

    private readonly Point _dialogSize = new(320, 196);

    private const int _buttonSize = 64;
    private const int _spacing = 40;

    private readonly Button _confirmButton;
    private readonly Button _cancelButton;

    private float _animationProgress;
    private bool _isClosing;

    private const float _openAnimationDuration = 0.25f;
    private const float _closeAnimationDuration = 0.1f;

    public DialogService(
        GraphicsDevice graphicsDevice,
        DrawService drawService,
        MouseService mouseService,
        ContentManager content)
    {
        _graphicsDevice = graphicsDevice;
        _drawService = drawService;
        _mouseService = mouseService;

        var pixelTexture = new Texture2D(
            _graphicsDevice,
            1,
            1);

        pixelTexture.SetData([Color.White]);

        _confirmButton = new Button(
            content.Load<Texture2D>("Icons/confirm"),
            new Rectangle(
                0,
                0,
                _buttonSize,
                _buttonSize));

        _cancelButton = new Button(
            content.Load<Texture2D>("Icons/cancel"),
            new Rectangle(
                0,
                0,
                _buttonSize,
                _buttonSize));
    }

    public void Update(MouseState mouse, GameTime gameTime)
    {
        if (!IsDialogOpen)
        {
            return;
        }

        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_isClosing)
        {
            _animationProgress -= deltaTime / _closeAnimationDuration;

            if (_animationProgress <= 0f)
            {
                _animationProgress = 0f;

                _isClosing = false;
                IsDialogOpen = false;

                _onConfirm = null;
                _mouseService.SetMouse(mouse);

                return;
            }
        }
        else
        {
            _animationProgress += deltaTime / _openAnimationDuration;

            if (_animationProgress >= 1f)
            {
                _animationProgress = 1f;
            }
        }

        _confirmButton.Update(mouse);
        _cancelButton.Update(mouse);
        
        if (!_isClosing && _mouseService.IsLeftMouseButtonClicked(mouse))
        {
            if (_confirmButton.IsHovered)
            {
                OnConfirmButtonClicked();
            }
            else if (_cancelButton.IsHovered)
            {
                HideDialog();
            }
        }

        _mouseService.SetMouse(mouse);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsDialogOpen)
        {
            return;
        }

        var screenCenter = new Vector2(_graphicsDevice.Viewport.Width / 2f, _graphicsDevice.Viewport.Height / 2f);

        var scale = _isClosing
            ? EaseInCubic(_animationProgress)
            : EaseOutBack(_animationProgress);

        var dialogPosition = new Point(
            (int)(screenCenter.X - _dialogSize.X / 2f),
            (int)(screenCenter.Y - _dialogSize.Y / 2f));

        var transformMatrix =
            Matrix.CreateTranslation(
                -screenCenter.X,
                -screenCenter.Y,
                0f)
            *
            Matrix.CreateScale(scale)
            *
            Matrix.CreateTranslation(
                screenCenter.X,
                screenCenter.Y,
                0f);

        spriteBatch.End();
        spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: transformMatrix);
        
        var textSize = _drawService.MeasureString(_text);
        
        _drawService.DrawRoundedRectangle(
            spriteBatch,
            new Rectangle(
                dialogPosition.X + 6,
                dialogPosition.Y + 6,
                _dialogSize.X,
                _dialogSize.Y),
            new Color(Colors.Black, 32),
            6);
        
        _drawService.DrawRoundedRectangle(
            spriteBatch,
            new Rectangle(
                dialogPosition.X,
                dialogPosition.Y,
                _dialogSize.X,
                _dialogSize.Y),
            Colors.LightBackground,
            6);
        
        _drawService.DrawRoundedRectangle(
            spriteBatch,
            new Rectangle(
                dialogPosition.X + 6,
                dialogPosition.Y + 6,
                _dialogSize.X - 12,
                _dialogSize.Y - 12),
            new Color(Colors.DarkBackground, 160),
            6);
        
        _drawService.DrawString(
            spriteBatch,
            _text,
            new Vector2(
                screenCenter.X,
                dialogPosition.Y + textSize.Y + _spacing),
            Colors.Yellow,
            2f);

        _confirmButton.Bounds = new Rectangle(
            (int)screenCenter.X - _buttonSize,
            (int)screenCenter.Y,
            _buttonSize,
            _buttonSize);

        _cancelButton.Bounds = new Rectangle(
            (int)screenCenter.X,
            (int)screenCenter.Y,
            _buttonSize,
            _buttonSize);

        _confirmButton.Draw(spriteBatch, Colors.Green);
        _cancelButton.Draw(spriteBatch, Colors.Red);

        spriteBatch.End();
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
    }

    public void ShowDialog(string text, Func<bool> onConfirm)
    {
        IsDialogOpen = true;

        _text = text;
        _onConfirm = onConfirm;

        _isClosing = false;
        _animationProgress = 0f;
    }

    private void OnConfirmButtonClicked()
    {
        if (_onConfirm?.Invoke() == true)
        {
            HideDialog();
        }
    }

    private void HideDialog()
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;

        return 1f + c3 * MathF.Pow(t - 1f, 3f) + c1 * MathF.Pow(t - 1f, 2f);
    }

    private static float EaseInCubic(float t)
    {
        return t * t * t;
    }
}