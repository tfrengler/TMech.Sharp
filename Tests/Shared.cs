using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;

[SetUpFixture]
public class GlobalSetup
{
    public static DirectoryInfo FirefoxTempInstallLocation;
    public static DirectoryInfo ChromeTempInstallLocation;
    public static DirectoryInfo DownloadsFolder;

    static GlobalSetup()
    {
        string Here = AppContext.BaseDirectory;

        FirefoxTempInstallLocation = new(Path.Combine(Here, "Firefox_Temp"));
        ChromeTempInstallLocation = new(Path.Combine(Here, "Chrome_Temp"));
        DownloadsFolder = new(Path.Combine(Here, "Downloads"));

        FirefoxTempInstallLocation.Create();
        ChromeTempInstallLocation.Create();
        DownloadsFolder.Create();
    }

    [OneTimeSetUp]
    public void BeforeAll()
    {
        Trace.Listeners.Add(new ConsoleTraceListener());
    }

    [OneTimeTearDown]
    public void AfterAll()
    {
        Trace.Flush();

        DownloadsFolder.Delete(true);
        FirefoxTempInstallLocation.Delete(true);
        ChromeTempInstallLocation.Delete(true);

        var AllProcesses = Process.GetProcesses();
        var BrowserDriverProcessNames = new string[] { "chromedriver", "geckodriver", "msedgedriver" };

        foreach(var CurrentProcess in AllProcesses)
        {
            if (Array.IndexOf(BrowserDriverProcessNames, CurrentProcess.ProcessName) > -1)
            {
                CurrentProcess.Kill(true);
            }
        }
    }
}