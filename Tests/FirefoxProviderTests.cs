using NUnit.Framework;
using System;
using System.IO;
using TMech.Utils;

namespace Tests
{
    [Parallelizable(ParallelScope.Fixtures)]
    [TestFixture]
    public class FirefoxProviderTests
    {
        [SetUp]
        public void ClearInstallFolder()
        {
            foreach (FileInfo CurrentFile in GlobalSetup.FirefoxTempInstallLocation.EnumerateFiles())
            {
                CurrentFile.Delete();
            }
            foreach (DirectoryInfo CurrentDirectory in GlobalSetup.FirefoxTempInstallLocation.EnumerateDirectories())
            {
                CurrentDirectory.Delete(true);
            }
        }

        private static readonly string FirefoxBinaryLocation = Path.Combine(GlobalSetup.FirefoxTempInstallLocation.FullName, "firefox");
        private static readonly string WebdriverBinaryLocation = Path.Combine(GlobalSetup.FirefoxTempInstallLocation.FullName, "geckodriver");

        #region INSTALLED

        private const string Category_Installed = "FirefoxProvider = Installed";

        [TestCase(Category = Category_Installed)]
        public void Installed_Version_Browser_Win64()
        {
            using (var FirefoxProvider = new FirefoxProvider(GlobalSetup.FirefoxTempInstallLocation))
            {
                string CurrentVersion = FirefoxProvider.GetCurrentInstalledBrowserVersion();
                Assert.That(CurrentVersion, Is.Empty);

                bool Updated = FirefoxProvider.DownloadLatestBrowserVersion(TMech.Platform.Win64);
                Assert.That(Updated, Is.True);
                Assert.That(File.Exists(FirefoxBinaryLocation + ".exe"), Is.True);

                CurrentVersion = FirefoxProvider.GetCurrentInstalledBrowserVersion();
                Console.WriteLine("Current version: " + CurrentVersion);
                Assert.That(CurrentVersion, Is.Not.Empty);

                Updated = FirefoxProvider.DownloadLatestBrowserVersion(TMech.Platform.Win64);
                Assert.That(Updated, Is.False);
                Assert.That(File.Exists(FirefoxBinaryLocation + ".exe"), Is.True);
            }
        }

        [TestCase(Category = Category_Installed)]
        public void Installed_Version_Driver_Win64()
        {
            bool Updated;

            using (var FirefoxProvider = new FirefoxProvider(GlobalSetup.FirefoxTempInstallLocation))
            {
                string CurrentVersion = FirefoxProvider.GetCurrentInstalledDriverVersion();
                Assert.That(CurrentVersion, Is.Empty);

                Updated = FirefoxProvider.DownloadLatestDriverVersion(TMech.Platform.Win64);
                Assert.That(Updated, Is.True);
                Assert.That(File.Exists(WebdriverBinaryLocation + ".exe"), Is.True);

                CurrentVersion = FirefoxProvider.GetCurrentInstalledDriverVersion();
                Console.WriteLine("Current version: " + CurrentVersion);
                Assert.That(CurrentVersion, Is.Not.Empty);

                Updated = FirefoxProvider.DownloadLatestDriverVersion(TMech.Platform.Win64);
                Assert.That(Updated, Is.False);
                Assert.That(File.Exists(WebdriverBinaryLocation + ".exe"), Is.True);
            }
        }

        [TestCase(Category = Category_Installed)]
        public void Installed_Version_Browser_Linux64()
        {
            using (var FirefoxProvider = new FirefoxProvider(GlobalSetup.FirefoxTempInstallLocation))
            {
                string CurrentVersion = FirefoxProvider.GetCurrentInstalledBrowserVersion();
                Assert.That(CurrentVersion, Is.Empty);

                bool Updated = FirefoxProvider.DownloadLatestBrowserVersion(TMech.Platform.Linux64);
                Assert.That(Updated, Is.True);
                Assert.That(File.Exists(FirefoxBinaryLocation), Is.True);

                CurrentVersion = FirefoxProvider.GetCurrentInstalledBrowserVersion();
                Console.WriteLine("Current version: " + CurrentVersion);
                Assert.That(CurrentVersion, Is.Not.Empty);

                Updated = FirefoxProvider.DownloadLatestBrowserVersion(TMech.Platform.Linux64);
                Assert.That(Updated, Is.False);
                Assert.That(File.Exists(FirefoxBinaryLocation), Is.True);
            }
        }

        [TestCase(Category = Category_Installed)]
        public void Installed_Version_Driver_Linux64()
        {
            bool Updated;

            using (var FirefoxProvider = new FirefoxProvider(GlobalSetup.FirefoxTempInstallLocation))
            {
                string CurrentVersion = FirefoxProvider.GetCurrentInstalledDriverVersion();
                Assert.That(CurrentVersion, Is.Empty);

                Updated = FirefoxProvider.DownloadLatestDriverVersion(TMech.Platform.Linux64);
                Assert.That(Updated, Is.True);
                Assert.That(File.Exists(WebdriverBinaryLocation), Is.True);

                CurrentVersion = FirefoxProvider.GetCurrentInstalledDriverVersion();
                Console.WriteLine("Current version: " + CurrentVersion);
                Assert.That(CurrentVersion, Is.Not.Empty);

                Updated = FirefoxProvider.DownloadLatestDriverVersion(TMech.Platform.Linux64);
                Assert.That(Updated, Is.False);
                Assert.That(File.Exists(WebdriverBinaryLocation), Is.True);
            }
        }

//#warning For following tests we have to manually update the version string to match (or mismatch) the latest available version online
        private const string IgnoreReason = "Requires manually updating the version string to match (or mismatch) the latest available version online";

