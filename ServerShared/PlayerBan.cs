using System;
using System.Net;
using Newtonsoft.Json;
using ServerShared.Player;

namespace ServerShared
{
    public class PlayerBan
    {
        public uint Ip => Identity.Ip;
        public ulong SteamId => Identity.SteamId;
        public IdentityType BanType => Identity.Type;

        public PlayerIdentity Identity;
        public DateTime? ExpirationDate;
        public string Reason;
        public string ReferenceName;

        /// <param name="reason">Optional reason, null is allowed.</param>
        /// <param name="referenceName">Optional name reference, null is allowed.</param>
        public PlayerBan(ulong steamId, string reason, DateTime? expirationDate, string referenceName) : this(reason, expirationDate, referenceName)
        {
            Identity = new PlayerIdentity(steamId);
        }

        /// <param name="reason">Optional reason, null is allowed.</param>
        /// <param name="referenceName">Optional name reference, null is allowed.</param>
        public PlayerBan(uint ip, string reason, DateTime? expirationDate, string referenceName) : this(reason, expirationDate, referenceName)
        {
            Identity = new PlayerIdentity(ip);
        }

        private PlayerBan(string reason, DateTime? expirationDate, string referenceName)
        {
            Reason = reason;
            ExpirationDate = expirationDate;
            ReferenceName = referenceName;
        }

        /// <summary>Do not use. Exclusively used for serialization/deserialization</summary>
        [JsonConstructor]
        private PlayerBan() { }
        
        public bool Expired()
        {
            if (ExpirationDate == null)
                return false;

            return DateTime.UtcNow >= ExpirationDate;
        }

        public string GetReasonWithExpiration()
        {
            string reason = Reason ?? "No reason given";
            string result = $"Reason: {reason}.";

            if (ExpirationDate != null)
            {
                TimeSpan timeLeft = ExpirationDate.Value - DateTime.UtcNow;
                int hours = timeLeft.Hours;
                int minutes = timeLeft.Minutes;
                int seconds = timeLeft.Seconds;

                string daysString = "";

                if (timeLeft.TotalDays >= 1)
                {
                    daysString = $"{(int) timeLeft.TotalDays} days, ";
                }

                result += $"\nExpires in: {daysString}{hours.ToString().PadLeft(2, '0')}h {minutes.ToString().PadLeft(2, '0')}m {seconds.ToString().PadLeft(2, '0')}s.";
            }

            return result;
        }

        /// <summary>Returns a user friendly string representing the ban type and the identifier of the ban (ip/steamid).</summary>
        public string GetIdentifier()
        {
            return Identity.ToString();
        }
    }
}
