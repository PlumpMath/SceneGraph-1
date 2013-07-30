using System.Collections.Generic;

namespace SceneGraph.AssetLoading
{
    class Model : List<Face>
    {
        public string Name;
        public Appearance Appearance = new Appearance();
    }
}
