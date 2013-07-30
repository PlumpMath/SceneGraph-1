using System;
using SharpDX;

namespace SceneGraph.Rendering
{
    static class Camera
    {
        private static Vector3 _position = new Vector3(0, 20, 20);
        public static Vector3 Position {
            get { return _position; }
            private set { _position = value; UpdateViewFrustrum(); }
        }

        private static Vector3 _look = Vector3.Normalize(Vector3.Zero - Position);
        public static Vector3 Look {
            get { return _look; }
            private set { _look = value; }
        }

        private static Vector3 _right = Vector3.Normalize(Vector3.Cross(new Vector3(0, 1, 0), _look));
        private static Vector3 _up = Vector3.Cross(_look, _right);

        private static float _fov = 60f * (float) Math.PI/180f;
        public static float Fov {
            get { return _fov; }
            set { _fov = value; UpdateViewFrustrum(); }
        }

        private static float _aspect = 1280f / 720f;
        public static float Aspect {
            get { return _aspect; } 
            set { _aspect = value; UpdateViewFrustrum(); }
        }

        private static BoundingFrustum _viewFrustrum = BoundingFrustum.FromCamera(_position, _look, _up, _fov, 0.1f, 300f, _aspect);
        public static BoundingFrustum ViewFrustrum
        {
            get { return _viewFrustrum; }
            private set { _viewFrustrum = value; } 
        }

        public static Matrix Projection()
        {
            return Matrix.PerspectiveFovLH(_fov, _aspect, 0.1f, 300f);
        }

        public static void Strafe(float distance)
        {
            Position += distance * _right;
        }

        public static void Walk(float distance)
        {
            Position += distance * _look;
        }

        public static void Pitch(float angle)
        {
            var rotationAxis = Matrix.RotationAxis(_right, angle);
            _up = Vector3.TransformNormal(_up, rotationAxis);
            _look = Vector3.TransformNormal(_look, rotationAxis);

            UpdateViewFrustrum();
        }

        public static void RotateY(float angle)
        {
            var rotationMatrix = Matrix.RotationY(angle);
            _right = Vector3.TransformNormal(_right, rotationMatrix);
            _up = Vector3.TransformNormal(_up, rotationMatrix);
            _look = Vector3.TransformNormal(_look, rotationMatrix);

            UpdateViewFrustrum();
        }

        public static void MoveUp(float distance)
        {
            _position.Y += distance;

            UpdateViewFrustrum();
        }

        public static Matrix View()
        {
            _look = Vector3.Normalize(_look);
            _up = Vector3.Normalize(Vector3.Cross(_look, _right));
            _right = Vector3.Cross(_up, _look);

            var x = -Vector3.Dot(Position, _right);
            var y = -Vector3.Dot(Position, _up);
            var z = -Vector3.Dot(Position, _look);

            var view = new Matrix {
                M11 = _right.X,
                M21 = _right.Y,
                M31 = _right.Z,
                M41 = x,

                M12 = _up.X,
                M22 = _up.Y,
                M32 = _up.Z,
                M42 = y,

                M13 = _look.X,
                M23 = _look.Y,
                M33 = _look.Z,
                M43 = z,

                M14 = 0,
                M24 = 0,
                M34 = 0,
                M44 = 1
            };

            return view;
        }

        private static void UpdateViewFrustrum()
        {
            _viewFrustrum = BoundingFrustum.FromCamera(_position, _look, _up, _fov, 0.1f, 300f, _aspect);
        }

    }
}
