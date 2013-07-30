using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;

namespace SceneGraph.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    struct VertexShaderInput
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TexCoord;
        public Vector3 Tangent;
        public Vector3 BiNormal;

        public static readonly int SizeInBytes = Utilities.SizeOf<VertexShaderInput>();

        public static readonly InputElement[] InputLayout = {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0),
            new InputElement("NORMAL", 0, Format.R32G32B32_Float, 0),
            new InputElement("TEXCOORD", 0, Format.R32G32_Float, 0), 
            new InputElement("TANGENT", 0, Format.R32G32B32_Float, 0),
            new InputElement("BINORMAL", 0, Format.R32G32B32_Float, 0),
        };
    }
}
