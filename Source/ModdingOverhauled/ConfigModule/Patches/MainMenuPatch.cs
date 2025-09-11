using System;
using System.Collections.Generic;
using Game;
using Game.Data;
using Game.UI;
using HarmonyLib;
using ModdingOverhauled.Logging;
using ModdingOverhauled.Utils;

namespace ModdingOverhauled.ConfigModule.Patches
{
    [HarmonyPatch(typeof(MainMenu), "ShowMainMenu")]
    public static class MainMenuPatch
    {
        [HarmonyPrefix]
        public static void Prefix(MainMenu __instance)
        {
            if (Main.ModConfigsTypes.Count == 0)
            {
                Printer.Error($"Tried to add config button to main menu, but there was no config loaded");
                return;
            }

            var button = CreateConfigButton();
            MainMenuUtils.AddMainMenuButton(button, 5);
        }

        private static MainMenuButton CreateConfigButton()
        {
            var button = MainMenuUtils.CreateButton(Translations.MainMenuButton);
            foreach (KeyValuePair<ModInfo, Type> config in Main.ModConfigsTypes)
            {
                button.AddSubmenuItem(CreateConfigSubMenu(config.Key, config.Value));
            }
            return button;
        }

        private static MainMenuButton CreateConfigSubMenu(ModInfo info, Type panelType) 
        {
            Action<MainMenuButton> action = delegate { 
                CreatePanelFromConfig(panelType); 
            };
            var button = MainMenuUtils.CreateButton(info.Name, action);
            return button;
        }

        private static void CreatePanelFromConfig(Type config) 
        {
            The.SysSig.ShowPanel.Send(new PanelDescriptor(config, withCloseButton: true, skipInGame: true));
        }
    }
}
