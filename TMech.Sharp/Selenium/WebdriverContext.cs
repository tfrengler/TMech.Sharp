using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;

namespace TMech.Sharp.Selenium
{
    /// <summary>
    /// A context acting as a wrapper around a Selenium <see cref="IWebDriver"/>-instance, with bootstrapping methods for getting a webdriver up and running.
    /// </summary>
    public sealed class WebdriverContext : IDisposable
    {
        /// <summary>
        /// Creates a webdriver to be used remotely, primarily intended for when you plan on using <see href="https://www.selenium.dev/documentation/grid/">Selenium Grid</see> to run tests against a browser on a remote machine.
        /// </summary>
        /// <param name="remoteUrl">The public address of the remote server.</param>
        /// <returns>An instance that is configured, but not yet initialized, for running against the server at <paramref name="remoteUrl"/>.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static WebdriverContext CreateRemote(Browsers.Browser browser, Uri remoteUrl)
        {
            ArgumentNullException.ThrowIfNull(remoteUrl);
            Trace.TraceInformation("WebdriverContext: Creating a remote webdriver context for {0} running against: {1}", browser, remoteUrl.ToString());
            return new WebdriverContext(browser, remoteUrl);
        }

        /// <summary>
        /// Creates a webdriver to be used locally, running tests against a browser on this machine.
        /// </summary>
        /// <param name="service">The service used to manage the lifetime of the browser driver. The type of <see cref="OpenQA.Selenium.DriverService"/> determines what type of browser this webdriver context represents.</param>
        /// <param name="browserBinaryLocation">Optional. The location of the browser executable for starting the browser. If omitted then Selenium attempts to find the browser installed on the machine.</param>
        /// <returns>An instance that is configured, but not yet initialized, for running against a local browser.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static WebdriverContext CreateLocal(DriverService service, FileInfo? browserBinaryLocation = null)
        {
            ArgumentNullException.ThrowIfNull(service);
            Trace.TraceInformation("WebdriverContext: Creating a local webdriver context using driver service running on: {0}", service.ServiceUrl.ToString());
            return new WebdriverContext(service, browserBinaryLocation);
        }

        /// <summary>
        /// Creates a webdriver to be used locally, running tests against a browser on this machine.
        /// </summary>
        /// <param name="browser">The service used to manage the lifetime of the browser driver.</param>
        /// <param name="browserBinaryLocation">Optional. The location of the browser executable for starting the browser. If omitted then Selenium attempts to find the browser installed on the machine.</param>
        /// <returns>An instance that is configured, but not yet initialized, for running against a local browser.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static WebdriverContext CreateLocal(Browsers.Browser browser, FileInfo? browserBinaryLocation = null)
        {
            Trace.TraceInformation("WebdriverContext: Creating a local webdriver context using a default driver service ({0})", browser);

            DriverService Service = browser switch
            {
                Browsers.Browser.CHROME => ChromeDriverService.CreateDefaultService(),
                Browsers.Browser.FIREFOX => FirefoxDriverService.CreateDefaultService(),
                Browsers.Browser.EDGE => EdgeDriverService.CreateDefaultService(),
                _ => throw new InvalidOperationException($"Not a valid value for argument '{nameof(browser)}': " + browser)
            };

            return new WebdriverContext(Service, browserBinaryLocation);
        }

