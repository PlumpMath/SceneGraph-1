using SceneGraph.Rendering;

namespace SceneGraph.Nodes
{
    class TranslationAnimationNode : GraphNode
    {
        private readonly float _speed;
        private readonly float _distance;

        private float _xTranslationSum;
        private float _zTranslationSum;

        private bool _negDirection;

        public TranslationAnimationNode(float speed, float distance)
        {
            _speed = speed;
            _distance = distance;
        }

        protected override void UpdateThis(GraphNode parent, RenderDevice device)
        {
            var scale = _speed * (float) Program.TickTime;

            var flip = _negDirection ? -1 : 1;

            if (_xTranslationSum <= _distance)
            {
                Translate(scale * flip, 0, 0);

                if (_xTranslationSum + scale > _distance)
                    Translate((_distance - _xTranslationSum) * flip, 0, 0);

                _xTranslationSum += scale;
            }
            else if (_zTranslationSum <= _distance)
            {
                Translate(0, 0, scale * flip);

                if (_zTranslationSum + scale > _distance)
                    Translate(0, 0, (_distance - _zTranslationSum) * flip);

                _zTranslationSum += scale;
            }
            else
            {
                _xTranslationSum = 0;
                _zTranslationSum = 0;

                _negDirection = !_negDirection;
            }

            UpdateTransforms(parent, device);
            UpdateChildren(device);
        }

        public override GraphNode Copy()
        {
            var newNode = new TranslationAnimationNode(_speed, _distance);

            foreach (var child in Children)
                newNode.AddChild(child.Copy());

            return newNode;
        }
    }
}
