using OpenQA.Selenium;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System;

namespace TMech.Sharp.Selenium;

/// <summary>
/// Contains helper methods for constructing Selenium locators.
/// </summary>
public static class Locator
{
    /// <summary>
    /// Escapes single quotes for use in css selector based locator strings.
    /// </summary>
    public static string EscapeCssValue(string input) => input.Replace("'", "\\'");

    /// <summary>
    /// Lifted verbatim from the C# source code of <see href="https://github.com/SeleniumHQ/selenium/blob/trunk/dotnet%2Fsrc%2Fsupport%2FUI%2FSelectElement.cs">SelectElement</see> (thanks Selenium team! Awesome function).
    /// Converts strings with quotes and/or ticks into escaped equivalents for use with XPath-expressions.
    /// <para>For example: <c>"foo'"bar"</c> turns into <c>concat("foo'", '"', "bar")</c></para>
    /// </summary>
    public static string EscapeQuotesForXPathExpression(string toEscape)
    {
        if (toEscape.IndexOf("\"", StringComparison.OrdinalIgnoreCase) > -1 && toEscape.IndexOf("'", StringComparison.OrdinalIgnoreCase) > -1)
        {
            bool flag = false;
            if (toEscape.LastIndexOf("\"", StringComparison.OrdinalIgnoreCase) == toEscape.Length - 1)
            {
                flag = true;
            }

            List<string> list = new List<string>(toEscape.Split('"'));
            if (flag && string.IsNullOrEmpty(list[list.Count - 1]))
            {
                list.RemoveAt(list.Count - 1);
            }

            StringBuilder stringBuilder = new StringBuilder("concat(");
            for (int i = 0; i < list.Count; i++)
            {
                stringBuilder.Append('\"').Append(list[i]).Append('\"');
                if (i == list.Count - 1)
                {
                    if (flag)
                    {
                        stringBuilder.Append(", '\"')");
                    }
                    else
                    {
                        stringBuilder.Append(')');
                    }
                }
                else
                {
                    stringBuilder.Append(", '\"', ");
                }
            }

            return stringBuilder.ToString();
        }

        if (toEscape.IndexOf('\"', StringComparison.OrdinalIgnoreCase) > -1)
        {
            return string.Format(CultureInfo.InvariantCulture, "'{0}'", toEscape);
        }

        return string.Format(CultureInfo.InvariantCulture, "\"{0}\"", toEscape);
    }

    /// <summary>
    /// Creates a Selenium locator that matches one or more elements whose id-attribute ends with a specific value.
    /// </summary>
    /// <param name="tagName">Optional. Which HTML-tagname to limit the locator to finding. Defaults to none, thus matching any element type.</param>
    public static By ByIdEndsWith(string id, string tagName = "")
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(tagName);

        return By.CssSelector($"{tagName}[id$='{EscapeCssValue(id)}']");
    }

    /// <summary>
    /// Creates a Selenium locator that matches one or more elements whose id-attribute starts with a specific value.
    /// </summary>
    /// <param name="tagName">Optional. Which HTML-tagname to limit the locator to finding. Defaults to none, thus matching any element type.</param>
    public static By ByIdStartsWith(string id, string tagName = "")
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(tagName);

        return By.CssSelector($"{tagName}[id^='{EscapeCssValue(id)}']");
    }

    /// <summary>
    /// Creates a Selenium locator that matches one or more elements whose id-attribute contains a specific value.
    /// </summary>
    /// <param name="tagName">Optional. Which HTML-tagname to limit the locator to finding. Defaults to none, thus matching any element type.</param>
    public static By ByIdContains(string id, string tagName = "")
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(tagName);

        return By.CssSelector($"{tagName}[id*='{EscapeCssValue(id)}']");
    }

    /// <summary>
    /// Creates a Selenium locator that matches one or more elements whose inner text equals a specific value.
    /// </summary>
    /// <param name="tagName">Optional. Which HTML-tagname to limit the locator to finding. Defaults to *, thus matching any element type.</param>
    public static By ByTextEquals(string text, string tagName = "*")
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        ArgumentNullException.ThrowIfNull(tagName);

        return By.XPath($".//{tagName}[normalize-space(text())={EscapeQuotesForXPathExpression(text)}]");
    }

    /// <summary>
    /// Creates a Selenium locator that matches one or more elements where either its own inner text - or any of its descendants' - equals a specific value.
    /// Text is normalized before matching, so you don't have to account for leading or trailing whitespaces for example.
    /// Note that unless you specify <paramref name="tagName"/> you are likely to get the first (outermost) element in the DOM tree whose descendant has the specific <paramref name="text"/>.
    /// </summary>
    /// <param name="tagName">Optional. Which HTML-tagname to limit the locator to finding. Defaults to *, thus matching any element type.</param>
    public static By BySelfOrDescendantTextEquals(string text, string tagName = "*")
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        ArgumentNullException.ThrowIfNull(tagName);

        return By.XPath($".//{tagName}[normalize-space(.)={EscapeQuotesForXPathExpression(text)}]");
    }

    /// <summary>
    /// Creates a Selenium locator that matches one or more elements whose inner text contains a specific value.
    /// </summary>
    /// <param name="tagName">Optional. Which HTML-tagname to limit the locator to finding. Defaults to *, thus matching any element type.</param>
    public static By ByTextContains(string text, string tagName = "*")
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        ArgumentNullException.ThrowIfNull(tagName);

        return By.XPath($".//{tagName}[contains(normalize-space(text()),{EscapeQuotesForXPathExpression(text)})]");
    }

    /// <summary>
    /// Creates a Selenium locator that matches one or more elements where either its own inner text - or any of its descendants' - contains a specific value.
    /// Text is normalized before matching, so you don't have to account for leading or trailing whitespaces for example.
    /// Note that unless you specify <paramref name="tagName"/> you are likely to get the first (outermost) element in the DOM tree whose descendant has the specific <paramref name="text"/>.
    /// </summary>
    /// <param name="tagName">Optional. Which HTML-tagname to limit the locator to finding. Defaults to *, thus matching any element type.</param>
    public static By BySelfOrDescendantTextContains(string text, string tagName = "*")
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        ArgumentNullException.ThrowIfNull(tagName);

        return By.XPath($".//{tagName}[contains(normalize-space(.),{EscapeQuotesForXPathExpression(text)})]");
    }
}
