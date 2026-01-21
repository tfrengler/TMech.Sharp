using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace RequestForge.Builders;

/// <summary>
/// A factory class for building a multipart formdata body using a fluent API.
/// </summary>
public sealed class MultipartFormBuilder
{
    /// <summary>
    /// Creates a new instance of the factory with an empty multipart formdata body that you can add content to.
    /// </summary>
    public static MultipartFormBuilder Create()
    {
        return new MultipartFormBuilder();
    }

    /// <summary>
    /// Creates a new instance of the factory with a specific boundary string and an empty multipart formdata body that you can add content to.
    /// </summary>
    public static MultipartFormBuilder CreateWithBoundary(string boundary)
    {
        return new MultipartFormBuilder(boundary);
    }

    private MultipartFormBuilder(string boundary = "")
    {
        if (boundary.Length == 0)
        {
            _content = new MultipartFormDataContent();
        }
        else
        {
            _content = new MultipartFormDataContent(boundary);
        }
    }

    private bool _hasBeenBuilt;
    private readonly MultipartFormDataContent _content;

    public MultipartFormBuilder WithString(string? content, string name)
    {
        if (content is null) return this;
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _content.Add(ContentBuilder.PlainTextBody(content), name);

        return this;
    }

    public MultipartFormBuilder WithInteger(int? content, string name)
    {
        if (content is null) return this;
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _content.Add(
            ContentBuilder.PlainTextBody(
                ContentBuilder.IntegerValue(content.Value)
            ),
            name
        );

        return this;
    }

    public MultipartFormBuilder WithBoolean(bool? content, string name)
    {
        if (content is null) return this;
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _content.Add(
            ContentBuilder.PlainTextBody(
                ContentBuilder.BooleanValue(content.Value)
            ),
            name
        );

        return this;
    }

    public MultipartFormBuilder WithJson(string? content, string name)
    {
        if (content is null) return this;
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _content.Add(ContentBuilder.JsonBody(content), name);

        return this;
    }

    public MultipartFormBuilder WithJson<T>(T? content, string name) where T : class
    {
        if (content is null) return this;
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _content.Add(ContentBuilder.JsonBody<T>(content), name);

        return this;
    }

    public MultipartFormBuilder WithFile(string? filePathAndName, string name)
    {
        return InternalWithFile(filePathAndName, name);
    }

    public MultipartFormBuilder WithFile(string? filePathAndName, string name, string mimeType)
    {
        return InternalWithFile(filePathAndName, name, mimeType);
    }

    private MultipartFormBuilder InternalWithFile(string? filePathAndName, string name, string mimeType = "application/octet-stream")
    {
        if (filePathAndName is null) return this;
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);

        //Logger.Info($"With file with field name '{name}' and mime-type '{mimeType}': {filePathAndName}");

        var sourceFile = new FileInfo(filePathAndName);

        if (!sourceFile.Exists)
        {
            throw new FileNotFoundException("Unable to add file to multipart formdata as it cannot be found: " + sourceFile.FullName);
        }

        HttpContent content;

        // If bigger than 50mb use a stream instead of loading the entire thing into memory.
        if (sourceFile.Length > 50 * 1024 * 1024)
        {
            var fileStream = File.OpenRead(sourceFile.FullName);
            content = new StreamContent(fileStream);
        }
        else
        {
            byte[] fileContent = File.ReadAllBytes(sourceFile.FullName);
            content = new ByteArrayContent(fileContent);
        }

        content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        _content.Add(content, name, sourceFile.Name);

        return this;
    }

    /// <summary>
    /// Constructs a <see cref="MultipartFormDataContent"/> instance from the data in this instance that can be used for <see cref="HttpRequestMessage.Content"/>. Throws an exception if this method has already been called on this instance.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public MultipartFormDataContent Build()
    {
        if (_hasBeenBuilt)
        {
            throw new InvalidOperationException("This instance has already been built");
        }

        _hasBeenBuilt = true;
        return _content;
    }
}
