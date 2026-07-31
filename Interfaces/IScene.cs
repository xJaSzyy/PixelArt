using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PixelArt.Services;

namespace PixelArt.Interfaces;

public interface IScene
{
    void LoadContent(ContentManager content);
    void Update(GameTime gameTime);
    void Draw(GameTime gameTime);
    void OnClientSizeChanged(object sender, EventArgs e);
}