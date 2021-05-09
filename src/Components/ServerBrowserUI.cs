using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Lidgren.Network;
using Oxide.Core;
using Oxide.GettingOverIt;
using Oxide.GettingOverItMP.Networking;
using ServerShared;
using ServerShared.Networking;
using Steamworks;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components
{
    public class ServerBrowserUI : MonoBehaviour
    {
        enum MenuState
        {
            Browser,
            CustomConnect,
            Host
        }

        public bool Open { get => _open; private set => SetOpen(value); }
        private bool _open;

        private static readonly Vector2 windowSize = new Vector2(600, 400);
        private Rect windowRect;

        private static readonly Vector2 ipWindowSize = new Vector2(250, 100);
        private Rect ipWindowRect;

        private static readonly Vector2 hostWindowSize = new Vector2(600, 400);
        private Rect hostWindowRect;

        private Vector2 scrollPosition;
        private PlayerControl control;
        private ChatUI chatUi;
        private GameObject ingameMenu;
        private Client client;
        private string playerName;
        private ServerInfo selectedServer;
        private GUIStyle rowStyle;
        private GUIStyle backgroundStyle;
        private GUIStyle windowStyle;

        private bool disconnected => client.Status == NetConnectionStatus.Disconnected || client.Status == NetConnectionStatus.Disconnecting;
        private bool connected => client.Status == NetConnectionStatus.Connected || client.Status == NetConnectionStatus.InitiatedConnect;

        private bool searching;
        private MenuState currentMenuState = MenuState.Browser;
        private string ipText = "";
        private string strHostPort;
        private bool hostPrivate;
        private string serverName;
        private string strMaxPlayers;
        private bool noSteam;

        private readonly List<ServerInfo> servers = new List<ServerInfo>();

        private void Start()
        {
            control = GameObject.Find("Player").GetComponent<PlayerControl>();
            chatUi = GetComponent<ChatUI>();

            var canvas = GameObject.Find("Canvas");
            ingameMenu = canvas.transform.Find("InGame Menu").gameObject ?? throw new NotImplementedException("Could not find Ingame Menu");

            client = GameObject.Find("GOIMP.Client").GetComponent<Client>() ?? throw new NotImplementedException("Could not find Client");

            playerName = PlayerPrefs.GetString("GOIMP_PlayerName", "");
            strHostPort = PlayerPrefs.GetString("GOIMP_HostPort", SharedConstants.DefaultPort.ToString());
            ipText = PlayerPrefs.GetString("GOIMP_ConnectIp", "");
            hostPrivate = PlayerPrefs.GetInt("GOIMP_HostPrivate", 0) != 0;
            serverName = PlayerPrefs.GetString("GOIMP_ServerName", "");
            strMaxPlayers = PlayerPrefs.GetString("GOIMP_MaxPlayers", "100");
            noSteam = PlayerPrefs.GetInt("GOIMP_NoSteam", 0) != 0;
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

                windowStyle = new GUIStyle(GUI.skin.window);
            }

            if (!control.IsPaused() || chatUi.Writing)
                return;

            if (Open)
            {
                windowRect = new Rect(Screen.width / 2f - windowSize.x / 2f, Screen.height / 2f - windowSize.y / 2f, windowSize.x, windowSize.y);
                ipWindowRect = new Rect(Screen.width / 2f - ipWindowSize.x / 2f, Screen.height / 2f - ipWindowSize.y / 2f, ipWindowSize.x, ipWindowSize.y);
                hostWindowRect = new Rect(Screen.width / 2f - hostWindowSize.x / 2f, Screen.height / 2f - hostWindowSize.y / 2f, hostWindowSize.x, hostWindowSize.y);

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

                if (currentMenuState == MenuState.Browser)
                {
                    GUI.Window(1, windowRect, DrawServersWindow, "Servers", windowStyle);

                    // Draw stuff below server list window
                    GUILayout.BeginArea(new Rect(windowRect.x + 5, windowRect.yMax + 5, windowRect.width - 10, 100));
                    {
                        bool oldEnabled = GUI.enabled;

                        // Browser stuff below
                        GUILayout.BeginHorizontal();
                        {
                            var refreshContent = new GUIContent(searching ? "Cancel" : "Refresh");
                            var refreshSize = GUI.skin.button.CalcSize(refreshContent);

                            if (GUILayout.Button(refreshContent, GUILayout.Width(refreshSize.x)))
                            {
                                if (!searching)
                                    StartSearching();
                                else
                                    CancelSearching();
                            }

                            var connectIpContent = new GUIContent("Connect to IP");
                            var connectIpSize = GUI.skin.button.CalcSize(connectIpContent);

                            GUI.enabled = !connected;
                            if (GUILayout.Button(connectIpContent, GUILayout.Width(connectIpSize.x)))
                            {
                                currentMenuState = MenuState.CustomConnect;
                            }

                            var hostContent = new GUIContent("Host server");
                            var hostSize = GUI.skin.button.CalcSize(hostContent);

                            GUI.enabled = !connected;
                            if (GUILayout.Button(hostContent, GUILayout.Width(hostSize.x)))
                            {
                                currentMenuState = MenuState.Host;
                            }
                        }
                        GUILayout.EndHorizontal();

                        // Local player stuff below
                        GUI.enabled = !connected;
                        GUILayout.BeginHorizontal();
                        {
                            var nameContent = new GUIContent("Name");
                            var nameSize = GUI.skin.label.CalcSize(nameContent);

                            GUILayout.Label(nameContent, GUILayout.Width(nameSize.x));

                            if (SteamClient.IsValid)
                            {
                                playerName = SteamClient.Name;

                                bool oldEnabled2 = GUI.enabled;
                                GUI.enabled = false;
                                GUILayout.TextField(playerName, SharedConstants.MaxNameLength);
                                GUI.enabled = oldEnabled2;
                            }
                            else
                            {
                                playerName = GUILayout.TextField(playerName, SharedConstants.MaxNameLength);
                            }
                        }
                        GUILayout.EndHorizontal();

                        // Selected server info below
                        GUI.enabled = selectedServer != null || connected;
                        GUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button(connected ? "Disconnect" : "Connect"))
                            {
                                if (disconnected)
                                    ConnectToServer(selectedServer);
                                else
                                {
                                    client.Disconnect();
                                }
                            }
                        }
                        GUILayout.EndHorizontal();

                        GUI.enabled = oldEnabled;
                    }
                    GUILayout.EndArea();
                }
                else if (currentMenuState == MenuState.CustomConnect)
                {
                    GUI.Window(2, ipWindowRect, DrawCustomConnectWindow, "Connect to IP", windowStyle);
                }
                else if (currentMenuState == MenuState.Host)
                {
                    GUI.Window(3, hostWindowRect, DrawHostWindow, "Host server", windowStyle);
                }

                if (GUILayout.Button("Close browser"))
                {
                    Open = false;

                    if (currentMenuState != MenuState.Host)
                        currentMenuState = MenuState.Browser;
                }
            }
            else
            {
                if (GUILayout.Button("Server browser"))
                {
                    Open = true;

                    if (!searching && currentMenuState == MenuState.Browser)
                        StartSearching();
                }
            }
        }

        private void DrawServersWindow(int id)
        {
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

                var servers = this.servers.ToList(); // Copy server list incase a server is added mid drawing (while querying).
                for (var i = 0; i < servers.Count; i++)
                {
                    ServerInfo serverInfo = servers[i];
                    DrawRow(serverInfo, i);
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
                GUI.skin.label.richText = false;
                GUILayout.Label(info.Name, GUILayout.Width(430));
                GUI.skin.label.richText = true;

                GUI.skin.label.alignment = TextAnchor.UpperRight;
                GUILayout.Label($"{info.Players}/{info.MaxPlayers}", GUILayout.Width(80));

                GUILayout.Label($"{info.Ping:0}", GUILayout.Width(45));
                GUI.skin.label.alignment = TextAnchor.UpperLeft;
            }
            GUILayout.EndHorizontal();

            if (UnityEngine.Event.current.type == EventType.MouseDown && UnityEngine.Event.current.button == 0)
            {
                var lastArea = GUILayoutUtility.GetLastRect();

                if (lastArea.Contains(UnityEngine.Event.current.mousePosition))
                {
                    selectedServer = info;

                    if (UnityEngine.Event.current.clickCount > 0 && UnityEngine.Event.current.clickCount == 2)
                    {
                        if (connected)
                        {
                            client.Disconnect();
                        }
                        
                        ConnectToServer(info);
                    }
                }
            }

            GUI.backgroundColor = oldBackground;
        }

        private void DrawCustomConnectWindow(int id)
        {
            GUILayout.Label("IP");
            ipText = GUILayout.TextField(ipText, "255.255.255.255:25565".Length);

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Connect"))
                {
                    string[] ipPort = ipText.Replace(" ", "").Contains(":") ? ipText.Split(':') : new[] {ipText, SharedConstants.DefaultPort.ToString()};
                    string ip = ipPort[0];
                    int port = int.Parse(ipPort[1]);

                    if (string.IsNullOrEmpty(ip))
                        ip = "127.0.0.1";

                    SavePlayerPrefs();
                    client.Connect(ip, port, playerName);
                    currentMenuState = MenuState.Browser;
                }

                if (GUILayout.Button("Cancel"))
                {
                    currentMenuState = MenuState.Browser;
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawHostWindow(int id)
        {
            bool fieldsValid = true;
            GUI.enabled = !ListenServer.Running;

            GUILayout.Label("Server name");
            serverName = GUILayout.TextField(serverName, SharedConstants.MaxServerNameLength);

            if (serverName.Length == 0)
            {
                Color oldColor = GUI.color;
                GUI.color = SharedConstants.ColorRed;
                GUILayout.Label($"Server name can't be empty.");
                GUI.color = oldColor;

                fieldsValid = false;
            }

            GUILayout.Label("Max players");
            strMaxPlayers = GUILayout.TextField(strMaxPlayers, SharedConstants.MaxPlayerLimit);

            {
                if (!int.TryParse(strMaxPlayers, out int maxPlayers) || maxPlayers < 2 || maxPlayers > SharedConstants.MaxPlayerLimit)
                {
                    Color oldColor = GUI.color;
                    GUI.color = SharedConstants.ColorRed;
                    GUILayout.Label($"Max players needs to be between 2-{SharedConstants.MaxPlayerLimit}.");
                    GUI.color = oldColor;

                    fieldsValid = false;
                }
            }

            GUILayout.Label("Port");
            strHostPort = GUILayout.TextField(strHostPort, "25565".Length);
            
            if (!ushort.TryParse(strHostPort, out _))
            {
                Color oldColor = GUI.color;
                GUI.color = SharedConstants.ColorRed;
                GUILayout.Label($"Port needs to be between 0-65535.");
                GUI.color = oldColor;

                fieldsValid = false;
            }

            hostPrivate = GUILayout.Toggle(hostPrivate, " Private server (don't show in server browser)");

            GUI.enabled = !ListenServer.Running && SteamClient.IsValid;
            noSteam = GUILayout.Toggle(noSteam, " No steam authentication (players can join without owning the game on Steam but user identity verification will be limited to IP.)");

            if (!SteamClient.IsValid)
                noSteam = true;
            
            GUI.enabled = true;

            GUILayout.BeginHorizontal();
            {
                GUIContent startContent = new GUIContent(ListenServer.Running ? "Stop" : "Start");
                Vector2 startSize = GUI.skin.button.CalcSize(startContent);

                GUI.enabled = fieldsValid || ListenServer.Running;
                if (GUILayout.Button(startContent, GUILayout.Width(startSize.x)))
                {
                    if (!ListenServer.Running)
                    {
                        var port = ushort.Parse(strHostPort);
                        int maxPlayers = int.Parse(strMaxPlayers);

                        ListenServer.Start(serverName, maxPlayers, port, hostPrivate, !noSteam);
                        client.Connect("127.0.0.1", port, playerName);
                        SavePlayerPrefs();
                    }
                    else
                    {
                        client.Disconnect();
                        ListenServer.Stop();
                    }
                }

                GUIContent backContent = new GUIContent("Back");
                Vector2 backSize = GUI.skin.button.CalcSize(backContent);

                GUI.enabled = !ListenServer.Running;
                if (GUILayout.Button(backContent, GUILayout.Width(backSize.x)))
                {
                    currentMenuState = MenuState.Browser;
                }
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
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

        private void ConnectToServer(ServerInfo info)
        {
            ConnectToServer(info.Ip, info.Port);
        }

        private void ConnectToServer(string ip, int port)
        {
            SavePlayerPrefs();
            client.Connect(ip, port, playerName);
        }

        private void StartSearching()
        {
            searching = true;
            selectedServer = null;
            servers.Clear();
            
            QueryServerList(serverList =>
            {
                if (!searching)
                    return;

                Interface.Oxide.LogDebug($"Quering {serverList.Length} server(s)...");

                if (serverList.Length == 0)
                    searching = false;

                foreach (var masterServerInfo in serverList)
                {
                    if (!searching)
                        return;

                    int numDone = 0;
                    ServerQuery.Query(masterServerInfo.Ip, masterServerInfo.Port, args =>
                    {
                        if (!searching)
                            return;

                        lock (this)
                        {
                            if (args.Successful && args.ServerInfo.ServerVersion == SharedConstants.Version)
                                servers.Add(args.ServerInfo);
                            else
                                Interface.Oxide.LogDebug($"Found incompatible server ({args.ServerInfo.ServerVersion}), ignoring.");

                            ++numDone;

                            if (numDone >= serverList.Length)
                                searching = false;
                        }
                    });
                }
            });
        }

        private void QueryServerList(Action<MasterServerInfo[]> callback)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    using (var webClient = new WebClient())
                    {
                        string serversString = webClient.DownloadString($"{SharedConstants.MasterServerUrl}/list");
                        callback(ParseServersString(serversString).ToArray());
                    }
                }
                catch (Exception ex)
                {
                    Interface.Oxide.LogError(ex.ToString());
                    callback(new MasterServerInfo[0]);
                }
            });
        }

        private IEnumerable<MasterServerInfo> ParseServersString(string servers)
        {
            string[] serversArray = servers.Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries);

            foreach (string endPoint in serversArray)
            {
                string[] ipPort = endPoint.Split(';');
                string host = ipPort[0];
                string strPort = ipPort[1];
                
                yield return new MasterServerInfo(host, int.Parse(strPort));
            }
        }

        private void CancelSearching()
        {
            searching = false;
        }

        private void SavePlayerPrefs()
        {
            PlayerPrefs.SetString("GOIMP_ConnectIp", ipText);
            PlayerPrefs.SetString("GOIMP_PlayerName", playerName);
            PlayerPrefs.SetString("GOIMP_HostPort", strHostPort);
            PlayerPrefs.SetInt("GOIMP_HostPrivate", hostPrivate ? 1 : 0);
            PlayerPrefs.SetString("GOIMP_ServerName", serverName);
            PlayerPrefs.SetString("GOIMP_MaxPlayers", strMaxPlayers);
            PlayerPrefs.SetInt("GOIMP_NoSteam", noSteam ? 1 : 0);
        }
    }
}
