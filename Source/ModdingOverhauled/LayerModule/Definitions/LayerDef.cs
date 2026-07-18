using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game;
using Game.Data;
using Game.Mods;
using Game.Rendering;
using Game.Utils;
using HarmonyLib;
using KL.Utils;
using ModdingOverhauled.LayerModule.Const;
using ModdingOverhauled.Logging;
using Newtonsoft.Json;
using UnityEngine;
// ReSharper disable UnassignedField.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable ConvertToConstant.Global

namespace ModdingOverhauled.LayerModule.Definitions;

public class LayerDef {
    
    [SelfInit(SelfInit.Stage.CoreDefs)]
    private static void Init()
    {
        All.Clear();
        LoadKnown();
    }
    
    public static Dictionary<string, LayerDef> All = new();
    public static Dictionary<int, LayerDef> AllH = new();
    public static LayerDef[] AllByLayerId = [];
    public static RenderLayerDef[] RenderLayerDefs;
    public static LayerDef[][] AffectsPathFindingLayers;
    public static int[] LayersBySelectionOrder = [];
    public static LayerDef[] CanAttachSpaceDeviceLayers;
    public string Id;
    public string Name;
    // Vanilla settings
    public int SortOrder;
    public int CullingLayer;
    public bool HasGrid;
    public bool HasTileRendererLayer;
    public bool ResetFromGridData;
    public bool IsData;
    public bool IsConnected;
    public bool IsPersistentTileOnGrid;
    public bool HasPersistentComponents;
    public bool CanAttachSpaceDevice;
    public PathFindingInfo PathFindingInfo;
    public Vector2 Origin = Vector2.zero;
    [JsonProperty(nameof(BlockedBy))] private string[] BlockedByStr;
    [JsonIgnore] public LayerDef[] BlockedBy;
    [JsonProperty(nameof(CanBePlacedOn))]private string[] CanBePlacedOnRaw;
    [JsonIgnore] public LayerDef[] CanBePlacedOn;
    public string LayerAbove ;
    public string Shader;
    public int ForcedLayerId = -1;
    public int RenderQueueOverride = -1;

    public int SelectionOrder = -1;
    
    [JsonIgnore] public RenderLayerDef RenderLayerDef;
    [JsonIgnore] public ModInfo Mod;
    
    [JsonIgnore] public int IdH { get; private set; }
    [JsonIgnore] public int LayerId { get; private set; }

