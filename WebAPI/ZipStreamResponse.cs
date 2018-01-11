using System;
using System.IO;
using System.IO.Compression;
using Nancy.Responses;

namespace WebAPI
{
    /// <summary>
    /// Response that returns the contents of a stream created from ZipArchiveEntry.Open. The stream and zip archive will be disposed after the response has ended.
    /// </summary>
    public class ZipStreamResponse : StreamResponse
    {
        private ZipArchive archive;

        public ZipStreamResponse(ZipArchive archive, Func<Stream> source, string contentType) : base(source, contentType)
        {
            this.archive = archive;
        }

        public override void Dispose()
        {
            base.Dispose();
            archive?.Dispose();
        }
    }
}
