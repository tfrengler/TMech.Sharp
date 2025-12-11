using System.Collections.Generic;

namespace RequestForge.Headers;

public sealed class KnownHeaders
{
    private readonly Dictionary<string,string> _headers;

    public KnownHeaders(Dictionary<string, string> headers)
    {
        _headers = headers;
    }

    public KnownHeaders Accept(string? value)
    {
        if (value is not null)
        {
            _headers.Add("Accept", value);
        }

        return this;
    }
}

