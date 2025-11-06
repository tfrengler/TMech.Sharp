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