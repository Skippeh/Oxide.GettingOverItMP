using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BuildAssetBundle : MonoBehaviour
{
    [MenuItem("Assets/Build AssetBundles")]
    static void BuildAllAssetBundles()
    {
        string assetBundleDirectory = Path.Combine(Application.dataPath, @"AssetBundles");

        foreach (string filePath in Directory.GetFiles(assetBundleDirectory))
        {
            try
            {
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                Debug.Log("Failed to delete file: '" + Path.GetFileName(filePath) + "': " + ex.Message);
            }
        }

        Debug.Log("Building asset bundles to " + assetBundleDirectory);

        if (!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }

        BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
    }
}
