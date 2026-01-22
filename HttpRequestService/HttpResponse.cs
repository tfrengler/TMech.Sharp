using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Xml.Linq;

namespace TMech.Sharp.HttpRequestService;

/// <summary>
/// <para>Represents the response to an HTTP request, exposing the status code and headers, as well as various methods to consume the contents of the body as bytes, string, JSON etc.</para>
/// <para>The various consume-methods are idempotent and can be called repeatedly. The raw response body (if it has any) is buffered after the first call to any of the consume-methods.</para>
/// </summary>
public class HttpResponse : IDisposable
{
    public HttpResponse(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        Response = response;

        _RawContentBuffer = new Lazy<byte[]>(() =>
        {
            if (Response.Content is not null)
            {
                return Response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            }

            throw new Exception("Unable to get response body as bytes as it appears to have no body");
        });

        _StringContentBuffer = new Lazy<string>(() =>
        {
            return System.Text.Encoding.UTF8.GetString(RawContent);
        });
    }

    private readonly HttpResponseMessage Response;

    private readonly Lazy<byte[]> _RawContentBuffer;
    public byte[] RawContent => _RawContentBuffer.Value;

    private readonly Lazy<string> _StringContentBuffer;
    public string StringContent => _StringContentBuffer.Value;

    private bool IsDisposed;
    private const int TruncateStringContentBeyond = 4096;

    private string GetStringContentForErrorOutput()
    {
        if (StringContent.Length <= TruncateStringContentBeyond)
        {
            return StringContent;
        }

        return string.Concat(StringContent.AsSpan(0, TruncateStringContentBeyond), $" ...TRUNCATED! (at {TruncateStringContentBeyond} characters). Full size: {StringContent.Length} characters");
    }

    /// <summary>
    /// Returns a collection of headers. Multiple values are delimited by vertical pipe (|).
    /// </summary>
    public Dictionary<string, string> GetHeaders()
    {
        var returnData = new Dictionary<string, string>();

        foreach (var current in Response.Headers)
        {
            returnData.Add(current.Key, string.Join('|', current.Value));
        }

        return returnData;
    }

    public string GetStatusCodeAsString()
    {
        return ((int)Response.StatusCode).ToString();
    }

    public HttpStatusCode GetStatusCode()
    {
        return Response.StatusCode;
    }

    public int GetStatusCodeAsInt()
    {
        return (int)Response.StatusCode;
    }

    public bool IsSuccessResponse()
    {
        return (int)Response.StatusCode >= 200 && (int)Response.StatusCode <= 299;
    }

    public bool IsClientErrorResponse()
    {
        return (int)Response.StatusCode >= 400 && (int)Response.StatusCode <= 499;
    }

    public bool IsServerErrorResponse()
    {
        return (int)Response.StatusCode >= 500 && (int)Response.StatusCode <= 599;
    }

    public bool IsErrorResponse()
    {
        return (int)Response.StatusCode >= 400 && (int)Response.StatusCode <= 599;
    }

    public bool IsRedirectResponse()
    {
        return (int)Response.StatusCode >= 300 && (int)Response.StatusCode <= 399;
    }

    /// <summary>
    /// Returns a collection of headers relevant to the body (ContentType,ContentDisposition,ContentEncoding,ContentLength etc).<br/>
    /// Throws an exception if the response does not have a body. Multi-values are delimited by vertical pipe (|).
    /// </summary>
    public Dictionary<string, string> GetContentHeaders()
    {
        if (RawContent.Length == 0)
        {
            throw new Exception("Unable to get response body headers as it appears to have no body");
        }

        var returnData = new Dictionary<string, string>();

        foreach (var current in Response.Content.Headers)
        {
            returnData.Add(current.Key, string.Join('|', current.Value));
        }

        return returnData;
    }

    /// <summary>
    /// Attempts to read the response body and returns the content as a byte array.<br/>
    /// Throws an exception if the response does not have a body.
    /// </summary>
    /// <exception cref="Exception"></exception>
    public byte[] ConsumeAsRawBytes()
    {
        return RawContent;
    }

    /// <summary>
    /// Attempts to read the response body and parse the content as string.<br/>
    /// Throws an exception if the response does not have a body.
    /// </summary>
    /// <exception cref="Exception"></exception>
    public string ConsumeAsString()
    {
        return StringContent;
    }

    /// <summary>
    /// Attempts to read the response body and parse the content as an integer.<br/>
    /// Throws an exception if the response does not have a body.
    /// </summary>
    public int ConsumeAsInteger()
    {
        int returnData;

        try
        {
            returnData = Convert.ToInt32(StringContent);
        }
        catch (FormatException parseException)
        {
            throw new Exception($"Unable to parse response body as integer: {Environment.NewLine} {GetStringContentForErrorOutput()}", parseException);
        }

        return returnData;
    }

    /// <summary>
    /// Attempts to deserialize the response body to an instance of <typeparamref name="T"/>. An exception is thrown if the body does not contain valid JSON that can be parsed.
    /// </summary>
    public T ConsumeAsJson<T>()
    {
        T? returnData;

        try
        {
            returnData = JsonSerializer.Deserialize<T>(StringContent);
        }
        catch (JsonException deserializeError)
        {
            throw new Exception($"Unable to deserialize response body as JSON: {Environment.NewLine}{GetStringContentForErrorOutput()}", deserializeError);
        }

        Debug.Assert(returnData is not null);
        return returnData;
    }

    /// <summary>
    /// Attempts to deserialize the response body to an instance of <typeparamref name="T"/>. Returns true if it succeeded, and false otherwise.
    /// </summary>
    /// <param name="output">The result of converting the body to JSON. Will be null if the method returns false.</param>
    public bool TryConsumeAsJson<T>([NotNullWhen(true)] out T? output)
    {
        T? returnData;

        try
        {
            returnData = JsonSerializer.Deserialize<T>(StringContent);
        }
        catch (JsonException)
        {
            output = default;
            return false;
        }

        Debug.Assert(returnData is not null);
        output = returnData;
        return true;
    }

    /// <summary>
    /// Attempts to parse the response body as XML and return instance of <see cref="XDocument"/>. An exception is thrown if the body does not contain valid XML that can be parsed.
    /// </summary>
    public XElement ConsumeAsXml()
    {
        XDocument returnData;

        try
        {
            returnData = XDocument.Parse(StringContent);
        }
        catch (System.Xml.XmlException parseError)
        {
            throw new Exception($"Unable to parse response body as XML: {Environment.NewLine} {GetStringContentForErrorOutput()}", parseError);
        }

        Debug.Assert(returnData.Root is not null);
        return returnData.Root;
    }

    public bool TryConsumeAsXml([NotNullWhen(true)] out XElement? output)
    {
        try
        {
            var parsedData = XDocument.Parse(StringContent);
            Debug.Assert(parsedData.Root is not null);
            output = parsedData.Root;

            return true;
        }
        catch (System.Xml.XmlException)
        {
            output = null;
            return false;
        }
    }

    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        Response?.Dispose();
        GC.SuppressFinalize(this);
    }

    ~HttpResponse()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        Response?.Dispose();
    }
}
