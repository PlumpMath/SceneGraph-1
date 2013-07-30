using System.Collections.Generic;
using SceneGraph.Rendering;

namespace SceneGraph.AssetLoading
{
    class Mesh
    {
        public string Name;

        public List<VertexShaderInput> Vertices = new List<VertexShaderInput>();
        public List<uint> Indices = new List<uint>();

        public Appearance Appearance;

        public Mesh Copy()
        {
            var mesh = new Mesh {
                    Appearance = Appearance.Copy(),
                    Name = Name,
                    Vertices = Vertices,
                    Indices = Indices
                };

            return mesh;
        }
    }
}
