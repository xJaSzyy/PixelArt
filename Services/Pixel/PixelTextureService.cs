using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelArt.Services.Pixel;

public sealed class PixelTextureService
{
    private Texture2D _texture;
    private Color[] _pixels = [];
    private bool _dirty;

    public void SetTexture(Texture2D texture)
    {
        _texture = texture;
        _pixels = new Color[texture.Width * texture.Height];

        texture.GetData(_pixels);

        _dirty = false;
    }

    public void SetPixel(int index, Color color)
    {
        _pixels[index] = color;
        _dirty = true;
    }

    public void Upload()
    {
        if (!_dirty)
        {
            return;
        }

        _texture.SetData(_pixels);
        _dirty = false;
    }

    public Color[] CreateCopy()
    {
        return (Color[])_pixels.Clone();
    }
}