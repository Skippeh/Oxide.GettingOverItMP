using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Lidgren.Network;
using Oxide.Core;
using ServerShared;
using ServerShared.Networking;

namespace Oxide.GettingOverItMP.Networking
{
    public static class ServerQuery
    {
        private struct QueryInfo
        {
            public IPEndPoint EndPoint;
            public Action<QueryDoneEventArgs> Callback;

            public QueryInfo(IPEndPoint endPoint, Action<QueryDoneEventArgs> callback)
            {
                EndPoint = endPoint;
                Callback = callback;
            }
        }

        /// <summary>Time in seconds before deciding to timeout the query.</summary>
        public const float MaxTime = 10;
        
        public static void Query(string ip, int port, Action<QueryDoneEventArgs> doneCallback)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    var endPoint2 = ((QueryInfo) state).EndPoint;
                    var doneCallback2 = ((QueryInfo) state).Callback;
                    NetClient client = CreateClientAndQuery(endPoint2);
                    Stopwatch pingStopwatch = Stopwatch.StartNew();

                    NetIncomingMessage response;

                    while (true)
                    {
                        response = client.WaitMessage((int) (MaxTime * 1000));

                        // Wait for timeout or DiscoveryResponse.
                        if (response == null || response.MessageType == NetIncomingMessageType.DiscoveryResponse)
                            break;
                    }

                    pingStopwatch.Stop();

                    if (response == null)
                    {
                        doneCallback2(new QueryDoneEventArgs
                        {
                            Successful = false
                        });
                    }
                    else
                    {
                        string serverName = response.ReadString();
                        ushort players = response.ReadUInt16();
                        ushort maxPlayers = response.ReadUInt16();
                        float ping = (float) pingStopwatch.Elapsed.TotalMilliseconds;

                        if (serverName.Length > SharedConstants.MaxServerNameLength)
                            serverName = serverName.Substring(0, SharedConstants.MaxServerNameLength);

                        doneCallback2(new QueryDoneEventArgs
                        {
                            ServerInfo = new ServerInfo
                            {
                                Ip = ip,
                                Port = port,
                                Name = serverName,
                                Players = players,
                                MaxPlayers = maxPlayers,
                                Ping = ping
                            },
                            Successful = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    doneCallback(new QueryDoneEventArgs
                    {
                        Successful = false
                    });
                }
            }, new QueryInfo(endPoint, doneCallback));
        }

        private static NetClient CreateClientAndQuery(IPEndPoint endPoint)
        {
            var config = new NetPeerConfiguration(SharedConstants.AppName);
            
            config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);

            var client = new NetClient(config);
            client.Start();
            
            client.DiscoverKnownPeer(endPoint);

            return client;
        }
    }
}
