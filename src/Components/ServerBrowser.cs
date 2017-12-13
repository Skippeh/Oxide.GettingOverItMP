using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.GettingOverIt;
using ServerShared.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace Oxide.GettingOverItMP.Components
{
    public class ServerBrowser : MonoBehaviour
    {
        public bool Open { get => _open; private set => SetOpen(value); }
        private bool _open;

        private static readonly Vector2 windowSize = new Vector2(600, 400);
        private Rect windowRect;
        private Vector2 scrollPosition;
        private PlayerControl control;
        private ChatUI chatUi;
        private GameObject ingameMenu;

        private readonly List<ServerInfo> servers = new List<ServerInfo>()
        {
            new ServerInfo
            {
                Name = "Test server with a long name boi",
                Ip = "127.0.0.1",
                Players = 10,
                MaxPlayers = 100,
                Ping = 43
            }
        };

        private ServerInfo selectedServer;

        private GUIStyle rowStyle;
        private GUIStyle backgroundStyle;

        private void Start()
        {
            windowRect = new Rect(Screen.width / 2f - windowSize.x / 2f, Screen.height / 2f - windowSize.y / 2f, windowSize.x, windowSize.y);

            control = GameObject.Find("Player").GetComponent<PlayerControl>();
            chatUi = GetComponent<ChatUI>();

            var canvas = GameObject.Find("Canvas");
            ingameMenu = canvas.transform.Find("InGame Menu").gameObject ?? throw new NotImplementedException("Could not find Ingame Menu");
        }

        private void OnGUI()
        {
            if (rowStyle == null)
            {
                rowStyle = new GUIStyle();
                rowStyle.normal.background = Texture2D.whiteTexture;
                rowStyle.padding.left = 10;

                backgroundStyle = new GUIStyle();
                backgroundStyle.normal.background = Texture2D.whiteTexture;
            }
            
            if (!control.IsPaused() || chatUi.Writing)
                return;

            if (Open)
            {
                // Draw background
                Color oldBackground = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0, 0, 0, 0.9f);
                GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "", backgroundStyle);
                GUI.backgroundColor = oldBackground;

                var currentEvent = UnityEngine.Event.current;
                if (currentEvent.isKey && currentEvent.keyCode == KeyCode.Escape)
                {
                    Open = false;
                    return;
                }

                GUI.ModalWindow(1, windowRect, DrawWindow, "Servers");
            }
            else
            {
                if (GUILayout.Button("Server browser"))
                {
                    Open = true;
                }
            }
        }

        private void DrawWindow(int id)
        {
            // Make window draggable
            GUI.DragWindow(new Rect(0, 0, windowSize.x, 20));
            
            float innerWidth = windowSize.x;
            float innerHeight = windowSize.y;
            
            GUILayout.BeginHorizontal(GUILayout.Width(innerWidth - 4));
            {
                GUILayout.Label("Name", GUILayout.Width(430));

                GUI.skin.label.alignment = TextAnchor.UpperRight;
                GUILayout.Label("Players", GUILayout.Width(80));

                GUILayout.Label("Ping", GUILayout.Width(45));
                GUI.skin.label.alignment = TextAnchor.UpperLeft;
            }
            GUILayout.EndHorizontal();

            var lastRect = GUILayoutUtility.GetLastRect();
            
            GUILayout.BeginArea(new Rect(0, 45, innerWidth - 4, innerHeight - lastRect.height - 48));
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);
            {
                GUILayout.BeginVertical();

                foreach (ServerInfo serverInfo in servers)
                {
                    DrawRow(serverInfo, 0);
                }

                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        
        private void DrawRow(ServerInfo info, int index)
        {
            bool even = index % 2 == 0;
            Color oldBackground = GUI.backgroundColor;

            float rgb = even ? 0.3f : 0.2f;

            if (selectedServer == info)
            {
                rgb = 0.7f;
            }

            GUI.backgroundColor = new Color(rgb, rgb, rgb, 0.2f);

            GUILayout.BeginHorizontal(rowStyle);
            {
                GUILayout.Label(info.Name, GUILayout.Width(430));

                GUI.skin.label.alignment = TextAnchor.UpperRight;
                GUILayout.Label($"{info.Players}/{info.MaxPlayers}", GUILayout.Width(80));

                GUILayout.Label($"{info.Ping}", GUILayout.Width(45));
                GUI.skin.label.alignment = TextAnchor.UpperLeft;
            }
            GUILayout.EndHorizontal();
            
            if (UnityEngine.Event.current.type == EventType.MouseDown && UnityEngine.Event.current.button == 0)
            {
                var lastArea = GUILayoutUtility.GetLastRect();
                
                if (lastArea.Contains(UnityEngine.Event.current.mousePosition))
                {
                    selectedServer = info;
                }
            }

            GUI.backgroundColor = oldBackground;
        }

        private void SetOpen(bool open)
        {
            if (open == _open)
                return;

            if (open)
            {
                // Disable the game UI
                ingameMenu.SetActive(false);
            }
            else
            {
                // Enable the game UI
                ingameMenu.SetActive(true);
            }

            _open = open;
        }
    }
}
