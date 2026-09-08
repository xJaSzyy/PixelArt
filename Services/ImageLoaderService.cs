#nullable enable
using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using NativeFileDialogSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PixelArt.Services;

public class ImageLoaderService
{
    private readonly Point _maxSize = new(128, 128);
    
    public static string? PickImage()
    {
        var result = Dialog.FileOpen("png,jpg,jpeg,bmp");

        if (result.IsError || string.IsNullOrWhiteSpace(result.Path))
        {
            return null;
        }

        return result.Path;
    }
    
    public Texture2D? LoadTexture(GraphicsDevice graphicsDevice, string path)
    {
        try
        {
            using var image = Image.Load<Rgba32>(path);

            image.Mutate(x => x.AutoOrient());

            if (image.Width > _maxSize.X || image.Height > _maxSize.Y)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(_maxSize.X, _maxSize.Y),
                    Mode = ResizeMode.Max
                }));
            }

            using var stream = new MemoryStream();

            image.SaveAsPng(stream);
            stream.Position = 0;

            var texture = Texture2D.FromStream(graphicsDevice, stream);
            
            return texture;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load image: {ex}");
        }

        return null;
    }
}