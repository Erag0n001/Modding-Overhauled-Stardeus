using System.Collections.Generic;
using Game.Data;
using Game.Data.Space;
using Game.Systems;
using Game.Utils;
using KL.Grid;
using ModdingOverhauled.LayerModule.Definitions;
using UnityEngine;

namespace ModdingOverhauled.LayerModule;

public class LayerSys : GameSystem, ISaveableSpecial{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Register()
    {
        GameSystems.Register(ID, () => new LayerSys());
    }

    public static LayerSys Instance;
    public static readonly Dictionary<int, int> OldLayerIdFor = new();
    private static ExtraGrids Data;
    public const string ID = "Eragon." + nameof(LayerSys);
    public override string Id => ID;
    protected override void OnInitialize() {
        Instance = this;
        OldLayerIdFor.Clear();
    }
    
    private static void ReconcileLayers(ExtraGrids data) {
        OldLayerIdFor.Clear();
        foreach (var def in LayerDef.AllByLayerId) {
            if (data.IdhToLayer.TryGetValue(def.IdH, out var previousLayerId)) {
                OldLayerIdFor[def.LayerId] = previousLayerId;
            }
        }
    }
    
    public override void Unload() {
        OldLayerIdFor.Clear();
        Instance = null;
    }

    public void SaveSpecial(SystemsDataSpecial sd) {
        var data = new ExtraGrids();
        data.IdhToLayer = new Dictionary<int, int>(LayerDef.All.Count);
        for (var i = 0; i < S.Map.Grids.Length; i++) {
            var def = LayerDef.AllByLayerId[i];
            data.IdhToLayer[def.IdH] = i;
        }

        sd.ModData[ID] = SaveLoadUtils.Serialize(data);
    }
    public void LoadSpecial(SystemsDataSpecial sd) {
        if (sd.ModData.TryGetValue(ID, out var raw)) {
            Data = SaveLoadUtils.Deserialize<ExtraGrids>(raw);
            ReconcileLayers(Data);
            return;
        }

        Data = new ExtraGrids();
        Data.IdhToLayer = new Dictionary<int, int>(LayerDef.All.Count);
        ReconcileLayers(Data);
    }
}