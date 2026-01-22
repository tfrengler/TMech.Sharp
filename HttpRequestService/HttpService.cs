using System;
using System.Net;
using System.Net.Http;

namespace TMech.Sharp.HttpRequestService;

public sealed class HttpService
{
    private readonly static HttpMessageHandler _handler = new SocketsHttpHandler()
    {
        Credentials = CredentialCache.DefaultCredentials
    };

    private readonly HttpClient _httpClient;

    public HttpService(string baseAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseAddress);

        _httpClient = new HttpClient(_handler, false)
        {
            BaseAddress = new Uri(baseAddress),
            Timeout = TimeSpan.FromSeconds(30.0d)
        };
    }

    public Request NewPutRequest(string? destination = null) => new(_httpClient, HttpMethod.Put, destination);
    public Request NewPostRequest(string? destination = null) => new(_httpClient, HttpMethod.Post, destination);
    public Request NewGetRequest(string? destination = null) => new(_httpClient, HttpMethod.Get, destination);
    public Request NewDeleteRequest(string? destination = null) => new(_httpClient, HttpMethod.Delete, destination);
}