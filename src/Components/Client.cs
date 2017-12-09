using System;
using System.Collections;
using System.Collections.Generic;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using LiteNetLib;
using LiteNetLib.Utils;
using Oxide.Core;
using Oxide.Core.Libraries;
using ServerShared;
using ServerShared.Player;
using UnityEngine;
using Time = UnityEngine.Time;

namespace Oxide.GettingOverItMP.Components
{
    public class Client : MonoBehaviour
    {
        public ConnectionState State => server?.ConnectionState ?? ConnectionState.Disconnected;
        public int Id { get; private set; }
        public string PlayerName { get; private set; }

        private EventBasedNetListener listener;
        private NetManager client;
        private NetPeer server;
        private LocalPlayer localPlayer;

        private Dictionary<int, RemotePlayer> RemotePlayers = new Dictionary<int, RemotePlayer>();

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
            this.server = server;
            SendHandshake();
        }

        private void OnDisconnected(NetPeer server, DisconnectInfo info)
        {
            this.server = null;
            Id = 0;

            RemoveAllRemotePlayers();
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
}
