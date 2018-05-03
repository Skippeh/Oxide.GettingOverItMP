using System.IO;
using UnityEditor;
using UnityEngine;

namespace GettingOverItMP.UnitySDK.Editor
{
    public class SdkSettingsWindow : EditorWindow
    {
        private string gameDirectory;

        private void Awake()
        {
            gameDirectory = EditorPrefs.GetString("GOIMP_GameDirectory", "");
            titleContent = new GUIContent("GOI-MP SDK Settings");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Game directory", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField(gameDirectory, EditorStyles.textField);

                if (GUILayout.Button("Browse", GUILayout.Width(75)))
                {
                    string selectedPath = EditorUtility.OpenFolderPanel("Select the Getting Over It game directory", gameDirectory, "");

                    if (string.IsNullOrEmpty(selectedPath))
                        return;
                    
                    if (!File.Exists(Path.Combine(selectedPath, "GettingOverIt.exe")))
                    {
                        EditorUtility.DisplayDialog("Invalid game directory", "Could not find GettingOverIt.exe in the selected path. Make sure you've selected the correct directory.", "Ok");
                    }
                    else
                    {
                        gameDirectory = selectedPath;
                        EditorPrefs.SetString("GOIMP_GameDirectory", Path.GetFullPath(gameDirectory));
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
