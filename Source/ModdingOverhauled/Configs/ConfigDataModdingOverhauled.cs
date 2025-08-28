using MessagePack;
using ModdingOverhauled.ConfigModule;

namespace ModdingOverhauled.Configs
{
    public class ConfigDataModdingOverhauled : ConfigData
    {
        [Key(0)] public bool IsUnityExplorerLoaded = false;
    }
}