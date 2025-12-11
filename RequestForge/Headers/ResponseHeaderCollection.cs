using System;

namespace RequestForge.Headers;

public sealed record ResponseHeaderCollection
{
    public ResponseHeaderCollection(ResponseHeaders response, ResponseBodyHeaders body)
    {
        Response = response;
        Body = body;
    }

    public ResponseHeaders Response { get; }
    public ResponseBodyHeaders Body { get; }

    public override string ToString()
    {
        return string.Join(Environment.NewLine, [
            Response.ToString(),
            Body.ToString()
        ]);
    }
}
