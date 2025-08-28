using System;
using UnityEditor;
using System.IO;
using UnityEngine;

public class CreateAssetBundles
{
    [MenuItem("Assets/Build AssetBundles for All Platforms")]
    static void BuildAllPlatforms()
    {
        string baseOutput = "Assets/AssetBundles";

        // Define target platforms
        BuildTarget[] platforms = new BuildTarget[]
        {
            BuildTarget.StandaloneWindows64,
            BuildTarget.StandaloneOSX,
            BuildTarget.StandaloneLinux64
        };
        foreach (var file in Directory.GetFiles(baseOutput))
        {
            File.Delete(file);
        }
        foreach (var platform in platforms)
        {
            string outputPath = Path.Combine(baseOutput, platform.ToString());
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            // Build AssetBundles for this platform
            BuildPipeline.BuildAssetBundles(outputPath,
                BuildAssetBundleOptions.None,
                platform);
            foreach (var file in Directory.GetFiles(outputPath))
            {
                if(Path.HasExtension(file))
                    continue;
                Debug.Log(Path.GetFileNameWithoutExtension(file));
				if(Path.GetFileNameWithoutExtension(file).Contains("Standalone"))
                    continue;
                File.Move(file, Path.Combine(baseOutput, Path.GetFileName(file) + GetSuffixFromPlatform(platform)));
                File.Delete(file + ".meta");
            }
            Debug.Log($"Built AssetBundles for {platform} at {outputPath}");
        }
    }

    private static string GetSuffixFromPlatform(BuildTarget platform)
    {
        switch (platform)
        {
            case BuildTarget.StandaloneWindows64: return "_win";
            case BuildTarget.StandaloneOSX: return "_mac";
            case BuildTarget.StandaloneLinux64: return "_linux";
            default: throw new NotImplementedException();
        }
    }
}