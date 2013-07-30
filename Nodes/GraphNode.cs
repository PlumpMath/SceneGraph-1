using System;
using System.Collections.Generic;
using System.Linq;
using SceneGraph.Rendering;
using SharpDX;

namespace SceneGraph.Nodes
{
    abstract class GraphNode
    {
        private Matrix _translation = Matrix.Identity;
        private Matrix _worldTranslation = Matrix.Identity;

        private Matrix _rotation = Matrix.Identity;
        private Matrix _worldRotation = Matrix.Identity;

        private Matrix _worldTransform = Matrix.Identity;

        public readonly ChildList Children = new ChildList();

        protected BoundingSphere BoundingSphere;
        private BoundingSphere _worldBoundingSphere;
        
        public void Update(GraphNode parent, RenderDevice device)
        {
            if (!Camera.ViewFrustrum.Intersects(ref _worldBoundingSphere))
                return;

            Children.FinalizeAdd();
            UpdateThis(parent, device);
        }

        protected abstract void UpdateThis(GraphNode parent, RenderDevice device);
        public abstract GraphNode Copy();

        protected void UpdateChildren(RenderDevice device)
        {
            foreach (var child in Children)
                child.Update(this, device);
        }

        protected void UpdateTransforms(GraphNode parent, RenderDevice device)
        {
            ApplyTransform(parent);

            if (this is ModelNode)
                WriteMatricesToConstantBuffers(device);
        }

        private void ApplyTransform(GraphNode parent)
        {
            if (parent is ModelNode)
                _worldTransform = _rotation * _translation * parent._worldTransform;
            else
            {
                _worldRotation = _rotation * parent._worldRotation;
                _worldTranslation = _translation * parent._worldTranslation;

                _worldTransform = _worldRotation * _worldTranslation;
            }
        }

        public BoundingSphere UpdateWorldBoundingSphere(GraphNode parent)
        {
            if (parent != this)
                ApplyTransform(parent);

            _worldBoundingSphere = BoundingSphere.Transform(_worldTransform);

            foreach (var child in Children)
                _worldBoundingSphere = BoundingSphere.Merge(child.UpdateWorldBoundingSphere(this), _worldBoundingSphere);

            return _worldBoundingSphere;
        }

        public void Translate(float x, float y, float z)
        {
            _translation *= Matrix.Translation(x, y, z);
        }

        public void Rotate(float yaw, float pitch, float roll)
        {
            _rotation 
                *= Matrix.Translation(-BoundingSphere.Center.X, -BoundingSphere.Center.Y, -BoundingSphere.Center.Z) 
                 * Matrix.RotationYawPitchRoll(yaw, pitch, roll)
                 * Matrix.Translation(BoundingSphere.Center.X, BoundingSphere.Center.Y, BoundingSphere.Center.Z);
        }

        public void AddChild(GraphNode node)
        {
            Children.Add(node);
        }

        public bool RemoveChild(GraphNode node)
        {
            if (Children.Contains(node))
            {
                Children.Remove(node);
                return true;
            }

            return Children.Any(child => child.RemoveChild(node));
        }

        public bool ReplaceChild(GraphNode oldChild, GraphNode newChild)
        {
            if (Children.Contains(oldChild))
            {
                Children.Remove(oldChild);
                Children.Add(newChild);
                return true;
            }

            return Children.Any(child => child.ReplaceChild(oldChild, newChild));
        }

        public IEnumerable<GraphNode> Flatten()
        {
            var list = new List<GraphNode> { this };
            foreach (var child in Children)
                list.AddRange(child.Flatten());

            return list;
        }

        public IEnumerable<GraphNode> Flatten(Func<GraphNode, bool> filter)
        {
            return Flatten().Where(filter);
        }

        public float DistanceToCenter(Vector3 other)
        {
            var transformedBound = BoundingSphere.Transform(_worldTransform);
            return Vector3.Distance(other, transformedBound.Center);
        }

        public float DistanceToMergedCenter(Vector3 other)
        {
            return Vector3.Distance(other, _worldBoundingSphere.Center);
        }

        public float DistanceMergedWithRadius(Vector3 other)
        {
            return Math.Max(0, DistanceToMergedCenter(other) - _worldBoundingSphere.Radius);
        }

        public float WorldRadius()
        {
            if (_worldBoundingSphere.Radius == 0) UpdateWorldBoundingSphere(this);
            return _worldBoundingSphere.Radius;
        }

        private void WriteMatricesToConstantBuffers(RenderDevice device)
        {
            device.ShaderParameters.World.SetMatrix(_worldTransform);
            device.ShaderParameters.WorldViewProjection.SetMatrix(_worldTransform * Camera.View() * Camera.Projection());
            device.ShaderParameters.TransposeInvWorld.SetMatrix(Matrix.Transpose(Matrix.Invert(_worldTransform)));
            
            device.ShaderParameters.ObjectSelected.Set(this == Program.SelectedGraphNode);

            var transformedBound = BoundingSphere.Transform(_worldTransform);
            device.ShaderParameters.ObjectCenter.Set(transformedBound.Center);
            device.ShaderParameters.ObjectRadius.Set(transformedBound.Radius);
        }
    }
}
