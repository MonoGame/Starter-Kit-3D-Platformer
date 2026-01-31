#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float2 Radius = float2(0.5f, 0.5f);
float2 Center = float2(0.5f, 0.5f);
float Smoothness = 0.5f;

Texture2D SpriteTexture;

sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 VignettePS(VertexShaderOutput input) : COLOR0
{
    float4 color = tex2D(SpriteTextureSampler, input.TextureCoordinates) * input.Color;
    
    float2 dist = (input.TextureCoordinates - Center) * Radius;
    float vignette = saturate(dot(dist, dist));
    vignette = smoothstep(0.0f, Smoothness, vignette);
    
    return float4(1,1,1, vignette) * color;
}

technique Vignette
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL VignettePS();
    }
}