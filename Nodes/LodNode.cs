using System.Collections.Generic;
using System.Linq;
using SceneGraph.Rendering;

namespace SceneGraph.Nodes
{
    class LodNode : GraphNode
    {
        private readonly List<float> _lodSwitches = new List<float>();

        protected override void UpdateThis(GraphNode parent, RenderDevice device)
        {
            if (_lodSwitches.Count - 1 < Children.Count)
                GenerateLodDistances();

            if (Children.Count == 0) return;

            var distance = Children.First().DistanceMergedWithRadius(Camera.Position);

            for (var i = 0; i < Children.Count; i++)
            {
                var intervalStart = _lodSwitches[i];
                var intervalEnd = _lodSwitches[i + 1];

                if (InInterval(intervalStart, intervalEnd, distance))
                    Children[i].Update(this, device);
            }
        }

        private static bool InInterval(float min, float max, float value)
        {
            return value >= min && value < max;
        }

        public override GraphNode Copy()
        {
            var newNode = new LodNode();
            foreach (var child in Children)
                newNode.AddChild(child.Copy());

            return newNode;
        }

        private void GenerateLodDistances()
        {
            _lodSwitches.Clear();

            var lodCounter = 0.0f;
            _lodSwitches.Add(lodCounter);

            for (var i = 0; i < Children.Count; i++)
            {
                if (i == Children.Count - 1) _lodSwitches.Add(float.MaxValue);
                else _lodSwitches.Add(lodCounter += Children[0].WorldRadius());
            }
        }
    }
}
