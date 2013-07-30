using System.Collections.Generic;
using SharpDX;

namespace SceneGraph.AssetLoading
{
    class ModelGroup
    {
        public List<Vector3> Vertices { get; private set; }
        public List<Vector2> TextureCoords { get; private set; }

        public Dictionary<int, List<Face>> Neighborhood { get; private set; }  
        public List<Model> Models { get; private set; }

        public ModelGroup()
        {
            Vertices = new List<Vector3>();
            TextureCoords = new List<Vector2>();

            Neighborhood = new Dictionary<int, List<Face>>();
            Models = new List<Model>();
        }
    }
}
