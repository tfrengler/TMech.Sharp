using System;
using System.Collections.Generic;
using System.Net;

namespace TMech.Sharp.RequestMonkey;

public sealed record Result
{
    public Result(List<string> validationErrors, HttpStatusCode status, byte[] responseBody, HeaderCollection headers, object? responseBodyTyped)
    {
        Errors = validationErrors;
        HttpStatus = status;
        ResponseBodyRaw = responseBody;
        Headers = headers;
        _responseBody = responseBodyTyped;
    }

    public HeaderCollection Headers { get; }
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

        throw new Exception($"Tried getting response body as type {typeof(T)} but it is in fact {_responseBody?.GetType()}");
    }

    public Type? GetResponseBodyType() => _responseBody?.GetType();
}

public sealed record HeaderCollection
{
    public HeaderCollection(ResponseHeaders response, ResponseBodyHeaders body)
    {
        Response = response;
        Body = body;
    }

    public ResponseHeaders Response { get; }
    public ResponseBodyHeaders Body { get; }
}