    private static void LoadKnown() {
        var sortedDefs = new SortedList<int, LayerDef>();
        var forcedLayers = new List<LayerDef>();
        foreach (var mod in The.ModLoader.ModInfos.Values)
        {
            var path = The.ModLoader.ModFolder(mod.Id, "Config/Layers");
            if (!Directory.Exists(path)) continue;
            
            var array = Res.ListFilesRecursive(path, ".json");
            foreach (var file in array)
            {
                var fullPath = Files.CombinePath(path, file);
                try {
                    var def = JsonConvert.DeserializeObject<LayerDef>(The.ModLoader.LoadPatchedJson(fullPath));
                    def.Mod = mod;
                    if (string.IsNullOrWhiteSpace(def.Id)) {
                        ModRegistry.MarkRuntimeBroken(def.Mod,$"LayerDef Id {path} is empty, please give your def a proper \"Id\" field!");
                        Printer.Error($"LayerDef Id {path} is empty, please give your def a proper \"Id\" field!");
                        continue;
                    }

                    def.IdH = Hashes.S(def.Id);
                    if (All.ContainsKey(def.Id)) {
                        ModRegistry.MarkRuntimeBroken(def.Mod,$"LayerDef {def.Id} already exists. Make sure your Id is unique!");
                        Printer.Error($"LayerDef {def.Id} already exists. Make sure your Id is unique!");
                        continue;
                    }

                    if (AllH.ContainsKey(def.IdH)) {
                        ModRegistry.MarkRuntimeBroken(def.Mod,$"LayerDef {def.Id} suffered a hash collision, try changing the name!");
                        Printer.Error($"LayerDef {def.Id} suffered a hash collision, try changing the name!");
                        continue;
                    }

                    All.Add(def.Id, def);
                    AllH[def.IdH] = def;
                    
                    if (def.ForcedLayerId > -1) {
                        forcedLayers.Add(def);
                    }
                    else {
                        sortedDefs[def.IdH] = def;
                    }
                }
                catch (Exception ex)
                {
                    ModRegistry.MarkRuntimeBroken(mod,$"Error while loading {nameof(LayerDef)} at: {file}\n{ex}");
                    Printer.Error($"Error while loading {nameof(LayerDef)} at: {file}\n{ex}");
                }
            }
        }
        
        AllByLayerId = new LayerDef[sortedDefs.Count + forcedLayers.Count];
        RenderLayerDefs = new RenderLayerDef[sortedDefs.Count + forcedLayers.Count];
        var usedLayers = new HashSet<int>();
        foreach (var def in forcedLayers) {
            if (def.LayerId > AllByLayerId.Length) {
                var reason = $"Tried forcing a layer id {def.ForcedLayerId} on def {def.Id}, " +
                             $"but it does not fit within the bounds of {AllByLayerId.Length}.";
                Printer.Error(reason);
                ModRegistry.MarkRuntimeBroken(def.Mod, reason);
                continue;
            }
            AllByLayerId[def.ForcedLayerId] = def;
            def.LayerId = def.ForcedLayerId;
            usedLayers.Add(def.ForcedLayerId);
        }
        var count = 0;
        foreach (var def in sortedDefs.Values) {
            while (usedLayers.Contains(count)) {
                count++;
            }
            def.LayerId = count++;
            AllByLayerId[def.LayerId] = def;
        }

        ValidateBlockedLayers();
        ValidateCanBePlacedOnLayers();
        DecideSelectionOrder();
        AssignPathfindLayers();
        
        var selectableDefsInOrder = new List<LayerDef>(AllByLayerId);
        selectableDefsInOrder.Sort((a, b) => a.SelectionOrder.CompareTo(b.SelectionOrder));
        LayersBySelectionOrder = selectableDefsInOrder.Select(x => x.LayerId).ToArray();

        CanAttachSpaceDeviceLayers = AllByLayerId.Where(layer => layer.CanAttachSpaceDevice).ToArray();
        
        AccessTools.Field(typeof(RenderLayers), "isInitialized").SetValue(null, false);
        RenderLayers.Init();
        Printer.Warn($"Loaded {All.Count} layer defs");
        Printer.Warn($"Loaded {RenderLayers.PersistentTileGridLayerIds.Length} persistent grids");
        Printer.Warn($"Loaded {RenderLayers.PersistentComponentLayerIds.Length} persistent component grids");
    }

    private static void AssignPathfindLayers() {
        List<int> layerTypeHashes = [];
        List<LayerDef> pathFindingLayers = [];
        foreach (var layer in AllByLayerId) {
            if (layer.PathFindingInfo == null) {
                continue;
            }

            if (string.IsNullOrWhiteSpace(layer.PathFindingInfo.LayerTypeRaw)) {
                Printer.Error($"Layer {layer.Id} has pathfinding info, but no LayerType property! ignoring");
                continue;
            }
            
            layer.PathFindingInfo.LayerTypeH = Hashes.S(layer.PathFindingInfo.LayerTypeRaw);
            layerTypeHashes.Add(layer.PathFindingInfo.LayerTypeH);
            
            pathFindingLayers.Add(layer);
        }
        
        layerTypeHashes.Sort();
        
        foreach (var layer in pathFindingLayers) {
            layer.PathFindingInfo.LayerTypeIndex = layerTypeHashes.IndexOf(layer.PathFindingInfo.LayerTypeH);
        }
        
        var typeCount = layerTypeHashes.Distinct().Count();
        
        var pathFindingLayerDefs = new LayerDef[typeCount][];
        for (var index = 0; index < pathFindingLayerDefs.Length; index++) {
            var layerGroup = pathFindingLayers.Where(x => x.PathFindingInfo.LayerTypeIndex == index).ToArray();
            layerGroup = layerGroup.OrderBy(def => def.PathFindingInfo.Priority).ToArray();
            pathFindingLayerDefs[index] = layerGroup;
        }

        AffectsPathFindingLayers = pathFindingLayerDefs;

        LayerConstants.AssignLayerIndexes();
        Printer.Warn($"Loaded {AffectsPathFindingLayers.Length} pathfinding layer types");
        Printer.Warn($"Loaded {AffectsPathFindingLayers.Sum(x => x.Length)} total pathfinding layers");
    }

