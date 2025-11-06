using CZ.DM.Art.Core.Shared;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using System.Xml.Linq;
using TMech.Sharp.HttpService;

namespace TMech.Sharp.RequestMonkey
{
    public sealed class Request : IDisposable
    {
        public Request(RequestConfiguration config)
        {
            if (!config.TryValidate(out var errors))
            {
                throw errors;
            }

            _config = config;

            _httpClient = new HttpClient(config.Handler)
            {
                Timeout = config.RequestTimeout
            };

            _request = new HttpRequestMessage()
            {
                Method = _config.Method
            };
        }

        private readonly RequestConfiguration _config;
        private bool _isDisposed;
        private readonly HttpClient _httpClient;
        private HttpRequestMessage _request = null!;

        /// <summary>
        /// The default number parsing format to use when outputting decimal numbers (floats and doubles) in URL-params or as part of the path:
        /// <list type="bullet">
        ///     <item>Dot (.) as decimal separator</item>
        ///     <item>No group (thousand) separator</item>
        ///     <item>No currency symbol</item>
        ///     <item>3 decimal places used</item>
        /// </summary>
        public static NumberFormatInfo DefaultDecimalFormat { get; } = new NumberFormatInfo()
        {
            CurrencyDecimalDigits = 3,
            CurrencyGroupSeparator = "",
            CurrencySymbol = ""
        };

        #region MISC

        public Request WithTimeOut(TimeSpan timeout)
        {
            _config.RequestTimeout = timeout;
            return this;
        }

        public Request WithHeader(string name, string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(value);

            _config.Headers[name] = value;
            return this;
        }

        public Request WithKnownHeaders(Func<KnownHeaders, KnownHeaders> headers) { return this; }

        #endregion

        #region URL PARAMETERS

        public Request WithUrlParameter(string name, string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(value);

            _config.UrlParameters.Add(name, value);
            return this;
        }

        public Request WithUrlParameter(string name, long value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            _config.UrlParameters.Add(name, Convert.ToString(value));
            return this;
        }

        public Request WithUrlParameter(string name, double value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            _config.UrlParameters.Add(name, value.ToString("C", DefaultDecimalFormat));
            return this;
        }

        public Request WithUrlParameter(string name, double value, NumberFormatInfo formatting)
        {
            ArgumentNullException.ThrowIfNull(formatting);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            _config.UrlParameters.Add(name, value.ToString("C", formatting));
            return this;
        }

        public Request WithUrlParameter(string name, bool value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            _config.UrlParameters.Add(name, Convert.ToString(value));
            return this;
        }

        public Request WithUrlParameter(string name, DateTime value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            _config.UrlParameters.Add(name, value.ToString("O"));
            return this;
        }

        public Request WithUrlParameter(string name, DateTime value, string format)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(format);

            _config.UrlParameters.Add(name, value.ToString(format));
            return this;
        }

        public Request WithUrlParameter(string name, DateOnly value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            _config.UrlParameters.Add(name, value.ToString("O"));
            return this;
        }

        public Request WithUrlParameter(string name, DateOnly value, string format)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(format);

            _config.UrlParameters.Add(name, value.ToString(format));
            return this;
        }

        #endregion

        #region TEMPLATE PARAMETERS

        public Request WithTemplateParameter(string name, string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            AddTemplateParameter(name, value);
            return this;
        }

        public Request WithTemplateParameter(string name, long value)
        {
            AddTemplateParameter(name, Convert.ToString(value));
            return this;
        }

        public Request WithTemplateParameter(string name, double value)
        {
            AddTemplateParameter(name, value.ToString("C", DefaultDecimalFormat));
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
            AddTemplateParameter(name, Convert.ToString(value));
            return this;
        }

        public Request WithTemplateParameter(string name, DateTime value)
        {
            AddTemplateParameter(name, value.ToString("O"));
            return this;
        }

        public Request WithTemplateParameter(string name, DateTime value, string format)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(format);

            AddTemplateParameter(name, value.ToString(format));
            return this;
        }

        public Request WithTemplateParameter(string name, DateOnly value)
        {
            AddTemplateParameter(name, value.ToString("O"));
            return this;
        }

        public Request WithTemplateParameter(string name, DateOnly value, string format)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(format);

            AddTemplateParameter(name, value.ToString(format));
            return this;
        }

        private Request AddTemplateParameter(string name, string value)
        {
            if (_config.RelativeDestination is null) return this;
            _config.RelativeDestination = _config.RelativeDestination.Replace('{' + name + '}', value);
            return this;
        }

        #endregion

        #region BODY

        public Request WithXmlBody(XElement body, MediaTypeHeaderValue? mediaType = null)
        {
            if (mediaType is null)
            {
                mediaType = MediaTypes.Xml;
            }

            _request.Content = new StringContent(XmlSerialization.Serialize(body), mediaType);
            return this;
        }

        public Request WithSoapXmlBody(XElement body, string action, SoapVersion version)
        {
            MediaTypeHeaderValue mediaType;

            switch (version)
            {
                case SoapVersion.v11:
                    mediaType = MediaTypes.TextXml;
                    _config.Headers.Add("SOAPAction", action);
                    break;
                case SoapVersion.v12:
                    mediaType = MediaTypes.SoapXml;
                    mediaType.Parameters.Add(new("action", $"\"{action}\""));
                    break;
                default:
                    throw new Exception("Invalid SoapVersion");
            }

            _request.Content = new StringContent(XmlSerialization.Serialize(body), mediaType);
            return this;
        }

        public Request WithXmlBody(string body)
        {
            _request.Content = new StringContent(body, MediaTypes.Xml);
            return this;
        }

        public Request WithJsonBody<T>(T body, JsonSerializerOptions? options = null) { return this;}
        public Request WithJsonBody(string body) { return this;}
        public Request WithPlainTextBody(string body) { return this;}
        public Request WithByteArrayBody(byte[] body) { return this;}
        public Request WithMultipartFormBody(Action<MultipartFormBuilder> builderDelegate) { return this; }
        public Request WithUrlEncodedBody(Action<UrlEncodedFormBuilder> builderDelegate) { return this; }

        #endregion

        public Response Send()
        {
            string destination = _config.RelativeDestination ?? string.Empty;
            if (_config.UrlParameters.Count > 0)
            {
                destination = destination + '?' + _config.UrlParameters.ToString();
            }

            if (destination.Length > 0)
            {
                _request.RequestUri = new Uri(destination);
            }

            return new Response(_httpClient, _request);
        }

        public void Dispose()
        {
            if (!_config.OwnsHandler || _isDisposed) return;
            _isDisposed = true;
            _httpClient.Dispose();
        }
    }

    public sealed record RequestConfiguration: FullConfiguration
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
}
