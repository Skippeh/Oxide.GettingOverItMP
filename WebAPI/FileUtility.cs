using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebAPI
{
    public static class FileUtility
    {
        /// <summary>
        /// Opens the specified file for reading/writing. Waits for the file to be unlocked if it's locked for a specified maximum amount of time.
        /// </summary>
        /// <param name="filePath">The path to the file to open.</param>
        /// <param name="maxWaitMs">The max amount of time in milliseconds to wait for the file to be unlocked. If this time expires null will be returned.</param>
        /// <returns></returns>
        public static async Task<FileStream> OpenFileAsync(string filePath, FileMode fileMode, int maxWaitMs, CancellationToken cancellationToken)
        {
            DateTime timeoutTime = DateTime.UtcNow.Add(TimeSpan.FromMilliseconds(maxWaitMs));

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var result = new FileStream(filePath, fileMode, FileAccess.ReadWrite);
                    return result;
                }
                catch (IOException ex)
                {
                    if ((DateTime.UtcNow - timeoutTime).TotalMilliseconds < 100)
                        return null;

                    await Task.Delay(100, cancellationToken);

                    if (DateTime.UtcNow >= timeoutTime)
                        return null;
                }
            }
        }
    }
}
