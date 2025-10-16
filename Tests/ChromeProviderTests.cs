using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.IO;
using TMech.Utils;

namespace Tests
{
    [Parallelizable(ParallelScope.Fixtures)]
    [TestFixture]
    public sealed class ChromeProviderTests
    {
        [SetUp]
        public void ClearInstallFolder()
        {
            foreach (FileInfo CurrentFile in GlobalSetup.ChromeTempInstallLocation.EnumerateFiles())
            {
                CurrentFile.Delete();
            }
            foreach (DirectoryInfo CurrentDirectory in GlobalSetup.ChromeTempInstallLocation.EnumerateDirectories())
            {
                CurrentDirectory.Delete(true);
            }
        }

        private static readonly string ChromeDriverExeLocation = Path.Combine(GlobalSetup.ChromeTempInstallLocation.FullName, "chromedriver");
        private static readonly string ChromeExeLocation = Path.Combine(GlobalSetup.ChromeTempInstallLocation.FullName, "chrome");

        #region INSTALLED

        private const string Category_Installed = "ChromePrivder = Installed";

        [TestCase(Category = Category_Installed)]
        public void Installed_Version_Win64()
        {
            TestContext.WriteLine($"Chrome download folder is: " + GlobalSetup.ChromeTempInstallLocation.FullName);

            using (var ChromeProvider = new ChromeProvider(GlobalSetup.ChromeTempInstallLocation))
            {
                string CurrentVersion = ChromeProvider.GetCurrentInstalledVersion();
                Assert.That(CurrentVersion, Is.Empty);

                TestContext.WriteLine($"Downloading latest Win64 version");

                bool Updated = ChromeProvider.DownloadLatestVersion(TMech.Platform.Win64);
                Assert.That(Updated, Is.True);
                Assert.That(File.Exists(ChromeDriverExeLocation + ".exe"), Is.True, $"Expected {ChromeDriverExeLocation + ".exe"} to exist but it does not");
                Assert.That(File.Exists(ChromeExeLocation + ".exe"), Is.True, $"Expected {ChromeDriverExeLocation + ".exe"} to exist but it does not");

                CurrentVersion = ChromeProvider.GetCurrentInstalledVersion();
                Console.WriteLine("Current version: " + CurrentVersion);
                Assert.That(CurrentVersion, Is.Not.Empty);

                Updated = ChromeProvider.DownloadLatestVersion(TMech.Platform.Win64);
                Assert.That(Updated, Is.False);
            }
        }

        [TestCase(Category = Category_Installed)]
        public void Installed_Version_Linux64()
        {
            using (var ChromeProvider = new ChromeProvider(GlobalSetup.ChromeTempInstallLocation))
            {
                string CurrentVersion = ChromeProvider.GetCurrentInstalledVersion();
                Assert.That(CurrentVersion, Is.Empty);

                bool Updated = ChromeProvider.DownloadLatestVersion(TMech.Platform.Linux64);
                Assert.That(Updated, Is.True);
                Assert.That(File.Exists(ChromeDriverExeLocation), Is.True);
                Assert.That(File.Exists(ChromeExeLocation), Is.True);

                CurrentVersion = ChromeProvider.GetCurrentInstalledVersion();
                Console.WriteLine("Current version: " + CurrentVersion);
                Assert.That(CurrentVersion, Is.Not.Empty);

                Updated = ChromeProvider.DownloadLatestVersion(TMech.Platform.Linux64);
                Assert.That(Updated, Is.False);
            }
        }

        [TestCase(Category = Category_Installed)]
        public void Installed_Version_WithoutDriver()
        {
            using (var ChromeProvider = new ChromeProvider(GlobalSetup.ChromeTempInstallLocation))
            {
                string CurrentVersion = ChromeProvider.GetCurrentInstalledVersion();
                Assert.That(CurrentVersion, Is.Empty);

                bool Updated = ChromeProvider.DownloadLatestVersion(TMech.Platform.Win64, true);
                Assert.That(Updated, Is.True);
                Assert.That(File.Exists(ChromeDriverExeLocation + ".exe"), Is.False);
            }
        }

        #endregion

        #region LATEST

        private const string Category_Latest = "ChromeProvider = Latest";

        [TestCase(Category = Category_Latest)]
        public void Latest_Version_Win64()
        {
            using (var ChromeProvider = new ChromeProvider(GlobalSetup.ChromeTempInstallLocation))
            {
                string LatestVersion = ChromeProvider.GetLatestAvailableVersion(TMech.Platform.Win64);
                Console.WriteLine("Latest version: " + LatestVersion);
                Assert.That(LatestVersion, Is.Not.Empty);
            }
        }

        [TestCase(Category = Category_Latest)]
        public void Latest_Version_Browser_Linux64()
        {
            using (var ChromeProvider = new ChromeProvider(GlobalSetup.ChromeTempInstallLocation))
            {
                string LatestVersion = ChromeProvider.GetLatestAvailableVersion(TMech.Platform.Linux64);
                Console.WriteLine("Latest version: " + LatestVersion);
                Assert.That(LatestVersion, Is.Not.Empty);
            }
        }

        #endregion
    }
}
