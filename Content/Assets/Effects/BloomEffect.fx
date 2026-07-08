#include "Macros.hlsl"

float BloomThreshold;
float TexelSize;
float2 Direction;
float BloomIntensity;
float BaseIntensity;

DECLARE_TEXTURE(ScreenTexture, 0)
{
    MipFilter = NONE;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    AddressU = Clamp;
    AddressV = Clamp;
};

DECLARE_TEXTURE(BloomTexture, 1)
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

float4 BloomExtractPS(VertexShaderOutput input) : SV_TARGET0
{
    float4 color = SAMPLE_TEXTURE(ScreenTexture, input.TexCoord);
    float brightness = dot(color.rgb, float3(0.299, 0.587, 0.114));
    float bloomFactor = saturate((brightness - BloomThreshold) / 0.2);
    return color * bloomFactor;
}

float4 GaussianBlurPS(VertexShaderOutput input) : SV_TARGET0
{
    float weights[5] = { 0.227027f, 0.1945946f, 0.1216216f, 0.054054f, 0.016216f };
    float2 texCoord = input.TexCoord;
    
    float4 color = SAMPLE_TEXTURE(ScreenTexture, texCoord) * weights[0];
    
    for (int i = 1; i < 5; ++i)
    {
        float2 offset = Direction * TexelSize * i;
        color += SAMPLE_TEXTURE(ScreenTexture, texCoord + offset) * weights[i];
        color += SAMPLE_TEXTURE(ScreenTexture, texCoord - offset) * weights[i];
    }
    
    return color;    
}

float4 CombinePS(VertexShaderOutput input) : SV_TARGET0
{
    float3 baseColor = SAMPLE_TEXTURE(ScreenTexture, input.TexCoord).rgb * BaseIntensity;
    float3 bloomColor = SAMPLE_TEXTURE(BloomTexture, input.TexCoord).rgb * BloomIntensity;
    float3 hdr = baseColor + bloomColor;
    return float4(saturate(hdr), 1);
}

technique BloomExtract
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL BloomExtractPS();
    }
}

technique GaussianBlur
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL GaussianBlurPS();
    }
}

technique Combine
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL CombinePS();
    }
}