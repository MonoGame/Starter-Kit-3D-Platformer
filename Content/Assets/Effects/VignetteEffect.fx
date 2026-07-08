#include "Macros.hlsl"

BEGIN_CONSTANTS
float2 Radius;
float2 Center;
float Smoothness;
END_CONSTANTS

DECLARE_TEXTURE(ScreenTexture, 0)
{
    MipFilter = NONE;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : Color0;
    float2 TexCoord : TEXCOORD0;
};

float4 VignettePS(VertexShaderOutput input) : SV_TARGET0
{
    float4 color = SAMPLE_TEXTURE(ScreenTexture, input.TexCoord);
    
    float2 dist = (input.TexCoord - Center) * Radius;
    float vignette = saturate(dot(dist, dist));
    vignette = smoothstep(0.0f, Smoothness, vignette);
    
    return float4(1,1,1, vignette) * input.Color;
}

technique Vignette
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL VignettePS();
    }
}
