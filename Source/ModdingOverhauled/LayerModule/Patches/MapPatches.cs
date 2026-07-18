using System.Collections;
using System.Collections.Generic;
using Game;
using Game.Commands;
using Game.Data;
using Game.Data.Space;
using Game.Rendering;
using Game.Systems.Atmo;
using HarmonyLib;
using KL.Grid;
using KL.Utils;
using ModdingOverhauled.Extensions;
using ModdingOverhauled.LayerModule.Definitions;
using ModdingOverhauled.Logging;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace ModdingOverhauled.LayerModule.Patches;

public static class MapPatches {
    [HarmonyPatch(typeof(Map), nameof(Map.CreateGrids))]
    public static class CreateGridsPatches {
        [HarmonyPrefix]
        public static bool Prefix(Map __instance) {
            if (__instance.Grid != null)
                // as a wise man once said
                // Fuck Off - Spajus
                return false;
            __instance.Grid = new Grid<int>("GameGrid", __instance.Width, __instance.Height, 1, Vector2.zero);
            __instance.Plan = new Grid<int>("Plan", __instance.Width, __instance.Height, 1, Vector2.zero);
            __instance.Grids = new Grid<Tile>[LayerDef.AllByLayerId.Length];
            __instance.DataGrids = new Grid<Tile>[LayerDef.AllByLayerId.Length];
            
            for (var layer = 0; layer < LayerDef.AllByLayerId.Length; ++layer)
            {
                var def = LayerDef.AllByLayerId[layer];
                __instance.Grids[layer] = new Grid<Tile>(def.Id, __instance.Width, __instance.Height, 1, def.Origin);
                __instance.Grids[layer].Layer = layer;
                __instance.Grids[layer].EnableOnChangeEvents();
                if (def.HasTileRendererLayer) {
                    __instance.RenderableGrids.Add(__instance.Grids[layer]);
                }

                if (def.IsData) {
                    __instance.DataGrids[layer] = __instance.Grids[layer];
                }
            }
            
            Shader.SetGlobalVector("_WorldSize", new Vector4(__instance.Width, __instance.Height));
            Shader.SetGlobalInteger("_GridWidth", __instance.Width);
            Shader.SetGlobalInteger("_GridHeight", __instance.Height);
            Shader.SetGlobalInteger("_GridSize", __instance.Size);
            __instance.O2 = new AtmoLayer("O2", Tunable.Float(-1222094702), __instance.Size);
            __instance.Temp = new AtmoLayer("Temp", Tunable.Float(1819189811), __instance.Size);
            return false;
        }
    }
    
    [HarmonyPatch(typeof(Map), nameof(Map.Deserialize))]
    public static class DeserializeTilesPatches {
        [HarmonyPrefix]
        public static bool Prefix(Map __instance, ref IEnumerator __result, int gameId) {
            __result = Deserialize(__instance, gameId);
            return false;
        }

        private static IEnumerator Deserialize(Map map, int gameId) {
            var data = map.Data();
            map.LastId = data.LastId;
            map.GameId = gameId;
            map.Width = data.Width;
            map.Height = data.Height;

            if (data.ComponentsData == null) {
                D.Err("Trying to deserialize with ComponentsData null! Aborting operation.");
                map.S.IsInconsistent = true;
                yield break;
            }
            map.Components.Data = data.ComponentsData;
            The.SysSig.LoadingMsg.Send("Restoring components");
            map.Components.RestoreAfterLoad(gameId);

            map.CreateGrids();

            for (var i = 0; i < LayerDef.AllByLayerId.Length; i++) {
                var def = LayerDef.AllByLayerId[i];
                var tiles = map.SavedTilesForLayer(i);
                if (tiles == null) continue;
                var componentCount = def.HasPersistentComponents
                    ? map.Components.CountInLayer(i)
                    : 0;
                The.SysSig.LoadingMsg.Send($"Loading {def.Name}'s components. Count {componentCount}");
                map.DeserializeTiles(tiles, i);
            }


            The.SysSig.LoadingMsg.Send($"Loading {data.ObjsData.Count} objects");
            map.DeserializeObjs(data.ObjsData);
            The.SysSig.LoadingMsg.Send($"Loading {data.BeingsData.Count} beings");
            map.DeserializeBeings(data.BeingsData);

            The.SysSig.LoadingMsg.Send("Populating data grids");
            yield return null;
            if (!GameState.IsCurrent(map.S)) { yield break; }
            
            for (var layer = 0; layer < LayerDef.AllByLayerId.Length; layer++) {
                var def = LayerDef.AllByLayerId[layer];

                var sourceLayerId = LayerSys.OldLayerIdFor.TryGetValue(layer, out var oldId) ? oldId : layer;
                var tiles = map.SavedTilesForLayer(sourceLayerId);
                if (tiles == null) continue;

                if (sourceLayerId != layer) {
                    Printer.Warn($"Migrating layer {sourceLayerId} to {layer}");
                    MigrateLayer(tiles, def);
                }

                var grid = map.Grids[layer];
                if (grid == null) {
                    Printer.Error($"Skipping saved tiles for layer without grid {def.Id}");
                    continue;
                }
                grid.Load(tiles);
            }
            map.Plan.Load(data.PlanData);
            map.O2.Load(data.OxygenData, data.AirtightData);
            map.Temp.Load(data.HeatData, data.InsulationData);

            if (data.Situation != null) {
                map.Situation.RestoreFrom(data.Situation);
                data.Situation = null;
            }
            map.SituationHistory = data.SituationHistory;

            map.UnstuckFreeEntities();
            map.S.Clock.OnNextUpdate(() => CheckStructuralIntegrity(map));
        }
    }

    private static void CheckStructuralIntegrity(Map map) {
        foreach (var layer in LayerDef.AllByLayerId) {
            if(layer.CanBePlacedOn.Length == 0)
                continue;
            for (var i = 0; i < map.Size; i++) {
                var existing = map.Grids[layer.LayerId].Get(i);
                if (existing == null) continue;
                
                var foundSurface = false;
                foreach (var other in layer.CanBePlacedOn) {
                    var otherSurface = map.Grids[other.LayerId].Get(i);
                    if (otherSurface != null && otherSurface.IsActive) {
                        foundSurface = true;
                        break;
                    }
                }

                if (!foundSurface ) {
                    if (existing.Definition == null) {
                        continue;
                    }
                    map.S.CmdQ.Enqueue(new CmdRemoveTile(existing));
                }
            }
        }
    }

    private static void MigrateLayer(Tile[] tiles, LayerDef def) {
        foreach (var tile in tiles) {
            if (tile == null) continue;
            foreach (var comp in tile.Components) {
                comp.LayerId = def.LayerId;
            }
        }
    }
}