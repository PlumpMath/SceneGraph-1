using System;
using SharpDX.Direct3D11;

namespace SceneGraph.Rendering
{
    class ShaderParameters
    {
        public EffectMatrixVariable World;
        public const String WorldName = "World";

        public EffectMatrixVariable WorldViewProjection;
        public const String WorldViewProjectionName = "WorldViewProjection";

        public EffectMatrixVariable TransposeInvWorld;
        public const String TransposeInvWorldName = "TransposeInvWorld";

        public EffectShaderResourceVariable Texture;
        public const String TextureName = "Texture";

        public EffectShaderResourceVariable NormalMap;
        public const String NormalMapName = "NormalMap";

        public EffectVectorVariable CameraPosition;
        public const String CameraPositionName = "CameraPosition";

        public EffectVectorVariable ViewVector;
        public const String ViewVectorName = "ViewVector";

        public EffectShaderResourceVariable Lights;
        public const String LightsName = "Lights";

        public EffectVectorVariable AmbientColor;
        public const String AmbientColorName = "AmbientColor";

        public EffectConstantBuffer MaterialProperties;
        public const String MaterialPropertiesName = "MaterialProperties";

        public EffectScalarVariable ObjectSelected;
        public const String ObjectSelectedName = "ObjectSelected";

        public EffectVectorVariable ObjectCenter;
        public const String ObjectCenterName = "ObjectCenter";

        public EffectScalarVariable ObjectRadius;
        public const String ObjectRadiusName = "ObjectRadius";

        public EffectScalarVariable Time;
        public const String TimeName = "Time";
    }
}
