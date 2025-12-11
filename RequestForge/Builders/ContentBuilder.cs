using RequestForge.Serialization;
using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Xml.Linq;

namespace RequestForge.Builders;

/// <summary>
/// Centralized class for creating URL-param content, request body content etc.
/// </summary>
internal static class ContentBuilder
{
    #region REQUEST BODIES

    internal static HttpContent XmlBody(XElement xml, MediaTypeHeaderValue? mediaType = null)
    {
        if (mediaType is null)
        {
            mediaType = StandardMediaTypes.Xml;
        }

        return new StringContent(XmlSerialization.Serialize(xml), mediaType);
    }

    internal static HttpContent SoapXmlBody(XElement body, string action, SoapVersion version = SoapVersion.v12)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(action);

        MediaTypeHeaderValue mediaType = version switch
        {
            SoapVersion.v11 => StandardMediaTypes.TextXml,
            SoapVersion.v12 => StandardMediaTypes.SoapXml,
            _ => throw new NotImplementedException()
        };
        var returnData = new StringContent(XmlSerialization.Serialize(body), mediaType);

        switch (version)
        {
            case SoapVersion.v11:
                returnData.Headers.Add("SOAPAction", action);
                break;
            case SoapVersion.v12:
                mediaType.Parameters.Add(new("action", $"\"{action}\""));
                break;
            default:
                throw new NotImplementedException();
        }

        return returnData;
    }

    internal static HttpContent XmlBody(string body)
    {
        ArgumentNullException.ThrowIfNull(body);
        return new StringContent(body, StandardMediaTypes.Xml);
    }

    internal static HttpContent JsonBody(string? body)
    {
        ArgumentNullException.ThrowIfNull(body);
        return new StringContent(body, StandardMediaTypes.Json);
    }

    internal static HttpContent JsonBody<T>(T body, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(body);

        return JsonContent.Create<T>(
            body,
            StandardMediaTypes.Json,
            options ?? Core.RequestForge.DefaultJsonSerializerOptions
        );
    }

    internal static HttpContent PlainTextBody(string body)
    {
        ArgumentNullException.ThrowIfNull(body);
        return new StringContent(body, StandardMediaTypes.PlainText);
    }

    internal static HttpContent ByteArrayBody(byte[] body, MediaTypeHeaderValue? contentType = null)
    {
        ArgumentNullException.ThrowIfNull(body);

        var returnData = new ByteArrayContent(body);
        returnData.Headers.ContentType = contentType ?? StandardMediaTypes.Binary;

        return returnData;
    }

    // https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings
    internal static HttpContent DecimalNumberBody(double input, string format = "F3")
    {
        return new StringContent(DecimalNumberValue(input, format), StandardMediaTypes.PlainText);
    }

    #endregion

    #region STRING VALUES

    internal static string DecimalNumberValue(double input, string format = "F3")
    {
        ArgumentNullException.ThrowIfNull(format);
        return input.ToString(format, CultureInfo.InvariantCulture);
    }

    internal static string IntegerValue(long input, string format = "")
    {
        ArgumentNullException.ThrowIfNull(format);
        return input.ToString(format, CultureInfo.InvariantCulture);
    }

    internal static string BooleanValue(bool input)
    {
        return Convert.ToString(input);
    }

    internal static string DateTimeValue(DateTime input, string format = "O")
    {
        ArgumentNullException.ThrowIfNull(format);
        return input.ToString("O");
    }

    internal static string DateOnlyValue(DateTime input, string format = "O")
    {
        ArgumentNullException.ThrowIfNull(format);
        return DateOnly.FromDateTime(input).ToString("O");
    }

    internal static string DateOnlyValue(DateOnly input, string format = "O")
    {
        ArgumentNullException.ThrowIfNull(format);
        return input.ToString("O");
    }

    #endregion
}
