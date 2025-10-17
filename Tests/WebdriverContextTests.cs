using NUnit.Framework;
using OpenQA.Selenium.Chrome;
using System;
using System.IO;
using TMech.Sharp.Browsers;
using TMech.Sharp.Selenium;

namespace Tests
{
    [Parallelizable(ParallelScope.Children)]
    [TestFixture]
    public class WebdriverContextTests
    {
        private static FileInfo ChromeLocation = null!;
        private static FileInfo ChromeDriverLocation = null!;

        private System.Drawing.Size WindowSize = new System.Drawing.Size(1024, 768);

        [OneTimeSetUp]
        public static void BeforeAll()
        {
            Assert.That(
                new ChromeProvider(GlobalSetup.ChromeTempInstallLocation).DownloadLatestVersion(Platform.Win64),
                Is.True,
                "Global setup failed! Tried to install Chrome for Testing (win64) and it didn't work"
            );

#warning Presuming we are always testing on Windows
            ChromeLocation = new FileInfo(Path.Combine(GlobalSetup.ChromeTempInstallLocation.FullName, "chrome.exe"));
            ChromeDriverLocation = new FileInfo(Path.Combine(GlobalSetup.ChromeTempInstallLocation.FullName, "chromedriver.exe"));
        }

        private const string Category = "WebdriverContext";
        [TestCase(Category = Category)]
        public void Remote_DefaultInitialize()
        {
            using (var DriverService = ChromeDriverService.CreateDefaultService(ChromeDriverLocation.FullName))
            {
                DriverService.Start();

                using var Context = WebdriverContext.CreateRemote(Browser.CHROME, DriverService.ServiceUrl);
                Context.Initialize(true);
            }
        }

        [TestCase(Category = Category)]
        public void Remote_CustomInitialize()
        {
            using (var DriverService = ChromeDriverService.CreateDefaultService(ChromeDriverLocation.FullName))
            {
                DriverService.Start();

                using var Context = WebdriverContext.CreateRemote(Browser.CHROME, DriverService.ServiceUrl);
                Context.Initialize(true, WindowSize, [ "--allow-file-access-from-files" ], GlobalSetup.DownloadsFolder);
            }
        }

        [TestCase(Category = Category)]
        public void Local_WithDriver_NoBinary_DefaultInitialize()
        {
            var DriverService = ChromeDriverService.CreateDefaultService();
            using var Context = WebdriverContext.CreateLocal(DriverService);
            Context.Initialize(true);
        }

        [TestCase(Category = Category)]
        public void Local_WithBrowser_AndBinary_DefaultInitialize()
        {
            using var Context = WebdriverContext.CreateLocal(Browser.CHROME, ChromeLocation);
            Context.Initialize(true);
        }

        [TestCase(Category = Category)]
        public void Local_WithBrowser_NoBinary_CustomInitialize()
        {
            using var Context = WebdriverContext.CreateLocal(Browser.CHROME);
            Context.Initialize(true, WindowSize, new string[] { "--allow-file-access-from-files" }, GlobalSetup.DownloadsFolder);
        }

        [TestCase(Category = Category)]
        public void Failure_Usage_Before_Initialization()
        {
            using var Context = WebdriverContext.CreateLocal(Browser.CHROME);
            Assert.Throws<InvalidOperationException>(() => _ = Context.Webdriver);
        }

        #region BROWSER SPECIFIC

        [TestCase(Category = Category)]
        public void Local_Chrome_DefaultInitialize()
        {
            using var Context = WebdriverContext.CreateLocal(Browser.CHROME);
            Context.Initialize(true);
        }

        [TestCase(Category = Category)]
        public void Local_Firefox_DefaultInitialize()
        {
            using var Context = WebdriverContext.CreateLocal(Browser.FIREFOX);
            Context.Initialize(true);
        }

        [TestCase(Category = Category)]
        public void Local_Edge_DefaultInitialize()
        {
            using var Context = WebdriverContext.CreateLocal(Browser.EDGE);
            Context.Initialize(true);
        }

        #endregion
    }
}
