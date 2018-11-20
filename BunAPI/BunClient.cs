using System;
using System.IO;
using System.Web;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;
using System.Net;
using System.Collections.Generic;
using System.Linq;

namespace BunAPI
{
    public class StreamResponse
    {
        public Stream Stream { get; private set; }
        public HttpStatusCode StatusCode { get; private set; }

        public StreamResponse(HttpStatusCode status, Stream stream)
        {
            StatusCode = status;
            Stream = stream;
        }
    }

    public class FileListResponse
    {
        public IEnumerable<BunFile> Files { get; private set; }
        public HttpStatusCode StatusCode { get; private set; }

        public FileListResponse(HttpStatusCode status, IEnumerable<BunFile> files)
        {
            StatusCode = status;
            Files = files;
        }
    }

    public class BunClient
    {
        private const string storageEndpoint = "https://storage.bunnycdn.com";

        private readonly string apiKey;

        HttpClient client = new HttpClient();

        private string storageZone;
        public string StorageZone
        {
            get
            {
                return storageZone;
            }
            set
            {
                storageZone = HttpUtility.UrlEncode(value);
            }
        }

        /// <summary>
        /// If true, filenames given to the api and returned from it will be automatically encoded and decoded.
        /// When true, this disables the ability to specify "folders" by using slashes in filenames.
        /// </summary>
        public bool AutoEncodeFilenames { get; set; } = true;

        public BunClient(string apiKey, string storageZone)
        {
            this.apiKey = HttpUtility.UrlEncode(apiKey);
            StorageZone = storageZone;
        }

        private string BuildUri(string path)
        {
            if (AutoEncodeFilenames)
            {
                path = HttpUtility.UrlEncode(path);
            }

            return $"{storageEndpoint}/{StorageZone}/{path}?AccessKey={apiKey}";
        }

        /// <summary>
        /// Returns a list of files stored at the current StorageZone.
        /// This does not recurse into subdirectories.
        /// </summary>
        public async Task<FileListResponse> ListFiles(CancellationToken cancelToken = default(CancellationToken))
        {
            var result = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, BuildUri(string.Empty)), HttpCompletionOption.ResponseHeadersRead, cancelToken);
            var files = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<BunFile>>(await result.Content.ReadAsStringAsync());

            // Handle decoding filenames lazily.
            if (AutoEncodeFilenames)
            {
                files = files.Select(x =>
                {
                    x.ObjectName = HttpUtility.UrlDecode(x.ObjectName);
                    return x;
                });
            }

            return new FileListResponse(result.StatusCode, files);
        }

        /// <summary>
        /// Returns an object containing the status code and a data stream from the given filename target.
        /// </summary>
        public async Task<StreamResponse> GetFile(string filename, CancellationToken cancelToken = default(CancellationToken))
        {
            var result = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, BuildUri(filename)), HttpCompletionOption.ResponseHeadersRead, cancelToken);
            return new StreamResponse(result.StatusCode, await result.Content.ReadAsStreamAsync());
        }

        /// <summary>
        /// Puts the given stream content from position zero to the given filename target. If the file exists, it is overwritten.
        /// </summary>
        public async Task<HttpStatusCode> PutFile(Stream content, string filename, CancellationToken cancelToken = default(CancellationToken))
        {
            content.Position = 0;
            var result = await client.SendAsync(new HttpRequestMessage(HttpMethod.Put, BuildUri(filename)) { Content = new StreamContent(content) }, HttpCompletionOption.ResponseHeadersRead, cancelToken);
            return result.StatusCode;
        }

        /// <summary>
        /// Puts a text file with the given content to the given filename target. If the file exists, it is overwritten.
        /// </summary>
        public async Task<HttpStatusCode> PutFile(string content, string filename, CancellationToken cancelToken = default(CancellationToken))
        {
            using (var ms = new MemoryStream())
            using (var writer = new StreamWriter(ms))
            {
                writer.Write(content);
                writer.Flush();
                return await PutFile(ms, filename);
            }
        }

        /// <summary>
        /// Deletes the file from the given filename target.
        /// </summary>
        public async Task<HttpStatusCode> DeleteFile(string filename, CancellationToken cancelToken = default(CancellationToken))
        {
            var result = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, BuildUri(filename)), HttpCompletionOption.ResponseHeadersRead, cancelToken);
            return result.StatusCode;
        }
    }
}
