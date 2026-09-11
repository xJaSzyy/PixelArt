using Microsoft.Xna.Framework.Input;

namespace PixelArt.Services;

public class KeyboardService
{
    private KeyboardState _prevState;
    
    public bool IsKeyPressed(KeyboardState state, Keys key)
    {
        return state.IsKeyDown(key);
    }
    
    public bool IsKeyUpOnce(KeyboardState state, Keys key)
    {
        return state.IsKeyUp(key) && _prevState.IsKeyDown(key);
    }

    public void SetState(KeyboardState state)
    {
        _prevState = state;
    }
}