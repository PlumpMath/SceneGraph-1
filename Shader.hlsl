float4x4 World;
float4x4 WorldViewProjection;
float4x4 TransposeInvWorld;

float3 CameraPosition;
float3 ViewVector;

float4 AmbientColor;

Texture2D Texture;
Texture2D NormalMap;

bool ObjectSelected;
float3 ObjectCenter;
float ObjectRadius;

float Time;

// Alternating Color and Direction
Buffer<float3> Lights;

cbuffer MaterialProperties
{
	float4 MaterialAmbient;
	float4 MaterialDiffuse;
	float4 MaterialSpecular;
	float  MaterialShininess;
	float3 __padding__not__data__;
}

struct VS_INPUT
{
	float3 position : POSITION;
	float3 normal   : NORMAL;
	float2 texcoord : TEXCOORD;
	float3 tangent  : TANGENT;
	float3 binormal : BINORMAL;
};

struct VS_OUTPUT
{
	float4 position : SV_POSITION;
	float4 posworld : POSITION;
	float3 normal   : NORMAL;
	float2 texcoord : TEXCOORD0;
	float3 tangent  : TEXCOORD1;
	float3 binormal : TEXCOORD2;
};

struct HS_CONSTANT_OUTPUT
{
	float edges[3] : SV_TessFactor;
	float inside   : SV_InsideTessFactor;
};

SamplerState stateAnsiotropic
{
	Filter = ANISOTROPIC;
	AddressU = Clamp;
	AddressV = Clamp;
	MaxAnisotropy = 16;
};

float TimeScale()
{
	return sin(Time * 2);
}

float4 PhongLighting(float3 normal, float3 position, float4 matAmbient, float4 matDiffuse, float4 matSpecular, float matShininess)
{
	uint numLights = 0;
	Lights.GetDimensions(numLights);

	float4 ambient = matAmbient * AmbientColor;
	for (uint i = 0; i < numLights; i += 2)
	{
		float4 lightColor = float4(Lights.Load(i), 1);
		float4 lightDirection = float4(Lights.Load(i + 1), 1);

		float4 diffuse = matDiffuse * saturate(dot(normal, -lightDirection));
		float4 specular = matSpecular * pow(saturate(dot(reflect(lightDirection, normal), normalize(CameraPosition - position))), matShininess);

		ambient += ((diffuse + specular) * lightColor);
	}

	return ambient;
}

void InterpolateSelectedObjectColors(out float4 ambient, out float4 diffuse, out float4 specular, out float shininess, float4 texVal)
{
	// Orange
	float4 ambientOrange = {1, 0.5, 0, 1};
	float4 diffuseOrange = {1, 0.5, 0, 1};
	float4 specularOrange = {0.5, 0.5, 0.5, 1};
	float shininessOrange = 64.0f;

	// Blue
	float4 ambientBlue = {0, 0.5, 1, 1};
	float4 diffuseBlue = {0, 0.5, 1, 1};
	float4 specularBlue = {1, 1, 1, 1};
	float shininessBlue = 3.0f;

	float scale = smoothstep(-1, 1, TimeScale());

	ambient = lerp(ambientOrange, ambientBlue, scale) * texVal;
	diffuse = lerp(diffuseOrange, diffuseBlue, scale);
	specular = lerp(specularOrange, specularBlue, scale);
	shininess = lerp(shininessOrange, shininessBlue, scale);
}

//----------------------------------------------------------------------------------------
// Pass-Through Vertex Shader for Tessellation
//----------------------------------------------------------------------------------------
VS_OUTPUT VS(VS_INPUT input)
{
	VS_OUTPUT output;

	output.position = float4(input.position, 1);
	output.posworld = float4(input.position, 1);
	output.normal = input.normal;
	output.texcoord = input.texcoord; 
	output.tangent = input.tangent;
	output.binormal = input.binormal; 

	return output;
}

//----------------------------------------------------------------------------------------
// Hull Shader Constant Function - None
//----------------------------------------------------------------------------------------
HS_CONSTANT_OUTPUT HSCONSTANT_NONE(InputPatch<VS_OUTPUT, 3> ip, uint pid : SV_PrimitiveID)
{
	HS_CONSTANT_OUTPUT output;

	float edge = 1;
	float inside = 1;

	output.edges[0] = edge;
	output.edges[1] = edge;
	output.edges[2] = edge;

	output.inside = inside;

	return output;
}

