using Microsoft.Xna.Framework.Input;

namespace PixelArt.Services;

public class MouseService
{
    private MouseState _prevMouse;
    
    public bool IsLeftMouseButtonClicked(MouseState mouse)
    {
        return mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
    }
    
    public bool IsLeftMouseButtonPressed(MouseState mouse)
    {
        return mouse.LeftButton == ButtonState.Pressed;
    }
    
    public bool IsRightMouseButtonClicked(MouseState mouse)
    {
        return mouse.RightButton == ButtonState.Pressed && _prevMouse.RightButton == ButtonState.Released;
    }

    public void SetMouse(MouseState mouse)
    {
        _prevMouse = mouse;
    }
}