using System.Collections.Generic;
using System.Linq;

namespace SceneGraph.Nodes
{
    class ChildList : List<GraphNode>
    {
        private List<GraphNode> _toRemove = new List<GraphNode>();
        private List<GraphNode> _toAdd = new List<GraphNode>(); 

        new public void Add(GraphNode member) 
        {
            _toAdd.Add(member);
        }

        new public void Remove(GraphNode member)
        {
            _toRemove.Add(member);
        }

        public void FinalizeAdd()
        {
            if (!_toRemove.Any() && !_toAdd.Any()) return;

            AddRange(_toAdd);

            RemoveAll(n => _toRemove.Contains(n));

            _toAdd = new List<GraphNode>();
            _toRemove = new List<GraphNode>();
        }
    }
}
