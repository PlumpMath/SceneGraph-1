using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SceneGraph.AssetLoading;
using SharpDX;

namespace SceneGraph
{
    static class Extensions
    {
        public static BoundingSphere Transform(this BoundingSphere sphere, Matrix transform)
        {
            var newSphere = new BoundingSphere {Center = Vector3.TransformCoordinate(sphere.Center, transform)};

            var row1 = (transform.M11 * transform.M11) + (transform.M12 * transform.M12) + (transform.M13 * transform.M13);
            var row2 = (transform.M21 * transform.M21) + (transform.M22 * transform.M22) + (transform.M23 * transform.M23);
            var row3 = (transform.M31 * transform.M31) + (transform.M32 * transform.M32) + (transform.M33 * transform.M33);
            var num = Math.Max(row1, Math.Max(row2, row3));

            newSphere.Radius = sphere.Radius * (float) Math.Sqrt(num);

            return newSphere;
        }

        public static bool GetValueOrDefault(this Dictionary<Keys, bool> dictionary, Keys key)
        {
            return dictionary.ContainsKey(key) && dictionary[key];
        }

        public static List<Face> GetValueOrDefault(this Dictionary<int, List<Face>> dictionary, int key)
        {
            if (dictionary.ContainsKey(key))
                return dictionary[key];
            return (dictionary[key] = new List<Face>());
        }
    }
}
