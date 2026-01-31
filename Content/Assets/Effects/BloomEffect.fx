#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float BloomThreshold;
float TexelSize;
float2 Direction;
float BloomIntensity;
float BaseIntensity;

texture ScreenTexture;
sampler2D ScreenSampler = sampler_state
{
    Texture = <ScreenTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

texture BloomTexture;
sampler2D BloomSampler = sampler_state
{
    Texture = <BloomTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;    
};

float4 BloomExtractPS(VertexShaderOutput input) : COLOR0
{
    float4 color = tex2D(ScreenSampler, input.TexCoord);
    float brightness = dot(color.rgb, float3(0.299, 0.587, 0.114));
    float bloomFactor = saturate((brightness - BloomThreshold) / 0.2);
    return color * bloomFactor;
}

float4 GaussianBlurPS(VertexShaderOutput input) : COLOR0
{
    float weights[5] = { 0.227027f, 0.1945946f, 0.1216216f, 0.054054f, 0.016216f };
    float2 texCoord = input.TexCoord;
    
    float4 color = tex2D(ScreenSampler, texCoord) * weights[0];
    
    for (int i = 1; i < 5; ++i)
    {
        float2 offset = Direction * TexelSize * i;
        color += tex2D(ScreenSampler, texCoord + offset) * weights[i];
        color += tex2D(ScreenSampler, texCoord - offset) * weights[i];
    }
    
    return color;    
}

float4 CombinePS(VertexShaderOutput input) : COLOR0
{
    float3 baseColor = tex2D(ScreenSampler, input.TexCoord).rgb * BaseIntensity;
    float3 bloomColor = tex2D(BloomSampler, input.TexCoord).rgb * BloomIntensity;
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