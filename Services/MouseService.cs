using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PixelArt.Services;

public class MouseService(GraphicsDevice graphicsDevice)
{
    private MouseState _prevMouse;
    
    public bool IsLeftMouseButtonClicked(MouseState mouse)
    {
        return mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released && IsMouseInsideWindow(mouse);
    }
    
    public bool IsLeftMouseButtonPressed(MouseState mouse)
    {
        return mouse.LeftButton == ButtonState.Pressed && IsMouseInsideWindow(mouse);
    }
    
    public bool IsRightMouseButtonClicked(MouseState mouse)
    {
        return mouse.RightButton == ButtonState.Pressed && _prevMouse.RightButton == ButtonState.Released && IsMouseInsideWindow(mouse);
    }

    public void SetMouse(MouseState mouse)
    {
        _prevMouse = mouse;
    }
    
    public bool IsScroll(MouseState mouse)
    {
        return mouse.ScrollWheelValue != _prevMouse.ScrollWheelValue;
    }

    public int GetScrollDelta(MouseState mouse)
    {
        return mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
    }
    
    private bool IsMouseInsideWindow(MouseState mouse)
    {
        return mouse.X >= 0 && mouse.Y >= 0 &&
               mouse.X < graphicsDevice.Viewport.Width && mouse.Y < graphicsDevice.Viewport.Height;
    }
}