using System;
using System.Linq;
using SceneGraph.AssetLoading;
using SceneGraph.Rendering;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace SceneGraph.Nodes
{
    class ModelNode : GraphNode, IDisposable
    {
        private readonly Mesh _mesh;

        private Buffer _indexBuffer;
        private Buffer _vertexBuffer;
        private Buffer _materialPropertiesBuffer;

        public string Name { get { return _mesh.Name; } }
        public int FaceCount { get { return _mesh.Indices.Count / 3; } }

        public bool TexturesEnabled;
        public bool WireframeEnabled;
        public bool TessellationEnabled;

        public ModelNode(Mesh mesh)
        {
            _mesh = mesh;

            if (_mesh.Appearance == null)
                _mesh.Appearance = new Appearance();

            BoundingSphere = BoundingSphere.FromPoints(_mesh.Vertices.Select(v => v.Position).ToArray());
            //Translate(-BoundingSphere.Center.X, -BoundingSphere.Center.Y, -BoundingSphere.Center.Z);
        }

        private ModelNode(Mesh mesh, Buffer indexBuffer, Buffer vertexBuffer, Buffer materialPropertiesBuffer, BoundingSphere bounding, bool textures, bool wireframe)
        {
            _mesh = mesh.Copy();
            _indexBuffer = indexBuffer;
            _vertexBuffer = vertexBuffer;
            _materialPropertiesBuffer = materialPropertiesBuffer;
            BoundingSphere = bounding;
            TexturesEnabled = textures;
            WireframeEnabled = wireframe;
        }

        protected override void UpdateThis(GraphNode parent, RenderDevice device)
        {
            if (_indexBuffer == null || _vertexBuffer == null || _materialPropertiesBuffer == null)
                InitResources(device);

            UpdateTransforms(parent, device);

            device.Device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuffer, VertexShaderInput.SizeInBytes, 0));
            device.Device.ImmediateContext.InputAssembler.SetIndexBuffer(_indexBuffer, Format.R32_UInt, 0);

            device.ShaderParameters.MaterialProperties.SetConstantBuffer(_materialPropertiesBuffer);

            if (!TexturesEnabled || _mesh.Appearance.Texture == null || _mesh.Appearance.NormalMap == null)
            {
                if (TessellationEnabled)
                    device.ShaderEffect.GetTechniqueByName("RenderNoTexTess").GetPassByIndex(0).Apply(device.Device.ImmediateContext);
                else
                    device.ShaderEffect.GetTechniqueByName("RenderNoTex").GetPassByIndex(0).Apply(device.Device.ImmediateContext);
            }
            else
            {
                device.ShaderParameters.Texture.SetResource(_mesh.Appearance.Texture);
                device.ShaderParameters.NormalMap.SetResource(_mesh.Appearance.NormalMap);
                
                if (TessellationEnabled)
                    device.ShaderEffect.GetTechniqueByName("RenderTexTess").GetPassByIndex(0).Apply(device.Device.ImmediateContext);
                else
                    device.ShaderEffect.GetTechniqueByName("RenderTex").GetPassByIndex(0).Apply(device.Device.ImmediateContext);
            }

            ToggleWireFrame(device, true);
            device.Device.ImmediateContext.DrawIndexed(_mesh.Indices.Count, 0, 0);
            ToggleWireFrame(device, false);

            UpdateChildren(device);
        }

        private void InitResources(RenderDevice device)
        {
            _vertexBuffer = Buffer.Create(device.Device, BindFlags.VertexBuffer, _mesh.Vertices.ToArray());
            _indexBuffer = Buffer.Create(device.Device, BindFlags.IndexBuffer, _mesh.Indices.ToArray());
            _materialPropertiesBuffer = Buffer.Create(device.Device, BindFlags.ConstantBuffer, ref _mesh.Appearance.MaterialProperties);

            TexturesEnabled = _mesh.Appearance.Texture != null && _mesh.Appearance.NormalMap != null;
        }

        private void ToggleWireFrame(RenderDevice device, bool showWires)
        {
            if (!WireframeEnabled) return;

            if (showWires) 
            {
                device.Device.ImmediateContext.Rasterizer.State =
                    new RasterizerState(device.Device, new RasterizerStateDescription {
                        CullMode = CullMode.Back,
                        FillMode = FillMode.Wireframe,
                        IsMultisampleEnabled = true
                    });
            }
            else 
            {
                device.Device.ImmediateContext.Rasterizer.State = 
                    new RasterizerState(device.Device, new RasterizerStateDescription {
                        CullMode = CullMode.Back,
                        FillMode = FillMode.Solid,
                        IsMultisampleEnabled = true
                    });
            }
        }

        public override GraphNode Copy()
        {
            var newNode = new ModelNode(_mesh, _indexBuffer, _vertexBuffer, _materialPropertiesBuffer, BoundingSphere, TexturesEnabled, WireframeEnabled);

            foreach (var child in Children)
                newNode.AddChild(child.Copy());

            return newNode;
        }

        public void Dispose()
        {
            if (_indexBuffer != null)
                _indexBuffer.Dispose();

            if (_vertexBuffer != null)
                _vertexBuffer.Dispose();
        }

    }
}
