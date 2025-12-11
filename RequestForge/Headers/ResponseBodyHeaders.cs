using System.Collections.Generic;
using System.Net.Http.Headers;

namespace RequestForge.Headers;

public sealed class ResponseBodyHeaders
{
    private HttpContentHeaders _originalHeaders = null!;
    public HttpContentHeaders GetStronglyTypedCollection() => _originalHeaders;

    public string Allow { get; init; } = string.Empty;
    public string ContentDisposition { get; init; } = string.Empty;
    public string ContentEncoding { get; init; } = string.Empty;
    public string ContentLanguage { get; init; } = string.Empty;
    public string ContentLength { get; init; } = string.Empty;
    public string ContentLocation { get; init; } = string.Empty;
    ///<summary>Base64 string of the byte-content</summary>
    public string ContentMD5 { get; init; } = string.Empty;
    public string ContentRange { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    ///<summary>Empty string or date in ISO 8601 format (YYYY-MM-DDTHH:MM:SS.0000000-HH:MM).</summary>
    public string Expires { get; init; } = string.Empty;
    ///<summary>Empty string or date in ISO 8601 format (YYYY-MM-DDTHH:MM:SS.0000000-HH:MM).</summary>
    public string LastModified { get; init; } = string.Empty;

    public static ResponseBodyHeaders ParseFromHttpResponseBodyHeaders(HttpContentHeaders input)
    {
        return new()
        {
            _originalHeaders = input,
            Allow = string.Join(';', input.Allow),
            ContentDisposition = input.ContentDisposition?.ToString() ?? string.Empty,
            ContentEncoding = string.Join(';', input.ContentEncoding),
            ContentLanguage = string.Join(';', input.ContentLanguage),
            ContentLength = System.Convert.ToString(input.ContentLength) ?? string.Empty,
            ContentLocation = input.ContentLocation?.ToString() ?? string.Empty,
            ContentMD5 = System.Convert.ToBase64String(input.ContentMD5 ?? []),
            ContentRange = input.ContentRange?.ToString() ?? string.Empty,
            ContentType = input.ContentType?.ToString() ?? string.Empty,
            Expires = input.Expires?.ToString("o") ?? string.Empty,
            LastModified = input.LastModified?.ToString("o") ?? string.Empty,
        };
    }

    public override string ToString()
    {
        List<string> output = [];

        if (!string.IsNullOrEmpty(Allow))               output.Add($"Allow                   : {Allow}");
        if (!string.IsNullOrEmpty(ContentDisposition))  output.Add($"ContentDisposition      : {ContentDisposition}");
        if (!string.IsNullOrEmpty(ContentEncoding))     output.Add($"ContentEncoding         : {ContentEncoding}");
        if (!string.IsNullOrEmpty(ContentLanguage))     output.Add($"ContentLanguage         : {ContentLanguage}");
        if (!string.IsNullOrEmpty(ContentLength))       output.Add($"ContentLength           : {ContentLength}");
        if (!string.IsNullOrEmpty(ContentLocation))     output.Add($"ContentLocation         : {ContentLocation}");
        if (!string.IsNullOrEmpty(ContentMD5))          output.Add($"ContentMD5              : {ContentMD5}");
        if (!string.IsNullOrEmpty(ContentRange))        output.Add($"ContentRange            : {ContentRange}");
        if (!string.IsNullOrEmpty(ContentType))         output.Add($"ContentType             : {ContentType}");
        if (!string.IsNullOrEmpty(Expires))             output.Add($"Expires                 : {Expires}");
        if (!string.IsNullOrEmpty(LastModified))        output.Add($"LastModified            : {LastModified}");

        return string.Join(System.Environment.NewLine, output);
    }
}
