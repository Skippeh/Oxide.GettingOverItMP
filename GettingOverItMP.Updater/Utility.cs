using System;
using System.IO;
using System.Security.Cryptography;

namespace GettingOverItMP.Updater
{
    internal static class Utility
    {
        public static string GetFileHash(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            using (var md5 = MD5.Create())
            {
                using (var fileStream = File.OpenRead(filePath))
                {
                    var hashBytes = md5.ComputeHash(fileStream);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public static bool FileInUse(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            try
            {
                using (File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                }
            }
            catch (IOException)
            {
                return true;
            }

            return false;
        }
    }
}
