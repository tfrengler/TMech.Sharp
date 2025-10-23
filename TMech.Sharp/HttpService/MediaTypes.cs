using System.Net.Http.Headers;

namespace TMech.Sharp.HttpService
{
    public static class MediaTypes
    {
        public static MediaTypeHeaderValue Json { get; } = new("application/json");
        public static MediaTypeHeaderValue Xml { get; } = new("application/xml");
        public static MediaTypeHeaderValue SoapXml { get; } = new("application/soap+xml");
        public static MediaTypeHeaderValue PlainText { get; } = new("text/plain");
        public static MediaTypeHeaderValue Binary { get; } = new("application/octet-stream");
    }
}
