using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CZ.DM.Art.Core.Shared
{
    public sealed class JsonSerialization
    {
        private object? _input;
        private readonly JsonSerializerOptions _settings;

        public JsonSerialization()
        {
            _settings = new();
        }

        /// <summary>
        /// Prepare the input to be serialized with default encoding, unindented, with enums written as integer values and including null values.
        /// </summary>
        public static JsonSerialization WithInput<T>(T input)
        {
            ArgumentNullException.ThrowIfNull(input);
            return new JsonSerialization()
            {
                _input = input,
            };
        }

        static JsonSerialization()
        {
            StandardOptions = new JsonSerialization()
                .WriteIndented()
                .WithEnumsAsStrings()
                .WithoutEscaping()
                .Options();

            OptionsWithoutNullValues = new JsonSerialization()
                .WriteIndented()
                .OmitNullValues()
                .WithoutEscaping()
                .Options();
        }

        /// <summary>
        /// Serializes the input to a pretty printed JSON-string using standard settings (enums as strings, no escaping/encoding, null-values included)
        /// </summary>
        public static string Serialize<T>(T input)
        {
            return JsonSerializer.Serialize(input, StandardOptions);
        }

        /// <summary>
        /// Serializes the input to a pretty printed JSON-string with enums as integer values, no escaping/encoding, null-values omitted
        /// </summary>
        public static string SerializeAndOmitNullValues<T>(T input)
        {
            return JsonSerializer.Serialize(input, OptionsWithoutNullValues);
        }

        public static JsonSerializerOptions StandardOptions { get; }
        public static JsonSerializerOptions OptionsWithoutNullValues { get; }

        public JsonSerialization WriteIndented()
        {
            _settings.WriteIndented = true;
            return this;
        }

        public JsonSerialization WithoutEscaping()
        {
            _settings.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            return this;
        }

        public JsonSerialization OmitNullValues()
        {
            _settings.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            return this;
        }

        public JsonSerialization WithEnumsAsStrings()
        {
            _settings.Converters.Add(new JsonStringEnumConverter(null, false));
            return this;
        }

        public string Serialize()
        {
            return JsonSerializer.Serialize(_input, _settings);
        }

        public T? Deserialize<T>(string input)
        {
            return JsonSerializer.Deserialize<T>(input, _settings);
        }

        public JsonSerializerOptions Options()
        {
            return _settings;
        }
    }
}
