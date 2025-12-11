using RequestForge.Builders;
using RequestForge.Configuration;
using RequestForge.Headers;
using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Xml.Linq;

namespace RequestForge.Core
{
    public sealed class Request : IDisposable
    {
        internal Request(RequestConfiguration config)
        {
            if (!config.TryValidate(out var errors))
            {
                throw errors;
            }

            _config = config;

            _httpClient = new HttpClient(config.Handler)
            {
                BaseAddress = new Uri(config.BaseAddress),
                Timeout = config.RequestTimeout
            };

            _request = new HttpRequestMessage()
            {
                Method = _config.Method
            };
        }

        private bool _isDisposed;
        private readonly RequestConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly HttpRequestMessage _request = null!;

        #region MISC

        public Request WithTimeOut(TimeSpan timeout)
        {
            _config.RequestTimeout = timeout;
            return this;
        }

        public Request WithHeader(string name, string? value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            
            if (value is not null)
            {
                _config.Headers[name] = value;
            }

            return this;
        }

        public Request WithKnownHeaders(Action<KnownHeaders> headers)
        {
            headers(new KnownHeaders(_config.Headers));
            return this;
        }

        #endregion

        #region URL PARAMETERS

        /// <summary>
        /// <para>Adds a URL parameter with the given name where the value is a string.</para>
        /// <para>This operation is additive. If you add another value to an existing parameter with the same name it is appended to the list of values.</para>
        /// <paramref name="name"/> cannot be <c>null</c> or empty. If <paramref name="value"/> is <c>null</c> nothing is added.
        /// </summary>
        public Request WithUrlParameter(string name, string? value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (value is not null)
            {
                _config.UrlParameters.Add(name, value);
            }

            return this;
        }

        public Request WithUrlParameter(string name, long value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            _config.UrlParameters.Add(name, ContentBuilder.IntegerValue(value));
            return this;
        }

        /// <summary>
        /// <para>Adds a URL parameter with the given name and floating-point value with dots as separators and 3 digits of precision. Example <c>-123,4567</c> would become <c>-123.457</c></para>
        /// <para>This operation is additive. If you add another value to an existing parameter with the same name it is appended to the list of values.</para>
        /// <paramref name="name"/> cannot be <c>null</c> or empty.
        /// </summary>
        public Request WithUrlParameter(string name, double value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            _config.UrlParameters.Add(name, ContentBuilder.DecimalNumberValue(value));
            return this;
        }

        /// <summary>
        /// <para>Adds a URL parameter with the given name an floating-point value in a format of your choice.</para>
        /// <para>This operation is additive. If you add another value to an existing parameter with the same name it is appended to the list of values.</para>
        /// <paramref name="name"/> cannot be <c>null</c> or empty.
        /// </summary>
        public Request WithUrlParameter(string name, double value, NumberFormatInfo formatting)
        {
            ArgumentNullException.ThrowIfNull(formatting);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            _config.UrlParameters.Add(name, value.ToString("C", formatting));
            return this;
        }

        /// <summary>
        /// <para>Adds a URL parameter with the given name and boolean value as <c>True</c> or <c>False</c></para>
        /// <para>This operation is additive. If you add another value to an existing parameter with the same name it is appended to the list of values.</para>
        /// <paramref name="name"/> cannot be <c>null</c> or empty.
        /// </summary>
        public Request WithUrlParameter(string name, bool value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            _config.UrlParameters.Add(name, ContentBuilder.BooleanValue(value));
            return this;
        }

        /// <summary>
        /// <para>Adds a URL parameter with the given name and DateTime value in ISO 8601 format (<c>YYYY-MM-DDTHH:MM:SS.0000000-HH:MM</c>)</para>
        /// <para>This operation is additive. If you add another value to an existing parameter with the same name it is appended to the list of values.</para>
        /// <paramref name="name"/> cannot be <c>null</c> or empty.
        /// </summary>
        public Request WithUrlParameter(string name, DateTime value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            _config.UrlParameters.Add(name, ContentBuilder.DateTimeValue(value));
            return this;
        }

        /// <summary>
        /// <para>Adds a URL parameter with the given name and DateTime value in a format of your choice.</para>
        /// <para>This operation is additive. If you add another value to an existing parameter with the same name it is appended to the list of values.</para>
        /// <paramref name="name"/> cannot be <c>null</c> or empty.
        /// </summary>
        public Request WithUrlParameter(string name, DateTime value, string format)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            _config.UrlParameters.Add(name, ContentBuilder.DateTimeValue(value, format));
            return this;
        }

        /// <summary>
        /// <para>Adds a URL parameter with the given name and DateOnly value in YYYY-MM-DD format.</para>
        /// <para>This operation is additive. If you add another value to an existing parameter with the same name it is appended to the list of values.</para>
        /// <paramref name="name"/> cannot be <c>null</c> or empty.
        /// </summary>
        public Request WithUrlParameter(string name, DateOnly value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            _config.UrlParameters.Add(name, ContentBuilder.DateOnlyValue(value));
            return this;
        }

