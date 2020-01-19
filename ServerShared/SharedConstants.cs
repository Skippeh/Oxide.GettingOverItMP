using UnityEngine;

namespace ServerShared
{
    public static class SharedConstants
    {
        public const string AppName = "GOIMP";
        public const int MaxNameLength = 30;
        public const int DefaultPort = 25050;
        public const int Version = 9;
        public const int MaxChatLength = 100;
        public static readonly Color ColorGreen = new Color(0.48f, 0.74f, 0.45f);
        public static readonly Color ColorRed = new Color(0.74f, 0.48f, 0.45f);
        public static readonly Color ColorBlue = new Color(0.54f, 0.58f, 0.75f);
        public const float UpdateRate = 30;
        public const int MoveDataChannel = 1;
        public const string MasterServerUrl = "http://master.gettingoverit.mp";
        public const int MaxServerNameLength = 40;
        public const int MaxPlayerLimit = 9999;
        public const uint SteamAppId = 240720;
    }
}
