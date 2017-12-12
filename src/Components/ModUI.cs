using System.Data;
using System.Reflection;
using Lidgren.Network;
using ServerShared;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components
{
    public class ModUI : MonoBehaviour
    {
        public GameObject LocalPlayer;

        private PlayerControl control;
        private Client client;
        private ChatUI chatUi;

        private string ipText = "";
        private string playerName = "";
        
        private static GUIStyle tintedBackground;

        private void Start()
        {
            control = LocalPlayer.GetComponent<PlayerControl>();
            client = GameObject.Find("GOIMP.Client").GetComponent<Client>();
            chatUi = gameObject.GetComponent<ChatUI>();

            playerName = PlayerPrefs.GetString("GOIMP_PlayerName", "");
        }

        private void Update()
        {
        }

        private void OnGUI()
        {
            if (tintedBackground == null)
            {
                tintedBackground = GUI.skin.GetStyle("Box");
            }

            if (!control.IsPaused() || chatUi.Writing)
                return;

            // Draw connect/disconnect area
            GUILayout.BeginArea(new Rect(10, 10, 1000, 1000));
            {
                if (client.Status == NetConnectionStatus.Disconnected)
                {
                    GUILayout.BeginHorizontal(GUILayout.Width(300));
                    {
                        GUILayout.BeginHorizontal(tintedBackground);
                        {
                            GUILayout.Label("Name", GUILayout.Width(40));
                        }
                        GUILayout.EndHorizontal();

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

                            if (string.IsNullOrEmpty(ip))
                                ip = "127.0.0.1";

                            client.Connect(ip, port, playerName);
                        }
                    }
                    GUILayout.EndHorizontal();

                    if (client.LastDisconnectReason != null)
                    {
                        GUILayout.Label("Disconnected from the server: " + client.LastDisconnectReason);
                    }
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
