using RequestForge.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RequestForge.Core;

public sealed class RequestForge
{
    private RequestForge(RequestConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config;
    }

    private readonly RequestConfiguration _config;
    private HttpMessageHandler? _messageHandler;

#warning TODO(Thomas): Consider making this configurable by the caller
    public static JsonSerializerOptions DefaultJsonSerializerOptions { get; } = new JsonSerializerOptions()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(null, true) },
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    #region STATIC / INITIALIZATION

    public static RequestForge FromBaseAddress(string baseAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseAddress);
        
        return new RequestForge(new RequestConfiguration()
        {
            BaseAddress = baseAddress
        });
    }

    /// <summary>
    /// <para>Creates an instance and allows you to configure settings via a delegate.</para>
    /// </summary>
    public static RequestForge FromConfiguration(Action<FullConfiguration> configurationDelegate)
    {
        var config = new RequestConfiguration();
        configurationDelegate(config);
        return new RequestForge(config);
    }

    /// <summary>
    /// <para>Creates an instance that uses the provided message handler instead of the internal one and allows you to configure settings via a delegate.</para>
    /// </summary>
    public static RequestForge FromConfiguration(HttpClientHandler handler, Action<BaseConfiguration> configurationDelegate)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var config = new RequestConfiguration();
        configurationDelegate(config);

        return new RequestForge(config)
        {
            _messageHandler = handler
        };
    }

    #endregion

    #region CONFIG/SETUP

    /// <summary>
    /// <para>Configures the underlying httpclient to use this message handler instead of its own internal instance.</para>
    /// <para><c>NOTE:</c> When providing your own handler many of the configuration methods become no-op's since they are configured on the handler level:
    /// <list type="bullet">
    ///     <item>UsingDefaultCredentials</item>
    ///     <item>UsingCredentials</item>
    ///     <item>FollowingRedirects</item>
    /// </list>
    /// </para>
    /// </summary>
    public RequestForge WithHttpHandler(HttpMessageHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _messageHandler = handler;

        return this;
    }

    public RequestForge UsingDefaultCredentials()
    {
        _config.Credentials = CredentialCache.DefaultCredentials;
        return this;
    }

    public RequestForge UsingCredentials(ICredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        _config.Credentials = credentials;
        return this;
    }

    public RequestForge FollowingRedirects()
    {
        _config.AllowAutoRedirect = true;
        return this;
    }

    /// <summary>
    /// Sets the timeout for all HTTP requests made. This can be overridden per <see cref="Request"/> spawned by PATCH, GET etc.
    /// </summary>
    public RequestForge WithTimeout(TimeSpan timeout)
    {
        _config.RequestTimeout = timeout;
        return this;
    }

    public RequestForge WithJsonHandlingOptions(JsonSerializerOptions option)
    {
        _config.JsonOptions = option;
        return this;
    }

    public RequestForge WithDefaultHeaders(IDictionary<string,string> headers)
    {
        foreach(KeyValuePair<string,string> currentHeader in headers)
        {
            _config.DefaultHeaders[currentHeader.Value] = currentHeader.Value;
        }

        return this;
    }

    #endregion

    #region REQUEST CREATION

    public Request GET(string? relativeDestination = null) { return CreateRequest(HttpMethod.Get, relativeDestination); }
    public Request PATCH(string? relativeDestination  = null) { return CreateRequest(HttpMethod.Patch, relativeDestination); }
    public Request DELETE(string? relativeDestination = null) { return CreateRequest(HttpMethod.Delete, relativeDestination); }
    public Request POST(string? relativeDestination = null) { return CreateRequest(HttpMethod.Post, relativeDestination); }
    public Request PUT(string? relativeDestination = null) { return CreateRequest(HttpMethod.Put, relativeDestination); }
    public Request HEAD(string? relativeDestination = null) { return CreateRequest(HttpMethod.Head, relativeDestination); }
    public Request OPTIONS(string? relativeDestination = null) { return CreateRequest(HttpMethod.Options, relativeDestination); }

    private Request CreateRequest(HttpMethod method, string? relativeDestination = null)
    {
        _config.OwnsHandler = _messageHandler is null;
        var messageHandler = _messageHandler ?? new SocketsHttpHandler();

        if (_config.OwnsHandler)
        {
            var handler = (SocketsHttpHandler)messageHandler;
            handler.AllowAutoRedirect = _config.AllowAutoRedirect;
            handler.Credentials = _config.Credentials;
        }

        _config.Handler = messageHandler;
        _config.Method = method;
        _config.RelativeDestination = relativeDestination;

        return new Request(_config with { });
    }

    #endregion
}
