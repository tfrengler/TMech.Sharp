using System.Net.Http.Headers;

namespace RequestForge;

public static class StandardMediaTypes
{
    public static MediaTypeHeaderValue Json => new("application/json", "UTF-8");
    public static MediaTypeHeaderValue TextXml => new("text/xml", "UTF-8");
    public static MediaTypeHeaderValue Xml => new("application/xml", "UTF-8");
    public static MediaTypeHeaderValue SoapXml => new("application/soap+xml", "UTF-8");
    public static MediaTypeHeaderValue PlainText => new("text/plain", "UTF-8");
    public static MediaTypeHeaderValue Binary => new("application/octet-stream");
}
