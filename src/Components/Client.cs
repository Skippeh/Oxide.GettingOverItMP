using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using LiteNetLib;
using LiteNetLib.Utils;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.GettingOverItMP.EventArgs;
using ServerShared;
using ServerShared.Player;
using UnityEngine;
using DisconnectReason = ServerShared.DisconnectReason;
using Time = UnityEngine.Time;

namespace Oxide.GettingOverItMP.Components
{
    public class Client : MonoBehaviour
    {
        public readonly Dictionary<int, RemotePlayer> RemotePlayers = new Dictionary<int, RemotePlayer>();

        public ConnectionState State => server?.ConnectionState ?? ConnectionState.Disconnected;
        public int Id { get => localPlayer.Id; set => localPlayer.Id = value; }
        public string PlayerName { get => localPlayer.PlayerName; set => localPlayer.PlayerName = value; }
        public event ChatMessageReceived ChatMessageReceived;
        public string LastDisconnectReason { get; private set; }

        public float LastReceiveDelta { get; private set; }

        private EventBasedNetListener listener;
        private NetManager client;
        private NetPeer server;
        private LocalPlayer localPlayer;
        private Spectator spectator;

        private ChatUI chatUi;


        private float nextSendTime = 0;
        private bool handshakeResponseReceived;

        private float lastReceiveTime = 0;

        private void Start()
        {
            localPlayer = GameObject.Find("Player").GetComponent<LocalPlayer>();

            listener = new EventBasedNetListener();
            listener.PeerConnectedEvent += OnConnected;
            listener.PeerDisconnectedEvent += OnDisconnected;
            listener.NetworkReceiveEvent += OnReceiveData;

            client = new NetManager(listener, SharedConstants.AppName);
            client.UpdateTime = 33; // Poll/send 30 times per second.
            client.Start();

            chatUi = GameObject.Find("GOIMP.UI").GetComponent<ChatUI>() ?? throw new NotImplementedException("Could not find ChatUI");
            spectator = GameObject.Find("GOIMP.Spectator").GetComponent<Spectator>() ?? throw new NotImplementedException("Could not find Spectator");
        }

        private void OnConnected(NetPeer server)
        {
            LastDisconnectReason = null;
            this.server = server;
            SendHandshake();
        }

        private void OnDisconnected(NetPeer server, DisconnectInfo info)
        {
            this.server = null;
            Id = 0;

            RemoveAllRemotePlayers();
            spectator.StopSpectating();

            switch (info.Reason)
            {
                default:
                {
                    LastDisconnectReason = $"Unknown ({info.Reason})";
                    break;
                }
                case LiteNetLib.DisconnectReason.DisconnectPeerCalled: // Client disconnected
                {
                    LastDisconnectReason = null;
                    break;
                }
                case LiteNetLib.DisconnectReason.RemoteConnectionClose: // Server disconnected the client
                {
                    if (info.AdditionalData.AvailableBytes == 0)
                    {
                        LastDisconnectReason = "Server closed the connection.";
                        break;
                    }

                    var reason = (DisconnectReason) info.AdditionalData.GetByte();

                    switch (reason)
                    {
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
                    }

                    break;
                }
            }

            if (info.Reason != LiteNetLib.DisconnectReason.DisconnectPeerCalled && info.Reason != LiteNetLib.DisconnectReason.ConnectionFailed)
                chatUi.AddMessage($"Disconnected from the server. ({LastDisconnectReason})", null, SharedConstants.ColorRed);
        }

