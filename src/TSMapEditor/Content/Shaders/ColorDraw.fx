#pragma enable_d3d11_debug_symbols

#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif

float SpriteDepthBottom;
float SpriteDepthTop;
float2 WorldTextureCoordinates;
float2 SpriteSizeToWorldSizeRatio;
bool IsShadow;

sampler2D SpriteTextureSampler : register(s0)
{
    Texture = (SpriteTexture); // this is set by spritebatch
};

sampler DepthTextureSampler : register(s1)
{
    Texture = <DepthTexture>; // passed in
    AddressU = clamp;
    AddressV = clamp;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    // We need to read from the main texture first,
    // otherwise the output will be black!
    float4 tex = tex2D(SpriteTextureSampler, input.TextureCoordinates);

    float xRatio = SpriteSizeToWorldSizeRatio.x;
    float yRatio = SpriteSizeToWorldSizeRatio.y;

    float2 finalPosition = WorldTextureCoordinates + float2(input.TextureCoordinates.x * xRatio, input.TextureCoordinates.y * yRatio);

    float spriteDepth = SpriteDepthBottom + ((SpriteDepthTop - SpriteDepthBottom) * (1.0 - input.TextureCoordinates.y));

    float4 worldDepth = tex2D(DepthTextureSampler, finalPosition);

    // Skip if worldDepth is smaller than spriteDepth, but leave some room
    // due to float imprecision (z-fighting)
    if (worldDepth.r - spriteDepth < -0.004)
    {
        discard;
    }

    if (IsShadow)
    {
        return float4(0, 0, 0, 128);
    }

    return tex * input.Color;
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};