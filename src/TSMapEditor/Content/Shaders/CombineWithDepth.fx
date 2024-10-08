#pragma enable_d3d11_debug_symbols

#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif

// Shader for combining the terrain and object render layers,
// taking depth of each into account in separate textures.

sampler2D SpriteTextureSampler : register(s0)
{
    Texture = (SpriteTexture); // this is set by spritebatch
    AddressU = clamp;
    AddressV = clamp;
    MipFilter = Point;
    MinFilter = Point;
    MagFilter = Point;
};

Texture2D TerrainDepthTexture;
sampler2D TerrainDepthTextureSampler
{
    Texture = <TerrainDepthTexture>;
    AddressU = clamp;
    AddressV = clamp;
    MipFilter = Point;
    MinFilter = Point;
    MagFilter = Point;
};

// We use SpriteTextureSampler in place of this, because it has to be supplied anyway
// Texture2D ObjectsTexture;
// sampler2D ObjectsTextureSampler
// {
//     Texture = <ObjectsTexture>;
//     AddressU = clamp;
//     AddressV = clamp;
//     MipFilter = Point;
//     MinFilter = Point;
//     MagFilter = Point;
// };

Texture2D ObjectsDepthTexture;
sampler2D ObjectsDepthTextureSampler
{
    Texture = <ObjectsDepthTexture>;
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
    float4 objectsTex = tex2D(SpriteTextureSampler, input.TextureCoordinates);
    float terrainDepth = tex2D(TerrainDepthTextureSampler, input.TextureCoordinates).x;
    float objectsDepth = tex2D(ObjectsDepthTextureSampler, input.TextureCoordinates).x;
    
    if (objectsTex.a == 0)
    {
        discard;
    }
   
    if (objectsDepth < terrainDepth)
    {
        discard;
    }
    
    return objectsTex;
}

technique SpriteDrawing
{
    pass P0
    {
        AlphaBlendEnable = TRUE;
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};