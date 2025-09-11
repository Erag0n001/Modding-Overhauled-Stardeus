using System.IO;
using System.Runtime.InteropServices;
using Game;
using Game.Rendering;
using UnityEngine;

namespace ModdingOverhauled.AssetBundleModule
{
    public static class AssetBundleManager
    {
        private static string PlatformSuffix = GetFileSuffixForPlatform();
        public static void LoadAssetBundle(AssetBundle bundle)
        {
            AtlasInfo atlasInfo = default(AtlasInfo);
            atlasInfo.Id = "Default";
            foreach (var texture in bundle.LoadAllAssets<Texture2D>())
            {
                RenderingService.Sprites.AddRaw(atlasInfo, texture.name, texture);
            }
            foreach (var material in bundle.LoadAllAssets<Material>())
            {
                AssetCache.Materials.Add(material.name, material);
            }
            foreach (var prefab in bundle.LoadAllAssets<GameObject>())
            {
                AssetCache.Prefabs.Add(prefab.name, prefab);
            }
            foreach (var shader in bundle.LoadAllAssets<Shader>())
            {
                RenderingService.Shaders.shaders.Add(shader.name, shader);
            }
            foreach (var audioCLip in bundle.LoadAllAssets<AudioClip>())
            {
                The.Sounds.Set(audioCLip.name, audioCLip);
            }
            AssetCache.AssetBundles.Add(bundle.name, bundle);
        }

        public static void LoadAssetBundleInFolderRecursive(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }
            foreach (string file in Directory.GetFiles(path))
            {
                if(!file.EndsWith(PlatformSuffix))
                {
                    continue;
                }
                AssetBundle bundle = AssetBundle.LoadFromFile(file);
                LoadAssetBundle(bundle);
            }

            foreach (string directory in Directory.GetDirectories(path))
            {
                LoadAssetBundleInFolderRecursive(directory);
            }
        }

        private static string GetFileSuffixForPlatform()
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "_win";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "_linux";
            }
            else
            {
                return "_mac";
            }
        }
    }
}