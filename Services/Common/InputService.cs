using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PixelArt.Services.Common;

public class InputService(GraphicsDevice graphicsDevice)
{
    private MouseState _prevMouse;
    private KeyboardState _prevKeyboard;

    #region Mouse

    public bool IsLeftMouseButtonClicked(MouseState mouse)
    {
        return mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released && IsMouseInsideWindow(mouse);
    }
    
    public bool IsLeftMouseButtonPressed(MouseState mouse)
    {
        return mouse.LeftButton == ButtonState.Pressed && IsMouseInsideWindow(mouse);
    }
    
    public bool IsLeftMouseButtonReleased(MouseState mouse)
    {
        return mouse.LeftButton == ButtonState.Released && _prevMouse.LeftButton != ButtonState.Released && IsMouseInsideWindow(mouse);
    }
    
    public bool IsRightMouseButtonClicked(MouseState mouse)
    {
        return mouse.RightButton == ButtonState.Pressed && _prevMouse.RightButton == ButtonState.Released && IsMouseInsideWindow(mouse);
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

    #endregion
    
    #region Keyboard 
    
    public bool IsKeyPressed(KeyboardState state, Keys key)
    {
        return state.IsKeyDown(key);
    }
    
    public bool IsKeyUpOnce(KeyboardState state, Keys key)
    {
        return state.IsKeyUp(key) && _prevKeyboard.IsKeyDown(key);
    }
    
    #endregion
    
    public void SetState(MouseState mouse, KeyboardState keyboard)
    {
        _prevMouse = mouse;
        _prevKeyboard = keyboard;
    }
}