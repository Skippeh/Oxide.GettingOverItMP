using System.Diagnostics;
using System.IO;
using Oxide.Core;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components
{
    public class MenuUI : MonoBehaviour
    {
        private enum MenuState
        {
            None,
            FirstLaunch,
            UpdateAvailable
        }

        private MenuState state;

        public void ShowUpdateAvailable()
        {
            state = MenuState.UpdateAvailable;
        }

        public void ShowFirstLaunch()
        {
            state = MenuState.FirstLaunch;
        }

        private void OnGUI()
        {
            if (state == MenuState.None)
                return;

            Rect windowRect = new Rect(0, 0, 400, 100);
            windowRect.center = new Vector2(Screen.width / 2f, Screen.height / 2f);

            string title = state == MenuState.UpdateAvailable ? "New update available" : "First time setup";
            
            switch (state)
            {
                case MenuState.FirstLaunch:
                    GUI.ModalWindow(1, windowRect, DrawFirstLaunch, title);
                    break;
                case MenuState.UpdateAvailable:
                    GUI.ModalWindow(1, windowRect, DrawUpdateAvailable, title);
                    break;
            }
        }

        private void DrawUpdateAvailable(int windowId)
        {
            GUILayout.Label("There is an update available. Do you want to download it now?");
            GUILayout.BeginHorizontal(GUILayout.Width(150));
            {
                if (GUILayout.Button("Yes"))
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "GettingOverItMP.Updater.exe");
                    Process.Start(filePath, "client launch");
                    Application.Quit();
                }

                if (GUILayout.Button("No"))
                {
                    state = MenuState.None;
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawFirstLaunch(int windowId)
        {
            GUILayout.Label("Do you want to enable automatically checking for updates for 'Getting Over It Multiplayer' when the game launches?");
            GUILayout.BeginHorizontal(GUILayout.Width(150));
            {
                if (GUILayout.Button("Yes"))
                {
                    PlayerPrefs.SetInt("GOIMP_CheckForUpdates", 1);
                    state = MenuState.None;
                }

                if (GUILayout.Button("No"))
                {
                    PlayerPrefs.SetInt("GOIMP_CheckForUpdates", 0);
                    state = MenuState.None;
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}