        /// <summary>
        /// <para>Adds a URL parameter with the given name and DateOnly value in a format of your choice.</para>
        /// <para>This operation is additive. If you add another value to an existing parameter with the same name it is appended to the list of values.</para>
        /// <paramref name="name"/> cannot be <c>null</c> or empty.
        /// </summary>
        public Request WithUrlParameter(string name, DateOnly value, string format)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            _config.UrlParameters.Add(name, ContentBuilder.DateOnlyValue(value, format));
            return this;
        }

        #endregion

        #region TEMPLATE PARAMETERS

        public Request WithTemplateParameter(string name, string? value)
        {
            AddTemplateParameter(name, value);
            return this;
        }

        public Request WithTemplateParameter(string name, long value)
        {
            AddTemplateParameter(name, ContentBuilder.IntegerValue(value));
            return this;
        }

        public Request WithTemplateParameter(string name, double value)
        {
            AddTemplateParameter(name, ContentBuilder.DecimalNumberValue(value));
            return this;
        }

        public Request WithTemplateParameter(string name, double value, NumberFormatInfo formatting)
        {
            ArgumentNullException.ThrowIfNull(formatting);

            AddTemplateParameter(name, value.ToString("C", formatting));
            return this;
        }

        public Request WithTemplateParameter(string name, bool value)
        {
            AddTemplateParameter(name, ContentBuilder.BooleanValue(value));
            return this;
        }

        public Request WithTemplateParameter(string name, DateTime value)
        {
            AddTemplateParameter(name, ContentBuilder.DateOnlyValue(value));
            return this;
        }

        public Request WithTemplateParameter(string name, DateTime value, string format)
        {
            AddTemplateParameter(name, ContentBuilder.DateTimeValue(value, format));
            return this;
        }

        public Request WithTemplateParameter(string name, DateOnly value)
        {
            AddTemplateParameter(name, ContentBuilder.DateOnlyValue(value));
            return this;
        }

        public Request WithTemplateParameter(string name, DateOnly value, string format)
        {
            AddTemplateParameter(name, ContentBuilder.DateOnlyValue(value, format));
            return this;
        }

        private Request AddTemplateParameter(string name, string? value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (_config.RelativeDestination is not null)
            {
                _config.RelativeDestination = _config.RelativeDestination.Replace('{' + name + '}', value);
            }

            return this;
        }

        #endregion

        #region BODY

        public Request WithXmlBody(XElement? body, MediaTypeHeaderValue? mediaType = null)
        {
            if (body is null)
            {
                return this;
            }

            _request.Content = ContentBuilder.XmlBody(body, mediaType);
            return this;
        }

        public Request WithSoapXmlBody(XElement? body, string action, SoapVersion version)
        {
            if (body is null)
            {
                return this;
            }

            _request.Content = ContentBuilder.SoapXmlBody(body, action, version);
            return this;
        }

        public Request WithXmlBody(string? body)
        {
            if (body is null) return this;
            _request.Content = ContentBuilder.XmlBody(body);
            return this;
        }

        public Request WithJsonBody(string? body)
        {
            if (body is null) return this;
            _request.Content = new StringContent(body, StandardMediaTypes.Json);
            return this;
        }

        public Request WithJsonBody<T>(T? body, JsonSerializerOptions? options = null)
        {
            if (body is null) return this;

            _request.Content = ContentBuilder.JsonBody(body, options);

            return this;
        }
        
        public Request WithPlainTextBody(string? body)
        {
            if (body is null) return this;
            _request.Content = ContentBuilder.PlainTextBody(body);

            return this;
        }

        public Request WithByteArrayBody(byte[]? body, MediaTypeHeaderValue? contentType = null)
        {
            if (body is null) return this;

            _request.Content = ContentBuilder.ByteArrayBody(body, contentType);

            return this;
        }

        public Request WithMultipartFormBody(Action<MultipartFormBuilder> builderDelegate)
        {
            ArgumentNullException.ThrowIfNull(builderDelegate);

            using (var multipartBuilder = MultipartFormBuilder.Create())
            {
                builderDelegate(multipartBuilder);
                _request.Content = multipartBuilder.Build();
            }

            return this;
        }

        public Request WithUrlEncodedBody(Action<UrlEncodedFormBuilder> builderDelegate)
        {
            ArgumentNullException.ThrowIfNull(builderDelegate);

            var multipartBuilder = UrlEncodedFormBuilder.Create();
            builderDelegate(multipartBuilder);
            _request.Content = multipartBuilder.Build();

            return this;
        }

        #endregion

        public Response WhenSendingRequest()
        {
            string destination = _config.RelativeDestination ?? string.Empty;
            if (_config.UrlParameters.Count > 0)
            {
                destination = destination + '?' + _config.UrlParameters.ToString();
            }

            if (destination.Length > 0)
            {
                _request.RequestUri = new Uri(destination, UriKind.Relative);
            }

            //Console.WriteLine($"DEBUG: Body? {_request.Content?.GetType().ToString() ?? "false"}");
            
            //if (_request.Content is not null)
            //{
            //    Console.WriteLine(_request.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            //}

            return new Response(_httpClient, _request);
        }

        public void Dispose()
        {
            if (!_config.OwnsHandler || _isDisposed) return;
            _isDisposed = true;
            _httpClient.Dispose();
        }
    }
}
