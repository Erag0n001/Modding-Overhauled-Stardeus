using Game.Data;
using KL.Grid;
using KL.Utils;
using ModdingOverhauled.Extensions;
using ModdingOverhauled.LayerModule.Definitions;

namespace ModdingOverhauled.LayerModule;

public static class LayerUtils {

    public static bool HasPlacementRestrictions(int layer) {
        var layerDef = LayerDef.AllByLayerId[layer];
        return layerDef.CanBePlacedOn.Length != 0;
    }
    
    public static bool HasPlacementRestrictions(LayerDef def) {
        return def.CanBePlacedOn.Length != 0;
    }
    
    public static bool CanBeBuiltOrPlacedAt(GameState s, int posIdx, int layer) {
        return CanBeBuiltOrPlacedAt(s, posIdx, layer, out _, out _);
    }

    public static Tile GetFloorFor(GameState s, int posIdx, LayerDef layerDef) {
        foreach (var canBePlacedOnLayer in layerDef.CanBePlacedOn) {
            var existing = s.Grids[canBePlacedOnLayer.LayerId].Get(posIdx);
            if (existing != null) return existing;
        }
        return null;
    }

    public static Tile GetBuiltFloorFor(GameState s, int posIdx, LayerDef layerDef) {
        foreach (var canBePlacedOnLayer in layerDef.CanBePlacedOn) {
            var existing = s.Grids[canBePlacedOnLayer.LayerId].Get(posIdx);
            if (existing != null && existing.IsConstructed) return existing;
        }
        return null;
    }

    public static bool HasAdjacentSupport(Tile tile, int objRootPos) {
        if (tile == null || tile.Transform == null) {
            D.Err("Checking has adjacent floor for tile that is null or without transform: {0}", tile);
            return false;
        }
        if (tile.Transform.IsMulti) {
            if (tile.Transform.IsChild) {
                D.Err("Reassigning child to parent!");
                tile = tile.Transform.Parent;
            }
        }
        
        
        if (tile.Transform.HasWorkSpot) {
            var workSpot = tile.Transform.RotatedWorkSpot(objRootPos);
            var hasFloor = GetBuiltFloorFor(tile.S, workSpot, tile.Transform.GetLayerDef()) != null;
            return hasFloor;
        }
        
        Pos.ToXY(objRootPos, out var rX, out var rY);
        for (var x = -1; x <= tile.Transform.RotatedWidth; x++) {
            for (var y = -1; y <= tile.Transform.RotatedHeight; y++) {
                if (x == -1 || x == tile.Transform.RotatedWidth) {
                    if (y == -1 || y == tile.Transform.RotatedHeight) {
                        continue;
                    }

                    if (GetFloorFor(tile.S, Pos.FromXY(rX + x, rY + y), tile.Transform.GetLayerDef()) != null) {
                        return true;
                    }
                }

                if (y == -1 || y == tile.Transform.RotatedHeight) {
                    if (x == -1 || x == tile.Transform.RotatedWidth) {
                        continue;
                    }

                    if (GetFloorFor(tile.S, Pos.FromXY(rX + x, rY + y), tile.Transform.GetLayerDef()) != null) {
                        return true;
                    }
                }
            }
        }

        return false;
    }
    
    public static bool CanBeBuiltOrPlacedAt(GameState s, int podIdx, int layer, out bool floorExist,
        out bool floorBuilt) {
        var layerDef = LayerDef.AllByLayerId[layer];
        if (!HasPlacementRestrictions(layerDef)) {
            floorExist = true;
            floorBuilt = true;
            return true;
        }

        floorExist = true;
        floorBuilt = true;
        foreach (var canBePlacedOnLayer in layerDef.CanBePlacedOn) {
            var tile = s.Grids[canBePlacedOnLayer.LayerId].Get(podIdx);
            if (tile == null)
                floorExist = false;
            if (tile?.IsConstructed == false)
                floorBuilt = false;
            if (!floorExist && !floorBuilt)
                break;
        }
        return floorExist && floorBuilt;
    }
}