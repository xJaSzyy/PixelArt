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

    private readonly Texture2D _pixelTexture;

    public bool ShowDialog { get; set; } = false;
    private readonly Point _dialogSize = new(512, 196);
    private const int _buttonSize = 64;
    private const int _spacing = 32;

    private readonly Button _confirmButton;
    private readonly Button _cancelButton;

    public DialogService(GraphicsDevice graphicsDevice, DrawService drawService, ContentManager content)
    {
        _graphicsDevice = graphicsDevice;
        _drawService = drawService;
        
        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData([Color.White]);
        
        _confirmButton = new Button(content.Load<Texture2D>("Icons/confirm"),
            new Rectangle(0, 0, _buttonSize, _buttonSize));
        
        _cancelButton = new Button(content.Load<Texture2D>("Icons/cancel"),
            new Rectangle(0, 0, _buttonSize, _buttonSize));
    }

    public void Update(MouseState mouse)
    {
        _confirmButton.Update(mouse);
        _cancelButton.Update(mouse);
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        if (!ShowDialog)
        {
            return;
        }

        var screenWidth = _graphicsDevice.Viewport.Width;
        var screenHeight = _graphicsDevice.Viewport.Height;

        var dialogPosition = new Point(screenWidth / 2 - _dialogSize.X / 2, screenHeight / 2 - _dialogSize.Y / 2);
        
        spriteBatch.Draw(_pixelTexture,
            new Rectangle(dialogPosition.X, dialogPosition.Y, _dialogSize.X, _dialogSize.Y), 
            new Color(45, 45, 45));

        const string text = "Pay 49 Pixoins?";
        _drawService.DrawString(spriteBatch, text, new Vector2(screenWidth * .5f, dialogPosition.Y + _drawService.MeasureString(text).Y + _spacing), Color.OrangeRed, 2f);

        _confirmButton.Bounds = new Rectangle(screenWidth / 2 - _buttonSize / 2 - _buttonSize / 2, screenHeight / 2, 
            _buttonSize, _buttonSize);
        
        _cancelButton.Bounds = new Rectangle(screenWidth / 2 - _buttonSize / 2 + _buttonSize / 2, screenHeight / 2, 
            _buttonSize, _buttonSize);
        
        _confirmButton.Draw(spriteBatch, Color.Green);
        _cancelButton.Draw(spriteBatch, Color.IndianRed);
    }
}