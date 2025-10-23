using CZ.DM.Art.Core.Shared;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TMech.Sharp.HttpService
{
    /// <summary>
    /// A factory class for building a multipart formdata body using a fluent API.
    /// </summary>
    public sealed class MultipartFormBuilder : IDisposable
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
            //Logger = LogProvider.GetLogFor<MultipartFormBuilder>();

            if (boundary.Length == 0)
            {
                Content = new MultipartFormDataContent();
                //Logger.Info("Initialized with random boundary");
            }
            else
            {
                Content = new MultipartFormDataContent(boundary);
                //Logger.Info($"Initialized with boundary '{boundary}'");
            }
        }

        private bool HasBeenBuilt;
        private readonly MultipartFormDataContent Content;
        //private readonly Logger Logger;
        private bool IsDisposed;

        public MultipartFormBuilder WithString(string content, string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(content);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            //Logger.Info($"With string with field name '{name}': {content}");
            Content.Add(new StringContent(content, MediaTypes.PlainText), name);

            return this;
        }

        public MultipartFormBuilder WithInteger(int content, string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Content.Add(new StringContent(Convert.ToString(content), MediaTypes.PlainText), name);
            //Logger.Info($"With integer with field name '{name}': {content}");

            return this;
        }

        public MultipartFormBuilder WithBoolean(bool content, string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Content.Add(new StringContent(Convert.ToString(content), MediaTypes.PlainText), name);
            //Logger.Info($"With boolean with field name '{name}': {content}");

            return this;
        }

        public MultipartFormBuilder WithJson(string content, string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(content);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Content.Add(new StringContent(content, MediaTypes.Json), name);
            //Logger.Info($"With JSON-string with field name '{name}': {content}");

            return this;
        }

        public MultipartFormBuilder WithJson<T>(T content, string name) where T : class
        {
            ArgumentNullException.ThrowIfNull(content);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            //Logger.Info($"With JSON-object of type {typeof(T)} and field name '{name}'");

            Content.Add(JsonContent.Create(
                content,
                MediaTypes.Json,
                JsonSerialization.StandardOptions
            ), name);

            return this;
        }

        public MultipartFormBuilder WithFile(string filePathAndName, string name)
        {
            return InternalWithFile(filePathAndName, name);
        }

        public MultipartFormBuilder WithFile(string filePathAndName, string name, string mimeType)
        {
            return InternalWithFile(filePathAndName, name, mimeType);
        }

        private MultipartFormBuilder InternalWithFile(string filePathAndName, string name, string mimeType = "application/octet-stream")
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePathAndName);
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
            Content.Add(content, name, sourceFile.Name);

            return this;
        }

        /// <summary>
        /// Constructs a <see cref="MultipartFormDataContent"/> instance from the data in this instance that can be used for <see cref="HttpRequestMessage.Content"/>. Throws an exception if this method has already been called on this instance.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public MultipartFormDataContent Build()
        {
            if (HasBeenBuilt)
            {
                throw new InvalidOperationException("This instance has already been built");
            }

            HasBeenBuilt = true;
            return Content;
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            Content.Dispose();
        }
    }
}
