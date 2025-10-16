using System;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace CZ.DM.Art.Core.HttpService
{
    public sealed class KnownHeaders
    {
        public KnownHeaders(in List<KeyValuePair<string, string>> headerContainer, in HttpRequest httpRequest)
        {
            HeaderContainer = headerContainer;
            HttpRequest = httpRequest;

            //Logger = LogProvider.GetLogFor<KnownHeaders>();
        }

        private readonly List<KeyValuePair<string, string>> HeaderContainer;
        private readonly HttpRequest HttpRequest;
        //private readonly Logger Logger;

        public KnownHeaders Authorization_Bearer(string token)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(token);
            //Logger.Info("With Authorization header of type 'Bearer' and a token");
            HeaderContainer.Add(new("Authorization", "Bearer " + token.Trim()));
            return this;
        }

        public KnownHeaders CZ_ApimSubscription(string subscriptionKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionKey);
            //Logger.Info("With Apim subscription key header");
            HeaderContainer.Add(new("Ocp-Apim-Subscription-Key", subscriptionKey));
            return this;
        }

        public KnownHeaders CacheControl_NoStore()
        {
            //Logger.Info("With Cache-Control header");
            HeaderContainer.Add(new("Cache-Control", "no-store"));
            return this;
        }

        public KnownHeaders Accept(MediaTypeWithQualityHeaderValue value)
        {
            ArgumentNullException.ThrowIfNull(value);
            //Logger.Info("With Accept-header");
            HeaderContainer.Add(new("Accept", value.ToString()));
            return this;
        }

        public KnownHeaders Accept(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            //Logger.Info("With Accept-header");
            HeaderContainer.Add(new("Accept", value));
            return this;
        }

        public KnownHeaders Correlation(string type = "DM_TestAutomation")
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(type);
            //Logger.Info("With correlation-type and -id headers");
            HeaderContainer.Add(new("x-correlation-type", type));
            HeaderContainer.Add(new("x-correlation-id", Guid.NewGuid().ToString()));
            return this;
        }

        public HttpRequest Done()
        {
            return HttpRequest;
        }
    }
}
