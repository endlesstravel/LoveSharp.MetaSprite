using Love;
using System.Collections.Generic;
namespace MetaSprite
{
    public class Sprite
    {
        public string name;
        public float duration;
        public Frame frame;
        public Image image;
        public Quad quad;
        public SpriteAlignment alignment;
        public RectangleF rect;
        public Vector2 imgQuadOffset;
        public Vector2 spritedPivot;

        readonly public Dictionary<string, RectangleF> rectDict = new Dictionary<string, RectangleF>();
        readonly public Dictionary<string, Vector2> transDict = new Dictionary<string, Vector2>();
    }

    public static class FunctionBoost
    {
        public static Color Color_Lerp(Color a, Color b, float t)
        {
            float it = 1 - t;
            return new Color(
                it * a.Rf + t * b.Rf,
                it * a.Gf + t * b.Gf,
                it * a.Bf + t * b.Bf,
                it * a.Af + t * b.Af
                );
        }

        public static Vector2 Vector2_Scale(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X * b.X, a.Y * b.Y);
        }
    }
}