using System;

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

        /// <param name="reason">Optional reason, null is allowed.</param>
        public PlayerBan(ulong steamId, string reason, DateTime? expirationDate) : this(reason, expirationDate)
        {
            SteamId = steamId;
            Type = BanType.SteamId;
        }

        /// <param name="reason">Optional reason, null is allowed.</param>
        public PlayerBan(uint ip, string reason, DateTime? expirationDate) : this(reason, expirationDate)
        {
            Ip = ip;
            Type = BanType.Ip;
        }

        private PlayerBan(string reason, DateTime? expirationDate)
        {
            Reason = reason;
            ExpirationDate = expirationDate;
        }

        public bool IsBanned()
        {
            if (ExpirationDate == null)
                return true;

            return DateTime.UtcNow < ExpirationDate;
        }

        public string GetReasonWithExpiration()
        {
            string result = $"You have been banned from this server: \"{Reason}\".";

            if (ExpirationDate != null)
                result += $" The ban will expire: {ExpirationDate.Value.ToLongDateString()}.";

            return result;
        }
    }
}
