using System.Collections.Generic;

namespace TMech.Sharp.HttpRequestService;

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

    public KnownHeaders Authorization_Bearer(string? tokenValue)
    {
        if (tokenValue is not null)
        {
            _headers.Add("Authorization", "Bearer " + tokenValue);
        }

        return this;
    }
}

