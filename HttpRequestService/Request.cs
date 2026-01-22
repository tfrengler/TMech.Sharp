using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace TMech.Sharp.HttpRequestService;

/// <summary>
/// A helper class for simplifying calling an API endpoint, adding url parameters and body content using a fluent API.
/// </summary>
public sealed class Request : IDisposable
{
    public Request(HttpClient httpClient, HttpMethod method, string? destinationRelative = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(httpClient.BaseAddress);

        if (destinationRelative is not null)
        {
            if (destinationRelative[0] == '/')
            {
                throw new ArgumentException($"Relative path should not start with a forward slash ({destinationRelative})", nameof(destinationRelative));
            }

            if (httpClient.BaseAddress.ToString()[^1] != '/')
            {
                Destination = new Uri($"{httpClient.BaseAddress}/{destinationRelative}");
            }
            else
            {
                Destination = new Uri(httpClient.BaseAddress, destinationRelative);
            }
        }
        else
        {
            Destination = httpClient.BaseAddress;
        }

        HttpClient = httpClient;
        Method = method;

        UrlParams = HttpUtility.ParseQueryString(string.Empty);
        Headers = new List<KeyValuePair<string, string>>();

        Debug.WriteLine($"Creating new {method}-request {(destinationRelative is null ? "" : $"(destination: {Destination})")}");
    }

    /// <summary>
    /// Gets the underlying http request that this instance wraps around. Will be <see langword="null"/> until <see cref="Send"/> is called.
    /// </summary>
    public HttpRequestMessage? TheRequest { get; private set; }

    private readonly Uri Destination;
    private readonly HttpMethod Method;
    private readonly HttpClient HttpClient;
    private readonly NameValueCollection UrlParams;
    private readonly List<KeyValuePair<string, string>> Headers;
    //private readonly Logger Logger;
    private HttpContent? Body;
    private bool IsDisposed;

    #region FORM BODY

    /// <summary>
    /// Adds a multipart formdata-body to the http request. Will throw an exception if the body of the request has already been set by another method.<br/>
    /// </summary>
    public Request WithMultipartFormBody(MultipartFormBuilder formFactory)
    {
        ArgumentNullException.ThrowIfNull(formFactory);
        Body = formFactory.Build();
        return this;
    }

    /// <summary>
    /// Adds a formdata-body with url encoded key/value-pairs to the http request. Will throw an exception if the body of the request has already been set by another method.<br/>
    /// </summary>
    public Request WithUrlEncodedFormBody(UrlEncodedFormBuilder formFactory)
    {
        ArgumentNullException.ThrowIfNull(formFactory);
        var BodyContent = formFactory.Build();
        ArgumentNullException.ThrowIfNull(BodyContent);
        Body = BodyContent;

        return this;
    }

    /// <summary>
    /// Adds a formdata-body to the http request. Will throw an exception if the body of the request has already been set by another method.
    /// </summary>
    public Request WithUrlEncodedFormBody(IEnumerable<KeyValuePair<string, string>> formContent)
    {
        ArgumentNullException.ThrowIfNull(formContent);
        Body = new FormUrlEncodedContent(formContent);

        return this;
    }

    #endregion

    #region PLAIN TEXT BODY

    /// <summary>
    /// Sets string data as the body to the http request and sets the 'Content-Type' to 'text/plain'. Will throw an exception if the body of the request has already been set by another method.<br/>
    /// </summary>
    public Request WithStringBody(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        Body = new StringContent(content, StandardMediaTypes.PlainText);
        return this;
    }

    /// <summary>
    /// Serializes a given object to a string and sets that as the body of the http request, along with setting the 'Content-Type' to 'application/json'. Will throw an exception if the body of the request has already been set by another method.<br/>
    /// </summary>
    public Request WithJsonBody<T>(T content)
    {
        ArgumentNullException.ThrowIfNull(content);

        Body = JsonContent.Create(
            content,
            StandardMediaTypes.Json
        );

        return this;
    }

    /// <summary>
    /// Serializes a given object to a string and sets that as the body of the http request, along with setting the 'Content-Type' to 'application/json'. Will throw an exception if the body of the request has already been set by another method.<br/>
    /// </summary>
    public Request WithJsonBody<T>(T content, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(options);

        Body = JsonContent.Create(
            content,
            StandardMediaTypes.Json,
            options
        );

        return this;
    }

    /// <summary>
    /// Sets string data as the body of the http request and sets the 'Content-Type' to 'application/json'. Will throw an exception if the body of the request has already been set by another method.
    /// </summary>
    public Request WithJsonBody(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        Body = new StringContent(content, StandardMediaTypes.Json);
        return this;
    }

