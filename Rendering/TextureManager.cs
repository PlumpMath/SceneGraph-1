using System.Collections.Concurrent;
using SharpDX.Direct3D11;

namespace SceneGraph.Rendering
{
    static class TextureManager
    {
        private static readonly ConcurrentDictionary<string, ShaderResourceView> Textures = new ConcurrentDictionary<string, ShaderResourceView>();

        public static ShaderResourceView Get(Device device, string path)
        {
            if (Textures.ContainsKey(path))
                return Textures[path];
            
            Textures[path] = ShaderResourceView.FromFile(device, path);

            return Textures[path];
        }
    }
}