        private void OnReceiveData(NetPeer peer, NetDataReader reader)
        {
            var messageType = (MessageType) reader.GetByte();

            switch (messageType)
            {
                case MessageType.HandshakeResponse: // Should be the first message received from the server. Contains local player id and remote player data.
                {
                    Id = reader.GetInt();
                    PlayerName = reader.GetString();
                    var names = reader.GetNamesDictionary();
                    var remotePlayers = reader.GetMovementDictionary();

                    localPlayer.PlayerName = PlayerName;
                    localPlayer.Id = Id;
                    
                    foreach (var kv in remotePlayers)
                    {
                        StartCoroutine(SpawnRemotePlayer(kv.Key, kv.Value, names[kv.Key]));
                    }

                    handshakeResponseReceived = true;
                    chatUi.AddMessage("Connected to the server.", null, SharedConstants.ColorGreen);
                    Interface.Oxide.LogDebug($"Got id: {Id} and {remotePlayers.Count} remote player(s)");

                    break;
                }
                case MessageType.CreatePlayer: // Received when a remote player connects.
                {
                    int id = reader.GetInt();
                    string name = reader.GetString();
                    Interface.Oxide.LogDebug($"Create player with id {id}");

                    if (id == Id)
                    {
                        Interface.Oxide.LogError("CreatePlayer contained the local player");
                        return;
                    }

                    PlayerMove move = reader.GetPlayerMove();
                    StartCoroutine(SpawnRemotePlayer(id, move, name));
                    
                    break;
                }
                case MessageType.RemovePlayer: // Received when a remote player disconnects.
                {
                    int id = reader.GetInt();

                    if (RemotePlayers.ContainsKey(id))
                    {
                        var player = RemotePlayers[id];
                        Destroy(RemotePlayers[id].gameObject);
                        RemotePlayers.Remove(id);
                    }
                    
                    break;
                }
                case MessageType.MoveData: // Received 30 times per second containing new movement data for every remote player. Includes the local player which needs to be filtered out.
                {
                    LastReceiveDelta = Time.time - lastReceiveTime;
                    lastReceiveTime = Time.time;
                    
                    var moveData = reader.GetMovementDictionary();

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
                    string name = reader.GetString();
                    Color color = reader.GetColor();
                    string message = reader.GetString();

                    ChatMessageReceived?.Invoke(this, new ChatMessageReceivedEventArgs
                    {
                        PlayerName = name,
                        Message = message,
                        Color = color
                    });

                    break;
                }
                case MessageType.SpectateTarget:
                {
                    int targetId = reader.GetInt();

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
            }
        }

        private IEnumerator SpawnRemotePlayer(int id, PlayerMove move, string playerName)
        {
            var remotePlayer = RemotePlayer.CreatePlayer($"Id {id}");
            yield return new WaitForSeconds(0);
            remotePlayer.PlayerName = playerName;
            remotePlayer.ApplyMove(move, 0);
            RemotePlayers.Add(id, remotePlayer);
            Interface.Oxide.LogDebug($"Added remote player with id {id} at {move.Position} ({remotePlayer.transform.position}");
        }

        private void Update()
        {
            client.PollEvents();

            if (server == null)
                return;

            if (State == ConnectionState.Connected && Id != 0 && handshakeResponseReceived && !spectator.Spectating)
            {
                if (Time.time >= nextSendTime)
                {
                    nextSendTime = Time.time + 0.033f;

                    var writer = new NetDataWriter();
                    writer.Put(MessageType.MoveData);
                    writer.Put(localPlayer.CreateMove());
                    server.Send(writer, SendOptions.Sequenced);
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
            RemotePlayers.ForEach(plr => Destroy(plr.Value.gameObject));
            RemotePlayers.Clear();
        }

        public void Connect(string ip, int port, string playerName)
        {
            if (string.IsNullOrEmpty(playerName?.Trim())) throw new ArgumentException("playerName can't be null or empty", nameof(playerName));
            
            Interface.Oxide.LogDebug($"Connecting to: {ip}:{port}...");
            PlayerName = playerName;
            client.Connect(ip, port);
        }

        public void Disconnect()
        {
            client.DisconnectPeer(server);
        }

        public void SendChatMessage(string text)
        {
            if (server == null)
                return;
            
            var writer = new NetDataWriter();
            writer.Put(MessageType.ChatMessage);
            writer.Put(text);

            server.Send(writer, SendOptions.ReliableOrdered);
        }

        public void SendStopSpectating()
        {
            if (server == null)
                return;

            var writer = new NetDataWriter();
            writer.Put(MessageType.ClientStopSpectating);

            server.Send(writer, SendOptions.ReliableOrdered);
        }

        private void SendHandshake()
        {
            Interface.Oxide.LogDebug("Sending handshake...");

            var writer = new NetDataWriter();
            writer.Put(MessageType.ClientHandshake);
            writer.Put(SharedConstants.Version);
            writer.Put(PlayerName);
            writer.Put(localPlayer.CreateMove());

            server.Send(writer, SendOptions.ReliableOrdered);
        }

        public void SendSpectate(RemotePlayer player)
        {
            var writer = new NetDataWriter();
            writer.Put(MessageType.SpectateTarget);
            writer.Put(player.Id);

            server.Send(writer, SendOptions.ReliableOrdered);
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

    public delegate void ChatMessageReceived(object sender, ChatMessageReceivedEventArgs args);
}
