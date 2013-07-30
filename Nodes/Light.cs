using System.Runtime.InteropServices;
using SharpDX;

namespace SceneGraph.Nodes
{
    [StructLayout(LayoutKind.Sequential)]
    struct Light
    {
        public Vector3 Color;
        public Vector3 Direction;
    }
}
