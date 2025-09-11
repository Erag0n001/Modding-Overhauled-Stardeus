using System.IO;
using System.Linq;
using Game.Platform.Steam;
using HarmonyLib;

namespace ModdingOverhauled.Misc.Patches;

public static class SteamWorkshopPatches
{
    [HarmonyPatch(typeof(SteamWorkshop), "TryRemoveSkippedFiles")]
    public static class TryRemoveSkippedFilesPath
    {
        public static void Postfix(string modTempDir)
        {
            var directory = Directory.GetDirectories(modTempDir).FirstOrDefault(x => x.EndsWith("Source"));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.Delete(directory, true);
            }
            var files =  Directory.GetFiles(modTempDir);
            foreach (var file in files)
            {
                if (file.EndsWith(".md"))
                {
                    File.Delete(file);
                }
            }
        }
    }
}