    /// <summary>
    /// <para>Sets the body of the http request to XML represented by the string you pass and the 'Content-Type' to 'application/soap+xml'. Will throw an exception if the body of the request has already been set by another method.</para>
    /// <para><b>NOTE</b>: Meant for SOAP 1.2 where the 'action' is sent as part of the 'Content-Type'-header and not via the custom 'SOAPAction'-header.</para>
    /// </summary>
    public Request WithSoapXmlBodyV12(string content, string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        var MediaType = new MediaTypeHeaderValue("application/soap+xml", "UTF-8");
        MediaType.Parameters.Add(new("action", $"\"{action}\""));

        Body = new StringContent(content, MediaType);
        return this;
    }

    /// <summary>
    /// <para>Sets the body of the http request to XML represented by the string you pass and the 'Content-Type' to 'text/xml'. Will throw an exception if the body of the request has already been set by another method.</para>
    /// <para><b>NOTE</b>: Meant for SOAP 1.1 where the 'action' is sent as part of the custom 'SOAPAction'-header.</para>
    /// </summary>
    public Request WithSoapXmlBodyV11(string content, string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        Headers.Add(new("SOAPAction", action));

        var MediaType = new MediaTypeHeaderValue("text/xml", "UTF-8");
        Body = new StringContent(content, MediaType);
        return this;
    }

    #endregion

    #region URL PARAMS

    /// <summary>
    /// Adds a URL parameter to the request, in the form of a name and a string-value.
    /// </summary>
    public Request WithUrlParameter(string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        UrlParams.Add(name, value);
        return this;
    }

