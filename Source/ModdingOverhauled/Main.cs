using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Game;
using Game.Data;
using HarmonyLib;
using ModdingOverhauled.ConfigModule;
using ModdingOverhauled.ConfigModule.Patches;
using ModdingOverhauled.Configs;
using ModdingOverhauled.Misc;
using ModdingOverhauled.UnityExplorerModule;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModdingOverhauled
{
    public static class Main
    {
        public static Harmony harmony;
        internal static ModInfo ModdingOverhauled;
        internal static ConfigDataModdingOverhauled Config;
        public static Dictionary<ModInfo, Type> ModConfigsTypes = new Dictionary<ModInfo, Type>();
        public static Dictionary<ModInfo, Type> ConfigDataTypes = new Dictionary<ModInfo, Type>();
        public static Dictionary<ModInfo, ConfigData> ConfigFromMod = new Dictionary<ModInfo, ConfigData>();
        public static Dictionary<Assembly, ModInfo> AssemblyToModInfo = new Dictionary<Assembly, ModInfo>();
        [RuntimeInitializeOnLoadMethod]
        static void StaticConstructorOnStartup()
        {
            ModdingOverhauled = The.ModLoader.ModInfos["Eragon.ModdingOverhauled"];
            LoadHarmony();
            Printer.Warn("Loaded Modding Overhaul!");
            CheckDirectories();
            SetupListeners();
            Printer.Warn($"Loaded config module!");
            LoadConfig();
            Printer.Warn($"Loaded AssetBundle module!");
        }

        static void LoadHarmony() 
        {
            harmony = new Harmony("Eragon.ModdingOverhauled");
            harmony.PatchAll();
        }

        static void CheckDirectories() 
        {
            if (!Directory.Exists(ModConfigManager.PathForModConfig))
            {
                Directory.CreateDirectory(ModConfigManager.PathForModConfig);
            }
        }

        static void SetupListeners() 
        {
            SceneManager.activeSceneChanged += ListenForSceneChange;
        }

        static void ListenForSceneChange(Scene before, Scene after) 
        {
            if (after.name == "MainMenu")
                MainMenuPatch.Patched = false;
        }

        static void LoadConfig()
        {
            ModConfigManager.GetConfigFromMod(Assembly.GetAssembly(typeof(Main)), ModdingOverhauled);
            Config = (ConfigDataModdingOverhauled)ConfigData.LoadConfig(ModdingOverhauled);
        }
    }
}
