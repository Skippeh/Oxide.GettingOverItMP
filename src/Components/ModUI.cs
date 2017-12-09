using System.Reflection;
using LiteNetLib;
using ServerShared;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components
{
    public class ModUI : MonoBehaviour
    {
        public GameObject LocalPlayer;

        private PlayerControl control;
        private Client client;

        private string ipText = "";
        private string playerName = "";

        private static readonly FieldInfo menuPauseField = typeof(PlayerControl).GetField("menuPause", BindingFlags.NonPublic | BindingFlags.Instance);

        private void Start()
        {
            control = LocalPlayer.GetComponent<PlayerControl>();
            client = GameObject.Find("GOIMP.Client").GetComponent<Client>();

            playerName = PlayerPrefs.GetString("GOIMP_PlayerName", "");
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
            GUILayout.BeginArea(new Rect(10, 10, 1000, 1000));
            {
                if (client.State == ConnectionState.Disconnected)
                {
                    GUILayout.BeginHorizontal(GUILayout.Width(300));
                    {
                        GUILayout.Label("Name", GUILayout.Width(40));
                        playerName = GUILayout.TextField(playerName, SharedConstants.MaxNameLength, GUILayout.Width(200));
                    }
                    GUILayout.EndHorizontal();

                    if (GUILayout.Button("Connect to public server", GUILayout.Width(200)))
                    {
                        SavePlayerName();
                        client.Connect(SharedConstants.PublicServerHost, SharedConstants.PublicServerPort, playerName);
                    }

                    GUILayout.BeginHorizontal(GUILayout.Width(300));
                    {
                        ipText = GUILayout.TextField(ipText, "255.255.255.255:65535".Length, GUILayout.Width(250));

                        if (GUILayout.Button("Connect"))
                        {
                            SavePlayerName();
                            string[] ipPort = ipText.Replace(" ", "").Contains(":") ? ipText.Split(':') : new[] {ipText, SharedConstants.DefaultPort.ToString()};
                            string ip = ipPort[0];
                            int port = int.Parse(ipPort[1]);

                            client.Connect(ip, port, playerName);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    if (GUILayout.Button("Disconnect", GUILayout.Width(80)))
                    {
                        client.Disconnect();
                    }
                }
            }
            GUILayout.EndArea();
        }

        private void SavePlayerName()
        {
            PlayerPrefs.SetString("GOIMP_PlayerName", playerName);
        }
    }
}
