using System.Collections.Generic;
using UnityEngine;

namespace ServerShared
{
    public static class SharedConstants
    {
        public const string AppName = "GOIMP";
        public const int MaxNameLength = 30;
        public const int DefaultPort = 25050;
        public const int Version = 5;
        public static readonly char[] AllowedCharacters;
        public const int MaxChatLength = 100;
        public static readonly Color ColorGreen = new Color(0.48f, 0.74f, 0.45f);
        public static readonly Color ColorRed = new Color(0.74f, 0.48f, 0.45f);
        public static readonly Color ColorBlue = new Color(0.54f, 0.58f, 0.75f);
        public const float UpdateRate = 30;
        public const int MoveDataChannel = 1;

        static SharedConstants()
        {
            string allowedCharactersString = "abcdefghijklmnopqrstuvxyzåäö" +
                                             "ABCDEFGHIJKLMNOPQRSTUVXYZÅÄÖ" +
                                             "0123456789" +
                                             "!\"#¤%&/()=?`´@£$€{[]}\\^ ";

            List<char> allowedCharacters = new List<char>();
            foreach (char ch in allowedCharactersString)
            {
                allowedCharacters.Add(ch);
            }

            AllowedCharacters = allowedCharacters.ToArray();
        }
    }
}
