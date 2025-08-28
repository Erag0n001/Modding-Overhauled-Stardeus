using System.Collections.Generic;
using UnityEngine;

namespace ModdingOverhauled.AssetBundleModule
{
    public static class AssetCache
    {
        public static Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
        public static Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        public static Dictionary<string, AssetBundle> AssetBundles = new Dictionary<string, AssetBundle>();
    }
}