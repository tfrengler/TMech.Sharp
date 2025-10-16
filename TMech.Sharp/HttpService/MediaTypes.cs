using System.Net.Http.Headers;

namespace CZ.DM.Art.Core.Shared
{
    public static class MediaTypes
    {
        public static MediaTypeWithQualityHeaderValue Json { get; } = new MediaTypeWithQualityHeaderValue("application/json");
        public static MediaTypeWithQualityHeaderValue Xml { get; } = new MediaTypeWithQualityHeaderValue("application/xml");
        public static MediaTypeWithQualityHeaderValue SoapXml { get; } = new MediaTypeWithQualityHeaderValue("application/soap+xml");
        public static MediaTypeWithQualityHeaderValue PlainText { get; } = new MediaTypeWithQualityHeaderValue("text/plain");
        public static MediaTypeWithQualityHeaderValue Binary { get; } = new MediaTypeWithQualityHeaderValue("application/octet-stream");
    }
}
