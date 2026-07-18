using System.Collections.Generic;
using MessagePack;

namespace ModdingOverhauled.LayerModule;

[MessagePackObject]
public class ExtraGrids {
    [IgnoreMember] public Dictionary<int, int> IdhToLayer = new();
}