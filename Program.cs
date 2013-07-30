using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SceneGraph.AssetLoading;
using SceneGraph.Nodes;
using SceneGraph.Rendering;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using SharpDX.Windows;

namespace SceneGraph
{
    static class Program
    {
        private static RenderDevice _renderDevice;
        private static RenderForm _renderForm;
        private static readonly Size WindowSize = new Size(1280, 720);

        private static readonly RootNode SceneGraphRootNode = new RootNode();
        public static GraphNode SelectedGraphNode;
        private static GraphNode _parentHelperNode;

        public static double TickTime;

        private static Vector2 _lastMousePos = Vector2.Zero;
        private static bool _rotationMode;
        private static readonly Dictionary<Keys, bool> KeyPressed = new Dictionary<Keys, bool>();

        private const bool LoadOnlyLodModels = false;

        [STAThread]
        private static void Main()
        {
            Start();

            var sw = Stopwatch.StartNew();
            double frameTicks = 0;
            RenderLoop.Run(_renderForm, () => {
                _renderDevice.Device.ImmediateContext.ClearRenderTargetView(_renderDevice.RenderTargetView, SharpDX.Color.CornflowerBlue);
                _renderDevice.Device.ImmediateContext.ClearDepthStencilView(_renderDevice.DepthStencilView, DepthStencilClearFlags.Depth, 1f, 0);

                SceneGraphRootNode.UpdateWorldBoundingSphere(SceneGraphRootNode);
                SceneGraphRootNode.Update(SceneGraphRootNode, _renderDevice);

                _renderDevice.SwapChain.Present(0, PresentFlags.None);

                TickTime = (double) sw.ElapsedTicks / Stopwatch.Frequency;

                Tick();

                frameTicks += TickTime;
                if (frameTicks > 0.25)
                {
                    _renderForm.Text = String.Format("FPS: {0:n0} | Selected: {1} ({2} Faces, {3} Children) | [T]ex: {4} | W[i]re: {5} | T[e]ss: {6} | {7} - [Num5]",
                        1d / TickTime,
                        SelectedGraphNode is ModelNode ? (SelectedGraphNode as ModelNode).Name : SelectedGraphNode != null ? SelectedGraphNode.ToString() : "NONE",
                        SelectedGraphNode is ModelNode ? (SelectedGraphNode as ModelNode).FaceCount : 0,
                        SelectedGraphNode != null ? SelectedGraphNode.Children.Count : 0,
                        SelectedGraphNode is ModelNode && (SelectedGraphNode as ModelNode).TexturesEnabled ? "ON" : "OFF",
                        SelectedGraphNode is ModelNode && (SelectedGraphNode as ModelNode).WireframeEnabled ? "ON" : "OFF",
                        SelectedGraphNode is ModelNode && (SelectedGraphNode as ModelNode).TessellationEnabled ? "ON" : "OFF",
                        _rotationMode ? "Rot Mode" : "Trans Mode");
                    frameTicks = 0;
                }

                sw.Restart();
            });

        }

        private async static void Start()
        {
            _renderForm = new RenderForm("SceneGraph") { ClientSize = WindowSize };
            _renderDevice = new RenderDevice(_renderForm, "Shader.hlsl");

            _renderForm.UserResized += FormResized;
            _renderForm.FormClosing += OnExit;

            _renderForm.KeyDown += OnKeyDown;
            _renderForm.KeyUp += OnKeyUp;

            _renderForm.MouseDown += OnMouseDown;
            _renderForm.MouseMove += OnMouseMove;
            _renderForm.MouseWheel += OnMouseWheel;

            _renderForm.Show();

            await FindObjFilesRecursive("Models");
        }

