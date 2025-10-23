using System;
using System.Net.Http;

namespace TMech.Sharp.HttpService
{
    public sealed class HttpService
    {
        public static readonly HttpClientHandler Handler = new HttpClientHandler { UseDefaultCredentials = true };

        public HttpClient HttpClient { get; }
        public Uri BaseAddress { get; }

        public HttpService(string baseAddress)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(baseAddress);

            BaseAddress = new Uri(baseAddress);
            HttpClient = new HttpClient(Handler, false)
            {
                Timeout = TimeSpan.FromSeconds(30.0d)
            };
        }

        public Request NewPutRequest(string? destination = null) => new(this, HttpMethod.Put, destination);
        public Request NewPostRequest(string? destination = null) => new(this, HttpMethod.Post, destination);
        public Request NewGetRequest(string? destination = null) => new(this, HttpMethod.Get, destination);
        public Request NewDeleteRequest(string? destination = null) => new(this, HttpMethod.Delete, destination);
    }
}