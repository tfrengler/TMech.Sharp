using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Web;

namespace TMech.Sharp.RequestMonkey;

/// <summary>
/// Includes base settings in addition to the ones used by the underlying message handler.
/// </summary>
public record FullConfiguration : BaseConfiguration
{
    public ICredentials? Credentials { get; set; }
    public bool AllowAutoRedirect { get; set; }
}

/// <summary>
/// Settings that apply per request (set on the HttpClient or used by <see cref="Request"/> directly).
/// </summary>
public record BaseConfiguration
{
    public string BaseAddress { get; set; } = string.Empty;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public IDictionary<string, string> DefaultHeaders { get; } = new Dictionary<string, string>();
}

public sealed record RequestConfiguration : FullConfiguration
{
    public string? RelativeDestination { get; set; }
    public HttpMethod Method { get; set; } = null!;
    public HttpMessageHandler Handler { get; set; } = null!;
    public bool OwnsHandler { get; set; }
    public Dictionary<string, string> Headers { get; } = [];
    public NameValueCollection UrlParameters { get; } = HttpUtility.ParseQueryString(string.Empty);
    public JsonSerializerOptions JsonOptions { get; set; }

    public RequestConfiguration()
    {
        JsonOptions = RequestForge.DefaultJsonSerializerOptions;
    }

    public bool TryValidate([NotNullWhen(false)] out AggregateException? errors)
    {
        var errorList = new List<Exception>();

        if (RelativeDestination is not null && RelativeDestination.Trim().Length == 0)
        {
            errorList.Add(new Exception("Relative destination cannot be empty string or consist entirely of whitespace"));
        }

        if (Method is null)
        {
            errorList.Add(new Exception("Method cannot be null"));
        }

        if (Handler is null)
        {
            errorList.Add(new Exception("Handler cannot be null"));
        }

        if (errorList.Count == 0)
        {
            errors = null;
            return true;
        }

        errors = new AggregateException(errorList);
        return false;
    }
}
