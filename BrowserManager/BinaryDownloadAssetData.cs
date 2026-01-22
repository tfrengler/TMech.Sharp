using System;

namespace TMech.Sharp.Browsers;

internal record BinaryDownloadAssetData
{
    public string ReadableVersion { get; init; } = string.Empty;
    public Version Version { get; init; }
    public required Uri DownloadUri { get; init; }
}
