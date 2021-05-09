using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using FluffyUnderware.DevTools.Extensions;
using Lidgren.Network;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.GettingOverIt;
using Oxide.GettingOverItMP.EventArgs;
using ServerShared;
using ServerShared.Networking;
using ServerShared.Player;
using Steamworks;
using UnityEngine;
using Color = UnityEngine.Color;
using DisconnectReason = ServerShared.DisconnectReason;
using Time = UnityEngine.Time;

namespace Oxide.GettingOverItMP.Components
{
    public class Client : MonoBehaviour
    {
        public event PlayerJoinedEventHandler PlayerJoined;
        public event PlayerLeftEventHandler PlayerLeft;

        public readonly Dictionary<int, RemotePlayer> RemotePlayers = new Dictionary<int, RemotePlayer>();

        public NetConnectionStatus Status => client.ConnectionStatus;
        public int Id { get => localPlayer.Id; set => localPlayer.Id = value; }
        public string PlayerName { get => localPlayer.PlayerName; set => localPlayer.PlayerName = value; }
        public event ChatMessageReceived ChatMessageReceived;
        public string LastDisconnectReason { get; private set; }
        public float LastReceiveDelta { get; private set; }
        public DiscoveryServerInfo ServerInfo { get; private set; }
        public LocalPlayer LocalPlayer => localPlayer;

        private GameClientPeer client;
        private NetConnection server;
        private LocalPlayer localPlayer;
        private Spectator spectator;

        private ChatUI chatUi;
        
        private float nextSendTime = 0;
        private bool handshakeResponseReceived;
        private float lastReceiveTime = 0;
        private AuthTicket authTicket;

        private IPEndPoint launchConnectEndPoint;

        private void Start()
        {
            localPlayer = GameObject.Find("Player").GetComponent<LocalPlayer>();

            client = new GameClientPeer(new NetPeerConfiguration(SharedConstants.AppName)
            {
                MaximumConnections = 1,
                ConnectionTimeout = 5,
                PingInterval = 1f
            });

            client.Connected += OnConnected;
            client.Disconnected += OnDisconnected;
            client.DataReceived += OnReceiveData;

            client.Start();

            chatUi = GameObject.Find("GOIMP.UI").GetComponent<ChatUI>() ?? throw new NotImplementedException("Could not find ChatUI");
            spectator = GameObject.Find("GOIMP.Spectator").GetComponent<Spectator>() ?? throw new NotImplementedException("Could not find Spectator");

            if (launchConnectEndPoint != null)
            {
                StartCoroutine(ConnectToLaunchEndpoint());
            }
        }

        private IEnumerator ConnectToLaunchEndpoint()
        {
            // Wait for end of frame so that all components have finished initializing.
            yield return new WaitForEndOfFrame();

            string playerName = PlayerPrefs.GetString("GOIMP_PlayerName", "");

            if (string.IsNullOrEmpty(playerName))
            {
                if (SteamClient.IsValid)
                {
                    playerName = SteamClient.Name;
                    PlayerPrefs.SetString("GOIMP_PlayerName", playerName);
                }
                else
                {
                    Interface.Oxide.LogError("Cancelling joining server, no player name set.");
                }
            }

            if (!string.IsNullOrEmpty(playerName))
                Connect(launchConnectEndPoint.Address.ToString(), launchConnectEndPoint.Port, playerName);

            launchConnectEndPoint = null;
        }

        private void OnConnected(object sender, ConnectedEventArgs args)
        {
            server = args.Connection;
            LastDisconnectReason = null;
        }

