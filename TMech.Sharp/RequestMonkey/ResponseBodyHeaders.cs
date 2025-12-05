using System.Net.Http.Headers;

namespace TMech.Sharp.RequestMonkey;

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
    public string ContentMD5 { get; init; } = string.Empty;
    public string ContentRange { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string Expires { get; init; } = string.Empty;
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
}
