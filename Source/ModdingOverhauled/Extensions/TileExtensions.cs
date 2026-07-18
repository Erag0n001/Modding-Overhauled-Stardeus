using Game.Components;
using ModdingOverhauled.LayerModule.Definitions;

namespace ModdingOverhauled.Extensions;

public static class TileExtensions {
    public static LayerDef GetLayerDef(this TileTransformComp comp) {
        return LayerDef.AllByLayerId[comp.LayerId];
    }
}