using System.Collections.Generic;

namespace ServerShared
{
    public static class SharedConstants
    {
        public const string AppName = "GOIMP";
        public const int MaxNameLength = 30;
        public const int DefaultPort = 25050;
        public const int Version = 1;
        public const string PublicServerHost = "skippy.pizza";
        public const int PublicServerPort = DefaultPort;
        public static readonly char[] AllowedCharacters;

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
