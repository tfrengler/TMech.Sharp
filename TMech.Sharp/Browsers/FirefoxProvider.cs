using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Compressors.BZip2;
using SharpCompress.Readers;
using System;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace TMech.Utils
{
    /// <summary>
    /// <para>A service that allows you to maintain the latest <b>stable</b> version of Firefox for <c>Windows 64-bit</c> and <c>Linux 64-bit</c> in a directory of your choice. It handles checking versions as well as downloading, and extracting the latest release.</para>
    /// </summary>
    public sealed class FirefoxProvider : IDisposable
    {
        public DirectoryInfo InstallLocation { get; }

        private readonly HttpClient HttpClient;
        private bool IsDisposed;
        private const UnixFileMode UnixFilePermissions = UnixFileMode.UserExecute | UnixFileMode.UserWrite | UnixFileMode.UserRead | UnixFileMode.GroupRead;

        public const string BrowserVersionFileName = "BROWSER_VERSION";
        public const string DriverVersionFileName = "DRIVER_VERSION";

        #region Github data structures

        private sealed record GithubApiRelease
        {
            [JsonPropertyName("tag_name")]
            public string TagName { get; set; } = string.Empty;

            [JsonPropertyName("assets")]
            public GithubApiAsset[] Assets { get; set; } = Array.Empty<GithubApiAsset>();
        }

        private sealed record GithubApiAsset
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("browser_download_url")]
            public string DownloadUrl { get; set; } = string.Empty;
        }

        #endregion

        /// <summary>Sets or gets the timeouts for requests as well as downloading data. Defaults to 30 seconds.</summary>
        public TimeSpan RequestTimeout { get => HttpClient.Timeout; set => HttpClient.Timeout = value; }

        /// <summary>
        /// Creates a new instance of <see cref="FirefoxProvider"/> that can be used to maintain a standalone Firefox browser and webdriver for testing.
        /// </summary>
        /// <param name="installLocation">The folder where Firefox and the webdriver will be kept. Must exist or else an exception will be thrown.</param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public FirefoxProvider(DirectoryInfo installLocation)
        {
            ArgumentNullException.ThrowIfNull(installLocation);
            InstallLocation = installLocation;

            if (!InstallLocation.Exists) throw new DirectoryNotFoundException("Install directory does not exist: " + InstallLocation.FullName);
            HttpClient = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = false })
            {
                Timeout = TimeSpan.FromSeconds(30.0d)
            };
            HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("TMech.FirefoxProvider", "1.0"));
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
        /// Retrieves the version of Firefox currently installed in <see cref="InstallLocation"/>.
        /// </summary>
        /// <returns>A string representing the version of Firefox currently installed <i>or</i> if Firefox cannot be found - the version file - then an empty string is returned.</returns>
        public string GetCurrentInstalledBrowserVersion()
        {
            var VersionFile = new FileInfo(Path.Combine(InstallLocation.FullName, BrowserVersionFileName));
            if (!VersionFile.Exists) return string.Empty;

            return File.ReadAllText(VersionFile.FullName);
        }

        /// <summary>
        /// Retrieves the version of the webdriver currently installed in <see cref="InstallLocation"/>.
        /// </summary>
        /// <returns>A string representing the version of the webdriver currently installed <i>or</i> if the webdriver cannot be found - the version file - then an empty string is returned.</returns>
        public string GetCurrentInstalledDriverVersion()
        {
            var VersionFile = new FileInfo(Path.Combine(InstallLocation.FullName, DriverVersionFileName));
            if (!VersionFile.Exists) return string.Empty;

            return File.ReadAllText(VersionFile.FullName);
        }

        /// <summary>
        /// Returns the latest <b>stable</b> version of Firefox that is available online.
        /// </summary>
        public string GetLatestAvailableBrowserVersion(Platform platform)
        {
            return GetBrowserVersionAndDownloadURL(platform).ReadableVersion;
        }

        /// <summary>
        /// Returns the latest <b>stable</b> version of Firefox that is available online.
        /// </summary>
        public string GetLatestAvailableDriverVersion(Platform platform)
        {
            return GetDriverVersionAndDownloadURL(platform).ReadableVersion;
        }

        /// <summary>
        /// <para>Downloads and extracts Firefox into <see cref="InstallLocation"/> if the currently installed version is lower than the latest available version <i>or</i> if Firefox is not installed.</para>
        /// <b>NOTE:</b> If there is already a version of Firefox in <see cref="InstallLocation"/> it will not be removed first! Existing files will merely be overwritten. This might leave certain version-specific files behind.
        /// </summary>
        /// <param name="force">Whether to force Firefox to be downloaded and installed even if the installed version is already the newest.</param>
        /// <returns><see langword="true"/> if Firefox was downloaded and installed, <see langword="false"/> otherwise.</returns>
        public bool DownloadLatestBrowserVersion(Platform platform, bool force = false)
        {
            BinaryDownloadAssetData DownloadAssetData = GetBrowserVersionAndDownloadURL(platform);
            string CurrentVersion = GetCurrentInstalledBrowserVersion();

            Version CurrentVersionComparable = Version.FromString(CurrentVersion);

            if (!force && CurrentVersionComparable.CompareTo(DownloadAssetData.Version) >= 0) return false;

            var Request = new HttpRequestMessage()
            {
                RequestUri = DownloadAssetData.DownloadUri,
                Method = HttpMethod.Get
            };

            HttpResponseMessage Response = HttpClient.Send(Request);
            Response.EnsureSuccessStatusCode();

            long? Downloadsize = Response.Content.Headers.ContentLength;
            if (Downloadsize is null)
            {
                throw new InvalidDataException($"A call to download the latest version of Firefox returned a response with 'Content-Length' not defined (URL: {DownloadAssetData.DownloadUri})");
            }

            var ResponseContentBuffer = new MemoryStream(Convert.ToInt32(Downloadsize));
            Response.Content.ReadAsStream().CopyTo(ResponseContentBuffer);
            Response.Dispose();
            ResponseContentBuffer.Position = 0;

            switch (platform)
            {
                case Platform.Win64:
                    ExtractWin64BrowserToInstallLocation(ResponseContentBuffer);
                    break;
                case Platform.Linux64:
                    ExtractLinux64BrowserToInstallLocation(ResponseContentBuffer);
                    break;

                default:
                    throw new InvalidDataException($"Argument '{nameof(platform)}' does not have a valid value: " + platform);
            }

            File.WriteAllText(Path.Combine(InstallLocation.FullName, BrowserVersionFileName), DownloadAssetData.ReadableVersion);

            return true;
        }

        /// <summary>
        /// <para>Downloads and extracts Firefox's webdriver into <see cref="InstallLocation"/> if the currently installed version is lower than the latest available version <i>or</i> if the webdriver is not installed.</para>
        /// <b>NOTE:</b> If there is already a version of the webdriver in <see cref="InstallLocation"/> it will be overwritten.
        /// </summary>
        /// <param name="force">Whether to force the webdriver to be downloaded and installed even if the installed version is already the newest.</param>
        /// <returns><see langword="true"/> if the webdriver was downloaded and installed, <see langword="false"/> otherwise.</returns>
        public bool DownloadLatestDriverVersion(Platform platform, bool force = false)
        {
            BinaryDownloadAssetData DownloadAssetData = GetDriverVersionAndDownloadURL(platform);
            string CurrentVersion = GetCurrentInstalledDriverVersion();

            Version CurrentVersionComparable = Version.FromString(CurrentVersion);

            if (!force && CurrentVersionComparable.CompareTo(DownloadAssetData.Version) >= 0) return false;

            var Request = new HttpRequestMessage()
            {
                RequestUri = DownloadAssetData.DownloadUri,
                Method = HttpMethod.Get
            };

            HttpResponseMessage Response = HttpClient.Send(Request);
            if (Response.StatusCode != System.Net.HttpStatusCode.Redirect)
            {
                throw new InvalidDataException($"A call to download the latest webdriver returned a response with a status code not '302' but rather '{Response.StatusCode}' (URL: {DownloadAssetData.DownloadUri})");
            }

            Uri? DownloadUri = Response.Headers.Location;
            if (DownloadUri is null)
            {
                throw new InvalidDataException($"A call to download the latest webdriver returned a response with an empty 'Location'-header (URL: {DownloadAssetData.DownloadUri})");
            }
            Response.Dispose();

            Request = new HttpRequestMessage()
            {
                RequestUri = DownloadUri,
                Method = HttpMethod.Get
            };

            Response = HttpClient.Send(Request);
            Response.EnsureSuccessStatusCode();

            string? ContentType = Response.Content.Headers.ContentType is null ? string.Empty : Response.Content.Headers.ContentType?.MediaType;

            if (ContentType is null || (ContentType is not null && !ContentType.Equals("application/octet-stream", StringComparison.InvariantCulture)))
            {
                throw new InvalidDataException($"A call to download the latest webdriver returned a response with 'Content-Type'-header not being 'application/octet-stream' but rather '{ContentType}' (URL: {DownloadAssetData.DownloadUri})");
            }

            long? Downloadsize = Response.Content.Headers.ContentLength;
            if (Downloadsize is null)
            {
                throw new InvalidDataException($"A call to download the latest webdriver returned a response with 'Content-Length' not defined (URL: {DownloadAssetData.DownloadUri})");
            }

            var ResponseContentBuffer = new MemoryStream(Convert.ToInt32(Downloadsize));
            Response.Content.ReadAsStream().CopyTo(ResponseContentBuffer);
            Response.Dispose();
            ResponseContentBuffer.Position = 0;

            switch (platform)
            {
                case Platform.Win64:
                    ZipFile.ExtractToDirectory(ResponseContentBuffer, InstallLocation.FullName, true);
                    break;

                case Platform.Linux64:
                    ExtractLinux64DriverToInstallLocation(ResponseContentBuffer);
                    break;

                default:
                    throw new InvalidDataException($"Argument '{nameof(platform)}' does not have a valid value: " + platform);
            }

            File.WriteAllText(Path.Combine(InstallLocation.FullName, DriverVersionFileName), DownloadAssetData.ReadableVersion);

            return true;
        }

        #region PRIVATE

        private void ExtractLinux64DriverToInstallLocation(MemoryStream driverArchiveStream)
        {
            using (var GZipStream = new GZipStream(driverArchiveStream, CompressionMode.Decompress))
            {
                TarFile.ExtractToDirectory(GZipStream, InstallLocation.FullName, true);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                File.SetUnixFileMode(Path.Combine(InstallLocation.FullName, "geckodriver"), UnixFilePermissions);
            }
        }

        private void ExtractWin64BrowserToInstallLocation(MemoryStream browserArchiveStream)
        {
            MemoryStream ArchiveBuffer = ExtractArchiveFromWinInstaller(browserArchiveStream);

            IArchive Archive = ArchiveFactory.Open(ArchiveBuffer);
            using IReader Reader = Archive.ExtractAllEntries();
            var Options = new ExtractionOptions() { ExtractFullPath = true, Overwrite = true };

            const string WindowsArchiveDirectoryPrefix = "core/";

            while (Reader.MoveToNextEntry())
            {
                if (Reader.Entry.IsDirectory) continue;
                string? EntryName = Reader.Entry.Key;
                Debug.Assert(EntryName is not null);
                if (!EntryName.StartsWith(WindowsArchiveDirectoryPrefix)) continue;

                string SanitizedName = EntryName.Replace(WindowsArchiveDirectoryPrefix, "");

                string FinalName = Path.Combine(InstallLocation.FullName, SanitizedName);
                Debug.Assert(!string.IsNullOrWhiteSpace(FinalName));

                new FileInfo(FinalName).Directory?.Create();
                Reader.WriteEntryToFile(FinalName, Options);
            }

            File.Delete(Path.Combine(InstallLocation.FullName, "updater.exe"));
        }

        private void ExtractLinux64BrowserToInstallLocation(MemoryStream browserArchiveStream)
        {
            using (var BZ2Stream = new BZip2Stream(browserArchiveStream, SharpCompress.Compressors.CompressionMode.Decompress, false))
            {
                var DirectoryPrefixRegex = new Regex("^firefox/");
                using var ArchiveReader = new TarReader(BZ2Stream, false);
                TarEntry? CurrentEntry = ArchiveReader.GetNextEntry();

                while (CurrentEntry is not null)
                {
                    FileInfo FileDestination = new FileInfo(Path.Combine(InstallLocation.FullName, DirectoryPrefixRegex.Replace(CurrentEntry.Name, "")));
                    FileDestination.Directory?.Create();

                    CurrentEntry.ExtractToFile(FileDestination.FullName, true);
                    CurrentEntry = ArchiveReader.GetNextEntry();
                }
            }

            File.Delete(Path.Combine(InstallLocation.FullName, "updater"));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                File.SetUnixFileMode(Path.Combine(InstallLocation.FullName, "firefox"), UnixFilePermissions);
                File.SetUnixFileMode(Path.Combine(InstallLocation.FullName, "crashreporter"), UnixFilePermissions);
            }
        }

        private BinaryDownloadAssetData GetBrowserVersionAndDownloadURL(Platform platform)
        {
            string PlatformName = platform switch
            {
                Platform.Win64 => "win64",
                Platform.Linux64 => "linux64",
                _ => throw new InvalidDataException($"Argument '{nameof(platform)}' does not have a valid value: " + platform)
            };

            var OriginalDownloadUri = new Uri($"https://download.mozilla.org/?product=firefox-latest&os={PlatformName}&lang=en-US");
            var Request = new HttpRequestMessage(HttpMethod.Head, OriginalDownloadUri);
            var Response = HttpClient.Send(Request, HttpCompletionOption.ResponseHeadersRead);

            if (Response.StatusCode != System.Net.HttpStatusCode.Redirect)
            {
                throw new InvalidDataException($"A GET call to determine the latest browser version returned a response with a status code not '302' but rather '{Response.StatusCode}' (URL: {OriginalDownloadUri})");
            }

            Uri? DownloadUri = Response.Headers.Location;
            if (DownloadUri is null)
            {
                throw new InvalidDataException($"A GET call to determine the latest browser version returned a response with an empty 'Location'-header (URL: {OriginalDownloadUri})");
            }

            var VersionCapture = new Regex("releases/(.+?)/").Match(DownloadUri.ToString());
            Debug.Assert(VersionCapture.Success && VersionCapture.Groups.Count > 1);

            string VersionString = VersionCapture.Groups[1].Value.Trim();
            if (VersionString.Length == 0)
            {
                throw new InvalidDataException($"Trying to determine the latest browser version from the redirect-url failed. Expected a parsed version string, but got an empty string instead (URL: {DownloadUri})");
            }

            return new BinaryDownloadAssetData()
            {
                DownloadUri = DownloadUri,
                ReadableVersion = VersionString,
                Version = Version.FromString(VersionString)
            };
        }

        private BinaryDownloadAssetData GetDriverVersionAndDownloadURL(Platform platform)
        {
            string DriverFileNameFragment = platform switch
            {
                Platform.Win64 => "win64.zip",
                Platform.Linux64 => "linux64.tar.gz",
                _ => throw new InvalidDataException($"Argument '{nameof(platform)}' does not have a valid value: " + platform)
            };

            var WebdriverGithubReleasesUri = new Uri("https://api.github.com/repos/mozilla/geckodriver/releases");
            var Request = new HttpRequestMessage(HttpMethod.Get, WebdriverGithubReleasesUri);
            Request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var Response = HttpClient.Send(Request);
            Response.EnsureSuccessStatusCode();

            string JsonResponseContent = Response.Content.ReadAsStringAsync().GetAwaiter().GetResult().Trim();
            if (JsonResponseContent.Length == 0)
            {
                throw new InvalidDataException($"A GET call to determine the latest webdriver version returned a response with an empty body (URL: {WebdriverGithubReleasesUri})");
            }

            GithubApiRelease[]? GithubResponseData = JsonSerializer.Deserialize<GithubApiRelease[]>(JsonResponseContent);
            if (GithubResponseData is null || (GithubResponseData is not null && GithubResponseData.Length == 0))
            {
                throw new InvalidDataException($"When deserializing the response JSON from a call to determine the latest webdriver version we got null or empty object (URL: {WebdriverGithubReleasesUri})");
            }
            Debug.Assert(GithubResponseData is not null);

            string LastVersionString = GithubResponseData[0].TagName;
            Version LastParsedVersion = Version.FromString(LastVersionString);
            GithubApiAsset[] NewestVersionAssets = GithubResponseData[0].Assets;

            for (int Index = 1; Index < GithubResponseData.Length; Index++)
            {
                string VersionString = GithubResponseData[Index].TagName;
                Version ParsedVersion = Version.FromString(VersionString);

                if (ParsedVersion.CompareTo(LastParsedVersion) > 0)
                {
                    LastVersionString = VersionString;
                    LastParsedVersion = ParsedVersion;
                    NewestVersionAssets = GithubResponseData[Index].Assets;
                }
            }

            string NewestVersionDownloadUrl = NewestVersionAssets.Single(x => x.Name.EndsWith(DriverFileNameFragment)).DownloadUrl;

            return new BinaryDownloadAssetData()
            {
                Version = LastParsedVersion,
                ReadableVersion = LastVersionString,
                DownloadUri = new Uri(NewestVersionDownloadUrl)
            };
        }

        /// <summary>
        /// Despite Firefox being portable it comes as an installer which is a 7-zip self-extracting archive.
        /// By searching through the byte stream we can find where the installer data ends and extract the actual archive data
        /// and simply treat that as a normal 7-zip archive which can be extracted as per normal.
        /// </summary>
        private MemoryStream ExtractArchiveFromWinInstaller(MemoryStream setupBinaryStream)
        {
            int ArchiveStart = 0;
            const string MagicWord = "@InstallEnd@";

            Span<byte> Buffer = stackalloc byte[50];
            while (true)
            {
                int BytesRead = setupBinaryStream.Read(Buffer);
                if (BytesRead == 0)
                {
                    throw new InvalidOperationException("Reached the end of the Firefox Win64 installer. Expected to find magic marker signifying the end of a 7zsfx installer somewhere in the binary.");
                }

                int FoundAt = Encoding.UTF8.GetString(Buffer).IndexOf(MagicWord);

                if (FoundAt > -1)
                {
                    ArchiveStart = Convert.ToInt32(setupBinaryStream.Position - Buffer.Length + FoundAt + MagicWord.Length + 1);
                    break;
                }
            }

            Debug.Assert(ArchiveStart > 0);

            int ArchiveSize = Convert.ToInt32(setupBinaryStream.Length - ArchiveStart);
            var ArchiveBuffer = new byte[ArchiveSize];

            setupBinaryStream.Seek(ArchiveStart, SeekOrigin.Begin);
            setupBinaryStream.Read(ArchiveBuffer, 0, ArchiveSize);

            return new MemoryStream(ArchiveBuffer);
        }

        #endregion

        #region Destructors

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;

            GC.SuppressFinalize(this);
            HttpClient?.Dispose();
        }

        ~FirefoxProvider()
        {
            HttpClient?.Dispose();
        }

        #endregion
    }

}
