using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SceneGraph.Nodes;
using SceneGraph.Rendering;
using SharpDX;
using SharpDX.Direct3D11;

namespace SceneGraph.AssetLoading
{
    static class MeshFactory
    {
        public static readonly Regex NumberRegex = new Regex(@"-?[\d]+(?:\.[\d]*)?(e[-|+]?\d+)?", RegexOptions.IgnoreCase);
        public static readonly Regex FaceRegex = new Regex(@"(\d+)/([\d]*)/([\d]*)", RegexOptions.IgnoreCase);
        public static readonly Regex NameRegex = new Regex(@"(?:map_|bump|newmtl|usemtl)\w*\s(\w+\.?\w*)", RegexOptions.IgnoreCase);

        private static IEnumerable<Mesh> LoadModelsFromObjFile(string path, Device device)
        {
            var group = new ModelGroup();
            var model = new Model();
            var mtlFileAppearances = new List<Appearance>();
            var defaultAppaearance = new Appearance();

            var matFile = Path.ChangeExtension(path, "mtl");
            if (File.Exists(matFile))
            {
                mtlFileAppearances = Appearance.FromMtlFile(matFile, device);
            }
            else
            {
                var textureFile = Path.ChangeExtension(path, "dds");
                if (File.Exists(textureFile)) 
                {
                    defaultAppaearance.TextureFile = textureFile;
                    defaultAppaearance.Texture = TextureManager.Get(device, textureFile);
                }

                var normalFile = new String(textureFile.TakeWhile(s => s != '.').ToArray()) + "_nm.dds";
                if (File.Exists(normalFile)) 
                {
                    defaultAppaearance.NormalMapFile = normalFile;
                    defaultAppaearance.NormalMap = TextureManager.Get(device, normalFile);
                }

                model.Appearance = defaultAppaearance;
            }

            foreach (var line in File.ReadLines(path))
            {
                var matches = NumberRegex.Matches(line);
                if (line.StartsWith("v "))
                {
                    group.Vertices.Add(
                        new Vector3 {
                            X = float.Parse(matches[0].Value),
                            Y = float.Parse(matches[1].Value),
                            Z = float.Parse(matches[2].Value)
                        });
                }
                else if (line.StartsWith("vt "))
                {
                    group.TextureCoords.Add(
                       new Vector2 {
                           X = float.Parse(matches[0].Value),
                           Y = float.Parse(matches[1].Value),
                       });
                }
                else if (line.StartsWith("f "))
                {
                    matches = FaceRegex.Matches(line);

                    var facePoints = matches.Cast<Match>()
                        .Select(m => new Point {
                                PositionIndex = int.Parse(m.Groups[1].Value) - 1,
                                TextureIndex = m.Groups[2].Value == String.Empty ? -1 : int.Parse(m.Groups[2].Value) - 1,
                                Normal = Vector3.Zero,
                                Tangent = Vector3.Zero,
                                BiNormal = Vector3.Zero,
                            }).ToList();

                    for (var i = 0; i + 2 < facePoints.Count; i++)
                    {
                        var face = new Face();
                        face.Points.Add(facePoints[0]);
                        face.Points.Add(facePoints[i + 1]);
                        face.Points.Add(facePoints[i + 2]);

                        face.Normal = CalculateNormal(face, group);

                        if (face.Points.Any(p => p.TextureIndex != -1))
                            CalculateTangentBiNormal(face, group);

                        group.Neighborhood.GetValueOrDefault(facePoints[0].PositionIndex).Add(face);
                        group.Neighborhood.GetValueOrDefault(facePoints[i + 1].PositionIndex).Add(face);
                        group.Neighborhood.GetValueOrDefault(facePoints[i + 2].PositionIndex).Add(face);

                        model.Add(face);
                    }
                }
                else if (line.StartsWith("usemtl ") && File.Exists(matFile))
                {
                    var matName = NameRegex.Matches(line)[0].Groups[1].Value;
                    model.Appearance = mtlFileAppearances.FirstOrDefault(a => a.Name == matName);
                }
                else if ((line.StartsWith("o") || line.StartsWith("g")))
                {
                    var name = new String(line.Skip(1).ToArray()).Trim();

                    if (!group.Models.Any())
                        model.Name = name;

                    if (String.IsNullOrEmpty(model.Appearance.Name) && group.Models.Any())
                        model.Appearance = group.Models.Last().Appearance;
                    
                    if (group.Vertices.Any() && model.Any())
                        group.Models.Add(model);

                    model = new Model { Appearance = defaultAppaearance, Name = name };
                }
            }
            
            if (model.Any())
                group.Models.Add(model);

            return CreateMeshes(group);
        }

