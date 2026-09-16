using System;
using PixelArt.Models;

namespace PixelArt.Services;

public sealed class PixelReplayService
{
    public bool IsRunning { get; private set; }

    private int _historyIndex;
    private float _pixelsAccumulator;

    public float ReplayDuration { get; } = 1.5f;

    public void Update(LevelData level, float deltaTime, Action<int> onPixel)
    {
        if (!IsRunning)
        {
            return;
        }

        var historyCount = level.History.Count;

        if (historyCount == 0)
        {
            Stop();
            return;
        }

        var pixelsPerSecond = historyCount / ReplayDuration;

        _pixelsAccumulator += pixelsPerSecond * deltaTime;

        while (_pixelsAccumulator >= 1f &&
               _historyIndex < historyCount)
        {
            var pixelIndex = level.History[_historyIndex++];

            onPixel(pixelIndex);

            _pixelsAccumulator -= 1f;
        }

        if (_historyIndex >= historyCount)
        {
            Stop();
        }
    }
    
    public void Start()
    {
        _historyIndex = 0;
        _pixelsAccumulator = 0;
        IsRunning = true;
    }

    private void Stop()
    {
        IsRunning = false;
    }
}