        private void OnDisconnected(object sender, DisconnectedEventArgs args)
        {
            if (SteamClient.IsValid)
                ClearSteamInfo();

            localPlayer.ResetPotProperties();

            this.server = null;
            Id = 0;
            authTicket?.Cancel();

            RemoveAllRemotePlayers();
            spectator.StopSpectating();

            LastDisconnectReason = null;

            switch (args.Reason)
            {
                default:
                    if (args.ReasonString != "bye")
                        LastDisconnectReason = args.ReasonString;
                    break;
                case DisconnectReason.DuplicateHandshake:
                {
                    LastDisconnectReason = "Duplicate handshake sent to the server.";
                    break;
                }
                case DisconnectReason.HandshakeTimeout:
                {
                    LastDisconnectReason = "Failed to send handshake within the time limit.";
                    break;
                }
                case DisconnectReason.InvalidMessage:
                {
                    LastDisconnectReason = "The last sent message was invalid.";
                    break;
                }
                case DisconnectReason.InvalidName:
                {
                    LastDisconnectReason = "The name is either empty or it contains invalid characters.";
                    break;
                }
                case DisconnectReason.NotAccepted:
                {
                    LastDisconnectReason = "Tried to send a message before getting a successful handshake response.";
                    break;
                }
                case DisconnectReason.VersionNewer:
                {
                    LastDisconnectReason = "The server is running an older version.";
                    break;
                }
                case DisconnectReason.VersionOlder:
                {
                    LastDisconnectReason = "The server is running a newer version.";
                    break;
                }
                case DisconnectReason.InvalidSteamSession:
                {
                    LastDisconnectReason = "Invalid steam session.";
                    break;
                }
                case DisconnectReason.Banned:
                {
                    LastDisconnectReason = $"You have been banned from this server.";
                    break;
                }
            }

            if (args.AdditionalInfo != null)
                LastDisconnectReason += $"\n{args.AdditionalInfo}";

            string chatMessage = "Disconnected from the server";

            if (args.ReasonString != "bye")
            {
                chatMessage = $"{chatMessage}:\n{LastDisconnectReason}";
            }
            else
            {
                chatMessage = $"{chatMessage}.";
            }

            if (args.ReasonString != "bye")
                chatUi.AddMessage(chatMessage, null, SharedConstants.ColorRed);
            else
                chatUi.AddMessage(chatMessage, null, SharedConstants.ColorRed);
        }