        private static IEnumerable<Mesh> CreateMeshes(ModelGroup group)
        {
            foreach (var model in group.Models)
            {
                SmoothNormTangBiNorm(model, group);

                var mesh = new Mesh { Appearance = model.Appearance, Name = model.Name };
                var indexLookUp = new Dictionary<Point, uint>();
                uint index = 0;

                foreach (var point in model.SelectMany(face => face.Points))
                {
                    if (indexLookUp.ContainsKey(point))
                        mesh.Indices.Add(indexLookUp[point]);
                    else
                    {
                        indexLookUp[point] = index;
                        mesh.Indices.Add(index++);

                        var vertexData = new VertexShaderInput {
                            Position = group.Vertices[point.PositionIndex],
                            Normal = point.Normal,
                            TexCoord = point.TextureIndex == -1 ? new Vector2() : group.TextureCoords[point.TextureIndex],
                            BiNormal = point.BiNormal,
                            Tangent = point.Tangent,
                        };

                        mesh.Vertices.Add(vertexData);
                    }
                }

                yield return mesh;
            }
        }

        private static void SmoothNormTangBiNorm(Model model, ModelGroup group)
        {
            var normalTangentBiNormalValues = new Dictionary<int, Tuple<Vector3, Vector3, Vector3>>();
            foreach (var position in group.Neighborhood.Keys)
            {
                var normal = Vector3.Normalize(group.Neighborhood[position].Select(f => f.Normal).Aggregate((sum, next) => sum + next));
                var tangent = Vector3.Normalize(group.Neighborhood[position].Select(f => f.Tangent).Aggregate((sum, next) => sum + next));
                var binormal = Vector3.Normalize(group.Neighborhood[position].Select(f => f.BiNormal).Aggregate((sum, next) => sum + next));

                normalTangentBiNormalValues[position] = new Tuple<Vector3, Vector3, Vector3>(normal, tangent, binormal);
            }

            foreach (var face in model)
            {
                face.Points = face.Points.Select(p => {
                    p.Normal = normalTangentBiNormalValues[p.PositionIndex].Item1;
                    p.Tangent = normalTangentBiNormalValues[p.PositionIndex].Item2;
                    p.BiNormal = normalTangentBiNormalValues[p.PositionIndex].Item3;
                    return p;
                }).ToList();
            }
        }

        private static void CalculateTangentBiNormal(Face face, ModelGroup group)
        {
            var vec1 = group.Vertices[face.Points[1].PositionIndex] - group.Vertices[face.Points[0].PositionIndex];
            var vec2 = group.Vertices[face.Points[2].PositionIndex] - group.Vertices[face.Points[0].PositionIndex];

            var texU = group.TextureCoords[face.Points[1].TextureIndex] - group.TextureCoords[face.Points[0].TextureIndex];
            var texV = group.TextureCoords[face.Points[2].TextureIndex] - group.TextureCoords[face.Points[0].TextureIndex];

            var area = texU.X * texV.Y - texU.Y * texV.X;

            var denominator = area == 0.0f ? 0f : 1f / area;

            var tangent = new Vector3 {
                    X = (texV.Y * vec1.X - texV.X * vec2.X) * denominator,
                    Y = (texV.Y * vec1.Y - texV.X * vec2.Y) * denominator,
                    Z = (texV.Y * vec1.Z - texV.X * vec2.Z) * denominator
                };

            var binormal = new Vector3 {
                    X = (texU.X * vec2.X - texU.Y * vec1.X) * denominator,
                    Y = (texU.X * vec2.Y - texU.Y * vec1.Y) * denominator,
                    Z = (texU.X * vec2.Z - texU.Y * vec1.Z) * denominator
                };

            face.Tangent = Vector3.Normalize(tangent);
            face.BiNormal = Vector3.Normalize(binormal);
        }

        private static Vector3 CalculateNormal(Face face, ModelGroup group)
        {
            var vec1 = group.Vertices[face.Points[1].PositionIndex] - group.Vertices[face.Points[0].PositionIndex];
            var vec2 = group.Vertices[face.Points[2].PositionIndex] - group.Vertices[face.Points[0].PositionIndex];

            return Vector3.Normalize(Vector3.Cross(vec1, vec2));
        }

        public static Task<List<ModelNode>> FromObjAsync(string path, Device device)
        {
            return Task.Run(() => LoadModelsFromObjFile(path, device).Select(m => new ModelNode(m)).ToList());
        }
    }
}
