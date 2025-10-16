using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;

namespace CZ.DM.Art.Core.HttpService
{
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
            Content = new List<KeyValuePair<string, string>>();
            //Logger = LogProvider.GetLogFor<UrlEncodedFormBuilder>();
            //Logger.Info("UrlEncodedFormBuilder initialized");
        }

        private bool HasBeenBuilt;
        private readonly List<KeyValuePair<string, string>> Content;
        //private readonly Logger Logger;

        public UrlEncodedFormBuilder WithString(string content, string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(content);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            //Logger.Info($"With string value '{content}' in key '{name}'");
            Content.Add(new KeyValuePair<string, string>(name, content));
            return this;
        }

        public UrlEncodedFormBuilder WithString(Enum content, string name)
        {
            ArgumentNullException.ThrowIfNull(content);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            string? Value = Enum.GetName(content.GetType(), content);
            Debug.Assert(Value is not null);
            //Logger.Info($"With enum value '{Value}' in key '{name}'");

            Content.Add(new KeyValuePair<string, string>(name, Value));
            return this;
        }

        public UrlEncodedFormBuilder WithString(DateTime content, string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            string Value = content.ToString("o");
            //Logger.Info($"With datetime value '{Value}' in key '{name}'");

            Content.Add(new KeyValuePair<string, string>(name, Value));
            return this;
        }

        public UrlEncodedFormBuilder WithInteger(int content, string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            string Value = Convert.ToString(content);
            //Logger.Info($"With datetime value '{Value}' in key '{name}'");

            Content.Add(new KeyValuePair<string, string>(name, Value));
            return this;
        }

        public UrlEncodedFormBuilder WithBoolean(bool content, string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            string Value = Convert.ToString(content);
            //Logger.Info($"With boolean value '{Value}' in key '{name}'");

            Content.Add(new KeyValuePair<string, string>(name, Value));
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
            //Logger.Info($"Building UrlEncodedFormBuilder-instance with {Content.Count} values");

            if (HasBeenBuilt)
            {
                throw new InvalidOperationException("This instance has already been built");
            }

            HasBeenBuilt = true;

            if (Content.Count > 0)
            {
                return new FormUrlEncodedContent(Content);
            }

            return null;
        }
    }
}
