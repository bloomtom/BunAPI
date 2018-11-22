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
    /// <summary>
    /// Encapsulates an HTTP status code and a data stream for the resource.
    /// If the status code is 200 (OK) the download operation was started. Close the stream to end it.
    /// </summary>
    public class StreamResponse
    {
        /// <summary>
        /// The download data stream. Data is loaded over the wire as the stream is read.
        /// </summary>
        public Stream Stream { get; private set; }
        /// <summary>
        /// The status code returned from BunnyCDN for the operation.
        /// </summary>
        public HttpStatusCode StatusCode { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="status"></param>
        /// <param name="stream"></param>
        public StreamResponse(HttpStatusCode status, Stream stream)
        {
            StatusCode = status;
            Stream = stream;
        }
    }

    /// <summary>
    /// Encapsulates the result of a file listing operation.
    /// </summary>
    public class FileListResponse
    {
        /// <summary>
        /// A collection of files stored in a distribution group.
        /// </summary>
        public IEnumerable<BunFile> Files { get; private set; }
        /// <summary>
        /// The status code returned from BunnyCDN for the operation.
        /// </summary>
        public HttpStatusCode StatusCode { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="status"></param>
        /// <param name="files"></param>
        public FileListResponse(HttpStatusCode status, IEnumerable<BunFile> files)
        {
            StatusCode = status;
            Files = files;
        }
    }

    /// <summary>
    /// A simple client for interfacing with the BunnyCDN REST API.
    /// </summary>
    public class BunClient
    {
        private const string storageEndpoint = "https://storage.bunnycdn.com";

        private readonly string apiKey;

        private readonly HttpClient client = new HttpClient();

        private string storageZone;
        /// <summary>
        /// The storage zone to work with. Storage zones can be created and managed at https://bunnycdn.com/dashboard/storagezones
        /// The setter of this property automatically encodes the value into a URL safe form for API access.
        /// </summary>
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
        /// If true, filenames given to the api and returned from it will be automatically encoded and decoded (for URL safety).
        /// When true, this disables the ability to specify "folders" by using slashes in filenames.
        /// If you set this false, you will need to manually ensure URL unsafe characters aren't allowed
        /// </summary>
        public bool AutoEncodeFilenames { get; set; } = true;

        /// <summary>
        /// Create a client with a default key and storage zone.
        /// </summary>
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
        /// <returns>Returns 200 OK on success and 401 Unauthorized on failure.</returns>
        public async Task<FileListResponse> ListFiles(CancellationToken cancelToken = default(CancellationToken))
        {
            var result = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, BuildUri(string.Empty)), HttpCompletionOption.ResponseHeadersRead, cancelToken);

            if(result.StatusCode == HttpStatusCode.OK)
            {
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

            return new FileListResponse(result.StatusCode, Enumerable.Empty<BunFile>());
        }

        /// <summary>
        /// Returns an object containing the status code and a data stream from the given filename target.
        /// </summary>
        /// <returns>Returns 200 OK on success and 401 Unauthorized or 404 NotFound on failure. The stream is populated regardless, and contains a json message on failure.</returns>
        public async Task<StreamResponse> GetFile(string filename, CancellationToken cancelToken = default(CancellationToken))
        {
            var result = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, BuildUri(filename)), HttpCompletionOption.ResponseHeadersRead, cancelToken);
            return new StreamResponse(result.StatusCode, await result.Content.ReadAsStreamAsync());
        }

        /// <summary>
        /// Puts the given stream content from position zero to the given filename target. If the file exists, it is overwritten.
        /// </summary>
        /// <returns>Returns 201 Created on success and 400 BadRequest on failure.</returns>
        public async Task<HttpStatusCode> PutFile(Stream content, string filename, CancellationToken cancelToken = default(CancellationToken))
        {
            if (content.CanSeek)
            {
                content.Position = 0;
            }
            var result = await client.SendAsync(new HttpRequestMessage(HttpMethod.Put, BuildUri(filename)) { Content = new StreamContent(content) }, HttpCompletionOption.ResponseHeadersRead, cancelToken);
            return result.StatusCode;
        }

        /// <summary>
        /// Puts a text file with the given content to the given filename target. If the file exists, it is overwritten.
        /// </summary>
        /// <returns>Returns 201 Created on success and 401 Unauthorized or 400 BadRequest on failure.</returns>
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
        /// <returns>Returns 200 OK on success and 404 NotFound, 401 Unauthorized or 400 BadRequest on failure.</returns>
        public async Task<HttpStatusCode> DeleteFile(string filename, CancellationToken cancelToken = default(CancellationToken))
        {
            var result = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, BuildUri(filename)), HttpCompletionOption.ResponseHeadersRead, cancelToken);
            return result.StatusCode;
        }
    }
}