    /// <summary>
    /// Adds a URL parameter to the request, in the form of a name and an object, which will be JSON serialized.
    /// </summary>
    public Request WithUrlParameter<T>(string name, T value) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);

        string serializedValue = JsonSerializer.Serialize(value);
        UrlParams.Add(name, serializedValue);

        return this;
    }

    /// <summary>
    /// Adds a URL parameter to the request, in the form of a name and an integer.
    /// </summary>
    public Request WithUrlParameter(string name, int value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        UrlParams.Add(name, Convert.ToString(value));

        return this;
    }

    /// <summary>
    /// Adds a URL parameter to the request, in the form of a name and a long (64-bit integer).
    /// </summary>
    public Request WithUrlParameter(string name, long value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        UrlParams.Add(name, Convert.ToString(value));

        return this;
    }

    /// <summary>
    /// Adds a URL parameter to the request, in the form of a name and a boolean.
    /// </summary>
    public Request WithUrlParameter(string name, bool value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        UrlParams.Add(name, Convert.ToString(value));

        return this;
    }

    /// <summary>
    /// Adds a URL parameter to the request, in the form of a name and a datetime-object, which will be written in the C# roundtrip-format.
    /// </summary>
    public Request WithUrlParameter(string name, DateTime value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        UrlParams.Add(name, value.ToString("o"));

        return this;
    }

    /// <summary>
    /// Adds a URL parameter to the request, in the form of a name and a datetime-object, which will be written in the format you specify.
    /// </summary>
    public Request WithUrlParameter(string name, DateTime value, string format)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        UrlParams.Add(name, value.ToString(format));

        return this;
    }

    /// <summary>
    /// Adds a URL parameter to the request, in the form of a name and a enum-value, which will be written by its name and not the underlying numeric value.
    /// </summary>
    public Request WithUrlParameter(string name, Enum value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        UrlParams.Add(name, Enum.GetName(value.GetType(), value));

        return this;
    }

    /// <summary>
    /// Adds a URL parameter to the request, in the form of a name and a collection of strings. The strings will be combined into one, separated by commas.
    /// </summary>
    public Request WithUrlParameter(string name, IEnumerable<string> values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(values);

        UrlParams.Add(name, string.Join(',', values));

        return this;
    }

    /// <summary>
    /// Adds a URL parameter to the request, in the form of a name and a collection of integers. The integers will be combined into one, separated by commas.
    /// </summary>
    public Request WithUrlParameter(string name, IEnumerable<int> values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(values);

        UrlParams.Add(name, string.Join(',', values));

        return this;
    }

    /// <summary>
    /// Adds a URL parameter to the request, in the form a collection of keyvalue-pairs of strings, where the key is the name of the URL param and the value is the value. The value will be combined into one string, separated by commas.
    /// </summary>
    public Request WithUrlParameters(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        foreach (var currentURLParam in parameters)
        {
            if (string.IsNullOrWhiteSpace(currentURLParam.Key)) throw new ArgumentException("A key in parameter 'parameters' is null or empty string");
            if (string.IsNullOrWhiteSpace(currentURLParam.Value)) throw new ArgumentException($"The value for parameter '{currentURLParam.Key}' is null or empty string");
            UrlParams.Add(currentURLParam.Key, currentURLParam.Value);
        }

        return this;
    }

    /// <summary>
    /// Adds a URL parameter to the request, in the form a collection of dictionary of strings, where the key is the name of the URL param and the value is the value. The value will be combined into one string, separated by commas.
    /// </summary>
    public Request WithUrlParameters(IDictionary<string, string> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        foreach (var currentURLParam in parameters)
        {
            if (string.IsNullOrWhiteSpace(currentURLParam.Key)) throw new ArgumentException("A key in parameter 'parameters' is null or empty string");
            if (string.IsNullOrWhiteSpace(currentURLParam.Value)) throw new ArgumentException($"The value for parameter '{currentURLParam.Key}' is null or empty string");
            UrlParams.Add(currentURLParam.Key, currentURLParam.Value);
        }

        return this;
    }

    #endregion

    #region HEADERS

    /// <summary>
    /// Adds an HTTP-header to the request, in the form of a name and a string-value.
    /// </summary>
    public Request WithHeader(string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        Headers.Add(new KeyValuePair<string, string>(name, value));
        return this;
    }

    /// <summary>
    /// Adds an HTTP-header to the request, in the form of a keyvalue-pair of strings, where the key is the header-name and the value are the values.
    /// </summary>
    public Request WithHeader(KeyValuePair<string, string> header)
    {
        Headers.Add(header);
        return this;
    }

    /// <summary>
    /// Adds several HTTP-headers to the request, in the form of a collection of keyvalue-pairs of strings, where the key is the header-name and the value are the values.
    /// </summary>
    public Request WithHeaders(IEnumerable<KeyValuePair<string, string>> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        foreach (KeyValuePair<string, string> currentHeader in headers!)
        {
            if (string.IsNullOrWhiteSpace(currentHeader.Key)) throw new ArgumentException("A key in parameter 'parameters' is null or empty string");
            if (string.IsNullOrWhiteSpace(currentHeader.Value)) throw new ArgumentException($"The value for parameter '{currentHeader.Key}' is null or empty string");

            Headers.Add(currentHeader);
        }

        return this;
    }

    public KnownHeaders WithKnownHeaders()
    {
        return new KnownHeaders(new(Headers));
    }

    #endregion

    /// <summary>
    /// Sends the request with the method, url parameters and body that has been configured and returns the response.
    /// </summary>
    /// <returns>The response to the request, allowing you to inspect statuscodes, headers etc and consume the body (if it has any).</returns>
    public async Task<HttpResponse> Send()
    {
        Debug.WriteLine($"Sending request");
        string requestURL = Destination.ToString();

        if (UrlParams.Count > 0)
        {
            string? UrlParamString = UrlParams.ToString();
            Debug.WriteLine($"With {UrlParams.Count} URL-params");
            requestURL = Destination + "?" + UrlParamString;
        }

        TheRequest = new HttpRequestMessage(Method, requestURL);

        if (Body is not null)
        {
            TheRequest.Content = Body;
            Debug.WriteLine($"With a body (type: {TheRequest.Content.Headers.ContentType?.MediaType ?? "application/octet-stream"})");
        }

        if (Headers.Count > 0)
        {
            Debug.WriteLine($"With {Headers.Count} headers");
            foreach (var currentHeader in Headers)
            {
                TheRequest.Headers.Add(currentHeader.Key, currentHeader.Value);
            }
        }

        Debug.WriteLine($"Awaiting response... (timeout: {HttpClient.Timeout})");
        HttpResponseMessage Response;
        var Timer = Stopwatch.StartNew();

        try
        {
            Response = await HttpClient.SendAsync(TheRequest);
        }
        catch (HttpRequestException error)
        {
            throw new Exception($"Error encountered while sending HTTP request:" + Environment.NewLine + error);
        }
        catch(TaskCanceledException)
        {
            throw new TimeoutException($"HTTP request was cancelled when destination failed to respond within the timeout");
        }

        Debug.WriteLine($"Received response with status {(int)Response.StatusCode} (time taken: {Timer.Elapsed})");
        var ReturnData = new HttpResponse(Response);

        string responseBodyOutput = $"Body present in response? {ReturnData.RawContent.Length > 0}";

        if (ReturnData.RawContent.Length > 0 && ReturnData.GetContentHeaders().TryGetValue("Content-Type", out string? contentType))
        {
            responseBodyOutput += $" ({contentType})";
        }

        Debug.WriteLine(responseBodyOutput);

        return ReturnData;
    }

    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        TheRequest?.Dispose();
    }
}
