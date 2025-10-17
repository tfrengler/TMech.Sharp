using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace TMech.Sharp.Browsers
{
    /// <summary>
    /// <para>A service that allows you to maintain the latest <b>stable</b> version of <b>Chrome for Testing</b> for <c>Windows 64-bit</c> and <c>Linux 64-bit</c> in a directory of your choice. It handles checking versions as well as downloading, and extracting the latest release.</para>
    /// <para>To learn more about Chrome for automation testing see <see href="https://googlechromelabs.github.io/chrome-for-testing/">here</see>.</para>
    /// </summary>
    public sealed class ChromeProvider : IDisposable
    {
        public DirectoryInfo InstallLocation { get; }
        public const string ManifestURL = "https://googlechromelabs.github.io/chrome-for-testing/last-known-good-versions-with-downloads.json";

        private readonly HttpClient HttpClient;
        private bool IsDisposed;
        private const string VersionFileName = "VERSION";
        private const UnixFileMode UnixFilePermissions = UnixFileMode.UserExecute | UnixFileMode.UserWrite | UnixFileMode.UserRead | UnixFileMode.GroupRead;

        #region Manifest JSON models
        private sealed record Manifest
        {
            [JsonPropertyName("channels")]
            public Channels Channels { get; init; } = new();
        }

        private sealed record Channels
        {
            [JsonPropertyName("Stable")]
            public Channel Stable { get; init; } = new();
        }

        private sealed record Channel
        {
            [JsonPropertyName("version")]
            public string Version { get; init; } = string.Empty;
            [JsonPropertyName("downloads")]
            public Downloads Downloads { get; init; } = new();
        }

        private sealed record Downloads
        {
            [JsonPropertyName("chrome")]
            public Download[] Chrome { get; init; } = Array.Empty<Download>();
            [JsonPropertyName("chromedriver")]
            public Download[] Chromedriver { get; init; } = Array.Empty<Download>();
        }

        private sealed record Download
        {
            [JsonPropertyName("platform")]
            public string Platform { get; init; } = string.Empty;
            [JsonPropertyName("url")]
            public string Url { get; init; } = string.Empty;
        }
        #endregion

        /// <summary>Sets or gets the timeouts for requests as well as downloading data. Defaults to 30 seconds.</summary>
        public TimeSpan RequestTimeout { get => HttpClient.Timeout; set => HttpClient.Timeout = value; }

        /// <summary>
        /// Creates a new instance of <see cref="ChromeProvider"/> that can be used to maintain a standalone Chrome browser and webdriver for testing.
        /// </summary>
        /// <param name="installLocation">The folder where Chrome and the webdriver will be kept. Must exist or else an exception will be thrown.</param>
        public ChromeProvider(DirectoryInfo installLocation)
        {
            Debug.Assert(installLocation != null);
            InstallLocation = installLocation;

            if (!InstallLocation.Exists) throw new DirectoryNotFoundException("Chrome install directory does not exist: " + installLocation.FullName);
            HttpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(30.0d)
            };
            HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("TMech.ChromeProvider", "1.0"));
        }

        /// <summary>
        /// Deletes all files and folders in <see cref="InstallLocation"/> but not the directory itself.
        /// </summary>
        public void ClearInstallLocation()
        {
            foreach (FileInfo CurrentFile in InstallLocation.EnumerateFiles())
            {
                CurrentFile.Delete();
            }
            foreach (DirectoryInfo CurrentDirectory in InstallLocation.EnumerateDirectories())
            {
                CurrentDirectory.Delete(true);
            }
        }

        /// <summary>
        /// Retrieves the version of Chrome currently installed in <see cref="InstallLocation"/>.
        /// </summary>
        /// <returns>A string representing the version of Chrome currently installed <i>or</i> if Chrome cannot be found - the version file - then an empty string is returned.</returns>
        public string GetCurrentInstalledVersion()
        {
            var VersionFile = new FileInfo(Path.Combine(InstallLocation.FullName, VersionFileName));
            if (!VersionFile.Exists) return string.Empty;

            return File.ReadAllText(VersionFile.FullName);
        }

        /// <summary>
        /// Returns the latest <b>stable</b> version of Chrome that is available online.
        /// </summary>
        public string GetLatestAvailableVersion(Platform platform)
        {
            return GetBinaryAssetData(platform)[0].ReadableVersion;
        }

        /// <summary>
        /// <para>Downloads and extracts Chrome and its webdriver (their versions always match each other) into <see cref="InstallLocation"/> if the currently installed version is lower than the latest available version <i>or</i> if Chrome is not installed.</para>
        /// <b>NOTE:</b> If there is already a version of Chrome in <see cref="InstallLocation"/> it will not be removed first! Existing files will merely be overwritten. This might leave certain version-specific files behind.
        /// </summary>
        /// <param name="force">Whether to force Chrome to be downloaded and installed even if the installed version is already the newest.</param>
        /// <returns><see langword="true"/> if Chrome was downloaded and installed, <see langword="false"/> otherwise.</returns>
        public bool DownloadLatestVersion(Platform platform, bool skipDriver = false, bool force = false)
        {
            BinaryDownloadAssetData[] AssetData = GetBinaryAssetData(platform);
            Debug.Assert(AssetData.Length is not 0);

            Uri[] DownloadURLs = skipDriver ? new Uri[] { AssetData[0].DownloadUri } : new Uri[] { AssetData[0].DownloadUri, AssetData[1].DownloadUri };
            Version CurrentParsedVersion = Version.FromString(GetCurrentInstalledVersion());

            if (!force && CurrentParsedVersion.CompareTo(AssetData[0].Version) >= 0) return false;

            string ExpectedMimeType = platform switch
            {
                Platform.Win64 => "application/x-zip-compressed",
                Platform.Linux64 => "application/zip",
                _ => throw new InvalidDataException("I'm here to satisfy the compiler because this is already handled in the call to GetBrowserVersionAndDownloadURL...")
            };

            foreach (Uri CurrentDownloadURL in DownloadURLs)
            {
                var Request = new HttpRequestMessage()
                {
                    RequestUri = CurrentDownloadURL,
                    Method = HttpMethod.Get
                };

                HttpResponseMessage Response = HttpClient.Send(Request);
                Response.EnsureSuccessStatusCode();

                string? ContentType = Response.Content.Headers.ContentType is null ? string.Empty : Response.Content.Headers.ContentType?.MediaType;

                if (ContentType is null || (ContentType is not null && !ContentType.Equals(ExpectedMimeType, StringComparison.InvariantCulture)))
                {
                    throw new InvalidDataException($"A call to download the latest version of Chrome or its webdriver returned a response with 'Content-Type' not '{ExpectedMimeType}' but rather '{ContentType}' (URL: {CurrentDownloadURL})");
                }

                long? Downloadsize = Response.Content.Headers.ContentLength;
                if (Downloadsize is null)
                {
                    throw new InvalidDataException($"A call to download the latest version of Chrome or its webdriver returned a response with 'Content-Length' not defined (URL: {CurrentDownloadURL})");
                }

                var Buffer = new MemoryStream(Convert.ToInt32(Downloadsize));
                Response.Content.ReadAsStream().CopyTo(Buffer);
                Response.Dispose();
                Buffer.Position = 0;
                IReader? Reader = null;

                try
                {
                    IArchive Archive = ArchiveFactory.Open(Buffer);
                    var Options = new ExtractionOptions() { ExtractFullPath = true, Overwrite = true };

                    foreach(var entry in Archive.Entries)
                    {
                        string? RootDir = entry.Key?.Split('/').First();
                        Debug.Assert(RootDir is not null);

                        string? SanitizedName = entry.Key?.Replace(RootDir, "").TrimStart('/');
                        Debug.Assert(SanitizedName is not null);

                        if (entry.IsDirectory) continue;

                        string FinalName = Path.Combine(InstallLocation.FullName, SanitizedName);
                        Debug.Assert(!string.IsNullOrWhiteSpace(FinalName));

                        new FileInfo(FinalName).Directory?.Create();
                        entry.WriteToFile(FinalName, Options);
                    }
                }
                finally
                {
                    Reader?.Dispose();
                }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                File.SetUnixFileMode(Path.Combine(InstallLocation.FullName, "chrome"), UnixFilePermissions);
                File.SetUnixFileMode(Path.Combine(InstallLocation.FullName, "chrome_crashpad_handler"), UnixFilePermissions);

                if (!skipDriver)
                {
                    File.SetUnixFileMode(Path.Combine(InstallLocation.FullName, "chromedriver"), UnixFilePermissions);
                }
            }

            File.WriteAllText(Path.Combine(InstallLocation.FullName, VersionFileName), AssetData[0].ReadableVersion);

            return true;
        }

        // Index 0 is browser, index 1 is driver
        private BinaryDownloadAssetData[] GetBinaryAssetData(Platform platform)
        {
            string PlatformName = platform switch
            {
                Platform.Win64 => "win64",
                Platform.Linux64 => "linux64",
                _ => throw new InvalidDataException($"Argument '{nameof(platform)}' does not have a valid value: " + platform)
            };

            var Request = new HttpRequestMessage()
            {
                RequestUri = new Uri(ManifestURL),
                Method = HttpMethod.Get
            };

            Request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage Response = HttpClient.Send(Request);
            Response.EnsureSuccessStatusCode();

            var Timeout = new CancellationTokenSource(HttpClient.Timeout.Milliseconds);
            string ResponseContent = Response.Content.ReadAsStringAsync(Timeout.Token).GetAwaiter().GetResult();
            Response.Dispose();

            Manifest? ReleaseData;
            try
            {
                ReleaseData = JsonSerializer.Deserialize<Manifest>(ResponseContent);
            }
            catch (JsonException error)
            {
                throw new JsonException($"Error when trying to determine latest available Chrome version online. Failed to deserialize GitHub response as JSON ({ManifestURL})", error);
            }

            Debug.Assert(ReleaseData is not null);

            string VersionString = ReleaseData.Channels.Stable.Version;
            Version ParsedVersion = Version.FromString(VersionString);
            var ChromeUrl = ReleaseData.Channels.Stable.Downloads.Chrome.Single(current => current.Platform == PlatformName).Url;
            var ChromedriverUrl = ReleaseData.Channels.Stable.Downloads.Chromedriver.Single(current => current.Platform == PlatformName).Url;

            return new BinaryDownloadAssetData[]
            {
                new BinaryDownloadAssetData()
                {
                    ReadableVersion = VersionString,
                    Version = ParsedVersion,
                    DownloadUri = new Uri(ChromeUrl)
                },
                new BinaryDownloadAssetData()
                {
                    ReadableVersion = VersionString,
                    Version = ParsedVersion,
                    DownloadUri = new Uri(ChromedriverUrl)
                }
            };
        }

        /// <summary>
        /// Releases all potentially open http connections used by this instance.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            HttpClient?.Dispose();
        }
    }

}