//----------------------------------------------------------------------------------------
// Hull Shader - None
//----------------------------------------------------------------------------------------
[domain("tri")]
[partitioning("fractional_odd")]
[outputtopology("triangle_cw")]
[outputcontrolpoints(3)]
[patchconstantfunc("HSCONSTANT_NONE")]
VS_OUTPUT HS_NONE(InputPatch<VS_OUTPUT, 3> ip, uint cpid : SV_OutputControlPointID)
{
	VS_OUTPUT output;

	output.position = ip[cpid].position; 
	output.posworld = ip[cpid].posworld; 
	output.normal = ip[cpid].normal;
	output.texcoord = ip[cpid].texcoord; 
	output.tangent = ip[cpid].tangent;
	output.binormal = ip[cpid].binormal; 

	return output;
}

//----------------------------------------------------------------------------------------
// Domain Shader - None
//----------------------------------------------------------------------------------------
[domain("tri")]
VS_OUTPUT DS_NONE(HS_CONSTANT_OUTPUT input, float3 UV : SV_DomainLocation, const OutputPatch<VS_OUTPUT, 3> patch)
{
	VS_OUTPUT output;

	float3 p = UV.x * patch[0].position.xyz + UV.y * patch[1].position.xyz + UV.z * patch[2].position.xyz;
	output.position = mul(float4(p, 1), WorldViewProjection);
	output.posworld = mul(float4(p, 1), World);
	
	output.texcoord = UV.x * patch[0].texcoord + UV.y * patch[1].texcoord + UV.z * patch[2].texcoord;

	output.normal = normalize(UV.x * patch[0].normal + UV.y * patch[1].normal + UV.z * patch[2].normal);
	output.normal = mul(float4(output.normal, 1), TransposeInvWorld);

	output.tangent = normalize(UV.x * patch[0].tangent + UV.y * patch[1].tangent + UV.z * patch[2].tangent);
	output.tangent = mul(float4(output.tangent, 1), TransposeInvWorld);

	output.binormal = normalize(UV.x * patch[0].binormal + UV.y * patch[1].binormal + UV.z * patch[2].binormal);
	output.binormal = mul(float4(output.binormal, 1), TransposeInvWorld);

	return output;
}

//----------------------------------------------------------------------------------------
// Hull Shader Constant Function - Phong
//----------------------------------------------------------------------------------------
HS_CONSTANT_OUTPUT HSCONSTANT_PHONG(InputPatch<VS_OUTPUT, 3> ip, uint pid : SV_PrimitiveID)
{
	HS_CONSTANT_OUTPUT output;

	float camDistance = max(0, distance(ObjectCenter, CameraPosition) - ObjectRadius);
	float factor = 1 - smoothstep(0, ObjectRadius * 2, camDistance);
	float scaledFactor = lerp(1, 5, factor);

	output.edges[0] = scaledFactor;
	output.edges[1] = scaledFactor;
	output.edges[2] = scaledFactor;

	output.inside = scaledFactor;

	return output;
}


//----------------------------------------------------------------------------------------
// Hull Shader - Phong
//----------------------------------------------------------------------------------------
[domain("tri")]
[partitioning("fractional_odd")]
[outputtopology("triangle_cw")]
[outputcontrolpoints(3)]
[patchconstantfunc("HSCONSTANT_PHONG")]
VS_OUTPUT HS(InputPatch<VS_OUTPUT, 3> ip, uint cpid : SV_OutputControlPointID)
{
	VS_OUTPUT output;

	output.position = ip[cpid].position; 
	output.posworld = ip[cpid].posworld; 
	output.normal = ip[cpid].normal;
	output.texcoord = ip[cpid].texcoord; 
	output.tangent = ip[cpid].tangent;
	output.binormal = ip[cpid].binormal; 

	return output;
}

float3 PhongOperator(float3 q, float3 p, float3 n)
{
	return q - dot(q - p, n) * n;
}

