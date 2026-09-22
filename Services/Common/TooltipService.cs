using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelArt.Buttons;

namespace PixelArt.Services.Common;

public class TooltipService
{
    private readonly DrawService _drawService;

    private bool IsVisible { get; set; }
    private string Text { get; set; }
    
    private Point _mousePosition;
    
    public TooltipService(DrawService drawService)
    {
        _drawService = drawService;
    }

    public void Update(MouseState mouse, Button[] buttons, string[] texts)
    {
        _mousePosition = mouse.Position;

        for (var buttonIndex = 0; buttonIndex < buttons.Length; buttonIndex++)
        {
            if (buttons[buttonIndex].IsHovered)
            {
                IsVisible = true;
                Text = texts[buttonIndex];
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

        var textSize = _drawService.MeasureString(Text) * scale;

        var viewport = spriteBatch.GraphicsDevice.Viewport;

        var halfWidth = textSize.X / 2f + padding;
        var halfHeight = textSize.Y / 2f + padding;

        var x = MathHelper.Clamp(
            _mousePosition.X,
            halfWidth,
            viewport.Width - halfWidth
        );

        var y = MathHelper.Clamp(
            _mousePosition.Y,
            halfHeight,
            viewport.Height - halfHeight
        );

        _drawService.DrawStringWithBackground(spriteBatch, Text, new Vector2(x, y), Colors.Text, Colors.Black, scale, padding);
    }
}