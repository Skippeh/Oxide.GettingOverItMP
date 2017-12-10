using System;
using System.Collections;
using System.Collections.Generic;
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
        public ConnectionState State => server?.ConnectionState ?? ConnectionState.Disconnected;
        public int Id { get => localPlayer.Id; set => localPlayer.Id = value; }
        public string PlayerName { get => localPlayer.PlayerName; set => localPlayer.PlayerName = value; }
        public event ChatMessageReceived ChatMessageReceived;
        public string LastDisconnectReason { get; private set; }

        private EventBasedNetListener listener;
        private NetManager client;
        private NetPeer server;
        private LocalPlayer localPlayer;

        private readonly Dictionary<int, RemotePlayer> RemotePlayers = new Dictionary<int, RemotePlayer>();

        private float nextSendTime = 0;
        private bool handshakeResponseReceived;

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
                            LastDisconnectReason = "The name contains invalid characters.";
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
                        Destroy(RemotePlayers[id].gameObject);
                        RemotePlayers.Remove(id);
                    }
                    
                    break;
                }
                case MessageType.MoveData: // Received 30 times per second containing new movement data for every remote player. Includes the local player which needs to be filtered out.
                {
                    var moveData = reader.GetMovementDictionary();

                    foreach (var kv in moveData)
                    {
                        if (RemotePlayers.ContainsKey(kv.Key))
                        {
                            var remotePlayer = RemotePlayers[kv.Key];
                            remotePlayer.ApplyMove(kv.Value);
                        }
                        else if (kv.Key != Id)
                        {
                            Interface.Oxide.LogDebug($"Got movement from unknown player id: {kv.Key}");
                        }
                    }

                    break;
                }
                case MessageType.ChatMessage:
                {
                    int playerId = reader.GetInt();
                    Color color = reader.GetColor();
                    string message = reader.GetString();

                    MPBasePlayer player;

                    Interface.Oxide.LogDebug($"{playerId}: {message}");

                    if (playerId == Id)
                        player = localPlayer;
                    else
                    {
                        player = RemotePlayers.ContainsKey(playerId) ? RemotePlayers[playerId] : null;
                    }

                    ChatMessageReceived?.Invoke(this, new ChatMessageReceivedEventArgs
                    {
                        Player = player,
                        Message = message,
                        Color = color
                    });

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

            if (State == ConnectionState.Connected && Id != 0 && handshakeResponseReceived)
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

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 500, 1000, 1000));
            {
                GUILayout.Label($"Connection state: {State} {(State == ConnectionState.Connected ? server.EndPoint.ToString() : "")}");

                if (State == ConnectionState.Connected)
                {
                    GUILayout.Label($"Next send time: {nextSendTime}");
                }
            }
            GUILayout.EndArea();
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
    }

    public delegate void ChatMessageReceived(object sender, ChatMessageReceivedEventArgs args);
}