//----------------------------------------------------------------------------------------
// Domain Shader - Phong
//----------------------------------------------------------------------------------------
[domain("tri")]
VS_OUTPUT DS(HS_CONSTANT_OUTPUT input, float3 UV : SV_DomainLocation, const OutputPatch<VS_OUTPUT, 3> patch)
{
	VS_OUTPUT output;

	float3 p = UV.x * patch[0].position.xyz + UV.y * patch[1].position.xyz + UV.z * patch[2].position.xyz;
  
    float3 c0 = PhongOperator(p, patch[0].position.xyz, patch[0].normal); 
    float3 c1 = PhongOperator(p, patch[1].position.xyz, patch[1].normal); 
    float3 c2 = PhongOperator(p, patch[2].position.xyz, patch[2].normal);
    
    float3 q = UV.x * c0 + UV.y * c1 + UV.z * c2;
    float3 r = lerp(p, q, 0.75);

	output.position = mul(float4(r, 1), WorldViewProjection);
	output.posworld = mul(float4(r, 1), World);
	
	output.texcoord = UV.x * patch[0].texcoord + UV.y * patch[1].texcoord + UV.z * patch[2].texcoord;

	output.normal = normalize(UV.x * patch[0].normal + UV.y * patch[1].normal + UV.z * patch[2].normal);
	output.normal = mul(float4(output.normal, 1), TransposeInvWorld);

	output.tangent = normalize(UV.x * patch[0].tangent + UV.y * patch[1].tangent + UV.z * patch[2].tangent);
	output.tangent = mul(float4(output.tangent, 1), TransposeInvWorld);

	output.binormal = normalize(UV.x * patch[0].binormal + UV.y * patch[1].binormal + UV.z * patch[2].binormal);
	output.binormal = mul(float4(output.binormal, 1), TransposeInvWorld);

	return output;
}

//----------------------------------------------------------------------------------------
// Pixel Shader
//----------------------------------------------------------------------------------------
float4 PS_NoTexture(VS_OUTPUT input) : SV_Target
{
	if (ObjectSelected)
	{
		//float3 xdir = ddx(input.posworld);
		//float3 ydir = ddy(input.posworld);
		//input.normal = normalize(cross(xdir, ydir));

		float4 ambient, diffuse, specular;
		float shininess;

		InterpolateSelectedObjectColors(ambient, diffuse, specular, shininess, float4(1, 1, 1, 1));

		return PhongLighting(normalize(input.normal), input.posworld, ambient, diffuse, specular, shininess);
	}

	return PhongLighting(normalize(input.normal), input.posworld, MaterialAmbient, MaterialDiffuse, MaterialSpecular, MaterialShininess);
}

float4 PS_Texture(VS_OUTPUT input) : SV_Target
{
	float4 texVal = Texture.Sample(stateAnsiotropic, input.texcoord);
	clip(texVal.a - 0.1f);

	float3 bumpMap = NormalMap.Sample(stateAnsiotropic, input.texcoord);
	bumpMap = (bumpMap * 2.0f) - 1.0f;

	bumpMap = normalize(input.normal + bumpMap.x * input.tangent + bumpMap.y * input.binormal);

	if (ObjectSelected)
	{
		float4 ambient, diffuse, specular;
		float shininess;

		InterpolateSelectedObjectColors(ambient, diffuse, specular, shininess, texVal);

		return PhongLighting(bumpMap, input.posworld, ambient, diffuse, specular, shininess);
	}

 	return PhongLighting(bumpMap, input.posworld, MaterialAmbient, MaterialDiffuse, MaterialSpecular, MaterialShininess) * texVal;
}

technique11 RenderNoTex
{
	pass P0
	{
		SetVertexShader (CompileShader(vs_5_0, VS()));
		SetHullShader (CompileShader(hs_5_0, HS_NONE()));
		SetDomainShader (CompileShader(ds_5_0, DS_NONE()));
		SetPixelShader (CompileShader(ps_5_0, PS_NoTexture()));
	}
}

technique11 RenderTex
{
	pass P0
	{
		SetVertexShader (CompileShader(vs_5_0, VS()));
		SetHullShader (CompileShader(hs_5_0, HS_NONE()));
		SetDomainShader (CompileShader(ds_5_0, DS_NONE()));
		SetPixelShader (CompileShader(ps_5_0, PS_Texture()));
	}
}
technique11 RenderNoTexTess
{
	pass P0
	{
		SetVertexShader (CompileShader(vs_5_0, VS()));
		SetHullShader (CompileShader(hs_5_0, HS()));
		SetDomainShader (CompileShader(ds_5_0, DS()));
		SetPixelShader (CompileShader(ps_5_0, PS_NoTexture()));
	}
}

technique11 RenderTexTess
{
	pass P0
	{
		SetVertexShader (CompileShader(vs_5_0, VS()));
		SetHullShader (CompileShader(hs_5_0, HS()));
		SetDomainShader (CompileShader(ds_5_0, DS()));
		SetPixelShader (CompileShader(ps_5_0, PS_Texture()));
	}
}