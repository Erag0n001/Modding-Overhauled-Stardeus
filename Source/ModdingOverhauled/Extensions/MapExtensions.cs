using System;
using System.Collections.Generic;
using Game.Data;
using Game.Data.Space;
using HarmonyLib;
using ModdingOverhauled.Utils;

namespace ModdingOverhauled.Extensions;

public static class MapExtensions {
    private static readonly AccessTools.FieldRef<Map, MapData> DataRefGetter =
        AccessTools.FieldRefAccess<Map, MapData>("data");

    private static readonly Action<Map, Tile[], int> DeserializeTilesMethod =
        (Action<Map, Tile[], int>)
        ReflectionUtilities.CreateMethodCall(AccessTools.Method(typeof(Map), "DeserializeTiles"));

    private static readonly Func<Map, int, Tile[]> SavedTilesForLayerMethod =
        (Func<Map, int, Tile[]>)ReflectionUtilities.CreateMethodCall(AccessTools.Method(typeof(Map), "SavedTilesForLayer"));

    private static readonly Action<Map, List<Obj>> DeserializeObjsMethod =
        (Action<Map, List<Obj>>)
        ReflectionUtilities.CreateMethodCall(AccessTools.Method(typeof(Map), "DeserializeObjs"));

    private static readonly Action<Map, List<Being>> DeserializeBeingsMethod =
        (Action<Map, List<Being>>)
        ReflectionUtilities.CreateMethodCall(AccessTools.Method(typeof(Map), "DeserializeBeings"));

    private static readonly Action<Map> UnstuckFreeEntitiesMethod =
        (Action<Map>)
        ReflectionUtilities.CreateMethodCall(AccessTools.Method(typeof(Map), "UnstuckFreeEntities"));

    extension(Map map) {
        public ref MapData Data() => ref DataRefGetter(map);

        public void DeserializeTiles(Tile[] tiles, int layer) =>
            DeserializeTilesMethod(map, tiles, layer);

        public Tile[] SavedTilesForLayer(int layer) => SavedTilesForLayerMethod(map, layer);
        public void DeserializeObjs(List<Obj> objs) => DeserializeObjsMethod(map, objs);
        public void DeserializeBeings(List<Being> beings) => DeserializeBeingsMethod(map, beings);
        public void UnstuckFreeEntities() => UnstuckFreeEntitiesMethod(map);
    }
}