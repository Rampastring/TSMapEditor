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

bool IsShadow;
bool UsePalette;
bool UseRemap;
float Opacity;

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

struct PixelShaderOutput
{
    float4 color : SV_Target0;
    float depthTarget : SV_Target1;
    float depthEmbedded : SV_Depth;
};

PixelShaderOutput MainPS(VertexShaderOutput input)
{
    PixelShaderOutput output = (PixelShaderOutput) 0;

    // We need to read from the main texture first,
    // otherwise the output will be black!
    float4 tex = tex2D(SpriteTextureSampler, input.TextureCoordinates);

    // Discard transparent areas
    clip(tex.a == 0.0f ? -1 : 1);

    // The depth value is actually passed in as the Alpha value of the input color.
    // This is because when doing batched rendering, shader parameters cannot be changed
    // within one batch, meaning we cannot introduce a shader parameter for depth
    // without sacrificing performance. And we need a unique depth value for each sprite.
    output.depthTarget = input.Color.a;
    output.depthEmbedded = input.Color.a;

    if (IsShadow)
    {
        output.color = float4(0, 0, 0, 0.5);
    }
    else
    {
        if (UsePalette)
        {
            // Get color from palette
            float4 paletteColor = tex2D(PaletteTextureSampler, float2(tex.a, 0.5));

            // We need to convert the grayscale into remap
            if (UseRemap)
            {
                float brightness = max(paletteColor.r, max(paletteColor.g, paletteColor.b));

                // Brigthen it up a bit
                brightness = brightness * 1.25;

                output.color = float4(brightness * input.Color.r, brightness * input.Color.g, brightness * input.Color.b, paletteColor.a) * Opacity;
            }
            else
            {
                // Multiply the color by 2. This is done because unlike map lighting which can exceed 1.0 and go up to 2.0,
                // the color values passed in the pixel shader input are capped at 1.0.
                // So the multiplication is done to translate pixel shader input color space into in-game color space.
                // We lose a bit of precision from doing this, but we'll have to accept that.
                output.color = float4(paletteColor.r * input.Color.r * 2.0,
                    paletteColor.g * input.Color.g * 2.0,
                    paletteColor.b * input.Color.b * 2.0,
                    paletteColor.a) * Opacity;
            }
        }
        else
        {
            output.color = float4(tex.r * input.Color.r * 2.0,
                tex.g * input.Color.g * 2.0,
                tex.b * input.Color.b * 2.0, tex.a) * Opacity;
        }
    }

    return output;
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};