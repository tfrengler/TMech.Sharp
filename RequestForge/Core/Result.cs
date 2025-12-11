using RequestForge.Headers;
using System;
using System.Collections.Generic;
using System.Net;

namespace RequestForge.Core;

public sealed record Result
{
    public Result(List<string> validationErrors, HttpStatusCode status, byte[] responseBody, ResponseHeaderCollection headers, object? responseBodyTyped)
    {
        ArgumentNullException.ThrowIfNull(validationErrors);
        ArgumentNullException.ThrowIfNull(responseBody);
        ArgumentNullException.ThrowIfNull(headers);

        Errors = validationErrors;
        HttpStatus = status;
        ResponseBodyRaw = responseBody;
        Headers = headers;
        _responseBody = responseBodyTyped;
    }

    public ResponseHeaderCollection Headers { get; }
    public List<string> Errors { get; } = [];
    public HttpStatusCode HttpStatus { get; }
    public byte[] ResponseBodyRaw { get; init; } = [];

    private readonly object? _responseBody;
    public T GetResponseBody<T>()
    {
        if (_responseBody is T returnData)
        {
            return returnData;
        }

        throw new Exception($"Tried getting response body as type {typeof(T)} but it is in fact {_responseBody?.GetType().ToString() ?? "null"}");
    }

    public Type? GetResponseBodyType() => _responseBody?.GetType();
}
