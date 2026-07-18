using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Game;
using Game.Data;
using HarmonyLib;
using ModdingOverhauled.ConfigModule;
using ModdingOverhauled.Logging;
using UnityEngine;

namespace ModdingOverhauled
{
    public static class Main
    {
        internal static Harmony Harmony;
        internal static ModInfo ModdingOverhauled;
        internal static readonly Dictionary<ModInfo, Type> ModConfigsTypes = new Dictionary<ModInfo, Type>();
        internal static readonly Dictionary<ModInfo, Type> ConfigDataTypes = new Dictionary<ModInfo, Type>();
        internal static readonly Dictionary<ModInfo, ConfigData> ConfigFromMod = new Dictionary<ModInfo, ConfigData>();
        internal static Dictionary<Assembly, ModInfo> AssemblyToModInfo = new Dictionary<Assembly, ModInfo>();
        
        [RuntimeInitializeOnLoadMethod]
        static void StaticConstructorOnStartup() {
            ModdingOverhauled = The.ModLoader.ModInfos.FirstOrDefault(x => x.Key.Contains("Eragon.ModdingOverhauled")).Value;
            LoadHarmony();
            Printer.Warn("Loaded Modding Overhaul!");
            CheckDirectories();
            Printer.Warn($"Loaded config module!");
            Printer.Warn($"Loaded AssetBundle module!");
            ModConfigManager.GetConfigFromMod(Assembly.GetExecutingAssembly(), ModdingOverhauled);
        }

        static void LoadHarmony() 
        {
            Harmony = new Harmony("Eragon.ModdingOverhauled");
            Harmony.PatchAll();
        }

        static void CheckDirectories() 
        {
            if (!Directory.Exists(ModConfigManager.PathForModConfig))
            {
                Directory.CreateDirectory(ModConfigManager.PathForModConfig);
            }
        }
    }
}