        private void OnReceiveData(object sender, DataReceivedEventArgs args)
        {
            var netMessage = args.Message;
            var messageType = args.MessageType;

            switch (messageType)
            {
                case MessageType.HandshakeResponse: // Should be the first message received from the server. Contains local player id and remote player data.
                {
                    Id = netMessage.ReadInt32();
                    PlayerName = netMessage.ReadString();
                    localPlayer.Wins = netMessage.ReadInt32();
                    localPlayer.SetGoldness(netMessage.ReadSingle());
                    localPlayer.SetPotColor(netMessage.ReadRgbaColor());
                    var names = netMessage.ReadNamesDictionary();
                    var wins = netMessage.ReadWinsDictionary();
                    var goldness = netMessage.ReadGoldnessDictionary();
                    var colors = netMessage.ReadColorsDictionary();
                    var remotePlayers = netMessage.ReadMovementDictionary();
                    ServerInfo = netMessage.ReadDiscoveryServerInfo();

                    PlayerName = $"[{Id}] {PlayerName}"; // Prefix player name with id.

                    localPlayer.PlayerName = PlayerName;
                    localPlayer.Id = Id;

                    foreach (var kv in remotePlayers)
                    {
                        StartCoroutine(SpawnRemotePlayer(kv.Key, kv.Value, names[kv.Key], wins[kv.Key], goldness[kv.Key], colors[kv.Key]));
                    }

                    handshakeResponseReceived = true;
                    chatUi.AddMessage("Connected to the server.", null, SharedConstants.ColorGreen);
                    Interface.Oxide.LogDebug($"Got id: {Id} and {remotePlayers.Count} remote player(s)");

                    if (SteamClient.IsValid)
                        UpdateSteamInfo();

                    break;
                }
                case MessageType.CreatePlayer: // Received when a remote player connects.
                {
                    int id = netMessage.ReadInt32();
                    string name = netMessage.ReadString();
                    Interface.Oxide.LogDebug($"Create player with id {id}");

                    if (id == Id)
                    {
                        Interface.Oxide.LogError("CreatePlayer contained the local player");
                        return;
                    }

                    PlayerMove move = netMessage.ReadPlayerMove();
                    int wins = netMessage.ReadInt32();
                    float goldness = netMessage.ReadSingle();
                    Color potColor = netMessage.ReadRgbaColor();
                    StartCoroutine(SpawnRemotePlayer(id, move, name, wins, goldness, potColor));
                    
                    break;
                }
                case MessageType.RemovePlayer: // Received when a remote player disconnects or starts spectating.
                {
                    int id = netMessage.ReadInt32();

                    if (RemotePlayers.ContainsKey(id))
                    {
                        var player = RemotePlayers[id];
                        PlayerLeft?.Invoke(this, new PlayerLeftEventArgs {Player = player});
                        Destroy(RemotePlayers[id].gameObject);
                        RemotePlayers.Remove(id);
                    }
                    
                    break;
                }
                case MessageType.MoveData: // Received 30 times per second containing new movement data for every remote player. Includes the local player which needs to be filtered out.
                {
                    LastReceiveDelta = Time.time - lastReceiveTime;
                    lastReceiveTime = Time.time;
                    
                    var moveData = netMessage.ReadMovementDictionary();

                    foreach (var kv in moveData)
                    {
                        if (RemotePlayers.ContainsKey(kv.Key))
                        {
                            var remotePlayer = RemotePlayers[kv.Key];
                            remotePlayer.ApplyMove(kv.Value);
                        }
                    }

                    break;
                }
                case MessageType.ChatMessage:
                {
                    int playerId = netMessage.ReadInt32();
                    string name = netMessage.ReadString();
                    Color color = netMessage.ReadRgbaColor();
                    string message = netMessage.ReadString();

                    ChatMessageReceived?.Invoke(this, new ChatMessageReceivedEventArgs
                    {
                        PlayerName = name,
                        PlayerId = playerId,
                        Message = message,
                        Color = color
                    });

                    break;
                }
                case MessageType.SpectateTarget:
                {
                    int targetId = netMessage.ReadInt32();

                    Interface.Oxide.LogDebug($"Spectate {targetId}");

                    if (targetId == 0)
                        spectator.StopSpectating();
                    else
                    {
                        var targetPlayer = RemotePlayers.ContainsKey(targetId) ? RemotePlayers[targetId] : null;

                        if (targetPlayer == null)
                        {
                            Interface.Oxide.LogError($"Could not find spectate target ({targetId}).");
                            chatUi.AddMessage($"Could not find spectate target (shouldn't happen, id: {targetId}). Disconnecting from server.", null, SharedConstants.ColorRed);
                            LastDisconnectReason = "Disconnected because of unexpected client message handling error.";
                            Disconnect();
                            return;
                        }

                        spectator.SpectatePlayer(targetPlayer);
                    }

                    break;
                }
                case MessageType.PlayerPotProperties:
                {
                    int targetId = netMessage.ReadInt32();
                    float goldness = netMessage.ReadSingle();
                    Color potColor = netMessage.ReadRgbaColor();
                    MPBasePlayer targetPlayer = RemotePlayers.ContainsKey(targetId) ? (MPBasePlayer) RemotePlayers[targetId] : localPlayer;
                    targetPlayer.SetPotColor(potColor);
                    targetPlayer.SetGoldness(goldness);
                    break;
                }
                case MessageType.PlayerWins:
                {
                    int targetId = netMessage.ReadInt32();
                    int wins = netMessage.ReadInt32();
                    MPBasePlayer targetPlayer = RemotePlayers.ContainsKey(targetId) ? (MPBasePlayer) RemotePlayers[targetId] : localPlayer;
                    targetPlayer.Wins = wins;
                    break;
                }
            }
        }

        private IEnumerator SpawnRemotePlayer(int id, PlayerMove move, string playerName, int wins, float goldness, Color potColor)
        {
            var remotePlayer = RemotePlayer.CreatePlayer($"Id {id}", id);
            yield return new WaitForSeconds(0);
            remotePlayer.PlayerName = $"[{id}] {playerName}"; // Prefix name with ID.
            remotePlayer.ApplyMove(move, 0);
            remotePlayer.Wins = wins;
            remotePlayer.SetGoldness(goldness);
            remotePlayer.SetPotColor(potColor);

            RemotePlayers.Add(id, remotePlayer);
            PlayerJoined?.Invoke(this, new PlayerJoinedEventArgs {Player = remotePlayer});
            Interface.Oxide.LogDebug($"Added remote player with id {id} at {move.Position} ({remotePlayer.transform.position}");
        }

