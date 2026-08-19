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

    private readonly Texture2D _pixelTexture;

    public bool IsDialogOpen { get; private set; } = false;
    private string _text = string.Empty;
    private Func<bool> _onConfirm;
    
    private readonly Point _dialogSize = new(320, 196);
    private const int _buttonSize = 64;
    private const int _spacing = 32;

    private readonly Button _confirmButton;
    private readonly Button _cancelButton;

    public DialogService(GraphicsDevice graphicsDevice, DrawService drawService, MouseService mouseService, ContentManager content)
    {
        _graphicsDevice = graphicsDevice;
        _drawService = drawService;
        _mouseService = mouseService;

        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData([Color.White]);
        
        _confirmButton = new Button(content.Load<Texture2D>("Icons/confirm"),
            new Rectangle(0, 0, _buttonSize, _buttonSize));
        
        _cancelButton = new Button(content.Load<Texture2D>("Icons/cancel"),
            new Rectangle(0, 0, _buttonSize, _buttonSize));
    }

    public void Update(MouseState mouse)
    {
        if (!IsDialogOpen)
        {
            return;
        }
        
        _confirmButton.Update(mouse);
        _cancelButton.Update(mouse);
        
        if (_mouseService.IsLeftMouseButtonClicked(mouse))
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

        var halfScreenWidth = _graphicsDevice.Viewport.Width / 2;
        var halfScreenHeight = _graphicsDevice.Viewport.Height / 2;
        var halfButtonSize = _buttonSize / 2;

        var dialogPosition = new Point(halfScreenWidth - _dialogSize.X / 2, halfScreenHeight - _dialogSize.Y / 2);
        
        _drawService.DrawRoundedRectangle(
            spriteBatch,
            new Rectangle(
                dialogPosition.X,
                dialogPosition.Y,
                _dialogSize.X,
                _dialogSize.Y),
            new Color(45, 45, 45),
            6);

        _drawService.DrawString(spriteBatch, _text, new Vector2(halfScreenWidth, dialogPosition.Y + _drawService.MeasureString(_text).Y + _spacing), Color.Yellow, 2f);

        _confirmButton.Bounds = new Rectangle(halfScreenWidth - halfButtonSize - halfButtonSize, halfScreenHeight, _buttonSize, _buttonSize);
        _cancelButton.Bounds = new Rectangle(halfScreenWidth, halfScreenHeight, _buttonSize, _buttonSize);
        
        _confirmButton.Draw(spriteBatch, Color.Green);
        _cancelButton.Draw(spriteBatch, Color.IndianRed);
    }

    public void ShowDialog(string text, Func<bool> onConfirm)
    {
        IsDialogOpen = true;
        _text = text;
        _onConfirm = onConfirm;
    }
    
    private void OnConfirmButtonClicked()
    {
        if (_onConfirm?.Invoke() == true)
        {
            HideDialog();
            _onConfirm = null;
        }
    }

    private void HideDialog()
    {
        IsDialogOpen = false;
    }
}