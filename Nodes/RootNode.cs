using System;
using System.Collections.Generic;
using System.Diagnostics;
using SceneGraph.Rendering;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace SceneGraph.Nodes
{
    class RootNode : GraphNode, IDisposable
    {
        private ShaderResourceView _lightsView;
        private Buffer _lightsBuffer;

        private readonly List<Light> _lights;
        private Color _ambientColor;

        public RootNode()
        {
            _lights = new List<Light> {
                    new Light { Color = Color.LightGoldenrodYellow.ToVector3(), Direction = Vector3.Normalize(new Vector3(1, -1, 1)) },
                    new Light { Color = Color.LightGoldenrodYellow.ToVector3(), Direction = Vector3.Normalize(new Vector3(-1, -1, -1)) }
                };

            _ambientColor = Color.White;
        }

        protected override void UpdateThis(GraphNode parent, RenderDevice device)
        {
            if (_lightsView == null)
            {
                _lightsBuffer = Buffer.Create(device.Device, BindFlags.ShaderResource, _lights.ToArray());
                _lightsView = new ShaderResourceView(device.Device, _lightsBuffer, new ShaderResourceViewDescription {
                        Format = Format.R32G32B32_Float, 
                        Dimension = ShaderResourceViewDimension.Buffer,
                        Buffer = new ShaderResourceViewDescription.BufferResource {
                                ElementCount = _lights.Count * 2,
                                ElementWidth = _lights.Count * 2,
                                ElementOffset = 0,
                                FirstElement = 0
                            }
                    });
            }

            device.ShaderParameters.Lights.SetResource(_lightsView);
            device.ShaderParameters.CameraPosition.Set(Camera.Position);
            device.ShaderParameters.ViewVector.Set(Camera.Look);
            device.ShaderParameters.AmbientColor.Set(_ambientColor.ToVector4());

            var time = (DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds / 1000;
            device.ShaderParameters.Time.Set((float) time);

            UpdateChildren(device);
        }

        public override GraphNode Copy()
        {
            throw new NotSupportedException("Cannot duplicate root node!");
        }

        public void Dispose()
        {
            if (_lightsView != null)
                _lightsView.Dispose();

            if (_lightsBuffer != null)
                _lightsBuffer.Dispose();
        }
    }
}
