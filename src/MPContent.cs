using System;
using System.IO;
using Oxide.Core;
using UnityEngine;

namespace Oxide.GettingOverItMP
{
    public static class MPContent
    {
        public static AssetBundle Assets { get; private set; }

        public static void LoadAssetBundle()
        {
            if (Assets != null)
                return;

            try
            {
                Assets = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "goimp_content"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load assets: {ex}");
                return;
            }

            Interface.Oxide.LogDebug("Loaded assets:\n- " + string.Join("\n- ", Assets.GetAllAssetNames()));
        }
    }
}