        private void Update()
        {
            client.Update();

            if (server == null)
                return;

            if (Status == NetConnectionStatus.Connected && Id != 0 && handshakeResponseReceived && !spectator.Spectating)
            {
                if (Time.time >= nextSendTime)
                {
                    nextSendTime = Time.time + 1f / SharedConstants.UpdateRate;
                    
                    var writer = client.CreateMessage();
                    writer.Write(MessageType.MoveData);
                    writer.Write(localPlayer.CreateMove());
                    server.SendMessage(writer, NetDeliveryMethod.UnreliableSequenced, SharedConstants.MoveDataChannel);
                }
            }
        }

        private void OnDestroy()
        {
            if (server != null)
                Disconnect();

            RemoveAllRemotePlayers();
        }

        private void RemoveAllRemotePlayers()
        {
            RemotePlayers.ForEach(kv =>
            {
                PlayerLeft?.Invoke(this, new PlayerLeftEventArgs {Player = kv.Value});
                Destroy(kv.Value.gameObject);
            });
            RemotePlayers.Clear();
        }

        private void UpdateSteamInfo()
        {
            SteamFriends.SetRichPresence("status", $"Playing on {ServerInfo.Name}.");
            SteamFriends.SetRichPresence("connect", $"--goimp-connect {server.RemoteEndPoint}");
        }

        private void ClearSteamInfo()
        {
            SteamFriends.ClearRichPresence();
        }

        // Called by MPCore using StartCoroutine.
        private void LaunchConnect(IPEndPoint endPoint)
        {
            Interface.Oxide.LogDebug($"launchConnectEndPoint = {endPoint}");
            launchConnectEndPoint = endPoint;
        }

        public void Connect(string ip, int port, string playerName)
        {
            if (string.IsNullOrEmpty(playerName?.Trim())) throw new ArgumentException("playerName can't be null or empty", nameof(playerName));
            
            PlayerName = playerName;

            NetOutgoingMessage hailMessage = client.CreateMessage();
            hailMessage.Write(SharedConstants.Version);
            hailMessage.Write(PlayerName);
            hailMessage.Write(localPlayer.CreateMove());

            if (SteamClient.IsValid)
            {
                authTicket = SteamUser.GetAuthSessionTicket();
                hailMessage.Write(true);
                hailMessage.Write(authTicket.Data.Length);
                hailMessage.Write(authTicket.Data);
                hailMessage.Write(SteamClient.SteamId);
            }
            else
            {
                hailMessage.Write(false);
            }

            hailMessage.Write(PlayerPrefs.GetInt("NumWins", 0));

            client.Connect(ip, port, hailMessage);
            Interface.Oxide.LogDebug($"Connecting to: {ip}:{port}...");
        }

        public void Disconnect()
        {
            server?.Disconnect("bye");
        }

        public void SendChatMessage(string text)
        {
            if (server == null || !handshakeResponseReceived)
                return;

            var message = client.CreateMessage();
            message.Write(MessageType.ChatMessage);
            message.Write(text);

            server.SendMessage(message, NetDeliveryMethod.ReliableOrdered, 0);
        }

        public void SendStopSpectating()
        {
            if (server == null)
                return;

            var writer = client.CreateMessage();
            writer.Write(MessageType.ClientStopSpectating);

            server.SendMessage(writer, NetDeliveryMethod.ReliableOrdered, 0);
        }

        public void SendSpectate(RemotePlayer player)
        {
            var writer = client.CreateMessage();
            writer.Write(MessageType.SpectateTarget);
            writer.Write(player.Id);

            server.SendMessage(writer, NetDeliveryMethod.ReliableOrdered, 0);
        }

        public void SendSwitchSpectateTarget(int indexDelta)
        {
            if (!spectator.Spectating)
                return;

            var players = RemotePlayers.Values.ToList();
            int targetIndex = players.IndexOf(spectator.Target) + indexDelta;

            while (targetIndex >= players.Count)
                targetIndex -= players.Count;

            while (targetIndex < 0)
                targetIndex += players.Count;

            if (players[targetIndex] == spectator.Target)
                return;

            SendSpectate(players[targetIndex]);
        }
    }
    
    public delegate void PlayerJoinedEventHandler(object sender, PlayerJoinedEventArgs args);
    public delegate void PlayerLeftEventHandler(object sender, PlayerLeftEventArgs args);
    public delegate void ChatMessageReceived(object sender, ChatMessageReceivedEventArgs args);
}
