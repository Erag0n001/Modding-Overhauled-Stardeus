using System.IO;
using Game.Data;
using Game.Mods;
using HarmonyLib;
using ModdingOverhauled.Misc;
using UnityEngine;

namespace ModdingOverhauled.AssetBundleModule.Patches
{
    public static class ModLoaderPatch
    {
        [HarmonyPatch(typeof(ModLoader), "LoadCodeIfSupported")]
        public static class LoadCodeIfSupportedPatch
        {
            [HarmonyPrefix]
            public static void Prefix(string mod, ModInfo info, ModLoader __instance)
            {
                string path = __instance.ModFolder(mod, "AssetBundles");
                AssetBundleManager.LoadAssetBundleInFolderRecursive(path);
            }
        }
    }
}
