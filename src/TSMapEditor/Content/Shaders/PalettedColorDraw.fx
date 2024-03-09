#pragma enable_d3d11_debug_symbols

#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif

// The "main shader" of the editor.
// Can render objects in either paletted or RGBA mode,
// takes depth into account, and can also draw shadows.

float SpriteDepthBottom;
float SpriteDepthTop;
float2 WorldTextureCoordinates;
float2 SpriteSizeToWorldSizeRatio;
bool IsShadow;
bool UsePalette;
bool UseRemap;
float4 Lighting;

sampler2D SpriteTextureSampler : register(s0)
{
    Texture = (SpriteTexture); // this is set by spritebatch
    MipFilter = Point;
    MinFilter = Point;
    MagFilter = Point;
};

Texture2D DepthTexture;
sampler2D DepthTextureSampler
{
    Texture = <DepthTexture>; // passed in
    AddressU = clamp;
    AddressV = clamp;
    MipFilter = Point;
    MinFilter = Point;
    MagFilter = Point;
};

Texture2D PaletteTexture;
sampler2D PaletteTextureSampler
{
    Texture = <PaletteTexture>; // passed in
    AddressU = clamp;
    AddressV = clamp;
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

    if (tex.a > 0 && IsShadow)
    {
        return float4(0, 0, 0, 0.5);
    }
    
    if (UsePalette)
    {
        // Get color from palette
        float4 paletteColor = tex2D(PaletteTextureSampler, float2(tex.a, 0.5));

        // We need to convert the remap into grayscale
        if (UseRemap)
        {
            float brightness = max(paletteColor.r, max(paletteColor.g, paletteColor.b));

            // Brigthen it up a bit
            brightness = brightness * 1.25;

            float4 brightened = float4(brightness, brightness, brightness, paletteColor.a) * input.Color;

            return brightened * Lighting;
        }

        return paletteColor * Lighting * input.Color;
    }

    return tex * input.Color * Lighting;
}

technique SpriteDrawing
{
    pass P0
    {
        AlphaBlendEnable = TRUE;
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};