        /// <summary>
        /// Initializes the webdriver, which should start the browser driver and the browser itself. The <see cref="Webdriver"/> should be populated and ready for interaction once this method returns.
        /// Browser options are automatically configured with what is considered sensible defaults:
        /// <list type="bullet">
        ///     <item>Downloads are automatic, meaning no pop-up dialog asking if or where you want to download a file.</item>
        ///     <item>Firefox uses a new temporary, discardable profile that is automatically deleted after the browser is shut down (Chrome and Edge does this automatically).</item>
        ///     <item>The timeout for loading a webpage is set to <c>30</c> seconds. This can be changed later via <c>Webdriver.Manage().Timeouts().PageLoad</c></item>
        ///     <item>Proxy autodetect is set to <c>False</c> and type to <c>Direct</c>.</item>
        ///     <item>If the webdriver is remote, then a local file detector is created, to enable uploading locally hosted files to the remote server.</item>
        /// </list>
        /// </summary>
        /// <param name="headless">Whether to run the browser in headless or GUI mode. If running remotely this has no effect, and the browser is run in headless mode.</param>
        /// <param name="windowSize">The size of the browser window when running in headless mode. Defaults to 1920 x 1080.</param>
        /// <param name="browserArguments">Additional command-line arguments to pass to the browser upon starting it.</param>
        /// <param name="downloadsFolder">A folder to redirect all via the browser downloads to. Keep in mind that if the webdriver is remote then this points to a folder on the server, and not on the local machine. If omitted then downloads will be done according to the respective browser's configuration. Note that no files are removed from this folder during the lifetime of the webdriver.</param>
        /// <returns>A reference to this instance, initialized and ready for use.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public WebdriverContext Initialize(bool headless, Size? windowSize = null, string[]? browserArguments = null, DirectoryInfo? downloadsFolder = null)
        {
            IsHeadless = headless;
            DownloadsFolder = downloadsFolder;

            Trace.TraceInformation("WebdriverContext: Creating browser options ({0})", Browser);
            var Options = CreateOptions(browserArguments);

            if (IsRemote)
            {
                Trace.TraceInformation("WebdriverContext: Initializing remote webdriver instance");
                var ReturnData = new RemoteWebDriver(RemoteServerUrl, Options);
                ReturnData.FileDetector = new LocalFileDetector();
                _Webdriver = ReturnData;
            }
            else
            {
                Trace.TraceInformation("WebdriverContext: Initializing local webdriver instance");
                if (BrowserLocation is not null)
                {
                    if (!BrowserLocation.Exists)
                    {
                        throw new FileNotFoundException("Unable to locate the binary of the browser you passed", BrowserLocation.FullName);
                    }
                    Options.BinaryLocation = BrowserLocation.FullName;
                };

                _Webdriver = Browser switch
                {
                    Browsers.Browser.CHROME => new ChromeDriver((ChromeDriverService)DriverService!, (ChromeOptions)Options),
                    Browsers.Browser.FIREFOX => new FirefoxDriver((FirefoxDriverService)DriverService!, (FirefoxOptions)Options),
                    Browsers.Browser.EDGE => new EdgeDriver((EdgeDriverService)DriverService!, (EdgeOptions)Options),
                    _ => throw new InvalidOperationException()
                };
            }

            // Firefox likes to throw exceptions if you try and interact with it too quickly after the driver has been started...
            // Apparently Chrome has started throwing 'no execution context' as well so let's extend this to include ALL drivers
            Thread.Sleep(2000);

            if (IsHeadless || RemoteServerUrl is not null)
            {
                Size WindowSize = windowSize is null || (windowSize.HasValue && windowSize.Value == default) ? new Size(1920, 1080) : windowSize.Value;
                Trace.TraceInformation("WebdriverContext: Running in headless mode, set window size to {0}", WindowSize);
                Webdriver.Manage().Window.Size = WindowSize;
            }
            else
            {
                Trace.TraceInformation("WebdriverContext: Running in GUI mode, maximizing viewport");
                Webdriver.Manage().Window.Maximize();
            }

            Webdriver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30.0d);

            return this;
        }

        private WebdriverContext(Browsers.Browser browser, Uri remoteUrl)
        {
            Debug.Assert(remoteUrl is not null);

            Browser = browser;
            IsRemote = true;
            RemoteServerUrl = remoteUrl;
        }

        private WebdriverContext(DriverService service, FileInfo? binaryLocation = null)
        {
            Debug.Assert(service is not null);

            Browser = service switch
            {
                var x when x is ChromeDriverService => Browsers.Browser.CHROME,
                var x when x is FirefoxDriverService => Browsers.Browser.FIREFOX,
                var x when x is EdgeDriverService => Browsers.Browser.EDGE,
                _ => throw new InvalidOperationException()
            };

            IsRemote = false;
            DriverService = service;
            BrowserLocation = binaryLocation;
        }

        private bool IsDisposed = false;
        private readonly DriverService? DriverService;
        private IWebDriver? _Webdriver;

        /// <summary>
        /// The url of the remote server. Is <c>null</c> if the context is not configured for a remote webdriver.
        /// </summary>
        public Uri? RemoteServerUrl { get; }
        /// <summary>
        /// Whether the webdriver should be run with its GUI showing or not.
        /// </summary>
        public bool IsHeadless { get; private set; }
        /// <summary>
        /// Whether this context represents a remote webdriver for running against a server running on <see cref="RemoteServerUrl"/>/
        /// </summary>
        public bool IsRemote { get; }
        /// <summary>
        /// If browser file downloads are redirected from their standard location this is where they should be put.
        /// </summary>
        public DirectoryInfo? DownloadsFolder { get; private set; }
        /// <summary>
        /// If the executable of the browser that should be started is in a different place than the default then this is where it lives.
        /// </summary>
        public FileInfo? BrowserLocation { get; }
        /// <summary>
        /// Which browser this webdriver represents:
        /// <list type="bullet">
        ///     <item>If <see cref="Browsers.Browser.CHROME"/> then <see cref="Webdriver"/> is an instance of <see cref="ChromeDriver"/>.</item>
        ///     <item>If <see cref="Browsers.Browser.FIREFOX"/> then <see cref="Webdriver"/> is an instance of <see cref="FirefoxDriver"/>.</item>
        ///     <item>If <see cref="Browsers.Browser.EDGE"/> then <see cref="Webdriver"/> is an instance of <see cref="EdgeDriver"/>.</item>
        /// </list>
        /// </summary>
        public Browsers.Browser Browser { get; private set; }

        /// <summary>
        /// A reference to <see cref="IWebDriver"/>-instance this context is wrapped around once initialized. Will throw an exception if accessed before calling <see cref="Initialize"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public IWebDriver Webdriver
        {
            get { 
                if (_Webdriver is null) throw new InvalidOperationException("Cannot interact with webdriver because it is not initialized or it has been disposed");
                return _Webdriver; 
            }
        }

