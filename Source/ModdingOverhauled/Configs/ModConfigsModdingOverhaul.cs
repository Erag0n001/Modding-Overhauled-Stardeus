using ModdingOverhauled.ConfigModule.UI;

namespace ModdingOverhauled.Configs;

public class ModConfigsModdingOverhaul : ModConfigs
{
    public override void DoWindowContent()
    {
        Checkbox("Dev Shortcuts Enabled", Main.Config.DevShortcuts, ConfigHooks.OnDevShortCutChanged);
    }
}