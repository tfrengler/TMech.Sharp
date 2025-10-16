using System;
using System.Net.Http;

namespace CZ.DM.Art.Core.HttpService
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

        public HttpRequest NewPutRequest(string? destination = null) => new HttpRequest(this, HttpMethod.Put, destination);
        public HttpRequest NewPostRequest(string? destination = null) => new HttpRequest(this, HttpMethod.Post, destination);
        public HttpRequest NewGetRequest(string? destination = null) => new HttpRequest(this, HttpMethod.Get, destination);
        public HttpRequest NewDeleteRequest(string? destination = null) => new HttpRequest(this, HttpMethod.Delete, destination);
    }
}