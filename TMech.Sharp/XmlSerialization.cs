using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace CZ.DM.Art.Core.Shared
{
    public static class StandardNamespaces
    {
        public static readonly XNamespace XSI = "http://www.w3.org/2001/XMLSchema-instance";
        public static readonly XNamespace XSD = "http://www.w3.org/2001/XMLSchema";
        public static readonly XNamespace SOAPv11 = "http://schemas.xmlsoap.org/soap/envelope/";
        public static readonly XNamespace SOAPv12 = "http://www.w3.org/2003/05/soap-envelope";
    }

    public class XmlSerialization
    {
        /// <summary>
        /// Attempts to parse a string to XML and then serializes it back to a string that is indented for easy reading. Returns the original string if it can't be parsed as XML.
        /// </summary>
        public static string PrettyPrint(string input)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(input);

            try
            {
                return XDocument.Parse(input).ToString();
            }
            catch (XmlException)
            {
                return input;
            }
        }

        /// <summary>Serializes an <see cref="XElement" /> to a <c>UTF-8</c> encoded XML-string that is indented for easy reading. Optionally leaves out the XML declaration.</summary>
        public static string Serialize(XElement input, bool omitXmlDeclaration = false)
        {
            ArgumentNullException.ThrowIfNull(input);

            using (var outputStream = new MemoryStream())
            {
                var settings = new XmlWriterSettings()
                {
                    Encoding = new UTF8Encoding(false),
                    Indent = true,
                    OmitXmlDeclaration = omitXmlDeclaration
                };

                using (XmlWriter xmlWriter = XmlWriter.Create(outputStream, settings))
                {
                    input.WriteTo(xmlWriter);
                }

                return Encoding.UTF8.GetString(outputStream.ToArray());
            }
        }

        public static XElement ToRequiredXmlElement<T>(T input, string name, XNamespace? xmlNamespace = null)
        {
            ArgumentNullException.ThrowIfNull(input);
            if (input is string stringValue)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(stringValue);
            }

            XName xmlName = xmlNamespace is null ? name : xmlNamespace + name;
            return new XElement(xmlName, input);
        }

        public static XElement ToNillableXmlElement<T>(T? input, string name, XNamespace? xmlNamespace = null)
        {
            if (input is null)
            {
                XName xmlName = xmlNamespace is null ? name : xmlNamespace + name;
                var returnData = new XElement(xmlName);
                returnData.SetAttributeValue(StandardNamespaces.XSI + "nil", "true");
                return returnData;
            }

            return new XElement((xmlNamespace ?? string.Empty) + name, input);
        }

        #region OPTIONAL

        public static XElement? ToOptionalXmlElement(string? input, string name, XNamespace? xmlNamespace = null)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            XName xmlName = xmlNamespace is null ? name : xmlNamespace + name;
            return new XElement(xmlName, input);
        }

        public static XElement? ToOptionalXmlElement(int? input, string name, XNamespace? xmlNamespace = null)
        {
            if ((input ?? 0) == 0) return null;
            XName xmlName = xmlNamespace is null ? name : xmlNamespace + name;
            return new XElement(xmlName, input);
        }

        public static XElement? ToOptionalXmlElement(long? input, string name, XNamespace? xmlNamespace = null)
        {
            if ((input ?? 0) == 0) return null;
            XName xmlName = xmlNamespace is null ? name : xmlNamespace + name;
            return new XElement(xmlName, input);
        }

        public static XElement? ToOptionalXmlElement(Enum input, string name, XNamespace? xmlNamespace = null)
        {
            if (Convert.ToInt32(input) == 0) return null;
            XName xmlName = xmlNamespace is null ? name : xmlNamespace + name;
            return new XElement(xmlName, input);
        }

        public static XElement? ToOptionalXmlElement(bool? input, string name, XNamespace? xmlNamespace = null)
        {
            if (input is null) return null;
            XName xmlName = xmlNamespace is null ? name : xmlNamespace + name;
            return new XElement(xmlName, input);
        }

        #endregion

        /// <summary>Creates an XML-element around a value. If <paramref name="input"/> is <c>null</c> you get an empty XML-element.</summary>
        public static XElement? ToXmlElement<T>(T? input, string name, XNamespace? xmlNamespace = null)
        {
            XName xmlName = xmlNamespace is null ? name : xmlNamespace + name;
            return new XElement(xmlName, input is null ? string.Empty : input);
        }
    }
}
