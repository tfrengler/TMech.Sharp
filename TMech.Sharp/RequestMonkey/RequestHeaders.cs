namespace TMech.Sharp.RequestMonkey;

public sealed record RequestHeaders
{
    public string Accept { get; set; } = string.Empty;
    public string AcceptCharset { get; set; } = string.Empty;
    public string AcceptEncoding { get; set; } = string.Empty;
    public string AcceptLanguage { get; set; } = string.Empty;
    public string Authorization { get; set; } = string.Empty;
    public string CacheControl { get; set; } = string.Empty;
    public string Connection { get; set; } = string.Empty;
    public string ConnectionClose { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Expect { get; set; } = string.Empty;
    public string ExpectContinue { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string IfMatch { get; set; } = string.Empty;
    public string IfModifiedSince { get; set; } = string.Empty;
    public string IfNoneMatch { get; set; } = string.Empty;
    public string IfRange { get; set; } = string.Empty;
    public string IfUnmodifiedSince { get; set; } = string.Empty;
    public string MaxForwards { get; set; } = string.Empty;
    public string Pragma { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public string ProxyAuthorization { get; set; } = string.Empty;
    public string Range { get; set; } = string.Empty;
    public string Referrer { get; set; } = string.Empty;
    public string TE { get; set; } = string.Empty;
    public string Trailer { get; set; } = string.Empty;
    public string TransferEncoding { get; set; } = string.Empty;
    public string TransferEncodingChunked { get; set; } = string.Empty;
    public string Upgrade { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string Via { get; set; } = string.Empty;
    public string Warning { get; set; } = string.Empty;
}