        private DriverOptions CreateOptions(string[]? browserArguments)
        {
            DriverOptions ReturnData;

            if (!IsRemote && DownloadsFolder is not null && !DownloadsFolder.Exists)
            {
                throw new DirectoryNotFoundException($"Download folder does not exist on the local machine: {DownloadsFolder.FullName}");
            }

            switch (Browser)
            {
                case Browsers.Browser.CHROME:
                    var ChromeOptions = new ChromeOptions();
                    if (browserArguments is not null) ChromeOptions.AddArguments(browserArguments);

                    // DO NOT use "--no-sandbox" as it causes chrome-processes to linger after shutdown!
                    ChromeOptions.AddUserProfilePreference("safebrowsing.enabled", "false");
                    ChromeOptions.AddUserProfilePreference("download.prompt_for_download", false);

                    if (DownloadsFolder is not null)
                    {
                        Trace.TraceInformation("WebdriverContext: Redirecting Chrome's download location ({0})", DownloadsFolder.FullName);
                        ChromeOptions.AddUserProfilePreference("download.directory_upgrade", true);
                        ChromeOptions.AddUserProfilePreference("download.default_directory", DownloadsFolder.FullName);
                    }

                    if (IsHeadless)
                    {
                        ChromeOptions.AddArgument("--headless=new");
                    }

                    ReturnData = ChromeOptions;
                    break;

                case Browsers.Browser.FIREFOX:
                    var FirefoxOptions = new FirefoxOptions();
                    FirefoxOptions.Profile = new FirefoxProfile() { DeleteAfterUse = true };

                    FirefoxOptions.AddAdditionalOption("browser.download.folderList", 2);
                    FirefoxOptions.AddAdditionalOption("browser.download.panel.show", false);
                    FirefoxOptions.AddAdditionalOption("browser.download.manager.showWhenStarting", false);
                    FirefoxOptions.AddAdditionalOption("browser.helperApps.alwaysAsk.force", false);
                    FirefoxOptions.AddAdditionalOption("browser.helperApps.neverAsk.saveToDisk",
                        string.Join(',', new string[]
                        {
                            "application/octet-stream",
                            "text/octet-stream",
                            "text/plain",
                            "application/json",
                            "application/xml",
                            "text/csv",
                            "text/xml",
                            "application/zip",
                            "application/gzip",
                            "application/x-gzip",
                            "application/x-tar",
                            "image/bmp",
                            "image/jpeg",
                            "image/png",
                            "application/pdf",
                            "application/msword",
                            "application/vnd.ms-excel",
                            "application/vnd.ms-powerpoint",
                            "application/vnd.openxmlformats-officedocument.wordprocessingml.document,",
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                            "application/vnd.oasis.opendocument.presentation",
                            "application/vnd.oasis.opendocument.spreadsheet",
                            "application/vnd.oasis.opendocument.text"
                        })
                    );

                    if (DownloadsFolder is not null)
                    {
                        Trace.TraceInformation("WebdriverContext: Redirecting Firefox's download location ({0})", DownloadsFolder.FullName);
                        FirefoxOptions.AddAdditionalOption("browser.download.dir", DownloadsFolder.FullName);
                    }

                    if (browserArguments is not null) FirefoxOptions.AddArguments(browserArguments);

                    if (IsHeadless)
                    {
                        FirefoxOptions.AddArgument("-headless");
                    }

                    ReturnData = FirefoxOptions;
                    break;

                case Browsers.Browser.EDGE:
                    var EdgeOptions = new EdgeOptions();
                    if (browserArguments is not null) EdgeOptions.AddArguments(browserArguments);

                    // DO NOT use "--no-sandbox" as it causes edge-processes to linger after shutdown!
                    EdgeOptions.AddUserProfilePreference("safebrowsing.enabled", "false");
                    EdgeOptions.AddUserProfilePreference("download.prompt_for_download", false);

                    if (DownloadsFolder is not null)
                    {
                        Trace.TraceInformation("WebdriverContext: Redirecting Edge's download location ({0})", DownloadsFolder.FullName);
                        EdgeOptions.AddUserProfilePreference("download.directory_upgrade", true);
                        EdgeOptions.AddUserProfilePreference("download.default_directory", DownloadsFolder.FullName);
                    }

                    if (IsHeadless)
                    {
                        EdgeOptions.AddArgument("--headless=new");
                    }

                    ReturnData = EdgeOptions;
                    break;

                default:
                    throw new ArgumentException(nameof(Browser));
            };

            ReturnData.Proxy = new Proxy()
            {
                IsAutoDetect = false,
                Kind = ProxyKind.Direct
            };

            Debug.Assert(ReturnData is not null);
            return ReturnData;
        }

        private void Dispose(bool isDisposing)
        {
            if (IsDisposed) return;
            IsDisposed = true;

            Trace.TraceInformation("WebdriverContext: Disposing the webdriver context (via GC? {0})", !isDisposing);
            _Webdriver?.Quit();
            _Webdriver = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~WebdriverContext()
        {
            Dispose(false);
        }
    }
}
