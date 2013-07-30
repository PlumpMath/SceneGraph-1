using SharpDX;

namespace SceneGraph.AssetLoading
{
    struct Point
    {
        public int PositionIndex;
        public int TextureIndex;

        public Vector3 Normal;
        public Vector3 Tangent;
        public Vector3 BiNormal;

        public static bool operator ==(Point x, Point y)
        {
            return x.PositionIndex == y.PositionIndex
                   && x.TextureIndex == y.TextureIndex;
        }

        public static bool operator !=(Point x, Point y)
        {
            return !(x == y);
        }

        public bool Equals(Point other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Point && Equals((Point)obj);
        }

        public override int GetHashCode()
        {
            return (PositionIndex * 397) ^ TextureIndex;
        }
    }
}
