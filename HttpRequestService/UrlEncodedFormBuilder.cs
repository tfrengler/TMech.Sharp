using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;

namespace TMech.Sharp.HttpRequestService;

/// <summary>
/// A factory class for building a url encoded formdata body using a fluent API.
/// </summary>
public sealed class UrlEncodedFormBuilder
{
    /// <summary>
    /// Creates a new instance of the factory with an empty url encoded formdata body that you can add content to.
    /// </summary>
    public static UrlEncodedFormBuilder Create() { return new UrlEncodedFormBuilder(); }
    private UrlEncodedFormBuilder()
    {
        _content = new List<KeyValuePair<string, string>>();
    }

    private bool _hasBeenBuilt;
    private readonly List<KeyValuePair<string, string>> _content;

    public UrlEncodedFormBuilder WithString(string content, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _content.Add(new KeyValuePair<string, string>(name, content));
        return this;
    }

    public UrlEncodedFormBuilder WithString(Enum content, string name)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string? Value = Enum.GetName(content.GetType(), content);
        Debug.Assert(Value is not null);

        _content.Add(new KeyValuePair<string, string>(name, Value));
        return this;
    }

    public UrlEncodedFormBuilder WithString(DateTime content, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string Value = content.ToString("o");

        _content.Add(new KeyValuePair<string, string>(name, Value));
        return this;
    }

    public UrlEncodedFormBuilder WithInteger(int content, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string Value = Convert.ToString(content);
        _content.Add(new KeyValuePair<string, string>(name, Value));

        return this;
    }

    public UrlEncodedFormBuilder WithBoolean(bool content, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string Value = Convert.ToString(content);
        _content.Add(new KeyValuePair<string, string>(name, Value));

        return this;
    }

    /// <summary>
    /// Constructs a <see cref="FormUrlEncodedContent"/> instance from the data in this instance that can be used for <see cref="HttpRequestMessage.Content"/>.<br/>
    /// If no content has been added then <see langword="null"/> is returned instead.<br/>
    /// Throws an exception if this method has already been called on this instance.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public FormUrlEncodedContent? Build()
    {
        if (_hasBeenBuilt)
        {
            throw new InvalidOperationException("This instance has already been built");
        }

        _hasBeenBuilt = true;

        if (_content.Count > 0)
        {
            return new FormUrlEncodedContent(_content);
        }

        return null;
    }
}