        private async static Task FindObjFilesRecursive(string modelFolder)
        {
            var modelFiles = Directory.EnumerateFiles(modelFolder).Where(f => f.EndsWith(".obj")).ToList();
            if (modelFiles.Count > 1)
            {
                var lodNode = new LodNode();
                foreach (var modelFile in modelFiles)
                {
                    var models = await MeshFactory.FromObjAsync(modelFile, _renderDevice.Device);
                    var node = models.First();

                    foreach (var subModel in models.Skip(1).ToList())
                        node.AddChild(subModel);

                    lodNode.AddChild(node);
                }

                if (SelectedGraphNode == null)
                    SelectedGraphNode = lodNode;

                SceneGraphRootNode.AddChild(lodNode);
            }
            else if (modelFiles.Any() && !LoadOnlyLodModels)
            {
                var models = await MeshFactory.FromObjAsync(modelFiles.First(), _renderDevice.Device);
                var node = models.First();

                foreach (var subModel in models.Skip(1).ToList())
                    node.AddChild(subModel);

                SceneGraphRootNode.AddChild(node);

                if (SelectedGraphNode == null)
                    SelectedGraphNode = node;
            }
 
            foreach (var folder in Directory.EnumerateDirectories(modelFolder))
                Task.Run(() => FindObjFilesRecursive(folder));
        }

        private static void Tick()
        {
            var baseSpeed = 10 * (float) TickTime;
            var baseRotate = 90 * (float) Math.PI / 180f * (float) TickTime;

            if (KeyPressed.GetValueOrDefault(Keys.ShiftKey)) {
                baseSpeed *= 4;
                baseRotate *= 4;
            }

            if (KeyPressed.GetValueOrDefault(Keys.W))
                Camera.Walk(baseSpeed);

            if (KeyPressed.GetValueOrDefault(Keys.A))
                Camera.Strafe(-baseSpeed);

            if (KeyPressed.GetValueOrDefault(Keys.S))
                Camera.Walk(-baseSpeed);

            if (KeyPressed.GetValueOrDefault(Keys.D))
                Camera.Strafe(baseSpeed);

            if (KeyPressed.GetValueOrDefault(Keys.Space))
                Camera.MoveUp(baseSpeed);

            if (KeyPressed.GetValueOrDefault(Keys.ControlKey))
                Camera.MoveUp(-baseSpeed);

            if (KeyPressed.GetValueOrDefault(Keys.NumPad6))
            {
                if (_rotationMode) SelectedGraphNode.Rotate(-baseRotate, 0, 0);
                else SelectedGraphNode.Translate(-baseSpeed, 0, 0);
            }

            if (KeyPressed.GetValueOrDefault(Keys.NumPad4))
            {
                if (_rotationMode) SelectedGraphNode.Rotate(baseRotate, 0, 0);
                else SelectedGraphNode.Translate(baseSpeed, 0, 0);
            }

            if (KeyPressed.GetValueOrDefault(Keys.NumPad2))
            {
                if (_rotationMode) SelectedGraphNode.Rotate(0, 0, baseRotate);
                else SelectedGraphNode.Translate(0, 0, baseSpeed);
            }

            if (KeyPressed.GetValueOrDefault(Keys.NumPad8))
            {
                if (_rotationMode) SelectedGraphNode.Rotate(0, 0, -baseRotate);
                else SelectedGraphNode.Translate(0, 0, -baseSpeed);
            }

            if (KeyPressed.GetValueOrDefault(Keys.NumPad7))
            {
                if (_rotationMode) SelectedGraphNode.Rotate(0, -baseRotate, 0);
                else SelectedGraphNode.Translate(0, -baseSpeed, 0);
            }

            if (KeyPressed.GetValueOrDefault(Keys.NumPad9))
            {
                if (_rotationMode) SelectedGraphNode.Rotate(0, baseRotate, 0);
                else SelectedGraphNode.Translate(0, baseSpeed, 0);
            }
        }

        private static void FormResized(object sender, EventArgs e)
        {
            var form = sender as RenderForm;
            _renderDevice.ResizeRenderTargets(form.ClientSize.Width, form.ClientSize.Height);
            Camera.Aspect = (float) form.ClientSize.Width / form.ClientSize.Height;
        }

        private static void OnExit(object sender, FormClosingEventArgs e)
        {
            if (_renderDevice != null)
                _renderDevice.Dispose();
        }

        private static void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var dx = 0.25f * (e.Location.X - _lastMousePos.X);
                var dy = 0.25f * (e.Location.Y - _lastMousePos.Y);

                dx *= (float) Math.PI / 180f;
                dy *= (float) Math.PI / 180f;

                Camera.RotateY(dx);
                Camera.Pitch(dy);
            }

