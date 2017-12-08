using System.Reflection;
using LiteNetLib;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components
{
    public class ModUI : MonoBehaviour
    {
        public GameObject LocalPlayer;

        private PlayerControl control;
        private Client client;

        private string ipText = "";

        private static readonly FieldInfo menuPauseField = typeof(PlayerControl).GetField("menuPause", BindingFlags.NonPublic | BindingFlags.Instance);

        private void Start()
        {
            control = LocalPlayer.GetComponent<PlayerControl>();
            client = GameObject.Find("GOIMP.Client").GetComponent<Client>();
        }

        private void Update()
        {
        }

        private void OnGUI()
        {
            bool paused = (bool) menuPauseField.GetValue(control);

            if (!paused)
                return;

            // Draw ip area
            GUILayout.BeginArea(new Rect(10, 200, 1000, 1000));
            {
                if (client.State == ConnectionState.Disconnected)
                {
                    if (GUILayout.Button("Connect to public server", GUILayout.Width(100)))
                    {
                        client.Connect("127.0.0.1", 25050);
                    }

                    GUILayout.BeginHorizontal(GUILayout.Width(300));
                    {
                        ipText = GUILayout.TextField(ipText, "255.255.255.255:65535".Length, GUILayout.Width(250));

                        if (GUILayout.Button("Connect"))
                        {
                            string[] ipPort = ipText.Contains(":") ? ipText.Split(':') : new[] {ipText, "25050"};
                            string ip = ipPort[0];
                            int port = int.Parse(ipPort[1]);

                            client.Connect(ip, port);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    if (GUILayout.Button("Disconnect"))
                    {
                        client.Disconnect();
                    }
                }
            }
            GUILayout.EndArea();
        }
    }
}
