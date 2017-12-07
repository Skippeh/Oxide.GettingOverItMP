using UnityEngine;

namespace Oxide.GettingOverItMP.Components
{
    public class ModUI : MonoBehaviour
    {
        public GameObject LocalPlayer;

        private PlayerControl control;

        private string ipText = "";

        private void Start()
        {
            control = LocalPlayer.GetComponent<PlayerControl>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    control.Pause();
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    control.UnPause();
                }
            }
        }

        private void OnGUI()
        {
            // Draw ip area
            GUILayout.BeginArea(new Rect(10, 200, 1000, 1000));
            {
                GUILayout.BeginHorizontal(GUILayout.Width(300));
                {
                    ipText = GUILayout.TextField(ipText, "255.255.255.255:65535".Length, GUILayout.Width(250));

                    if (GUILayout.Button("Connect"))
                    {
                        
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }
    }
}
