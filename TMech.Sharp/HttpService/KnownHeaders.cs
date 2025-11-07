using System.Collections.Generic;

namespace TMech.Sharp.HttpService
{
    public sealed class KnownHeaders
    {
        private Dictionary<string,string> _headers;

        public KnownHeaders(Dictionary<string, string> headers)
        {
            _headers = headers;
        }

        public KnownHeaders Accept(string? value)
        {
            if (value is not null)
            {
                _headers.Add("Accept", value);
            }

            return this;
        }
    }
}