    private static void DecideSelectionOrder() {
        foreach (var def in AllByLayerId) {
            var renderLayerDef = CreateRenderLayerDefFor(def);
            def.RenderLayerDef = renderLayerDef;
            RenderLayerDefs[def.LayerId] = renderLayerDef;
            if (def.SelectionOrder != -1 && !def.IsData) {
                Printer.Warn($"Layer def {def.Id} has a selection order but no data, so it cannot be selected.");
                def.SelectionOrder = -1;
                continue;
            }

            if (def.SelectionOrder == -1 && def.IsData) {
                def.SelectionOrder = def.LayerId;
            }
        }
    }

    private static void ValidateCanBePlacedOnLayers() {
        foreach (var layer in AllByLayerId) {
            if (layer.CanBePlacedOnRaw == null) {
                layer.CanBePlacedOn = [];
                continue;
            }   
            layer.CanBePlacedOn = new LayerDef[layer.CanBePlacedOnRaw.Length];
            var assigned = 0;
            foreach (var defId in layer.CanBePlacedOnRaw) {
                if (!All.TryGetValue(defId, out var def)) {
                    Printer.Error($"Def can be placed on layer {defId}, but it does not exist");
                    continue;
                }
                layer.CanBePlacedOn[assigned++] = def;
            }

            if (assigned != layer.CanBePlacedOn.Length) {
                Array.Resize(ref layer.CanBePlacedOn, assigned);
            }
        }
    }
    
    private static void ValidateBlockedLayers() {
        foreach (var layer in AllByLayerId) {
            if (layer.BlockedByStr == null) {
                layer.BlockedBy = [];
                continue;
            }
            layer.BlockedBy = new LayerDef[layer.BlockedByStr.Length];
            var assigned = 0;
            foreach (var blockingLayerId in layer.BlockedByStr) {
                if (!All.TryGetValue(blockingLayerId, out var def)) {
                    Printer.Error($"Def is blocked by layer {blockingLayerId}, but it does not exist");
                    continue;
                }
                layer.BlockedBy[assigned++] = def;
            }

            if (assigned != layer.BlockedBy.Length) {
                Array.Resize(ref layer.BlockedBy, assigned);
            }
        }
    }
    
    private static RenderLayerDef CreateRenderLayerDefFor(LayerDef def) {
        var layerAbove = -1;
        if (!string.IsNullOrEmpty(def.LayerAbove)) {
            foreach (var otherDef in All.Values) {
                if (otherDef.Id == def.LayerAbove) {
                    layerAbove = otherDef.LayerId;
                    break;
                }
            }
        }
        return new RenderLayerDef(def.LayerId, def.Id, def.Name, def.SortOrder, def.CullingLayer, def.HasGrid,
            def.HasTileRendererLayer, def.ResetFromGridData, def.IsData, def.IsConnected, def.IsPersistentTileOnGrid,
            def.HasPersistentComponents, layerAbove, def.Shader, def.RenderQueueOverride);
    }
}

