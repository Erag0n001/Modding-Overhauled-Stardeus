using Game.Rendering;
using HarmonyLib;

namespace ModdingOverhauled.Extensions;

public static class TileRendererExtensions {
    private static readonly AccessTools.FieldRef<TileRenderer, TileLayer[]> LayersRefGetter = AccessTools.FieldRefAccess<TileRenderer, TileLayer[]>("layers");

    public static ref TileLayer[] Layers(this TileRenderer instance) {
        return ref LayersRefGetter.Invoke(instance);
    }
}