using RequestForge.Headers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RequestForge.Core;

public sealed class Response
{
    internal Response(HttpClient httpClient, HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(request);

        _httpClient = httpClient;
        _request = request;
    }

    private readonly HttpClient _httpClient;
    private readonly HttpRequestMessage _request;
    private readonly List<string> _validationErrors = [];
    private readonly List<ResponsePredicate> _predicates = [];
    private HttpResponseMessage _response = null!;
    private bool _isContentBuffered = false;
    private byte[] _contentBuffer = [];
    private bool _continueOnFailure = false;
    private object? _parsedContent = null;

    private byte[] GetContent()
    {
        if (_isContentBuffered) return _contentBuffer;

        _isContentBuffered = true;

        _contentBuffer = _response.Content
            .ReadAsByteArrayAsync()
            .GetAwaiter()
            .GetResult();

        return _contentBuffer;
    }

    private List<string> ResolvePredicates()
    {
        foreach(ResponsePredicate predicate in _predicates)
        {
            if (!predicate(_response))
            {
                return _validationErrors;
            }
        }

        return _validationErrors;
    }

    public Response ThenContinueOnFailure()
    {
        _continueOnFailure = true;
        return this;
    }

    #region STATUS

    public Response ThenResponseStatusShouldBeOK(bool continueOnFailure = false)
    {
        _continueOnFailure = continueOnFailure;
        _predicates.Add(httpResponseMessage =>
        {
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                return true;
            }
            _validationErrors.Add("Expected status code to indicate success (200-299) but it was: " + httpResponseMessage.StatusCode);
            return _continueOnFailure;
        });

        return this;
    }

    public Response ThenResponseStatusIs(HttpStatusCode expectedStatus, bool continueOnFailure = false)
    {
        _continueOnFailure = continueOnFailure;

        _predicates.Add(httpResponseMessage =>
        {
            if (httpResponseMessage.StatusCode == expectedStatus)
            {
                return true;
            }
            _validationErrors.Add($"Expected response to have statuscode {expectedStatus} but it was {httpResponseMessage.StatusCode}");
            return _continueOnFailure;
        });

        return this;
    }

    public Response ThenResponseStatusIs(int expectedStatus, bool continueOnFailure = false)
    {
        _continueOnFailure = continueOnFailure;

        _predicates.Add(httpResponseMessage =>
        {
            if ((int)httpResponseMessage.StatusCode == expectedStatus)
            {
                return true;
            }
            _validationErrors.Add($"Expected response to have statuscode {expectedStatus} but it was {(int)httpResponseMessage.StatusCode}");
            return _continueOnFailure;
        });

        return this;
    }

    #endregion

    #region HEADERS

    public Response ThenResponseHeaderHasValueEqualTo(string name, string? value, bool continueOnFailure = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (value is null) return this;
        continueOnFailure = _continueOnFailure;

        _predicates.Add(httpResponseMessage =>
        {
            if (!httpResponseMessage.Headers.TryGetValues(name, out var values))
            {
                _validationErrors.Add($"Expected response to have a header by name '{name}' but it does not");
                return false;
            }

            string actualHeaderValue = string.Join(';', values);
            bool headerValuesAreEqual = string.Equals(value, actualHeaderValue, StringComparison.OrdinalIgnoreCase);
            if (headerValuesAreEqual) return true;

            _validationErrors.Add($"Expected response header by name '{name}' that have a value equal to '{value}' but instead it was '{actualHeaderValue}'");
            return continueOnFailure;
        });

        return this;
    }

    #endregion

    #region BODY

    public Response ThenConsumeResponseBody(Func<HttpStatusCode, byte[], bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        _predicates.Add(httpResponseMessage =>
        {
            byte[] responseBody = GetContent();
            if (responseBody.Length > 0)
            {
                return true;
            }
            _validationErrors.Add("Expected response to have a body but it does not");
            return predicate(httpResponseMessage.StatusCode, responseBody);
        });

        return this;
    }

    public Response ThenConsumeResponseBodyAsString(Func<HttpStatusCode,string,bool> predicate, StringEncoding encoding = StringEncoding.UTF8)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        _predicates.Add(httpRequestMessage =>
        {
            var stringEncoder = encoding.GetEncoder();
            string responseBody = string.Empty;

            try
            {
                responseBody = stringEncoder.GetString(GetContent());
            }
            catch(Exception error) when (error is DecoderFallbackException || error is ArgumentException)
            {
                _validationErrors.Add($"Excepted response body to contain a valid {encoding} string but decoding failed.");
            }

            _parsedContent = responseBody;
            return predicate(httpRequestMessage.StatusCode, responseBody);
        });

        return this;
    }

    public Response ThenConsumeResponseBodyAsJson<T>(ResponseBodyPredicate<T> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        _predicates.Add(httpRequestMessage =>
        {
            string responseBodyAsString = Encoding.UTF8.GetString(GetContent());
            string errorMessage = $"Expected response body to contain valid JSON that could be deserialized to {typeof(T)}";
            T? responseBodyAsJson;
            try
            {
                responseBodyAsJson = JsonSerializer.Deserialize<T>(responseBodyAsString);
            }
            catch(JsonException)
            {
                _validationErrors.Add(errorMessage);
                return false;
            }

            if (responseBodyAsJson is null)
            {
                _validationErrors.Add(errorMessage);
                return false;
            }
            
            _parsedContent = responseBodyAsJson;
            return predicate(httpRequestMessage.StatusCode, responseBodyAsJson);
        });

        return this;
    }

    public Response ThenConsumeResponseBodyAsJson(ResponseBodyPredicate<JsonElement> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        _predicates.Add(httpRequestMessage =>
        {
            string responseBodyAsString = Encoding.UTF8.GetString(GetContent());
            JsonElement responseBodyAsJson;

            try
            {
                responseBodyAsJson = JsonSerializer.Deserialize<JsonElement>(responseBodyAsString);
            }
            catch (JsonException)
            {
                _validationErrors.Add($"Expected response body to contain valid JSON");
                return false;
            }

            _parsedContent = responseBodyAsJson;
            return predicate(httpRequestMessage.StatusCode, responseBodyAsJson);
        });

        return this;
    }

    #endregion

    public async Task<Result> GetResult()
    {
        _response = await _httpClient.SendAsync(_request);

        var headers = new ResponseHeaderCollection(
            ResponseHeaders.ParseFromHttpResponseHeaders(_response.Headers),
            ResponseBodyHeaders.ParseFromHttpResponseBodyHeaders(_response.Content.Headers)
        );

        return new Result(
            ResolvePredicates(),
            _response.StatusCode,
            GetContent(),
            headers,
            _parsedContent
        );
    }

    // ThenHeadersAre(Action<Headers>)
    // ThenHeadersContain(Action<Headers>)
    // ThenParseBodyAsLong()
    // ThenParseBodyAsDouble()
    // ThenParseBodyAsJson()
    // ThenParseBodyAsXml()
}

delegate bool ResponsePredicate(HttpResponseMessage response);
public delegate bool ResponseBodyPredicate<T>(HttpStatusCode statuscode, T body);