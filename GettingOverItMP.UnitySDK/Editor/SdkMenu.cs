using System.IO;
using System.Linq;
using GettingOverItMP.UnitySDK.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GettingOverItMP.UnitySDK.Editor
{
    public static class SdkMenu
    {
        private static SdkSettingsWindow settingsWindow;

        [MenuItem("GOI-MP SDK/Export active scene as level %F6")]
        private static void ExportLevel()
        {
            string gameDirectory = EditorPrefs.GetString("GOIMP_GameDirectory");

            if (string.IsNullOrEmpty(gameDirectory))
            {
                EditorUtility.DisplayDialog("Game directory not set", "The game directory is not configured. Open the settings to configure it.", "Ok");
                return;
            }
            
            var scene = SceneManager.GetActiveScene();
            GameObject[] rootObjects = scene.GetRootGameObjects();
            var levelHost = rootObjects.FirstOrDefault(go => go.GetComponentInChildren<LevelHost>());

            if (!levelHost)
            {
                Debug.LogError("Can not export level, could not find an active LevelHost component.");
                return;
            }

            string savePath = Path.Combine(gameDirectory, "levels");

            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            var manifest = BuildPipeline.BuildAssetBundles(savePath, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);

            if (manifest == null)
            {
                Debug.LogError("Failed to export level.");
                return;
            }

            // Delete unnecessary files
            File.Delete(Path.Combine(gameDirectory, "levels/levels"));
            File.Delete(Path.Combine(gameDirectory, "levels/levels.manifest"));

            foreach (string assetBundle in manifest.GetAllAssetBundles())
            {
                File.Delete(Path.Combine(gameDirectory, $"levels/{assetBundle}.manifest"));
            }

            Debug.Log("Level exported successfully.");
        }
        
        [MenuItem("GOI-MP SDK/Settings")]
        private static void Settings()
        {
            if (!settingsWindow)
            {
                settingsWindow = ScriptableObject.CreateInstance<SdkSettingsWindow>();
            }

            settingsWindow.Show();
        }
    }
}
