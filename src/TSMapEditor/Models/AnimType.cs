using TSMapEditor.Models.ArtConfig;

namespace TSMapEditor.Models
{
    public class AnimType : GameObjectType, IArtConfigContainer
    {
        public AnimType(string iniName) : base(iniName) { }

        public override RTTIType WhatAmI() => RTTIType.AnimType;

        public AnimArtConfig ArtConfig { get; set; } = new AnimArtConfig();
        public IArtConfig GetArtConfig() => ArtConfig;

        public bool RenderInEditor { get; set; } = true;
    }
}
