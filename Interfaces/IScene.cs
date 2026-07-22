using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PixelArt.Interfaces;

public interface IScene
{
    void Initialize(SceneManager sceneManager);
    void LoadContent(GraphicsDevice graphicsDevice, ContentManager content);
    void Update(GameTime gameTime);
    void Draw(GameTime gameTime);
}