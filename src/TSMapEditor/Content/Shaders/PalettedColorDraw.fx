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

float WorldTextureHeight;
bool ComplexDepth;
bool IncreaseDepthUpwards;
bool IsShadow;
bool UsePalette;
bool UseRemap;
float Opacity;

Texture2D MainTexture;
SamplerState MainSampler
{
    Texture = <MainTexture>;
    AddressU = clamp;
    AddressV = clamp;
    MipFilter = Point;
    MinFilter = Point;
    MagFilter = Point;
};

Texture2D PaletteTexture;
SamplerState PaletteSampler
{
    Texture = <PaletteTexture>;
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

    uint width;
    uint height;
    MainTexture.GetDimensions(width, height);

    // We need to read from the main texture first,
    // otherwise the output will be black!
    float4 tex = MainTexture.Sample(MainSampler, input.TextureCoordinates);

    // Discard transparent areas
    clip(tex.a == 0.0f ? -1 : 1);

    float depthMultiplier = 0.75;

    float distanceFromTop = input.TextureCoordinates.y * height;
    float distanceFromBottom = height - distanceFromTop;

    float totalDepth;

    if (IsShadow)
    {
        output.color = float4(0, 0, 0, 0.5);

        totalDepth = input.Position.z;

        output.depthTarget = totalDepth;
        output.depthEmbedded = totalDepth;
    }
    else
    {
        if (ComplexDepth)
        {
            float centralX = width * input.Color.a;
            float dx = abs((width * input.TextureCoordinates.x) - centralX);
            totalDepth = input.Position.z + (((distanceFromBottom - dx) / WorldTextureHeight) * depthMultiplier);
        }
        else if (IncreaseDepthUpwards)
        {
            totalDepth = input.Position.z + ((distanceFromBottom / WorldTextureHeight) * depthMultiplier);
        }
        else
        {
            totalDepth = input.Position.z;
        }

        output.depthTarget = totalDepth;
        output.depthEmbedded = totalDepth;

        if (UsePalette)
        {
            // Get color from palette
            float4 paletteColor = PaletteTexture.Sample(PaletteSampler, float2(tex.a, 0.5));

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