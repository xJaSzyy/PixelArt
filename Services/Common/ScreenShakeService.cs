using System;

namespace PixelArt.Services.Common;

using Microsoft.Xna.Framework;

public class ScreenShakeService
{
    private readonly Random _random = new();

    private float _timeRemaining;
    private float _duration;
    private float _intensity;

    public bool IsShaking => _timeRemaining > 0;

    public void Shake(float duration, float intensity)
    {
        _duration = duration;
        _timeRemaining = duration;
        _intensity = intensity;
    }

    public Vector2 Update(float deltaTime)
    {
        if (_timeRemaining <= 0)
        {
            return Vector2.Zero;
        }

        _timeRemaining -= deltaTime;

        var progress = _timeRemaining / _duration;

        // Постепенно уменьшаем силу к концу.
        var currentIntensity = _intensity * progress;

        var x = (float)(_random.NextDouble() * 2 - 1) * currentIntensity;
        var y = (float)(_random.NextDouble() * 2 - 1) * currentIntensity;

        return new Vector2(x, y);
    }
}