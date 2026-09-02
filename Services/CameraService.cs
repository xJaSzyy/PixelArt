using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace PixelArt.Services;

public class CameraService
{
    public float Zoom { get; set; } = 1f;
    public float MinZoom { get; private set; } = 0.2f;
    public float MaxZoom { get; private set; } = 2f;
    
    private Vector2 _cameraPosition;

    private float _zoomSpeed = 0.1f;

    private bool _isDragging;
    private Point _lastMousePosition;
    
    public void Update(MouseState mouse)
    {
        if (mouse.RightButton == ButtonState.Pressed)
        {
            if (!_isDragging)
            {
                _lastMousePosition = mouse.Position;
                _isDragging = true;
            }

            var delta = mouse.Position - _lastMousePosition;

            _cameraPosition += delta.ToVector2();

            _lastMousePosition = mouse.Position;
        }
        else
        {
            _isDragging = false;
        }
    }
    
    public void ChangeZoom(MouseState mouse, int scrollDelta)
    {
        var oldZoom = Zoom;

        var mouseWorld = (mouse.Position.ToVector2() - _cameraPosition) / oldZoom;

        Zoom += scrollDelta > 0 ? _zoomSpeed : -_zoomSpeed;
        Zoom = MathHelper.Clamp(Zoom, MinZoom, MaxZoom);

        _cameraPosition =
            mouse.Position.ToVector2() - mouseWorld * Zoom;
    }

    public void SetPosition(Vector2 position)
    {
        _cameraPosition = position;
    }
    
    public Vector2 GetPosition()
    {
        return _cameraPosition;
    }

    public void SetZoomBounds(float min, float max, float speed)
    {
        MinZoom = min;
        MaxZoom = max;
        _zoomSpeed = speed;

        Zoom = 1f;
        _cameraPosition = Vector2.Zero;
    }
}