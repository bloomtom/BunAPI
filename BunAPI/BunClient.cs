using System;
using System.IO;
using System.Web;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using HttpProgress;

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

        /// <summary>
        /// Get or set the timeout for http operations.
        /// </summary>
        public TimeSpan Timeout
        {
            get { return client.Timeout; }
            set { client.Timeout = value; }
        }

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
        /// If set true, filenames given to the api and returned from it will be automatically encoded and decoded (for URL safety).
        /// This may mangle your filenames pretty badly though.
        /// </summary>
        public bool AutoEncodeFilenames { get; set; } = false;

        /// <summary>
        /// Create a client with a default key and storage zone.
        /// </summary>
        public BunClient(string apiKey, string storageZone)
        {
            client.Timeout = TimeSpan.FromHours(4);
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
        /// <param name="cancelToken">A cancel token for aborting the operation.</param>
        /// <returns>Returns 200 OK on success and 401 Unauthorized on failure.</returns>
        public async Task<FileListResponse> ListFiles(CancellationToken cancelToken = default(CancellationToken))
        {
            var result = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, BuildUri(string.Empty)), HttpCompletionOption.ResponseHeadersRead, cancelToken);

            if (result.StatusCode == HttpStatusCode.OK)
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
        /// Returns the http status code for a get operation and copies the retrieved content to the given output stream.
        /// </summary>
        /// <param name="filename">The remote file name, including any virtual folders in the path.</param>
        /// <param name="output">The stream to read data into. Must support writing.</param>
        /// <param name="progress">A progress callback which will fire on every buffer cycle. Take care not to perform expensive operations or transfer performance will suffer.</param>
        /// <param name="cancelToken">A cancel token for aborting the operation.</param>
        /// <returns>Returns 200 OK on success and 401 Unauthorized or 404 NotFound on failure. The stream is populated regardless, and contains a json message on failure.</returns>
        public async Task<HttpStatusCode> GetFile(string filename, Stream output, IProgress<ICopyProgress> progress = null, CancellationToken cancelToken = default(CancellationToken))
        {
            if (!output.CanWrite)
            {
                throw new ArgumentException("Output stream must be writable.", "output");
            }
            using (var r = await client.GetAsync(BuildUri(filename), output, progress, cancelToken))
            {
                return r.StatusCode;
            }
        }

        /// <summary>
        /// Returns an object containing the status code and a data stream from the given filename target.
        /// Since this method returns a stream directly with no copying, copy progress cannot be given.
        /// </summary>
        /// <param name="filename">The remote file name, including any virtual folders in the path.</param>
        /// <param name="cancelToken">A cancel token for aborting the operation.</param>
        /// <returns>Returns 200 OK on success and 401 Unauthorized or 404 NotFound on failure. The stream is populated regardless, and contains a json message on failure.</returns>
        public async Task<StreamResponse> GetFile(string filename, CancellationToken cancelToken = default(CancellationToken))
        {
            var r = await client.GetAsync(BuildUri(filename), cancelToken);
            return new StreamResponse(r.StatusCode, await r.Content.ReadAsStreamAsync());
        }

        /// <summary>
        /// Puts the given stream content from position zero to the given filename target. If the file exists, it is overwritten.
        /// </summary>
        /// <param name="content">A binary data stream to write. Must support reading. If the stream can seek, the position is set to zero before starting.</param>
        /// <param name="filename">The remote file name to store, including any virtual folders in the desired path.</param>
        /// <param name="startPosition">The postion to seek the stream to before starting. Only takes effect if the stream can seek.</param>
        /// <param name="progress">A progress callback which will fire on every buffer cycle. Take care not to perform expensive operations or transfer performance will suffer.</param>
        /// <param name="cancelToken">A cancel token for aborting the operation.</param>
        /// <param name="autoDisposeStream">When set true, the content stream will be disposed after successful consumption.</param>
        /// <returns>Returns 201 Created on success and 400 BadRequest on failure.</returns>
        public async Task<HttpStatusCode> PutFile(Stream content, string filename, bool autoDisposeStream = false, IProgress<ICopyProgress> progress = null, long startPosition = 0, CancellationToken cancelToken = default(CancellationToken))
        {
            if (content.CanSeek)
            {
                content.Position = startPosition;
            }
            using (var result = await client.PutAsync(BuildUri(filename), content, autoDisposeStream, progress, 0, cancelToken))
            {
                return result.StatusCode;
            }
        }

        /// <summary>
        /// Puts a text file with the given content to the given filename target. If the file exists, it is overwritten.
        /// </summary>
        /// <param name="content">A string to write to the endpoint. This will be encoded as UTF-8 and sent as a data stream.</param>
        /// <param name="filename">The remote file name to store, including any virtual folders in the desired path.</param>
        /// <param name="progress">A progress callback which will fire on every buffer cycle. Take care not to perform expensive operations or transfer performance will suffer.</param>
        /// <param name="cancelToken">A cancel token for aborting the operation.</param>
        /// <returns>Returns 201 Created on success and 401 Unauthorized or 400 BadRequest on failure.</returns>
        public async Task<HttpStatusCode> PutFile(string content, string filename, IProgress<ICopyProgress> progress = null, CancellationToken cancelToken = default(CancellationToken))
        {
            using (var ms = new MemoryStream())
            using (var writer = new StreamWriter(ms))
            {
                writer.Write(content);
                writer.Flush();
                return await PutFile(ms, filename, false, progress, 0, cancelToken);
            }
        }

        /// <summary>
        /// Deletes the file from the given filename target.
        /// </summary>
        /// <param name="filename">The remote file name, including any virtual folders in the path.</param>
        /// <param name="cancelToken">A cancel token for aborting the operation.</param>
        /// <returns>Returns 200 OK on success and 404 NotFound, 401 Unauthorized or 400 BadRequest on failure.</returns>
        public async Task<HttpStatusCode> DeleteFile(string filename, CancellationToken cancelToken = default(CancellationToken))
        {
            var result = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, BuildUri(filename)), HttpCompletionOption.ResponseHeadersRead, cancelToken);
            return result.StatusCode;
        }
    }
}
