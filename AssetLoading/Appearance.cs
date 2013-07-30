using System;
using System.Collections.Generic;
using System.IO;
using SceneGraph.Rendering;
using SharpDX;
using SharpDX.Direct3D11;

namespace SceneGraph.AssetLoading
{
    class Appearance
    {
        public string Name;

        public string TextureFile;
        public ShaderResourceView Texture;

        public string NormalMapFile;
        public ShaderResourceView NormalMap;

        public MaterialProperties MaterialProperties = MaterialProperties.Default();

        public static List<Appearance> FromMtlFile(string path, Device device)
        {
            var numberRegex = MeshFactory.NumberRegex;
            var nameRegex = MeshFactory.NameRegex;

            var appearances = new List<Appearance>();

            var appearance = new Appearance();
            foreach (var line in File.ReadLines(path))
            {
                var numberMatches = numberRegex.Matches(line);
                var nameMatches = nameRegex.Matches(line);
                if (line.StartsWith("newmtl "))
                {
                    if (!String.IsNullOrEmpty(appearance.Name))
                        appearances.Add(appearance);
                    appearance = new Appearance { Name = nameMatches[0].Groups[1].Value };
                }
                else if (line.StartsWith("Ka "))
                {
                    appearance.MaterialProperties.Ambient = new Vector4 {
                            X = float.Parse(numberMatches[0].Value),
                            Y = float.Parse(numberMatches[1].Value),
                            Z = float.Parse(numberMatches[2].Value)
                        };
                }
                else if (line.StartsWith("Kd "))
                {
                    appearance.MaterialProperties.Diffuse = new Vector4 {
                        X = float.Parse(numberMatches[0].Value),
                        Y = float.Parse(numberMatches[1].Value),
                        Z = float.Parse(numberMatches[2].Value)
                    };
                }
                else if (line.StartsWith("Ks "))
                {
                    appearance.MaterialProperties.Specular = new Vector4 {
                        X = float.Parse(numberMatches[0].Value),
                        Y = float.Parse(numberMatches[1].Value),
                        Z = float.Parse(numberMatches[2].Value)
                    };
                }
                else if (line.StartsWith("Ns "))
                {
                    appearance.MaterialProperties.Shininess = float.Parse(numberMatches[0].Value);
                }
                else if (line.StartsWith("map_Kd "))
                {
                    if (nameMatches.Count > 0)
                    {
                        appearance.TextureFile = Path.Combine(Path.GetDirectoryName(path), Path.ChangeExtension(nameMatches[0].Groups[1].Value, "dds"));
                        appearance.Texture = TextureManager.Get(device, appearance.TextureFile);
                    }
                }
                else if (line.StartsWith("bump ") || line.StartsWith("map_bump "))
                {
                    if (nameMatches.Count > 0)
                    {
                        appearance.NormalMapFile = Path.Combine(Path.GetDirectoryName(path), Path.ChangeExtension(nameMatches[0].Groups[1].Value, "dds"));
                        appearance.NormalMap = TextureManager.Get(device, appearance.NormalMapFile);
                    }
                }
            }

            if (!String.IsNullOrEmpty(appearance.Name))
                appearances.Add(appearance);
            return appearances;
        }

        public Appearance Copy()
        {
            return new Appearance {
                    Name = Name,
                    MaterialProperties = MaterialProperties,
                    TextureFile = TextureFile,
                    NormalMapFile = NormalMapFile,
                    Texture = Texture,
                    NormalMap = NormalMap
                };
        }
    }

    
}
