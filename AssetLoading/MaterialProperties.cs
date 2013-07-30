using System.Runtime.InteropServices;
using SharpDX;

namespace SceneGraph.AssetLoading
{
    [StructLayout(LayoutKind.Sequential)]
    struct MaterialProperties
    {
        public Vector4 Ambient;
        public Vector4 Diffuse;
        public Vector4 Specular;
        public float Shininess;
        private Vector3 pad;

        public static MaterialProperties Default()
        {
            return new MaterialProperties {
                    Ambient = new Vector4(0.4f, 0.4f, 0.4f, 0.4f),
                    Diffuse = new Vector4(0.5f, 0.5f, 0.5f, 0.5f),
                    Specular = new Vector4(0.5f, 0.5f, 0.5f, 0.5f),
                    Shininess = 40f,
                    pad = new Vector3(0, 0, 0)
                };
        }
    }
}
