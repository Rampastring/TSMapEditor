#pragma enable_d3d11_debug_symbols

#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif

float4 Lighting;

sampler2D SpriteTextureSampler : register(s0)
{
    Texture = (SpriteTexture); // this is set by spritebatch
    MipFilter = Point;
    MinFilter = Point;
    MagFilter = Point;
};

Texture2D PaletteTexture;
sampler2D PaletteTextureSampler
{
    Texture = <PaletteTexture>; // passed in
    AddressU = wrap;
    AddressV = wrap;
    MipFilter = Point;
    MinFilter = Point;
    MagFilter = Point;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float4 tex = tex2D(SpriteTextureSampler, input.TextureCoordinates);

    // Get color from palette
    float4 paletteColor = tex2D(PaletteTextureSampler, float2(tex.a, 0.5));

    return paletteColor * Lighting;
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};