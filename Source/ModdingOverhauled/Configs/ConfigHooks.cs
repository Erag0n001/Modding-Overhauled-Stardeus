using ModdingOverhauled.Logging;
using ModdingOverhauled.Utils;

namespace ModdingOverhauled.Configs;

public static class ConfigHooks
{
    public static void OnDevShortCutChanged(bool value)
    {
        Main.Config.DevShortcuts = value;
        Printer.Warn($"Toggled Dev Shortcuts: {value}");
        if (value)
        {
            if (MainMenuUtils.Menu)
            {
                DeveloperShortcuts.Patches.MainMenuPatches.ShowMainMenuPatch.AddButton(MainMenuUtils.Menu);
            }
        }
        else
        {
            if (MainMenuUtils.Menu)
            {
                MainMenuUtils.RemoveMainMenuButton(DeveloperShortcuts.Patches.MainMenuPatches.ShowMainMenuPatch.Button);
            }
        }
    }
}