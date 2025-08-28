using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Game;
using Game.UI;
using Game.Utils;
using ModdingOverhauled.Misc;
using UnityExplorer;

namespace ModdingOverhauled.UnityExplorerModule
{
    public static class UnityExplorerManager
    {
        public static void ToggleEditor(bool value)
        {
            if (value)
            {
                LoadUnityExplorer();
                UIPopupWidget.Spawn("Icons/Color/Warning", Texts.Yellow("Message"),
                    "Unity explorer started, default hotkey is F7.");
            }

            if (!value)
            {
                UIPopupWidget.Spawn("Icons/Color/Warning", Texts.Yellow("Warning"),
                    "Unity explorer requires a restart to properly turn off.");
            }
            Main.Config.IsUnityExplorerLoaded = value;
        }
        private static void LoadUnityExplorer()
        {
            ExplorerStandalone.CreateInstance();
        }
    }
}