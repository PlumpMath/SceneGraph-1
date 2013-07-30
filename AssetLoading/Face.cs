using System.Collections.Generic;
using SharpDX;

namespace SceneGraph.AssetLoading
{
    class Face
    {
        public Vector3 BiNormal = Vector3.Zero;
        public Vector3 Tangent = Vector3.Zero;
        public Vector3 Normal = Vector3.Zero;

        public List<Point> Points = new List<Point>();
    }
}
