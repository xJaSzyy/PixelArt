using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PixelArt.Services;

namespace PixelArt.Interfaces;

public interface IScene
{
    void Initialize(SceneService sceneService, MouseService mouseService, DrawService drawService);
    void LoadContent(GraphicsDevice graphicsDevice, ContentManager content);
    void Update(GameTime gameTime);
    void Draw(GameTime gameTime);
}