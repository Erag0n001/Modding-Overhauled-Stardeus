using MessagePack;
using ModdingOverhauled.ConfigModule;

namespace ModdingOverhauled.Configs;

public class ConfigDataModdingOverhaul : ConfigData
{
    [Key(0)] public bool DevShortcuts = false;
}