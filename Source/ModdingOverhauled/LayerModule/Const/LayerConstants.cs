using Game.Constants;
using KL.Utils;
using ModdingOverhauled.LayerModule.Definitions;

namespace ModdingOverhauled.LayerModule.Const;

public static class LayerConstants {
    public static readonly int FloorHash = Hashes.S("Floor");
    public static readonly int WallHash = Hashes.S("Walls");
    public static readonly int ObjHash = Hashes.S("Objects");
    public static int FloorPathfindingIndex { get; private set; }
    public static int WallPathfindingIndex { get; private set; }
    public static int ObjPathfindingIndex { get; private set; }
    internal static void AssignLayerIndexes() {
        var floors = LayerDef.AllByLayerId[WorldLayer.Floor];
        var walls = LayerDef.AllByLayerId[WorldLayer.Walls];
        var objs = LayerDef.AllByLayerId[WorldLayer.Objects];
        FloorPathfindingIndex = floors.PathFindingInfo.LayerTypeIndex;
        WallPathfindingIndex = walls.PathFindingInfo.LayerTypeIndex;
        ObjPathfindingIndex = objs.PathFindingInfo.LayerTypeIndex;
    }
}