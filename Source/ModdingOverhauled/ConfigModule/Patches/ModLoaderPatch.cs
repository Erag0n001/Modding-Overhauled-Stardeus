using System.Reflection;
using Game.Data;
using Game.Mods;
using HarmonyLib;

namespace ModdingOverhauled.ConfigModule.Patches
{
    public static class ModLoaderPatch
    {
        [HarmonyPatch(typeof(ModLoader), "LoadDLLFile")]
        public static class LoadModPatch 
        {
            [HarmonyPrefix]
            public static void Prefix(string path, ModInfo info) 
            {
                Assembly assembly = Assembly.LoadFrom(path);
                ModConfigManager.GetConfigFromMod(assembly, info);
            }
        }
    }
}