        //[Ignore(IgnoreReason)]
        [TestCase(Category = Category_Installed)]
        public void Installed_Version_Driver_Newer()
        {
            bool Updated;
            File.WriteAllText(Path.Combine(GlobalSetup.FirefoxTempInstallLocation.FullName, FirefoxProvider.DriverVersionFileName), "v123.4.5");

            using (var FirefoxProvider = new FirefoxProvider(GlobalSetup.FirefoxTempInstallLocation))
            {
                Updated = FirefoxProvider.DownloadLatestDriverVersion(TMech.Platform.Win64);
                Assert.That(Updated, Is.False);
            }
        }

        //[Ignore(IgnoreReason)]
        [TestCase(Category = Category_Installed)]
        public void Installed_Version_Driver_Older()
        {
            bool Updated;
            File.WriteAllText(Path.Combine(GlobalSetup.FirefoxTempInstallLocation.FullName, FirefoxProvider.DriverVersionFileName), "v0.01.0");

            using (var FirefoxProvider = new FirefoxProvider(GlobalSetup.FirefoxTempInstallLocation))
            {
                Updated = FirefoxProvider.DownloadLatestDriverVersion(TMech.Platform.Win64);
                Assert.That(Updated, Is.True);
            }
        }

        //[Ignore(IgnoreReason)]
        [TestCase(Category = Category_Installed)]
        public void Installed_Version_Browser_Newer()
        {
            File.WriteAllText(Path.Combine(GlobalSetup.FirefoxTempInstallLocation.FullName, FirefoxProvider.BrowserVersionFileName), "1234.5.6");

            using (var FirefoxProvider = new FirefoxProvider(GlobalSetup.FirefoxTempInstallLocation))
            {
                bool Updated = FirefoxProvider.DownloadLatestBrowserVersion(TMech.Platform.Win64);
                Assert.That(Updated, Is.False);
            }
        }

        //[Ignore(IgnoreReason)]
        [TestCase(Category = Category_Installed)]
        public void Installed_Version_Browser_Older()
        {
            File.WriteAllText(Path.Combine(GlobalSetup.FirefoxTempInstallLocation.FullName, FirefoxProvider.BrowserVersionFileName), "1.2.3");

            using (var FirefoxProvider = new FirefoxProvider(GlobalSetup.FirefoxTempInstallLocation))
            {
                bool Updated = FirefoxProvider.DownloadLatestBrowserVersion(TMech.Platform.Win64);
                Assert.That(Updated, Is.True);
            }
        }

        #endregion

        #region LATEST

        private const string Category_Latest = "FirefoxProvider = Latest";

        [TestCase(Category = Category_Latest)]
        public void Latest_Version_Browser_Win64()
        {
            using (var FirefoxProvider = new FirefoxProvider(GlobalSetup.FirefoxTempInstallLocation))
            {
                string LatestVersion = FirefoxProvider.GetLatestAvailableBrowserVersion(TMech.Platform.Win64);
                Console.WriteLine("Latest version: " + LatestVersion);
                Assert.That(LatestVersion, Is.Not.Empty);
            }
        }

        [TestCase(Category = Category_Latest)]
        public void Latest_Version_Driver_Win64()
        {
            using (var FirefoxProvider = new FirefoxProvider(GlobalSetup.FirefoxTempInstallLocation))
            {
                string LatestVersion = FirefoxProvider.GetLatestAvailableDriverVersion(TMech.Platform.Win64);
                Console.WriteLine("Latest version: " + LatestVersion);
                Assert.That(LatestVersion, Is.Not.Empty);
            }
        }

        [TestCase(Category = Category_Latest)]
        public void Latest_Version_Browser_Linux64()
        {
            using (var FirefoxProvider = new FirefoxProvider(GlobalSetup.FirefoxTempInstallLocation))
            {
                string LatestVersion = FirefoxProvider.GetLatestAvailableBrowserVersion(TMech.Platform.Linux64);
                Console.WriteLine("Latest version: " + LatestVersion);
                Assert.That(LatestVersion, Is.Not.Empty);
            }
        }

        [TestCase(Category = Category_Latest)]
        public void Latest_Version_Driver_Linux64()
        {
            using (var FirefoxProvider = new FirefoxProvider(GlobalSetup.FirefoxTempInstallLocation))
            {
                string LatestVersion = FirefoxProvider.GetLatestAvailableDriverVersion(TMech.Platform.Linux64);
                Console.WriteLine("Latest version: " + LatestVersion);
                Assert.That(LatestVersion, Is.Not.Empty);
            }
        }

        #endregion
    }
}
 