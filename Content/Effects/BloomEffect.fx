#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float BloomThreshold = 0.8f;
float2 BlurAmount = float2(1.0f, 0.0f);
texture ScreenTexture;

sampler2D ScreenSampler = sampler_state
{
    Texture = <ScreenTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

float4 BloomExtractPS(VertexShaderOutput input) : COLOR0
{
    float4 color = tex2D(ScreenSampler, input.TexCoord);
    return saturate((color - BloomThreshold) / (1 - BloomThreshold));
}

float4 GaussianBlurPS(VertexShaderOutput input) : COLOR0
{
    float4 color = 0;
    float2 texCoord = input.TexCoord;
    
    // Simple 5-tap blur
    color += tex2D(ScreenSampler, texCoord - 2.0 * BlurAmount) * 0.1;
    color += tex2D(ScreenSampler, texCoord - BlurAmount) * 0.25;
    color += tex2D(ScreenSampler, texCoord) * 0.3;
    color += tex2D(ScreenSampler, texCoord + BlurAmount) * 0.25;
    color += tex2D(ScreenSampler, texCoord + 2.0 * BlurAmount) * 0.1;
    
    return color;
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