using System;
using System.Net;
using Newtonsoft.Json;

namespace ServerShared
{
    public class PlayerBan
    {
        public enum BanType
        {
            SteamId,
            Ip
        }

        public BanType Type;
        public ulong SteamId;
        public uint Ip;
        public DateTime? ExpirationDate;
        public string Reason;
        public string ReferenceName;

        /// <param name="reason">Optional reason, null is allowed.</param>
        public PlayerBan(ulong steamId, string reason, DateTime? expirationDate, string referenceName) : this(reason, expirationDate, referenceName)
        {
            SteamId = steamId;
            Type = BanType.SteamId;
        }

        /// <param name="reason">Optional reason, null is allowed.</param>
        public PlayerBan(uint ip, string reason, DateTime? expirationDate, string referenceName) : this(reason, expirationDate, referenceName)
        {
            Ip = ip;
            Type = BanType.Ip;
        }

        private PlayerBan(string reason, DateTime? expirationDate, string referenceName)
        {
            Reason = reason;
            ExpirationDate = expirationDate;
            ReferenceName = referenceName;
        }

        /// <summary>Do not use. Exclusively used for serialization/deserialization</summary>
        [JsonConstructor]
        public PlayerBan() { }
        
        public bool Expired()
        {
            if (ExpirationDate == null)
                return false;

            return DateTime.UtcNow >= ExpirationDate;
        }

        public string GetReasonWithExpiration()
        {
            string reason = Reason ?? "No reason given";
            string result = $"You have been banned from this server: \"{reason}\".";

            if (ExpirationDate != null)
                result += $" The ban will expire: {ExpirationDate.Value.ToLongDateString()} {ExpirationDate.Value.ToShortTimeString()} UTC.";

            return result;
        }

        /// <summary>Returns a user friendly string representing the ban type and the identifier of the ban (ip/steamid).</summary>
        public string GetIdentifier()
        {
            switch (Type)
            {
                case BanType.Ip:
                    return $"IP: {new IPAddress(Ip)}";
                case BanType.SteamId:
                    return $"SteamID64: {SteamId}";
            }

            throw new NotImplementedException($"BanType not implemented: {Type}");
        }
    }
}
