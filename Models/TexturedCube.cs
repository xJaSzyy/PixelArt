using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelArt.Models;

public class TexturedCube
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly BasicEffect effect;

    private readonly VertexPositionTexture[] vertices;
    private readonly short[] indices;

    private readonly Texture2D[] textures;

    public TexturedCube(
        GraphicsDevice graphicsDevice,
        Texture2D front,
        Texture2D back,
        Texture2D left,
        Texture2D right,
        Texture2D top,
        Texture2D bottom)
    {
        this.graphicsDevice = graphicsDevice;

        textures = new Texture2D[]
        {
            front,
            back,
            left,
            right,
            top,
            bottom
        };

        effect = new BasicEffect(graphicsDevice)
        {
            TextureEnabled = true,
            VertexColorEnabled = false,
            LightingEnabled = false
        };

        vertices = new VertexPositionTexture[24];
        indices = new short[36];

        CreateCube();
    }

    private void CreateCube()
    {
        float s = 0.5f;

        // Front (+Z)
        AddFace(
            0,
            new Vector3(-s, -s, s),
            new Vector3(s, -s, s),
            new Vector3(s, s, s),
            new Vector3(-s, s, s));

        // Back (-Z)
        AddFace(
            1,
            new Vector3(s, -s, -s),
            new Vector3(-s, -s, -s),
            new Vector3(-s, s, -s),
            new Vector3(s, s, -s));

        // Left (-X)
        AddFace(
            2,
            new Vector3(-s, -s, -s),
            new Vector3(-s, -s, s),
            new Vector3(-s, s, s),
            new Vector3(-s, s, -s));

        // Right (+X)
        AddFace(
            3,
            new Vector3(s, -s, s),
            new Vector3(s, -s, -s),
            new Vector3(s, s, -s),
            new Vector3(s, s, s));

        // Top (+Y)
        AddFace(
            4,
            new Vector3(-s, s, s),
            new Vector3(s, s, s),
            new Vector3(s, s, -s),
            new Vector3(-s, s, -s));

        // Bottom (-Y)
        AddFace(
            5,
            new Vector3(-s, -s, -s),
            new Vector3(s, -s, -s),
            new Vector3(s, -s, s),
            new Vector3(-s, -s, s));
    }

    private void AddFace(
        int faceIndex,
        Vector3 bottomLeft,
        Vector3 bottomRight,
        Vector3 topRight,
        Vector3 topLeft)
    {
        int vertexIndex = faceIndex * 4;

        vertices[vertexIndex + 0] =
            new VertexPositionTexture(
                bottomLeft,
                new Vector2(0, 1));

        vertices[vertexIndex + 1] =
            new VertexPositionTexture(
                bottomRight,
                new Vector2(1, 1));

        vertices[vertexIndex + 2] =
            new VertexPositionTexture(
                topRight,
                new Vector2(1, 0));

        vertices[vertexIndex + 3] =
            new VertexPositionTexture(
                topLeft,
                new Vector2(0, 0));

        int index = faceIndex * 6;

        indices[index + 0] = (short)(vertexIndex + 0);
        indices[index + 1] = (short)(vertexIndex + 1);
        indices[index + 2] = (short)(vertexIndex + 2);

        indices[index + 3] = (short)(vertexIndex + 0);
        indices[index + 4] = (short)(vertexIndex + 2);
        indices[index + 5] = (short)(vertexIndex + 3);
    }

    public void Draw(
        Matrix world,
        Matrix view,
        Matrix projection)
    {
        effect.World = world;
        effect.View = view;
        effect.Projection = projection;

        effect.TextureEnabled = true;
        effect.LightingEnabled = false;

        graphicsDevice.BlendState = BlendState.Opaque;
        graphicsDevice.DepthStencilState = DepthStencilState.Default;
        graphicsDevice.RasterizerState =
            RasterizerState.CullClockwise;

        for (int face = 0; face < 6; face++)
        {
            effect.Texture = textures[face];

            int startIndex = face * 6;

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    vertices,
                    0,
                    vertices.Length,
                    indices,
                    startIndex,
                    2);
            }
        }
    }
}