            if (e.Button == MouseButtons.Right)
            {
                var dx = 0.25f * (e.Location.X - _lastMousePos.X);
                var dy = 0.25f * (e.Location.Y - _lastMousePos.Y);

                dx *= (float) Math.PI / 180f;
                dy *= (float) Math.PI / 180f;

                if (_rotationMode)
                    SelectedGraphNode.Rotate(dx, dy, 0);
                else
                    SelectedGraphNode.Translate(-dx * 10, -dy * 10, 0);
            }

            _lastMousePos.X = e.Location.X;
            _lastMousePos.Y = e.Location.Y;
        }

        private static void OnMouseDown(object sender, MouseEventArgs e)
        {
            _lastMousePos.X = e.Location.X;
            _lastMousePos.Y = e.Location.Y;
        }

        private static void OnKeyUp(object sender, KeyEventArgs e)
        {
            KeyPressed[e.KeyCode] = false;
        }

        private static void OnKeyDown(object sender, KeyEventArgs e)
        {
            KeyPressed[e.KeyCode] = true;

            if (e.KeyCode == Keys.NumPad5)
                _rotationMode = !_rotationMode;

            if (e.KeyCode == Keys.T && SelectedGraphNode is ModelNode)
                (SelectedGraphNode as ModelNode).TexturesEnabled = !(SelectedGraphNode as ModelNode).TexturesEnabled;

            if (e.KeyCode == Keys.I && SelectedGraphNode is ModelNode)
                (SelectedGraphNode as ModelNode).WireframeEnabled = !(SelectedGraphNode as ModelNode).WireframeEnabled;

            if (e.KeyCode == Keys.E && SelectedGraphNode is ModelNode)
                (SelectedGraphNode as ModelNode).TessellationEnabled = !(SelectedGraphNode as ModelNode).TessellationEnabled;

            if (e.KeyCode == Keys.J)
                OnMouseWheel(sender, new MouseEventArgs(MouseButtons.None, 0, 0, 0, -1));

            if (e.KeyCode == Keys.K)
                OnMouseWheel(sender, new MouseEventArgs(MouseButtons.None, 0, 0, 0, 1));

            if (e.KeyCode == Keys.Delete) 
            {
                SceneGraphRootNode.RemoveChild(SelectedGraphNode);
                OnMouseWheel(sender, new MouseEventArgs(MouseButtons.None, 0, 0, 0, 1));
            }

            if (e.KeyCode == Keys.C && (SelectedGraphNode is ModelNode || SelectedGraphNode is LodNode))
            {
                var newNode = SelectedGraphNode.Copy();
                SceneGraphRootNode.AddChild(newNode);
                SelectedGraphNode = newNode;
            }

            if (e.KeyCode == Keys.P)
            {
                if (_parentHelperNode == null)
                {
                    _parentHelperNode = SelectedGraphNode;
                }
                else
                {
                    SceneGraphRootNode.RemoveChild(_parentHelperNode);
                    SelectedGraphNode.AddChild(_parentHelperNode);

                    _parentHelperNode = null;
                }
            }

            if (e.KeyCode == Keys.N)
            {
                var rotationNode = new RotatorAnimationNode(1, 90);
                rotationNode.AddChild(SelectedGraphNode);
                SceneGraphRootNode.ReplaceChild(SelectedGraphNode, rotationNode);
            }

            if (e.KeyCode == Keys.M)
            {
                var translationNode = new TranslationAnimationNode(5, 10);
                translationNode.AddChild(SelectedGraphNode);
                SceneGraphRootNode.ReplaceChild(SelectedGraphNode, translationNode);
            }
        }

        private static void OnMouseWheel(object sender, MouseEventArgs e)
        {
            var candidates = SceneGraphRootNode.Flatten(n => n is ModelNode || n is LodNode).ToList();

            if (!candidates.Any()) return;

            var index = candidates.FindIndex(c => c == SelectedGraphNode);

            if (e.Delta > 0)
                SelectedGraphNode = index + 1 == candidates.Count ? candidates[0] : candidates[index + 1];
            else
                SelectedGraphNode = index - 1 < 0 ? candidates[candidates.Count - 1] : candidates[index - 1];
        }
    }
}
