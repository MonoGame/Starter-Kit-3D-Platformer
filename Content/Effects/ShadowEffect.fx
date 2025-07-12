#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// used by both shadow and shadow map
float4x4 ModelToLight;

// used by shadow map only
float4x4 ModelToView;
float3x3 NormalToView;
float4x4 ModelToScreen;

float4 Color;
float3 LightPosition;
float3 LightColor;
float SpecularIntensity; // Controls the intensity of specular highlights
float Shininess; // Controls the size/tightness of specular highlights
float AmbientIntensity; // Controls the intensity of ambient light
float EdgeFadeScale;

static const int ShadowSamples = 64;

texture ShadowMap;
sampler2D ShadowMapSampler = sampler_state
{
    Texture = (ShadowMap);
    MinFilter = point;
    MagFilter = point;
    MipFilter = point;
    AddressU = Clamp;
    AddressV = Clamp;
};

texture Texture;
sampler2D TextureSampler = sampler_state
{
    Texture = (Texture);
    Filter = ANISOTROPIC;
    MaxAnisotropy = 16;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct VSInputDepth
{
    float4 Position : POSITION0;
    float4 Normal : NORMAL0;
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
    float2 SMPosition : TEXCOORD3;
    float SMDepth : TEXCOORD4;
    float4 Color : COLOR;
};

float2 randomOffset(float4 seed)
{
    float dot_product = dot(seed, float4(12.9898, 78.233, 45.164, 94.673));
    return float2(frac(sin(dot_product) * 43758.5453), frac(sin(dot_product) * 68654.4865));
}

float4 ApplyLightingModel(V2P input, float4 color)
{
    float3 lightVector = normalize(LightPosition);
    float3 normalVector = normalize(input.ViewNormal);
    
    // Ambient colour
    float3 ambientColor = color.rgb * AmbientIntensity;
    
    // diffuse color
    float incidence = clamp(dot(normalVector, lightVector), 0.0f, 1.0f);
    float3 diffuseColor = color.rgb * LightColor * incidence;
    
    // specular color
    float3 cameraDir = normalize(-input.ViewPosition.xyz);
    float3 reflectVector = reflect(-lightVector, normalVector);
    float specularStrength = clamp(dot(cameraDir, reflectVector), 0.0f, 1.0f);
    float3 specularColor = LightColor * pow(specularStrength, Shininess) * SpecularIntensity;
    
    // shadow mappping
    float shadowScalar = 1.0f;
    
    for (int i = 0; i < ShadowSamples; i++)
    {
        float4 seed = float4(i, input.ViewPosition.xyz);
        
        float2 samplePosition = input.SMPosition + (randomOffset(seed) / 500.0f);
        
        float2 edgeDist = min(samplePosition, 1.0 - samplePosition);
        float edgeFade = saturate(min(edgeDist.x, edgeDist.y) * EdgeFadeScale); 
        
        float sampledDepth = tex2D(ShadowMapSampler, samplePosition).x;
        if (sampledDepth < input.SMDepth)
        {
            shadowScalar -= (1.0f / ShadowSamples) * edgeFade;
        }
    }
    
    return float4(ambientColor +
        shadowScalar * diffuseColor +
        shadowScalar * specularColor, color.a);
}

V2PDepth VSDepthMap(VSInputDepth input)
{
    V2PDepth output;
        
    output.Position = mul(input.Position, ModelToLight);
    output.Depth = output.Position.z / output.Position.w;
    
    return output;
};

V2P VShader(VSInput input)
{
    V2P output;
    
    output.ViewPosition = mul(input.Position, ModelToView);
    output.Position = mul(input.Position, ModelToScreen);
    output.Color = Color;
    
    float4 lightPosition = mul(input.Position, ModelToLight);
    float2 shadowMapCoord = mad(lightPosition.xy / lightPosition.w, 0.5f, float2(0.5f, 0.5f));
    shadowMapCoord.y = 1.0f - shadowMapCoord.y;
    
    output.SMPosition = shadowMapCoord;
    output.SMDepth = lightPosition.z / lightPosition.w;
    
    output.ViewNormal = mul(input.Normal, NormalToView);
    output.TextureCoords = input.TextureCoords;

    return output;
}

float4 PSDepthMap(V2PDepth input) : COLOR
{
    // Add a little bias to the final depth to avoid shadow acne.
    return float4(input.Depth + 0.001, 0, 0, 1);
}

float4 PShaderTextureColor(V2P input) : COLOR
{
    float4 diffuse = input.Color * tex2D(TextureSampler, input.TextureCoords);   
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