using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Lidgren.Network;
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
                var client = CreateClientAndQuery(endPoint);
                NetIncomingMessage response;

                while (true)
                {
                    response = client.WaitMessage((int) (MaxTime * 1000));

                    // Wait for timeout or DiscoveryResponse.
                    if (response == null || response.MessageType == NetIncomingMessageType.DiscoveryResponse)
                        break;
                }

                if (response == null)
                {
                    doneCallback(new QueryDoneEventArgs
                    {
                        ServerInfo = null,
                        Successful = false
                    });
                }
                else
                {
                    string serverName = response.ReadString();
                    ushort players = response.ReadUInt16();
                    ushort maxPlayers = response.ReadUInt16();
                    float ping = response.SenderConnection?.AverageRoundtripTime ?? -1;

                    doneCallback(new QueryDoneEventArgs
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
            }, new QueryInfo(endPoint, doneCallback));
        }

        private static NetClient CreateClientAndQuery(IPEndPoint endPoint)
        {
            var config = new NetPeerConfiguration(SharedConstants.AppName)
            {
                Port = endPoint.Port
            };
            
            config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);

            var client = new NetClient(config);
            client.Start();

            var message = client.CreateMessage();
            message.Write(SharedConstants.QueryVersion);
            client.SendDiscoveryResponse(message, endPoint);

            return client;
        }
    }
}
