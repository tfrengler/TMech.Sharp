using System;

namespace TMech.Sharp.Browsers
{
    internal record BinaryDownloadAssetData
    {
        public string ReadableVersion { get; init; } = string.Empty;
        public Version Version { get; init; }
        public Uri DownloadUri { get; init; }

        public BinaryDownloadAssetData()
        {
            // To get the compiler to shut up about possible null deferences as I am certain it is caught elsewhere
            DownloadUri = new Uri("http://localhost");
        }
    }
}
