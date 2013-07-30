using System;
using System.Linq;
using SceneGraph.Rendering;
using SharpDX;

namespace SceneGraph.Nodes
{
    class RotatorAnimationNode : GraphNode
    {
        private readonly float _speed;
        private readonly float _maxSum;

        private float _xRotationSum;
        private float _yRotationSum;
        private float _zRotationSum;

        private bool _negDirection;

        public RotatorAnimationNode(float speed, float degrees)
        {
            _speed = speed;
            _maxSum = degrees * (float) Math.PI / 180f;
        }

        protected override void UpdateThis(GraphNode parent, RenderDevice device)
        {
            var scale = _speed * (float) Program.TickTime;

            var flip = _negDirection ? -1 : 1;

            if (_xRotationSum <= _maxSum)
            {
               Rotate(scale * flip, 0, 0);
                
                if (_xRotationSum + scale > _maxSum)
                    Rotate((_maxSum - _xRotationSum) * flip, 0, 0);

                _xRotationSum += scale;
            }
            else if (_yRotationSum <= _maxSum)
            {
                Rotate(0, scale * flip, 0);

                if (_yRotationSum + scale > _maxSum)
                    Rotate(0, (_maxSum - _yRotationSum) * flip, 0);

                _yRotationSum += scale;
            }
            else if (_zRotationSum <= _maxSum)
            {
                Rotate(0, 0, scale * flip);

                if (_zRotationSum + scale > _maxSum)
                    Rotate(0, 0, (_maxSum - _zRotationSum) * flip);

                _zRotationSum += scale;
            }
            else
            {
                _xRotationSum = -_xRotationSum;
                _yRotationSum = -_yRotationSum;
                _zRotationSum = -_zRotationSum;

                _negDirection = !_negDirection;
            }

            UpdateTransforms(parent, device);
            UpdateChildren(device);
        }

        public override GraphNode Copy()
        {
            var newNode = new TranslationAnimationNode(_speed, _maxSum * 180f / (float) Math.PI);

            foreach (var child in Children)
                newNode.AddChild(child.Copy());

            return newNode;
        }
    }
}
