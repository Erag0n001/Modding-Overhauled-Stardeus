using Newtonsoft.Json;

namespace ModdingOverhauled.LayerModule.Definitions;

public class PathFindingInfo {
    /// <summary>
    /// Due to how the pathfinding system works internally, it's impossible to have multiple teleporters or be a wall and a door.
    /// As such, higher priority layers will be accounted for first
    /// </summary>
    public int Priority;
    [JsonProperty("LayerType")] public string LayerTypeRaw;
    [JsonIgnore] public int LayerTypeH;
    [JsonIgnore] public int LayerTypeIndex;
}