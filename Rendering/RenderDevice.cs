using System;
using System.Windows.Forms;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Windows;
using Device = SharpDX.Direct3D11.Device;
using Resource = SharpDX.Direct3D11.Resource;

namespace SceneGraph.Rendering
{
    class RenderDevice : IDisposable
    {
        public readonly Device Device;
        public readonly SwapChain SwapChain;

        public RenderTargetView RenderTargetView;
        public DepthStencilView DepthStencilView;

        public Effect ShaderEffect;
        public readonly ShaderParameters ShaderParameters = new ShaderParameters();

        public RenderDevice(RenderForm form, string shaderPath)
        {
            var swapDesc = new SwapChainDescription {
                    BufferCount = 2,
                    ModeDescription = new ModeDescription(form.ClientSize.Width, form.ClientSize.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                    IsWindowed = true,
                    OutputHandle = form.Handle,
                    SampleDescription = new SampleDescription(4, 4),
                    SwapEffect = SwapEffect.Discard,
                    Usage = Usage.RenderTargetOutput
                };
            
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, swapDesc, out Device, out SwapChain);

            var factory = SwapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);
            factory.Dispose();

            CreateRenderTargets(form.ClientSize.Width, form.ClientSize.Height);

            Device.ImmediateContext.OutputMerger.SetTargets(DepthStencilView, RenderTargetView);
            Device.ImmediateContext.Rasterizer.State = new RasterizerState(Device, 
                new RasterizerStateDescription {
                    CullMode = CullMode.Back,
                    FillMode = FillMode.Solid,
                    IsMultisampleEnabled = true
                });

            CompileShaders(shaderPath);
        }

        private void CompileShaders(string shaderPath)
        {
            ShaderEffect = new Effect(Device, ShaderBytecode.CompileFromFile(shaderPath, "fx_5_0"));

            var effectPass = ShaderEffect.GetTechniqueByIndex(0).GetPassByIndex(0);

            SetupParameterBindings();

            Device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.PatchListWith3ControlPoints;
            Device.ImmediateContext.InputAssembler.InputLayout = new InputLayout(Device, effectPass.Description.Signature, VertexShaderInput.InputLayout);
        }

        private void SetupParameterBindings()
        {
            ShaderParameters.World                  = ShaderEffect.GetVariableByName(ShaderParameters.WorldName).AsMatrix();
            ShaderParameters.WorldViewProjection    = ShaderEffect.GetVariableByName(ShaderParameters.WorldViewProjectionName).AsMatrix();
            ShaderParameters.TransposeInvWorld      = ShaderEffect.GetVariableByName(ShaderParameters.TransposeInvWorldName).AsMatrix();
            ShaderParameters.ObjectSelected         = ShaderEffect.GetVariableByName(ShaderParameters.ObjectSelectedName).AsScalar();
            ShaderParameters.ObjectCenter           = ShaderEffect.GetVariableByName(ShaderParameters.ObjectCenterName).AsVector();
            ShaderParameters.ObjectRadius           = ShaderEffect.GetVariableByName(ShaderParameters.ObjectRadiusName).AsScalar();
            ShaderParameters.Texture                = ShaderEffect.GetVariableByName(ShaderParameters.TextureName).AsShaderResource();
            ShaderParameters.NormalMap              = ShaderEffect.GetVariableByName(ShaderParameters.NormalMapName).AsShaderResource();
            ShaderParameters.CameraPosition         = ShaderEffect.GetVariableByName(ShaderParameters.CameraPositionName).AsVector();
            ShaderParameters.ViewVector             = ShaderEffect.GetVariableByName(ShaderParameters.ViewVectorName).AsVector();
            ShaderParameters.Lights                 = ShaderEffect.GetVariableByName(ShaderParameters.LightsName).AsShaderResource();
            ShaderParameters.MaterialProperties     = ShaderEffect.GetConstantBufferByName(ShaderParameters.MaterialPropertiesName).AsConstantBuffer();
            ShaderParameters.AmbientColor           = ShaderEffect.GetVariableByName(ShaderParameters.AmbientColorName).AsVector();
            ShaderParameters.Time                   = ShaderEffect.GetVariableByName(ShaderParameters.TimeName).AsScalar();
        }

        private void CreateRenderTargets(int width, int height)
        {
            var backBuffer = Resource.FromSwapChain<Texture2D>(SwapChain, 0);
            RenderTargetView = new RenderTargetView(Device, backBuffer);

            var depthDescription = new Texture2DDescription {
                ArraySize = 1,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.D32_Float,
                Height = backBuffer.Description.Height,
                Width = backBuffer.Description.Width,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = backBuffer.Description.SampleDescription,
                Usage = ResourceUsage.Default
            };

            var depthBuffer = new Texture2D(Device, depthDescription);

            var depthStencilViewDescription = new DepthStencilViewDescription {
                Format = depthDescription.Format,
                Flags = DepthStencilViewFlags.None,
                Dimension = depthDescription.SampleDescription.Count > 1 ? DepthStencilViewDimension.Texture2DMultisampled : DepthStencilViewDimension.Texture2D
            };

            DepthStencilView = new DepthStencilView(Device, depthBuffer, depthStencilViewDescription);

            Device.ImmediateContext.Rasterizer.SetViewports(new Viewport(0, 0, width, height, 0f, 1f));

            backBuffer.Dispose();
            depthBuffer.Dispose();
        }

        public void ResizeRenderTargets(int width, int height)
        {
            RenderTargetView.Dispose();
            DepthStencilView.Dispose();

            SwapChain.ResizeBuffers(2, 0, 0, Format.R8G8B8A8_UNorm, SwapChainFlags.AllowModeSwitch);
            CreateRenderTargets(width, height);
            Device.ImmediateContext.OutputMerger.SetTargets(DepthStencilView, RenderTargetView);
        }

        public void Dispose()
        {
            if (ShaderEffect != null)
                ShaderEffect.Dispose();

            if (Device != null)
                Device.Dispose();

            if (SwapChain != null)
                SwapChain.Dispose();

            if (RenderTargetView != null)
                RenderTargetView.Dispose();

            if (DepthStencilView != null)
                DepthStencilView.Dispose();
        }
    }
}
