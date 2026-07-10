#include "Macros.hlsl"

// used by both shadow and shadow map
float4x4 ModelToLight0;
float4x4 ModelToLight1;

// used by shadow map only
float4x4 ModelToView;
float3x3 NormalToView;
float4x4 ModelToScreen;

float4 Color;
float3 LightPosition0;
float3 LightPosition1;
float3 LightColor;
float SpecularIntensity; // Controls the intensity of specular highlights
float Shininess; // Controls the size/tightness of specular highlights
float AmbientIntensity; // Controls the intensity of ambient light
float EdgeFadeScale;
float2 ShadowMask;

static const int ShadowSamples = 64;

DECLARE_TEXTURE(ShadowMap0, 0)
{
    MinFilter = linear;
    MagFilter = linear;
    MipFilter = linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

DECLARE_TEXTURE(ShadowMap1, 1)
{
    MinFilter = linear;
    MagFilter = linear;
    MipFilter = linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

DECLARE_TEXTURE(Texture, 3)
{
    Filter = ANISOTROPIC;
    MaxAnisotropy = 16;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct VSInputDepth
{
    float4 Position : POSITION0;
};

struct V2PDepth
{
    float4 Position : SV_Position;
    float Depth : TEXCOORD0;
};

struct VSInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float2 TextureCoords : TEXCOORD0;
};

struct V2P
{
    float4 Position : SV_Position;
    float2 TextureCoords : TEXCOORD0;
    float4 ViewPosition : TEXCOORD1;
    float3 ViewNormal : TEXCOORD2;
    float2 SMPosition0 : TEXCOORD3;
    float2 SMPosition1 : TEXCOORD4;
    float SMDepth0 : TEXCOORD5;
    float SMDepth1 : TEXCOORD6;
    float4 Color : Color0;
};

float2 randomOffset(float4 seed)
{
    float dot_product = dot(seed, float4(12.9898, 78.233, 45.164, 94.673));
    return float2(frac(sin(dot_product) * 43758.5453), frac(sin(dot_product) * 68654.4865));
}

float4 ApplyLightingModel(V2P input, float4 color)
{
    float3 lightVector = normalize(LightPosition0);
    float3 normalVector = normalize(input.ViewNormal);
    
    // Ambient colour
    float3 ambientColor = color.rgb * AmbientIntensity;
    float3 cameraDir = normalize(-input.ViewPosition.xyz);
    
    float ndotl = clamp(dot(normalVector, lightVector), 0, 1);
    float3 diffuseColor = color.rgb * LightColor * ndotl;
    
    float3 reflectVector = reflect(-lightVector, normalVector);
    float ndoth = clamp(dot(cameraDir, reflectVector), 0, 1);
    float3 specularColor = LightColor * pow(ndoth, Shininess) * SpecularIntensity;
   
    // This is our specialized shadow mapping that
    // generates shadows from two different directions
    // on the same light.   
    float shadowScalar = 1.0f;    
    for (int i = 0; i < ShadowSamples; i++)
    {
        float4 seed = float4(i, input.ViewPosition.xyz);
        float2 jitter = randomOffset(seed) / 500.0f;

        // First do the world shadows.
        float2 samplePosition = input.SMPosition0 + jitter;        
        float2 edgeDist = min(samplePosition, 1.0 - samplePosition);
        float edgeFade = saturate(min(edgeDist.x, edgeDist.y) * EdgeFadeScale);         
        float sampledDepth = SAMPLE_TEXTURE(ShadowMap0, samplePosition).x;
        if (sampledDepth < input.SMDepth0)
            shadowScalar -= ShadowMask.x * (1.0f / ShadowSamples) * edgeFade;
        
        // Then the placement shadows for player/enemies/coins.
        float2 samplePosition1 = input.SMPosition1 + jitter;       
        float2 edgeDist1 = min(samplePosition1, 1.0 - samplePosition1);
        float edgeFade1 = saturate(min(edgeDist1.x, edgeDist1.y) * EdgeFadeScale);       
        float sampledDepth1 = SAMPLE_TEXTURE(ShadowMap1, samplePosition1).x;
        if (sampledDepth1 < input.SMDepth1)
            shadowScalar -= ShadowMask.y * (1.0f / ShadowSamples) * edgeFade1;
    }

    return float4(
        ambientColor +
        (shadowScalar * diffuseColor) +
        (shadowScalar * specularColor),
        color.a);
}

V2PDepth VSDepthMap(VSInputDepth input)
{
    V2PDepth output;

    output.Position = mul(input.Position, ModelToLight0);
    output.Depth = output.Position.z / output.Position.w;
    
    return output;
};

V2P VShader(VSInput input)
{
    V2P output;
    
    output.ViewPosition = mul(input.Position, ModelToView);
    output.Position = mul(input.Position, ModelToScreen);
    output.Color = Color;

    {
        float4 lightPosition = mul(input.Position, ModelToLight0);
        float2 shadowMapCoord = mad(lightPosition.xy / lightPosition.w, 0.5f, float2(0.5f, 0.5f));
        shadowMapCoord.y = 1.0f - shadowMapCoord.y;
    
        output.SMPosition0 = shadowMapCoord;
        output.SMDepth0 = lightPosition.z / lightPosition.w;
    }
    
    {
        float4 lightPosition = mul(input.Position, ModelToLight1);
        float2 shadowMapCoord = mad(lightPosition.xy / lightPosition.w, 0.5f, float2(0.5f, 0.5f));
        shadowMapCoord.y = 1.0f - shadowMapCoord.y;
    
        output.SMPosition1 = shadowMapCoord;
        output.SMDepth1 = lightPosition.z / lightPosition.w;
    }
    
    output.ViewNormal = mul(input.Normal, NormalToView);
    output.TextureCoords = input.TextureCoords;

    return output;
}

float4 PSDepthMap(V2PDepth input) : SV_TARGET0
{
    // Add a little bias to the final depth to avoid shadow acne.
    return float4(input.Depth + 0.001, 0, 0, 1);
}

float4 PShaderTextureColor(V2P input) : SV_TARGET0
{
    float4 diffuse = input.Color * SAMPLE_TEXTURE(Texture, input.TextureCoords);   
    return ApplyLightingModel(input, diffuse);
}

technique RenderDepth
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL VSDepthMap();
        PixelShader = compile PS_SHADERMODEL PSDepthMap();
    }
}

technique RenderTextured
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL VShader();
        PixelShader = compile PS_SHADERMODEL PShaderTextureColor();
    }
}
