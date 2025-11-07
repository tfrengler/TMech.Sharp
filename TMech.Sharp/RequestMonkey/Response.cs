using System;
using System.Net.Http;

namespace TMech.Sharp.RequestMonkey
{
    public sealed class Response
    {
        private readonly HttpClient _httpClient;
        private readonly HttpRequestMessage _request;

        public Response(HttpClient httpClient, HttpRequestMessage request)
        {
            ArgumentNullException.ThrowIfNull(httpClient);
            ArgumentNullException.ThrowIfNull(request);

            _httpClient = httpClient;
            _request = request;
        }

        // WhenStatusIsOK
        // WhenStatusIs
        // WhenHeadersAre(Action<Headers>)
        // WhenHeadersContain(Action<Headers>)
        // WhenHeaderIs(string name, string value)
        // WhenBodyIsPresent
        // ThenExecute()
        // ThenParseBodyAsString()
        // ThenParseBodyAsLong()
        // ThenParseBodyAsDouble()
        // ThenParseBodyAsJson()
        // ThenParseBodyAsXml()
    }
}


/* RETURNS:
 * {
 *      Validation: {
 *          Success: bool
 *          FailureMessage: string
 *      },
 *      ResponseHeaders: TBD
 *      ResponseBody: <T>
 *      StatusCode: int
 * }
 */