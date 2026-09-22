using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelArt.Buttons;

namespace PixelArt.Services.Common;

public class TooltipService
{
    private readonly DrawService _drawService;
    private readonly LanguageService _languageService;

    private bool IsVisible { get; set; }
    private string TextKey { get; set; }
    
    private Point _mousePosition;
    
    public TooltipService(DrawService drawService, LanguageService languageService)
    {
        _drawService = drawService;
        _languageService = languageService;
    }

    public void Update(MouseState mouse, Button[] buttons, string[] textKeys)
    {
        _mousePosition = mouse.Position;

        for (var buttonIndex = 0; buttonIndex < buttons.Length; buttonIndex++)
        {
            if (buttons[buttonIndex].IsHovered)
            {
                IsVisible = true;
                TextKey = textKeys[buttonIndex];
                return;
            }

            IsVisible = false;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible)
        {
            return;
        }

        const float scale = .5f;
        const int padding = 4;

        var text = _languageService.GetText(TextKey);
        var textSize = _drawService.MeasureString(text) * scale;

        var halfWidth = textSize.X / 2f + padding;
        var halfHeight = textSize.Y / 2f + padding;
        
        var viewport = spriteBatch.GraphicsDevice.Viewport;

        var x = MathHelper.Clamp(_mousePosition.X, halfWidth, viewport.Width - halfWidth);
        var y = MathHelper.Clamp(_mousePosition.Y, halfHeight, viewport.Height - halfHeight);

        _drawService.DrawStringWithBackground(spriteBatch, text, new Vector2(x, y), Colors.Text, Colors.Black, scale, padding);
    }
}