#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0
    #define PS_SHADERMODEL ps_4_0
#endif

float2 Radius = float2(0.5f, 0.5f);
float2 Center = float2(0.5f, 0.5f);
float Smoothness = 0.5f;

texture ScreenTexture;

sampler2D ScreenSampler = sampler_state
{
    Texture = <ScreenTexture>;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
    float4 Color : COLOR0;
};

float4 VignettePS(VertexShaderOutput input) : COLOR0
{
    float4 color = tex2D(ScreenSampler, input.TexCoord);
    
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