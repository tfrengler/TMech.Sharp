using System.Collections.Generic;
using System.Net.Http.Headers;

namespace RequestForge.Headers;

public sealed record ResponseHeaders
{
    private HttpResponseHeaders _originalHeaders = null!;
    public HttpResponseHeaders GetStronglyTypedCollection() => _originalHeaders;

    public string AcceptRanges { get; init; } = string.Empty;
    public string Age { get; init; } = string.Empty;
    public string CacheControl { get; init; } = string.Empty;
    public string Connection { get; init; } = string.Empty;
    public string ConnectionClose { get; init; } = string.Empty;
    ///<summary>Empty string or date in ISO 8601 format (YYYY-MM-DDTHH:MM:SS.0000000-HH:MM).</summary>
    public string Date { get; init; } = string.Empty;
    public string ETag { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string Pragma { get; init; } = string.Empty;
    public string ProxyAuthenticate { get; init; } = string.Empty;
    public string RetryAfter { get; init; } = string.Empty;
    public string Server { get; init; } = string.Empty;
    public string Trailer { get; init; } = string.Empty;
    public string TransferEncoding { get; init; } = string.Empty;
    ///<summary><c>True</c>, <c>False</c> or an empty string.</summary>
    public string TransferEncodingChunked { get; init; } = string.Empty;
    public string Upgrade { get; init; } = string.Empty;
    public string Vary { get; init; } = string.Empty;
    public string Via { get; init; } = string.Empty;
    public string Warning { get; init; } = string.Empty;
    public string WwwAuthenticate { get; init; } = string.Empty;

    public static ResponseHeaders ParseFromHttpResponseHeaders(HttpResponseHeaders input)
    {
        return new()
        {
            _originalHeaders = input,
            AcceptRanges = input.AcceptRanges.ToString(),
            Age = input.Age.ToString() ?? string.Empty,
            CacheControl = input.CacheControl?.ToString() ?? string.Empty,
            Connection = input.Connection.ToString(),
            ConnectionClose = System.Convert.ToString(input.ConnectionClose) ?? string.Empty,
            Date = input.Date?.ToString("o") ?? string.Empty,
            ETag = input.ETag?.ToString() ?? string.Empty,
            Location = input.Location?.ToString() ?? string.Empty,
            Pragma = input.Pragma.ToString(),
            ProxyAuthenticate = input.ProxyAuthenticate.ToString(),
            RetryAfter = input.RetryAfter?.ToString() ?? string.Empty,
            Server = input.Server.ToString(),
            Trailer = input.Trailer.ToString(),
            TransferEncoding = input.TransferEncoding.ToString(),
            TransferEncodingChunked = System.Convert.ToString(input.TransferEncodingChunked) ?? string.Empty,
            Upgrade = input.Upgrade.ToString(),
            Vary = input.Vary.ToString(),
            Via = input.Via.ToString(),
            Warning = input.Warning.ToString(),
            WwwAuthenticate = input.WwwAuthenticate.ToString()
        };
    }

    public override string ToString()
    {
        List<string> output = [];

        if (!string.IsNullOrEmpty(AcceptRanges))            output.Add($"AcceptRanges            : {AcceptRanges}");
        if (!string.IsNullOrEmpty(Age))                     output.Add($"Age                     : {Age}");
        if (!string.IsNullOrEmpty(CacheControl))            output.Add($"CacheControl            : {CacheControl}");
        if (!string.IsNullOrEmpty(Connection))              output.Add($"Connection              : {Connection}");
        if (!string.IsNullOrEmpty(ConnectionClose))         output.Add($"ConnectionClose         : {ConnectionClose}");
        if (!string.IsNullOrEmpty(Date))                    output.Add($"Date                    : {Date}");
        if (!string.IsNullOrEmpty(ETag))                    output.Add($"ETag                    : {ETag}");
        if (!string.IsNullOrEmpty(Location))                output.Add($"Location                : {Location}");
        if (!string.IsNullOrEmpty(Pragma))                  output.Add($"Pragma                  : {Pragma}");
        if (!string.IsNullOrEmpty(ProxyAuthenticate))       output.Add($"ProxyAuthenticate       : {ProxyAuthenticate}");
        if (!string.IsNullOrEmpty(RetryAfter))              output.Add($"RetryAfter              : {RetryAfter}");
        if (!string.IsNullOrEmpty(Server))                  output.Add($"Server                  : {Server}");
        if (!string.IsNullOrEmpty(Trailer))                 output.Add($"Trailer                 : {Trailer}");
        if (!string.IsNullOrEmpty(TransferEncoding))        output.Add($"TransferEncoding        : {TransferEncoding}");
        if (!string.IsNullOrEmpty(TransferEncodingChunked)) output.Add($"TransferEncodingChunked : {TransferEncodingChunked}");
        if (!string.IsNullOrEmpty(Upgrade))                 output.Add($"Upgrade                 : {Upgrade}");
        if (!string.IsNullOrEmpty(Vary))                    output.Add($"Vary                    : {Vary}");
        if (!string.IsNullOrEmpty(Via))                     output.Add($"Via                     : {Via}");
        if (!string.IsNullOrEmpty(Warning))                 output.Add($"Warning                 : {Warning}");
        if (!string.IsNullOrEmpty(WwwAuthenticate))         output.Add($"WwwAuthenticate         : {WwwAuthenticate}");
        
        return string.Join(System.Environment.NewLine, output);
    }
}
